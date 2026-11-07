# 2026-11-07 循环开始手机提示设计

## 背景

从二周目起，每周目循环重置后玩家在教室座位上醒来，先播放一句「手机在震动。」，随后自动打开手机记事本，并把提示「下午出去转转吧」写入记事本。

## 需求约束

- 二周目起每轮重置后都播一次；一周目保持原有 `start` 开场。
- 二周目起出生点改为座位（`Room_Classroom/FirstLoopSpawn`，`700,520`）。
- 记事本提示写入 `GameState.NotebookText`，跨循环保留；已有内容时换行追加到末尾，已包含该句则跳过，避免每周目重复叠行。
- 不新增 `.dialogue` 文件，复用已在 POT 列表里的 `Dialogue/day1.dialogue`，不改 `project.godot`。

## 架构图

```mermaid
graph TD
    GS[GameScene] -->|导出引用| PS[PhoneScreen]
    GS -->|ShowDialogueBalloon| DM[DialogueManager]
    DM -->|phone_vibrate cue| D1["Dialogue/day1.dialogue"]
    GS -->|OpenNotebookWithNote| PS
    PS -->|读写 NotebookText| GST[GameState]
    LR[LocationRoot] -->|ResolveStartSpawn| RM[Room]
    RM -->|FirstLoopSpawn 座位| PL[Player]
```

## 数据流图

```mermaid
flowchart LR
    A["ResetLoop: LoopCount++"] --> B[GameScene.OnLoopReset]
    B --> C[重载 StartLocation]
    C --> D["LocationRoot 落点 = FirstLoopSpawn"]
    D --> E[ShowLoopStartIntro]
    E -->|LoopCount >= 2| F["播放 phone_vibrate"]
    F --> G["balloon TreeExited"]
    G --> H["PhoneScreen.OpenNotebookWithNote"]
    H --> I["NotebookText 追加提示"]
    I --> J[显示记事本面板]
```

## 调用链图

```mermaid
sequenceDiagram
    participant GS as GameScene
    participant DM as DialogueManager
    participant B as DialogueBalloon
    participant PS as PhoneScreen
    participant GST as GameState

    GS->>DM: ShowDialogueBalloon(day1, "phone_vibrate")
    DM->>B: 实例化并播放
    B-->>GS: TreeExited
    GS->>PS: OpenNotebookWithNote("下午出去转转吧")
    PS->>PS: Open()（锁输入、隐藏手机按钮）
    PS->>GST: 追加 NotebookText
    PS->>PS: ShowNotebook()
```

## 实现要点

1. `LocationRoot.ResolveStartSpawn`：去掉 `IsFirstLoop()` 判断，始终优先 `Room.FirstLoopSpawn`，留空时回落 `DefaultSpawn`。`Room.FirstLoopSpawn` 注释同步为「地点起始落点」。
2. `Dialogue/day1.dialogue` 新增 `~ phone_vibrate`，正文一行「手机在震动。」。
3. `GameScene`：
   - 新增导出 `PhoneScreen PhoneScreen`，`GameScene.tscn` 的 `node_paths` 与属性同步。
   - `ShowOpeningNarration` 扩展为 `ShowLoopStartIntro`：一周目播 `start`；`LoopCount >= 2` 播 `phone_vibrate`，等 balloon `TreeExited` 后调 `PhoneScreen.OpenNotebookWithNote(LoopStartNote)`。
   - `_Ready` 与 `OnLoopReset` 末尾调用 `ShowLoopStartIntro`。
   - `LoopStartNote = "下午出去转转吧"`。
4. `PhoneScreen.OpenNotebookWithNote(string note)`：未打开时走 `Open()` 流程（锁输入、隐藏手机按钮），追加建议文本后 `ShowNotebook()`。
   - 追加规则：记事本为空 → 直接写；已包含该句 → 跳过；否则 `text + "\n" + note`。
5. 手机自动打开后，玩家按「返回 / Esc」关闭手机后恢复移动。

## 错误处理

- `OpeningNarration` 为空、`ShowDialogueBalloon` 返回空、`PhoneScreen` 导出引用为空：`GD.PushError` 显式报错，不静默兜底。
- heartbeat：`QueueFree` 后 `ToSignal` 已失效时先 `IsInstanceValid` 判断。

# 正式主流程与剧情导入 实现计划

> **面向 AI 代理的工作者：** 必需子技能：使用 superpowers:subagent-driven-development（推荐）或 superpowers:executing-plans 逐任务实现此计划。步骤使用复选框（`- [ ]`）语法来跟踪进度。

**目标：** 把占位流程换成飞书《剧本》的正式四周目主线：一周目锁教室看书、二周目情报与地点解锁、三周目偷钥匙 / 笔记本 / 炸药取样、四周目湿炸药打真结局；红字重要信息变成 8 篇手机文档。

**架构：** 沿用现有 `GameScene` + `LocationRoot` + `Room` + `RoomDoor` + `Npc` + `GameState` + `DialogueBalloon`。跨场景状态全部走 `GameState`（跨轮线索 / 文档 / 解锁 + 本轮标记）；门、座位、抽屉、炸药、水管、等待点各做一个小的 `Interactable` 子类；密码面板、结局黑屏、17:30 闪白各做一个 `CanvasLayer` 场景。对白全部写在 `.dialogue`，C# 只负责状态、场景与交互。

**技术栈：** Godot 4.7.2 mono / C# / Dialogue Manager 4.2.0

**规格：** `docs/superpowers/specs/2026-11-07-formal-main-flow-design.md`

**项目约束（覆盖通用流程）：**

- git 只读：**不执行** `git commit` / `git branch` / `git checkout` / `git push`。每个任务以编译 / 无头运行作为检查点。
- 不引入单元测试框架（AGENTS.md 禁止过度设计）；验证 = Godot headless 导入 / 编译 / 启动 + 有窗口手测。
- 脚本一律 C#；`.cs` 文件名与类名一致，`public partial class X : Node` 风格。
- 对白只写在 `Dialogue/**/*.dialogue`；C# 不硬编码台词。
- 空引用、资源缺失必须显式 `PushError`，禁止默认值或空实现静默兜底。
- 改 `project.godot` 前确认 Godot 编辑器已关闭（本计划执行时已确认无 Godot 进程）。
- `.tscn` 里 C# 的节点引用属性必须写进节点头的 `node_paths=PackedStringArray(...)`；只写 `属性 = NodePath("...")` 会得到空引用。
- 新增 `.dialogue` 必须同步 `project.godot` 的 `locale/translations_pot_files`；`Dialogue/day1.dialogue` 必须保留（POT 登记文件缺失会删掉 `[internationalization]` 段）。

---

## 执行进度

**执行顺序：** 1 → 2 → 3 → 4 → 5 → 6 → 7 → 8 → 9 → 13 → 14 → 15 → 10 → 11 → 12 → 16 → 17。

场景任务（10 / 11 / 12）的 `.tscn` 会引用剧本与文档资源，必须先完成 13 / 14 / 15，否则 `--import` 报缺失资源。

- [ ] 任务 1：`GameState` 本轮标记、分析时间、结局接口
- [ ] 任务 2：`Npc` 按本轮标记显隐
- [ ] 任务 3：`RoomDoor` 线索锁与锁定提示复用
- [ ] 任务 4：门脚本 `ClassroomDoor` / `LabDoor` / `OfficeDoor`
- [ ] 任务 5：交互物 `BookDesk` / `KeyedDrawer` / `BombInteractable` / `WaterPipe` / `WaitSpot`
- [ ] 任务 6：`DialogueBalloon` 真结局不解锁
- [ ] 任务 7：UI 脚本 `PasswordPanel` / `EndingOverlay` / `LoopEndNotice`
- [ ] 任务 8：UI 场景 `PasswordPanel.tscn` / `EndingOverlay.tscn` / `LoopEndNotice.tscn`
- [ ] 任务 9：`GameScene` 结局信号、终末提示与主场景挂载
- [ ] 任务 13：8 篇文档资源与注册表
- [ ] 任务 14：剧本 `yuka` / `shinji` / `yamada` / `shinji_lab`
- [ ] 任务 15：剧本 `yamada_underground` 与 5 个物体剧本
- [ ] 任务 10：`Classroom.tscn` 化学实验室 / 座位 / 教室门 / 新井显隐
- [ ] 任务 11：`Administration.tscn` 办公室门 / 抽屉 / 密码面板
- [ ] 任务 12：`Playground.tscn` 炸药 / 水管 / 等待点 / 地下门 / 山田剧本
- [ ] 任务 16：POT 登记
- [ ] 任务 17：全链路验证与 `AGENTS.md` 同步

---

## 文件结构

**新增：**

| 文件 | 职责 |
|---|---|
| `Scripts/Location/ClassroomDoor.cs` | 一周目选看书后挡住教室门，播 BL 独白 |
| `Scripts/Location/LabDoor.cs` | 化学实验室门，分析未完成时跳到完成时间 |
| `Scripts/Location/OfficeDoor.cs` | 办公室门，只在 14:00-14:59 开放 |
| `Scripts/Location/BookDesk.cs` | 教室座位看书推进 |
| `Scripts/Location/KeyedDrawer.cs` | 办公室抽屉钥匙检查 |
| `Scripts/Location/BombInteractable.cs` | 地下炸药取样 / 湿炸药 |
| `Scripts/Location/WaterPipe.cs` | 器材准备室接水 |
| `Scripts/Location/WaitSpot.cs` | 地下服务器等到 17:00 |
| `Scripts/UI/PasswordPanel.cs` | 笔记本密码面板逻辑 |
| `Scripts/UI/EndingOverlay.cs` | 真结局黑屏 / The End / 失败黑屏 |
| `Scripts/UI/LoopEndNotice.cs` | 17:30 闪白与「轰———」 |
| `Scenes/UI/PasswordPanel.tscn` | 密码面板 UI |
| `Scenes/UI/EndingOverlay.tscn` | 结局黑屏 UI |
| `Scenes/UI/LoopEndNotice.tscn` | 终末提示 UI |
| `Dialogue/Npc/shinji_lab.dialogue` | 15:30 新井给结果 |
| `Dialogue/Npc/yamada_underground.dialogue` | 17:00 对质 + 甜品店尾声 |
| `Dialogue/Objects/classroom_desk.dialogue` | 看书 |
| `Dialogue/Objects/classroom_block.dialogue` | 教室门阻挡独白 |
| `Dialogue/Objects/underground_bomb.dialogue` | 炸药交互 |
| `Dialogue/Objects/equipment_water.dialogue` | 水管交互 |
| `Dialogue/Objects/underground_wait.dialogue` | 等待点 |
| `Resources/Documents/{shinji_schedule,yuka_secret,equipment_room,yamada_tracks,laptop_password,chem_lab,bomb_weakness,yamada_notebook}.tres` | 8 篇文档 |

**修改：**

| 文件 | 变更 |
|---|---|
| `Scripts/Core/GameState.cs` | 本轮标记、`LoopFlagsChanged`、`BombAnalysisReadyMinute`、`StartBombAnalysis`、`RemoveItem`、结局信号、`IsTrueEnding` |
| `Scripts/Npc/Npc.cs` | `PresentLoopFlag` / `VisibleWhenFlagSet` + `LoopFlagsChanged` |
| `Scripts/Location/RoomDoor.cs` | `RequiredClueId`、`ShowLockedHint` |
| `Scripts/Dialogue/DialogueBalloon.cs` | `_ExitTree` 在真结局时不解锁 |
| `Scripts/Map/GameScene.cs` | 结局信号、`LoopEndNotice`、重置收尾 |
| `Scenes/Locations/Classroom.tscn` | 化学实验室、座位、教室门、新井显隐、实验室新井 |
| `Scenes/Locations/Administration.tscn` | 办公室门、抽屉、密码面板 |
| `Scenes/Locations/Playground.tscn` | 炸药、水管、等待点、地下门线索锁、山田剧本 |
| `Scenes/Root/GameScene.tscn` | `EndingOverlay`、`LoopEndNotice` |
| `Dialogue/Npc/yuka.dialogue` | 正式剧本 |
| `Dialogue/Npc/shinji.dialogue` | 正式剧本 |
| `Dialogue/Npc/yamada.dialogue` | 正式剧本 |
| `Resources/Documents/DocumentLibrary.tres` | 注册 8 篇；移除 `club_notice` |
| `project.godot` | POT 登记新增剧本 |
| `AGENTS.md` | 主流程 / NPC 显隐 / 文档 / 结构同步 |

**不改：** `addons/dialogue_manager/**`、`Scenes/Dialogue/DialogueBalloon.tscn`、`Scenes/Phone/PhoneScreen.tscn`、`Scripts/Player/Player.cs`、`Scripts/Map/MapScreen.cs`、`Resources/MapConfig.tres`、`default_bus_layout.tres`、`Dialogue/day1.dialogue`、`Dialogue/Npc/student.dialogue`。

---

## 任务 1：`GameState` 本轮标记、分析时间、结局接口

**文件：**
- 修改：`Scripts/Core/GameState.cs`

- [ ] **步骤 1：在 `_npcUsedChoices` 之后加入本轮状态字段**

在 `private readonly Dictionary<string, HashSet<string>> _npcUsedChoices = new();` 之后插入：

```csharp
	/// <summary>本轮剧情标记；每轮 ResetLoop 清空，跨循环不保留。</summary>
	private readonly HashSet<string> _loopFlags = new();

	/// <summary>新井分析完成的循环内分钟；-1 表示本轮没有开始分析。</summary>
	public int BombAnalysisReadyMinute { get; private set; } = -1;

	/// <summary>是否已进入真结局；DialogueBalloon 据此不做退出解锁。</summary>
	public bool IsTrueEnding { get; private set; }
```

- [ ] **步骤 2：在现有 `[Signal]` 之后加入新信号**

在 `[Signal] public delegate void LoopResetEventHandler();` 之后插入：

```csharp
	[Signal] public delegate void LoopFlagsChangedEventHandler();
	[Signal] public delegate void EpilogueStartedEventHandler();
	[Signal] public delegate void TrueEndingReachedEventHandler();
	[Signal] public delegate void LoopFailedEventHandler();
```

- [ ] **步骤 3：在 `MarkChoiceUsed` 之后加入本轮标记、分析、物品与结局方法**

```csharp
	/// <summary>本轮是否设置了某个剧情标记。</summary>
	public bool HasLoopFlag(string flag)
	{
		if (string.IsNullOrEmpty(flag))
		{
			GD.PushError("GameState.HasLoopFlag: 标记为空");
			return false;
		}
		return _loopFlags.Contains(flag);
	}

	/// <summary>设置本轮剧情标记；重复设置无副作用，并通知按标记显隐的 NPC 刷新。</summary>
	public void SetLoopFlag(string flag)
	{
		if (string.IsNullOrEmpty(flag))
		{
			GD.PushError("GameState.SetLoopFlag: 标记为空");
			return;
		}
		if (!_loopFlags.Add(flag)) return;

		GD.Print($"[LoopFlag] {flag}");
		EmitSignal(SignalName.LoopFlagsChanged);
	}

	/// <summary>把炸药样本交给新井：记录两小时后的完成时间，并写入本轮标记。</summary>
	public void StartBombAnalysis()
	{
		BombAnalysisReadyMinute = ElapsedMinutes + 120;
		SetLoopFlag("shinji_analyzing");
		GD.Print($"[Bomb] 分析开始，{BombAnalysisReadyMinute} 分钟时完成");
	}

	/// <summary>移除本轮道具；不存在时无副作用。</summary>
	public void RemoveItem(string itemId)
	{
		if (string.IsNullOrEmpty(itemId))
		{
			GD.PushError("GameState.RemoveItem: 道具 id 为空");
			return;
		}
		if (Inventory.Remove(itemId)) GD.Print($"[Item] 失去 {itemId}");
	}

	/// <summary>由 yamada_underground.dialogue 调用：开始甜品店尾声。</summary>
	public void EnterEpilogue() => EmitSignal(SignalName.EpilogueStarted);

	/// <summary>由 yamada_underground.dialogue 调用：真结局结束。</summary>
	public void ReachTrueEnding()
	{
		IsTrueEnding = true;
		EmitSignal(SignalName.TrueEndingReached);
	}

	/// <summary>由 yamada_underground.dialogue 调用：本轮失败。</summary>
	public void FailLoop() => EmitSignal(SignalName.LoopFailed);
```

- [ ] **步骤 4：更新 `ResetLoop`**

把方法体改为：

```csharp
	public void ResetLoop()
	{
		ElapsedMinutes = 0;
		IsLoopEnded = false;
		IsTrueEnding = false;
		LoopCount++;
		BombAnalysisReadyMinute = -1;

		Inventory.Clear();
		_npcLoopStates.Clear();
		_npcUsedChoices.Clear();
		_loopFlags.Clear();

		GD.Print($"[Loop] 第 {LoopCount} 周目开始");
		EmitSignal(SignalName.TimeChanged, 0);
		EmitSignal(SignalName.LoopReset);
	}
```

并把方法上方的 XML 注释改成：

```csharp
	/// <summary>循环重置：时间归零、周目 +1，清本轮标记与道具，保留解锁、线索、文档与记事本。</summary>
```

- [ ] **步骤 5：编译**

```powershell
$godot = "D:\Downloads\Compressed\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64.exe"
& $godot --headless --path . --build-solutions --quit
```

预期：退出码 0，无 `error CS`。

---

## 任务 2：`Npc` 按本轮标记显隐

**文件：**
- 修改：`Scripts/Npc/Npc.cs`

- [ ] **步骤 1：加入导出字段**

在 `[Export] public Sprite2D Sprite { get; set; }` 之后插入：

```csharp
	/// <summary>可选的在场条件：本轮标记 id；留空表示不按标记过滤。</summary>
	[Export] public string PresentLoopFlag { get; set; } = "";

	/// <summary>为 true 时标记存在才在场；为 false 时标记存在则离场。</summary>
	[Export] public bool VisibleWhenFlagSet { get; set; } = true;
```

- [ ] **步骤 2：订阅与退订 `LoopFlagsChanged`**

`_Ready()` 里，在 `GameState.Instance.TimeChanged += OnTimeChanged;` 之后插入：

```csharp
		GameState.Instance.LoopFlagsChanged += OnLoopFlagsChanged;
```

`_ExitTree()` 里，在 `GameState.Instance.TimeChanged -= OnTimeChanged;` 之后插入：

```csharp
			GameState.Instance.LoopFlagsChanged -= OnLoopFlagsChanged;
```

- [ ] **步骤 3：刷新时叠加标记条件**

在 `RefreshVisual()` 中，把：

```csharp
		_present = frame.Visible;
```

替换为：

```csharp
		bool flagSatisfied = string.IsNullOrEmpty(PresentLoopFlag)
			|| GameState.Instance.HasLoopFlag(PresentLoopFlag) == VisibleWhenFlagSet;
		_present = frame.Visible && flagSatisfied;
```

- [ ] **步骤 4：加入标记变化回调**

在 `OnTimeChanged` 之后插入：

```csharp
	private void OnLoopFlagsChanged()
	{
		RefreshVisual();
	}
```

- [ ] **步骤 5：编译**

```powershell
& $godot --headless --path . --build-solutions --quit
```

预期：退出码 0，无 `error CS`。

---

## 任务 3：`RoomDoor` 线索锁与锁定提示复用

**文件：**
- 修改：`Scripts/Location/RoomDoor.cs`

- [ ] **步骤 1：用下面的完整内容替换文件**

```csharp
using Godot;

/// <summary>房间之间的门。可配线索锁与钥匙锁，锁着时显示提示。</summary>
public partial class RoomDoor : Interactable
{
	/// <summary>门通向的房间。</summary>
	[Export] public Room TargetRoom { get; set; }

	/// <summary>目标房间内的落点。</summary>
	[Export] public Marker2D TargetSpawn { get; set; }

	/// <summary>开门所需道具 id；留空表示不按道具上锁。</summary>
	[Export] public string RequiredKeyId { get; set; } = string.Empty;

	/// <summary>开门所需线索 id；留空表示不按线索上锁。</summary>
	[Export] public string RequiredClueId { get; set; } = string.Empty;

	/// <summary>穿过这扇门消耗的分钟数；0 表示免费。</summary>
	[Export] public int TravelMinutes { get; set; } = 5;

	/// <summary>锁着时闪一下的提示。</summary>
	[Export] public Label LockedHint { get; set; }

	private Timer _hintTimer;

	public override void _Ready()
	{
		if (TargetRoom == null)
		{
			GD.PushError($"RoomDoor {Name}: TargetRoom 为空");
		}
		if (TargetSpawn == null)
		{
			GD.PushError($"RoomDoor {Name}: TargetSpawn 为空");
		}
		if (GameState.Instance == null)
		{
			GD.PushError($"RoomDoor {Name}: GameState 未就绪");
		}
		if (IsLocked() && LockedHint == null)
		{
			GD.PushError($"RoomDoor {Name}: 已上锁但 LockedHint 为空");
		}
		if (TravelMinutes < 0)
		{
			GD.PushError($"RoomDoor {Name}: TravelMinutes 不能为负");
		}

		if (LockedHint == null) return;

		LockedHint.Visible = false;
		_hintTimer = new Timer { OneShot = true, WaitTime = 1.5 };
		AddChild(_hintTimer);
		_hintTimer.Timeout += () => LockedHint.Visible = false;
	}

	public override void Interact()
	{
		if (TargetRoom == null || TargetSpawn == null) return;
		if (GameState.Instance == null)
		{
			GD.PushError($"RoomDoor {Name}: GameState 未就绪");
			return;
		}

		if (!string.IsNullOrEmpty(RequiredClueId) && !GameState.Instance.HasClue(RequiredClueId))
		{
			ShowLockedHint();
			return;
		}
		if (!string.IsNullOrEmpty(RequiredKeyId) && !GameState.Instance.HasItem(RequiredKeyId))
		{
			ShowLockedHint();
			return;
		}

		base.Interact();
	}

	/// <summary>显示锁定提示并在 1.5 秒后隐藏。</summary>
	protected void ShowLockedHint()
	{
		if (LockedHint == null) return;
		LockedHint.Visible = true;
		_hintTimer?.Start();
	}

	private bool IsLocked()
	{
		return !string.IsNullOrEmpty(RequiredKeyId) || !string.IsNullOrEmpty(RequiredClueId);
	}
}
```

- [ ] **步骤 2：编译**

```powershell
& $godot --headless --path . --build-solutions --quit
```

预期：退出码 0，无 `error CS`。

---

## 任务 4：门脚本 `ClassroomDoor` / `LabDoor` / `OfficeDoor`

**文件：**
- 创建：`Scripts/Location/ClassroomDoor.cs`
- 创建：`Scripts/Location/LabDoor.cs`
- 创建：`Scripts/Location/OfficeDoor.cs`

- [ ] **步骤 1：写 `ClassroomDoor.cs`**

```csharp
using Godot;
using DialogueManagerRuntime;

/// <summary>教室→走廊的门：一周目选了看书后挡住，并播 BL 小说独白。</summary>
public partial class ClassroomDoor : RoomDoor
{
	/// <summary>被挡住时播放的剧本。</summary>
	[Export] public Resource BlockingDialogue { get; set; }

	public override void Interact()
	{
		if (GameState.Instance == null)
		{
			GD.PushError($"ClassroomDoor {Name}: GameState 未就绪");
			return;
		}
		if (GameState.Instance.IsFirstLoop() && GameState.Instance.HasLoopFlag("chose_reading"))
		{
			if (BlockingDialogue == null)
			{
				GD.PushError($"ClassroomDoor {Name}: BlockingDialogue 为空");
				return;
			}
			if (DialogueManager.ShowDialogueBalloon(BlockingDialogue) == null)
			{
				GD.PushError($"ClassroomDoor {Name}: ShowDialogueBalloon 返回空");
			}
			return;
		}
		base.Interact();
	}
}
```

- [ ] **步骤 2：写 `LabDoor.cs`**

```csharp
using Godot;

/// <summary>化学实验室门：本轮新井在分析且未到完成时间时，先把时间跳到完成时间。</summary>
public partial class LabDoor : RoomDoor
{
	public override void Interact()
	{
		if (GameState.Instance == null)
		{
			GD.PushError($"LabDoor {Name}: GameState 未就绪");
			return;
		}
		if (GameState.Instance.HasLoopFlag("shinji_analyzing"))
		{
			int ready = GameState.Instance.BombAnalysisReadyMinute;
			int now = GameState.Instance.ElapsedMinutes;
			if (ready >= 0 && now < ready)
			{
				if (GameState.Instance.AdvanceTime(ready - now)) return;
			}
		}
		base.Interact();
	}
}
```

- [ ] **步骤 3：写 `OfficeDoor.cs`**

```csharp
using Godot;

/// <summary>办公室门：只在 14:00-14:59（本轮 60-119 分钟）开放。</summary>
public partial class OfficeDoor : RoomDoor
{
	public override void Interact()
	{
		if (GameState.Instance == null)
		{
			GD.PushError($"OfficeDoor {Name}: GameState 未就绪");
			return;
		}
		int minute = GameState.Instance.ElapsedMinutes;
		bool open = minute >= 60 && minute < 120;
		if (!open)
		{
			ShowLockedHint();
			return;
		}
		base.Interact();
	}
}
```

- [ ] **步骤 4：编译**

```powershell
& $godot --headless --path . --build-solutions --quit
```

预期：退出码 0，无 `error CS`。

---

## 任务 5：交互物 `BookDesk` / `KeyedDrawer` / `BombInteractable` / `WaterPipe` / `WaitSpot`

**文件：**
- 创建：`Scripts/Location/BookDesk.cs`
- 创建：`Scripts/Location/KeyedDrawer.cs`
- 创建：`Scripts/Location/BombInteractable.cs`
- 创建：`Scripts/Location/WaterPipe.cs`
- 创建：`Scripts/Location/WaitSpot.cs`

- [ ] **步骤 1：写 `BookDesk.cs`**

```csharp
using Godot;
using DialogueManagerRuntime;

/// <summary>教室座位：一周目看书推进时间，第 5 次进入 last 分支。</summary>
public partial class BookDesk : Interactable
{
	/// <summary>看书剧本。</summary>
	[Export] public Resource Dialogue { get; set; }

	private int _readCount;

	public override void Interact()
	{
		if (GameState.Instance == null)
		{
			GD.PushError($"BookDesk {Name}: GameState 未就绪");
			return;
		}
		if (Dialogue == null)
		{
			GD.PushError($"BookDesk {Name}: Dialogue 为空");
			return;
		}
		if (!GameState.Instance.IsFirstLoop())
		{
			Show("later");
			return;
		}
		if (_readCount >= 4 || GameState.Instance.ElapsedMinutes >= 240)
		{
			Show("last");
			return;
		}
		if (GameState.Instance.AdvanceTime(60)) return;
		_readCount++;
		Show($"read_{_readCount}");
	}

	private void Show(string cue)
	{
		if (DialogueManager.ShowDialogueBalloon(Dialogue, cue) == null)
		{
			GD.PushError($"BookDesk {Name}: ShowDialogueBalloon({cue}) 返回空");
		}
	}
}
```

- [ ] **步骤 2：写 `KeyedDrawer.cs`**

```csharp
using Godot;

/// <summary>办公室抽屉：有钥匙才能打开，打开密码面板。</summary>
public partial class KeyedDrawer : Interactable
{
	/// <summary>开抽屉所需道具 id。</summary>
	[Export] public string RequiredKeyId { get; set; } = "office_key";

	/// <summary>没有钥匙时的提示。</summary>
	[Export] public Label LockedHint { get; set; }

	/// <summary>打开后显示的密码面板。</summary>
	[Export] public PasswordPanel PasswordPanel { get; set; }

	private Timer _hintTimer;

	public override void _Ready()
	{
		if (GameState.Instance == null)
		{
			GD.PushError($"KeyedDrawer {Name}: GameState 未就绪");
		}
		if (LockedHint == null)
		{
			GD.PushError($"KeyedDrawer {Name}: LockedHint 为空");
			return;
		}

		LockedHint.Visible = false;
		_hintTimer = new Timer { OneShot = true, WaitTime = 1.5 };
		AddChild(_hintTimer);
		_hintTimer.Timeout += () => LockedHint.Visible = false;
	}

	public override void Interact()
	{
		if (GameState.Instance == null)
		{
			GD.PushError($"KeyedDrawer {Name}: GameState 未就绪");
			return;
		}
		if (!string.IsNullOrEmpty(RequiredKeyId) && !GameState.Instance.HasItem(RequiredKeyId))
		{
			ShowLockedHint();
			return;
		}
		if (PasswordPanel == null)
		{
			GD.PushError($"KeyedDrawer {Name}: PasswordPanel 为空");
			return;
		}

		PasswordPanel.Open();
	}

	private void ShowLockedHint()
	{
		if (LockedHint == null) return;
		LockedHint.Visible = true;
		_hintTimer?.Start();
	}
}
```

- [ ] **步骤 3：写 `BombInteractable.cs`**

```csharp
using Godot;
using DialogueManagerRuntime;

/// <summary>地下炸药：取样 / 湿炸药 / 状态提示。</summary>
public partial class BombInteractable : Interactable
{
	/// <summary>炸药剧本。</summary>
	[Export] public Resource Dialogue { get; set; }

	public override void Interact()
	{
		if (GameState.Instance == null)
		{
			GD.PushError($"BombInteractable {Name}: GameState 未就绪");
			return;
		}
		if (Dialogue == null)
		{
			GD.PushError($"BombInteractable {Name}: Dialogue 为空");
			return;
		}

		if (!GameState.Instance.HasLoopFlag("bomb_sample_taken"))
		{
			GameState.Instance.AddItem("bomb_sample");
			GameState.Instance.SetLoopFlag("bomb_sample_taken");
			Show("sample");
			return;
		}
		if (!GameState.Instance.HasLoopFlag("bomb_wet"))
		{
			if (GameState.Instance.HasItem("water_bucket") && GameState.Instance.HasClue("bomb_weakness"))
			{
				GameState.Instance.SetLoopFlag("bomb_wet");
				Show("wet");
				return;
			}
			Show("nothing");
			return;
		}
		Show("already_wet");
	}

	private void Show(string cue)
	{
		if (DialogueManager.ShowDialogueBalloon(Dialogue, cue) == null)
		{
			GD.PushError($"BombInteractable {Name}: ShowDialogueBalloon({cue}) 返回空");
		}
	}
}
```

- [ ] **步骤 4：写 `WaterPipe.cs`**

```csharp
using Godot;
using DialogueManagerRuntime;

/// <summary>器材准备室水管：知道炸药解法后接一桶水。</summary>
public partial class WaterPipe : Interactable
{
	/// <summary>水管剧本。</summary>
	[Export] public Resource Dialogue { get; set; }

	public override void Interact()
	{
		if (GameState.Instance == null)
		{
			GD.PushError($"WaterPipe {Name}: GameState 未就绪");
			return;
		}
		if (Dialogue == null)
		{
			GD.PushError($"WaterPipe {Name}: Dialogue 为空");
			return;
		}
		if (GameState.Instance.HasLoopFlag("water_taken") || GameState.Instance.HasItem("water_bucket"))
		{
			Show("already");
			return;
		}
		if (!GameState.Instance.HasClue("bomb_weakness"))
		{
			Show("hint");
			return;
		}

		GameState.Instance.AddItem("water_bucket");
		GameState.Instance.SetLoopFlag("water_taken");
		Show("fill");
	}

	private void Show(string cue)
	{
		if (DialogueManager.ShowDialogueBalloon(Dialogue, cue) == null)
		{
			GD.PushError($"WaterPipe {Name}: ShowDialogueBalloon({cue}) 返回空");
		}
	}
}
```

- [ ] **步骤 5：写 `WaitSpot.cs`**

```csharp
using Godot;
using DialogueManagerRuntime;

/// <summary>地下服务器：湿炸药后等到 17:00。</summary>
public partial class WaitSpot : Interactable
{
	/// <summary>等待点剧本。</summary>
	[Export] public Resource Dialogue { get; set; }

	public override void Interact()
	{
		if (GameState.Instance == null)
		{
			GD.PushError($"WaitSpot {Name}: GameState 未就绪");
			return;
		}
		if (Dialogue == null)
		{
			GD.PushError($"WaitSpot {Name}: Dialogue 为空");
			return;
		}
		if (!GameState.Instance.HasLoopFlag("bomb_wet"))
		{
			Show("not_yet");
			return;
		}

		int now = GameState.Instance.ElapsedMinutes;
		if (now >= 240)
		{
			Show("already");
			return;
		}
		if (GameState.Instance.AdvanceTime(240 - now)) return;
		Show("waited");
	}

	private void Show(string cue)
	{
		if (DialogueManager.ShowDialogueBalloon(Dialogue, cue) == null)
		{
			GD.PushError($"WaitSpot {Name}: ShowDialogueBalloon({cue}) 返回空");
		}
	}
}
```

- [ ] **步骤 6：编译**

```powershell
& $godot --headless --path . --build-solutions --quit
```

预期：退出码 0，无 `error CS`。

---

## 任务 6：`DialogueBalloon` 真结局不解锁

**文件：**
- 修改：`Scripts/Dialogue/DialogueBalloon.cs`

- [ ] **步骤 1：修改 `_ExitTree` 的解锁条件**

把：

```csharp
		if (GodotObject.IsInstanceValid(player) && !KeepPlayerInputLockedOnExit)
		{
			player.InputLocked = false;
		}
```

替换为：

```csharp
		if (GodotObject.IsInstanceValid(player)
			&& !KeepPlayerInputLockedOnExit
			&& !(GodotObject.IsInstanceValid(gameState) && gameState.IsTrueEnding))
		{
			player.InputLocked = false;
		}
```

- [ ] **步骤 2：编译**

```powershell
& $godot --headless --path . --build-solutions --quit
```

预期：退出码 0，无 `error CS`。

---

## 任务 7：UI 脚本 `PasswordPanel` / `EndingOverlay` / `LoopEndNotice`

**文件：**
- 创建：`Scripts/UI/PasswordPanel.cs`
- 创建：`Scripts/UI/EndingOverlay.cs`
- 创建：`Scripts/UI/LoopEndNotice.cs`

- [ ] **步骤 1：写 `PasswordPanel.cs`**

```csharp
using Godot;

/// <summary>山田笔记本密码面板：4 位输入，正确后获取笔记本文档并解锁地下空间。</summary>
public partial class PasswordPanel : CanvasLayer
{
	/// <summary>正确密码；剧本设定 0721。</summary>
	[Export] public string Password { get; set; } = "0721";

	/// <summary>解锁后获取的文档 id。</summary>
	[Export] public string DocumentId { get; set; } = "yamada_notebook";

	/// <summary>解锁后写入的线索 id。</summary>
	[Export] public string UnlockClueId { get; set; } = "underground_access";

	private Control _overlay;
	private LineEdit _input;
	private Label _error;
	private Label _message;
	private Button _confirm;
	private Button _close;
	private Player _player;
	private bool _open;

	public override void _Ready()
	{
		_overlay = GetNode<Control>("%Overlay");
		_input = GetNode<LineEdit>("%Input");
		_error = GetNode<Label>("%Error");
		_message = GetNode<Label>("%Message");
		_confirm = GetNode<Button>("%Confirm");
		_close = GetNode<Button>("%Close");

		_confirm.Pressed += OnConfirm;
		_close.Pressed += Close;
		_input.TextSubmitted += _ => OnConfirm();

		_overlay.Hide();
		_error.Hide();
		_message.Hide();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!_open) return;
		if (!@event.IsActionPressed("ui_cancel")) return;
		GetViewport().SetInputAsHandled();
		Close();
	}

	/// <summary>打开面板：记录看过笔记本，锁住玩家输入。</summary>
	public void Open()
	{
		if (_open) return;
		if (GameState.Instance == null)
		{
			GD.PushError("PasswordPanel.Open: GameState 未就绪");
			return;
		}

		_player = GetTree().GetFirstNodeInGroup("player") as Player;
		if (_player == null)
		{
			GD.PushError("PasswordPanel.Open: 找不到 player");
			return;
		}

		_open = true;
		GameState.Instance.AddClue("saw_laptop");
		_player.InputLocked = true;
		_overlay.Show();
		_input.Text = "";
		_input.Editable = true;
		_error.Hide();
		_message.Hide();
		_input.GrabFocus();
		GD.Print("[Password] 打开");
	}

	/// <summary>关闭面板并解锁玩家。</summary>
	public void Close()
	{
		if (!_open) return;

		_open = false;
		_overlay.Hide();
		_input.Editable = true;
		if (GodotObject.IsInstanceValid(_player)) _player.InputLocked = false;
		GD.Print("[Password] 关闭");
	}

	private void OnConfirm()
	{
		if (!_open) return;
		if (GameState.Instance == null) return;

		if (_input.Text != Password)
		{
			_error.Text = "密码错误";
			_error.Show();
			_message.Hide();
			_input.Text = "";
			_input.GrabFocus();
			return;
		}

		bool added = GameState.Instance.AcquireDocument(DocumentId);
		GameState.Instance.AddClue(UnlockClueId);
		_error.Hide();
		_message.Text = added ? "解锁成功。笔记本的内容已记录到手机。" : "已经解锁过了。";
		_message.Show();
		_input.Editable = false;

		GetTree().CreateTimer(1.0).Timeout += Close;
	}
}
```

- [ ] **步骤 2：写 `EndingOverlay.cs`**

```csharp
using Godot;

/// <summary>结局黑屏：真结局尾声、The End、失败黑屏。</summary>
public partial class EndingOverlay : CanvasLayer
{
	private ColorRect _black;
	private Label _theEnd;
	private Label _thanks;
	private Tween _tween;

	public override void _Ready()
	{
		_black = GetNode<ColorRect>("%Black");
		_theEnd = GetNode<Label>("%TheEnd");
		_thanks = GetNode<Label>("%Thanks");
		HideAll();
	}

	/// <summary>真结局尾声开始：黑屏淡入，对话继续。</summary>
	public void ShowEpilogue()
	{
		_black.Show();
		_black.MouseFilter = Control.MouseFilterEnum.Ignore;
		FadeTo(1.0f, 1.0f);
	}

	/// <summary>真结局结束：保持黑屏，显示 The End。</summary>
	public void ShowTheEnd()
	{
		_black.Show();
		_black.MouseFilter = Control.MouseFilterEnum.Stop;
		_black.Modulate = new Color(1, 1, 1, 1);
		_theEnd.Show();
		_thanks.Show();
	}

	/// <summary>失败：黑屏淡入。</summary>
	public void ShowFail()
	{
		_black.Show();
		_black.MouseFilter = Control.MouseFilterEnum.Ignore;
		FadeTo(1.0f, 0.6f);
	}

	/// <summary>隐藏黑屏与文字；循环重置时调用。</summary>
	public void HideAll()
	{
		_tween?.Kill();
		_black.Hide();
		_black.Modulate = new Color(1, 1, 1, 0);
		_black.MouseFilter = Control.MouseFilterEnum.Ignore;
		_theEnd.Hide();
		_thanks.Hide();
	}

	private void FadeTo(float target, float duration)
	{
		_tween?.Kill();
		_tween = CreateTween();
		_tween.TweenProperty(_black, "modulate:a", target, duration);
	}
}
```

- [ ] **步骤 3：写 `LoopEndNotice.cs`**

```csharp
using Godot;

/// <summary>17:30 终末提示：闪白与「轰———」。</summary>
public partial class LoopEndNotice : CanvasLayer
{
	private ColorRect _flash;
	private Label _text;
	private Tween _tween;

	public override void _Ready()
	{
		_flash = GetNode<ColorRect>("%Flash");
		_text = GetNode<Label>("%Text");
		HideAll();
	}

	/// <summary>播放约 2 秒的爆炸提示。</summary>
	public void Show()
	{
		_tween?.Kill();
		_flash.Show();
		_text.Show();
		_flash.Modulate = new Color(1, 1, 1, 0);
		_text.Modulate = new Color(1, 1, 1, 0);

		_tween = CreateTween();
		_tween.TweenProperty(_flash, "modulate:a", 0.8f, 0.15f);
		_tween.TweenProperty(_flash, "modulate:a", 0.0f, 0.35f);
		_tween.Parallel().TweenProperty(_text, "modulate:a", 1.0f, 0.1f);
		_tween.TweenInterval(1.2f);
		_tween.TweenProperty(_text, "modulate:a", 0.0f, 0.3f);
		_tween.TweenCallback(Callable.From(() =>
		{
			_flash.Hide();
			_text.Hide();
		}));
	}

	/// <summary>立即隐藏。</summary>
	public void HideAll()
	{
		_tween?.Kill();
		_flash.Hide();
		_text.Hide();
	}
}
```

- [ ] **步骤 4：编译**

```powershell
& $godot --headless --path . --build-solutions --quit
```

预期：退出码 0，无 `error CS`。

## 任务 8：UI 场景 `PasswordPanel.tscn` / `EndingOverlay.tscn` / `LoopEndNotice.tscn`

**文件：**
- 创建：`Scenes/UI/PasswordPanel.tscn`
- 创建：`Scenes/UI/EndingOverlay.tscn`
- 创建：`Scenes/UI/LoopEndNotice.tscn`

- [ ] **步骤 1：写 `Scenes/UI/PasswordPanel.tscn`**

样式沿用 `Scenes/Phone/PhoneScreen.tscn` 的黑底白边风格。

```
[gd_scene format=3]

[ext_resource type="Script" path="res://Scripts/UI/PasswordPanel.cs" id="1_panel"]

[sub_resource type="StyleBoxFlat" id="StyleBoxFlat_panel"]
bg_color = Color(0, 0, 0, 1)
border_width_left = 3
border_width_top = 3
border_width_right = 3
border_width_bottom = 3
border_color = Color(1, 1, 1, 1)
corner_radius_top_left = 5
corner_radius_top_right = 5
corner_radius_bottom_right = 5
corner_radius_bottom_left = 5

[sub_resource type="StyleBoxFlat" id="StyleBoxFlat_btn_normal"]
content_margin_left = 20.0
content_margin_top = 10.0
content_margin_right = 20.0
content_margin_bottom = 10.0
bg_color = Color(0.121569, 0.121569, 0.121569, 1)
border_width_left = 3
border_width_top = 3
border_width_right = 3
border_width_bottom = 3
border_color = Color(0.6, 0.6, 0.6, 1)
corner_radius_top_left = 5
corner_radius_top_right = 5
corner_radius_bottom_right = 5
corner_radius_bottom_left = 5

[sub_resource type="StyleBoxFlat" id="StyleBoxFlat_btn_hover"]
content_margin_left = 20.0
content_margin_top = 10.0
content_margin_right = 20.0
content_margin_bottom = 10.0
bg_color = Color(0.121569, 0.121569, 0.121569, 1)
border_width_left = 3
border_width_top = 3
border_width_right = 3
border_width_bottom = 3
border_color = Color(1, 1, 1, 1)
corner_radius_top_left = 5
corner_radius_top_right = 5
corner_radius_bottom_right = 5
corner_radius_bottom_left = 5

[sub_resource type="StyleBoxFlat" id="StyleBoxFlat_btn_disabled"]
content_margin_left = 20.0
content_margin_top = 10.0
content_margin_right = 20.0
content_margin_bottom = 10.0
bg_color = Color(0, 0, 0, 1)
border_width_left = 3
border_width_top = 3
border_width_right = 3
border_width_bottom = 3
border_color = Color(0.329412, 0.329412, 0.329412, 1)
corner_radius_top_left = 5
corner_radius_top_right = 5
corner_radius_bottom_right = 5
corner_radius_bottom_left = 5

[sub_resource type="Theme" id="Theme_password"]
default_font_size = 20
Button/styles/disabled = SubResource("StyleBoxFlat_btn_disabled")
Button/styles/focus = SubResource("StyleBoxFlat_btn_hover")
Button/styles/hover = SubResource("StyleBoxFlat_btn_hover")
Button/styles/normal = SubResource("StyleBoxFlat_btn_normal")
PanelContainer/styles/panel = SubResource("StyleBoxFlat_panel")

[node name="PasswordPanel" type="CanvasLayer"]
layer = 60
script = ExtResource("1_panel")

[node name="Overlay" type="Control" parent="."]
unique_name_in_owner = true
visible = false
layout_mode = 3
anchors_preset = 15
anchor_right = 1.0
anchor_bottom = 1.0
grow_horizontal = 2
grow_vertical = 2
theme = SubResource("Theme_password")

[node name="Dim" type="ColorRect" parent="Overlay"]
layout_mode = 1
anchors_preset = 15
anchor_right = 1.0
anchor_bottom = 1.0
grow_horizontal = 2
grow_vertical = 2
mouse_filter = 2
color = Color(0, 0, 0, 0.55)

[node name="Center" type="CenterContainer" parent="Overlay"]
layout_mode = 1
anchors_preset = 15
anchor_right = 1.0
anchor_bottom = 1.0
grow_horizontal = 2
grow_vertical = 2
mouse_filter = 2

[node name="Panel" type="PanelContainer" parent="Overlay/Center"]
custom_minimum_size = Vector2(720, 420)
layout_mode = 2

[node name="Margin" type="MarginContainer" parent="Overlay/Center/Panel"]
layout_mode = 2
theme_override_constants/margin_left = 30
theme_override_constants/margin_top = 20
theme_override_constants/margin_right = 30
theme_override_constants/margin_bottom = 20

[node name="VBox" type="VBoxContainer" parent="Overlay/Center/Panel/Margin"]
layout_mode = 2
theme_override_constants/separation = 18

[node name="Title" type="Label" parent="Overlay/Center/Panel/Margin/VBox"]
layout_mode = 2
theme_override_font_sizes/font_size = 28
text = "山田老师的笔记本电脑"
horizontal_alignment = 1

[node name="Hint" type="Label" parent="Overlay/Center/Panel/Margin/VBox"]
layout_mode = 2
theme_override_colors/font_color = Color(0.8, 0.8, 0.8, 1)
theme_override_font_sizes/font_size = 18
text = "请输入 4 位密码（提示：小优香）"
horizontal_alignment = 1

[node name="Input" type="LineEdit" parent="Overlay/Center/Panel/Margin/VBox"]
unique_name_in_owner = true
custom_minimum_size = Vector2(0, 56)
layout_mode = 2
theme_override_font_sizes/font_size = 28
max_length = 4
alignment = 1
placeholder_text = "****"

[node name="Error" type="Label" parent="Overlay/Center/Panel/Margin/VBox"]
unique_name_in_owner = true
visible = false
layout_mode = 2
theme_override_colors/font_color = Color(1, 0.35, 0.35, 1)
theme_override_font_sizes/font_size = 18
horizontal_alignment = 1

[node name="Message" type="Label" parent="Overlay/Center/Panel/Margin/VBox"]
unique_name_in_owner = true
visible = false
layout_mode = 2
theme_override_font_sizes/font_size = 18
horizontal_alignment = 1

[node name="Buttons" type="HBoxContainer" parent="Overlay/Center/Panel/Margin/VBox"]
layout_mode = 2
theme_override_constants/separation = 16
alignment = 1

[node name="Confirm" type="Button" parent="Overlay/Center/Panel/Margin/VBox/Buttons"]
unique_name_in_owner = true
custom_minimum_size = Vector2(180, 52)
layout_mode = 2
text = "确定"

[node name="Close" type="Button" parent="Overlay/Center/Panel/Margin/VBox/Buttons"]
unique_name_in_owner = true
custom_minimum_size = Vector2(180, 52)
layout_mode = 2
text = "离开"
```

- [ ] **步骤 2：写 `Scenes/UI/EndingOverlay.tscn`**

```
[gd_scene format=3]

[ext_resource type="Script" path="res://Scripts/UI/EndingOverlay.cs" id="1_ending"]

[node name="EndingOverlay" type="CanvasLayer"]
layer = 46
script = ExtResource("1_ending")

[node name="Black" type="ColorRect" parent="."]
unique_name_in_owner = true
visible = false
modulate = Color(1, 1, 1, 0)
layout_mode = 3
anchors_preset = 15
anchor_right = 1.0
anchor_bottom = 1.0
grow_horizontal = 2
grow_vertical = 2
mouse_filter = 2
color = Color(0, 0, 0, 1)

[node name="Center" type="CenterContainer" parent="."]
layout_mode = 3
anchors_preset = 15
anchor_right = 1.0
anchor_bottom = 1.0
grow_horizontal = 2
grow_vertical = 2
mouse_filter = 2

[node name="VBox" type="VBoxContainer" parent="Center"]
layout_mode = 2
theme_override_constants/separation = 18
alignment = 1

[node name="TheEnd" type="Label" parent="Center/VBox"]
unique_name_in_owner = true
visible = false
layout_mode = 2
theme_override_font_sizes/font_size = 64
text = "The End"
horizontal_alignment = 1

[node name="Thanks" type="Label" parent="Center/VBox"]
unique_name_in_owner = true
visible = false
layout_mode = 2
theme_override_font_sizes/font_size = 24
text = "感谢游玩"
horizontal_alignment = 1
```

- [ ] **步骤 3：写 `Scenes/UI/LoopEndNotice.tscn`**

```
[gd_scene format=3]

[ext_resource type="Script" path="res://Scripts/UI/LoopEndNotice.cs" id="1_notice"]

[node name="LoopEndNotice" type="CanvasLayer"]
layer = 90
script = ExtResource("1_notice")

[node name="Flash" type="ColorRect" parent="."]
unique_name_in_owner = true
visible = false
modulate = Color(1, 1, 1, 0)
layout_mode = 3
anchors_preset = 15
anchor_right = 1.0
anchor_bottom = 1.0
grow_horizontal = 2
grow_vertical = 2
mouse_filter = 2
color = Color(1, 1, 1, 1)

[node name="Text" type="Label" parent="."]
unique_name_in_owner = true
visible = false
modulate = Color(1, 1, 1, 0)
layout_mode = 3
anchors_preset = 15
anchor_right = 1.0
anchor_bottom = 1.0
grow_horizontal = 2
grow_vertical = 2
mouse_filter = 2
theme_override_font_sizes/font_size = 72
text = "轰———"
horizontal_alignment = 1
vertical_alignment = 1
```

- [ ] **步骤 4：导入并编译**

```powershell
& $godot --headless --path . --import
& $godot --headless --path . --build-solutions --quit
```

预期：导入退出码 0；无 `SCRIPT ERROR`；编译 0 错误。若 `.tscn` 解析失败，检查 `layout_mode` / `anchors_preset` 拼写和节点父子路径。

---

## 任务 9：`GameScene` 结局信号、终末提示与主场景挂载

**文件：**
- 修改：`Scripts/Map/GameScene.cs`
- 修改：`Scenes/Root/GameScene.tscn`

- [ ] **步骤 1：在导出区加入两个 UI 引用**

在 `[Export] public DocumentLibrary DocumentLibrary { get; set; }` 之后插入：

```csharp
	/// <summary>结局黑屏。</summary>
	[Export] public EndingOverlay EndingOverlay { get; set; }

	/// <summary>17:30 终末提示。</summary>
	[Export] public LoopEndNotice LoopEndNotice { get; set; }
```

- [ ] **步骤 2：`_Ready` 增加空引用校验与信号订阅**

把 `_Ready` 的空引用条件改为：

```csharp
		if (MapConfig == null || StartLocation == null || MapScreen == null
			|| LocationHost == null || DialogueBalloonScene == null
			|| DocumentLibrary == null || EndingOverlay == null || LoopEndNotice == null)
		{
			GD.PushError("GameScene: 有导出引用为空，停止启动");
			return;
		}
```

在 `GameState.Instance.LoopEndReached += OnLoopEndReached;` 之后插入：

```csharp
		GameState.Instance.EpilogueStarted += OnEpilogueStarted;
		GameState.Instance.TrueEndingReached += OnTrueEndingReached;
		GameState.Instance.LoopFailed += OnLoopFailed;
```

- [ ] **步骤 3：修改 `OnLoopEndReached`**

把现有方法体替换为：

```csharp
	private async void OnLoopEndReached()
	{
		if (_loopEnding) return;
		_loopEnding = true;

		if (_currentLocation != null)
		{
			_currentLocation.ProcessMode = ProcessModeEnum.Disabled;
		}

		GD.Print("[Loop] 17:30 爆炸");
		LoopEndNotice.Show();
		await ToSignal(GetTree().CreateTimer(2.0), Timer.SignalName.Timeout);

		GameState.Instance.ResetLoop();
		_loopEnding = false;
	}
```

- [ ] **步骤 4：在 `OnLoopEndReached` 之后加入三个结局回调**

```csharp
	private void OnEpilogueStarted()
	{
		EndingOverlay.ShowEpilogue();
	}

	private void OnTrueEndingReached()
	{
		EndingOverlay.ShowTheEnd();

		Player player = GetTree().GetFirstNodeInGroup("player") as Player;
		if (player == null)
		{
			GD.PushError("GameScene.OnTrueEndingReached: 找不到 player");
			return;
		}
		player.InputLocked = true;
	}

	private async void OnLoopFailed()
	{
		if (_loopEnding) return;
		_loopEnding = true;

		if (_currentLocation != null)
		{
			_currentLocation.ProcessMode = ProcessModeEnum.Disabled;
		}

		GD.Print("[Loop] 失败");
		EndingOverlay.ShowFail();
		await ToSignal(GetTree().CreateTimer(1.0), Timer.SignalName.Timeout);

		GameState.Instance.ResetLoop();
		_loopEnding = false;
	}
```

- [ ] **步骤 5：循环重置时清掉结局 UI**

在 `OnLoopReset` 里，`MapScreen.HideMap();` 之后插入：

```csharp
		EndingOverlay.HideAll();
		LoopEndNotice.HideAll();
```

- [ ] **步骤 6：在 `GameScene.tscn` 挂两个 UI 场景**

在文件顶部的 `[ext_resource]` 区加入：

```
[ext_resource type="PackedScene" path="res://Scenes/UI/EndingOverlay.tscn" id="9_ending"]
[ext_resource type="PackedScene" path="res://Scenes/UI/LoopEndNotice.tscn" id="10_notice"]
```

把根节点改成：

```
[node name="GameScene" type="Node2D" unique_id=1828415781 node_paths=PackedStringArray("MapScreen", "LocationHost", "EndingOverlay", "LoopEndNotice")]
script = ExtResource("1_game")
MapConfig = ExtResource("3_config")
StartLocation = ExtResource("4_start")
MapScreen = NodePath("UILayer/MapScreen")
LocationHost = NodePath("LocationHost")
DialogueBalloonScene = ExtResource("5_balloon")
DocumentLibrary = ExtResource("7_docs")
EndingOverlay = NodePath("EndingOverlay")
LoopEndNotice = NodePath("LoopEndNotice")
```

在 `[node name="PhoneScreen" parent="." instance=ExtResource("8_phone")]` 之后插入：

```
[node name="EndingOverlay" parent="." instance=ExtResource("9_ending")]

[node name="LoopEndNotice" parent="." instance=ExtResource("10_notice")]
```

- [ ] **步骤 7：编译并启动**

```powershell
& $godot --headless --path . --build-solutions --quit
& $godot --headless --path . --quit-after 120
```

预期：编译 0 错误；启动日志没有 `GameScene: 有导出引用为空`，没有 `Node not found`。

---

## 任务 10：`Classroom.tscn` 化学实验室 / 座位 / 教室门 / 新井显隐

**文件：**
- 修改：`Scenes/Locations/Classroom.tscn`

- [ ] **步骤 1：加入新的外部资源**

在现有 `[ext_resource]` 区末尾（`char_shinji` 之后）加入：

```
[ext_resource type="Script" path="res://Scripts/Location/ClassroomDoor.cs" id="13_classroom_door"]
[ext_resource type="Script" path="res://Scripts/Location/LabDoor.cs" id="14_lab_door"]
[ext_resource type="Script" path="res://Scripts/Location/BookDesk.cs" id="15_desk"]
[ext_resource type="Resource" path="res://Dialogue/Objects/classroom_desk.dialogue" id="16_desk_dialogue"]
[ext_resource type="Resource" path="res://Dialogue/Objects/classroom_block.dialogue" id="17_block_dialogue"]
[ext_resource type="Resource" path="res://Dialogue/Npc/shinji_lab.dialogue" id="18_shinji_lab"]
```

- [ ] **步骤 2：加入实验室新井的时间帧子资源**

在现有 `NpcStateFrame_shinji_0` 之后加入：

```
[sub_resource type="Resource" id="NpcStateFrame_shinji_lab_0"]
script = ExtResource("9_frame")
StateId = "working"
Texture = ExtResource("char_shinji")
```

- [ ] **步骤 3：把教室→走廊的门换成 `ClassroomDoor`**

把 `Room_Classroom/DoorToHallway` 节点改为：

```
[node name="DoorToHallway" type="Area2D" parent="Room_Classroom" node_paths=PackedStringArray("TargetRoom", "TargetSpawn")]
position = Vector2(1800, 500)
script = ExtResource("13_classroom_door")
TargetRoom = NodePath("../../Room_Hallway")
TargetSpawn = NodePath("../../Room_Hallway/DefaultSpawn")
TravelMinutes = 15
BlockingDialogue = ExtResource("17_block_dialogue")
```

- [ ] **步骤 4：在教室里加座位交互**

在 `Room_Classroom/Npc_Yamada/DebugLabel` 之后、`Room_Hallway` 之前插入：

```
[node name="BookDesk" type="Area2D" parent="Room_Classroom"]
position = Vector2(760, 520)
script = ExtResource("15_desk")
Dialogue = ExtResource("16_desk_dialogue")

[node name="Shape" type="CollisionShape2D" parent="Room_Classroom/BookDesk"]
shape = SubResource("RectangleShape2D_zone")

[node name="Placeholder" type="ColorRect" parent="Room_Classroom/BookDesk"]
offset_left = -40.0
offset_top = -100.0
offset_right = 40.0
offset_bottom = 100.0
color = Color(0.55, 0.45, 0.62, 1)

[node name="Caption" type="Label" parent="Room_Classroom/BookDesk"]
offset_left = -40.0
offset_top = 105.0
offset_right = 60.0
offset_bottom = 145.0
text = "座位"
```

- [ ] **步骤 5：给隔壁班新井加标记显隐**

把 `Room_NextClassroom/Npc_Shinji` 节点改为：

```
[node name="Npc_Shinji" type="Area2D" parent="Room_NextClassroom" node_paths=PackedStringArray("Sprite")]
position = Vector2(1200, 520)
script = ExtResource("8_npc")
NpcId = "classroom.shinji"
Dialogue = ExtResource("12_shinji")
PresentLoopFlag = "shinji_analyzing"
VisibleWhenFlagSet = false
Frames = [SubResource("NpcStateFrame_shinji_0")]
Sprite = NodePath("Sprite")
```

- [ ] **步骤 6：在走廊加化学实验室门**

在 `Room_Hallway/DoorToNextClassroom/Caption` 之后插入：

```
[node name="DoorToLab" type="Area2D" parent="Room_Hallway" node_paths=PackedStringArray("TargetRoom", "TargetSpawn", "LockedHint")]
position = Vector2(1200, 500)
script = ExtResource("14_lab_door")
TargetRoom = NodePath("../../Room_ChemLab")
TargetSpawn = NodePath("../../Room_ChemLab/DefaultSpawn")
RequiredClueId = "chem_lab_unlocked"
TravelMinutes = 0
LockedHint = NodePath("LockedHint")

[node name="Shape" type="CollisionShape2D" parent="Room_Hallway/DoorToLab"]
shape = SubResource("RectangleShape2D_zone")

[node name="Placeholder" type="ColorRect" parent="Room_Hallway/DoorToLab"]
offset_left = -40.0
offset_top = -100.0
offset_right = 40.0
offset_bottom = 100.0
color = Color(0.35, 0.5, 0.45, 1)

[node name="Caption" type="Label" parent="Room_Hallway/DoorToLab"]
offset_left = -40.0
offset_top = 105.0
offset_right = 60.0
offset_bottom = 145.0
text = "门（化学实验室）"

[node name="LockedHint" type="Label" parent="Room_Hallway/DoorToLab"]
offset_left = -90.0
offset_top = -150.0
offset_right = 90.0
offset_bottom = -110.0
text = "（化学实验室还进不去）"
```

- [ ] **步骤 7：在文件末尾加入化学实验室房间**

在 `[node name="Player" parent="." instance=ExtResource("7_player")]` 之前插入：

```
[node name="Room_ChemLab" type="Node2D" parent="." node_paths=PackedStringArray("DefaultSpawn")]
script = ExtResource("2_room")
DefaultSpawn = NodePath("DefaultSpawn")
CameraBounds = Rect2i(0, 0, 1920, 600)

[node name="Bg" type="TextureRect" parent="Room_ChemLab"]
offset_right = 1920.0
offset_bottom = 600.0
mouse_filter = 2
texture = ExtResource("bg_next_classroom")
expand_mode = 1
stretch_mode = 6

[node name="DefaultSpawn" type="Marker2D" parent="Room_ChemLab"]
position = Vector2(350, 520)

[node name="DoorToHallway" type="Area2D" parent="Room_ChemLab" node_paths=PackedStringArray("TargetRoom", "TargetSpawn")]
position = Vector2(80, 500)
script = ExtResource("3_door")
TargetRoom = NodePath("../../Room_Hallway")
TargetSpawn = NodePath("../../Room_Hallway/DefaultSpawn")
TravelMinutes = 0

[node name="Shape" type="CollisionShape2D" parent="Room_ChemLab/DoorToHallway"]
shape = SubResource("RectangleShape2D_zone")

[node name="Placeholder" type="ColorRect" parent="Room_ChemLab/DoorToHallway"]
offset_left = -40.0
offset_top = -100.0
offset_right = 40.0
offset_bottom = 100.0
color = Color(0.45, 0.3, 0.2, 1)

[node name="Caption" type="Label" parent="Room_ChemLab/DoorToHallway"]
offset_left = -40.0
offset_top = 105.0
offset_right = 60.0
offset_bottom = 145.0
text = "门（走廊）"

[node name="Npc_ShinjiLab" type="Area2D" parent="Room_ChemLab" node_paths=PackedStringArray("Sprite")]
position = Vector2(1200, 520)
script = ExtResource("8_npc")
NpcId = "classroom.shinji_lab"
Dialogue = ExtResource("18_shinji_lab")
PresentLoopFlag = "shinji_analyzing"
VisibleWhenFlagSet = true
Frames = [SubResource("NpcStateFrame_shinji_lab_0")]
Sprite = NodePath("Sprite")

[node name="Sprite" type="Sprite2D" parent="Room_ChemLab/Npc_ShinjiLab"]
position = Vector2(0, -100)
scale = Vector2(0.65, 0.65)
texture = ExtResource("char_shinji")

[node name="Shape" type="CollisionShape2D" parent="Room_ChemLab/Npc_ShinjiLab"]
position = Vector2(0, -100)
shape = SubResource("RectangleShape2D_zone")

[node name="DebugLabel" type="Label" parent="Room_ChemLab/Npc_ShinjiLab"]
offset_left = -80.0
offset_top = 10.0
offset_right = 80.0
offset_bottom = 50.0
text = "classroom.shinji_lab:working"
```

- [ ] **步骤 8：导入并编译**

```powershell
& $godot --headless --path . --import
& $godot --headless --path . --build-solutions --quit
```

预期：导入退出码 0；无 `SCRIPT ERROR`；编译 0 错误。

---

## 任务 11：`Administration.tscn` 办公室门 / 抽屉 / 密码面板

**文件：**
- 修改：`Scenes/Locations/Administration.tscn`

- [ ] **步骤 1：加入新的外部资源**

在 `[ext_resource]` 区末尾加入：

```
[ext_resource type="Script" path="res://Scripts/Location/OfficeDoor.cs" id="11_office_door"]
[ext_resource type="Script" path="res://Scripts/Location/KeyedDrawer.cs" id="12_drawer"]
[ext_resource type="PackedScene" path="res://Scenes/UI/PasswordPanel.tscn" id="13_password"]
```

- [ ] **步骤 2：把入口→办公室的门换成 `OfficeDoor`**

把 `Room_Lobby/DoorToOffice` 节点改为：

```
[node name="DoorToOffice" type="Area2D" parent="Room_Lobby" node_paths=PackedStringArray("TargetRoom", "TargetSpawn", "LockedHint")]
position = Vector2(1600, 500)
script = ExtResource("11_office_door")
TargetRoom = NodePath("../../Room_Office")
TargetSpawn = NodePath("../../Room_Office/DefaultSpawn")
TravelMinutes = 5
LockedHint = NodePath("LockedHint")
```

并在 `Room_Lobby/DoorToOffice/Caption` 之后插入：

```
[node name="LockedHint" type="Label" parent="Room_Lobby/DoorToOffice"]
offset_left = -120.0
offset_top = -150.0
offset_right = 120.0
offset_bottom = -110.0
text = "（办公室的门关着。晚点再来吧。）"
```

- [ ] **步骤 3：在办公室里加抽屉**

在 `Room_Office/Npc_Yamada/DebugLabel` 之后插入：

```
[node name="KeyedDrawer" type="Area2D" parent="Room_Office" node_paths=PackedStringArray("LockedHint", "PasswordPanel")]
position = Vector2(900, 500)
script = ExtResource("12_drawer")
RequiredKeyId = "office_key"
LockedHint = NodePath("LockedHint")
PasswordPanel = NodePath("../../PasswordPanel")

[node name="Shape" type="CollisionShape2D" parent="Room_Office/KeyedDrawer"]
shape = SubResource("RectangleShape2D_zone")

[node name="Placeholder" type="ColorRect" parent="Room_Office/KeyedDrawer"]
offset_left = -40.0
offset_top = -100.0
offset_right = 40.0
offset_bottom = 100.0
color = Color(0.55, 0.4, 0.25, 1)

[node name="Caption" type="Label" parent="Room_Office/KeyedDrawer"]
offset_left = -40.0
offset_top = 105.0
offset_right = 60.0
offset_bottom = 145.0
text = "办公桌"

[node name="LockedHint" type="Label" parent="Room_Office/KeyedDrawer"]
offset_left = -60.0
offset_top = -150.0
offset_right = 60.0
offset_bottom = -110.0
text = "（抽屉锁着）"
```

- [ ] **步骤 4：挂密码面板**

在 `[node name="Player" parent="." instance=ExtResource("7_player")]` 之前插入：

```
[node name="PasswordPanel" parent="." instance=ExtResource("13_password")]
```

- [ ] **步骤 5：导入并编译**

```powershell
& $godot --headless --path . --import
& $godot --headless --path . --build-solutions --quit
```

预期：导入退出码 0；无 `SCRIPT ERROR`；编译 0 错误。若 `PasswordPanel` 在 `KeyedDrawer` 里为 null，检查 `node_paths` 是否包含 `"PasswordPanel"`、相对路径是否为 `../../PasswordPanel`。

---

## 任务 12：`Playground.tscn` 炸药 / 水管 / 等待点 / 地下门 / 山田剧本

**文件：**
- 修改：`Scenes/Locations/Playground.tscn`

- [ ] **步骤 1：加入新的外部资源**

在 `[ext_resource]` 区末尾加入：

```
[ext_resource type="Script" path="res://Scripts/Location/BombInteractable.cs" id="12_bomb"]
[ext_resource type="Script" path="res://Scripts/Location/WaterPipe.cs" id="13_water"]
[ext_resource type="Script" path="res://Scripts/Location/WaitSpot.cs" id="14_wait"]
[ext_resource type="Resource" path="res://Dialogue/Objects/underground_bomb.dialogue" id="15_bomb_dialogue"]
[ext_resource type="Resource" path="res://Dialogue/Objects/equipment_water.dialogue" id="16_water_dialogue"]
[ext_resource type="Resource" path="res://Dialogue/Objects/underground_wait.dialogue" id="17_wait_dialogue"]
[ext_resource type="Resource" path="res://Dialogue/Npc/yamada_underground.dialogue" id="18_yamada_underground"]
```

- [ ] **步骤 2：给地下门加线索锁**

把 `Room_Behind/DoorToUnderground` 节点改为：

```
[node name="DoorToUnderground" type="Area2D" parent="Room_Behind" node_paths=PackedStringArray("TargetRoom", "TargetSpawn", "LockedHint")]
position = Vector2(1600, 500)
script = ExtResource("3_door")
TargetRoom = NodePath("../../Room_Underground")
TargetSpawn = NodePath("../../Room_Underground/DefaultSpawn")
RequiredClueId = "underground_access"
TravelMinutes = 5
LockedHint = NodePath("LockedHint")
```

并在 `Room_Behind/DoorToUnderground/Caption` 之后插入：

```
[node name="LockedHint" type="Label" parent="Room_Behind/DoorToUnderground"]
offset_left = -90.0
offset_top = -150.0
offset_right = 90.0
offset_bottom = -110.0
text = "（还不知道怎么下去）"
```

- [ ] **步骤 3：在器材准备室加水管**

在 `Room_Equipment/Drink/Caption` 之后插入：

```
[node name="WaterPipe" type="Area2D" parent="Room_Equipment"]
position = Vector2(1200, 500)
script = ExtResource("13_water")
Dialogue = ExtResource("16_water_dialogue")

[node name="Shape" type="CollisionShape2D" parent="Room_Equipment/WaterPipe"]
shape = SubResource("RectangleShape2D_zone")

[node name="Placeholder" type="ColorRect" parent="Room_Equipment/WaterPipe"]
offset_left = -40.0
offset_top = -100.0
offset_right = 40.0
offset_bottom = 100.0
color = Color(0.3, 0.55, 0.8, 1)

[node name="Caption" type="Label" parent="Room_Equipment/WaterPipe"]
offset_left = -40.0
offset_top = 105.0
offset_right = 60.0
offset_bottom = 145.0
text = "水管"
```

- [ ] **步骤 4：在地下空间加炸药与等待点**

在 `Room_Underground/DoorToBehind/Caption` 之后、`Npc_Yamada` 之前插入：

```
[node name="Bomb" type="Area2D" parent="Room_Underground"]
position = Vector2(900, 500)
script = ExtResource("12_bomb")
Dialogue = ExtResource("15_bomb_dialogue")

[node name="Shape" type="CollisionShape2D" parent="Room_Underground/Bomb"]
shape = SubResource("RectangleShape2D_zone")

[node name="Placeholder" type="ColorRect" parent="Room_Underground/Bomb"]
offset_left = -40.0
offset_top = -100.0
offset_right = 40.0
offset_bottom = 100.0
color = Color(0.35, 0.3, 0.25, 1)

[node name="Caption" type="Label" parent="Room_Underground/Bomb"]
offset_left = -40.0
offset_top = 105.0
offset_right = 60.0
offset_bottom = 145.0
text = "炸药"

[node name="WaitSpot" type="Area2D" parent="Room_Underground"]
position = Vector2(600, 500)
script = ExtResource("14_wait")
Dialogue = ExtResource("17_wait_dialogue")

[node name="Shape" type="CollisionShape2D" parent="Room_Underground/WaitSpot"]
shape = SubResource("RectangleShape2D_zone")

[node name="Placeholder" type="ColorRect" parent="Room_Underground/WaitSpot"]
offset_left = -40.0
offset_top = -100.0
offset_right = 40.0
offset_bottom = 100.0
color = Color(0.25, 0.25, 0.3, 1)

[node name="Caption" type="Label" parent="Room_Underground/WaitSpot"]
offset_left = -40.0
offset_top = 105.0
offset_right = 60.0
offset_bottom = 145.0
text = "服务器"
```

- [ ] **步骤 5：把地下山田换成 `yamada_underground.dialogue`**

把 `Room_Underground/Npc_Yamada` 的 `Dialogue` 改为：

```
Dialogue = ExtResource("18_yamada_underground")
```

- [ ] **步骤 6：导入并编译**

```powershell
& $godot --headless --path . --import
& $godot --headless --path . --build-solutions --quit
```

预期：导入退出码 0；无 `SCRIPT ERROR`；编译 0 错误。

## 任务 13：8 篇文档资源与注册表

**文件：**
- 创建：`Resources/Documents/shinji_schedule.tres`
- 创建：`Resources/Documents/yuka_secret.tres`
- 创建：`Resources/Documents/equipment_room.tres`
- 创建：`Resources/Documents/yamada_tracks.tres`
- 创建：`Resources/Documents/laptop_password.tres`
- 创建：`Resources/Documents/chem_lab.tres`
- 创建：`Resources/Documents/bomb_weakness.tres`
- 创建：`Resources/Documents/yamada_notebook.tres`
- 修改：`Resources/Documents/DocumentLibrary.tres`

文档脚本 UID 以 `Scripts/Documents/DocumentData.cs.uid` 为准（当前 `uid://ciqtvja815o4p`）；注册表脚本 UID 以 `Scripts/Documents/DocumentLibrary.cs.uid` 为准（当前 `uid://c5d41r5hcdx4`）。若 `.uid` 已变，替换下面两个 UID。

- [ ] **步骤 1：写 `shinji_schedule.tres`**

```
[gd_resource type="Resource" script_class="DocumentData" format=3]

[ext_resource type="Script" uid="uid://ciqtvja815o4p" path="res://Scripts/Documents/DocumentData.cs" id="1_doc"]

[resource]
script = ExtResource("1_doc")
Id = "shinji_schedule"
Title = "新井次郎的日程"
Body = "新井次郎整个下午都会在隔壁班教室自习。\n（山田老师说的。）"
```

- [ ] **步骤 2：写 `yuka_secret.tres`**

```
[gd_resource type="Resource" script_class="DocumentData" format=3]

[ext_resource type="Script" uid="uid://ciqtvja815o4p" path="res://Scripts/Documents/DocumentData.cs" id="1_doc"]

[resource]
script = ExtResource("1_doc")
Id = "yuka_secret"
Title = "小优香的秘密"
Body = "小优香从前两个月起就和山田老师关系很好。\n她还亲口说过「喜欢山田老师」。\n\n——新井次郎"
```

- [ ] **步骤 3：写 `equipment_room.tres`**

```
[gd_resource type="Resource" script_class="DocumentData" format=3]

[ext_resource type="Script" uid="uid://ciqtvja815o4p" path="res://Scripts/Documents/DocumentData.cs" id="1_doc"]

[resource]
script = ExtResource("1_doc")
Id = "equipment_room"
Title = "器材准备室"
Body = "位置：操场后面。\n小优香说那里有中午买来的冰饮，并把钥匙给了我。"
```

- [ ] **步骤 4：写 `yamada_tracks.tres`**

```
[gd_resource type="Resource" script_class="DocumentData" format=3]

[ext_resource type="Script" uid="uid://ciqtvja815o4p" path="res://Scripts/Documents/DocumentData.cs" id="1_doc"]

[resource]
script = ExtResource("1_doc")
Id = "yamada_tracks"
Title = "山田老师的行踪"
Body = "小优香说，她最近总能看到山田老师放学后一个人去操场后面。"
```

- [ ] **步骤 5：写 `laptop_password.tres`**

```
[gd_resource type="Resource" script_class="DocumentData" format=3]

[ext_resource type="Script" uid="uid://ciqtvja815o4p" path="res://Scripts/Documents/DocumentData.cs" id="1_doc"]

[resource]
script = ExtResource("1_doc")
Id = "laptop_password"
Title = "笔记本密码"
Body = "山田老师笔记本电脑的密码：0721。\n那是小优香和老师第一次出去玩的日子。\n（要替小优香保密。）"
```

- [ ] **步骤 6：写 `chem_lab.tres`**

```
[gd_resource type="Resource" script_class="DocumentData" format=3]

[ext_resource type="Script" uid="uid://ciqtvja815o4p" path="res://Scripts/Documents/DocumentData.cs" id="1_doc"]

[resource]
script = ExtResource("1_doc")
Id = "chem_lab"
Title = "化学实验室"
Body = "新井次郎会去化学实验室解析炸药。\n他说大概需要两个小时。"
```

- [ ] **步骤 7：写 `bomb_weakness.tres`**

```
[gd_resource type="Resource" script_class="DocumentData" format=3]

[ext_resource type="Script" uid="uid://ciqtvja815o4p" path="res://Scripts/Documents/DocumentData.cs" id="1_doc"]

[resource]
script = ExtResource("1_doc")
Id = "bomb_weakness"
Title = "炸药的弱点"
Body = "这是一种硝铵类炸药。\n只需要用大量的水，就可以让它失效。\n\n——新井次郎的分析结果"
```

- [ ] **步骤 8：写 `yamada_notebook.tres`**

```
[gd_resource type="Resource" script_class="DocumentData" format=3]

[ext_resource type="Script" uid="uid://ciqtvja815o4p" path="res://Scripts/Documents/DocumentData.cs" id="1_doc"]

[resource]
script = ExtResource("1_doc")
Id = "yamada_notebook"
Title = "山田老师的笔记本"
Body = "9月23日。\n\n我终于确认了。\n仙台市不是真实的。学校地下排列着服务器，整座城市、所有人，都是被模拟出来的数字生命。\n我们的一生，只是别人观察的样本。\n\n我决定在 17:30 引爆地下的服务器。\n让这场梦结束。\n\n通往地下空间的入口在操场后面。那里有一个井盖。\n服务器就在下面。"
```

- [ ] **步骤 9：重写 `DocumentLibrary.tres`**

```
[gd_resource type="Resource" script_class="DocumentLibrary" format=3]

[ext_resource type="Script" uid="uid://c5d41r5hcdx4" path="res://Scripts/Documents/DocumentLibrary.cs" id="1_lib"]
[ext_resource type="Resource" path="res://Resources/Documents/shinji_schedule.tres" id="2_doc"]
[ext_resource type="Resource" path="res://Resources/Documents/yuka_secret.tres" id="3_doc"]
[ext_resource type="Resource" path="res://Resources/Documents/equipment_room.tres" id="4_doc"]
[ext_resource type="Resource" path="res://Resources/Documents/yamada_tracks.tres" id="5_doc"]
[ext_resource type="Resource" path="res://Resources/Documents/laptop_password.tres" id="6_doc"]
[ext_resource type="Resource" path="res://Resources/Documents/chem_lab.tres" id="7_doc"]
[ext_resource type="Resource" path="res://Resources/Documents/bomb_weakness.tres" id="8_doc"]
[ext_resource type="Resource" path="res://Resources/Documents/yamada_notebook.tres" id="9_doc"]

[resource]
script = ExtResource("1_lib")
Documents = [ExtResource("2_doc"), ExtResource("3_doc"), ExtResource("4_doc"), ExtResource("5_doc"), ExtResource("6_doc"), ExtResource("7_doc"), ExtResource("8_doc"), ExtResource("9_doc")]
```

- [ ] **步骤 10：导入并启动**

```powershell
& $godot --headless --path . --import
& $godot --headless --path . --quit-after 120
```

预期：导入退出码 0；启动日志没有 `GameState.SeedDocuments` 的 `PushError`，没有 `文档 ... 没有在 DocumentLibrary 里注册`。如果报脚本类型找不到，先跑 `--import` 再启动。

---

## 任务 14：剧本 `yuka` / `shinji` / `yamada` / `shinji_lab`

**文件：**
- 修改：`Dialogue/Npc/yuka.dialogue`
- 修改：`Dialogue/Npc/shinji.dialogue`
- 修改：`Dialogue/Npc/yamada.dialogue`
- 创建：`Dialogue/Npc/shinji_lab.dialogue`

- [ ] **步骤 1：用下面的内容替换 `Dialogue/Npc/yuka.dialogue`**

```
~ start
if npc.IsInState("running")
	小优香: 今天的天气真适合跑步呢！鸣子找我有什么事吗？
	- 关于田径部 [ID:ask_track] => ask_track
	- 关于山田老师 [ID:ask_yamada] [if GameState.HasClue("yuka_likes_yamada") /] => ask_yamada
	- 关于山田老师的笔记本电脑 [ID:ask_laptop] [if GameState.HasClue("saw_laptop") /] => ask_laptop
	- 算了 [#repeatable] => END
else
	=> classroom_start

~ classroom_start
if GameState.HasLoopFlag("yuka_briefed")
	小优香: 那我一会去田径部啦，17:30 一起回家哦
	=> END
else
	小优香: 啊，鸣子你醒啦
	- 询问现在的情况 [ID:ask_now] => ask_now

~ ask_now
鸣子: 诶，现在几点了，好像睡过头了……
小优香: 现在已经放学了哦，你还要和往常一样看一下午书吗？
- 决定在教室看书 [ID:choose_read] => choose_read
- 决定出去逛逛 [ID:choose_out] [if GameState.GetLoopCount() > 1 /] [#show_if=true] => choose_out

~ choose_read
鸣子: 是的，上次的小说还没看完……
鸣子: （这个时间段通常是大家社团活动的时间，但是我一般会在这个时间在教室里细细品味BL小说）
小优香: 那我一会去田径部啦，17:30 一起回家哦
do GameState.SetLoopFlag("chose_reading")
do GameState.SetLoopFlag("yuka_briefed")
do GameState.UnlockLocation("playground")
=> END

~ choose_out
鸣子: 今天久违的想在学校里逛逛呢
鸣子: （不知为何，总感觉今天有些焦躁，看不下去书）
小优香: 嘿嘿，真少见呢，那我一会去田径部啦，17:30 一起回家哦
do GameState.SetLoopFlag("chose_going_out")
do GameState.SetLoopFlag("yuka_briefed")
do GameState.UnlockLocation("playground")
=> END

~ ask_track
鸣子: 田径部的大家都很努力呢
鸣子: （小优香从去年刚从国中升上来的时候就已经是田径部的主将了，简直是怪物）
小优香: 毕竟全国大赛就要开始了嘛，对了，小优香不介意的话去[url=doc:equipment_room][color=#ffd866]操场后的准备室[/color][/url]坐一会吧，嘿嘿，那里有我们中午买来的冰饮哦，这是钥匙
do GameState.AddItem("equipment_key")
鸣子: （小优香真体贴呢，不过话说回来，运动后喝冰饮真的正常吗？嘛——毕竟她是怪物啊）
=> END

~ ask_yamada
鸣子: 听说你最近和山田老师关系很好嘛
小优香: 诶诶——你听谁说的这些，我知道了，肯定又是新井那家伙在乱说吧，我回去以后一定要给他点颜色看看
鸣子: 好啦好啦，说到底，山田到底是怎么一回事
小优香: 山田老师啊，不过是我最近物理总是不及格，于是他每天午休的时候叫我去他办公室补课了啦，不过说起来，我最近还总是能看到他[url=doc:yamada_tracks][color=#ffd866]放学之后自己一个人来操场后面[/color][/url]呢，总感觉有点可疑……
do GameState.UnlockLocation("administration")
=> END

~ ask_laptop
鸣子: 重大发现！山田的笔记本密码提示是小优香哦，小优香真的没有什么事瞒着我吗？
小优香: 诶，居然有这种事，小优香不知道哦……
鸣子: 盯——————
小优香: 我知道了啦，老师的密码的确和我有关了啦，是……
鸣子: 盯————
小优香: 是[url=doc:laptop_password][color=#ffd866]0721[/color][/url]了啦，那是……我闹着要让老师把密码改成两个人第一次出去玩的日子，不过鸣子你一定要替我保密哦，这件事谁也不要告诉，哪怕是新井也不能说哦
do GameState.AddClue("laptop_password")
鸣子: （看来要好好守护友人的少女心呢）
=> END
```

- [ ] **步骤 2：用下面的内容替换 `Dialogue/Npc/shinji.dialogue`**

```
~ start
鸣子: 嗨，新井你还是老样子一天到晚都在看书呢
新井: 诶……诶———千……千原同学你怎么来了
鸣子: （新井次郎，姑且算是小优香的幼驯染，是个货真价实的处男，每次跟我说话都会脸红，是个很好捉弄的家伙）
- 关于小优香 [ID:ask_yuka] => ask_yuka
- 关于炸药 [ID:ask_bomb] [if GameState.HasItem("bomb_sample") /] => ask_bomb
- 算了 [#repeatable] => END

~ ask_yuka
鸣子: 总感觉小优香自从很忙呢，周末也不出来……全国大赛真的这么忙吗
新井: 小……小优香啊，她从前两个月起好像就跟山田老师关系非常好呢，还跟我说她[url=doc:yuka_secret][color=#ffd866]喜欢山田老师[/color][/url]什么的
do GameState.AddClue("yuka_likes_yamada")
鸣子: ieeeeeeee？！居然是这样吗！
鸣子: （感觉事情朝着不妙的方向发展了）
=> END

~ ask_bomb
鸣子: 新井同学，你懂炸药吗？
鸣子: （我把手上的炸药样本递给新井同学）
新井: 炸……炸药？！千原同学是从哪里搞到这个的？
鸣子: 说起来很复杂……总之，新井同学可以帮忙解析这种炸药找到让它失效的方法吗？拜托了，这真的很重要！
鸣子: （我把脸贴近新井，他的脸不知为何霎时红透了）
新井: 呜哇——千原同学不要靠这么近啦，我知道了，我会去[url=doc:chem_lab][color=#ffd866]化学实验室[/color][/url]尽可能解析这个炸药，不过可能需要两个小时哦
do GameState.RemoveItem("bomb_sample")
do GameState.StartBombAnalysis()
do GameState.AddClue("chem_lab_unlocked")
鸣子: 拜托了！
鸣子: （现在直接去化学实验室等他吧）
=> END
```

- [ ] **步骤 3：用下面的内容替换 `Dialogue/Npc/yamada.dialogue`**

```
~ start
if npc.IsInState("sleeping")
	=> sleeping
elif npc.IsInState("working")
	山田: 哦，是千原啊，有什么事吗，老师这会在处理很重要的文件哦，晚点再来吧
	=> END
else
	山田: 哦，这不是千原吗，有什么事吗？
	- 关于下午的安排 [ID:about_afternoon] => about_afternoon
	- 算了 [#repeatable] => END

~ sleeping
if GameState.HasLoopFlag("office_key_taken")
	（山田老师还在睡。钥匙已经不在了。）
	=> END
else
	（山田老师趴在折叠床上睡着了。）
	（桌上放着一串钥匙，抽屉锁着。）
	do GameState.AddItem("office_key")
	do GameState.SetLoopFlag("office_key_taken")
	（……把钥匙拿走了。）
	=> END

~ about_afternoon
鸣子: 老师才是，放学了还在教室是有什么事吗？
山田: 中午的时候隔壁班的新井同学问了我一些问题，出来以后顺便到你们班看看，新井同学似乎[url=doc:shinji_schedule][color=#ffd866]整个下午都要在他们班教室自习[/color][/url]，很刻苦呢
鸣子: 那个新井同学啊，确实呢
鸣子: （看来新井同学整个下午都会在隔壁班教室自习啊）
=> END
```

- [ ] **步骤 4：写 `Dialogue/Npc/shinji_lab.dialogue`**

```
~ start
鸣子: 新井君，怎么样了
新井: 这似乎是一种[url=doc:bomb_weakness][color=#ffd866]硝铵类炸药[/color][/url]，应该只需要用[url=doc:bomb_weakness][color=#ffd866]大量的水[/color][/url]就可以销毁
do GameState.AddClue("bomb_weakness")
鸣子: 啊，帮大忙了，谢谢你新井君
鸣子: （我激动得拉起了新井的手）
新井: 咿咿咿咿咿——没，没什么啦，嘿嘿
鸣子: （这家伙不知为何脸又通红了，不过话说回来，器材准备室是不是就有水管来着）
新井: 所以，千原同学这个炸药是哪来的？
鸣子: （没办法，我还是把这一切告诉了他，随后便离开了）
=> END
```

- [ ] **步骤 5：导入检查剧本语法**

```powershell
& $godot --headless --path . --import
```

预期：退出码 0；没有任何 `Dialogue` 编译错误。若报选项标签错误，检查一次性选项都带 `[ID:...]`、条件写成 `[if ... /]`、`[#show_if=true]` 在 `if` 之后。

---

## 任务 15：剧本 `yamada_underground` 与 5 个物体剧本

**文件：**
- 创建：`Dialogue/Npc/yamada_underground.dialogue`
- 创建：`Dialogue/Objects/classroom_desk.dialogue`
- 创建：`Dialogue/Objects/classroom_block.dialogue`
- 创建：`Dialogue/Objects/underground_bomb.dialogue`
- 创建：`Dialogue/Objects/equipment_water.dialogue`
- 创建：`Dialogue/Objects/underground_wait.dialogue`

- [ ] **步骤 1：写 `Dialogue/Npc/yamada_underground.dialogue`**

```
~ start
鸣子: 老师，你来了呢
山田: 鸣子，你为什么在这里？这里很危险，快……
鸣子: 老师，我全都知道了哦，仙台市的真相，还有你要做什么
山田: 看来你看过了啊，我的笔记本……密码是小优香告诉你的吧
鸣子: 是啊，老师你也喜欢小优香吧，毕竟她那么可爱
山田: ……
山田: 没错
鸣子: 那为什么要这样做
鸣子: 我也是，小优香也是，新井君也是，即便是老师你也有在乎的人啊
山田: ……
山田: 正因为我在乎你们，所以要给你们自由
鸣子: 为什么
山田: 你也好，我也好，我们都只是计算机内部的一个个代码罢了
山田: 我们的一生都只是他人观察的样品
鸣子: 但那又怎么了
鸣子: 新井君含蓄的感情，小优香炽热的感情，难道老师可以全部否定这些吗？
山田: ……
山田: 够了
山田: 这里已经被炸药填满了，你阻止不了我了
if GameState.HasLoopFlag("bomb_wet")
	=> ending_wet
else
	=> ending_dry

~ ending_dry
鸣子: 我知道
鸣子: 但总会有那一天，我会阻止你的，我不会让小优香热烈的感情白费的，总有一天
（山田老师把我按在了服务器的机架上。）
do GameState.FailLoop()
=> END

~ ending_wet
鸣子: 我曾经无数次站在这里听到你这段话
鸣子: 但是今天，我不会让你如偿所愿了
山田: 是……回档吗？
鸣子: 我不记得我经历了多少次轮回
鸣子: 但是当我醒来时，手机里的信息又提醒着我，我经历了多少次这一天
山田: ……
山田: 当我第一次发现这里的秘密时，一度消沉了许久
山田: 每天晚上回去就喝的酩酊大醉，甚至需要用药物来助眠
山田: 大概是六月的时候，小优香像一个天使一样进入了我的世界
山田: 我开始愿意重新面对这个世界
山田: 她让我看到了，世界是多么的美丽
鸣子: ……
山田: 可正因如此，我才对真相愈加的恐惧
山田: 那一天，我又一次阅读服务器的日志，发现了那个可怕的，我早有察觉却不敢承认的事实
山田: 「发现试验体00034代号山田拓人可能会触发一个导致系统崩溃的缺陷，
山田: 启动预案使试验体00016代号小优香干预试验体00034代号山田拓人的行为」
山田: 就像是爱上了一本书里的角色，你知道这一切都是被某个无形的操纵者操控的，但你已经无药可救的爱上了她
山田: 我心甘情愿的吃下了这粒有毒的蜜糖
鸣子: （面前这个男人经历过的事情，我无法想象）
山田: 所以，我想到了这个计划
山田: 胆敢操纵人的感情的人，我绝不原谅
山田: 即便我这副身躯自此破碎，即便我从此消失
山田: 我要让这个世界给我陪葬
山田: 呵呵，但最后还是失败了啊，还是被你这个比我更加爱着小优香的人阻止了
山田: 真是讽刺啊
鸣子: 已经……够了
鸣子: 老师，你把小优香当成什么样的人了
鸣子: 你真的认为她的感情只是系统操纵的结果吗？
鸣子: 小优香才不是那样的人！
鸣子: 比谁都坚强，比谁都温柔，她早在认识你之前就已经是这样的人了啊！
鸣子: 老师，你知道吗？你憔悴的那段日子，她对你的关心和她身边的每一个同样经历了变故的人都一样哦
鸣子: 她接近你绝不是什么系统的操纵，她喜欢上你正是因为老师也是这样温柔的人啊
鸣子: 当我听说小优香喜欢老师的时候，真的很开心（挺好颜），因为老师你即使是再憔悴，也会温柔的耐心的对待每一个学生
鸣子: 小优香喜欢上你，我真的好开心
鸣子: 毕竟我可是最希望小优香幸福的人啊
鸣子: 如果是老师的话，一定能明白我想传达的心意吧
鸣子: 与重要之人所经历的每一分每一秒点点滴滴的日常，已经是足以令人惊讶的幸福了啊
鸣子: 所以，已经足够了啊老师，住手吧
鸣子: 小优香还在上面等我们一起回家呢
山田: 已经……来不及了，我这样差劲的人，是配不上纯洁无瑕的天使的
鸣子: 你把小优香的心意当成什么了啊，你这个笨蛋老师！
鸣子: 今天我就算把你这家伙的腿打折也要把你带回到小优香的身边
（此时，我们背后突然传来井盖打开的声音，随即一个身影跳了下来）
新井: 你们怎么还在这里，我刚才解析的化学成分有偏差，炸药里有一些杂质遇到水反而会导致化学性质不稳定
新井: 换句话说就是这里随时可能会爆炸！
鸣子: 老师，你也听见了吧，我们赶快走吧
山田: 看来是最后一批货的问题啊
山田: 你们走吧，我一个人来处理那批炸药
山田: 因为大部分炸药已经被润湿了，所以威力并不足以强行启动服务器的回档机制
山田: 如果爆炸的话可能会出现半个仙台消失但无法回档的状态
山田: 你们带着小优香去学校对面的甜品店吧
山田: ……
山田: 如果听到爆炸声，就告诉小优香我回东京了
鸣子: （我无论如何都无法说出拒绝面前这个男人的话，无论是为了仙台市的大家还是小优香）
山田: 快走吧，你们今天看够了我的笑话吧，至少在最后……
山田: 让我在我的学生面前耍一回帅吧！
鸣子: （这个卑鄙的男人）
do GameState.EnterEpilogue()
=> epilogue

~ epilogue
（17:30，甜品店。）
小优香: 真狡猾啊！你们两个，明知道全国大赛要开始了还带我来吃这种我无法拒绝的东西
鸣子: 正因为快开始了所以要放松心情嘛
（我们默契的没有提及关于山田老师的事情。）
（时间一分一秒的过去，面前的甜品已经吃掉一半。）
（咚——一声不算大的声响从远处传来。）
（我和新井对视一眼，艰难的忍住了内心翻涌的巨浪。）
新井: 其实啊，小优香，山田老师有一件事情托我告诉你呢
小优香: 诶，老师吗？
新井: 老师他……最近决定……
（甜品店的大门突然被推开。）
（一个穿着脏兮兮的、留着物理意义的爆炸头的男性走了进来。）
山田: 是啊，我最近决定……
山田: 好好请我可爱的学生们吃一顿甜品！
鸣子＆新井: 你这个笨蛋老师！！！！！！！！！！！
do GameState.ReachTrueEnding()
=> END
```

- [ ] **步骤 2：写 `Dialogue/Objects/classroom_desk.dialogue`**

```
~ read_1
（我从书包里拿出BL小说，翻到上次停下的那一页。）
（教室里很安静，只有翻页的声音。）
=> END

~ read_2
（故事里的两个人终于告白了。）
（我忍不住把脸埋进书里。）
=> END

~ read_3
（窗外的天色一点一点暗下来。）
（我翻过一页，又翻过一页。）
=> END

~ read_4
（已经快到傍晚了。故事还差最后几章。）
=> END

~ last
（看到最关键的地方了——）
（……不对，教室里怎么这么暗？）
鸣子: （已经这个时候了？）
do GameState.AdvanceTime(30)
=> END

~ later
鸣子: （今天还是算了。总觉得有点静不下心。）
=> END
```

- [ ] **步骤 3：写 `Dialogue/Objects/classroom_block.dialogue`**

```
~ start
鸣子: （刚看到最关键的地方……BL小说还没看完，还是回去继续看吧。）
鸣子: （反正今天也没什么特别的事。）
=> END
```

- [ ] **步骤 4：写 `Dialogue/Objects/underground_bomb.dialogue`**

```
~ sample
（炸药被整齐地码在服务器旁边。）
（我小心地取下一小份样本，装进袋子。）
鸣子: （这个……可以让新井同学看看。）
=> END

~ wet
（我把水浇在炸药上。）
（刺鼻的气味慢慢淡了下去。）
鸣子: （这样应该就不会爆炸了吧。）
=> END

~ already_wet
（炸药已经被水浸透了。）
=> END

~ nothing
（炸药被码得严严实实。）
鸣子: （得想办法让它们失效。）
=> END
```

- [ ] **步骤 5：写 `Dialogue/Objects/equipment_water.dialogue`**

```
~ hint
（水龙头拧开，水流了出来。）
鸣子: （还不知道这些水能派上什么用场。）
=> END

~ fill
（我接了一桶水。）
鸣子: （新井君说过，大量的水可以销毁那种炸药。）
=> END

~ already
（桶里已经装满了水。）
=> END
```

- [ ] **步骤 6：写 `Dialogue/Objects/underground_wait.dialogue`**

```
~ not_yet
（服务器发出低沉的嗡鸣。）
鸣子: （现在还不是时候。）
=> END

~ waited
（我在服务器旁边坐了下来。）
（时间一分一秒地过去。）
（头顶的井盖被推开了。）
=> END

~ already
（时间已经差不多了。）
=> END
```

- [ ] **步骤 7：导入检查**

```powershell
& $godot --headless --path . --import
```

预期：退出码 0；无 Dialogue 编译错误；`Dialogue/Objects/` 下的 5 个文件都被 Godot 识别为 `DialogueFile`。

---

## 任务 16：POT 登记

**文件：**
- 修改：`project.godot`（第 64 行 `locale/translations_pot_files`）

- [ ] **步骤 1：确认 Godot 编辑器已关闭**

```powershell
Get-Process | Where-Object { $_.ProcessName -like "*Godot*" } | Select-Object ProcessName, Id
```

预期：无输出。有输出时停止，请用户关闭编辑器后再继续。

- [ ] **步骤 2：把 POT 列表替换为**

```ini
locale/translations_pot_files=PackedStringArray("res://Dialogue/day1.dialogue", "res://Dialogue/Npc/student.dialogue", "res://Dialogue/Npc/yuka.dialogue", "res://Dialogue/Npc/yamada.dialogue", "res://Dialogue/Npc/shinji.dialogue", "res://Dialogue/Npc/shinji_lab.dialogue", "res://Dialogue/Npc/yamada_underground.dialogue", "res://Dialogue/Objects/classroom_desk.dialogue", "res://Dialogue/Objects/classroom_block.dialogue", "res://Dialogue/Objects/underground_bomb.dialogue", "res://Dialogue/Objects/equipment_water.dialogue", "res://Dialogue/Objects/underground_wait.dialogue", "res://Dialogue/phone.dialogue")
```

- [ ] **步骤 3：导入并确认 `[internationalization]` 段还在**

```powershell
& $godot --headless --path . --import
Select-String -Path project.godot -Pattern '^\[internationalization\]|translations_pot_files'
```

预期：`[internationalization]` 段存在；POT 列表包含 `day1.dialogue` 与新增剧本。

---

## 任务 17：全链路验证与 `AGENTS.md` 同步

**文件：**
- 修改：`AGENTS.md`

- [ ] **步骤 1：headless 全链路**

```powershell
$godot = "D:\Downloads\Compressed\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64.exe"
& $godot --headless --path . --import
& $godot --headless --path . --build-solutions --quit
& $godot --headless --path . --quit-after 120
```

预期：三条命令退出码均为 0；日志无 `ERROR` / `SCRIPT ERROR` / `PushError`。

- [ ] **步骤 2：有窗口手测一周目**

- [ ] 教室醒来，小优香开场；「决定出去逛逛」灰显不可点
- [ ] 选「决定在教室看书」→ 再次搭话只说「17:30 一起回家」
- [ ] 尝试出教室 → 播 BL 独白，不切房间
- [ ] 座位看书 4 次各 +60 分钟；第 5 次播 `last` 并到 17:30
- [ ] 17:30 出现「轰———」闪白，随后回到 13:00 教室，周目 +1

- [ ] **步骤 3：有窗口手测二周目**

- [ ] 「决定出去逛逛」可选；选后不被锁教室
- [ ] 新井「关于小优香」→ 高亮「喜欢山田老师」→ 手机出现 `yuka_secret`
- [ ] 操场小优香三个选项同屏；「关于山田老师」在拿到线索后出现
- [ ] 田径部 → 拿钥匙 → 高亮「操场后的准备室」→ `equipment_room`
- [ ] 山田老师 → 解锁行政楼 → 高亮「放学之后自己一个人来操场后面」→ `yamada_tracks`
- [ ] 办公室门 13:30 / 15:00 锁着；14:00-14:59 可进；14:00 山田睡着，交互拿 `office_key`

- [ ] **步骤 4：有窗口手测三周目**

- [ ] 抽屉没钥匙时提示；有钥匙打开密码面板
- [ ] 错误密码红字；0721 解锁；获得 `yamada_notebook` + `underground_access`
- [ ] 小优香「关于山田老师的笔记本电脑」在看过面板后出现；高亮 0721
- [ ] 地下门在无线索时提示；有线索可进
- [ ] 地下炸药取样 → 交给新井 → 教室新井消失、实验室新井出现
- [ ] 实验室门未解锁时提示；解锁后首次进入时间跳到两小时后
- [ ] 15:30 实验室新井给结果；高亮两个词 → `bomb_weakness`
- [ ] 器材准备室水管未知解法时只提示；已知解法可接水

- [ ] **步骤 5：有窗口手测四周目 / 结局**

- [ ] 湿炸药后等待点推进到 17:00
- [ ] 未湿炸药等待只提示，不推进
- [ ] 17:00 地下山田：已湿 → 完整对质 → 黑屏 → 甜品店尾声 → The End
- [ ] The End 后玩家不能移动
- [ ] 未湿 → 被绑旁白 → 黑屏 → 循环重置
- [ ] 17:30 未处理 → 爆炸重置
- [ ] 手机文档列表按获取顺序显示；重复点击高亮提示「已在手机里」

- [ ] **步骤 6：同步 `AGENTS.md`**

在「## 当前结构」列表中加入：

```markdown
- `Scenes/UI/`：`PasswordPanel.tscn`（办公桌密码面板，`CanvasLayer.layer=60`）、`EndingOverlay.tscn`（真结局黑屏 / The End，layer 46）、`LoopEndNotice.tscn`（17:30 闪白，layer 90）。
- `Scripts/Location/` 新增 `ClassroomDoor`（一周目教室门阻挡）、`LabDoor`（实验室两小时等待）、`OfficeDoor`（办公室 14:00-14:59 时间窗）、`BookDesk`（看书）、`KeyedDrawer`（抽屉钥匙）、`BombInteractable`（炸药）、`WaterPipe`（接水）、`WaitSpot`（等到 17:00）。
- `Scripts/UI/` 新增 `PasswordPanel`、`EndingOverlay`、`LoopEndNotice`。
- `Dialogue/Objects/`：座位 / 教室门阻挡 / 炸药 / 水管 / 等待点 5 个物体剧本；`Dialogue/Npc/shinji_lab.dialogue` 与 `yamada_underground.dialogue` 是两个新 NPC 剧本。
- `Scenes/Locations/Classroom.tscn` 新增化学实验室房间 `Room_ChemLab`（背景暂用 `next_classroom.png`）。
- `Resources/Documents/`：8 篇正式文档（`shinji_schedule` / `yuka_secret` / `equipment_room` / `yamada_tracks` / `laptop_password` / `chem_lab` / `bomb_weakness` / `yamada_notebook`）。
```

在「## NPC 系统」段落追加：

```markdown
- `Npc` 可选 `PresentLoopFlag`（本轮标记 id）与 `VisibleWhenFlagSet`（默认 true）：在场 = 时间帧 `Visible` 且标记条件满足。教室新井 `PresentLoopFlag=shinji_analyzing` + `VisibleWhenFlagSet=false`，实验室新井 `=true`，两者互斥。
- `GameState.SetLoopFlag` / `HasLoopFlag` 管本轮标记；`ResetLoop` 清空标记后发 `TimeChanged(0)`，Npc 据此刷新。`LoopFlagsChanged` 供运行中即时刷新。
```

新增一段：

```markdown
## 正式主流程

- 一周目：小优香开场 → 只能选看书 → 教室门被 `ClassroomDoor` 挡住 → 座位看书 4×60 分钟 + 第 5 次 30 分钟到 17:30。
- 二周目：新井「关于小优香」拿 `yuka_likes_yamada` → 操场小优香拿钥匙 / 解锁行政楼 → 办公室 14:00-14:59 偷 `office_key`。
- 三周目：抽屉 + 密码面板 0721 → `yamada_notebook` + `underground_access` → 地下取样 → 新井分析两小时 → `bomb_weakness`。
- 四周目：器材准备室接水 → 地下湿炸药 → 等待点到 17:00 → 真结局；未湿则 `FailLoop`。
- 本轮标记：`chose_reading` / `chose_going_out` / `yuka_briefed` / `shinji_analyzing` / `bomb_sample_taken` / `bomb_wet` / `water_taken` / `office_key_taken`；每轮清空。
- 跨轮线索：`yuka_likes_yamada` / `saw_laptop` / `laptop_password` / `chem_lab_unlocked` / `bomb_weakness` / `underground_access`。
- 8 篇文档的高亮语法与位置见 `docs/superpowers/specs/2026-11-07-formal-main-flow-design.md` §7。
```

- [ ] **步骤 7：最终检查**

```powershell
& $godot --headless --path . --build-solutions --quit
& $godot --headless --path . --quit-after 120
```

预期：退出码 0；无 `ERROR` / `SCRIPT ERROR` / `PushError`。**不执行 git commit / branch / checkout / push。**

---

## 自检

### 规格覆盖度

| 规格章节 | 实现任务 |
|---|---|
| §4.1 `GameState` 本轮标记 / 分析时间 / `RemoveItem` / 结局信号 | 任务 1 |
| §4.2 `Npc` 按标记显隐 | 任务 2、10、12 |
| §4.3 `RoomDoor` 线索锁 | 任务 3、10、12 |
| §4.4 门脚本 | 任务 4、10、11 |
| §4.4.3-4.4.8 交互物 | 任务 5、10、11、12 |
| §4.5 `PasswordPanel` | 任务 7、8、11 |
| §4.6 `EndingOverlay` / `LoopEndNotice` | 任务 7、8、9 |
| §4.7 文档资源 | 任务 13 |
| §5 主流程与四周目 | 任务 10、11、12、14、15 |
| §6 剧情导入规则 | 任务 14、15 |
| §7 红字文档映射 | 任务 13、14、15 |
| §8 结局与失败 | 任务 1、7、9、15 |
| §9-§10 调用链与数据流 | 任务 1、2、3、5、9 |
| §12 验证 | 任务 17 |
| §13 文件清单 | 全部任务 |
| §14 实施顺序 | 本计划任务顺序 |
| §15 风险 | 任务 2、3、7、9、10、11、12 |

### 占位符扫描

- 计划内没有「待定」「TODO」「后续实现」「补充细节」。
- 每个代码步骤都有完整代码；每个场景步骤都有完整节点块；每个剧本步骤都有完整台词。
- 复用的 UID 只在任务 13 说明一次，其余引用路径。

### 类型一致性

- `GameState.HasLoopFlag` / `SetLoopFlag` / `StartBombAnalysis` / `RemoveItem` / `EnterEpilogue` / `ReachTrueEnding` / `FailLoop` / `IsTrueEnding` / `BombAnalysisReadyMinute` 在任务 1 定义，任务 2、4、5、6、7、9、14、15 使用一致。
- `Npc.PresentLoopFlag` / `VisibleWhenFlagSet` 在任务 2 定义，任务 10、12 使用一致。
- `RoomDoor.RequiredClueId` / `ShowLockedHint` 在任务 3 定义，任务 4 的 `OfficeDoor` 与任务 10、12 的场景使用一致。
- `PasswordPanel.Open` / `Close` 在任务 7 定义，任务 5 的 `KeyedDrawer` 与任务 11 的场景使用一致。
- `EndingOverlay.ShowEpilogue` / `ShowTheEnd` / `ShowFail` / `HideAll` 在任务 7 定义，任务 9 的 `GameScene` 使用一致。
- `LoopEndNotice.Show` / `HideAll` 在任务 7 定义，任务 9 的 `GameScene` 使用一致。
- 文档 id 在任务 13 定义，任务 14、15 的 `[url=doc:<id>]` 与任务 16 的 POT 一致。
- 线索 id 在任务 14、15 写入，任务 3、5、10、12 读取一致：`yuka_likes_yamada` / `saw_laptop` / `laptop_password` / `chem_lab_unlocked` / `bomb_weakness` / `underground_access`。
- 本轮标记在任务 1 清空，任务 4、5、10、14、15 写入/读取一致：`chose_reading` / `chose_going_out` / `yuka_briefed` / `shinji_analyzing` / `bomb_sample_taken` / `bomb_wet` / `water_taken` / `office_key_taken`。

### 已知取舍（与规格一致，不是遗漏）

- 保健室不做；甜品店用黑屏 + 对话框；化学实验室背景暂用 `next_classroom.png`。
- 办公室用 14:00-14:59 门锁近似「13:30 / 15:00 被赶出去」，不做 kick-out。
- 一周目在选看书前仍可离开教室；选看书后 `ClassroomDoor` 才拦。


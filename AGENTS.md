# AGENTS.md

Godot 4.7.2 (mono) + C# 的 48 小时 Game Jam 项目：2D 时间循环（loop）叙事游戏（策划案标题：环cycle）。

## 策划案（唯一需求来源）

- 飞书 wiki：<https://my.feishu.cn/wiki/IjAAwgxwviZZHrkELxVcACSjnEg> —— 用户会持续更新，动手实现前先拉最新：
  `lark-cli docs +fetch --doc "https://my.feishu.cn/wiki/IjAAwgxwviZZHrkELxVcACSjnEg" --as user --doc-format markdown`
- 正文只返回 `<whiteboard token=...>`，五个画板要单独导出（`--output` 只接受 cwd 相对路径）：
  - 头脑风暴：`BPx2w4eIThB8Itb4lHbciSoVnGb`
  - 参考游戏：`XuL4wDrLthTcvCbdKqkcf9bNn4H`
  - 美术参考：`IJQvwZPEphfe2TbizY6clvk7nGl`
  - 系统设计：`DBPXwz6bZhoMYibtavJcRnOvnFg`
  - 流程设计：`TuvrwaeUthUybzbXURocPXA1nDg`
  命令：`lark-cli whiteboard +export --whiteboard-token <token> --output-type raw --output ./wb.json --as user`；文字在 `nodes[].text.text`，按 x/y 排序可还原大概布局。
- 已定设定：JK 主角、现代日本校园；定时炸弹末日 + 幕后黑手；平视 2D，A/D 左右走、F 交互，部分交互切到第一人称鼠标点击；到点触发大事件后重置循环，NPC 在各时间点的行为固定。
- 定位：点击式步行模拟器 + 知识锁推进（靠每轮累积的线索拼出真相）。**不做平台跳跃**。
- 系统设计：场景内是卷轴地图，与物品交互（拾取钥匙、打火机等；有钥匙可开锁；翻阅文档并记录）；离开场景后出现地图，点击地点前往，知识锁解锁新地点；手机记录信息（画板还标了「某一时间某一…」，细节未定），**不随循环回溯**。
- 流程设计：与 NPC 对话 / 与场景交互 → 获得新信息、出现新对话选项 → 解锁新场景 → 移动、时间流逝 → 场景与 NPC 行为变化 → 通关；到点触发大事件则重置循环。
- 参考：命运石之门、梅祖拉的假面（NPC 固定日程）、十三机兵防卫圈、星际拓荒（到点大事件立刻重启）、人狼村之谜、杀手47、加利宅邸悬案（每个场景的人物清单）、最后一次循环（幕后黑手操纵循环）；美术走十三机兵的 JK 与 galgame 对话框。

## 当前结构

- `Scenes/Root/GameScene.tscn`：主场景（`run/main_scene` 已指向它），`GameScene.cs` 负责地点装载/卸载、地图显隐、过渡、响应 `LoopReset`。节点：`LocationHost`（当前地点挂载点）、`UILayer/MapScreen`。
- `Scenes/player.tscn`：`CharacterBody2D` + `AnimatedSprite2D` + `CollisionShape2D` + `Camera2D` + `InteractionRange`（`PlayerInteractor`）；贴图暂用 `icon.svg`。
- `Scenes/Map/MapScreen.tscn`：地图 UI 面板（占位色块 + 地点按钮 `BtnClassroom` / `BtnPlayground`）。
- `Scenes/Locations/`：`Classroom.tscn`（两房间：教室 / 器材室）、`Playground.tscn`（demo 占位地点）。
- `Resources/MapConfig.tres`：初始解锁地点 `["classroom"]`。
- `Scripts/`：按模块分目录 —— `Core/`（`GameState`、`ScreenFade`、`Interactable`）、`Map/`（`GameScene`、`MapScreen`、`MapLocationButton`、`MapConfig`）、`Location/`（`LocationRoot`、`Room`、`RoomDoor`、`LocationExit`）、`Player/`（`Player`、`PlayerInteractor`）、`Demo/`（`PickupItem`、`ReadDocument`）。
- `Dialogue/day1.dialogue`：占位剧本，**必须存在**，否则每次跑 Godot 都会把 `project.godot` 的 `[internationalization]` 段删掉。
- `ArtRes/`：美术素材（当前为空）。
- `addons/dialogue_manager/`：Dialogue Manager 4.2.0；`export_presets.cfg` 已有 Windows Desktop 预设（`export_path` 为空）。

## 对话系统

- Dialogue Manager 4.2.0（Nathan Hoad），已启用编辑器插件并设置 `runtime/advanced/uses_dotnet=true`，autoload 名 `DialogueManager`（`uid://c3rodes2l3gxb`）。
- 剧本写在 `Dialogue/*.dialogue`，不要把对白硬编码进 C#；对话 UI 从 `addons/dialogue_manager/example_balloon/ExampleBalloon.tscn`（C# 版）起步，不要自己写解析器。
- C# API 在 `DialogueManagerRuntime` 命名空间：快速验证用 `DialogueManager.ShowExampleDialogueBalloon(resource)`，正式 UI 用 `ShowDialogueBalloonScene(自己的 balloon, resource)`。
- `project.godot` 的 POT 生成已登记 `res://Dialogue/day1.dialogue`（目前是占位内容）；新增/改名剧本时同步 `[internationalization] locale/translations_pot_files`。

## 工具链

- 引擎：`D:\Downloads\Compressed\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64.exe`（项目 features=4.7；C# 目标框架 net8.0，本机 .NET SDK 10 + 运行时 8 构建正常）。同目录 `_console.exe` 便于在终端看输出。
- 校验命令（仓库根目录执行）：
  - 导入资源：`& $godot --headless --path . --import`
  - 编译 C#：`& $godot --headless --path . --build-solutions --quit`（或 `dotnet build`）
  - 无头跑主场景并立刻退出：`& $godot --headless --path . --quit-after 120`（需先在项目设置指定主场景）
- `*.csproj` / `*.sln` 已存在且可编译；它们由 Godot 首次创建 C# 脚本时生成。
- godot MCP 已配在 `opencode.json`（`mcp.servers.godot`，源码构建于 `%USERPROFILE%\.local\share\godot-mcp`，`GODOT_PATH` 指向上面的 exe）。优先用它做反馈闭环：`launch_editor`、`run_project` + `get_debug_output`、`stop_project`；项目相关工具要传绝对 `projectPath`。场景用 `create_scene` / `add_node` / `save_scene` 生成，手写 `.tscn` 容易破坏资源引用。

## 开发约定（48h jam）

- 先做能玩的最小闭环，再谈表现。**不要过度设计**：不引入 DI / 事件总线 / 通用框架 / 单元测试框架，不做为「以后扩展」预留的分层。
- 只实现用户当轮要求，不顺手重构、不主动扩需求。
- 脚本一律 C#：`.cs` 文件名与类名一致，`public partial class X : Node`。不要写 GDScript。
- 对白/分支走 Dialogue Manager 的 `.dialogue`，C# 只负责状态、场景与交互；不要把对白硬编码进脚本。
- 空引用、资源缺失要显式报错，禁止用默认值或空实现静默兜底。
- git 只读：禁止 commit / branch / checkout / push（用户明确要求）。
- 这是 2D 项目，但 `project.godot` 仍是 Forward Plus + d3d12、拉伸 `canvas_items/expand`；没有明确需要不要改渲染设置。

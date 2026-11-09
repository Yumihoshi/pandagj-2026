# AGENTS.md

Godot 4.7.2 (mono) + C# 的 48 小时 Game Jam 项目：2D 时间循环（loop）叙事游戏（正式名：今天也准时爆炸，策划案标题：环cycle）。

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

- `Scenes/Root/MainMenu.tscn`：主菜单（`run/main_scene` 已指向它）——教室背景 + 径向暗角 + 「今天也准时爆炸」标题 / `cycle` 副题 / 分割线 + 「开始循环」「退出循环」两个按钮；`Scripts/UI/MainMenu.cs` 负责入场淡入上浮、背景缓慢推拉，「开始循环」淡出后 `ChangeSceneToFile` 进 `GameScene.tscn`，「退出循环」淡出后 `GetTree().Quit()`。右下角 `Mascot` 用 `ArtRes/UI/mascot.png`（三只吉祥物并排，透明底，1438×568）贴角摆放（760×300，`stretch_mode = KEEP_ASPECT_CENTERED`、`mouse_filter = IGNORE`），比标题晚 0.35 秒上浮淡入，入场结束后做 5px / 2.5 秒的往复呼吸浮动。
- `Scenes/Root/GameScene.tscn`：游戏主场景（由主菜单切入），`GameScene.cs` 负责地点装载/卸载、地图显隐、过渡、响应 `LoopReset` 与 `LoopEndReached`（17:30 终末）。节点：`LocationHost`（当前地点挂载点）、`UILayer/MapScreen`、`UILayer/TimePanel`（右上角游戏时钟）、`PhoneScreen`（左上角手机）、`CutsceneStage`（剧情立绘舞台，layer 44）、`EndingOverlay`（The End / 失败黑屏，layer 46）、`LoopEndNotice`（17:30 爆炸 1/2 轮切 → 爆炸 3 + 震屏，layer 90）。
- `Scenes/player.tscn`：`CharacterBody2D` + `AnimatedSprite2D` + `CollisionShape2D` + `Camera2D` + `InteractionRange`（`PlayerInteractor`）；立绘帧用 `ArtRes/Characters/muriko/muriko_frames.tres`（`idle` 立绘 + `run` 6 帧跑步 + `back` 背影），`AnimatedSprite2D` scale 0.29592、offset y -1053（脚底落在碰撞体下沿），碰撞 72×144、交互范围 216×216；`Player` 移动时播 `run`、静止播 `idle`，向右移动 `FlipH=true`（跑步帧原图朝左）；与非 NPC 物件交互触发对话时播 `back` 背影 —— `PlayerInteractor` 调 `Player.RequestBackPose()`，`Player` 把「对话锁住输入」当作开始信号、解锁即回 `idle`，0.15 秒内没锁输入就丢弃请求（开门 / 出口不闪背影）。
- `Scenes/Map/MapScreen.tscn`：大地图（全屏 1920×1080）—— 底层 `MapBase` 是黑白校园平面图 `ArtRes/UI/Map/campus_map_line.png`（1601×900 拉伸铺满），`Highlights` 里三个彩色建筑（`campus_classroom_color` / `campus_administration_color` / `campus_playground_color`）随解锁显隐，所以已解锁地点在地图上显示为彩色、未解锁区域保持黑白线稿；`LocationButtons` 的 `BtnClassroom` / `BtnAdministration` / `BtnPlayground` 是透明按钮，压在各建筑切图的包围盒上（518/118、513/691、979/668）；`MapLocationButton.Highlight` 指向对应高亮，悬停时提亮并播 Rollover。地图不再有标题「学校地图」、底部提示与地点名药丸标签（2026-11-08 移除）。
- `Scenes/Locations/`：`Classroom.tscn`（地点「教室」：教室 / 走廊 / 隔壁班，NPC 小优香 / 山田老师 / 新井次郎）、`Playground.tscn`（地点「操场」：操场 / 操场后 / 器材准备室 / 地下空间，NPC 小优香 / 山田老师）、`Administration.tscn`（地点「行政楼」：入口 / 办公室，NPC 山田老师）。「操场后」用新背景 `Playground/playground_back_2.png`（房子正门 `DoorToPlayground` x=1020、砖砌井口 `DoorToUnderground` x=3297，`DoorToEquipment` 无美术锚点留在 x=2000）。化学实验室已建（`Room_ChemLab`，背景 `Classroom/chem_lab.png`）；保健室尚未建场景。
- 地点房间的世界尺寸：高 1080（铺满视口），宽按背景图比例 —— 多数 `CameraBounds` 3844×1080，化学实验室 / 器材准备室 3240×1080；房间左右边界即空气墙，`LocationRoot.ApplyRoomBounds` 用同一份 `CameraBounds` 同时设相机限位与 `Player.SetHorizontalBounds`，玩家中心被夹在边界内侧 `Player.EdgeMargin`（默认 120，避免立绘被屏幕边缘裁掉），不需要在场景里另放碰撞体；`Bg` 的 `offset_right` 与 `CameraBounds` 同步。房间内所有对象、立绘、碰撞由旧 1920×600 布局放大 1.8 倍得到（x 另乘「背景宽/1920」），玩家 `MoveSpeed = 900`；人物脚底贴在屏幕下边缘（世界 y=1080）—— NPC 节点 y=1083.5、玩家与所有 Marker2D 落点 y=1008，立绘的 `offset` 是贴图内像素偏移，不可跟着缩放。`Room_NextClassroom` 暂无专属背景，复用 `Classroom/classroom.png`。立绘缩放 2026-11-08 统一翻倍（玩家 0.29592、小优香 0.3082、山田 0.2798、新井 0.329）：`offset` 按「贴图内脚底像素 - 1424 + offset，再乘新缩放」保持脚底落在 1080 重算，`NameLabel` 同步上移。
- `Scenes/Dialogue/DialogueBalloon.tscn`：NPC 对话 balloon（从 `ExampleBalloon` 复制改造，`layer = 50`；名字 / 正文 / 选项 / 进度点 / `CopyToast` 复制提示）。
- `Resources/MapConfig.tres`：初始解锁地点 `["classroom"]`。
- `Scripts/`：按模块分目录 —— `Core/`（`GameState`、`ScreenFade`、`Interactable`）、`Map/`（`GameScene`、`MapScreen`、`MapLocationButton`、`MapConfig`）、`Location/`（`LocationRoot`、`Room`、`RoomDoor`、`LocationExit`）、`Player/`（`Player`、`PlayerInteractor`）、`Npc/`（`Npc`、`NpcStateFrame`）、`Dialogue/`（`DialogueBalloon`）、`Documents/`（`DocumentData`、`DocumentLibrary`）、`Phone/`（`PhoneScreen`）、`Audio/`（`AudioManager`）、`UI/`（`TimeLabel`、`ButtonTextColors`、`InteractPrompt`）、`Demo/`（`PickupItem`、`ReadDocument`，当前未挂场景）。
- `Scenes/UI/`：`PasswordPanel.tscn`（办公桌密码面板，`CanvasLayer.layer=60`）、`CutsceneStage.tscn`（剧情立绘舞台：甜品店 CG `ArtRes/Backgrounds/Ending/dessert_shop.png` + 左侧鸣子 / 小优香 / 新井三人立绘 + 右侧单人立绘，layer 44）、`EndingOverlay.tscn`（The End / 失败黑屏，layer 46）、`LoopEndNotice.tscn`（17:30 爆炸 1/2 轮切 → 爆炸 3 + 震屏，layer 90）、`InteractPrompt.tscn`（头顶「F 交互」提示）。
- 交互提示：`InteractPrompt`（`Node2D`，`top_level` + `z_index=100`，深色药丸 + 金色键帽 `F` + 白字「交互」）作为 Player 子节点挂在 `player.tscn`，被每处地点实例化的 Player 自动带出；`PlayerInteractor` 定位优先级：目标下有 `NameLabel` 时贴到该标签上边中点（避免压住头顶名字），否则用 `_current` 的碰撞形状全局包围盒上边中点（目标切换时重算头部坐标），`InputLocked` 或没有可交互目标时淡出隐藏；目标没有可用 `CollisionShape2D` 时 `PushError` 并退到 `FallbackHeadOffset`。
- `Scripts/Location/` 新增 `ClassroomDoor`（一周目教室门阻挡）、`LabDoor`（实验室两小时等待）、`OfficeDoor`（办公室 13:30-16:59 开放，14:00-14:59 山田午睡）、`BookDesk`（看书）、`KeyedDrawer`（抽屉钥匙）、`BombInteractable`（炸药：带水桶交互直接浇灭，否则首次取样；湿完自动跳 17:00 进终局）、`WaterPipe`（接水）、`TalkToYukaFirst`（一周目开场门禁）。
- `Scripts/UI/` 新增 `PasswordPanel`、`CutsceneStage`、`EndingOverlay`、`LoopEndNotice`。
- `Dialogue/Objects/`：座位 / 教室门阻挡 / 炸药 / 水管 / 办公室赶人 / 井盖 6 个物体剧本；`Dialogue/Npc/shinji_lab.dialogue` 与 `yamada_underground.dialogue` 是两个新 NPC 剧本。
- `Scenes/Locations/Classroom.tscn` 新增化学实验室房间 `Room_ChemLab`（背景暂用 `next_classroom.png`）。
- `Dialogue/day1.dialogue`：开场 / 循环开始剧本，三个 cue —— `~ start`（一周目在座位上迷迷糊糊醒来、点名要去找小优香）、`~ phone_vibrate`（二周目起循环开始的「手机在震动。」）与 `~ talk_first`（门禁提示）。**必须存在**，否则每次跑 Godot 都会把 `project.godot` 的 `[internationalization]` 段删掉。
- `Dialogue/Npc/student.dialogue`：旧「同学」demo 剧本，当前未挂到任何 NPC；`yuka.dialogue` / `yamada.dialogue` / `shinji.dialogue` 是三个 NPC 的正式剧本；`shinji_lab.dialogue` / `yamada_underground.dialogue` 是新 NPC 剧本；`Dialogue/Objects/` 是物体剧本。
- `Scenes/Phone/PhoneScreen.tscn`：手机（`CanvasLayer.layer=45`）：左上角「手机」按钮、遮罩、重要的事列表 / 阅读 / 记事本面板。
- `Resources/Documents/`：`DocumentLibrary.tres` 注册表与 9 篇正式条目（`shinji_schedule` / `yuka_secret` / `equipment_room` / `yamada_tracks` / `laptop_password` / `chem_lab` / `bomb_weakness` / `yamada_notebook` / `yamada_nap`）；`club_notice.tres` 保留但不注册。
- `Dialogue/phone.dialogue`：手机主菜单剧本（重要的事 / 记事本 / 关闭）。
- `ArtRes/`：正式美术（飞书需求表导入）在 `Characters/<角色>/`（muriko / yuka / yamada / shinji，含 `muriko_run_01..06` 跑步帧、`muriko_back` 背影与 `yamada_sleep` / `yamada_working` 两张办公坐姿图）与 `Backgrounds/<地点>/`（Classroom / Playground / Administration / Ending；「操场后」用 `Playground/playground_back_2.png`，旧 `playground_back.png` 已不再被场景引用）；大地图切图在 `UI/Map/`（`campus_map_line` 黑白底图、`campus_map_color` 彩色全图、`campus_map_locked_silhouette` 黑色剪影版、`campus_others` 其它建筑、三处建筑的 `_color` / `_gray` 两版）；旧占位素材仍在 `Backgrounds/*.png`（9 张 1920x600 场景背景）与 `Characters/*.png`（4 张 240x300 透明立绘），被现有场景按路径引用；导入映射表见 `CREDITS.md`，`Source/` 保留原图与裁切源。
- `Audio/Sfx/`：Kenney UI Audio 音效，按 `Click`(5) / `Rollover`(6) / `Switch`(38) / `Mouse`(2) 分组，另有 `爆炸,重启.wav` / `爆炸，结尾.wav` / `脚步声_上楼梯_x3.wav` 三个单次长音效，保留 `License.txt`；`Audio/Bgm/` 7 首 mp3。
- `default_bus_layout.tres`：音频总线 `SFX` / `BGM`（`send = Master`），播放器按总线独立控音量。
- `addons/dialogue_manager/`：Dialogue Manager 4.2.0；`export_presets.cfg` 已有 Windows Desktop 预设（`export_path` 为空）。

## 对话系统

- Dialogue Manager 4.2.0（Nathan Hoad），已启用编辑器插件并设置 `runtime/advanced/uses_dotnet=true`，autoload 名 `DialogueManager`（`uid://c3rodes2l3gxb`）。
- 剧本写在 `Dialogue/*.dialogue`，不要把对白硬编码进 C#；对话 UI 用 `Scenes/Dialogue/DialogueBalloon.tscn`（从 `ExampleBalloon` 复制改造），不要直接改 addon 目录。
- C# API 在 `DialogueManagerRuntime` 命名空间：快速验证用 `DialogueManager.ShowExampleDialogueBalloon(resource)`；正式对话用 `DialogueManager.ShowDialogueBalloon(resource)`（默认 balloon 由 `GameScene` 注册）或 `ShowDialogueBalloonScene`。
- 默认 balloon 注册：`GameScene._Ready` 调 `DialogueManager.SetDefaultBalloon(DialogueBalloonScene)` 之后，必须再 `ProjectSettings.SetSetting("dialogue_manager/runtime/balloon_path", DialogueBalloonScene.ResourcePath)`；addon 的 `set_default_balloon` 写的是 `runtime/balloon_path`，读的是 `dialogue_manager/runtime/balloon_path`，只调它会静默回退到 `ExampleBalloon`。
- `.dialogue` 语法：标签写 `[#repeatable]` / `[#show_if=true]` / `[#hide_if=true]`（裸 `#tag` 不解析）；响应条件写 `[if 表达式 /]`（末尾斜杠）；`~ cue` 下的台词/`do`/`=> END` 顶格，只有 `if/elif/else` 分支缩进；同一文件里 `[ID:]` 必须唯一；选项正文不要加方括号，一次性选项必须写 `[ID:]`。
- 剧情立绘由剧本 `do` 驱动，不硬编码在对话 UI：`do GameState.ShowCharacterPortrait("shinji")` 让角色在 `CutsceneStage` 右侧登场；`do GameState.EnterEpilogue()` 切到甜品店 CG 与左侧三人立绘（`GameScene` 把信号转给 `CutsceneStage`）。
- `project.godot` 的 POT 生成已登记 `res://Dialogue/day1.dialogue`、`res://Dialogue/Npc/student.dialogue`、`yuka` / `yamada` / `shinji` / `shinji_lab` / `yamada_underground` 五个 NPC 剧本、`Dialogue/Objects/` 的 7 个物体剧本与 `res://Dialogue/phone.dialogue`；新增/改名剧本时同步 `[internationalization] locale/translations_pot_files`。
- 对白正文支持鼠标拖拽选择：打字完成后打开 `RichTextLabel` 原生选区（主题 `selection_color`），松开时若有选中文本则自动写入系统剪贴板并显示「已复制」；原地单击仍推进对话，判定阈值 `DialogueBalloon.ClickDragThreshold`（默认 6px）。`Balloon/CenterContainer` 必须保持 `mouse_filter = 2`，否则全屏容器会抢走正文的鼠标事件。
- 对话推进只保留鼠标左键：空格/回车既不推进下一句，也不确认选项（2026-11-07 起）。`DialogueBalloon` 已删除 `NextAction` 导出字段，并把插件 `ResponsesMenu.NextAction` 指向运行时注册的无绑定动作 `dialogue_mouse_only_advance`（`InputMap.AddAction` 不加事件，`IsActionPressed` 恒为 false）；不要改 addon 目录，也不要用 `_Input` 吞 `ui_accept`（会误伤密码面板回车确认与记事本换行）。Esc 跳过打字保留。

## 手机系统

- 左上角 `PhoneScreen`（`Scenes/Phone/PhoneScreen.tscn`，`CanvasLayer.layer=45`）提供手机入口；打开时用 `DialogueBalloon` 加载 `Dialogue/phone.dialogue` 显示「重要的事 / 记事本 / 关闭」，重要的事列表 / 阅读 / 记事本用 `PhoneScreen` 自己的面板。
- 重要的事（玩家面向术语，原「文档」）：`DocumentData`（Id/Title/Body）+ `DocumentLibrary` 注册表放在 `Resources/Documents/`，启动时 `GameScene` 调 `GameState.SeedDocuments` 灌入；对话正文用 `[url=doc:<id>][color=#ffd866]文字[/color][/url]` 标可获取的重要的事，点击只获取、不推进对话；已获取的跨循环保留。已获取的链接由 `DialogueBalloon` 自动换成灰色 `#8a8a8a`（同句收集后立即重绘），再点只提示「已在手机里」；带高亮的选项一律加 `[#repeatable]`，漏点后可再进同一分支补收。
- 文档正文里的线索链接：`DocumentData.Body` 里 `[url=clue:<id>][color=#ffd866]文字[/color][/url]` 是可点击解锁线索的链接，`PhoneScreen.DocReadBody` 的 `MetaClicked` 处理 `clue:` 前缀 → `GameState.AddClue`，点击后由 `ApplyClueStyle` 把内层颜色换灰 `#8a8a8a`（与对话里 `[url=doc:]` 的收集是两个独立机制）。
- 记事本：`GameState.NotebookText` 单个字符串，每次 `TextEdit.TextChanged` 直接写入；跨循环保留但不落盘。
- 未读高亮：手机主菜单的「重要的事」选项文字（`phone.dialogue` 的 `[ID:open_documents]`）与「重要的事」列表里未读的文档标题，在有未读文档时标黄 `#ffd866`；在手机里点进正文即视为已读（`GameState.MarkDocumentRead`），已读集合 `_readDocumentIds` 跨循环保留。通用 balloon 只提供 `DialogueBalloon.ResponseColorResolver`（`Func<DialogueResponse, Color?>`）回调，颜色规则写在 `PhoneScreen`，不改 addon 目录。上色必须走 `ButtonTextColors.Apply`（`Scripts/UI/ButtonTextColors.cs`，同时覆盖 `font_color` / `font_hover_color` / `font_focus_color` / `font_pressed_color` / `font_hover_pressed_color`）：只写 `font_color` 时，按钮一旦聚焦（列表重建后首项 `GrabFocus`、菜单项 `mouse_entered` 即 `grab_focus`）或悬浮 / 按下就会被主题的近白色盖掉，黄色只在未聚焦时显示。
- `DialogueBalloon.KeepPlayerInputLockedOnExit`：手机会话期间保持玩家输入锁，由 `PhoneScreen` 在关闭 / 循环重置时统一释放。
- `Player.InputLocked` 从 `true` 变回 `false` 时会 `Input.ActionRelease("move_left"/"move_right")`：在记事本这类 `TextEdit` 里打字时字母键同样写进动作状态，输入法会吞掉 key-up，不清掉的话解锁后玩家会自己一直往一个方向走。
- `PhoneScreen` 的面板容器 `Center` 必须 `mouse_filter = IGNORE`，否则全屏容器会抢走面板鼠标事件。
- 术语：玩家可见文案统一写「重要的事」，不写「文档」；代码侧类名 / 节点名 / 资源 id（`DocumentData`、`DocumentLibrary`、`AcquireDocument`、`DocListPanel` 等）保持不变，避免破坏 `.tres` 的 uid 与场景唯一名引用。
- 玩手机消磨时间：主菜单 `[ID:waste_time]` → `phone.OpenTimeSkip()` → `PhoneScreen` 打开 `TimeSkipPanel`（四位数显 HH:MM，每格 ▲▼ 按本位权值 600 / 60 / 10 / 1 分钟调整；可选范围 13:00–17:30，时十位恒为 1 所以它的箭头常禁用）。目标 ≤ 当前时刻时「确定」置灰；底部「关闭」按钮与 Esc 一样退回手机主菜单，不推进时间；确认后先 `CloseSession()` 释放输入锁，再 `AdvanceTime(目标 - 当前)`，目标 17:30 照常触发终末重置。设计见 `docs/superpowers/specs/2026-11-07-phone-time-skip-design.md`。
- 钥匙 HUD：`PhoneScreen` 的 `KeyRing`（`Scripts/UI/KeyRing.cs`）在手机按钮右侧显示已持有的钥匙图标 —— `office_key`「办公室抽屉的钥匙」、`equipment_key`「器材准备室的钥匙」，鼠标悬浮时在图标下方显示名称；图标与药丸底、悬浮框都由代码绘制，不依赖图标素材。`GameState.InventoryChanged` 驱动刷新，手机打开 / 17:30 终末时随手机按钮一起隐藏，循环重置清空背包后自动消失。

## 时间系统

- `GameState.ElapsedMinutes`：循环内分钟，0 = 13:00，270 = 17:30；`AdvanceTime(int minutes)` 返回 `true` 表示本次推进触发或已处于终末，**调用方必须随即中止后续动作**（不装载场景 / 不切房间）。
- 默认扣时点：地图旅行（`MapLocationButton.TravelMinutes`，默认 30）与房间切换（`RoomDoor.TravelMinutes`，默认 5）。当前值：地图→教室 30、地图→操场 35、地图→行政楼 25；教室↔走廊 15、走廊↔隔壁班 20、操场↔操场后 10、操场后↔器材准备室 5、操场后↔地下空间 5（剧本未给，初值）、行政楼入口↔办公室 5。从地点退回地图不扣时；对话 / 调查 / 拾取不扣时。
- `TimeChanged(int elapsedMinutes)` 驱动 NPC 换帧；`NpcStateFrame.FromMinute` 是循环内分钟（0 = 13:00，45 = 13:45）。
- 17:30 发 `LoopEndReached`；`GameScene` `await LoopEndNotice.PlayAsync()`（爆炸 1/2 快速轮切 → 爆炸 3 + 震屏，约 2.0 秒）后调 `ResetLoop()`。`ResetLoop()` 把时间归零、`LoopCount++`、清背包 / 本轮 NPC 状态 / 已选选项 / 本轮标记；`UnlockedLocations` / `Clues` / 已收集的重要的事 / 记事本保留。
- 右上角 `UILayer/TimePanel/TimeLabel`（`Scripts/UI/TimeLabel.cs`）显示 `GameState.GetClockText()`，订阅 `TimeChanged` 刷新；`ResetLoop()` 也会发 `TimeChanged(0)`，所以重置后自动回到 13:00。
- `LoopCount` 只在当前进程内有效，只用于一周目固定锁；二 / 三 / 四周目是预期打法，不是系统强制的状态机，不按周目数限制地点 / 道具 / 对话 / 路线。
- 工程只保留 `move_left` / `move_right` / `interact` 三个输入；循环重置由 17:30 终末或 17:00 地下失败（`FailLoop`）触发；真结局不重置。
- 17:30 终末后到重载完成之间，当前地点 ProcessMode 置 Disabled，冻结移动与交互；从地图触发终末时没有地点可冻结，地图按钮由 _loopEnding 挡住。

## NPC 系统

- `Npc`（`Area2D`，继承 `Interactable`）：导出 `NpcId`（建议 `地点.名字`）、`DisplayName`（头顶常驻中文名，写进子节点 `NameLabel`；为空或缺 `NameLabel` 都会 `PushError` 并判为无效）、`Dialogue`、`NpcStateFrame[] Frames`、`Sprite`、`PresentLoopFlag` / `VisibleWhenFlagSet`。`NameLabel` 在 NPC 本地坐标 `offset_top=-791.5`/`offset_bottom=-719.5`（比翻倍后的头顶再高约 14px）、字号 36、暖白字 + 深墨描边 11 + 阴影。`Frames` 首帧 `FromMinute=0`（0=13:00）、严格递增；`Visible=true` 的帧必须有 `Texture`；同一 `StateId` 跨帧贴图必须一致。
- `NpcStateFrame` 另有三个可选覆盖：`SpriteScale`（为 `(0,0)` 时用 `Sprite` 节点自身缩放，非零时同时启用 `SpriteOffset`）、`SpriteOffset`（贴图内像素，同 `Sprite2D.Offset`）、`NameLabelTop`（为 `0` 时用默认标签位置）。用于坐姿 / 床铺这类与站立立绘尺寸完全不同的状态图。
- 有效状态：优先取 `GameState.GetNpcLoopState(NpcId)`（本轮覆盖，`SetLoopState` 写入），没有覆盖时取当前时间帧的 `StateId`；**在场 = 当前时间帧的 `Visible` 且 `PresentLoopFlag` 条件满足**，覆盖不改在场。
- `Npc` 可选 `PresentLoopFlag`（本轮标记 id）与 `VisibleWhenFlagSet`（默认 true）：在场 = 时间帧 `Visible` 且标记条件满足。教室新井 `PresentLoopFlag=shinji_analyzing` + `VisibleWhenFlagSet=false`，实验室新井 `=true`，两者互斥。
- `GameState.SetLoopFlag` / `HasLoopFlag` 管本轮标记；`ResetLoop` 清空标记后发 `TimeChanged(0)`，Npc 据此刷新。`LoopFlagsChanged` 供运行中即时刷新。
- 在场用 `Area2D.Monitorable` 切换，不要用 `CollisionShape2D.Disabled`（`Room.SetActive` 会覆盖它）。
- `Npc` 给 `.dialogue` 暴露 `IsPresent()` / `IsInState("...")` / `SetLoopState("...")`；状态动作必须在帧表里有对应状态。
- `GameState` 新增本轮 `NpcLoopStates` / `NpcUsedChoices`；`ResetLoop()` 清空它们，`UnlockedLocations` / `Clues` 保留；`AddClue(string)` 写入跨循环线索，`HasClue` 读取。
- `DialogueBalloon`：`Start` 置 `Player.InputLocked=true`，`_ExitTree` 复位；选项过滤顺序是 `hide_if` → 已选灰显 → `IsAllowed` / `show_if`；一次性选项必须写 `[ID:]`，`[#repeatable]` 不记录；过滤后没有任何可选选项时报错并关闭。
- `LocationRoot` 在 `_Ready` 里检查同地点 `NpcId` 重复；`NpcId` 跨地点唯一靠 `地点.名字` 命名约定。
- 当前 NPC：`classroom.yuka`（教室 13:00-13:44）、`classroom.yamada`（教室讲台 13:00-13:29）、`classroom.shinji`（隔壁班 整个下午；`shinji_analyzing` 时离场）、`classroom.shinji_lab`（化学实验室；`shinji_analyzing` 时在场）、`playground.yuka`（操场 13:45 起）、`playground.yamada`（地下空间 17:00 起）、`administration.yamada`（办公室 13:30-16:59，14:00-14:59 睡觉；「工作」帧贴 `yamada_working.png` 背身坐姿，节点 x=2564 对准办公室背景里那把空办公椅，帧覆盖 `SpriteScale=(0.5291,0.5291)` / `SpriteOffset=(4,-425.8)` / `NameLabelTop=-845.5`；「睡觉」帧贴 `yamada_sleep.png` 折叠床躺姿，`SpriteScale=(0.5,0.5)` / `SpriteOffset=(0,-487)`，名字标签沿用默认位置）。

## 一周目开场（出生在座位 + 先找小优香）

- 出生点（地点入口 / 循环开始）：`LocationRoot` 的两个导出房间决定落点 —— `StartingRoom`（从地图进入时激活，教室为 `Room_Hallway` 走廊，落 `DefaultSpawn` = `(700.638, 1008)`）与 `LoopStartRoom`（循环重置时激活，教室为 `Room_Classroom`，落 `FirstLoopSpawn` = `(1401.276, 1008)` 座位、留空回落 `DefaultSpawn`）；`LoopStartRoom` 留空时循环开始也用 `StartingRoom`。两者由 `GameScene.LoadLocationAsync(scene, loopStart)` 通过 `LocationRoot.IsLoopStart` 在挂入场景树前指定：`_Ready` 与 `OnLoopReset` 传 `true`，地图进入不传。所以从大地图回教室落在走廊，每周目循环重置后在座位上醒来。
- 房间之间的落点：每扇 `RoomDoor` 的 `TargetSpawn` 指向目标房间内与来源对应的 `Marker2D`（命名 `SpawnFrom<来源>`，如 `Room_Hallway/SpawnFromNextClassroom`、`Room_Behind/SpawnFromEquipment`），玩家从哪扇门进来、返回时就落在同一扇门旁边，不再统一回到目标房间的 `DefaultSpawn`。教室 `Room_Classroom` 左右各有一扇 `ClassroomDoor`（`DoorToHallwayLeft` x=140 / `DoorToHallwayRight` x=3710），两侧都能进走廊，且都落回 `Room_Hallway/SpawnFromClassroom`（x=549，即走廊那扇教室门）；隔壁班 `Room_NextClassroom` 同样左右各一扇 `RoomDoor`（`DoorToHallwayLeft` x=160.1458 / `DoorToHallwayRight` x=3710），都回 `Room_Hallway/SpawnFromNextClassroom`（x=1715）。
- 开场剧情：`GameScene.OpeningNarration`（`Dialogue/day1.dialogue`）在淡入后按周目播放：一周目播 `start` cue；二周目起播 `phone_vibrate` cue（「手机在震动。」），对话结束后 `PhoneScreen.OpenNotebookWithNote` 自动打开手机记事本并把「下午出去转转吧」追加到 `GameState.NotebookText`（已有这句则不重复，否则换行追加）。`GameScene._Ready` 与 `OnLoopReset` 末尾都调 `ShowLoopStartIntro`；对话与手机界面期间 `Player.InputLocked` 锁住移动与交互。
- 门禁：`TalkToYukaFirst`（`Scripts/Location/TalkToYukaFirst.cs`）挂在 `Room_Classroom` 下，加入组 `talk_to_yuka_first`；`PlayerInteractor` 在 `_current.Interact()` 之前调 `TryInterceptAny`（排在 `OfficeKickOut` 之后）。判定顺序：本房间（`_room.IsAncestorOf`）→ 目标不是 `RequiredNpcId` 本人 → `IsArmed()`（一周目且无 `yuka_briefed`）；命中就播 `day1.dialogue` 的 `talk_first` 并中止本体逻辑。
- `yuka_briefed` 由 `yuka.dialogue` 的 `choose_read` / `choose_out` 写入，所以「谈过话」= 走完开场对话并做出选择；`ResetLoop` 后 `LoopCount` 变 2，门禁自动失效、开场旁白不再播。
- 设计说明见 `docs/superpowers/specs/2026-11-06-map-scene-system-design.md` §7.8。

## 正式主流程

- 一周目：小优香开场 → 只能选看书 → 教室门被 `ClassroomDoor` 挡住 → 座位看一次书（`BookDesk` 一次播完整段 BL 小说，对话退出后直接推进到 17:30 终末）。
- 二周目：新井「关于小优香」拿 `yuka_likes_yamada` → 操场小优香拿钥匙 / 解锁行政楼 → 办公室 14:00-14:59 偷 `office_key`。
- 三周目：抽屉 + 密码面板 0721 → `yamada_notebook`（电脑日志）→ 手机里点黄色文字「我发现了操场后面那个井盖下的秘密空间」→ `underground_access` → 地下取样 → 新井分析两小时 → `bomb_weakness`。
- 四周目：器材准备室接水 → 地下湿炸药 → 湿炸药旁白结束即自动跳到 17:00 并直接进入山田终局对质 → 新井立绘登场 → 甜品店 CG + 左侧三人立绘 → 老师来后右侧立绘 → 真结局；未湿则 17:00 后与地下山田对话触发 `FailLoop`。带水桶与炸药交互会直接浇灭，不再先取样。地下空间没有单独的「服务器 / 等到傍晚」交互物。
- 本轮标记：`chose_reading` / `chose_going_out` / `yuka_briefed` / `shinji_analyzing` / `bomb_sample_taken` / `bomb_wet` / `water_taken` / `office_key_taken`；每轮清空。
- 办公室（行政楼）：门在 13:30-16:59 开放（`OfficeDoor`）；不在 14:00-14:59 时，玩家在办公室里与门 / 出口以外的任何东西交互，都会由 `OfficeKickOut` 播 `Dialogue/Objects/office_kick_out.dialogue` 并赶回入口（走 `DoorToLobby`，5 分钟）。赶人台词按时间选 cue：13:30-13:59 用 `start`，15:00-16:59 用 `woken`（山田打哈欠说「我刚刚直到15:00都在睡觉」，黄字给出 `yamada_nap`「山田老师的午睡」= 老师 14:00-15:00 在办公室睡觉）。偷 `office_key` 仍只限 14:00-14:59 山田午睡时。
- 跨轮线索：`yuka_likes_yamada` / `saw_laptop` / `laptop_password` / `chem_lab_unlocked` / `bomb_weakness` / `underground_access`。
- 9 篇重要的事的高亮语法与位置见 `docs/superpowers/specs/2026-11-07-formal-main-flow-design.md` §7。

## 显示与窗口

- 默认全屏启动：`display/window/size/mode=3`（普通全屏，不用独占全屏），基准分辨率 1920×1080；窗口化初始尺寸 1600×900（`window_width_override` / `window_height_override`）。
- 缩放用 `canvas_items` + `keep`：全屏或缩放窗口时整张画面等比缩放并居中，16:9（1920×1080 / 2560×1440 / 3840×2160）完全铺满，非 16:9 屏幕留黑边，避免地图按钮、UI 与背景错位。2560×1440 全屏下画布缩放为 1.333。
- 全屏 UI 都按锚点自适应（`anchor_right=1` / `anchor_bottom=1`），背景 TextureRect 用 `KEEP_ASPECT_COVERED`；房间背景铺满 1080 世界高度，缩放时不会露底。
- `Scripts/UI/FullscreenToggle.cs` 注册为 autoload `FullscreenToggle`：任意场景按 F11 在「全屏 ↔ 1600×900 窗口化」之间切换，窗口化时在当前屏幕可用区域居中；不需要在各场景重复挂脚本。

## 音频

- autoload `AudioManager`（`Scripts/Audio/AudioManager.cs`）：8 个 `AudioStreamPlayer` 轮转池，同组多个 ogg 随机播放，可叠加不互相打断。
- 接口：`AudioManager.Instance.PlayClick()` / `PlayHover()` / `PlaySwitch()` / `PlayMouseClick()` / `PlayMouseRelease()`；通用入口 `PlaySfx(SfxId)` / `PlaySfxStream(AudioStream)`；尾声远处的闷响用 `PlayEndingExplosion()`；BGM 用 `PlayBgm(stream, loop: true)` / `StopBgm()`。
- 音量：`SfxVolumeDb` / `BgmVolumeDb`（分贝，改对应的 `SFX` / `BGM` 总线）。
- 音效按 `Audio/Sfx/{Click,Rollover,Switch,Mouse}/<name><n>.ogg` 约定加载；增删变体时同步 `AudioManager.LoadSfxGroup` 的数量。
- 单次长音效：`Audio/Sfx/爆炸,重启.wav`（17:30 终末，`LoopEndNotice.ExplosionSfx` 在定格第 3 张爆炸时播放）、`Audio/Sfx/爆炸，结尾.wav`（甜品店尾声远处的闷响，`PlayEndingExplosion`）、`Audio/Sfx/脚步声_上楼梯_x3.wav`（跑步脚步声，`player.tscn` 的 `Footsteps` 节点，移动时反复播放、停下即停，走 `SFX` 总线）。
- BGM：`Audio/Bgm/` 下 7 首 mp3，曲目 id → 路径写在 `AudioManager.BgmCueFiles`（`cheer` 开开心心啦啦啦 / `adventure` 暑假大冒险 / `still` 静止的宇宙 / `moonlight` 浅浅的月光 / `rainy` 雨的恋人们 / `mist` 夏天的雾 / `dream` 梦，`dream` 暂未使用）；播放时统一开 `Loop`，mp3 与 ogg 都支持。
- BGM 分三层：`PlayLocationBgm(locationId)` 按地点选环境曲（`classroom`→cheer / `playground`→adventure / `administration`→still，`GameScene.LoadLocationAsync` 每次装载地点后调用，地点 id 取 `LocationRoot` 根节点名小写）；`HasStage("bomb_seen")` 为真时环境曲全局改为 `rainy`（跨循环保留）；打开手机时 `PhoneScreen.Open` 调 `PushBgmOverride("moonlight")`、`CloseSession` 调 `PopBgmOverride()` 恢复。同一曲目正在播放时不会重头播。
- 剧情阶段：`GameState.MarkStage` / `HasStage` / `StageChanged`，`_stages` 跨 `ResetLoop` 保留（与 `_loopFlags` 相反）；`Room.StageOnEnter` 在房间第一次激活时写入阶段，`Playground.tscn` 的 `Room_Underground` 配 `StageOnEnter = "bomb_seen"`。
- 终局 BGM 由 `Dialogue/Npc/yamada_underground.dialogue` 里的 `do AudioManager.PlayBgmCue(...)` 驱动：老师刚来 `mist` → 开始阐述经历 `moonlight` → 新井来了以后 `rainy` → 甜品店老师来之前 `rainy` → 老师来之后 `cheer`。
- 资源缺失、分组为空、总线缺失都会 `PushError`，不会静默兜底。

## 工具链

- 引擎：`D:\Downloads\Compressed\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64.exe`（项目 features=4.7；C# 目标框架 net8.0，本机 .NET SDK 10 + 运行时 8 构建正常）。同目录 `_console.exe` 便于在终端看输出。
- 校验命令（仓库根目录执行）：
  - 导入资源：`& $godot --headless --path . --import`
  - 编译 C#：优先 `dotnet build --nologo -v q`（快、不启动编辑器）；`--build-solutions` 会拉起 headless 编辑器，经常卡住，不要用。
  - 无头跑主场景并立刻退出：`& $godot --headless --path . --quit-after 120`（需先在项目设置指定主场景）
- `*.csproj` / `*.sln` 已存在且可编译；它们由 Godot 首次创建 C# 脚本时生成。
- `.vscode/settings.json`：已把 `.godot/`、`*.import`、`*.uid`、`*.translation`、`bin/`、`obj/`、`mono_crash.*.json` 从资源管理器与搜索中排除，并跳过 `.godot`/`bin`/`obj` 的文件监听；仅隐藏，不影响 git 与 Godot。
- godot MCP 用 **Godot MCP Toolkit v1.1.2**（编辑器内插件，不是外部 CLI）：插件在 `addons/godot_mcp_toolkit/`、已写进 `project.godot` 的 `[editor_plugins] enabled`（`res://addons/godot_mcp_toolkit/plugin.cfg`）；npm 桥 `@npgamedev/godot-mcp-server@1.1.2` 全局安装于 `C:\Users\starrysky\AppData\Roaming\npm\node_modules\`，DSH 侧注册为 `mcp-godot`（机器级 `cordis.patch.yml`，工具前缀 `mcp__godot__*`）。**编辑器开着就能用**，不需要关编辑器或另起 headless：插件在编辑器进程内监听 `127.0.0.1:6550`（6550-6560 顺延），token 落在 `user://addons/godot_mcp_toolkit/project_instance_*/mcp_token`，桥按项目注册表自动发现端口。
- 桥的 `env` 必须带 `GODOT_MCP_PROJECT_PATH` 指向项目根：否则它以 cwd（DSH 的 profile 目录）查注册表，报 `no registry entry ... code AUTH_FAILED`。
- 核心 36 个工具常驻，其余工具按组用 `discover_tools` 激活（`scene_advanced` / `tilemap` / `runtime_advanced` 等，一次别开超过 5 组）。
- 场景用 `scene_create` / `scene_create_node` / `node_set_property` / `editor_save_scene` 生成，手写 `.tscn` 容易破坏资源引用。常用闭环：`script_write` → `script_check`（离线 GDScript 诊断）→ `dotnet build`；跑游戏用 `game_start` + `debugger_get_log` / `editor_get_console` + `runtime_screenshot` + `game_stop`。
- 注意：agent 写盘会覆盖你未保存的同名脚本缓冲区；agent 重挂 / 删除你正在拖拽的节点，会让那一次拖拽变成空操作。

## 开发约定（48h jam）

- 先做能玩的最小闭环，再谈表现。**不要过度设计**：不引入 DI / 事件总线 / 通用框架 / 单元测试框架，不做为「以后扩展」预留的分层。
- 只实现用户当轮要求，不顺手重构、不主动扩需求。
- 脚本一律 C#：`.cs` 文件名与类名一致，`public partial class X : Node`。不要写 GDScript。
- 对白/分支走 Dialogue Manager 的 `.dialogue`，C# 只负责状态、场景与交互；不要把对白硬编码进脚本。
- 空引用、资源缺失要显式报错，禁止用默认值或空实现静默兜底。
- git 只读：禁止 commit / branch / checkout / push（用户明确要求）。
- 外部修改 `project.godot`（输入映射 / autoload / POT / 渲染设置等）前，先确认没有 Godot 编辑器进程打开本项目；编辑器保存时会把内存中的 `ProjectSettings` 整文件写回，覆盖外部改动。
- 这是 2D 项目，但 `project.godot` 仍是 Forward Plus + d3d12、拉伸 `canvas_items/keep`；没有明确需要不要改渲染设置。

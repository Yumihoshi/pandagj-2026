# 手机系统（文档 / 记事本）实现计划

> **面向 AI 代理的工作者：** 必需子技能：使用 superpowers:subagent-driven-development（推荐）或 superpowers:executing-plans 逐任务实现此计划。步骤使用复选框（`- [ ]`）语法来跟踪进度。

**目标：** 在左上角加手机入口，用现有对话系统显示「查看文档 / 记事本」；文档通过 NPC 对话里的高亮文字获取并可在手机里阅读，记事本可自由编辑，两者跨时间循环保留。

**架构：** `PhoneScreen`（`CanvasLayer.layer = 45`）持有手机按钮与三类面板；主菜单复用 `DialogueBalloon` 加载 `Dialogue/phone.dialogue`，选「查看文档」/「记事本」时通过 `do phone.OpenDocuments()` / `do phone.OpenNotebook()` 关闭 balloon 并打开专用面板。文档静态内容走 `DocumentData` + `DocumentLibrary`，启动时由 `GameScene` 灌入 `GameState`；对话正文里的 `[url=doc:<id>]` 点击只获取文档、不推进对话。记事本是单个 `TextEdit`，内容存 `GameState.NotebookText`。

**技术栈：** Godot 4.7.2 mono / C# / Dialogue Manager 4.2.0

**规格：** `docs/superpowers/specs/2026-11-07-phone-system-design.md`

**项目约束（覆盖通用流程）：**

- git 只读：**不执行** `git commit` / `git branch` / `git checkout` / `git push`。每个任务以编译/运行检查作为检查点。
- 不引入单元测试框架（AGENTS.md 明确禁止过度设计）；验证 = Godot headless 导入/编译/启动 + 有窗口手测。
- 脚本一律 C#；`.cs` 文件名与类名一致，`public partial class X : Node` 风格。
- 场景与资源用 Godot 编辑器 / godot MCP 生成，避免手写 `.tscn` 破坏引用；`project.godot` 的外部修改必须在 Godot 编辑器关闭时进行。
- 空引用、资源缺失必须显式 `PushError`，禁止默认值或空实现静默兜底。

---

## 执行进度

- [x] 任务 1：文档数据资源
- [x] 任务 2：GameState 文档注册与记事本
- [x] 任务 3：GameScene 灌入文档注册表
- [x] 任务 4：DialogueBalloon 文档链接、输入锁与 toast
- [x] 任务 5：手机主菜单剧本与 POT 登记
- [x] 任务 6：文档资源
- [x] 任务 7：PhoneScreen 脚本
- [x] 任务 8：PhoneScreen 场景
- [x] 任务 9：挂载到主场景
- [x] 任务 10：示例高亮链接
- [ ] 任务 11：端到端验证与文档同步（headless 已通过；有窗口手测待用户）

---

## 文件结构

**新增：**

| 文件 | 职责 |
|---|---|
| `Scripts/Documents/DocumentData.cs` | 单篇文档的 Id / Title / Body |
| `Scripts/Documents/DocumentLibrary.cs` | 文档注册表 |
| `Scripts/Phone/PhoneScreen.cs` | 手机按钮、会话状态、三类面板、输入锁 |
| `Scenes/Phone/PhoneScreen.tscn` | 手机按钮、遮罩、文档列表 / 阅读 / 记事本面板 |
| `Dialogue/phone.dialogue` | 手机主菜单剧本 |
| `Resources/Documents/DocumentLibrary.tres` | 启动注册表 |
| `Resources/Documents/club_notice.tres` | 示例文档（占位） |

**修改：**

| 文件 | 变更 |
|---|---|
| `Scripts/Core/GameState.cs` | 文档注册表 / 已获取列表 / 记事本文本；`ResetLoop` 保留它们 |
| `Scripts/Dialogue/DialogueBalloon.cs` | 文档链接获取、帧末推进判定、`KeepPlayerInputLockedOnExit`、`ShowToast` |
| `Scripts/Map/GameScene.cs` | 导出并灌入 `DocumentLibrary` |
| `Scenes/Root/GameScene.tscn` | 挂载 `PhoneScreen`、引用 `DocumentLibrary` |
| `project.godot` | POT 登记 `res://Dialogue/phone.dialogue` |
| `Dialogue/Npc/yamada.dialogue` | 示例高亮文档链接 |
| `AGENTS.md` | 手机系统段落、结构清单、POT 清单 |

---

## 任务 1：文档数据资源

**文件：**
- 创建：`Scripts/Documents/DocumentData.cs`
- 创建：`Scripts/Documents/DocumentLibrary.cs`

- [ ] **步骤 1：写 `DocumentData.cs`**

```csharp
using Godot;

/// <summary>单篇可收集文档的静态内容。</summary>
[GlobalClass]
public partial class DocumentData : Resource
{
	/// <summary>全局唯一 id；对话里的 [url=doc:&lt;id&gt;] 使用它。</summary>
	[Export] public string Id { get; set; } = "";

	/// <summary>手机文档列表里显示的名字。</summary>
	[Export] public string Title { get; set; } = "";

	/// <summary>文档正文，允许多行与 BBCode。</summary>
	[Export(PropertyHint.MultilineText)] public string Body { get; set; } = "";
}
```

- [ ] **步骤 2：写 `DocumentLibrary.cs`**

```csharp
using Godot;

/// <summary>启动时注册的全部文档；由 GameScene 灌入 GameState。</summary>
[GlobalClass]
public partial class DocumentLibrary : Resource
{
	[Export] public DocumentData[] Documents { get; set; } = System.Array.Empty<DocumentData>();
}
```

- [ ] **步骤 3：编译并注册全局类**

运行：

```powershell
$godot = "D:\Downloads\Compressed\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64.exe"
& $godot --headless --path . --build-solutions --quit
& $godot --headless --path . --import
```

预期：两条命令退出码 0，输出无 `error CS`。`--import` 会把 `DocumentData` / `DocumentLibrary` 写进 `global_script_class_cache.cfg`；新建 `[GlobalClass]` 后只跑 build 不会刷新这个缓存，创建 `.tres` 前必须先 import。若报类型未找到，检查文件名与类名是否一致。

---

## 任务 2：GameState 文档注册与记事本

**文件：**
- 修改：`Scripts/Core/GameState.cs`（字段区 `19-41`，`HasClue` / `AddClue` 之后 `103-125`，`ResetLoop` `204-218`）

- [ ] **步骤 1：在字段区加入文档与记事本状态**

在 `_npcUsedChoices` 声明之后、`[Signal]` 之前插入：

```csharp
	/// <summary>文档注册表：id → DocumentData。</summary>
	private readonly Dictionary<string, DocumentData> _documentsById = new();

	/// <summary>已获取文档，按获取顺序；跨循环保留。</summary>
	private readonly List<DocumentData> _acquiredDocuments = new();

	/// <summary>已获取文档 id，用于 O(1) 判重；跨循环保留。</summary>
	private readonly HashSet<string> _acquiredDocumentIds = new();

	/// <summary>已获取的文档，按获取顺序。</summary>
	public IReadOnlyList<DocumentData> AcquiredDocuments => _acquiredDocuments;

	/// <summary>手机记事本内容；跨循环保留，不落盘。</summary>
	public string NotebookText { get; set; } = "";
```

- [ ] **步骤 2：在 `AddClue` 之后加入文档方法**

```csharp
	/// <summary>把 DocumentLibrary 里的文档灌入注册表；重复调用只重建注册表。</summary>
	public void SeedDocuments(DocumentLibrary library)
	{
		if (library == null)
		{
			GD.PushError("GameState.SeedDocuments: DocumentLibrary 为空");
			return;
		}
		if (library.Documents == null)
		{
			GD.PushError("GameState.SeedDocuments: Documents 为空");
			return;
		}

		_documentsById.Clear();
		foreach (DocumentData document in library.Documents)
		{
			if (document == null)
			{
				GD.PushError("GameState.SeedDocuments: Documents 里有 null 项");
				continue;
			}
			if (string.IsNullOrEmpty(document.Id))
			{
				GD.PushError($"GameState.SeedDocuments: {document.ResourcePath} 的 Id 为空");
				continue;
			}
			if (!_documentsById.TryAdd(document.Id, document))
			{
				GD.PushError($"GameState.SeedDocuments: 文档 id 重复 —— {document.Id}");
				continue;
			}
			if (string.IsNullOrEmpty(document.Title))
			{
				GD.PushError($"GameState.SeedDocuments: 文档 {document.Id} 的 Title 为空");
			}
			if (string.IsNullOrEmpty(document.Body))
			{
				GD.PushError($"GameState.SeedDocuments: 文档 {document.Id} 的 Body 为空");
			}
		}
	}

	/// <summary>获取一篇文档；已获取或 id 未注册时返回 false。</summary>
	public bool AcquireDocument(string documentId)
	{
		if (string.IsNullOrEmpty(documentId))
		{
			GD.PushError("GameState.AcquireDocument: 文档 id 为空");
			return false;
		}
		if (!_documentsById.TryGetValue(documentId, out DocumentData document))
		{
			GD.PushError($"GameState.AcquireDocument: 文档 {documentId} 没有在 DocumentLibrary 里注册");
			return false;
		}
		if (!_acquiredDocumentIds.Add(documentId)) return false;

		_acquiredDocuments.Add(document);
		GD.Print($"[Doc] 获取 {documentId}");
		return true;
	}

	/// <summary>是否已经获取过某篇文档。</summary>
	public bool HasDocument(string documentId)
	{
		if (string.IsNullOrEmpty(documentId))
		{
			GD.PushError("GameState.HasDocument: 文档 id 为空");
			return false;
		}
		return _acquiredDocumentIds.Contains(documentId);
	}
```

- [ ] **步骤 3：更新 `ResetLoop` 的注释与保留范围**

把方法上方的 XML 注释改为：

```csharp
	/// <summary>循环重置：时间归零、周目 +1，清本轮状态，保留解锁、线索、文档与记事本。</summary>
```

方法体保持现有清空逻辑，**不要**清 `_documentsById` / `_acquiredDocuments` / `_acquiredDocumentIds` / `NotebookText`。

- [ ] **步骤 4：编译**

```powershell
& $godot --headless --path . --build-solutions --quit
```

预期：退出码 0，无 `error CS`。

---

## 任务 3：GameScene 灌入文档注册表

**文件：**
- 修改：`Scripts/Map/GameScene.cs`（导出区 `8-21`，`_Ready` `29-65`）

- [ ] **步骤 1：新增导出**

在 `DialogueBalloonScene` 导出之后加入：

```csharp
	/// <summary>全部可收集文档的注册表。</summary>
	[Export] public DocumentLibrary DocumentLibrary { get; set; }
```

- [ ] **步骤 2：启动校验**

在 `_Ready` 的空引用条件里追加 `|| DocumentLibrary == null`：

```csharp
		if (MapConfig == null || StartLocation == null || MapScreen == null
			|| LocationHost == null || DialogueBalloonScene == null
			|| DocumentLibrary == null)
		{
			GD.PushError("GameScene: 有导出引用为空，停止启动");
			return;
		}
```

- [ ] **步骤 3：灌入注册表**

在 `GameState.Instance.SeedInitialUnlocks(MapConfig);` 之后插入：

```csharp
		GameState.Instance.SeedDocuments(DocumentLibrary);
```

- [ ] **步骤 4：编译**

```powershell
& $godot --headless --path . --build-solutions --quit
```

预期：退出码 0，无 `error CS`。

---

## 任务 4：DialogueBalloon 文档链接、输入锁与 toast

**文件：**
- 修改：`Scripts/Dialogue/DialogueBalloon.cs`（`_Ready` `58-96`，`_ExitTree` `98-109`，`OnBalloonGuiInput` `155-221`，`CopySelectedText` `370-381`，`ShowCopyToast` `383-396`）

- [ ] **步骤 1：新增手机会话保持输入锁的属性**

在 `Npc` 属性之后加入：

```csharp
	/// <summary>为 true 时 _ExitTree 不释放玩家输入锁；由 PhoneScreen 在会话结束时统一释放。</summary>
	public bool KeepPlayerInputLockedOnExit { get; set; }
```

- [ ] **步骤 2：`_ExitTree` 按属性决定是否解锁**

把 `_ExitTree` 中解锁玩家的条件改为：

```csharp
		if (GodotObject.IsInstanceValid(player) && !KeepPlayerInputLockedOnExit)
		{
			player.InputLocked = false;
		}
```

- [ ] **步骤 3：新增链接点击状态字段**

在 `pressPosition` 之后加入：

```csharp
	private Vector2 pressGlobalPosition;
	private ulong lastMetaClickFrame = ulong.MaxValue;
```

- [ ] **步骤 4：订阅 `MetaClicked`**

在 `_Ready` 的 `dialogueLabel.SelectionEnabled = false;` 之后加入：

```csharp
		dialogueLabel.MetaClicked += OnDialogueMetaClicked;
```

在 `_ExitTree` 的 `DialogueManager.Mutated -= OnMutated;` 之后加入：

```csharp
		if (GodotObject.IsInstanceValid(dialogueLabel))
		{
			dialogueLabel.MetaClicked -= OnDialogueMetaClicked;
		}
```

- [ ] **步骤 5：按下时记录全局坐标**

在 `OnBalloonGuiInput` 的按下分支里，`pressPosition = mouseButton.Position;` 之后加入：

```csharp
				pressGlobalPosition = mouseButton.GlobalPosition;
```

- [ ] **步骤 6：松开推进改为帧末提交**

把松开分支里的 `Next(dialogueLine.NextId);` 替换为：

```csharp
				string nextId = dialogueLine.NextId;
				Callable.From(() => AdvanceAfterMetaClick(nextId)).CallDeferred();
```

- [ ] **步骤 7：新增链接处理与帧末推进方法**

在 `OnBalloonGuiInput` 之后加入：

```csharp
	private void OnDialogueMetaClicked(Variant meta)
	{
		lastMetaClickFrame = Engine.GetProcessFrames();

		// 拖拽选择结束时松开在链接上：算复制，不算点击获取。
		if (pressGlobalPosition.DistanceTo(GetViewport().GetMousePosition()) > ClickDragThreshold)
		{
			return;
		}

		string url = meta.AsString();
		if (!url.StartsWith("doc:"))
		{
			GD.PushError($"DialogueBalloon: 不支持的高亮链接 {url}");
			return;
		}

		string documentId = url["doc:".Length..];
		if (string.IsNullOrEmpty(documentId))
		{
			GD.PushError("DialogueBalloon: 文档链接的 id 为空");
			return;
		}
		if (gameState == null)
		{
			GD.PushError("DialogueBalloon: GameState 未就绪，无法记录文档");
			return;
		}

		bool added = gameState.AcquireDocument(documentId);
		if (added)
		{
			ShowToast("已记录到手机");
			return;
		}

		// 未注册的 id 已由 AcquireDocument 报错；只有重复获取才提示已在手机里。
		if (gameState.HasDocument(documentId))
		{
			ShowToast("已在手机里");
		}
	}

	private void AdvanceAfterMetaClick(string nextId)
	{
		if (!GodotObject.IsInstanceValid(this)) return;
		// 同一帧内发生过 meta 点击：这次松开是点链接，不是推进。
		if (lastMetaClickFrame == Engine.GetProcessFrames()) return;
		Next(nextId);
	}
```

- [ ] **步骤 8：把 `ShowCopyToast` 泛化为 `ShowToast`**

把 `ShowCopyToast` 改名并加参数：

```csharp
	private void ShowToast(string text)
	{
		if (copyToast == null) return;   // 缺失已在 _Ready 报错

		copyToast.Text = text;
		copyToastTween?.Kill();
		copyToast.Visible = true;
		copyToast.Modulate = new Color(1, 1, 1, 0);

		copyToastTween = CreateTween();
		copyToastTween.TweenProperty(copyToast, "modulate:a", 1.0f, 0.1f);
		copyToastTween.TweenInterval(0.35f);
		copyToastTween.TweenProperty(copyToast, "modulate:a", 0.0f, 0.25f);
		copyToastTween.TweenCallback(Callable.From(() => copyToast.Visible = false));
	}
```

把 `CopySelectedText` 里的 `ShowCopyToast();` 改成 `ShowToast("已复制");`。

- [ ] **步骤 9：编译**

```powershell
& $godot --headless --path . --build-solutions --quit
```

预期：退出码 0，无 `error CS`。若报 `MetaClicked` 不存在，确认使用的是 `DialogueLabel`（`RichTextLabel`）的 C# 信号 `MetaClicked`。

---

## 任务 5：手机主菜单剧本与 POT 登记

**文件：**
- 创建：`Dialogue/phone.dialogue`
- 修改：`project.godot`（`[internationalization]` 段，`64` 行）

- [ ] **步骤 1：关闭 Godot 编辑器**

运行：

```powershell
Get-Process | Where-Object { $_.ProcessName -like "*Godot*" } | Select-Object ProcessName, Id
```

预期：无输出。有输出时停止，请用户关闭编辑器后再继续（编辑器保存会整文件覆盖 `project.godot`）。

- [ ] **步骤 2：写 `Dialogue/phone.dialogue`**

```
~ start
手机: 要看什么？
- 查看文档 [ID:open_documents] [#repeatable] => documents
- 记事本 [ID:open_notes] [#repeatable] => notes
- 关闭 [#repeatable] => END

~ documents
do phone.OpenDocuments()
=> END

~ notes
do phone.OpenNotebook()
=> END
```

- [ ] **步骤 3：登记 POT**

把 `project.godot` 的 `locale/translations_pot_files` 改为：

```ini
locale/translations_pot_files=PackedStringArray("res://Dialogue/day1.dialogue", "res://Dialogue/Npc/student.dialogue", "res://Dialogue/Npc/yuka.dialogue", "res://Dialogue/Npc/yamada.dialogue", "res://Dialogue/Npc/shinji.dialogue", "res://Dialogue/phone.dialogue")
```

- [ ] **步骤 4：导入并编译**

```powershell
& $godot --headless --path . --import
& $godot --headless --path . --build-solutions --quit
```

预期：导入退出码 0；无 `ERROR` / `SCRIPT ERROR`；无 Dialogue Manager 编译错误。若 `phone.dialogue` 的 `do`-only cue 报解析错误，在 `~ documents` / `~ notes` 下各加一行短提示（例如 `手机: ……`），再导入验证；不要改用其他触发机制。

---

## 任务 6：文档资源

**文件：**
- 创建：`Resources/Documents/DocumentLibrary.tres`
- 创建：`Resources/Documents/club_notice.tres`

- [ ] **步骤 1：写示例文档 `club_notice.tres`**

godot MCP 没有资源创建工具；这两个 `.tres` 结构简单，直接写文本并用 `--import` / 无头运行验证。脚本 UID 以 `Scripts/Documents/*.cs.uid` 为准。

```ini
[gd_resource type="Resource" script_class="DocumentData" format=3]

[ext_resource type="Script" uid="uid://ciqtvja815o4p" path="res://Scripts/Documents/DocumentData.cs" id="1_doc"]

[resource]
script = ExtResource("1_doc")
Id = "club_notice"
Title = "社团活动申请表"
Body = "社团活动申请表\n\n申请人：文学部 二年级\n活动内容：周五放学后，图书馆二层。\n\n备注：申请理由一栏被涂黑，看不清。"
```

正文是占位，可由策划替换；`Body` 里的 `\n` 是 `.tres` 字符串的换行转义。

- [ ] **步骤 2：写注册表 `DocumentLibrary.tres`**

```ini
[gd_resource type="Resource" script_class="DocumentLibrary" format=3]

[ext_resource type="Script" uid="uid://c5d41r5hcdx4" path="res://Scripts/Documents/DocumentLibrary.cs" id="1_lib"]
[ext_resource type="Resource" path="res://Resources/Documents/club_notice.tres" id="2_doc"]

[resource]
script = ExtResource("1_lib")
Documents = [ExtResource("2_doc")]
```

`Documents = [ExtResource("2_doc")]` 与项目中 `Frames = [SubResource(...)]` 的数组写法一致；引擎会按 `DocumentData[]` 做类型转换。

- [ ] **步骤 3：导入**

```powershell
& $godot --headless --path . --import
& $godot --headless --path . --build-solutions --quit
```

预期：两条命令退出码 0；无 `ERROR` / `SCRIPT ERROR`。此时 `GameScene.tscn` 还没挂 `DocumentLibrary`（任务 9 才挂），不要用主场景无头运行来判断资源；资源解析在任务 9 挂载后由 `SeedDocuments` 验证。若报脚本类型找不到，先完成任务 1 的 `--import` 再重新导入。

---

## 任务 7：PhoneScreen 脚本

**文件：**
- 创建：`Scripts/Phone/PhoneScreen.cs`

- [ ] **步骤 1：写 `PhoneScreen.cs`**

```csharp
using Godot;
using System.Collections.Generic;

/// <summary>手机：左上角入口、主菜单 balloon、文档列表 / 阅读 / 记事本面板。</summary>
public partial class PhoneScreen : CanvasLayer
{
	/// <summary>本游戏的对话 balloon 场景，用于手机主菜单。</summary>
	[Export] public PackedScene DialogueBalloonScene { get; set; }

	/// <summary>手机主菜单剧本。</summary>
	[Export] public Resource PhoneDialogue { get; set; }

	/// <summary>区分拖拽选择与单击的位移阈值（像素）。</summary>
	private const float DocDragThreshold = 6.0f;

	private enum PhonePage
	{
		Documents,
		Notebook
	}

	private Button PhoneButton;
	private Control Overlay;
	private Control DocListPanel;
	private ScrollContainer DocScroll;
	private VBoxContainer DocList;
	private Label DocEmpty;
	private Button DocListBack;
	private Control DocReadPanel;
	private Label DocReadTitle;
	private ScrollContainer DocReadScroll;
	private RichTextLabel DocReadBody;
	private Label DocCopyToast;
	private Button DocReadBack;
	private Control NotebookPanel;
	private TextEdit NotebookEdit;
	private Button NotebookBack;

	private bool _open;
	private bool _loopEnded;
	private bool _closing;
	private PhonePage? _pendingPage;
	private DialogueBalloon _menuBalloon;
	private Player _player;
	private bool _docPointerPressed;
	private Vector2 _docPressPosition;
	private Tween _docCopyTween;

	public override void _Ready()
	{
		PhoneButton = GetNode<Button>("%PhoneButton");
		Overlay = GetNode<Control>("%Overlay");
		DocListPanel = GetNode<Control>("%DocListPanel");
		DocScroll = GetNode<ScrollContainer>("%DocScroll");
		DocList = GetNode<VBoxContainer>("%DocList");
		DocEmpty = GetNode<Label>("%DocEmpty");
		DocListBack = GetNode<Button>("%DocListBack");
		DocReadPanel = GetNode<Control>("%DocReadPanel");
		DocReadTitle = GetNode<Label>("%DocReadTitle");
		DocReadScroll = GetNode<ScrollContainer>("%DocReadScroll");
		DocReadBody = GetNode<RichTextLabel>("%DocReadBody");
		DocCopyToast = GetNode<Label>("%DocCopyToast");
		DocReadBack = GetNode<Button>("%DocReadBack");
		NotebookPanel = GetNode<Control>("%NotebookPanel");
		NotebookEdit = GetNode<TextEdit>("%NotebookEdit");
		NotebookBack = GetNode<Button>("%NotebookBack");

		if (DialogueBalloonScene == null)
		{
			GD.PushError("PhoneScreen: DialogueBalloonScene 为空");
		}
		if (PhoneDialogue == null)
		{
			GD.PushError("PhoneScreen: PhoneDialogue 为空");
		}
		if (GameState.Instance == null)
		{
			GD.PushError("PhoneScreen: GameState 未就绪");
		}
		else
		{
			GameState.Instance.LoopEndReached += OnLoopEndReached;
			GameState.Instance.LoopReset += OnLoopReset;
		}

		PhoneButton.Pressed += Open;
		DocListBack.Pressed += BackToMainMenu;
		DocReadBack.Pressed += ShowDocumentList;
		NotebookBack.Pressed += BackToMainMenu;
		NotebookEdit.TextChanged += OnNotebookTextChanged;
		DocReadBody.GuiInput += OnDocReadGuiInput;

		Overlay.Hide();
		DocListPanel.Hide();
		DocReadPanel.Hide();
		NotebookPanel.Hide();
		DocReadBody.SelectionEnabled = false;
		DocCopyToast.Visible = false;
	}

	public override void _ExitTree()
	{
		if (GodotObject.IsInstanceValid(GameState.Instance))
		{
			GameState.Instance.LoopEndReached -= OnLoopEndReached;
			GameState.Instance.LoopReset -= OnLoopReset;
		}

		if (GodotObject.IsInstanceValid(_menuBalloon))
		{
			_menuBalloon.TreeExited -= OnMenuBalloonExited;
			_menuBalloon.QueueFree();
		}

		if (GodotObject.IsInstanceValid(_player))
		{
			_player.InputLocked = false;
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!_open || !@event.IsActionPressed("ui_cancel")) return;

		if (DocReadPanel.Visible)
		{
			GetViewport().SetInputAsHandled();
			ShowDocumentList();
			return;
		}
		if (DocListPanel.Visible || NotebookPanel.Visible)
		{
			GetViewport().SetInputAsHandled();
			BackToMainMenu();
		}
	}

	/// <summary>由 phone.dialogue 的 do 调用：关闭主菜单后打开文档列表。</summary>
	public void OpenDocuments()
	{
		if (!_open)
		{
			GD.PushError("PhoneScreen.OpenDocuments: 手机未打开");
			return;
		}
		if (_closing) return;
		_pendingPage = PhonePage.Documents;
	}

	/// <summary>由 phone.dialogue 的 do 调用：关闭主菜单后打开记事本。</summary>
	public void OpenNotebook()
	{
		if (!_open)
		{
			GD.PushError("PhoneScreen.OpenNotebook: 手机未打开");
			return;
		}
		if (_closing) return;
		_pendingPage = PhonePage.Notebook;
	}

	private void Open()
	{
		if (_open) return;
		if (_loopEnded) return;
		if (GameState.Instance == null)
		{
			GD.PushError("PhoneScreen.Open: GameState 未就绪");
			return;
		}
		if (GameState.Instance.IsLoopEnded) return;

		_player = GetTree().GetFirstNodeInGroup("player") as Player;
		if (_player == null)
		{
			GD.PushError("PhoneScreen.Open: 找不到 player");
			return;
		}

		_open = true;
		_player.InputLocked = true;
		PhoneButton.Hide();
		GD.Print("[Phone] 打开");
		ShowMainMenu();
	}

	private void ShowMainMenu()
	{
		if (DialogueBalloonScene == null || PhoneDialogue == null)
		{
			GD.PushError("PhoneScreen.ShowMainMenu: DialogueBalloonScene 或 PhoneDialogue 为空");
			CloseSession();
			return;
		}

		HideAllPages();

		if (GodotObject.IsInstanceValid(_menuBalloon))
		{
			_menuBalloon.TreeExited -= OnMenuBalloonExited;
			_menuBalloon.QueueFree();
		}
		_menuBalloon = null;

		DialogueBalloon balloon = DialogueBalloonScene.Instantiate<DialogueBalloon>();
		if (balloon == null)
		{
			GD.PushError("PhoneScreen.ShowMainMenu: 默认 balloon 场景根节点不是 DialogueBalloon");
			CloseSession();
			return;
		}

		balloon.KeepPlayerInputLockedOnExit = true;
		balloon.TreeExited += OnMenuBalloonExited;
		AddChild(balloon);
		_menuBalloon = balloon;

		var states = new Godot.Collections.Array<Variant>
		{
			new Godot.Collections.Dictionary { { "phone", this } }
		};
		balloon.Start(PhoneDialogue, "start", states);
	}

	private void OnMenuBalloonExited()
	{
		if (_closing || !_open) return;
		_menuBalloon = null;

		PhonePage? page = _pendingPage;
		_pendingPage = null;

		switch (page)
		{
			case PhonePage.Documents:
				ShowDocumentList();
				break;
			case PhonePage.Notebook:
				ShowNotebook();
				break;
			default:
				CloseSession();
				break;
		}
	}

	private void BackToMainMenu()
	{
		if (!_open) return;
		HideAllPages();
		ShowMainMenu();
	}

	private void CloseSession()
	{
		if (_closing) return;
		_closing = true;
		_pendingPage = null;
		GD.Print("[Phone] 关闭");

		if (GodotObject.IsInstanceValid(_menuBalloon))
		{
			_menuBalloon.TreeExited -= OnMenuBalloonExited;
			_menuBalloon.QueueFree();
		}
		_menuBalloon = null;

		HideAllPages();
		PhoneButton.Show();
		_open = false;

		if (GodotObject.IsInstanceValid(_player))
		{
			_player.InputLocked = false;
		}
		_closing = false;
	}

	private void HideAllPages()
	{
		Overlay.Hide();
		DocListPanel.Hide();
		DocReadPanel.Hide();
		NotebookPanel.Hide();
		DocReadBody.SelectionEnabled = false;
	}

	private void ShowDocumentList()
	{
		if (GameState.Instance == null)
		{
			GD.PushError("PhoneScreen.ShowDocumentList: GameState 未就绪");
			CloseSession();
			return;
		}

		HideAllPages();
		Overlay.Show();
		DocListPanel.Show();
		GD.Print("[Phone] 文档列表");
		RebuildDocumentList();

		if (DocList.GetChildCount() > 0 && DocList.GetChild(0) is Button first)
		{
			first.GrabFocus();
		}
		else
		{
			DocListBack.GrabFocus();
		}
	}

	private void RebuildDocumentList()
	{
		foreach (Node child in DocList.GetChildren())
		{
			DocList.RemoveChild(child);
			child.QueueFree();
		}

		IReadOnlyList<DocumentData> documents = GameState.Instance.AcquiredDocuments;
		DocEmpty.Visible = documents.Count == 0;
		DocScroll.Visible = documents.Count > 0;

		foreach (DocumentData document in documents)
		{
			var button = new Button
			{
				Text = document.Title,
				CustomMinimumSize = new Vector2(0f, 48f)
			};
			button.Pressed += () => ShowDocumentRead(document);
			DocList.AddChild(button);
		}

		DocScroll.ScrollVertical = 0;
	}

	private void ShowDocumentRead(DocumentData document)
	{
		if (document == null)
		{
			GD.PushError("PhoneScreen.ShowDocumentRead: 文档为空");
			return;
		}

		HideAllPages();
		Overlay.Show();
		DocReadPanel.Show();
		GD.Print($"[Phone] 阅读文档 {document.Id}");
		DocReadTitle.Text = document.Title;
		DocReadBody.Text = document.Body;
		DocReadBody.SelectionEnabled = true;
		DocReadBody.GrabFocus();
		Callable.From(() => DocReadScroll.ScrollVertical = 0).CallDeferred();
	}

	private void ShowNotebook()
	{
		if (GameState.Instance == null)
		{
			GD.PushError("PhoneScreen.ShowNotebook: GameState 未就绪");
			CloseSession();
			return;
		}

		HideAllPages();
		Overlay.Show();
		NotebookPanel.Show();
		GD.Print("[Phone] 记事本");
		NotebookEdit.Text = GameState.Instance.NotebookText;
		NotebookEdit.GrabFocus();
		NotebookEdit.SetCaretLine(NotebookEdit.GetLineCount() - 1);
		NotebookEdit.SetCaretColumn(NotebookEdit.GetLine(NotebookEdit.GetLineCount() - 1).Length);
	}

	private void OnNotebookTextChanged()
	{
		if (GameState.Instance == null) return;
		GameState.Instance.NotebookText = NotebookEdit.Text;
	}

	private void OnDocReadGuiInput(InputEvent @event)
	{
		if (@event is not InputEventMouseButton mouse || mouse.ButtonIndex != MouseButton.Left)
		{
			return;
		}

		if (mouse.Pressed)
		{
			_docPointerPressed = true;
			_docPressPosition = mouse.Position;
			return;
		}

		if (!_docPointerPressed) return;
		_docPointerPressed = false;

		if (mouse.Position.DistanceTo(_docPressPosition) <= DocDragThreshold) return;

		string selected = DocReadBody.GetSelectedText();
		if (string.IsNullOrWhiteSpace(selected)) return;

		if (!DisplayServer.HasFeature(DisplayServer.Feature.Clipboard))
		{
			GD.PushError("PhoneScreen: 当前平台没有剪贴板，无法复制文档");
			return;
		}

		DisplayServer.ClipboardSet(selected);
		ShowDocCopyToast();
		GD.Print($"[Phone] 已复制文档 {selected.Length} 字");
	}

	private void ShowDocCopyToast()
	{
		_docCopyTween?.Kill();
		DocCopyToast.Visible = true;
		DocCopyToast.Modulate = new Color(1, 1, 1, 0);

		_docCopyTween = CreateTween();
		_docCopyTween.TweenProperty(DocCopyToast, "modulate:a", 1.0f, 0.1f);
		_docCopyTween.TweenInterval(0.35f);
		_docCopyTween.TweenProperty(DocCopyToast, "modulate:a", 0.0f, 0.25f);
		_docCopyTween.TweenCallback(Callable.From(() => DocCopyToast.Visible = false));
	}

	private void OnLoopEndReached()
	{
		_loopEnded = true;
		if (_open) CloseSession();
		PhoneButton.Hide();
	}

	private void OnLoopReset()
	{
		_loopEnded = false;
		if (_open) CloseSession();
		PhoneButton.Show();
	}
}
```

- [ ] **步骤 2：编译**

```powershell
& $godot --headless --path . --build-solutions --quit
```

预期：退出码 0，无 `error CS`。常见问题：
- `DialogueBalloonScene.Instantiate<DialogueBalloon>()` 返回 `null`：确认 balloon 场景根节点挂了 `DialogueBalloon` 脚本。

---

## 任务 8：PhoneScreen 场景

**文件：**
- 创建：`Scenes/Phone/PhoneScreen.tscn`（用 Godot 编辑器 / godot MCP 创建，不手写）

- [ ] **步骤 1：创建根节点**

用 godot MCP 的 `create_scene`（或编辑器）创建 `CanvasLayer`，保存为 `res://Scenes/Phone/PhoneScreen.tscn`，设置：

| 属性 | 值 |
|---|---|
| `name` | `PhoneScreen` |
| `layer` | `45` |
| `script` | `res://Scripts/Phone/PhoneScreen.cs` |
| `DialogueBalloonScene` | `res://Scenes/Dialogue/DialogueBalloon.tscn` |
| `PhoneDialogue` | `res://Dialogue/phone.dialogue` |

- [ ] **步骤 2：加手机按钮**

在根下加 `Button`：

| 属性 | 值 |
|---|---|
| `name` | `PhoneButton` |
| `unique_name_in_owner` | `true` |
| `offset_left` | `24` |
| `offset_top` | `24` |
| `offset_right` | `144` |
| `offset_bottom` | `80` |
| `text` | `手机` |
| `focus_mode` | `None`（0） |
| `theme_override_font_sizes/font_size` | `24` |

样式（`StyleBoxFlat` 子资源，三态共用或分别设）：
- `bg_color = Color(0.0470588, 0.0470588, 0.0705882, 0.62)`
- `border_width_* = 1`
- `border_color = Color(1, 1, 1, 0.16)`
- `corner_radius_* = 10`
- `content_margin_left/right = 18`，`content_margin_top/bottom = 8`

- [ ] **步骤 3：加遮罩**

在根下加 `Control`：

| 属性 | 值 |
|---|---|
| `name` | `Overlay` |
| `unique_name_in_owner` | `true` |
| `layout_mode` | `3`（Full Rect） |
| `anchors_preset` | `15` |
| `anchor_right` / `anchor_bottom` | `1.0` |
| `grow_horizontal` / `grow_vertical` | `2` |
| `mouse_filter` | `STOP`（0） |
| `visible` | `false` |

在 `Overlay` 下加 `ColorRect`：

| 属性 | 值 |
|---|---|
| `name` | `Dim` |
| `layout_mode` | `1` |
| `anchors_preset` | `15` |
| `anchor_right` / `anchor_bottom` | `1.0` |
| `color` | `Color(0, 0, 0, 0.45)` |
| `mouse_filter` | `IGNORE`（2） |

在 `Overlay` 下加 `CenterContainer`：

| 属性 | 值 |
|---|---|
| `name` | `Center` |
| `layout_mode` | `1` |
| `anchors_preset` | `15` |
| `anchor_right` / `anchor_bottom` | `1.0` |
| `mouse_filter` | `IGNORE`（2） |

**`CenterContainer` 必须是 `IGNORE`**，否则全屏容器会抢走面板的鼠标事件（与 `DialogueBalloon.tscn` 的 `CenterContainer` 同一个坑）。

- [ ] **步骤 4：加文档列表面板**

在 `Overlay/Center` 下加 `PanelContainer`：

| 属性 | 值 |
|---|---|
| `name` | `DocListPanel` |
| `unique_name_in_owner` | `true` |
| `custom_minimum_size` | `Vector2(1000, 760)` |
| `visible` | `false` |

结构：

```
DocListPanel (PanelContainer)
└── MarginContainer
    └── VBox (VBoxContainer)
        ├── Header (Label)             text = "文档"，font_size = 32
        ├── DocScroll (ScrollContainer) unique_name，size_flags_vertical = 3
        │   └── DocList (VBoxContainer) unique_name，size_flags_horizontal = 3，separation = 8
        ├── DocEmpty (Label)            unique_name，text = "还没有记录任何文档。\n在对话里点击高亮文字即可记录。"，autowrap_mode = 3，horizontal_alignment = 1，size_flags_vertical = 3
        └── DocListBack (Button)        unique_name，text = "返回"，custom_minimum_size = Vector2(0, 48)
```

- `MarginContainer`：`margin_left/right = 30`，`margin_top/bottom = 15`。
- `VBox`：`separation = 16`。
- `DocScroll` 与 `DocEmpty` 互斥显示：`DocEmpty` 初始 `visible = false`，由脚本控制。

- [ ] **步骤 5：加文档阅读面板**

在 `Overlay/Center` 下加 `PanelContainer`：

| 属性 | 值 |
|---|---|
| `name` | `DocReadPanel` |
| `unique_name_in_owner` | `true` |
| `custom_minimum_size` | `Vector2(1100, 800)` |
| `visible` | `false` |

结构：

```
DocReadPanel (PanelContainer)
└── MarginContainer
    └── VBox (VBoxContainer)
        ├── DocReadTitle (Label)             unique_name，font_size = 32
        ├── DocReadScroll (ScrollContainer)  unique_name，size_flags_vertical = 3
        │   └── DocReadBody (RichTextLabel)  unique_name，bbcode_enabled = true，fit_content = true，scroll_active = false，autowrap_mode = 3
        ├── DocCopyToast (Label)             unique_name，visible = false，modulate = Color(1,1,1,0)，text = "已复制"，horizontal_alignment = 2，font_size = 16，mouse_filter = 2
        └── DocReadBack (Button)             unique_name，text = "返回列表"，custom_minimum_size = Vector2(0, 48)
```

- `MarginContainer`：`margin_left/right = 30`，`margin_top/bottom = 15`。
- `VBox`：`separation = 16`。
- `DocReadBody` 的 `selection_color` 由 `PhoneScreen.tscn` 的 Theme 设 `Color(1, 1, 1, 0.35)`。

- [ ] **步骤 6：加记事本面板**

在 `Overlay/Center` 下加 `PanelContainer`：

| 属性 | 值 |
|---|---|
| `name` | `NotebookPanel` |
| `unique_name_in_owner` | `true` |
| `custom_minimum_size` | `Vector2(1000, 760)` |
| `visible` | `false` |

结构：

```
NotebookPanel (PanelContainer)
└── MarginContainer
    └── VBox (VBoxContainer)
        ├── Header (Label)             text = "记事本"，font_size = 32
        ├── NotebookEdit (TextEdit)    unique_name，custom_minimum_size = Vector2(900, 560)，wrap_mode = 1
        └── NotebookBack (Button)      unique_name，text = "返回"，custom_minimum_size = Vector2(0, 48)
```

`NotebookEdit` 额外设置：

| 属性 | 值 |
|---|---|
| `placeholder_text` | `在这里整理线索、粘贴对话……` |
| `theme_override_font_sizes/font_size` | `20` |
| `theme_override_colors/font_color` | `Color(1, 1, 1, 0.9)` |
| `theme_override_colors/caret_color` | `Color(1, 1, 1, 1)` |
| `theme_override_colors/selection_color` | `Color(1, 1, 1, 0.35)` |

- [ ] **步骤 7：加主题**

给 `Overlay` 加 `Theme` 子资源（或把主题挂在 `PhoneScreen` 根并对 `Overlay` 覆盖），设置：

| 项 | 值 |
|---|---|
| `default_font_size` | `20` |
| `PanelContainer/styles/panel` | `StyleBoxFlat`：`bg_color = Color(0,0,0,1)`，`border_width_* = 3`，`border_color = Color(1,1,1,1)`，`corner_radius_* = 5`，`content_margin_* = 0` |
| `Button/styles/normal` | `StyleBoxFlat`：`bg_color = Color(0.121569,0.121569,0.121569,1)`，`border_width_* = 3`，`border_color = Color(0.6,0.6,0.6,1)`，`corner_radius_* = 5`，`content_margin_* = 20/10` |
| `Button/styles/hover` | 同上，`border_color = Color(1,1,1,1)` |
| `Button/styles/focus` | 同上，`border_color = Color(1,1,1,1)` |
| `Button/styles/disabled` | 同上，`bg_color = Color(0,0,0,1)`，`border_color = Color(0.329412,0.329412,0.329412,1)` |
| `RichTextLabel/colors/selection_color` | `Color(1, 1, 1, 0.35)` |
| `TextEdit/colors/selection_color` | `Color(1, 1, 1, 0.35)` |

- [ ] **步骤 8：保存并验证场景可加载**

用 `save_scene` / 编辑器保存。运行：

```powershell
& $godot --headless --path . --build-solutions --quit
& $godot --headless --path . --quit-after 120
```

预期：编译退出码 0；无头运行无 `ERROR` / `SCRIPT ERROR`。此时 `PhoneScreen` 还没挂到主场景，所以只验证脚本与场景资源能加载；缺节点问题要到任务 9 挂载后才暴露。

---

## 任务 9：挂载到主场景

**文件：**
- 修改：`Scenes/Root/GameScene.tscn`（根节点 `26-32`，`UILayer` `36`）
- 修改：`Scripts/Map/GameScene.cs`（任务 3 已加导出）

- [ ] **步骤 1：加 `PhoneScreen` 实例**

用编辑器 / godot MCP 在 `GameScene` 根下（与 `LocationHost`、`UILayer` 同级）实例化 `res://Scenes/Phone/PhoneScreen.tscn`，节点名 `PhoneScreen`。不要改它的 `layer`（场景里已是 45）。

- [ ] **步骤 2：设置 `DocumentLibrary`**

选中 `GameScene` 根，把 `DocumentLibrary` 指到 `res://Resources/Documents/DocumentLibrary.tres`。

- [ ] **步骤 3：保存并运行**

```powershell
& $godot --headless --path . --build-solutions --quit
& $godot --headless --path . --quit-after 120
```

预期：退出码 0；无 `ERROR` / `SCRIPT ERROR`。若报 `PhoneScreen: ... 找不到 ...` 或 `GetNode` 失败，回到任务 8 检查节点名与 `unique_name_in_owner`。

---

## 任务 10：示例高亮链接

**文件：**
- 修改：`Dialogue/Npc/yamada.dialogue`（`~ ask_shinji`，`17-19`）

- [ ] **步骤 1：加示例文档链接**

把 `~ ask_shinji` 改为：

```
~ ask_shinji
山田老师: 新井吗？他整个下午都在隔壁教室自习。
山田老师: 对了，这份[url=doc:club_notice][color=#ffd866]社团活动申请表[/color][/url]你拿去看看吧。
=> END
```

- [ ] **步骤 2：导入**

```powershell
& $godot --headless --path . --import
```

预期：退出码 0；无 Dialogue Manager 编译错误。若 `[url]` / `[color]` 报错，确认 Dialogue Manager 的 BBCode 透传（`resolved_line_data.gd` 会保留未知 BBCode）；不要把方括号写成全角。

- [ ] **步骤 3：手测获取**

有窗口运行游戏 → 进教室 → 和山田老师对话 → 点「关于新井次郎」→ 在第二句点黄色「社团活动申请表」。预期：

- 控制台出现 `[Doc] 获取 club_notice`；
- 对话**不推进**，仍停在原句；
- 对话框右上缘出现「已记录到手机」；
- 再点一次 →「已在手机里」。

---

## 任务 11：端到端验证与文档同步

**文件：**
- 修改：`AGENTS.md`

- [ ] **步骤 1：headless 全链路**

```powershell
$godot = "D:\Downloads\Compressed\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64.exe"
& $godot --headless --path . --import
& $godot --headless --path . --build-solutions --quit
& $godot --headless --path . --quit-after 120
```

预期：三条命令退出码均为 0；日志无 `ERROR` / `SCRIPT ERROR`；无 `PushError` 输出。

- [ ] **步骤 2：有窗口手测清单**

- [ ] 地点内左上角可见「手机」按钮；地图界面也可见
- [ ] 点按钮 → 底部 balloon，角色名「手机」，选项「查看文档 / 记事本 / 关闭」
- [ ] 打开手机后时钟不变（不推进时间）
- [ ] 打开手机时左上角按钮隐藏；关闭 / 循环重置后重新出现
- [ ] 主菜单按 Esc 不关闭手机（只跳过打字）
- [ ] NPC 对话期间点手机按钮位置 → 被 balloon 挡住，不打开
- [ ] 「关闭」→ 手机消失，玩家可移动
- [ ] 无文档时「查看文档」→ 空状态文案，不报错
- [ ] 对话里点黄色高亮 → 「已记录到手机」，对话不推进
- [ ] 重复点同一高亮 → 「已在手机里」
- [ ] 拖拽选择对话正文 → 只复制，不获取文档、不推进
- [ ] 点普通正文 → 正常推进
- [ ] 手机 → 查看文档 → 列表按获取顺序显示标题
- [ ] 点文档 → 标题与正文；长正文可滚动
- [ ] 文档正文拖拽选择 → 松开自动复制；粘贴到记事本一致
- [ ] 「返回列表」→ 列表；列表「返回」→ 主菜单
- [ ] 记事本输入 / 粘贴 → 返回再打开，内容仍在
- [ ] 记事本里按 A/D → 输入文字，玩家不移动
- [ ] 记录文档 + 记事本输入 → 触发 17:30 终末 / `ResetLoop` → 手机自动关闭、输入解锁
- [ ] 重置后再开手机 → 文档与记事本内容都在
- [ ] 面板页按 Esc → 返回上一级
- [ ] 手机打开时点面板外 → 被遮罩挡住，不操作世界
- [ ] 关闭手机后 A/D / F 恢复
- [ ] 不同窗口尺寸下按钮与面板位置合理

> 17:30 终末需要等时间推进，或用 Godot 远程调试调用 `GameState.ResetLoop()`；**不要把调试入口留在工程里**。

- [ ] **步骤 3：同步 `AGENTS.md`**

在「## 当前结构」列表中加入：

```markdown
- `Scenes/Phone/PhoneScreen.tscn`：手机（`CanvasLayer.layer=45`）：左上角「手机」按钮、遮罩、文档列表 / 阅读 / 记事本面板；`Dialogue/phone.dialogue` 是主菜单剧本。
- `Scripts/Documents/`：`DocumentData`（Id/Title/Body）与 `DocumentLibrary` 注册表；`Resources/Documents/` 放注册表与示例文档 `club_notice.tres`。
- `Scripts/Phone/PhoneScreen.cs`：手机会话状态机、文档列表 / 阅读 / 记事本、输入锁。
```

在「## 对话系统」之外新增一段：

```markdown
## 手机系统

- 左上角 `PhoneScreen`（`CanvasLayer.layer=45`）提供手机入口；打开时用 `DialogueBalloon` 加载 `Dialogue/phone.dialogue` 显示「查看文档 / 记事本 / 关闭」，文档列表 / 阅读 / 记事本用 `PhoneScreen` 自己的面板。
- 文档：`DocumentData`（Id/Title/Body）+ `DocumentLibrary` 注册表放在 `Resources/Documents/`，启动时 `GameScene` 调 `GameState.SeedDocuments` 灌入；对话正文用 `[url=doc:<id>][color=#ffd866]文字[/color][/url]` 标可获取文档，点击只获取、不推进对话；已获取文档跨循环保留。
- 记事本：`GameState.NotebookText` 单个字符串，每次 `TextEdit.TextChanged` 直接写入；跨循环保留但不落盘。
- `DialogueBalloon.KeepPlayerInputLockedOnExit`：手机会话期间保持玩家输入锁，由 `PhoneScreen` 在关闭 / 循环重置时统一释放。
- `PhoneScreen` 的面板容器 `Center` 必须 `mouse_filter = IGNORE`，否则全屏容器会抢走面板鼠标事件。
```

在「## 对话系统」的 POT 登记句末尾追加 `、res://Dialogue/phone.dialogue`。

- [ ] **步骤 4：最终检查**

```powershell
& $godot --headless --path . --build-solutions --quit
& $godot --headless --path . --quit-after 120
```

预期：退出码 0；无 `ERROR` / `SCRIPT ERROR`。**不执行 git commit / branch / checkout**。

---

## 风险与收尾

| 风险 | 处理 |
|---|---|
| 菜单 balloon 作为 `PhoneScreen` 子节点（嵌套 `CanvasLayer`）层级异常 | 改为挂到 `GameScene` 根节点；`PhoneScreen` 仍通过 `TreeExited` 管理；不要改 balloon 场景的 `layer` |
| `phone.dialogue` 的 `do`-only cue 解析失败 | 在 `~ documents` / `~ notes` 下加一行短提示，再导入验证 |
| `KeepPlayerInputLockedOnExit` 为 true 时 `PhoneScreen` 异常退出 | 所有关闭路径（关闭按钮、返回、循环信号、`_ExitTree`）都解锁；手测清单覆盖 |
| 链接点击与推进同帧竞争 | 帧末 `CallDeferred` + `lastMetaClickFrame` 判定 |
| 拖拽选择误获取文档 | `MetaClicked` 里用按下点到当前鼠标位移 > 6px 排除 |
| 文档正文里的方括号被当 BBCode | 正文由策划控制；需要字面方括号时转义或改写 |
| `project.godot` 被编辑器覆盖 | 修改前确认 Godot 编辑器已关闭 |
| 记事本不落盘 | 明确范围：只跨循环；关闭游戏即清空 |
| git 只读 | 本计划不产生任何 commit / branch / checkout |

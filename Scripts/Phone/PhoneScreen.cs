using Godot;
using System.Collections.Generic;
using DialogueManagerRuntime;

/// <summary>手机：左上角入口、主菜单 balloon、文档列表 / 阅读 / 记事本面板。</summary>
public partial class PhoneScreen : CanvasLayer
{
	/// <summary>本游戏的对话 balloon 场景，用于手机主菜单。</summary>
	[Export] public PackedScene DialogueBalloonScene { get; set; }

	/// <summary>手机主菜单剧本。</summary>
	[Export] public Resource PhoneDialogue { get; set; }

	/// <summary>区分拖拽选择与单击的位移阈值（像素）。</summary>
	private const float DocDragThreshold = 6.0f;

	/// <summary>未读的重要的事的高亮色，与对话里的文档高亮同色。</summary>
	private static readonly Color UnreadHighlightColor = Color.FromHtml("ffd866");

	/// <summary>手机主菜单里「重要的事」选项的选项 id，对应 Dialogue/phone.dialogue 的 [ID:]。</summary>
	private const string DocumentsOptionId = "open_documents";

	private enum PhonePage
	{
		Documents,
		Notebook,
		TimeSkip
	}

	private Button PhoneButton;
	private KeyRing KeyRing;
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
	private TimeSkipPanel TimeSkipPanel;

	private bool _open;
	private bool _loopEnded;
	private bool _closing;
	private PhonePage? _pendingPage;
	private DialogueBalloon _menuBalloon;
	private Player _player;
	private bool _docPointerPressed;
	private Vector2 _docPressPosition;
	private Tween _docCopyTween;
	private DocumentData _currentDoc;

	/// <summary>正文里当前处于鼠标悬浮状态的链接 meta；空串表示没有。</summary>
	private string _hoveredLinkMeta = "";

	public override void _Ready()
	{
		PhoneButton = GetNode<Button>("%PhoneButton");
		KeyRing = GetNode<KeyRing>("%KeyRing");
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
		TimeSkipPanel = GetNode<TimeSkipPanel>("%TimeSkipPanel");

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
			GameState.Instance.InventoryChanged += OnInventoryChanged;
		}

		PhoneButton.Pressed += Open;
		DocListBack.Pressed += BackToMainMenu;
		DocReadBack.Pressed += ShowDocumentList;
		NotebookBack.Pressed += BackToMainMenu;
		NotebookEdit.TextChanged += OnNotebookTextChanged;
		TimeSkipPanel.Confirmed += OnTimeSkipConfirmed;
		GetNode<Button>("%TimeSkipClose").Pressed += BackToMainMenu;
		DocReadBody.GuiInput += OnDocReadGuiInput;
		DocReadBody.MetaClicked += OnDocMetaClicked;
		LinkTextColors.ApplyUnderline(DocReadBody);
		DocReadBody.MetaHoverStarted += OnDocMetaHoverStarted;
		DocReadBody.MetaHoverEnded += OnDocMetaHoverEnded;

		Overlay.Hide();
		DocListPanel.Hide();
		DocReadPanel.Hide();
		NotebookPanel.Hide();
		TimeSkipPanel.Hide();
		DocReadBody.SelectionEnabled = false;
		DocCopyToast.Visible = false;
	}

	public override void _ExitTree()
	{
		if (GodotObject.IsInstanceValid(GameState.Instance))
		{
			GameState.Instance.LoopEndReached -= OnLoopEndReached;
			GameState.Instance.LoopReset -= OnLoopReset;
			GameState.Instance.InventoryChanged -= OnInventoryChanged;
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
		if (DocListPanel.Visible || NotebookPanel.Visible || TimeSkipPanel.Visible)
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

	/// <summary>由 phone.dialogue 的 do 调用：关闭主菜单后打开等待时间选择器。</summary>
	public void OpenTimeSkip()
	{
		if (!_open)
		{
			GD.PushError("PhoneScreen.OpenTimeSkip: 手机未打开");
			return;
		}
		if (_closing) return;
		_pendingPage = PhonePage.TimeSkip;
	}

	/// <summary>循环开始提示：自动打开手机，把提示追加到记事本并直接显示记事本面板。</summary>
	public void OpenNotebookWithNote(string note)
	{
		if (GameState.Instance == null)
		{
			GD.PushError("PhoneScreen.OpenNotebookWithNote: GameState 未就绪");
			return;
		}
		if (string.IsNullOrEmpty(note))
		{
			GD.PushError("PhoneScreen.OpenNotebookWithNote: 提示文本为空");
			return;
		}

		if (!_open)
		{
			Open();
			if (!_open) return;
		}

		AppendNotebookNote(note);
		ShowNotebook();
	}

	/// <summary>把一条提示追加到记事本末尾；记事本里已有同样内容时不重复追加。</summary>
	private static void AppendNotebookNote(string note)
	{
		string text = GameState.Instance.NotebookText;
		if (string.IsNullOrEmpty(text))
		{
			GameState.Instance.NotebookText = note;
		}
		else if (!text.Contains(note))
		{
			GameState.Instance.NotebookText = text + "\n" + note;
		}
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
		KeyRing.SetHudVisible(false);
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
		balloon.ResponseColorResolver = ResolveMenuResponseColor;
		balloon.TreeExited += OnMenuBalloonExited;
		AddChild(balloon);
		_menuBalloon = balloon;

		var states = new Godot.Collections.Array<Variant>
		{
			new Godot.Collections.Dictionary { { "phone", this } }
		};
		balloon.Start(PhoneDialogue, "start", states);
	}

	/// <summary>手机主菜单的选项配色：有还没读过的重要的事时，把「重要的事」选项标黄。</summary>
	private Color? ResolveMenuResponseColor(DialogueResponse response)
	{
		if (response == null) return null;
		if (response.StaticId != DocumentsOptionId) return null;
		if (GameState.Instance == null)
		{
			GD.PushError("PhoneScreen.ResolveMenuResponseColor: GameState 未就绪");
			return null;
		}

		return GameState.Instance.HasUnreadDocuments ? UnreadHighlightColor : null;
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
			case PhonePage.TimeSkip:
				ShowTimeSkip();
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
		KeyRing.SetHudVisible(true);
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
		TimeSkipPanel.Hide();
		DocReadBody.SelectionEnabled = false;
		_hoveredLinkMeta = "";
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
			if (GameState.Instance != null && !GameState.Instance.IsDocumentRead(document.Id))
			{
				ButtonTextColors.Apply(button, UnreadHighlightColor);
			}
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
		if (GameState.Instance == null)
		{
			GD.PushError("PhoneScreen.ShowDocumentRead: GameState 未就绪");
			return;
		}

		// 打开正文即视为已读：菜单里的「重要的事」与列表标题的黄色高亮随之消失。
		GameState.Instance.MarkDocumentRead(document.Id);

		HideAllPages();
		Overlay.Show();
		DocReadPanel.Show();
		GD.Print($"[Phone] 阅读文档 {document.Id}");
		DocReadTitle.Text = document.Title;
		_currentDoc = document;
		_hoveredLinkMeta = "";
		DocReadBody.Text = ApplyClueStyle(document.Body);
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

	private void ShowTimeSkip()
	{
		if (GameState.Instance == null)
		{
			GD.PushError("PhoneScreen.ShowTimeSkip: GameState 未就绪");
			CloseSession();
			return;
		}

		HideAllPages();
		Overlay.Show();
		TimeSkipPanel.Show();
		GD.Print("[Phone] 玩手机消磨时间");
		TimeSkipPanel.Open(GameState.Instance.ElapsedMinutes, GameState.LoopDurationMinutes);
	}

	/// <summary>时间选择器确认：先关掉手机释放输入锁，再把时钟推进到选定的时刻。</summary>
	private void OnTimeSkipConfirmed(int targetElapsedMinutes)
	{
		if (GameState.Instance == null)
		{
			GD.PushError("PhoneScreen.OnTimeSkipConfirmed: GameState 未就绪");
			return;
		}

		int now = GameState.Instance.ElapsedMinutes;
		int delta = targetElapsedMinutes - now;
		if (delta <= 0)
		{
			GD.PushError($"PhoneScreen.OnTimeSkipConfirmed: 目标时刻 {targetElapsedMinutes} 不晚于当前时刻 {now}");
			return;
		}

		CloseSession();
		GameState.Instance.AdvanceTime(delta);
	}

	private void OnDocMetaClicked(Variant meta)
	{
		if (meta.VariantType != Variant.Type.String) return;
		string value = meta.AsString();
		if (!value.StartsWith("clue:")) return;
		string clueId = value.Substring("clue:".Length);
		if (string.IsNullOrEmpty(clueId) || GameState.Instance == null) return;

		GameState.Instance.AddClue(clueId);
		// 解锁后链接立刻变灰；鼠标还停在上面时就显示灰字的悬浮色。
		ApplyDocHover();
	}

	/// <summary>鼠标移进线索链接：把这条链接换成悬浮色；同一链接重复触发时不重写正文。</summary>
	private void OnDocMetaHoverStarted(Variant meta)
	{
		if (meta.VariantType != Variant.Type.String) return;
		string value = meta.AsString();
		if (!value.StartsWith("clue:")) return;
		if (_hoveredLinkMeta == value) return;
		// 拖拽选择 / 已有选区时重写正文会清掉选区。
		if (_docPointerPressed) return;
		if (!string.IsNullOrEmpty(DocReadBody.GetSelectedText())) return;

		_hoveredLinkMeta = value;
		ApplyDocHover();
	}

	/// <summary>鼠标移出线索链接：还原这条链接的高亮色。</summary>
	private void OnDocMetaHoverEnded(Variant meta)
	{
		if (meta.VariantType != Variant.Type.String) return;
		if (_hoveredLinkMeta != meta.AsString()) return;

		_hoveredLinkMeta = "";
		if (_docPointerPressed) return;
		ApplyDocHover();
	}

	/// <summary>按 _hoveredLinkMeta 重绘正文：有悬浮的链接时该链接换成悬浮色，否则回到已解锁 / 未解锁的原色。</summary>
	private void ApplyDocHover()
	{
		if (_currentDoc == null) return;

		string text = ApplyClueStyle(_currentDoc.Body);
		if (_hoveredLinkMeta != "")
		{
			text = LinkTextColors.RecolorLinkForHover(text, _hoveredLinkMeta);
		}

		if (DocReadBody.Text != text)
		{
			DocReadBody.Text = text;
		}
	}

	/// <summary>拖拽 / 点击结束后还原悬浮色；正文有选区时不重写，避免把选区清掉。</summary>
	private void RestoreLinkHoverIfIdle()
	{
		if (!string.IsNullOrEmpty(DocReadBody.GetSelectedText())) return;
		ApplyDocHover();
	}

	/// <summary>已解锁的线索链接在正文里变灰。</summary>
	private static string ApplyClueStyle(string body)
	{
		if (string.IsNullOrEmpty(body) || GameState.Instance == null) return body;
		return System.Text.RegularExpressions.Regex.Replace(
			body,
			@"\[url=clue:([^\]]+)\]\[color=#ffd866\]",
			m => GameState.Instance.HasClue(m.Groups[1].Value)
				? $"[url=clue:{m.Groups[1].Value}][color=#8a8a8a]"
				: m.Value);
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

		if (mouse.Position.DistanceTo(_docPressPosition) <= DocDragThreshold)
		{
			RestoreLinkHoverIfIdle();
			return;
		}

		string selected = DocReadBody.GetSelectedText();
		if (string.IsNullOrWhiteSpace(selected))
		{
			RestoreLinkHoverIfIdle();
			return;
		}

		if (!DisplayServer.HasFeature(DisplayServer.Feature.Clipboard))
		{
			GD.PushError("PhoneScreen: 当前平台没有剪贴板，无法复制文档");
			return;
		}

		DisplayServer.ClipboardSet(selected);
		ShowDocCopyToast();
		GD.Print($"[Phone] 已复制文档 {selected.Length} 字");
		RestoreLinkHoverIfIdle();
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
		KeyRing.SetHudVisible(false);
	}

	private void OnLoopReset()
	{
		_loopEnded = false;
		if (_open) CloseSession();
		PhoneButton.Show();
		KeyRing.SetHudVisible(true);
	}

	private void OnInventoryChanged()
	{
		KeyRing.Refresh();
	}
}

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

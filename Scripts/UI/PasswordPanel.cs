using Godot;

/// <summary>山田电脑密码面板：4 位输入，正确后把电脑日志记进手机（地下空间改由在日志里点击高亮文字解锁）。</summary>
public partial class PasswordPanel : CanvasLayer
{
	/// <summary>正确密码；剧本设定 0721。</summary>
	[Export] public string Password { get; set; } = "0721";

	/// <summary>解锁后获取的文档 id。</summary>
	[Export] public string DocumentId { get; set; } = "yamada_notebook";

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
		_error.Hide();
		_message.Text = added ? "解锁成功。电脑日志的内容已记录到手机。" : "已经解锁过了。";
		_message.Show();
		_input.Editable = false;

		GetTree().CreateTimer(1.0).Timeout += Close;
	}
}

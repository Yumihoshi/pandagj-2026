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

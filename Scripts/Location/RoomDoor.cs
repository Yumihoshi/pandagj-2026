using Godot;
using DialogueManagerRuntime;

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

	/// <summary>锁着时播的对话；设置后优先于 LockedHint。</summary>
	[Export] public Resource LockedDialogue { get; set; }

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
		if (IsLocked() && LockedHint == null && LockedDialogue == null)
		{
			GD.PushError($"RoomDoor {Name}: 已上锁但 LockedHint 与 LockedDialogue 都为空");
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

		if (IsCurrentlyLocked())
		{
			HandleLocked();
			return;
		}

		base.Interact();
	}

	/// <summary>当前是否因线索或钥匙不足而锁着。</summary>
	private bool IsCurrentlyLocked()
	{
		if (!string.IsNullOrEmpty(RequiredClueId) && !GameState.Instance.HasClue(RequiredClueId)) return true;
		if (!string.IsNullOrEmpty(RequiredKeyId) && !GameState.Instance.HasItem(RequiredKeyId)) return true;
		return false;
	}

	/// <summary>锁着时的反馈：有对话播对话，否则闪提示。</summary>
	private void HandleLocked()
	{
		if (LockedDialogue != null)
		{
			if (DialogueManager.ShowDialogueBalloon(LockedDialogue, "start") == null)
			{
				GD.PushError($"RoomDoor {Name}: ShowDialogueBalloon 返回空");
			}
			return;
		}
		ShowLockedHint();
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

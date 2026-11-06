using Godot;

/// <summary>房间之间的门。配了钥匙 id 时，背包里没有就不开。</summary>
public partial class RoomDoor : Interactable
{
	/// <summary>门通向的房间。</summary>
	[Export] public Room TargetRoom { get; set; }

	/// <summary>目标房间内的落点。</summary>
	[Export] public Marker2D TargetSpawn { get; set; }

	/// <summary>开门所需道具 id；留空表示不锁。</summary>
	[Export] public string RequiredKeyId { get; set; } = string.Empty;

	/// <summary>锁着时闪一下的占位提示。</summary>
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
		if (!string.IsNullOrEmpty(RequiredKeyId) && LockedHint == null)
		{
			GD.PushError($"RoomDoor {Name}: 已上锁但 LockedHint 为空");
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

		if (!string.IsNullOrEmpty(RequiredKeyId) && !GameState.Instance.HasItem(RequiredKeyId))
		{
			if (LockedHint == null) return;

			LockedHint.Visible = true;
			_hintTimer.Start();
			return;
		}

		base.Interact();
	}
}

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

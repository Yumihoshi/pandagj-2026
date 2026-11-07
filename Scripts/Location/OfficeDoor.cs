using Godot;

/// <summary>办公室门：山田在办公室的时段（13:30-16:59，本轮 30-239 分钟）开放；14:00-14:59 他午睡时才能偷钥匙。</summary>
public partial class OfficeDoor : RoomDoor
{
	/// <summary>山田回到办公室的分钟（13:30）。</summary>
	private const int OpensAtMinute = 30;

	/// <summary>山田离开办公室去地下的分钟（17:00）。</summary>
	private const int ClosesAtMinute = 240;

	public override void Interact()
	{
		if (GameState.Instance == null)
		{
			GD.PushError($"OfficeDoor {Name}: GameState 未就绪");
			return;
		}

		int minute = GameState.Instance.ElapsedMinutes;
		bool open = minute >= OpensAtMinute && minute < ClosesAtMinute;
		if (!open)
		{
			ShowLockedHint();
			return;
		}

		base.Interact();
	}
}

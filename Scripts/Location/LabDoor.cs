using Godot;

/// <summary>化学实验室门：本轮新井在分析且未到完成时间时，先把时间跳到完成时间。</summary>
public partial class LabDoor : RoomDoor
{
	public override void Interact()
	{
		if (GameState.Instance == null)
		{
			GD.PushError($"LabDoor {Name}: GameState 未就绪");
			return;
		}
		if (GameState.Instance.HasLoopFlag("shinji_analyzing"))
		{
			int ready = GameState.Instance.BombAnalysisReadyMinute;
			int now = GameState.Instance.ElapsedMinutes;
			if (ready >= 0 && now < ready)
			{
				if (GameState.Instance.AdvanceTime(ready - now)) return;
			}
		}
		base.Interact();
	}
}

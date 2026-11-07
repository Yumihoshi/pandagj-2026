using Godot;
using DialogueManagerRuntime;

/// <summary>地下服务器：湿炸药后等到 17:00。</summary>
public partial class WaitSpot : Interactable
{
	/// <summary>等待点剧本。</summary>
	[Export] public Resource Dialogue { get; set; }

	public override void Interact()
	{
		if (GameState.Instance == null)
		{
			GD.PushError($"WaitSpot {Name}: GameState 未就绪");
			return;
		}
		if (Dialogue == null)
		{
			GD.PushError($"WaitSpot {Name}: Dialogue 为空");
			return;
		}
		if (!GameState.Instance.HasLoopFlag("bomb_wet"))
		{
			Show("not_yet");
			return;
		}

		int now = GameState.Instance.ElapsedMinutes;
		if (now >= 240)
		{
			Show("already");
			return;
		}
		if (GameState.Instance.AdvanceTime(240 - now)) return;
		Show("waited");
	}

	private void Show(string cue)
	{
		if (DialogueManager.ShowDialogueBalloon(Dialogue, cue) == null)
		{
			GD.PushError($"WaitSpot {Name}: ShowDialogueBalloon({cue}) 返回空");
		}
	}
}

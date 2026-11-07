using Godot;
using DialogueManagerRuntime;

/// <summary>器材准备室水管：知道炸药解法后接一桶水。</summary>
public partial class WaterPipe : Interactable
{
	/// <summary>水管剧本。</summary>
	[Export] public Resource Dialogue { get; set; }

	public override void Interact()
	{
		if (GameState.Instance == null)
		{
			GD.PushError($"WaterPipe {Name}: GameState 未就绪");
			return;
		}
		if (Dialogue == null)
		{
			GD.PushError($"WaterPipe {Name}: Dialogue 为空");
			return;
		}
		if (GameState.Instance.HasLoopFlag("water_taken") || GameState.Instance.HasItem("water_bucket"))
		{
			Show("already");
			return;
		}
		if (!GameState.Instance.HasClue("bomb_weakness"))
		{
			Show("hint");
			return;
		}

		GameState.Instance.AddItem("water_bucket");
		GameState.Instance.SetLoopFlag("water_taken");
		Show("fill");
	}

	private void Show(string cue)
	{
		if (DialogueManager.ShowDialogueBalloon(Dialogue, cue) == null)
		{
			GD.PushError($"WaterPipe {Name}: ShowDialogueBalloon({cue}) 返回空");
		}
	}
}

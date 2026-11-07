using Godot;
using DialogueManagerRuntime;

/// <summary>地下炸药：取样 / 湿炸药 / 状态提示。</summary>
public partial class BombInteractable : Interactable
{
	/// <summary>炸药剧本。</summary>
	[Export] public Resource Dialogue { get; set; }

	public override void Interact()
	{
		if (GameState.Instance == null)
		{
			GD.PushError($"BombInteractable {Name}: GameState 未就绪");
			return;
		}
		if (Dialogue == null)
		{
			GD.PushError($"BombInteractable {Name}: Dialogue 为空");
			return;
		}

		if (!GameState.Instance.HasLoopFlag("bomb_sample_taken"))
		{
			GameState.Instance.AddItem("bomb_sample");
			GameState.Instance.SetLoopFlag("bomb_sample_taken");
			Show("sample");
			return;
		}
		if (!GameState.Instance.HasLoopFlag("bomb_wet"))
		{
			if (GameState.Instance.HasItem("water_bucket") && GameState.Instance.HasClue("bomb_weakness"))
			{
				GameState.Instance.SetLoopFlag("bomb_wet");
				Show("wet");
				return;
			}
			Show("nothing");
			return;
		}
		Show("already_wet");
	}

	private void Show(string cue)
	{
		if (DialogueManager.ShowDialogueBalloon(Dialogue, cue) == null)
		{
			GD.PushError($"BombInteractable {Name}: ShowDialogueBalloon({cue}) 返回空");
		}
	}
}

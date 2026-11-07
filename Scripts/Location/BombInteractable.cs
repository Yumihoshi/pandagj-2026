using Godot;
using DialogueManagerRuntime;

/// <summary>地下炸药：取样 / 湿炸药后直接进入 17:00 终局对质。</summary>
public partial class BombInteractable : Interactable
{
	/// <summary>炸药剧本。</summary>
	[Export] public Resource Dialogue { get; set; }

	/// <summary>湿炸药后自动播放的山田终局剧本。</summary>
	[Export] public Resource EndingDialogue { get; set; }

	/// <summary>终局时刻：17:00（循环内第 240 分钟）。</summary>
	private const int EndingMinute = 240;

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
				if (EndingDialogue == null)
				{
					GD.PushError($"BombInteractable {Name}: EndingDialogue 为空");
					return;
				}
				GameState.Instance.SetLoopFlag("bomb_wet");
				Node balloon = Show("wet");
				if (balloon != null) balloon.TreeExited += OnWetDialogueExited;
				return;
			}
			Show("nothing");
			return;
		}
		Show("already_wet");
	}

	/// <summary>湿炸药旁白结束后把时间推到 17:00，山田登场并直接进入终局对质。</summary>
	private void OnWetDialogueExited()
	{
		if (!IsInsideTree() || GameState.Instance == null) return;

		int now = GameState.Instance.ElapsedMinutes;
		if (now < EndingMinute && GameState.Instance.AdvanceTime(EndingMinute - now)) return;

		if (DialogueManager.ShowDialogueBalloon(EndingDialogue, "start") == null)
		{
			GD.PushError($"BombInteractable {Name}: 终局对话 ShowDialogueBalloon(start) 返回空");
		}
	}

	private Node Show(string cue)
	{
		Node balloon = DialogueManager.ShowDialogueBalloon(Dialogue, cue);
		if (balloon == null)
		{
			GD.PushError($"BombInteractable {Name}: ShowDialogueBalloon({cue}) 返回空");
		}
		return balloon;
	}
}

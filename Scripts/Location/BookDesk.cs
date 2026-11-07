using Godot;
using DialogueManagerRuntime;

/// <summary>教室座位：一周目一次坐下读完整段 BL 小说，读完后时间直接到 17:30 终末。</summary>
public partial class BookDesk : Interactable
{
	/// <summary>看书剧本。</summary>
	[Export] public Resource Dialogue { get; set; }

	public override void Interact()
	{
		if (GameState.Instance == null)
		{
			GD.PushError($"BookDesk {Name}: GameState 未就绪");
			return;
		}
		if (Dialogue == null)
		{
			GD.PushError($"BookDesk {Name}: Dialogue 为空");
			return;
		}
		if (!GameState.Instance.IsFirstLoop())
		{
			Show("later");
			return;
		}

		Node balloon = Show("read");
		if (balloon != null) balloon.TreeExited += OnReadingExited;
	}

	/// <summary>整段阅读结束后把时间推进到 17:30，触发本轮终末。</summary>
	private void OnReadingExited()
	{
		if (!IsInsideTree() || GameState.Instance == null) return;

		int remain = GameState.LoopDurationMinutes - GameState.Instance.ElapsedMinutes;
		if (remain > 0) GameState.Instance.AdvanceTime(remain);
	}

	private Node Show(string cue)
	{
		Node balloon = DialogueManager.ShowDialogueBalloon(Dialogue, cue);
		if (balloon == null)
		{
			GD.PushError($"BookDesk {Name}: ShowDialogueBalloon({cue}) 返回空");
		}
		return balloon;
	}
}

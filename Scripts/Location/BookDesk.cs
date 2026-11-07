using Godot;
using DialogueManagerRuntime;

/// <summary>教室座位：一周目看书推进时间，第 5 次进入 last 分支。</summary>
public partial class BookDesk : Interactable
{
	/// <summary>看书剧本。</summary>
	[Export] public Resource Dialogue { get; set; }

	private int _readCount;

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
		if (_readCount >= 4 || GameState.Instance.ElapsedMinutes >= 240)
		{
			Show("last");
			return;
		}
		if (GameState.Instance.AdvanceTime(60)) return;
		_readCount++;
		Show($"read_{_readCount}");
	}

	private void Show(string cue)
	{
		if (DialogueManager.ShowDialogueBalloon(Dialogue, cue) == null)
		{
			GD.PushError($"BookDesk {Name}: ShowDialogueBalloon({cue}) 返回空");
		}
	}
}

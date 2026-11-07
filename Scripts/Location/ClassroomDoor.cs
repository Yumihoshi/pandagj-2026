using Godot;
using DialogueManagerRuntime;

/// <summary>教室→走廊的门：一周目始终挡住（只能看书），并播 BL 小说独白。</summary>
public partial class ClassroomDoor : RoomDoor
{
	/// <summary>被挡住时播放的剧本。</summary>
	[Export] public Resource BlockingDialogue { get; set; }

	public override void Interact()
	{
		if (GameState.Instance == null)
		{
			GD.PushError($"ClassroomDoor {Name}: GameState 未就绪");
			return;
		}
		if (GameState.Instance.IsFirstLoop())
		{
			if (BlockingDialogue == null)
			{
				GD.PushError($"ClassroomDoor {Name}: BlockingDialogue 为空");
				return;
			}
			if (DialogueManager.ShowDialogueBalloon(BlockingDialogue) == null)
			{
				GD.PushError($"ClassroomDoor {Name}: ShowDialogueBalloon 返回空");
			}
			return;
		}
		base.Interact();
	}
}

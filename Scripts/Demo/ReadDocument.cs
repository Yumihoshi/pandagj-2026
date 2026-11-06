using Godot;

/// <summary>demo 用的一次性文档：读完解锁指定大地点。</summary>
public partial class ReadDocument : Interactable
{
	[Export] public string UnlocksLocationId { get; set; } = "playground";

	public override void _Ready()
	{
		if (string.IsNullOrEmpty(UnlocksLocationId))
		{
			GD.PushError($"ReadDocument {Name}: UnlocksLocationId 为空");
		}
	}

	public override void Interact()
	{
		GameState.Instance.UnlockLocation(UnlocksLocationId);
		GD.Print($"[Doc] 读完《{Name}》，解锁 {UnlocksLocationId}");
	}
}

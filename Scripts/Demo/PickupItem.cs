using Godot;

/// <summary>demo 用的一次性拾取物：交互后把 itemId 放进背包并消失。</summary>
public partial class PickupItem : Interactable
{
	[Export] public string ItemId { get; set; } = "equipment_key";

	public override void _Ready()
	{
		if (string.IsNullOrEmpty(ItemId))
		{
			GD.PushError($"PickupItem {Name}: ItemId 为空");
			return;
		}
		if (GameState.Instance.HasItem(ItemId))
		{
			QueueFree();
		}
	}

	public override void Interact()
	{
		GameState.Instance.AddItem(ItemId);
		QueueFree();
	}
}

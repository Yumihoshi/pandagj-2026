using Godot;
using System.Linq;

/// <summary>最小交互触发：取范围内最近的 interactable，按 F 调它的 Interact()。</summary>
public partial class PlayerInteractor : Area2D
{
	private Interactable _current;

	public override void _PhysicsProcess(double delta)
	{
		_current = GetOverlappingAreas()
			.OfType<Interactable>()
			.OrderBy(a => a.GlobalPosition.DistanceSquaredTo(GlobalPosition))
			.FirstOrDefault();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (_current == null || !@event.IsActionPressed("interact")) return;

		_current.Interact();
		GetViewport().SetInputAsHandled();
	}
}

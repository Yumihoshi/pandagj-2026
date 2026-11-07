using Godot;
using System.Linq;

/// <summary>最小交互触发：取范围内最近的 interactable，按 F 调它的 Interact()，并在它头顶显示「F 交互」提示。</summary>
public partial class PlayerInteractor : Area2D
{
	/// <summary>目标头顶的提示，位置由本节点按目标碰撞盒顶部算出。</summary>
	[Export] public InteractPrompt Prompt { get; set; }

	/// <summary>算不出碰撞盒时，提示相对目标原点上移的兜底距离。</summary>
	[Export] public float FallbackHeadOffset { get; set; } = 80f;

	private Interactable _current;
	private Player _player;
	private Interactable _headTarget;
	private Vector2 _headPoint;

	public override void _Ready()
	{
		_player = GetParent<Player>();
		if (_player == null)
		{
			GD.PushError("PlayerInteractor: 父节点不是 Player");
		}
		if (Prompt == null)
		{
			GD.PushError("PlayerInteractor: Prompt 为空，交互提示不会显示");
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		_current = GetOverlappingAreas()
			.OfType<Interactable>()
			.OrderBy(a => a.GlobalPosition.DistanceSquaredTo(GlobalPosition))
			.FirstOrDefault();

		UpdatePrompt();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (_player == null || _player.InputLocked) return;
		if (_current == null || !@event.IsActionPressed("interact")) return;

		if (OfficeKickOut.TryInterceptAny(GetTree(), _current))
		{
			GetViewport().SetInputAsHandled();
			return;
		}

		if (TalkToYukaFirst.TryInterceptAny(GetTree(), _current))
		{
			GetViewport().SetInputAsHandled();
			return;
		}

		_current.Interact();
		GetViewport().SetInputAsHandled();
	}

	/// <summary>有机会交互时把提示贴到目标头顶，否则隐藏；提示位置按目标缓存，避免每帧重算。</summary>
	private void UpdatePrompt()
	{
		if (Prompt == null) return;

		bool canPrompt = _player != null && !_player.InputLocked
			&& _current != null && GodotObject.IsInstanceValid(_current);

		if (!canPrompt)
		{
			_headTarget = null;
			Prompt.HidePrompt();
			return;
		}

		if (_current != _headTarget)
		{
			_headTarget = _current;
			_headPoint = ResolveHeadPoint(_current);
		}

		Prompt.ShowAt(_headPoint);
	}

	/// <summary>目标头顶锚点（全局坐标）：有名字标签时贴到标签上方，否则用碰撞盒上边界。</summary>
	private Vector2 ResolveHeadPoint(Interactable target)
	{
		if (target.GetNodeOrNull<Control>("NameLabel") is Control nameLabel)
		{
			return new Vector2(
				nameLabel.GlobalPosition.X + nameLabel.Size.X * 0.5f,
				nameLabel.GlobalPosition.Y);
		}

		Rect2? bounds = CollectBounds(null, target);
		if (bounds == null)
		{
			GD.PushError($"PlayerInteractor: {target.Name} 没有带形状的 CollisionShape2D，提示改用兜底位置");
			return target.GlobalPosition - new Vector2(0f, FallbackHeadOffset);
		}

		Rect2 rect = bounds.Value;
		return new Vector2(rect.Position.X + rect.Size.X * 0.5f, rect.Position.Y);
	}

	/// <summary>递归收集节点下所有启用碰撞形状的全局包围盒。</summary>
	private static Rect2? CollectBounds(Rect2? bounds, Node node)
	{
		foreach (Node child in node.GetChildren())
		{
			if (child is CollisionShape2D shape)
			{
				if (shape.Disabled) continue;
				if (shape.Shape == null)
				{
					GD.PushError($"PlayerInteractor: {shape.GetPath()} 没有 Shape，已跳过");
					continue;
				}

				Rect2 local = shape.Shape.GetRect();
				Vector2 topLeft = shape.ToGlobal(local.Position);
				Vector2 topRight = shape.ToGlobal(new Vector2(local.End.X, local.Position.Y));
				Vector2 bottomLeft = shape.ToGlobal(new Vector2(local.Position.X, local.End.Y));
				Vector2 bottomRight = shape.ToGlobal(local.End);

				float minX = Mathf.Min(Mathf.Min(topLeft.X, topRight.X), Mathf.Min(bottomLeft.X, bottomRight.X));
				float maxX = Mathf.Max(Mathf.Max(topLeft.X, topRight.X), Mathf.Max(bottomLeft.X, bottomRight.X));
				float minY = Mathf.Min(Mathf.Min(topLeft.Y, topRight.Y), Mathf.Min(bottomLeft.Y, bottomRight.Y));
				float maxY = Mathf.Max(Mathf.Max(topLeft.Y, topRight.Y), Mathf.Max(bottomLeft.Y, bottomRight.Y));

				Rect2 shapeBounds = new(minX, minY, maxX - minX, maxY - minY);
				bounds = bounds == null ? shapeBounds : bounds.Value.Merge(shapeBounds);
			}

			bounds = CollectBounds(bounds, child);
		}

		return bounds;
	}
}

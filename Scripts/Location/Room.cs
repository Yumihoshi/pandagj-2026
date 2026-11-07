using Godot;
using System.Collections.Generic;

/// <summary>地点内的一个房间。同一时刻只有一个房间处于激活状态。</summary>
public partial class Room : Node2D
{
	/// <summary>玩家进入本房间时的默认落点。</summary>
	[Export] public Marker2D DefaultSpawn { get; set; }

	/// <summary>一周目进入本房间时的落点；留空则沿用默认落点。</summary>
	[Export] public Marker2D FirstLoopSpawn { get; set; }

	/// <summary>相机限位，本房间局部坐标。</summary>
	[Export] public Rect2I CameraBounds { get; set; }

	/// <summary>启用/停用整个房间：可见性、物理碰撞、处理模式一起切。</summary>
	public void SetActive(bool active)
	{
		Visible = active;
		ProcessMode = active ? ProcessModeEnum.Inherit : ProcessModeEnum.Disabled;

		foreach (CollisionShape2D shape in FindShapes(this))
		{
			shape.SetDeferred("disabled", !active);
		}
	}

	private static IEnumerable<CollisionShape2D> FindShapes(Node node)
	{
		foreach (Node child in node.GetChildren())
		{
			if (child is CollisionShape2D shape) yield return shape;
			foreach (CollisionShape2D nested in FindShapes(child)) yield return nested;
		}
	}
}

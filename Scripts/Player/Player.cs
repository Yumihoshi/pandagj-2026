using Godot;

public partial class Player : CharacterBody2D
{
	/// <summary>水平移动速度（像素/秒）。</summary>
	[Export] public float MoveSpeed { get; set; } = 200f;

	/// <summary>跟随玩家的相机，由地点场景设置限位。</summary>
	[Export] public Camera2D Camera { get; set; }

	public override void _PhysicsProcess(double delta)
	{
		float direction = Input.GetAxis("move_left", "move_right");
		Velocity = new Vector2(direction * MoveSpeed, 0f);
		MoveAndSlide();
	}
}

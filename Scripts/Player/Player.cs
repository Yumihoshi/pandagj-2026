using Godot;

/// <summary>玩家：左右移动，对话时锁输入。</summary>
public partial class Player : CharacterBody2D
{
	/// <summary>水平移动速度（像素/秒）。</summary>
	[Export] public float MoveSpeed { get; set; } = 200f;

	/// <summary>跟随玩家的相机，由地点场景设置限位。</summary>
	[Export] public Camera2D Camera { get; set; }

	/// <summary>对话等 UI 打开时锁住移动与交互；由持有 UI 的一方负责复位。</summary>
	public bool InputLocked { get; set; }

	public override void _EnterTree()
	{
		AddToGroup("player");
	}

	public override void _PhysicsProcess(double delta)
	{
		if (InputLocked)
		{
			Velocity = Vector2.Zero;
			return;
		}

		float direction = Input.GetAxis("move_left", "move_right");
		Velocity = new Vector2(direction * MoveSpeed, 0f);
		MoveAndSlide();
	}
}

using Godot;

/// <summary>玩家：左右移动，对话时锁输入。</summary>
public partial class Player : CharacterBody2D
{
	/// <summary>水平移动速度（像素/秒）。</summary>
	[Export] public float MoveSpeed { get; set; } = 200f;

	/// <summary>跟随玩家的相机，由地点场景设置限位。</summary>
	[Export] public Camera2D Camera { get; set; }

	private bool _inputLocked;

	/// <summary>对话等 UI 打开时锁住移动与交互；由持有 UI 的一方负责复位。</summary>
	public bool InputLocked
	{
		get => _inputLocked;
		set
		{
			if (_inputLocked == value) return;
			_inputLocked = value;
			if (!_inputLocked) ReleaseMovementInput();
		}
	}

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

	/// <summary>
	/// 清掉移动键的按压状态。UI 里用键盘输入文字时字母键同样会写进动作状态，
	/// key-up 一旦丢失（例如输入法吞掉抬起事件）动作就会一直算作按住，
	/// 解锁后玩家会自己朝一个方向走；因此解锁时把锁期间积压的移动输入丢掉。
	/// </summary>
	private static void ReleaseMovementInput()
	{
		Input.ActionRelease("move_left");
		Input.ActionRelease("move_right");
	}
}

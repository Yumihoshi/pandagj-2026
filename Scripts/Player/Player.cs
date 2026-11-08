using Godot;

/// <summary>玩家：左右移动与跑步动画，对话时锁输入。</summary>
public partial class Player : CharacterBody2D
{
	private const string IdleAnimation = "idle";
	private const string RunAnimation = "run";

	/// <summary>水平移动速度（像素/秒）。</summary>
	[Export] public float MoveSpeed { get; set; } = 200f;

	/// <summary>跟随玩家的相机，由地点场景设置限位。</summary>
	[Export] public Camera2D Camera { get; set; }

	/// <summary>玩家立绘动画；静止播 idle，移动播 run。</summary>
	[Export] public AnimatedSprite2D Sprite { get; set; }

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

	public override void _Ready()
	{
		if (Sprite == null) GD.PushError("Player: Sprite 未设置，移动动画不会播放");
	}

	public override void _PhysicsProcess(double delta)
	{
		if (InputLocked)
		{
			Velocity = Vector2.Zero;
			UpdateAnimation(0f);
			return;
		}

		float direction = Input.GetAxis("move_left", "move_right");
		Velocity = new Vector2(direction * MoveSpeed, 0f);
		MoveAndSlide();
		UpdateAnimation(direction);
	}

	/// <summary>
	/// 按移动方向切换立绘：静止播 idle，移动播 run。跑步帧原图朝左，
	/// 所以向右移动时水平镜像。
	/// </summary>
	private void UpdateAnimation(float direction)
	{
		if (Sprite == null) return;

		if (direction == 0f)
		{
			if (Sprite.Animation != IdleAnimation) Sprite.Play(IdleAnimation);
			return;
		}

		Sprite.FlipH = direction > 0f;
		if (Sprite.Animation != RunAnimation) Sprite.Play(RunAnimation);
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

using Godot;

/// <summary>玩家：左右移动与跑步动画，对话时锁输入。</summary>
public partial class Player : CharacterBody2D
{
	private const string IdleAnimation = "idle";
	private const string RunAnimation = "run";
	private const string BackAnimation = "back";

	/// <summary>背影请求的存活时间（秒）：交互后这么久仍未锁输入，说明没有弹出对话，请求作废。</summary>
	private const float BackPoseRequestTimeout = 0.15f;

	/// <summary>水平移动速度（像素/秒）。</summary>
	[Export] public float MoveSpeed { get; set; } = 200f;

	/// <summary>跟随玩家的相机，由地点场景设置限位。</summary>
	[Export] public Camera2D Camera { get; set; }

	/// <summary>玩家立绘动画；静止播 idle，移动播 run。</summary>
	[Export] public AnimatedSprite2D Sprite { get; set; }

	/// <summary>撞到房间左右边界时，玩家中心与边界保持的距离，避免立绘被屏幕边缘裁掉。</summary>
	[Export] public float EdgeMargin { get; set; } = 120f;

	private float _minX = float.NegativeInfinity;
	private float _maxX = float.PositiveInfinity;
	private bool _inputLocked;
	private bool _backPoseActive;
	private bool _backPoseRequested;
	private float _backPoseRequestAge;

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
		UpdateBackPoseState((float)delta);

		if (InputLocked)
		{
			Velocity = Vector2.Zero;
			UpdateAnimation(0f);
			return;
		}

		float direction = Input.GetAxis("move_left", "move_right");
		Velocity = new Vector2(direction * MoveSpeed, 0f);
		MoveAndSlide();
		ClampToRoomBounds();
		UpdateAnimation(direction);
	}

	/// <summary>设置可移动的水平范围（全局 x，房间左右边缘）；由地点在切换房间时调用。</summary>
	public void SetHorizontalBounds(float minX, float maxX)
	{
		_minX = minX;
		_maxX = maxX;
	}

	/// <summary>房间左右边缘的隐形墙：把玩家中心限制在边界内侧 EdgeMargin 处。</summary>
	private void ClampToRoomBounds()
	{
		float left = _minX + EdgeMargin;
		float right = _maxX - EdgeMargin;
		if (left > right) return;

		float clampedX = Mathf.Clamp(GlobalPosition.X, left, right);
		if (Mathf.IsEqualApprox(clampedX, GlobalPosition.X)) return;

		GlobalPosition = new Vector2(clampedX, GlobalPosition.Y);
		Velocity = new Vector2(0f, Velocity.Y);
	}

	/// <summary>请求在接下来的交互对话里显示背影立绘；交互没有触发对话时请求自动作废。</summary>
	public void RequestBackPose()
	{
		_backPoseRequested = true;
		_backPoseRequestAge = 0f;
	}

	/// <summary>
	/// 推进背影立绘的请求 / 激活状态：交互触发对话时输入会被锁住，以此作为「对话已开始」的信号；
	/// 迟迟不锁输入视为这次交互没有对话，丢弃请求以免在门口闪一下背影。
	/// </summary>
	private void UpdateBackPoseState(float delta)
	{
		if (_backPoseRequested)
		{
			if (InputLocked)
			{
				_backPoseRequested = false;
				_backPoseActive = true;
				return;
			}

			_backPoseRequestAge += delta;
			if (_backPoseRequestAge > BackPoseRequestTimeout) _backPoseRequested = false;
			return;
		}

		if (_backPoseActive && !InputLocked) _backPoseActive = false;
	}

	/// <summary>
	/// 按移动方向切换立绘：静止播 idle（要求背影时播 back），移动播 run。跑步帧原图朝左，
	/// 所以向右移动时水平镜像。
	/// </summary>
	private void UpdateAnimation(float direction)
	{
		if (Sprite == null) return;

		if (direction == 0f)
		{
			string animation = _backPoseActive ? BackAnimation : IdleAnimation;
			if (Sprite.Animation != animation) Sprite.Play(animation);
			return;
		}

		_backPoseActive = false;
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

using Godot;

/// <summary>目标头顶的「F 交互」提示：由 PlayerInteractor 喂世界坐标，自己负责贴底边居中、浮动与淡入淡出。</summary>
public partial class InteractPrompt : Node2D
{
	/// <summary>提示本体（键帽 + 文字），按它的实际尺寸把底边中点贴到本节点的原点。</summary>
	[Export] public Control Pill { get; set; }

	/// <summary>提示底边与目标顶部之间的空隙。</summary>
	[Export] public float Gap { get; set; } = 14f;

	/// <summary>上下浮动的幅度（像素）。</summary>
	[Export] public float FloatAmplitude { get; set; } = 2.5f;

	/// <summary>上下浮动的角速度（弧度/秒）。</summary>
	[Export] public float FloatSpeed { get; set; } = 2.4f;

	private const float FadeInSeconds = 0.12f;
	private const float FadeOutSeconds = 0.09f;
	private const float PopStartScale = 0.86f;

	private Tween _tween;
	private bool _shown;
	private float _floatTime;

	public override void _Ready()
	{
		if (Pill == null)
		{
			GD.PushError("InteractPrompt: Pill 为空，提示不会显示");
			return;
		}

		Visible = false;
		Modulate = new Color(1f, 1f, 1f, 0f);
		Pill.Resized += ApplyLayout;
		Pill.ResetSize();
		ApplyLayout();
	}

	/// <summary>把提示显示在世界坐标 anchor（目标顶部中心）处；已显示时只更新位置。</summary>
	public void ShowAt(Vector2 anchor)
	{
		if (Pill == null) return;

		GlobalPosition = anchor;
		if (_shown) return;

		_shown = true;
		_floatTime = 0f;
		Visible = true;

		_tween?.Kill();
		_tween = CreateTween();
		_tween.SetParallel(true);
		_tween.TweenProperty(this, "modulate:a", 1f, FadeInSeconds);
		_tween.TweenProperty(this, "scale", Vector2.One, FadeInSeconds * 1.5f)
			.SetTrans(Tween.TransitionType.Back)
			.SetEase(Tween.EaseType.Out)
			.From(new Vector2(PopStartScale, PopStartScale));
	}

	/// <summary>淡出后隐藏提示。</summary>
	public void HidePrompt()
	{
		if (Pill == null || !_shown) return;

		_shown = false;

		_tween?.Kill();
		_tween = CreateTween();
		_tween.TweenProperty(this, "modulate:a", 0f, FadeOutSeconds);
		_tween.TweenCallback(Callable.From(FinishHide));
	}

	public override void _Process(double delta)
	{
		if (!_shown || Pill == null) return;

		_floatTime += (float)delta;
		ApplyLayout();
	}

	/// <summary>按提示尺寸把底边中点贴到原点（锚点），并叠加轻微上下浮动。</summary>
	private void ApplyLayout()
	{
		if (Pill == null) return;

		Vector2 size = Pill.Size;
		float bob = Mathf.Sin(_floatTime * FloatSpeed) * FloatAmplitude;
		Pill.Position = new Vector2(-size.X * 0.5f, -size.Y - Gap + bob);
	}

	private void FinishHide()
	{
		if (_shown) return;
		Visible = false;
	}
}

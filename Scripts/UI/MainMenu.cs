using Godot;
using System.Threading.Tasks;

/// <summary>主菜单：教室背景上居中排布标题与「开始循环 / 退出循环」，右下角放吉祥物装饰，负责入场淡入与切换前的淡出。</summary>
public partial class MainMenu : Control
{
	/// <summary>「开始循环」进入的游戏主场景路径。</summary>
	[Export] public string GameScenePath { get; set; } = "res://Scenes/Root/GameScene.tscn";

	/// <summary>全屏教室背景，入场后做极缓慢的推拉，让静止画面有一点呼吸感。</summary>
	[Export] public TextureRect Background { get; set; }

	/// <summary>标题与按钮的整体容器，入场时上浮淡入。</summary>
	[Export] public Control Content { get; set; }

	/// <summary>两个按钮所在的容器，比标题稍晚浮现。</summary>
	[Export] public Control MenuButtons { get; set; }

	/// <summary>「开始循环」按钮，同时是默认焦点。</summary>
	[Export] public Button StartButton { get; set; }

	/// <summary>「退出循环」按钮。</summary>
	[Export] public Button QuitButton { get; set; }

	/// <summary>右下角的吉祥物装饰，比标题稍晚上浮淡入，之后在角落里极缓慢地浮动。</summary>
	[Export] public TextureRect Mascot { get; set; }

	/// <summary>整体上浮淡入的时长（秒）。</summary>
	private const float RevealSeconds = 0.9f;

	/// <summary>淡入时内容上浮的距离（像素）。</summary>
	private const float RevealRise = 28f;

	/// <summary>按钮比标题晚浮现的时间（秒）。</summary>
	private const float ButtonsDelay = 0.24f;

	/// <summary>按钮自身淡入的时长（秒）。</summary>
	private const float ButtonsRevealSeconds = 0.6f;

	/// <summary>背景推近一次所需的时间（秒）。</summary>
	private const float DriftSeconds = 22f;

	/// <summary>背景推近的最大缩放倍数。</summary>
	private const float DriftScale = 1.045f;

	/// <summary>吉祥物比标题晚浮现的时间（秒）。</summary>
	private const float MascotDelay = 0.35f;

	/// <summary>吉祥物自身淡入并上浮的时长（秒）。</summary>
	private const float MascotRevealSeconds = 0.85f;

	/// <summary>吉祥物入场时上浮的距离（像素）。</summary>
	private const float MascotRise = 24f;

	/// <summary>吉祥物呼吸时上浮的幅度（像素）。</summary>
	private const float MascotBreath = 5f;

	/// <summary>吉祥物上浮一次所需的时间（秒），一个完整呼吸约 5 秒。</summary>
	private const float MascotBreathSeconds = 2.5f;

	/// <summary>已经开始切场景 / 退出，用于挡住重复点击。</summary>
	private bool _leaving;

	private ScreenFade Fade => GetNodeOrNull<ScreenFade>("/root/ScreenFade");

	public override async void _Ready()
	{
		if (Background == null || Content == null || MenuButtons == null
			|| StartButton == null || QuitButton == null || Mascot == null)
		{
			GD.PushError("MainMenu: 场景节点引用不完整，主菜单不可用");
			return;
		}
		if (Background.Texture == null)
		{
			GD.PushError("MainMenu: 背景贴图缺失，请检查 res://ArtRes/Backgrounds/Classroom/classroom.png");
			return;
		}
		if (Mascot.Texture == null)
		{
			GD.PushError("MainMenu: 吉祥物贴图缺失，请检查 res://ArtRes/UI/mascot.png");
			return;
		}
		if (AudioManager.Instance == null)
		{
			GD.PushError("MainMenu: AudioManager 未就绪，按钮音效不会播放");
		}
		if (!ResourceLoader.Exists(GameScenePath))
		{
			GD.PushError($"MainMenu: 找不到游戏主场景 {GameScenePath}");
		}

		// 默认焦点落在「开始循环」上；先聚焦再接线，免得进场就响一声悬浮音。
		StartButton.GrabFocus();

		StartButton.Pressed += OnStartPressed;
		QuitButton.Pressed += OnQuitPressed;
		StartButton.MouseEntered += PlayHover;
		StartButton.FocusEntered += PlayHover;
		QuitButton.MouseEntered += PlayHover;
		QuitButton.FocusEntered += PlayHover;

		Background.Resized += CenterBackgroundPivot;
		CenterBackgroundPivot();
		StartBackgroundDrift();

		// 先让内容完全透明，等黑幕退掉再浮现，避免淡入过程中先闪一下完整版式。
		Content.Modulate = new Color(1f, 1f, 1f, 0f);
		MenuButtons.Modulate = Colors.Transparent;
		Mascot.Modulate = new Color(1f, 1f, 1f, 0f);

		if (Fade != null) await Fade.FadeInAsync();
		await RevealAsync();
	}

	/// <summary>标题上浮淡入，按钮接着跟上，吉祥物最后在角落里浮现。</summary>
	private async Task RevealAsync()
	{
		Vector2 restingPosition = Content.Position;
		Content.Position = restingPosition + new Vector2(0f, RevealRise);

		Vector2 mascotRestingPosition = Mascot.Position;
		Mascot.Position = mascotRestingPosition + new Vector2(0f, MascotRise);

		Tween tween = CreateTween();
		tween.SetParallel(true);
		tween.TweenProperty(Content, "modulate:a", 1f, RevealSeconds)
			.SetTrans(Tween.TransitionType.Cubic)
			.SetEase(Tween.EaseType.Out);
		tween.TweenProperty(Content, "position", restingPosition, RevealSeconds)
			.SetTrans(Tween.TransitionType.Cubic)
			.SetEase(Tween.EaseType.Out);
		tween.TweenProperty(MenuButtons, "modulate:a", 1f, ButtonsRevealSeconds)
			.SetDelay(ButtonsDelay)
			.SetTrans(Tween.TransitionType.Quad)
			.SetEase(Tween.EaseType.Out);
		tween.TweenProperty(Mascot, "modulate:a", 1f, MascotRevealSeconds)
			.SetDelay(MascotDelay)
			.SetTrans(Tween.TransitionType.Cubic)
			.SetEase(Tween.EaseType.Out);
		tween.TweenProperty(Mascot, "position", mascotRestingPosition, MascotRevealSeconds)
			.SetDelay(MascotDelay)
			.SetTrans(Tween.TransitionType.Cubic)
			.SetEase(Tween.EaseType.Out);

		await ToSignal(tween, Tween.SignalName.Finished);
		StartMascotBreath();
	}

	/// <summary>入场结束后让吉祥物在角落里极缓慢地上下浮动，每个循环都回到原点，不会越飘越偏。</summary>
	private void StartMascotBreath()
	{
		Vector2 restingPosition = Mascot.Position;

		Tween tween = CreateTween();
		tween.SetLoops();
		tween.TweenProperty(Mascot, "position", restingPosition + new Vector2(0f, -MascotBreath), MascotBreathSeconds)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.InOut);
		tween.TweenProperty(Mascot, "position", restingPosition, MascotBreathSeconds)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.InOut);
	}

	/// <summary>鼠标移入按钮或焦点落上去时，播一声轻响。</summary>
	private void PlayHover()
	{
		AudioManager.Instance?.PlayHover();
	}

	/// <summary>淡出后切到游戏主场景；淡出期间按钮全部禁用。</summary>
	private async void OnStartPressed()
	{
		if (_leaving) return;
		_leaving = true;
		AudioManager.Instance?.PlayClick();
		SetButtonsEnabled(false);

		if (Fade != null) await Fade.FadeOutAsync();
		if (!IsInstanceValid(this)) return;

		Error error = GetTree().ChangeSceneToFile(GameScenePath);
		if (error == Error.Ok) return;

		GD.PushError($"MainMenu: 切换到 {GameScenePath} 失败（{error}）");
		_leaving = false;
		SetButtonsEnabled(true);
		StartButton.GrabFocus();
		if (Fade != null) await Fade.FadeInAsync();
	}

	/// <summary>淡出后关闭游戏。</summary>
	private async void OnQuitPressed()
	{
		if (_leaving) return;
		_leaving = true;
		AudioManager.Instance?.PlayClick();
		SetButtonsEnabled(false);

		if (Fade != null) await Fade.FadeOutAsync();
		if (!IsInstanceValid(this)) return;

		GetTree().Quit();
	}

	/// <summary>切换期间锁住两个按钮，避免重复触发。</summary>
	private void SetButtonsEnabled(bool enabled)
	{
		StartButton.Disabled = !enabled;
		QuitButton.Disabled = !enabled;
	}

	/// <summary>把缩放支点放到画面中心，推近时四条边都不会露出底色。</summary>
	private void CenterBackgroundPivot()
	{
		Background.PivotOffset = Background.Size * 0.5f;
	}

	/// <summary>背景缓慢推近再退回，无限循环。</summary>
	private void StartBackgroundDrift()
	{
		Tween tween = CreateTween();
		tween.SetLoops();
		tween.TweenProperty(Background, "scale", new Vector2(DriftScale, DriftScale), DriftSeconds)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.InOut);
		tween.TweenProperty(Background, "scale", Vector2.One, DriftSeconds)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.InOut);
	}
}

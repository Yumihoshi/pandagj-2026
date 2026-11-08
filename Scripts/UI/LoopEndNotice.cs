using Godot;
using System.Threading.Tasks;

/// <summary>17:30 终末提示：爆炸 1 / 2 快速轮切，定格到爆炸 3 后震屏一次。</summary>
public partial class LoopEndNotice : CanvasLayer
{
	/// <summary>每张轮切帧的停留时长。</summary>
	private const float FlickerHold = 0.07f;
	/// <summary>爆炸 1 / 2 轮切的切换次数。</summary>
	private const int FlickerCount = 6;
	private const float ShakeDuration = 0.45f;
	private const float ShakeAmplitude = 26f;
	private const float HoldAfterShake = 1.0f;
	private const float FadeOutDuration = 0.3f;

	[Export] public Texture2D Explosion1 { get; set; }
	[Export] public Texture2D Explosion2 { get; set; }
	[Export] public Texture2D Explosion3 { get; set; }

	/// <summary>爆炸音效，在定格到第 3 张时播放。</summary>
	[Export] public AudioStream ExplosionSfx { get; set; }

	private TextureRect _frame;

	public override void _Ready()
	{
		if (Explosion1 == null || Explosion2 == null || Explosion3 == null)
		{
			GD.PushError("LoopEndNotice: 爆炸贴图未配置");
		}
		if (ExplosionSfx == null)
		{
			GD.PushError("LoopEndNotice: 爆炸音效未配置");
		}

		_frame = GetNode<TextureRect>("%Frame");
		HideAll();
	}

	/// <summary>播放终末爆炸（约 2 秒），结束后画面已隐藏。</summary>
	public async Task PlayAsync()
	{
		_frame.Texture = Explosion1;
		_frame.Modulate = new Color(1, 1, 1, 1);
		_frame.Show();
		Offset = Vector2.Zero;

		// 先在爆炸 1 / 2 之间快速轮切，制造冲击闪光。
		for (int i = 0; i < FlickerCount; i++)
		{
			await ToSignal(GetTree().CreateTimer(FlickerHold), SceneTreeTimer.SignalName.Timeout);
			_frame.Texture = i % 2 == 0 ? Explosion2 : Explosion1;
		}

		// 定格在爆炸 3，并震屏一次。
		_frame.Texture = Explosion3;
		if (AudioManager.Instance == null)
		{
			GD.PushError("LoopEndNotice: AudioManager 未就绪，爆炸音效无法播放");
		}
		else
		{
			AudioManager.Instance.PlaySfxStream(ExplosionSfx);
		}
		await ShakeAsync();
		await ToSignal(GetTree().CreateTimer(HoldAfterShake), SceneTreeTimer.SignalName.Timeout);

		Tween fade = CreateTween();
		fade.TweenProperty(_frame, "modulate:a", 0f, FadeOutDuration);
		await ToSignal(fade, Tween.SignalName.Finished);
		HideAll();
	}

	/// <summary>立即隐藏。</summary>
	public void HideAll()
	{
		Offset = Vector2.Zero;
		if (_frame == null) return;

		_frame.Hide();
		_frame.Modulate = new Color(1, 1, 1, 0);
	}

	/// <summary>震屏：整个提示层做一次幅度衰减的随机偏移。</summary>
	private async Task ShakeAsync()
	{
		Tween shake = CreateTween();
		shake.TweenMethod(Callable.From<float>(progress =>
		{
			float amplitude = ShakeAmplitude * (1f - progress);
			Offset = new Vector2((float)GD.RandRange(-amplitude, amplitude), (float)GD.RandRange(-amplitude, amplitude));
		}), 0f, 1f, ShakeDuration);

		await ToSignal(shake, Tween.SignalName.Finished);
		Offset = Vector2.Zero;
	}
}

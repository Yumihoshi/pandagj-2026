using Godot;

/// <summary>17:30 终末提示：闪白与「轰———」。</summary>
public partial class LoopEndNotice : CanvasLayer
{
	private ColorRect _flash;
	private Label _text;
	private Tween _tween;

	public override void _Ready()
	{
		_flash = GetNode<ColorRect>("%Flash");
		_text = GetNode<Label>("%Text");
		HideAll();
	}

	/// <summary>播放约 2 秒的爆炸提示。</summary>
	public void Show()
	{
		_tween?.Kill();
		_flash.Show();
		_text.Show();
		_flash.Modulate = new Color(1, 1, 1, 0);
		_text.Modulate = new Color(1, 1, 1, 0);

		_tween = CreateTween();
		_tween.TweenProperty(_flash, "modulate:a", 0.8f, 0.15f);
		_tween.TweenProperty(_flash, "modulate:a", 0.0f, 0.35f);
		_tween.Parallel().TweenProperty(_text, "modulate:a", 1.0f, 0.1f);
		_tween.TweenInterval(1.2f);
		_tween.TweenProperty(_text, "modulate:a", 0.0f, 0.3f);
		_tween.TweenCallback(Callable.From(() =>
		{
			_flash.Hide();
			_text.Hide();
		}));
	}

	/// <summary>立即隐藏。</summary>
	public void HideAll()
	{
		_tween?.Kill();
		_flash.Hide();
		_text.Hide();
	}
}

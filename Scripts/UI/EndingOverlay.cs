using Godot;

/// <summary>结局黑屏：真结局尾声、The End、失败黑屏。</summary>
public partial class EndingOverlay : CanvasLayer
{
	private ColorRect _black;
	private Label _theEnd;
	private Label _thanks;
	private Tween _tween;

	public override void _Ready()
	{
		_black = GetNode<ColorRect>("%Black");
		_theEnd = GetNode<Label>("%TheEnd");
		_thanks = GetNode<Label>("%Thanks");
		HideAll();
	}

	/// <summary>真结局结束：保持黑屏，显示 The End。</summary>
	public void ShowTheEnd()
	{
		_black.Show();
		_black.MouseFilter = Control.MouseFilterEnum.Stop;
		_black.Modulate = new Color(1, 1, 1, 1);
		_theEnd.Show();
		_thanks.Show();
	}

	/// <summary>失败：黑屏淡入。</summary>
	public void ShowFail()
	{
		_black.Show();
		_black.MouseFilter = Control.MouseFilterEnum.Ignore;
		FadeTo(1.0f, 0.6f);
	}

	/// <summary>隐藏黑屏与文字；循环重置时调用。</summary>
	public void HideAll()
	{
		_tween?.Kill();
		_black.Hide();
		_black.Modulate = new Color(1, 1, 1, 0);
		_black.MouseFilter = Control.MouseFilterEnum.Ignore;
		_theEnd.Hide();
		_thanks.Hide();
	}

	private void FadeTo(float target, float duration)
	{
		_tween?.Kill();
		_tween = CreateTween();
		_tween.TweenProperty(_black, "modulate:a", target, duration);
	}
}

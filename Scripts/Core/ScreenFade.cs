using Godot;
using System.Threading.Tasks;

/// <summary>全屏黑幕过渡。所有切换路径共用这一份实现。</summary>
public partial class ScreenFade : CanvasLayer
{
	private const float Duration = 0.25f;
	private ColorRect _rect;

	public override void _Ready()
	{
		Layer = 100;
		_rect = new ColorRect
		{
			Color = new Color(0f, 0f, 0f, 0f),
			MouseFilter = Control.MouseFilterEnum.Ignore,
			Visible = false,
		};
		_rect.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		AddChild(_rect);
	}

	/// <summary>立刻铺满黑幕，用于启动时先遮住第一帧。</summary>
	public void SetOpaque()
	{
		_rect.Visible = true;
		_rect.Color = new Color(0f, 0f, 0f, 1f);
		_rect.MouseFilter = Control.MouseFilterEnum.Stop;
	}

	public async Task FadeOutAsync()
	{
		_rect.Visible = true;
		_rect.MouseFilter = Control.MouseFilterEnum.Stop;
		Tween tween = CreateTween();
		tween.TweenProperty(_rect, "color:a", 1f, Duration);
		await ToSignal(tween, Tween.SignalName.Finished);
	}

	public async Task FadeInAsync()
	{
		Tween tween = CreateTween();
		tween.TweenProperty(_rect, "color:a", 0f, Duration);
		await ToSignal(tween, Tween.SignalName.Finished);
		_rect.Visible = false;
		_rect.MouseFilter = Control.MouseFilterEnum.Ignore;
	}
}

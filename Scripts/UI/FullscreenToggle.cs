using Godot;

/// <summary>
/// 全局 F11 全屏切换：在任意场景中把窗口在全屏与窗口化之间切换。
/// </summary>
public partial class FullscreenToggle : Node
{
	private static readonly Vector2I WindowedSize = new(1600, 900);

	public override void _UnhandledKeyInput(InputEvent @event)
	{
		if (@event is not InputEventKey { Pressed: true, Echo: false } key || key.Keycode != Key.F11)
		{
			return;
		}

		bool isFullscreen = DisplayServer.WindowGetMode() is
			DisplayServer.WindowMode.Fullscreen or DisplayServer.WindowMode.ExclusiveFullscreen;

		if (isFullscreen)
		{
			DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
			DisplayServer.WindowSetSize(WindowedSize);
			CenterWindow(WindowedSize);
		}
		else
		{
			DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen);
		}

		GetViewport().SetInputAsHandled();
	}

	/// <summary>把窗口移到当前屏幕可用区域的中央。</summary>
	private static void CenterWindow(Vector2I size)
	{
		Rect2I usable = DisplayServer.ScreenGetUsableRect(DisplayServer.WindowGetCurrentScreen());
		var offset = new Vector2I(
			Mathf.Max(0, (usable.Size.X - size.X) / 2),
			Mathf.Max(0, (usable.Size.Y - size.Y) / 2));
		DisplayServer.WindowSetPosition(usable.Position + offset);
	}
}

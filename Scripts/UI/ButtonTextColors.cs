using Godot;

/// <summary>
/// 按钮正文颜色的统一入口：把同一个颜色写到普通 / 悬浮 / 聚焦 / 按下各状态，
/// 否则按钮一旦获得焦点（列表默认聚焦首项、菜单项悬浮即聚焦），引擎会改用主题的
/// font_focus_color / font_hover_color（近白），高亮色就被盖掉了。
/// </summary>
public static class ButtonTextColors
{
	private static readonly string[] States =
	{
		"font_color",
		"font_hover_color",
		"font_focus_color",
		"font_pressed_color",
		"font_hover_pressed_color"
	};

	/// <summary>让按钮在任何交互状态下的正文都用 <paramref name="color"/>。</summary>
	public static void Apply(Button button, Color color)
	{
		if (button == null)
		{
			GD.PushError("ButtonTextColors.Apply: button 为空");
			return;
		}

		foreach (string state in States)
		{
			button.AddThemeColorOverride(state, color);
		}
	}
}

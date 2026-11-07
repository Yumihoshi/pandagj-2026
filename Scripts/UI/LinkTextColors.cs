using System;
using System.Collections.Generic;
using Godot;

/// <summary>
/// 正文里可点击高亮链接的统一样式：常驻下划线，鼠标悬浮时换成暖色的悬浮色。
/// 两种颜色与剧本一一对应（黄 = 未收集 / 未解锁，灰 = 已收集 / 已解锁）。
/// </summary>
public static class LinkTextColors
{
	/// <summary>未收集 / 未解锁的高亮色，对应剧本里的 [color=#ffd866]。</summary>
	public const string HighlightHtml = "ffd866";

	/// <summary>已收集 / 已解锁的灰色，对应剧本里的 [color=#8a8a8a]。</summary>
	public const string CollectedHtml = "8a8a8a";

	/// <summary>下划线不透明度（百分比）；默认 50 在深色底上偏淡，80 清晰又不显粗重。</summary>
	private const int UnderlineAlpha = 80;

	/// <summary>
	/// 基础色 → 悬浮色：未收集的黄字换成偏橙的暖色（不变灰），已收集的灰字略微提亮，
	/// 都保持与深色对话框 / 面板底色的对比度。
	/// </summary>
	private static readonly Dictionary<string, string> HoverHtmlColors = new(StringComparer.OrdinalIgnoreCase)
	{
		{ HighlightHtml, "ffb03a" },
		{ CollectedHtml, "a9a9a9" }
	};

	/// <summary>让正文里的 [url] 链接常驻下划线，并把下划线调得清楚一些。</summary>
	public static void ApplyUnderline(RichTextLabel label)
	{
		if (label == null)
		{
			GD.PushError("LinkTextColors.ApplyUnderline: label 为空");
			return;
		}

		label.MetaUnderlined = true;
		label.AddThemeConstantOverride("underline_alpha", UnderlineAlpha);
	}

	/// <summary>
	/// 把 <paramref name="bbcode"/> 里 <paramref name="meta"/> 指向的链接（可能不止一处）换成悬浮色；
	/// 找不到链接或颜色不在映射表里时原样返回。
	/// </summary>
	public static string RecolorLinkForHover(string bbcode, string meta)
	{
		if (string.IsNullOrEmpty(bbcode) || string.IsNullOrEmpty(meta)) return bbcode;

		string openTag = $"[url={meta}]";
		string result = bbcode;
		int searchFrom = 0;

		while (true)
		{
			int linkStart = result.IndexOf(openTag, searchFrom, StringComparison.Ordinal);
			if (linkStart < 0) return result;

			int contentStart = linkStart + openTag.Length;
			int linkEnd = result.IndexOf("[/url]", contentStart, StringComparison.Ordinal);
			if (linkEnd < 0) return result;

			// 同一条链接在正文里可能出现多次，颜色标签等长替换，索引不会错位。
			searchFrom = linkEnd + "[/url]".Length;

			// 链接正文是 [color=#xxxxxx]…[/color]：只换起始标签里的色值，正文结构不变。
			int colorStart = result.IndexOf("[color=#", contentStart, StringComparison.Ordinal);
			if (colorStart < 0 || colorStart > linkEnd) continue;

			int colorTagEnd = result.IndexOf(']', colorStart);
			if (colorTagEnd < 0 || colorTagEnd > linkEnd) continue;

			string baseHtml = result[(colorStart + "[color=#".Length)..colorTagEnd];
			if (!HoverHtmlColors.TryGetValue(baseHtml, out string hoverHtml)) continue;

			result = string.Concat(result[..colorStart], $"[color=#{hoverHtml}]", result[(colorTagEnd + 1)..]);
		}
	}
}

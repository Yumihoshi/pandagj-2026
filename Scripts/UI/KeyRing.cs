using Godot;
using System.Collections.Generic;

/// <summary>左上角手机旁边的钥匙 HUD：持有钥匙时显示图标，鼠标悬浮时显示这把钥匙开哪。</summary>
public partial class KeyRing : Control
{
	/// <summary>单把钥匙的图标格子边长。</summary>
	private const float SlotSize = 48.0f;

	/// <summary>两把钥匙图标之间的间距。</summary>
	private const float SlotGap = 12.0f;

	/// <summary>悬浮文字与钥匙图标下边缘的间距。</summary>
	private const float TooltipGap = 8.0f;

	/// <summary>钥匙图标与悬浮文字共用的金色。</summary>
	private static readonly Color KeyColor = Color.FromHtml("ffd866");

	/// <summary>全部钥匙：道具 id → 悬浮文字。全局只有这两把。</summary>
	private static readonly (string ItemId, string Label)[] Keys =
	{
		("office_key", "办公室抽屉的钥匙"),
		("equipment_key", "器材准备室的钥匙"),
	};

	private StyleBoxFlat _slotStyle;
	private PanelContainer _tooltip;
	private Label _tooltipLabel;

	/// <summary>与 Keys 一一对应的持有状态。</summary>
	private readonly List<bool> _owned = new();

	/// <summary>鼠标悬浮的钥匙在 Keys 里的下标；-1 表示没有。</summary>
	private int _hovered = -1;

	/// <summary>手机等界面是否允许显示钥匙栏；实际可见性还要叠加「持有钥匙」。</summary>
	private bool _hudVisible = true;

	public override void _Ready()
	{
		MouseFilter = MouseFilterEnum.Stop;
		BuildSlotStyle();
		BuildTooltip();
		MouseExited += ClearHover;
		Refresh();
	}

	/// <summary>手机打开 / 循环终末时隐藏；关闭后再按持有情况决定是否显示。</summary>
	public void SetHudVisible(bool visible)
	{
		_hudVisible = visible;
		UpdateVisibility();
	}

	/// <summary>按当前背包重算持有状态、重绘图标并刷新可见性。</summary>
	public void Refresh()
	{
		if (GameState.Instance == null)
		{
			GD.PushError("KeyRing.Refresh: GameState 未就绪");
			Hide();
			return;
		}

		_owned.Clear();
		foreach (var key in Keys)
		{
			_owned.Add(GameState.Instance.HasItem(key.ItemId));
		}

		if (_hovered >= 0 && !IsOwned(_hovered))
		{
			ClearHover();
		}

		UpdateVisibility();
		QueueRedraw();
	}

	private void UpdateVisibility()
	{
		Visible = _hudVisible && HasAnyOwned();
		if (!Visible) ClearHover();
	}

	private bool HasAnyOwned()
	{
		foreach (bool owned in _owned)
		{
			if (owned) return true;
		}
		return false;
	}

	private bool IsOwned(int keyIndex)
	{
		return keyIndex >= 0 && keyIndex < _owned.Count && _owned[keyIndex];
	}

	/// <summary>持有钥匙在 HUD 里从左到右依次占的槽位。</summary>
	private List<int> GetVisibleKeys()
	{
		var visible = new List<int>();
		for (int i = 0; i < Keys.Length; i++)
		{
			if (IsOwned(i)) visible.Add(i);
		}
		return visible;
	}

	private static Rect2 GetSlotRect(int slot)
	{
		return new Rect2(new Vector2(slot * (SlotSize + SlotGap), 0.0f), new Vector2(SlotSize, SlotSize));
	}

	public override void _GuiInput(InputEvent @event)
	{
		if (@event is not InputEventMouseMotion motion) return;

		int hovered = -1;
		List<int> visible = GetVisibleKeys();
		for (int slot = 0; slot < visible.Count; slot++)
		{
			if (GetSlotRect(slot).HasPoint(motion.Position))
			{
				hovered = visible[slot];
				break;
			}
		}

		if (hovered < 0)
		{
			ClearHover();
			return;
		}
		ShowHover(hovered);
	}

	private void ShowHover(int keyIndex)
	{
		if (_hovered == keyIndex) return;
		_hovered = keyIndex;

		_tooltipLabel.Text = Keys[keyIndex].Label;
		_tooltip.ResetSize();

		int slot = GetVisibleKeys().IndexOf(keyIndex);
		float centerX = slot * (SlotSize + SlotGap) + SlotSize / 2.0f;
		_tooltip.Position = new Vector2(centerX - _tooltip.Size.X / 2.0f, SlotSize + TooltipGap);
		_tooltip.Show();
	}

	private void ClearHover()
	{
		if (_hovered < 0) return;
		_hovered = -1;
		_tooltip.Hide();
	}

	private void BuildSlotStyle()
	{
		_slotStyle = new StyleBoxFlat
		{
			BgColor = new Color(0.0470588f, 0.0470588f, 0.0705882f, 0.62f),
			BorderColor = new Color(1.0f, 1.0f, 1.0f, 0.16f),
			BorderWidthLeft = 1,
			BorderWidthTop = 1,
			BorderWidthRight = 1,
			BorderWidthBottom = 1,
			CornerRadiusTopLeft = 10,
			CornerRadiusTopRight = 10,
			CornerRadiusBottomRight = 10,
			CornerRadiusBottomLeft = 10,
		};
	}

	private void BuildTooltip()
	{
		var style = new StyleBoxFlat
		{
			BgColor = new Color(0.0470588f, 0.0470588f, 0.0705882f, 0.92f),
			BorderColor = new Color(1.0f, 1.0f, 1.0f, 0.24f),
			BorderWidthLeft = 1,
			BorderWidthTop = 1,
			BorderWidthRight = 1,
			BorderWidthBottom = 1,
			CornerRadiusTopLeft = 8,
			CornerRadiusTopRight = 8,
			CornerRadiusBottomRight = 8,
			CornerRadiusBottomLeft = 8,
			ContentMarginLeft = 12.0f,
			ContentMarginTop = 6.0f,
			ContentMarginRight = 12.0f,
			ContentMarginBottom = 6.0f,
		};

		_tooltipLabel = new Label
		{
			Text = "",
			MouseFilter = MouseFilterEnum.Ignore,
		};
		_tooltipLabel.AddThemeFontSizeOverride("font_size", 16);

		_tooltip = new PanelContainer
		{
			MouseFilter = MouseFilterEnum.Ignore,
			Visible = false,
		};
		_tooltip.AddThemeStyleboxOverride("panel", style);
		_tooltip.AddChild(_tooltipLabel);
		AddChild(_tooltip);
	}

	public override void _Draw()
	{
		List<int> visible = GetVisibleKeys();
		for (int slot = 0; slot < visible.Count; slot++)
		{
			Vector2 origin = GetSlotRect(slot).Position;
			DrawStyleBox(_slotStyle, GetSlotRect(slot));
			DrawKey(origin + new Vector2(1.0f, 1.0f), new Color(0.0f, 0.0f, 0.0f, 0.35f), false);
			DrawKey(origin, KeyColor, true);
		}
	}

	/// <summary>在 48x48 格子里画一把钥匙：左侧圆环把手 + 向右的钥匙杆 + 两枚齿。</summary>
	private void DrawKey(Vector2 origin, Color color, bool highlight)
	{
		DrawArc(origin + new Vector2(16.0f, 24.0f), 8.0f, 0.0f, Mathf.Tau, 28, color, 3.2f, true);
		DrawRect(new Rect2(origin + new Vector2(23.0f, 22.5f), new Vector2(18.0f, 3.0f)), color);
		DrawRect(new Rect2(origin + new Vector2(32.0f, 25.0f), new Vector2(3.0f, 7.0f)), color);
		DrawRect(new Rect2(origin + new Vector2(37.0f, 25.0f), new Vector2(3.0f, 5.0f)), color);

		if (highlight)
		{
			DrawCircle(origin + new Vector2(13.0f, 21.0f), 1.8f, new Color(1.0f, 1.0f, 1.0f, 0.75f));
		}
	}
}

using Godot;
using System.Collections.Generic;

/// <summary>大地图面板：按解锁状态刷新按钮可见性，把点击转成 LocationSelected。</summary>
public partial class MapScreen : Control
{
	/// <summary>地图上全部地点按钮，显式配置避免按名字查找。</summary>
	[Export] public MapLocationButton[] Buttons { get; set; } = System.Array.Empty<MapLocationButton>();

	[Signal] public delegate void LocationSelectedEventHandler(
		string locationId,
		PackedScene locationScene,
		int travelMinutes);

	public override void _Ready()
	{
		Validate();
		foreach (MapLocationButton button in Buttons)
		{
			if (button == null) continue;
			MapLocationButton captured = button;
			captured.Pressed += () => EmitSignal(
				SignalName.LocationSelected,
				captured.LocationId,
				captured.LocationScene,
				captured.TravelMinutes);
		}
		Visible = false;
	}

	/// <summary>按 GameState 的解锁集合刷新每颗按钮的可见性，并同步叠加的灰色建筑。</summary>
	public void Refresh()
	{
		int visibleCount = 0;
		foreach (MapLocationButton button in Buttons)
		{
			if (button == null) continue;
			bool unlocked = GameState.Instance.IsLocationUnlocked(button.LocationId);
			button.Visible = unlocked;
			if (button.Highlight != null) button.Highlight.Visible = unlocked;
			if (unlocked) visibleCount++;
		}

		if (visibleCount == 0)
		{
			GD.PushError("MapScreen.Refresh: 没有任何可见地点，地图死局");
		}
	}

	public void ShowMap()
	{
		Refresh();
		Visible = true;
	}

	public void HideMap()
	{
		Visible = false;
	}

	private void Validate()
	{
		if (Buttons.Length == 0)
		{
			GD.PushError("MapScreen: Buttons 为空");
			return;
		}

		HashSet<string> seen = new();
		foreach (MapLocationButton button in Buttons)
		{
			if (button == null)
			{
				GD.PushError("MapScreen: Buttons 里有 null 项");
				continue;
			}
			if (string.IsNullOrEmpty(button.LocationId))
			{
				GD.PushError($"MapScreen: 按钮 {button.Name} 的 LocationId 为空");
			}
			if (button.LocationScene == null)
			{
				GD.PushError($"MapScreen: 按钮 {button.Name} 的 LocationScene 为空");
			}
			if (button.TravelMinutes < 0)
			{
				GD.PushError($"MapScreen: 按钮 {button.Name} 的 TravelMinutes 不能为负");
			}
			if (!string.IsNullOrEmpty(button.LocationId) && !seen.Add(button.LocationId))
			{
				GD.PushError($"MapScreen: LocationId 重复 —— {button.LocationId}");
			}
			if (button.Highlight == null)
			{
				GD.PushError($"MapScreen: 按钮 {button.Name} 的 Highlight 为空，解锁后不会有灰色建筑");
			}
		}
	}
}

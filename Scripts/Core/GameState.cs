using Godot;
using System.Collections.Generic;

/// <summary>跨场景共享的游戏状态：解锁、背包、线索、时间步。</summary>
public partial class GameState : Node
{
	/// <summary>全局唯一实例，由 autoload 保证在 _Ready 时赋值。</summary>
	public static GameState Instance { get; private set; }

	/// <summary>已解锁的大地点 id，跨循环保留。</summary>
	public HashSet<string> UnlockedLocations { get; } = new();

	/// <summary>本轮持有的道具 id，每轮清空。</summary>
	public HashSet<string> Inventory { get; } = new();

	/// <summary>手机里记录的线索 id，跨循环保留（本规格只留位）。</summary>
	public HashSet<string> Clues { get; } = new();

	/// <summary>本轮的时间推进计数，每轮归零。</summary>
	public int TimeStep { get; private set; }

	[Signal] public delegate void LocationUnlockedEventHandler(string locationId);
	[Signal] public delegate void TimeAdvancedEventHandler(int timeStep);
	[Signal] public delegate void LoopResetEventHandler();

	public override void _Ready()
	{
		Instance = this;
	}

	/// <summary>把 MapConfig 里的初始解锁地点灌入集合，重复调用无副作用。</summary>
	public void SeedInitialUnlocks(MapConfig config)
	{
		if (config == null)
		{
			GD.PushError("GameState.SeedInitialUnlocks: MapConfig 为空");
			return;
		}

		foreach (string id in config.InitialUnlockedLocationIds)
		{
			if (string.IsNullOrEmpty(id))
			{
				GD.PushError("GameState.SeedInitialUnlocks: MapConfig 里有空的地点 id");
				continue;
			}
			UnlockedLocations.Add(id);
		}
	}

	/// <summary>解锁一个大地点；已解锁时什么都不做。</summary>
	public void UnlockLocation(string locationId)
	{
		if (string.IsNullOrEmpty(locationId))
		{
			GD.PushError("GameState.UnlockLocation: 地点 id 为空");
			return;
		}
		if (!UnlockedLocations.Add(locationId)) return;

		GD.Print($"[Map] 解锁地点 {locationId}");
		EmitSignal(SignalName.LocationUnlocked, locationId);
	}

	public bool IsLocationUnlocked(string locationId) => UnlockedLocations.Contains(locationId);

	public bool HasItem(string itemId) => Inventory.Contains(itemId);

	/// <summary>本轮拾取道具。道具系统接管后由它替换。</summary>
	public void AddItem(string itemId)
	{
		if (string.IsNullOrEmpty(itemId))
		{
			GD.PushError("GameState.AddItem: 道具 id 为空");
			return;
		}
		Inventory.Add(itemId);
		GD.Print($"[Item] 获得 {itemId}");
	}

	/// <summary>从大地图进入地点、或房间之间切换时推进一格时间。</summary>
	public void AdvanceTime()
	{
		TimeStep++;
		GD.Print($"[Time] TimeStep = {TimeStep}");
		EmitSignal(SignalName.TimeAdvanced, TimeStep);
	}

	/// <summary>循环重置：清掉每轮状态，保留解锁与线索。</summary>
	public void ResetLoop()
	{
		Inventory.Clear();
		TimeStep = 0;
		GD.Print("[Loop] 重置");
		EmitSignal(SignalName.LoopReset);
	}
}

using Godot;

/// <summary>地图上的一个地点按钮，自带解锁 id、要装载的场景与旅行耗时。</summary>
public partial class MapLocationButton : Button
{
	/// <summary>与 GameState.UnlockedLocations 对应的地点 id。</summary>
	[Export] public string LocationId { get; set; }

	/// <summary>点击后装载的地点场景。</summary>
	[Export] public PackedScene LocationScene { get; set; }

	/// <summary>前往该地点消耗的分钟数；0 表示免费。</summary>
	[Export] public int TravelMinutes { get; set; } = 30;
}

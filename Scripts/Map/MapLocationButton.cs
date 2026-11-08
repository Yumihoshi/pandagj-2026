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

	/// <summary>解锁后叠在地图上的灰色建筑高亮，跟按钮一起显隐。</summary>
	[Export] public CanvasItem Highlight { get; set; }

	private static readonly Color HoverTint = new(1.12f, 1.12f, 1.12f);

	public override void _Ready()
	{
		MouseEntered += OnMouseEntered;
		MouseExited += OnMouseExited;
	}

	private void OnMouseEntered()
	{
		Modulate = HoverTint;
		if (AudioManager.Instance != null) AudioManager.Instance.PlayHover();
	}

	private void OnMouseExited()
	{
		Modulate = Colors.White;
	}
}

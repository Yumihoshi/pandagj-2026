using Godot;

/// <summary>地图的静态配置：开局就可以去的大地点。</summary>
[GlobalClass]
public partial class MapConfig : Resource
{
	/// <summary>开局即在图上可见的地点 id。</summary>
	[Export] public string[] InitialUnlockedLocationIds { get; set; } = System.Array.Empty<string>();
}

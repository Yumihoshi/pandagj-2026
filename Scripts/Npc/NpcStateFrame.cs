using Godot;

/// <summary>NPC 在循环内某分钟之后的状态：立绘、是否在场。</summary>
[GlobalClass]
public partial class NpcStateFrame : Resource
{
	/// <summary>从循环内第几分钟开始生效；0 = 13:00，45 = 13:45。</summary>
	[Export] public int FromMinute { get; set; }

	/// <summary>状态 id，例如 "standing" / "sleeping" / "absent"。</summary>
	[Export] public string StateId { get; set; } = "";

	/// <summary>该状态的立绘；Visible 为 false 时可为空。</summary>
	[Export] public Texture2D Texture { get; set; }

	/// <summary>该帧是否在场。</summary>
	[Export] public bool Visible { get; set; } = true;

	/// <summary>该状态立绘的缩放；为 (0,0) 时沿用 Sprite 节点自身的缩放。</summary>
	[Export] public Vector2 SpriteScale { get; set; }

	/// <summary>该状态立绘的 offset（贴图内像素，同 Sprite2D.Offset）；只有设置了 SpriteScale 时才生效。</summary>
	[Export] public Vector2 SpriteOffset { get; set; }

	/// <summary>该状态头顶名字标签的 offset_top；为 0 时沿用节点默认位置。</summary>
	[Export] public float NameLabelTop { get; set; }
}

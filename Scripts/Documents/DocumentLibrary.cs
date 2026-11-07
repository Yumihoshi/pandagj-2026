using Godot;

/// <summary>启动时注册的全部文档；由 GameScene 灌入 GameState。</summary>
[GlobalClass]
public partial class DocumentLibrary : Resource
{
	[Export] public DocumentData[] Documents { get; set; } = System.Array.Empty<DocumentData>();
}

using Godot;

/// <summary>单篇可收集文档的静态内容。</summary>
[GlobalClass]
public partial class DocumentData : Resource
{
	/// <summary>全局唯一 id；对话里的 [url=doc:&lt;id&gt;] 使用它。</summary>
	[Export] public string Id { get; set; } = "";

	/// <summary>手机文档列表里显示的名字。</summary>
	[Export] public string Title { get; set; } = "";

	/// <summary>文档正文，允许多行与 BBCode。</summary>
	[Export(PropertyHint.MultilineText)] public string Body { get; set; } = "";
}

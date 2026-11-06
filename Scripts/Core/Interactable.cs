using Godot;

/// <summary>可交互物基类：按 F 触发 Interact()，由使用方连接 Interacted 信号。</summary>
public partial class Interactable : Area2D
{
	[Signal] public delegate void InteractedEventHandler();

	public override void _EnterTree()
	{
		AddToGroup("interactable");
	}

	public virtual void Interact()
	{
		EmitSignal(SignalName.Interacted);
	}
}

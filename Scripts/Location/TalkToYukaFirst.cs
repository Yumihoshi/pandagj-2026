using Godot;
using DialogueManagerRuntime;

/// <summary>
/// 一周目开场门禁：玩家还没跟指定 NPC 说过话时，房间里任何交互都只播提示对白，不执行本体逻辑。
/// 由 PlayerInteractor 在调用 Interact 之前通过 <see cref="TryInterceptAny"/> 拦截。
/// </summary>
public partial class TalkToYukaFirst : Node
{
	/// <summary>门禁节点注册的分组名，PlayerInteractor 按分组查找。</summary>
	public const string GroupName = "talk_to_yuka_first";

	/// <summary>要先说话的 NPC id；跟这个 NPC 本人交互不会被拦截。</summary>
	[Export] public string RequiredNpcId { get; set; } = "";

	/// <summary>拦截时播放的对白资源。</summary>
	[Export] public Resource PromptDialogue { get; set; }

	/// <summary>提示对白里要播放的 cue 名。</summary>
	[Export] public string PromptCue { get; set; } = "talk_first";

	/// <summary>门禁所属房间；只有本房间内的交互才会被拦截。</summary>
	private Room _room;

	public override void _EnterTree()
	{
		AddToGroup(GroupName);
	}

	public override void _Ready()
	{
		_room = GetParent<Room>();
		if (_room == null)
		{
			GD.PushError($"TalkToYukaFirst {Name}: 父节点不是 Room");
		}
		if (string.IsNullOrEmpty(RequiredNpcId))
		{
			GD.PushError($"TalkToYukaFirst {Name}: RequiredNpcId 为空");
		}
		if (PromptDialogue == null)
		{
			GD.PushError($"TalkToYukaFirst {Name}: PromptDialogue 为空");
		}

		GD.Print($"[Gate] {Name} 就绪：先跟 {RequiredNpcId} 说话（生效={IsArmed()}）");
	}

	/// <summary>对所有门禁节点尝试拦截；返回 true 表示目标交互已被拦下，调用方不要再执行本体逻辑。</summary>
	public static bool TryInterceptAny(SceneTree tree, Interactable target)
	{
		foreach (Node node in tree.GetNodesInGroup(GroupName))
		{
			if (node is TalkToYukaFirst gate && gate.TryIntercept(target)) return true;
		}
		return false;
	}

	/// <summary>目标属于本房间、不是要拜访的 NPC、且门禁生效时，播放提示对白并返回 true。</summary>
	public bool TryIntercept(Interactable target)
	{
		// 先按房间过滤，否则会把别的房间、地图出口等交互一起吞掉
		if (_room == null || !_room.IsAncestorOf(target)) return false;
		// 要找的 NPC 本人当然可以谈，不然玩家永远出不去
		if (target is Npc npc && npc.NpcId == RequiredNpcId) return false;
		if (!IsArmed()) return false;

		ShowPrompt(target);
		return true;
	}

	/// <summary>门禁是否生效：仅一周目，且还没跟目标 NPC 谈过话。</summary>
	private bool IsArmed()
	{
		if (GameState.Instance == null)
		{
			GD.PushError($"TalkToYukaFirst {Name}: GameState 未初始化");
			return false;
		}
		if (!GameState.Instance.IsFirstLoop()) return false;
		return !GameState.Instance.HasLoopFlag("yuka_briefed");
	}

	/// <summary>播放「先去找小优香说话」提示。</summary>
	private void ShowPrompt(Interactable target)
	{
		if (PromptDialogue == null) return; // 缺失已在 _Ready 报错

		GD.Print($"[Gate] 拦下与「{target.Name}」的交互：还没跟 {RequiredNpcId} 说过话");
		if (DialogueManager.ShowDialogueBalloon(PromptDialogue, PromptCue) == null)
		{
			GD.PushError($"TalkToYukaFirst {Name}: ShowDialogueBalloon 返回空");
		}
	}
}

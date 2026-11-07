using Godot;
using DialogueManagerRuntime;

/// <summary>办公室看门人：山田在办公的时段，玩家在办公室里碰任何东西都会被他请出办公室。</summary>
public partial class OfficeKickOut : Node
{
	/// <summary>看门人注册的分组名，供 PlayerInteractor 查找。</summary>
	public const string GroupName = "office_kick_out";

	/// <summary>山田午睡、可以偷钥匙的分钟区间起点（14:00）。</summary>
	private const int SleepFromMinute = 60;

	/// <summary>山田午睡的分钟区间终点，不含（15:00）。</summary>
	private const int SleepToMinute = 120;

	/// <summary>山田离开办公室去地下的分钟（17:00），此后没人赶人。</summary>
	private const int LeavesAtMinute = 240;

	/// <summary>被赶出去时走的门，负责黑幕与 5 分钟路程。</summary>
	[Export] public RoomDoor ExitDoor { get; set; }

	/// <summary>赶人时播的对白。</summary>
	[Export] public Resource KickDialogue { get; set; }

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
			GD.PushError($"OfficeKickOut {Name}: 父节点不是 Room");
		}
		if (ExitDoor == null)
		{
			GD.PushError($"OfficeKickOut {Name}: ExitDoor 为空");
		}
		if (KickDialogue == null)
		{
			GD.PushError($"OfficeKickOut {Name}: KickDialogue 为空");
		}
	}

	/// <summary>场上任意看门人拦下这次交互就返回 true，调用方不要再执行目标对象。</summary>
	public static bool TryInterceptAny(SceneTree tree, Interactable target)
	{
		foreach (Node node in tree.GetNodesInGroup(GroupName))
		{
			if (node is OfficeKickOut guard && guard.TryIntercept(target)) return true;
		}
		return false;
	}

	/// <summary>目标是办公室里的东西、山田又在办公时，赶人并返回 true。</summary>
	public bool TryIntercept(Interactable target)
	{
		// 只拦办公室里的东西：先判房间，否则门外的交互（地图出口、办公室门）也会被吞掉。
		if (_room == null || !_room.IsAncestorOf(target)) return false;
		if (target is RoomDoor || target is LocationExit) return false;
		if (!IsArmed()) return false;

		Kick();
		return true;
	}

	/// <summary>现在是不是「山田在办公室办公」的时段。</summary>
	private bool IsArmed()
	{
		if (GameState.Instance == null)
		{
			GD.PushError($"OfficeKickOut {Name}: GameState 未就绪");
			return false;
		}

		int minute = GameState.Instance.ElapsedMinutes;
		if (minute >= SleepFromMinute && minute < SleepToMinute) return false;
		return minute < LeavesAtMinute;
	}

	/// <summary>播赶人对白；把玩家送回入口要等剧本最后一句播完，由对白里的 <c>do guard.LeaveOffice()</c> 触发。</summary>
	private void Kick()
	{
		if (ExitDoor == null || KickDialogue == null)
		{
			GD.PushError($"OfficeKickOut {Name}: 缺少 ExitDoor 或 KickDialogue");
			return;
		}

		var states = new Godot.Collections.Array<Variant>
		{
			new Godot.Collections.Dictionary { { "guard", this } }
		};

		if (DialogueManager.ShowDialogueBalloon(KickDialogue, "start", states) == null)
		{
			GD.PushError($"OfficeKickOut {Name}: ShowDialogueBalloon 返回空");
		}
	}

	/// <summary>走门把玩家送回入口：对白播完后由剧本调用。</summary>
	public void LeaveOffice()
	{
		if (ExitDoor == null)
		{
			GD.PushError($"OfficeKickOut {Name}: ExitDoor 为空，无法把玩家送出办公室");
			return;
		}

		ExitDoor.Interact();
	}
}

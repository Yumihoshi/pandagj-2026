using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>地点场景根节点：管理房间互斥激活、玩家出生、出口信号。</summary>
public partial class LocationRoot : Node2D
{
	/// <summary>从地图进入时激活的房间。</summary>
	[Export] public Room StartingRoom { get; set; }

	/// <summary>循环重置时激活的房间（如教室座位所在的教室）；留空时用 StartingRoom。</summary>
	[Export] public Room LoopStartRoom { get; set; }

	/// <summary>本次载入是否来自循环开始，由 GameScene 在挂入场景树前设置。</summary>
	public bool IsLoopStart { get; set; }

	/// <summary>本地点唯一的玩家实例。</summary>
	[Export] public Player PlayerNode { get; set; }

	[Signal] public delegate void ExitRequestedEventHandler();

	private bool _transitioning;

	public override void _Ready()
	{
		if (StartingRoom == null)
		{
			GD.PushError($"LocationRoot {Name}: StartingRoom 为空");
			return;
		}
		if (PlayerNode == null || PlayerNode.Camera == null)
		{
			GD.PushError($"LocationRoot {Name}: PlayerNode 或它的 Camera 为空");
			return;
		}

		List<Room> rooms = FindAll<Room>(this);
		List<RoomDoor> doors = FindAll<RoomDoor>(this);
		List<LocationExit> exits = FindAll<LocationExit>(this);

		if (rooms.Count == 0)
		{
			GD.PushError($"LocationRoot {Name}: 一个 Room 都没有");
			return;
		}
		if (exits.Count != 1)
		{
			GD.PushError($"LocationRoot {Name}: LocationExit 数量为 {exits.Count}，应恰好 1 个");
			return;
		}

		List<Npc> npcs = FindAll<Npc>(this);
		HashSet<string> npcIds = new();
		foreach (Npc npc in npcs)
		{
			if (string.IsNullOrEmpty(npc.NpcId)) continue; // Npc 自己会报错
			if (!npcIds.Add(npc.NpcId))
			{
				GD.PushError($"LocationRoot {Name}: NpcId {npc.NpcId} 重复");
			}
		}

		foreach (Room room in rooms) room.SetActive(false);

		foreach (RoomDoor door in doors)
		{
			RoomDoor captured = door;
			captured.Interacted += () => _ = TransitionToRoomAsync(captured);
		}

		exits[0].Interacted += () => EmitSignal(SignalName.ExitRequested);

		Room startRoom = ResolveStartRoom();
		Marker2D spawn = ResolveStartSpawn(startRoom);
		ActivateRoom(startRoom, spawn);
		GD.Print($"[Map] 地点 {Name} 就绪，当前房间 {startRoom.Name}，落点 {spawn?.GlobalPosition}");
	}

	/// <summary>循环开始时用 LoopStartRoom（没配则用 StartingRoom），从地图进入时用 StartingRoom。</summary>
	private Room ResolveStartRoom()
	{
		return IsLoopStart && LoopStartRoom != null ? LoopStartRoom : StartingRoom;
	}

	/// <summary>循环开始用房间配的 FirstLoopSpawn（如教室座位）且留空时用 DefaultSpawn；从地图进入一律用 DefaultSpawn。</summary>
	private Marker2D ResolveStartSpawn(Room room)
	{
		return IsLoopStart && room.FirstLoopSpawn != null ? room.FirstLoopSpawn : room.DefaultSpawn;
	}

	private async Task TransitionToRoomAsync(RoomDoor door)
	{
		if (_transitioning) return;
		if (GameState.Instance == null)
		{
			GD.PushError($"LocationRoot {Name}: GameState 未就绪");
			return;
		}
		if (GameState.Instance.AdvanceTime(door.TravelMinutes)) return;

		_transitioning = true;

		ScreenFade fade = GetNode<ScreenFade>("/root/ScreenFade");
		await fade.FadeOutAsync();

		ActivateRoom(door.TargetRoom, door.TargetSpawn);

		await fade.FadeInAsync();
		_transitioning = false;
	}

	private void ActivateRoom(Room room, Marker2D spawn)
	{
		if (spawn == null)
		{
			GD.PushError($"LocationRoot {Name}: 房间 {room.Name} 缺少落点");
			return;
		}

		foreach (Room other in FindAll<Room>(this)) other.SetActive(other == room);
		PlayerNode.GlobalPosition = spawn.GlobalPosition;
		ApplyRoomBounds(room);
	}

	private void ApplyRoomBounds(Room room)
	{
		Rect2I bounds = room.CameraBounds;
		Vector2 origin = room.GlobalPosition;
		int left = (int)(origin.X + bounds.Position.X);
		int right = left + bounds.Size.X;

		Camera2D camera = PlayerNode.Camera;
		camera.LimitLeft = left;
		camera.LimitTop = (int)(origin.Y + bounds.Position.Y);
		camera.LimitRight = right;
		camera.LimitBottom = camera.LimitTop + bounds.Size.Y;
		camera.ResetSmoothing();

		PlayerNode.SetHorizontalBounds(left, right);
	}

	private static List<T> FindAll<T>(Node root) where T : Node
	{
		List<T> result = new();
		foreach (Node child in root.GetChildren())
		{
			if (child is T match) result.Add(match);
			result.AddRange(FindAll<T>(child));
		}
		return result;
	}
}

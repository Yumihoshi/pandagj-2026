using Godot;
using System.Collections.Generic;

/// <summary>跨场景共享的游戏状态：时钟、周目、解锁、背包、线索、NPC 本轮状态。</summary>
public partial class GameState : Node
{
	/// <summary>循环开始时的时间（分钟，从 0 点算）。13:00 = 780。</summary>
	public const int LoopStartMinuteOfDay = 13 * 60;

	/// <summary>循环结束时的时间（分钟，从 0 点算）。17:30 = 1050。</summary>
	public const int LoopEndMinuteOfDay = 17 * 60 + 30;

	/// <summary>一轮的分钟长度。270。</summary>
	public const int LoopDurationMinutes = LoopEndMinuteOfDay - LoopStartMinuteOfDay;

	/// <summary>全局唯一实例，由 autoload 保证在 _Ready 时赋值。</summary>
	public static GameState Instance { get; private set; }

	/// <summary>已解锁的大地点 id，同一进程内跨循环保留。</summary>
	public HashSet<string> UnlockedLocations { get; } = new();

	/// <summary>本轮持有的道具 id，每轮清空。</summary>
	public HashSet<string> Inventory { get; } = new();

	/// <summary>手机里记录的线索 id，同一进程内跨循环保留。</summary>
	public HashSet<string> Clues { get; } = new();

	/// <summary>本轮已经过的分钟数；0 = 13:00，270 = 17:30。</summary>
	public int ElapsedMinutes { get; private set; }

	/// <summary>当前进程内的周目序号；开局 1，每次 ResetLoop +1。</summary>
	public int LoopCount { get; private set; } = 1;

	/// <summary>是否已到 17:30；ResetLoop 后复位。</summary>
	public bool IsLoopEnded { get; private set; }

	/// <summary>本轮 NPC 状态覆盖：npcId → stateId。</summary>
	private readonly Dictionary<string, string> _npcLoopStates = new();

	/// <summary>本轮已选的一次性选项：npcId → staticId 集合。</summary>
	private readonly Dictionary<string, HashSet<string>> _npcUsedChoices = new();

	/// <summary>本轮剧情标记；每轮 ResetLoop 清空，跨循环不保留。</summary>
	private readonly HashSet<string> _loopFlags = new();

	/// <summary>新井分析完成的循环内分钟；-1 表示本轮没有开始分析。</summary>
	public int BombAnalysisReadyMinute { get; private set; } = -1;

	/// <summary>是否已进入真结局；DialogueBalloon 据此不做退出解锁。</summary>
	public bool IsTrueEnding { get; private set; }

	/// <summary>文档注册表：id → DocumentData。</summary>
	private readonly Dictionary<string, DocumentData> _documentsById = new();

	/// <summary>已获取文档，按获取顺序；跨循环保留。</summary>
	private readonly List<DocumentData> _acquiredDocuments = new();

	/// <summary>已获取文档 id，用于 O(1) 判重；跨循环保留。</summary>
	private readonly HashSet<string> _acquiredDocumentIds = new();

	/// <summary>已获取的文档，按获取顺序。</summary>
	public IReadOnlyList<DocumentData> AcquiredDocuments => _acquiredDocuments;

	/// <summary>手机记事本内容；跨循环保留，不落盘。</summary>
	public string NotebookText { get; set; } = "";

	[Signal] public delegate void LocationUnlockedEventHandler(string locationId);
	[Signal] public delegate void TimeChangedEventHandler(int elapsedMinutes);
	[Signal] public delegate void LoopEndReachedEventHandler();
	[Signal] public delegate void LoopResetEventHandler();
	[Signal] public delegate void LoopFlagsChangedEventHandler();
	[Signal] public delegate void EpilogueStartedEventHandler();
	[Signal] public delegate void TrueEndingReachedEventHandler();
	[Signal] public delegate void LoopFailedEventHandler();

	public override void _Ready()
	{
		Instance = this;
	}

	/// <summary>把 MapConfig 里的初始解锁地点灌入集合，重复调用无副作用。</summary>
	public void SeedInitialUnlocks(MapConfig config)
	{
		if (config == null)
		{
			GD.PushError("GameState.SeedInitialUnlocks: MapConfig 为空");
			return;
		}

		foreach (string id in config.InitialUnlockedLocationIds)
		{
			if (string.IsNullOrEmpty(id))
			{
				GD.PushError("GameState.SeedInitialUnlocks: MapConfig 里有空的地点 id");
				continue;
			}
			UnlockedLocations.Add(id);
		}
	}

	/// <summary>解锁一个大地点；已解锁时什么都不做。</summary>
	public void UnlockLocation(string locationId)
	{
		if (string.IsNullOrEmpty(locationId))
		{
			GD.PushError("GameState.UnlockLocation: 地点 id 为空");
			return;
		}
		if (!UnlockedLocations.Add(locationId)) return;

		GD.Print($"[Map] 解锁地点 {locationId}");
		EmitSignal(SignalName.LocationUnlocked, locationId);
	}

	public bool IsLocationUnlocked(string locationId) => UnlockedLocations.Contains(locationId);

	public bool HasItem(string itemId) => Inventory.Contains(itemId);

	/// <summary>本轮拾取道具。道具系统接管后由它替换。</summary>
	public void AddItem(string itemId)
	{
		if (string.IsNullOrEmpty(itemId))
		{
			GD.PushError("GameState.AddItem: 道具 id 为空");
			return;
		}
		Inventory.Add(itemId);
		GD.Print($"[Item] 获得 {itemId}");
	}

	/// <summary>手机里是否记录了某条线索。</summary>
	public bool HasClue(string clueId)
	{
		if (string.IsNullOrEmpty(clueId))
		{
			GD.PushError("GameState.HasClue: 线索 id 为空");
			return false;
		}
		return Clues.Contains(clueId);
	}

	/// <summary>把一条线索记进手机；跨循环保留，重复添加无副作用。</summary>
	public void AddClue(string clueId)
	{
		if (string.IsNullOrEmpty(clueId))
		{
			GD.PushError("GameState.AddClue: 线索 id 为空");
			return;
		}
		if (!Clues.Add(clueId)) return;

		GD.Print($"[Clue] 获得线索 {clueId}");
	}

	/// <summary>把 DocumentLibrary 里的文档灌入注册表；重复调用只重建注册表。</summary>
	public void SeedDocuments(DocumentLibrary library)
	{
		if (library == null)
		{
			GD.PushError("GameState.SeedDocuments: DocumentLibrary 为空");
			return;
		}
		if (library.Documents == null)
		{
			GD.PushError("GameState.SeedDocuments: Documents 为空");
			return;
		}

		_documentsById.Clear();
		foreach (DocumentData document in library.Documents)
		{
			if (document == null)
			{
				GD.PushError("GameState.SeedDocuments: Documents 里有 null 项");
				continue;
			}
			if (string.IsNullOrEmpty(document.Id))
			{
				GD.PushError($"GameState.SeedDocuments: {document.ResourcePath} 的 Id 为空");
				continue;
			}
			if (!_documentsById.TryAdd(document.Id, document))
			{
				GD.PushError($"GameState.SeedDocuments: 文档 id 重复 —— {document.Id}");
				continue;
			}
			if (string.IsNullOrEmpty(document.Title))
			{
				GD.PushError($"GameState.SeedDocuments: 文档 {document.Id} 的 Title 为空");
			}
			if (string.IsNullOrEmpty(document.Body))
			{
				GD.PushError($"GameState.SeedDocuments: 文档 {document.Id} 的 Body 为空");
			}
		}
	}

	/// <summary>获取一篇文档；已获取或 id 未注册时返回 false。</summary>
	public bool AcquireDocument(string documentId)
	{
		if (string.IsNullOrEmpty(documentId))
		{
			GD.PushError("GameState.AcquireDocument: 文档 id 为空");
			return false;
		}
		if (!_documentsById.TryGetValue(documentId, out DocumentData document))
		{
			GD.PushError($"GameState.AcquireDocument: 文档 {documentId} 没有在 DocumentLibrary 里注册");
			return false;
		}
		if (!_acquiredDocumentIds.Add(documentId)) return false;

		_acquiredDocuments.Add(document);
		GD.Print($"[Doc] 获取 {documentId}");
		return true;
	}

	/// <summary>是否已经获取过某篇文档。</summary>
	public bool HasDocument(string documentId)
	{
		if (string.IsNullOrEmpty(documentId))
		{
			GD.PushError("GameState.HasDocument: 文档 id 为空");
			return false;
		}
		return _acquiredDocumentIds.Contains(documentId);
	}

	/// <summary>本轮某个 NPC 的状态覆盖；没有时返回空字符串。</summary>
	public string GetNpcLoopState(string npcId)
	{
		if (string.IsNullOrEmpty(npcId))
		{
			GD.PushError("GameState.GetNpcLoopState: NPC id 为空");
			return "";
		}
		return _npcLoopStates.TryGetValue(npcId, out string stateId) ? stateId : "";
	}

	/// <summary>写入某个 NPC 的本轮状态覆盖。</summary>
	public void SetNpcLoopState(string npcId, string stateId)
	{
		if (string.IsNullOrEmpty(npcId) || string.IsNullOrEmpty(stateId))
		{
			GD.PushError("GameState.SetNpcLoopState: NPC id 或状态 id 为空");
			return;
		}
		_npcLoopStates[npcId] = stateId;
		GD.Print($"[Npc] {npcId} 本轮状态覆盖 -> {stateId}");
	}

	/// <summary>某个 NPC 的一次性选项是否已选过。</summary>
	public bool IsChoiceUsed(string npcId, string staticId)
	{
		if (string.IsNullOrEmpty(npcId) || string.IsNullOrEmpty(staticId))
		{
			GD.PushError("GameState.IsChoiceUsed: NPC id 或选项 id 为空");
			return false;
		}
		return _npcUsedChoices.TryGetValue(npcId, out HashSet<string> used) && used.Contains(staticId);
	}

	/// <summary>记录某个 NPC 的一次性选项已选。</summary>
	public void MarkChoiceUsed(string npcId, string staticId)
	{
		if (string.IsNullOrEmpty(npcId) || string.IsNullOrEmpty(staticId))
		{
			GD.PushError("GameState.MarkChoiceUsed: NPC id 或选项 id 为空");
			return;
		}
		if (!_npcUsedChoices.TryGetValue(npcId, out HashSet<string> used))
		{
			used = new HashSet<string>();
			_npcUsedChoices[npcId] = used;
		}
		used.Add(staticId);
		GD.Print($"[Npc] {npcId} 已选 {staticId}");
	}

	/// <summary>本轮是否设置了某个剧情标记。</summary>
	public bool HasLoopFlag(string flag)
	{
		if (string.IsNullOrEmpty(flag))
		{
			GD.PushError("GameState.HasLoopFlag: 标记为空");
			return false;
		}
		return _loopFlags.Contains(flag);
	}

	/// <summary>设置本轮剧情标记；重复设置无副作用，并通知按标记显隐的 NPC 刷新。</summary>
	public void SetLoopFlag(string flag)
	{
		if (string.IsNullOrEmpty(flag))
		{
			GD.PushError("GameState.SetLoopFlag: 标记为空");
			return;
		}
		if (!_loopFlags.Add(flag)) return;

		GD.Print($"[LoopFlag] {flag}");
		EmitSignal(SignalName.LoopFlagsChanged);
	}

	/// <summary>把炸药样本交给新井：记录两小时后的完成时间，并写入本轮标记。</summary>
	public void StartBombAnalysis()
	{
		BombAnalysisReadyMinute = ElapsedMinutes + 120;
		SetLoopFlag("shinji_analyzing");
		GD.Print($"[Bomb] 分析开始，{BombAnalysisReadyMinute} 分钟时完成");
	}

	/// <summary>移除本轮道具；不存在时无副作用。</summary>
	public void RemoveItem(string itemId)
	{
		if (string.IsNullOrEmpty(itemId))
		{
			GD.PushError("GameState.RemoveItem: 道具 id 为空");
			return;
		}
		if (Inventory.Remove(itemId)) GD.Print($"[Item] 失去 {itemId}");
	}

	/// <summary>由 yamada_underground.dialogue 调用：开始甜品店尾声。</summary>
	public void EnterEpilogue() => EmitSignal(SignalName.EpilogueStarted);

	/// <summary>由 yamada_underground.dialogue 调用：真结局结束。</summary>
	public void ReachTrueEnding()
	{
		IsTrueEnding = true;
		EmitSignal(SignalName.TrueEndingReached);
	}

	/// <summary>由 yamada_underground.dialogue 调用：本轮失败。</summary>
	public void FailLoop() => EmitSignal(SignalName.LoopFailed);

	/// <summary>推进 minutes 分钟；返回 true 表示本次推进触发或已处于终末。返回 true 时调用方必须中止后续动作（不装载场景 / 不切房间）。</summary>
	public bool AdvanceTime(int minutes)
	{
		if (IsLoopEnded) return true;
		if (minutes < 0)
		{
			GD.PushError("GameState.AdvanceTime: minutes 不能为负");
			return false;
		}
		if (minutes == 0) return false;

		long next = (long)ElapsedMinutes + minutes;
		ElapsedMinutes = next > LoopDurationMinutes ? LoopDurationMinutes : (int)next;
		GD.Print($"[Time] {GetClockText()} (+{minutes})");
		EmitSignal(SignalName.TimeChanged, ElapsedMinutes);

		if (ElapsedMinutes >= LoopDurationMinutes)
		{
			IsLoopEnded = true;
			EmitSignal(SignalName.LoopEndReached);
			return true;
		}

		return false;
	}

	/// <summary>循环重置：时间归零、周目 +1，清本轮标记与道具，保留解锁、线索、文档与记事本。</summary>
	public void ResetLoop()
	{
		ElapsedMinutes = 0;
		IsLoopEnded = false;
		IsTrueEnding = false;
		LoopCount++;
		BombAnalysisReadyMinute = -1;

		Inventory.Clear();
		_npcLoopStates.Clear();
		_npcUsedChoices.Clear();
		_loopFlags.Clear();

		GD.Print($"[Loop] 第 {LoopCount} 周目开始");
		EmitSignal(SignalName.TimeChanged, 0);
		EmitSignal(SignalName.LoopReset);
	}

	/// <summary>返回 "13:45" 形式的时钟文本。</summary>
	public string GetClockText()
	{
		int total = LoopStartMinuteOfDay + ElapsedMinutes;
		return $"{total / 60:00}:{total % 60:00}";
	}

	/// <summary>只读：本轮已过去的分钟数。</summary>
	public int GetElapsedMinutes() => ElapsedMinutes;

	/// <summary>只读：当前周目序号。</summary>
	public int GetLoopCount() => LoopCount;

	/// <summary>是否一周目；一周目固定锁用。</summary>
	public bool IsFirstLoop() => LoopCount == 1;
}

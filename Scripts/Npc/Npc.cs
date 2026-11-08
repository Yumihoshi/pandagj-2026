using Godot;
using System.Collections.Generic;
using DialogueManagerRuntime;

/// <summary>固定站位的 NPC：时间表状态、立绘切换、发起对话。</summary>
public partial class Npc : Interactable
{
	/// <summary>全局唯一 id，用于已选记录与本轮状态覆盖；建议 "地点.名字"。</summary>
	[Export] public string NpcId { get; set; } = "";

	/// <summary>显示在头顶的中文名字。</summary>
	[Export] public string DisplayName { get; set; } = "";

	/// <summary>这个 NPC 的对话资源。</summary>
	[Export] public Resource Dialogue { get; set; }

	/// <summary>按 FromMinute 升序的状态帧；首帧必须是 0（13:00）。</summary>
	[Export] public NpcStateFrame[] Frames { get; set; } = System.Array.Empty<NpcStateFrame>();

	/// <summary>立绘槽。</summary>
	[Export] public Sprite2D Sprite { get; set; }

	/// <summary>可选的在场条件：本轮标记 id；留空表示不按标记过滤。</summary>
	[Export] public string PresentLoopFlag { get; set; } = "";

	/// <summary>为 true 时标记存在才在场；为 false 时标记存在则离场。</summary>
	[Export] public bool VisibleWhenFlagSet { get; set; } = true;

	private bool _valid;
	private bool _present;
	private string _effectiveStateId = "";

	private Label _nameLabel;
	private Vector2 _defaultSpriteScale;
	private Vector2 _defaultSpriteOffset;
	private float _defaultNameLabelTop;
	private float _defaultNameLabelHeight;

	public override void _Ready()
	{
		if (GameState.Instance == null)
		{
			GD.PushError($"Npc {NpcId}: GameState 未就绪");
			Monitorable = false;
			return;
		}

		_valid = Validate();
		if (!_valid) return;

		_nameLabel = GetNode<Label>("NameLabel");
		_nameLabel.Text = DisplayName;

		_defaultSpriteScale = Sprite.Scale;
		_defaultSpriteOffset = Sprite.Offset;
		_defaultNameLabelTop = _nameLabel.OffsetTop;
		_defaultNameLabelHeight = _nameLabel.OffsetBottom - _nameLabel.OffsetTop;

		RefreshVisual();
		GameState.Instance.TimeChanged += OnTimeChanged;
		GameState.Instance.LoopFlagsChanged += OnLoopFlagsChanged;
	}

	public override void _ExitTree()
	{
		if (GodotObject.IsInstanceValid(GameState.Instance))
		{
			GameState.Instance.TimeChanged -= OnTimeChanged;
			GameState.Instance.LoopFlagsChanged -= OnLoopFlagsChanged;
		}
	}

	/// <summary>当前时间帧是否在场。</summary>
	public bool IsPresent() => _present;

	/// <summary>有效状态（本轮覆盖优先）是否等于 stateId。</summary>
	public bool IsInState(string stateId) => _effectiveStateId == stateId;

	/// <summary>写入本轮状态覆盖并立即刷新；stateId 必须出现在帧表里。</summary>
	public void SetLoopState(string stateId)
	{
		if (!_valid) return;
		if (string.IsNullOrEmpty(stateId))
		{
			GD.PushError($"Npc {NpcId}.SetLoopState: 状态 id 为空");
			return;
		}
		if (!HasState(stateId))
		{
			GD.PushError($"Npc {NpcId}.SetLoopState: 帧表里没有状态 {stateId}");
			return;
		}

		GameState.Instance.SetNpcLoopState(NpcId, stateId);
		RefreshVisual();
	}

	public override void Interact()
	{
		if (!_valid) return;
		if (!_present)
		{
			GD.PushWarning($"Npc {NpcId}: 不在场，忽略交互");
			return;
		}
		if (Dialogue == null)
		{
			GD.PushError($"Npc {NpcId}: Dialogue 为空");
			return;
		}

		GD.Print($"[Npc] 交互 {NpcId}");
		var states = new Godot.Collections.Array<Variant>
		{
			new Godot.Collections.Dictionary { { "npc", this } }
		};

		Node balloon = DialogueManager.ShowDialogueBalloon(Dialogue, "start", states);
		if (balloon == null)
		{
			GD.PushError($"Npc {NpcId}: ShowDialogueBalloon 返回空");
			return;
		}
		if (balloon is DialogueBalloon dialogueBalloon)
		{
			dialogueBalloon.Npc = this;
		}
		else
		{
			GD.PushError($"Npc {NpcId}: 默认 balloon 不是 DialogueBalloon");
			balloon.QueueFree();
		}
	}

	private void OnTimeChanged(int elapsedMinutes)
	{
		RefreshVisual();
	}

	private void OnLoopFlagsChanged()
	{
		RefreshVisual();
	}

	private void RefreshVisual()
	{
		if (!_valid) return;

		NpcStateFrame frame = ResolveFrame(GameState.Instance.ElapsedMinutes);
		if (frame == null)
		{
			GD.PushError($"Npc {NpcId}: {GameState.Instance.GetClockText()} 没有可用帧");
			return;
		}

		string overrideState = GameState.Instance.GetNpcLoopState(NpcId);
		_effectiveStateId = string.IsNullOrEmpty(overrideState) ? frame.StateId : overrideState;

		NpcStateFrame textureFrame = FindFrame(_effectiveStateId);
		if (textureFrame == null)
		{
			GD.PushError($"Npc {NpcId}: 帧表里没有状态 {_effectiveStateId}");
			return;
		}

		bool flagSatisfied = string.IsNullOrEmpty(PresentLoopFlag)
			|| GameState.Instance.HasLoopFlag(PresentLoopFlag) == VisibleWhenFlagSet;
		_present = frame.Visible && flagSatisfied;
		Sprite.Texture = textureFrame.Texture;

		bool hasSpriteOverride = textureFrame.SpriteScale != Vector2.Zero;
		Sprite.Scale = hasSpriteOverride ? textureFrame.SpriteScale : _defaultSpriteScale;
		Sprite.Offset = hasSpriteOverride ? textureFrame.SpriteOffset : _defaultSpriteOffset;

		_nameLabel.OffsetTop = textureFrame.NameLabelTop == 0f ? _defaultNameLabelTop : textureFrame.NameLabelTop;
		_nameLabel.OffsetBottom = _nameLabel.OffsetTop + _defaultNameLabelHeight;

		Visible = _present;
		SetDeferred(Area2D.PropertyName.Monitorable, _present);

		GD.Print($"[Npc] {NpcId} {GameState.Instance.GetClockText()} state={_effectiveStateId} present={_present}");
	}

	private NpcStateFrame ResolveFrame(int elapsedMinutes)
	{
		NpcStateFrame result = null;
		foreach (NpcStateFrame frame in Frames)
		{
			if (frame.FromMinute <= elapsedMinutes) result = frame;
			else break;
		}
		return result;
	}

	private NpcStateFrame FindFrame(string stateId)
	{
		foreach (NpcStateFrame frame in Frames)
		{
			if (frame.StateId == stateId) return frame;
		}
		return null;
	}

	private bool HasState(string stateId) => FindFrame(stateId) != null;

	private bool Validate()
	{
		bool valid = true;

		if (string.IsNullOrEmpty(NpcId))
		{
			GD.PushError("Npc: NpcId 为空");
			valid = false;
		}
		if (string.IsNullOrEmpty(DisplayName))
		{
			GD.PushError($"Npc {NpcId}: DisplayName 为空");
			valid = false;
		}
		if (GetNodeOrNull<Label>("NameLabel") == null)
		{
			GD.PushError($"Npc {NpcId}: 缺少 NameLabel 节点");
			valid = false;
		}
		if (Dialogue == null)
		{
			GD.PushError($"Npc {NpcId}: Dialogue 为空");
			valid = false;
		}
		if (Sprite == null)
		{
			GD.PushError($"Npc {NpcId}: Sprite 为空");
			valid = false;
		}
		if (Frames == null || Frames.Length == 0)
		{
			GD.PushError($"Npc {NpcId}: Frames 为空");
			Monitorable = false;
			Visible = false;
			return false;
		}
		if (Frames[0] == null)
		{
			GD.PushError($"Npc {NpcId}: 首帧为空");
			valid = false;
		}
		else if (Frames[0].FromMinute != 0)
		{
			GD.PushError($"Npc {NpcId}: 首帧 FromMinute 必须是 0");
			valid = false;
		}

		int previousMinute = -1;
		var textures = new Dictionary<string, Texture2D>();
		foreach (NpcStateFrame frame in Frames)
		{
			if (frame == null)
			{
				GD.PushError($"Npc {NpcId}: Frames 里有空元素");
				valid = false;
				continue;
			}
			if (frame.FromMinute <= previousMinute)
			{
				GD.PushError($"Npc {NpcId}: FromMinute 必须严格递增（重复或倒退：{frame.FromMinute}）");
				valid = false;
			}
			previousMinute = frame.FromMinute;

			if (string.IsNullOrEmpty(frame.StateId))
			{
				GD.PushError($"Npc {NpcId}: 第 {frame.FromMinute} 分钟帧 StateId 为空");
				valid = false;
				continue;
			}
			if (frame.Visible && frame.Texture == null)
			{
				GD.PushError($"Npc {NpcId}: 状态 {frame.StateId} 在场但 Texture 为空");
				valid = false;
			}

			if (textures.TryGetValue(frame.StateId, out Texture2D existing))
			{
				if (existing != frame.Texture)
				{
					GD.PushError($"Npc {NpcId}: 状态 {frame.StateId} 对应了不同贴图");
					valid = false;
				}
			}
			else
			{
				textures[frame.StateId] = frame.Texture;
			}
		}

		if (!valid)
		{
			Monitorable = false;
			Visible = false;
		}
		return valid;
	}
}

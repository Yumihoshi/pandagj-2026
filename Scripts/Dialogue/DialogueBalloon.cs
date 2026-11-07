using System;
using Godot;
using Godot.Collections;
using DialogueManagerRuntime;

/// <summary>NPC 对话 balloon：正文、选项过滤、已选记录、玩家输入锁。</summary>
public partial class DialogueBalloon : CanvasLayer
{
	/// <summary>发起本次对话的 NPC；非 NPC 对话时为空。</summary>
	public Npc Npc { get; set; }

	/// <summary>为 true 时 _ExitTree 不释放玩家输入锁；由 PhoneScreen 在会话结束时统一释放。</summary>
	public bool KeepPlayerInputLockedOnExit { get; set; }

	[Export] public string NextAction = "ui_accept";
	[Export] public string SkipAction = "ui_cancel";

	private Control balloon;
	private RichTextLabel characterLabel;
	private DialogueLabel dialogueLabel;
	private DialogueResponsesMenu responsesMenu;
	private Polygon2D progress;

	/// <summary>区分「拖拽选择」与「单击推进」的位移阈值（像素）。</summary>
	[Export] public float ClickDragThreshold = 6.0f;

	/// <summary>对话正文里标记可获取文档的链接前缀。</summary>
	private const string DocumentLinkPrefix = "[url=doc:";

	/// <summary>文档链接标签的结束标记。</summary>
	private const string DocumentLinkEnd = "[/url]";

	/// <summary>未收集文档的高亮色。</summary>
	private const string DocumentHighlightColor = "[color=#ffd866]";

	/// <summary>已收集文档的灰色。</summary>
	private const string DocumentCollectedColor = "[color=#8a8a8a]";

	private Label copyToast;
	private Tween copyToastTween;
	private bool pointerPressed;
	private bool pressStartedInLabel;
	private Vector2 pressPosition;
	private Vector2 pressGlobalPosition;
	private ulong lastMetaClickFrame = ulong.MaxValue;

	private Resource dialogueResource;
	private string startFromLabel = "";
	private Array<Variant> temporaryGameStates = new();
	private Array<DialogueResponse> filteredResponses = new();
	private DialogueLine dialogueLine;
	private bool isWaitingForInput;
	private bool willHideBalloon;
	private bool isAdvancing;

	private Player player;
	private GameState gameState;

	private readonly Timer mutationCooldown = new();

	private DialogueLine CurrentLine
	{
		get => dialogueLine;
		set
		{
			if (value == null)
			{
				QueueFree();
				return;
			}
			dialogueLine = value;
			ApplyDialogueLine();
		}
	}

	public override void _Ready()
	{
		balloon = GetNode<Control>("%Balloon");
		characterLabel = GetNode<RichTextLabel>("%CharacterLabel");
		dialogueLabel = GetNode<DialogueLabel>("%DialogueLabel");
		responsesMenu = GetNode<DialogueResponsesMenu>("%ResponsesMenu");
		progress = GetNode<Polygon2D>("%Progress");

		copyToast = GetNodeOrNull<Label>("%CopyToast");
		if (copyToast == null)
		{
			GD.PushError("DialogueBalloon: 找不到 %CopyToast，复制提示不可用");
		}
		dialogueLabel.SelectionEnabled = false;
		dialogueLabel.MetaClicked += OnDialogueMetaClicked;

		balloon.Hide();
		balloon.GuiInput += OnBalloonGuiInput;

		if (string.IsNullOrEmpty(responsesMenu.NextAction))
		{
			responsesMenu.NextAction = NextAction;
		}
		responsesMenu.ResponseSelected += OnResponseSelected;

		mutationCooldown.OneShot = true;
		mutationCooldown.Timeout += OnMutationCooldownTimeout;
		AddChild(mutationCooldown);
		DialogueManager.Mutated += OnMutated;

		gameState = GameState.Instance;
		if (gameState == null)
		{
			GD.PushError("DialogueBalloon: GameState 未就绪");
		}
		else
		{
			gameState.LoopReset += OnLoopReset;
		}
	}

	public override void _ExitTree()
	{
		DialogueManager.Mutated -= OnMutated;
		if (GodotObject.IsInstanceValid(dialogueLabel))
		{
			dialogueLabel.MetaClicked -= OnDialogueMetaClicked;
		}
		if (GodotObject.IsInstanceValid(gameState))
		{
			gameState.LoopReset -= OnLoopReset;
		}
		if (GodotObject.IsInstanceValid(player)
			&& !KeepPlayerInputLockedOnExit
			&& !(GodotObject.IsInstanceValid(gameState) && gameState.IsTrueEnding))
		{
			player.InputLocked = false;
		}
	}

	public override void _Process(double delta)
	{
		base._Process(delta);

		if (dialogueLine != null)
		{
			progress.Visible = !dialogueLabel.IsTyping
				&& filteredResponses.Count == 0
				&& !dialogueLine.HasTag("voice");
		}
	}

	public async void Start(Resource resource = null, string label = "", Array<Variant> extraGameStates = null)
	{
		if (gameState == null)
		{
			GD.PushError("DialogueBalloon.Start: GameState 未就绪");
			QueueFree();
			return;
		}

		player = GetTree().GetFirstNodeInGroup("player") as Player;
		if (player == null)
		{
			GD.PushError("DialogueBalloon.Start: 找不到 player");
		}
		else
		{
			player.InputLocked = true;
		}

		temporaryGameStates = new Array<Variant> { this } + (extraGameStates ?? new Array<Variant>());
		isWaitingForInput = false;

		if (IsInstanceValid(resource)) dialogueResource = resource;
		if (label != "") startFromLabel = label;

		GD.Print($"[Dialogue] 打开 {Npc?.NpcId ?? "(无 NPC)"}");
		DialogueLine next = await DialogueManager.GetNextDialogueLine(dialogueResource, startFromLabel, temporaryGameStates);
		if (!GodotObject.IsInstanceValid(this)) return;
		CurrentLine = next;
		Show();
	}

	private void OnBalloonGuiInput(InputEvent @event)
	{
		// 打字中：点击或 Esc 都是跳过打字（现有行为）。
		if (dialogueLabel.IsTyping)
		{
			bool mouseWasClicked = @event is InputEventMouseButton mb
				&& mb.ButtonIndex == MouseButton.Left
				&& @event.IsPressed();
			bool skipButtonWasPressed = @event.IsActionPressed(SkipAction);
			if (mouseWasClicked || skipButtonWasPressed)
			{
				GetViewport().SetInputAsHandled();
				dialogueLabel.SkipTyping();
				return;
			}
		}

		if (@event is InputEventMouseButton mouseButton && mouseButton.ButtonIndex == MouseButton.Left)
		{
			if (mouseButton.Pressed)
			{
				// 只要不是打字中，按下就记录位置；复制与推进都留到松开时判定。
				// 选项菜单 / 自动推进等待期间也记录，否则那两种状态下无法拖拽复制。
				pointerPressed = true;
				pressPosition = mouseButton.Position;
				pressGlobalPosition = mouseButton.GlobalPosition;
				pressStartedInLabel = dialogueLabel.GetGlobalRect()
					.HasPoint(mouseButton.GlobalPosition);
				return;
			}

			if (!pointerPressed) return;
			pointerPressed = false;

			float dragDistance = mouseButton.Position.DistanceTo(pressPosition);
			string selected = dialogueLabel.GetSelectedText();

			if (pressStartedInLabel
				&& dragDistance > ClickDragThreshold
				&& !string.IsNullOrWhiteSpace(selected))
			{
				GetViewport().SetInputAsHandled();
				CopySelectedText(selected);
				return;
			}

			if (dragDistance <= ClickDragThreshold
				&& isWaitingForInput
				&& filteredResponses.Count == 0)
			{
				GetViewport().SetInputAsHandled();
				string nextId = dialogueLine.NextId;
				Callable.From(() => AdvanceAfterMetaClick(nextId)).CallDeferred();
				return;
			}

			// 位移超过阈值但没有选中（拖在对话框空白处）：什么都不做。
			return;
		}

		if (!isWaitingForInput) return;
		if (filteredResponses.Count > 0) return;

		if (@event.IsActionPressed(NextAction) && GetViewport().GuiGetFocusOwner() == balloon)
		{
			GetViewport().SetInputAsHandled();
			Next(dialogueLine.NextId);
		}
	}

	private void OnDialogueMetaClicked(Variant meta)
	{
		lastMetaClickFrame = Engine.GetProcessFrames();

		// 拖拽选择结束时松开在链接上：算复制，不算点击获取。
		if (pressGlobalPosition.DistanceTo(GetViewport().GetMousePosition()) > ClickDragThreshold)
		{
			return;
		}

		string url = meta.AsString();
		if (!url.StartsWith("doc:"))
		{
			GD.PushError($"DialogueBalloon: 不支持的高亮链接 {url}");
			return;
		}

		string documentId = url["doc:".Length..];
		if (string.IsNullOrEmpty(documentId))
		{
			GD.PushError("DialogueBalloon: 文档链接的 id 为空");
			return;
		}
		if (gameState == null)
		{
			GD.PushError("DialogueBalloon: GameState 未就绪，无法记录文档");
			return;
		}

		bool added = gameState.AcquireDocument(documentId);
		if (added)
		{
			RefreshLineAfterDocumentChange();
			ShowToast("已记录到手机");
			return;
		}

		// 未注册的 id 已由 AcquireDocument 报错；只有重复获取才提示已在手机里。
		if (gameState.HasDocument(documentId))
		{
			ShowToast("已在手机里");
		}
	}

	/// <summary>把当前句里已收集文档的高亮改成灰色；只换颜色不动结构，正文长度不变。</summary>
	private void ApplyCollectedDocumentStyle()
	{
		if (gameState == null || dialogueLine == null) return;
		if (!dialogueLine.Text.Contains(DocumentLinkPrefix, StringComparison.Ordinal)) return;

		string text = dialogueLine.Text;
		int searchFrom = 0;
		while (true)
		{
			int open = text.IndexOf(DocumentLinkPrefix, searchFrom, StringComparison.Ordinal);
			if (open < 0) break;

			int tagEnd = text.IndexOf(']', open);
			int linkEnd = tagEnd < 0 ? -1 : text.IndexOf(DocumentLinkEnd, tagEnd, StringComparison.Ordinal);
			if (tagEnd < 0 || linkEnd < 0) break;

			searchFrom = linkEnd + DocumentLinkEnd.Length;

			string documentId = text[(open + DocumentLinkPrefix.Length)..tagEnd];
			if (string.IsNullOrEmpty(documentId) || !gameState.HasDocument(documentId)) continue;

			string inner = text[tagEnd..linkEnd];
			text = string.Concat(text[..tagEnd], inner.Replace(DocumentHighlightColor, DocumentCollectedColor), text[linkEnd..]);
		}

		dialogueLine.Text = text;
	}

	/// <summary>收集文档后把当前句原地重绘成灰色，不必等下一句。</summary>
	private void RefreshLineAfterDocumentChange()
	{
		if (dialogueLine == null || dialogueLabel.IsTyping) return;

		ApplyCollectedDocumentStyle();
		dialogueLabel.Text = dialogueLine.Text;
		dialogueLabel.VisibleCharacters = -1;
	}

	private void AdvanceAfterMetaClick(string nextId)
	{
		if (!GodotObject.IsInstanceValid(this)) return;
		// 同一帧内发生过 meta 点击：这次松开是点链接，不是推进。
		if (lastMetaClickFrame == Engine.GetProcessFrames()) return;
		Next(nextId);
	}

	private async void ApplyDialogueLine()
	{
		// 新的一句：关掉上一句的选区，并清掉上一句的复制提示。
		dialogueLabel.SelectionEnabled = false;
		copyToastTween?.Kill();
		if (copyToast != null) copyToast.Visible = false;

		mutationCooldown.Stop();
		isWaitingForInput = false;
		balloon.FocusMode = Control.FocusModeEnum.All;
		balloon.GrabFocus();

		characterLabel.Visible = !string.IsNullOrEmpty(dialogueLine.Character);
		characterLabel.Text = Tr(dialogueLine.Character, "dialogue");

		dialogueLabel.Hide();
		ApplyCollectedDocumentStyle();
		dialogueLabel.DialogueLine = dialogueLine;

		Array<DialogueResponse> filtered = FilterResponses(dialogueLine);
		filteredResponses = filtered;

		if (dialogueLine.Responses.Count > 0 && !HasSelectable(filtered))
		{
			GD.PushError($"DialogueBalloon: {Npc?.NpcId} 的选项全部不可选，关闭对话");
			QueueFree();
			return;
		}

		responsesMenu.Hide();
		responsesMenu.Responses = filtered;

		balloon.Show();
		willHideBalloon = false;
		dialogueLabel.Show();

		if (!string.IsNullOrEmpty(dialogueLine.Text))
		{
			dialogueLabel.TypeOut();
			await ToSignal(dialogueLabel, DialogueLabel.SignalName.FinishedTyping);
			if (!GodotObject.IsInstanceValid(this)) return;
		}

		// 打字完成（或本句无文字）：本句正文可以拖拽选择。
		dialogueLabel.SelectionEnabled = true;
		// 引擎在 set_selection_enabled(true) 里会把 focus_mode 设成 ALL；
		// 点击正文会抢走 Balloon 的键盘焦点，导致空格/回车推进失效。
		dialogueLabel.FocusMode = Control.FocusModeEnum.None;

		if (filtered.Count > 0)
		{
			balloon.FocusMode = Control.FocusModeEnum.None;
			responsesMenu.Show();
			responsesMenu.FocusFirstItem();
		}
		else if (!string.IsNullOrEmpty(dialogueLine.Time))
		{
			if (!float.TryParse(dialogueLine.Time, out float time))
			{
				time = dialogueLine.Text.Length * 0.02f;
			}
			await ToSignal(GetTree().CreateTimer(time), Timer.SignalName.Timeout);
			if (!GodotObject.IsInstanceValid(this)) return;
			Next(dialogueLine.NextId);
		}
		else
		{
			isWaitingForInput = true;
			balloon.FocusMode = Control.FocusModeEnum.All;
			balloon.GrabFocus();
		}
	}

	private Array<DialogueResponse> FilterResponses(DialogueLine line)
	{
		var result = new Array<DialogueResponse>();

		foreach (DialogueResponse response in line.Responses)
		{
			if (response.GetTagValue("hide_if") == "true") continue;

			bool repeatable = response.HasTag("repeatable");
			bool used = Npc != null
				&& gameState != null
				&& !repeatable
				&& !string.IsNullOrEmpty(response.StaticId)
				&& gameState.IsChoiceUsed(Npc.NpcId, response.StaticId);

			if (used)
			{
				response.IsAllowed = false;
				result.Add(response);
				continue;
			}

			if (!response.IsAllowed)
			{
				if (response.GetTagValue("show_if") == "true") result.Add(response);
				continue;
			}

			result.Add(response);
		}

		return result;
	}

	private static bool HasSelectable(Array<DialogueResponse> responses)
	{
		foreach (DialogueResponse response in responses)
		{
			if (response.IsAllowed) return true;
		}
		return false;
	}

	private void OnResponseSelected(DialogueResponse response)
	{
		if (isAdvancing) return;

		if (Npc != null && !response.HasTag("repeatable"))
		{
			if (string.IsNullOrEmpty(response.StaticId))
			{
				GD.PushError($"DialogueBalloon: {Npc.NpcId} 的一次性选项缺少 [ID:]");
			}
			else if (gameState != null)
			{
				gameState.MarkChoiceUsed(Npc.NpcId, response.StaticId);
			}
		}

		GD.Print($"[Dialogue] 选择 {response.Text}");
		Next(response.NextId);
	}

	private async void Next(string nextId)
	{
		if (isAdvancing) return;
		isAdvancing = true;

		DialogueLine next = await DialogueManager.GetNextDialogueLine(dialogueResource, nextId, temporaryGameStates);
		if (!GodotObject.IsInstanceValid(this)) return;

		CurrentLine = next;
		isAdvancing = false;
	}

	private void CopySelectedText(string text)
	{
		if (!DisplayServer.HasFeature(DisplayServer.Feature.Clipboard))
		{
			GD.PushError("DialogueBalloon: 当前平台没有剪贴板，无法复制对话");
			return;
		}

		DisplayServer.ClipboardSet(text);
		ShowToast("已复制");
		GD.Print($"[Dialogue] 已复制 {text.Length} 字");
	}

	private void ShowToast(string text)
	{
		if (copyToast == null) return;   // 缺失已在 _Ready 报错

		copyToast.Text = text;
		copyToastTween?.Kill();
		copyToast.Visible = true;
		copyToast.Modulate = new Color(1, 1, 1, 0);

		copyToastTween = CreateTween();
		copyToastTween.TweenProperty(copyToast, "modulate:a", 1.0f, 0.1f);
		copyToastTween.TweenInterval(0.35f);
		copyToastTween.TweenProperty(copyToast, "modulate:a", 0.0f, 0.25f);
		copyToastTween.TweenCallback(Callable.From(() => copyToast.Visible = false));
	}

	private void OnMutated(Dictionary mutation)
	{
		if (!(bool)mutation["is_inline"])
		{
			isWaitingForInput = false;
			willHideBalloon = true;
			mutationCooldown.Start(0.1f);
		}
	}

	private void OnMutationCooldownTimeout()
	{
		if (willHideBalloon)
		{
			willHideBalloon = false;
			balloon.Hide();
		}
	}

	private void OnLoopReset()
	{
		QueueFree();
	}
}

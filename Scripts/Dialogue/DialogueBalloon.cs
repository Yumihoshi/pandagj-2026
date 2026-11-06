using Godot;
using Godot.Collections;
using DialogueManagerRuntime;

/// <summary>NPC 对话 balloon：正文、选项过滤、已选记录、玩家输入锁。</summary>
public partial class DialogueBalloon : CanvasLayer
{
	/// <summary>发起本次对话的 NPC；非 NPC 对话时为空。</summary>
	public Npc Npc { get; set; }

	[Export] public string NextAction = "ui_accept";
	[Export] public string SkipAction = "ui_cancel";

	private Control balloon;
	private RichTextLabel characterLabel;
	private DialogueLabel dialogueLabel;
	private DialogueResponsesMenu responsesMenu;
	private Polygon2D progress;

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
		if (GodotObject.IsInstanceValid(gameState))
		{
			gameState.LoopReset -= OnLoopReset;
		}
		if (GodotObject.IsInstanceValid(player))
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

		if (!isWaitingForInput) return;
		if (filteredResponses.Count > 0) return;

		if (@event is InputEventMouseButton mouseButton
			&& mouseButton.IsPressed()
			&& mouseButton.ButtonIndex == MouseButton.Left)
		{
			GetViewport().SetInputAsHandled();
			Next(dialogueLine.NextId);
		}
		else if (@event.IsActionPressed(NextAction) && GetViewport().GuiGetFocusOwner() == balloon)
		{
			GetViewport().SetInputAsHandled();
			Next(dialogueLine.NextId);
		}
	}

	private async void ApplyDialogueLine()
	{
		mutationCooldown.Stop();
		isWaitingForInput = false;
		balloon.FocusMode = Control.FocusModeEnum.All;
		balloon.GrabFocus();

		characterLabel.Visible = !string.IsNullOrEmpty(dialogueLine.Character);
		characterLabel.Text = Tr(dialogueLine.Character, "dialogue");

		dialogueLabel.Hide();
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

using Godot;
using System.Threading.Tasks;
using DialogueManagerRuntime;

/// <summary>常驻根场景：装载/卸载地点、显隐地图、衔接过渡、响应循环重置与终末。</summary>
public partial class GameScene : Node2D
{
	/// <summary>初始解锁配置。</summary>
	[Export] public MapConfig MapConfig { get; set; }

	/// <summary>开局与循环重置后进入的起点地点。</summary>
	[Export] public PackedScene StartLocation { get; set; }

	/// <summary>地图面板。</summary>
	[Export] public MapScreen MapScreen { get; set; }

	/// <summary>当前地点的挂载点。</summary>
	[Export] public Node2D LocationHost { get; set; }

	/// <summary>本游戏的对话 balloon 场景。</summary>
	[Export] public PackedScene DialogueBalloonScene { get; set; }

	/// <summary>全部可收集文档的注册表。</summary>
	[Export] public DocumentLibrary DocumentLibrary { get; set; }

	/// <summary>开场 / 循环开始的剧情文本：一周目播 start，二周目起播 phone_vibrate。</summary>
	[Export] public Resource OpeningNarration { get; set; }

	/// <summary>手机 UI：循环开始提示要自动打开它的记事本。</summary>
	[Export] public PhoneScreen Phone { get; set; }

	/// <summary>结局黑屏。</summary>
	[Export] public EndingOverlay EndingOverlay { get; set; }

	/// <summary>17:30 终末提示。</summary>
	[Export] public LoopEndNotice LoopEndNotice { get; set; }

	/// <summary>循环开始时自动写入记事本的提示。</summary>
	private const string LoopStartNote = "下午出去转转吧";

	private LocationRoot _currentLocation;
	private bool _transitioning;
	private bool _loopEnding;

	private ScreenFade Fade => GetNode<ScreenFade>("/root/ScreenFade");

	public override async void _Ready()
	{
		if (MapConfig == null || StartLocation == null || MapScreen == null
			|| LocationHost == null || DialogueBalloonScene == null
			|| DocumentLibrary == null || EndingOverlay == null || LoopEndNotice == null
			|| Phone == null)
		{
			GD.PushError("GameScene: 有导出引用为空，停止启动");
			return;
		}
		if (GameState.Instance == null)
		{
			GD.PushError("GameScene: GameState 未就绪，停止启动");
			return;
		}
		if (!ValidateConfigIds()) return;

		Error balloonError = DialogueManager.SetDefaultBalloon(DialogueBalloonScene);
		if (balloonError != Error.Ok)
		{
			GD.PushError($"GameScene: 设置默认对话 balloon 失败（{balloonError}）");
			return;
		}

		// set_default_balloon 写的是 runtime/balloon_path，而 DMSettings.get_setting
		// 读的是 dialogue_manager/runtime/balloon_path；直接写读取键，
		// 否则 show_dialogue_balloon 会回退到 addon 的示例 balloon。
		ProjectSettings.SetSetting("dialogue_manager/runtime/balloon_path", DialogueBalloonScene.ResourcePath);

		GameState.Instance.SeedInitialUnlocks(MapConfig);
		GameState.Instance.SeedDocuments(DocumentLibrary);
		GameState.Instance.LoopReset += OnLoopReset;
		GameState.Instance.LoopEndReached += OnLoopEndReached;
		GameState.Instance.EpilogueStarted += OnEpilogueStarted;
		GameState.Instance.TrueEndingReached += OnTrueEndingReached;
		GameState.Instance.LoopFailed += OnLoopFailed;
		MapScreen.LocationSelected += OnLocationSelected;
		MapScreen.HideMap();

		Fade.SetOpaque();
		await LoadLocationAsync(StartLocation, loopStart: true);
		await Fade.FadeInAsync();

		ShowLoopStartIntro();
	}

	/// <summary>循环开始：一周目播开场旁白；二周目起播手机震动并自动打开记事本。</summary>
	private async void ShowLoopStartIntro()
	{
		if (OpeningNarration == null)
		{
			GD.PushError("GameScene: OpeningNarration 为空");
			return;
		}

		if (GameState.Instance.IsFirstLoop())
		{
			if (DialogueManager.ShowDialogueBalloon(OpeningNarration, "start") == null)
			{
				GD.PushError("GameScene: 开场剧情 ShowDialogueBalloon 返回空");
			}
			return;
		}

		Node balloon = DialogueManager.ShowDialogueBalloon(OpeningNarration, "phone_vibrate");
		if (balloon == null)
		{
			GD.PushError("GameScene: 手机震动剧情 ShowDialogueBalloon 返回空");
			return;
		}
		await ToSignal(balloon, Node.SignalName.TreeExited);

		if (!IsInsideTree()) return;

		if (Phone == null)
		{
			GD.PushError("GameScene: Phone 为空，无法打开记事本");
			return;
		}
		Phone.OpenNotebookWithNote(LoopStartNote);
	}

	/// <summary>MapConfig 里写的地点 id 必须在地图按钮中存在，否则是配置写错。</summary>
	private bool ValidateConfigIds()
	{
		foreach (string id in MapConfig.InitialUnlockedLocationIds)
		{
			bool found = false;
			foreach (MapLocationButton button in MapScreen.Buttons)
			{
				if (button != null && button.LocationId == id)
				{
					found = true;
					break;
				}
			}

			if (!found)
			{
				GD.PushError($"GameScene: MapConfig 的地点 {id} 在地图按钮里找不到");
				return false;
			}
		}
		return true;
	}

	private void OnLocationSelected(string locationId, PackedScene scene, int travelMinutes)
	{
		if (_transitioning || _loopEnding) return;
		if (GameState.Instance.AdvanceTime(travelMinutes)) return;

		_ = EnterLocationAsync(locationId, scene);
	}

	private async void OnLoopEndReached()
	{
		if (_loopEnding) return;
		_loopEnding = true;

		if (_currentLocation != null)
		{
			_currentLocation.ProcessMode = ProcessModeEnum.Disabled;
		}

		GD.Print("[Loop] 17:30 爆炸");
		LoopEndNotice.Show();
		await ToSignal(GetTree().CreateTimer(2.0), Timer.SignalName.Timeout);

		GameState.Instance.ResetLoop();
		_loopEnding = false;
	}

	private void OnEpilogueStarted()
	{
		EndingOverlay.ShowEpilogue();
	}

	private void OnTrueEndingReached()
	{
		EndingOverlay.ShowTheEnd();

		Player player = GetTree().GetFirstNodeInGroup("player") as Player;
		if (player == null)
		{
			GD.PushError("GameScene.OnTrueEndingReached: 找不到 player");
			return;
		}
		player.InputLocked = true;
	}

	private async void OnLoopFailed()
	{
		if (_loopEnding) return;
		_loopEnding = true;

		if (_currentLocation != null)
		{
			_currentLocation.ProcessMode = ProcessModeEnum.Disabled;
		}

		GD.Print("[Loop] 失败");
		EndingOverlay.ShowFail();
		await ToSignal(GetTree().CreateTimer(1.0), Timer.SignalName.Timeout);

		GameState.Instance.ResetLoop();
		_loopEnding = false;
	}

	private async void OnLoopReset()
	{
		if (_transitioning)
		{
			GD.PushError("GameScene: LoopReset 在过渡中到达，重置未重载场景");
			return;
		}

		_transitioning = true;
		MapScreen.HideMap();
		EndingOverlay.HideAll();
		LoopEndNotice.HideAll();
		await Fade.FadeOutAsync();
		UnloadLocation();
		await LoadLocationAsync(StartLocation, loopStart: true);
		await Fade.FadeInAsync();
		_transitioning = false;
		GD.Print("[Loop] 已回到起点地点");
		ShowLoopStartIntro();
	}

	private async Task EnterLocationAsync(string locationId, PackedScene scene)
	{
		if (scene == null)
		{
			GD.PushError($"GameScene: 地点 {locationId} 的 LocationScene 为空");
			return;
		}

		_transitioning = true;
		await Fade.FadeOutAsync();
		UnloadLocation();
		await LoadLocationAsync(scene);
		MapScreen.HideMap();
		await Fade.FadeInAsync();
		_transitioning = false;
		GD.Print($"[Map] 进入地点 {locationId}");
	}

	private async Task LoadLocationAsync(PackedScene scene, bool loopStart = false)
	{
		Node instance = scene.Instantiate();
		if (instance is not LocationRoot location)
		{
			GD.PushError($"GameScene: {scene.ResourcePath} 的根节点不是 LocationRoot");
			instance.QueueFree();
			return;
		}

		location.IsLoopStart = loopStart;
		LocationHost.AddChild(location);
		_currentLocation = location;
		location.ExitRequested += OnExitRequested;
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
	}

	private void UnloadLocation()
	{
		if (_currentLocation == null) return;

		_currentLocation.ExitRequested -= OnExitRequested;
		LocationHost.RemoveChild(_currentLocation);
		_currentLocation.QueueFree();
		_currentLocation = null;
	}

	private async void OnExitRequested()
	{
		if (_transitioning || _loopEnding) return;

		_transitioning = true;
		await Fade.FadeOutAsync();
		UnloadLocation();
		MapScreen.ShowMap();
		await Fade.FadeInAsync();
		_transitioning = false;
	}
}

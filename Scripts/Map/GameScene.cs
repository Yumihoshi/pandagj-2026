using Godot;
using System.Threading.Tasks;

/// <summary>常驻根场景：装载/卸载地点、显隐地图、衔接过渡、响应循环重置。</summary>
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

	private LocationRoot _currentLocation;
	private bool _transitioning;

	private ScreenFade Fade => GetNode<ScreenFade>("/root/ScreenFade");

	public override async void _Ready()
	{
		if (MapConfig == null || StartLocation == null || MapScreen == null || LocationHost == null)
		{
			GD.PushError("GameScene: 有导出引用为空，停止启动");
			return;
		}
		if (!ValidateConfigIds()) return;

		GameState.Instance.SeedInitialUnlocks(MapConfig);
		GameState.Instance.LoopReset += OnLoopReset;
		MapScreen.LocationSelected += OnLocationSelected;
		MapScreen.HideMap();

		Fade.SetOpaque();
		await LoadLocationAsync(StartLocation);
		await Fade.FadeInAsync();
	}

	// 临时调试入口：时间系统接入后删掉，循环重置改由时间系统触发。
	public override void _UnhandledInput(InputEvent @event)
	{
		if (!@event.IsActionPressed("debug_reset_loop")) return;

		GameState.Instance.ResetLoop();
		GetViewport().SetInputAsHandled();
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

	private void OnLocationSelected(string locationId, PackedScene scene)
	{
		if (_transitioning) return;

		GameState.Instance.AdvanceTime();
		_ = EnterLocationAsync(locationId, scene);
	}

	private async void OnLoopReset()
	{
		if (_transitioning) return;

		_transitioning = true;
		await Fade.FadeOutAsync();
		UnloadLocation();
		await LoadLocationAsync(StartLocation);
		await Fade.FadeInAsync();
		_transitioning = false;
		GD.Print("[Loop] 已回到起点地点");
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

	private async Task LoadLocationAsync(PackedScene scene)
	{
		Node instance = scene.Instantiate();
		if (instance is not LocationRoot location)
		{
			GD.PushError($"GameScene: {scene.ResourcePath} 的根节点不是 LocationRoot");
			instance.QueueFree();
			return;
		}

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
		if (_transitioning) return;

		_transitioning = true;
		await Fade.FadeOutAsync();
		UnloadLocation();
		MapScreen.ShowMap();
		await Fade.FadeInAsync();
		_transitioning = false;
	}
}

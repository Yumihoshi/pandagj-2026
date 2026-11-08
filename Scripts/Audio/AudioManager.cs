using Godot;
using System.Collections.Generic;

/// <summary>界面音效分组，对应 <c>Audio/Sfx</c> 下的文件分类。</summary>
public enum SfxId
{
	Click,
	Rollover,
	Switch,
	MouseClick,
	MouseRelease,
}

/// <summary>全局音频服务：随机播放同组界面音效，并负责 BGM 的播放与停止。</summary>
public partial class AudioManager : Node
{
	private const string SfxBus = "SFX";
	private const string BgmBus = "BGM";
	private const int SfxPlayerCount = 8;

	private readonly Dictionary<SfxId, AudioStream[]> _sfxGroups = new();
	private readonly List<AudioStreamPlayer> _sfxPlayers = new();
	private AudioStreamPlayer _bgmPlayer;
	private int _nextSfxPlayer;

	/// <summary>BGM 曲目 id → 资源路径。</summary>
	private static readonly Dictionary<string, string> BgmCueFiles = new()
	{
		["cheer"] = "res://Audio/Bgm/开开心心啦啦啦.mp3",
		["adventure"] = "res://Audio/Bgm/暑假大冒险（活泼）.mp3",
		["still"] = "res://Audio/Bgm/静止的宇宙（静谧）.mp3",
		["moonlight"] = "res://Audio/Bgm/浅浅的月光（抒情，感情爆发）.mp3",
		["rainy"] = "res://Audio/Bgm/雨的恋人们（动脑）.mp3",
		["mist"] = "res://Audio/Bgm/夏天的雾（朦胧，抒情）.mp3",
		["dream"] = "res://Audio/Bgm/梦（朦胧，深情）.mp3",
	};

	/// <summary>大地点 id → 默认环境 BGM 曲目 id。</summary>
	private static readonly Dictionary<string, string> LocationCueIds = new()
	{
		["classroom"] = "cheer",
		["playground"] = "adventure",
		["administration"] = "still",
	};

	/// <summary>跨循环剧情阶段：地下空间第一次被激活时写入。</summary>
	private const string BombSeenStage = "bomb_seen";
	private const string BombSeenCueId = "rainy";

	private string _locationId = "";
	private string _ambientCueId = "";
	private string _playingCueId = "";
	private bool _overrideActive;

	/// <summary>全局唯一实例，由 autoload 保证在 _Ready 时赋值。</summary>
	public static AudioManager Instance { get; private set; }

	/// <summary>界面音效总线音量（分贝）。</summary>
	public float SfxVolumeDb
	{
		get => AudioServer.GetBusVolumeDb(AudioServer.GetBusIndex(SfxBus));
		set => AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex(SfxBus), value);
	}

	/// <summary>背景音乐总线音量（分贝）。</summary>
	public float BgmVolumeDb
	{
		get => AudioServer.GetBusVolumeDb(AudioServer.GetBusIndex(BgmBus));
		set => AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex(BgmBus), value);
	}

	public override void _Ready()
	{
		Instance = this;

		if (AudioServer.GetBusIndex(SfxBus) < 0 || AudioServer.GetBusIndex(BgmBus) < 0)
		{
			GD.PushError($"AudioManager: 缺少音频总线 {SfxBus}/{BgmBus}，请检查 res://default_bus_layout.tres");
		}

		_sfxGroups[SfxId.Click] = LoadSfxGroup("Click/click", 5);
		_sfxGroups[SfxId.Rollover] = LoadSfxGroup("Rollover/rollover", 6);
		_sfxGroups[SfxId.Switch] = LoadSfxGroup("Switch/switch", 38);
		_sfxGroups[SfxId.MouseClick] = LoadSfxGroup("Mouse/mouseclick", 1);
		_sfxGroups[SfxId.MouseRelease] = LoadSfxGroup("Mouse/mouserelease", 1);

		for (int i = 0; i < SfxPlayerCount; i++)
		{
			var player = new AudioStreamPlayer { Bus = SfxBus };
			AddChild(player);
			_sfxPlayers.Add(player);
		}

		_bgmPlayer = new AudioStreamPlayer { Bus = BgmBus };
		AddChild(_bgmPlayer);

		if (GameState.Instance == null)
		{
			GD.PushError("AudioManager: GameState 未就绪，无法跟随剧情阶段切换 BGM");
		}
		else
		{
			GameState.Instance.StageChanged += OnStageChanged;
		}
	}

	/// <summary>播放指定分组里的一个随机音效，同组多个变体轮换使用可避免重复感。</summary>
	public void PlaySfx(SfxId id)
	{
		if (!_sfxGroups.TryGetValue(id, out AudioStream[] group) || group.Length == 0)
		{
			GD.PushError($"AudioManager.PlaySfx: 没有可用的音效分组 {id}");
			return;
		}

		AudioStreamPlayer player = _sfxPlayers[_nextSfxPlayer];
		_nextSfxPlayer = (_nextSfxPlayer + 1) % _sfxPlayers.Count;
		player.Stream = group[GD.RandRange(0, group.Length - 1)];
		player.Play();
	}

	/// <summary>播放点击音效。</summary>
	public void PlayClick() => PlaySfx(SfxId.Click);

	/// <summary>播放鼠标悬停音效。</summary>
	public void PlayHover() => PlaySfx(SfxId.Rollover);

	/// <summary>播放开关/确认类音效。</summary>
	public void PlaySwitch() => PlaySfx(SfxId.Switch);

	/// <summary>播放鼠标按下音效。</summary>
	public void PlayMouseClick() => PlaySfx(SfxId.MouseClick);

	/// <summary>播放鼠标松开音效。</summary>
	public void PlayMouseRelease() => PlaySfx(SfxId.MouseRelease);

	/// <summary>播放背景音乐；loop 为 true 时循环播放同一首。</summary>
	public void PlayBgm(AudioStream stream, bool loop = true)
	{
		if (stream == null)
		{
			GD.PushError("AudioManager.PlayBgm: AudioStream 为空");
			return;
		}

		switch (stream)
		{
			case AudioStreamOggVorbis ogg:
				ogg.Loop = loop;
				break;
			case AudioStreamMP3 mp3:
				mp3.Loop = loop;
				break;
		}

		_bgmPlayer.Stream = stream;
		_bgmPlayer.Play();
	}

	/// <summary>停止背景音乐。</summary>
	public void StopBgm()
	{
		_bgmPlayer.Stop();
		_playingCueId = "";
	}

	/// <summary>切换到某个大地点：按地点与剧情阶段选择环境 BGM；同名曲目正在播放时不重头播。</summary>
	public void PlayLocationBgm(string locationId)
	{
		if (string.IsNullOrEmpty(locationId))
		{
			GD.PushError("AudioManager.PlayLocationBgm: 地点 id 为空");
			return;
		}
		_locationId = locationId;
		ApplyAmbientBgm();
	}

	/// <summary>临时覆盖环境 BGM（如打开手机）；关闭时用 PopBgmOverride 恢复。</summary>
	public void PushBgmOverride(string cueId)
	{
		_overrideActive = true;
		StartBgm(cueId);
	}

	/// <summary>结束临时覆盖，恢复当前环境 BGM。</summary>
	public void PopBgmOverride()
	{
		if (!_overrideActive) return;
		_overrideActive = false;
		StartBgm(_ambientCueId);
	}

	/// <summary>剧本指定的 BGM：成为新的环境曲目，之后进出地点沿用。</summary>
	public void PlayBgmCue(string cueId)
	{
		_overrideActive = false;
		_ambientCueId = cueId;
		StartBgm(cueId);
	}

	/// <summary>剧情阶段变化时重新解析环境曲目（如进入地下空间后切到紧张曲）。</summary>
	private void OnStageChanged(string stage) => ApplyAmbientBgm();

	private void ApplyAmbientBgm()
	{
		string cue = ResolveAmbientCue();
		if (string.IsNullOrEmpty(cue)) return;

		_ambientCueId = cue;
		if (_overrideActive) return;
		StartBgm(cue);
	}

	private string ResolveAmbientCue()
	{
		if (GameState.Instance != null && GameState.Instance.HasStage(BombSeenStage))
		{
			return BombSeenCueId;
		}
		if (LocationCueIds.TryGetValue(_locationId, out string cue)) return cue;

		GD.PushError($"AudioManager: 地点 {_locationId} 没有配置 BGM");
		return "";
	}

	private void StartBgm(string cueId)
	{
		if (string.IsNullOrEmpty(cueId)) return;
		if (cueId == _playingCueId && _bgmPlayer.Playing) return;
		if (!BgmCueFiles.TryGetValue(cueId, out string path))
		{
			GD.PushError($"AudioManager.StartBgm: 未知 BGM 曲目 {cueId}");
			return;
		}

		AudioStream stream = GD.Load<AudioStream>(path);
		if (stream == null)
		{
			GD.PushError($"AudioManager.StartBgm: 加载失败 {path}");
			return;
		}

		PlayBgm(stream);
		_playingCueId = cueId;
	}

	private static AudioStream[] LoadSfxGroup(string relativePathPrefix, int count)
	{
		var streams = new AudioStream[count];
		for (int i = 0; i < count; i++)
		{
			string path = $"res://Audio/Sfx/{relativePathPrefix}{i + 1}.ogg";
			streams[i] = GD.Load<AudioStream>(path);
			if (streams[i] == null)
			{
				GD.PushError($"AudioManager: 音效加载失败 {path}");
			}
		}
		return streams;
	}
}

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

		if (stream is AudioStreamOggVorbis ogg)
		{
			ogg.Loop = loop;
		}

		_bgmPlayer.Stream = stream;
		_bgmPlayer.Play();
	}

	/// <summary>停止背景音乐。</summary>
	public void StopBgm() => _bgmPlayer.Stop();

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

using Godot;

/// <summary>剧情立绘舞台：甜品店 CG + 三人立绘，以及单个角色的右侧立绘。</summary>
public partial class CutsceneStage : CanvasLayer
{
	private const float FadeDuration = 0.6f;

	/// <summary>右侧 / 甜品店 CG 上显示的四人立绘。</summary>
	[Export] public Texture2D MurikoPortrait { get; set; }
	[Export] public Texture2D YukaPortrait { get; set; }
	[Export] public Texture2D ShinjiPortrait { get; set; }
	[Export] public Texture2D YamadaPortrait { get; set; }

	private Control _root;
	private TextureRect _background;
	private TextureRect _muriko;
	private TextureRect _yuka;
	private TextureRect _shinji;
	private TextureRect _right;
	private Tween _tween;

	/// <summary>甜品店舞台是否已经搭好；此时右侧补立绘不再隐藏背景与三人。</summary>
	private bool _epilogueVisible;

	public override void _Ready()
	{
		_root = GetNode<Control>("Root");
		_background = GetNode<TextureRect>("Root/Background");
		_muriko = GetNode<TextureRect>("Root/Muriko");
		_yuka = GetNode<TextureRect>("Root/Yuka");
		_shinji = GetNode<TextureRect>("Root/Shinji");
		_right = GetNode<TextureRect>("Root/Right");

		Validate();
		HideAll();
	}

	/// <summary>甜品店尾声：显示甜品店背景与左侧的鸣子 / 小优香 / 新井三人立绘。</summary>
	public void ShowEpilogue()
	{
		_tween?.Kill();

		_root.Show();
		_root.Modulate = Colors.White;

		_background.Show();
		_muriko.Texture = MurikoPortrait;
		_muriko.Show();
		_yuka.Texture = YukaPortrait;
		_yuka.Show();
		_shinji.Texture = ShinjiPortrait;
		_shinji.Show();
		_right.Hide();
		_epilogueVisible = true;

		FadeIn(_background, _muriko, _yuka, _shinji);
	}

	/// <summary>让指定角色在右侧独自登场（如地下空间跳下来的新井）。</summary>
	public void ShowRightCharacter(string characterId)
	{
		Texture2D texture = ResolvePortrait(characterId);
		if (texture == null) return;

		_tween?.Kill();

		_root.Show();
		_root.Modulate = Colors.White;
		if (!_epilogueVisible)
		{
			_background.Hide();
			_muriko.Hide();
			_yuka.Hide();
			_shinji.Hide();
		}

		_right.Texture = texture;
		_right.Show();
		FadeIn(_right);
	}

	/// <summary>隐藏整个舞台；循环重置时调用。</summary>
	public void HideAll()
	{
		_tween?.Kill();

		_root.Hide();
		_root.Modulate = Colors.White;
		_epilogueVisible = false;
		_background.Hide();
		_muriko.Hide();
		_yuka.Hide();
		_shinji.Hide();
		_right.Hide();
	}

	private void FadeIn(params CanvasItem[] items)
	{
		if (items.Length == 0) return;

		_tween = CreateTween();
		foreach (CanvasItem item in items)
		{
			item.Modulate = new Color(1, 1, 1, 0);
			_tween.Parallel().TweenProperty(item, "modulate:a", 1.0f, FadeDuration);
		}
	}

	private Texture2D ResolvePortrait(string characterId)
	{
		switch (characterId)
		{
			case "muriko": return MurikoPortrait;
			case "yuka": return YukaPortrait;
			case "shinji": return ShinjiPortrait;
			case "yamada": return YamadaPortrait;
			default:
				GD.PushError($"CutsceneStage: 未知角色 id {characterId}");
				return null;
		}
	}

	private void Validate()
	{
		if (_background.Texture == null) GD.PushError("CutsceneStage: Background 贴图未配置");
		if (MurikoPortrait == null) GD.PushError("CutsceneStage: MurikoPortrait 未配置");
		if (YukaPortrait == null) GD.PushError("CutsceneStage: YukaPortrait 未配置");
		if (ShinjiPortrait == null) GD.PushError("CutsceneStage: ShinjiPortrait 未配置");
		if (YamadaPortrait == null) GD.PushError("CutsceneStage: YamadaPortrait 未配置");
	}
}

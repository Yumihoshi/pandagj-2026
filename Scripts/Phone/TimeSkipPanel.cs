using Godot;

/// <summary>手机里的等待时间选择器：四个数字格加箭头，玩家选定目标时刻后把结果交给手机面板调度。</summary>
public partial class TimeSkipPanel : PanelContainer
{
	/// <summary>玩家确认目标时刻时触发，参数为目标时刻在循环内已过去的分钟数。</summary>
	public event System.Action<int> Confirmed;

	/// <summary>时十位 / 时个位 / 分十位 / 分个位的步进权值（分钟）。</summary>
	private static readonly int[] DigitWeights = { 600, 60, 10, 1 };

	/// <summary>四位数格对应的节点名前缀。</summary>
	private static readonly string[] DigitNodeNames = { "HourTens", "HourOnes", "MinuteTens", "MinuteOnes" };

	private readonly Label[] _digitValues = new Label[4];
	private readonly Button[] _digitUps = new Button[4];
	private readonly Button[] _digitDowns = new Button[4];

	private Button _confirmButton;

	private int _currentElapsed;
	private int _maxElapsed;
	private int _targetElapsed;

	public override void _Ready()
	{
		for (int i = 0; i < DigitNodeNames.Length; i++)
		{
			string prefix = DigitNodeNames[i];
			_digitValues[i] = GetNode<Label>($"%{prefix}Value");
			_digitUps[i] = GetNode<Button>($"%{prefix}Up");
			_digitDowns[i] = GetNode<Button>($"%{prefix}Down");

			int weight = DigitWeights[i];
			_digitUps[i].Pressed += () => Step(weight);
			_digitDowns[i].Pressed += () => Step(-weight);
		}

		_confirmButton = GetNode<Button>("%TimeSkipConfirm");
		_confirmButton.Pressed += OnConfirmPressed;
	}

	/// <summary>打开面板，把目标时刻复位到当前时刻并刷新显示。</summary>
	public void Open(int currentElapsedMinutes, int maxElapsedMinutes)
	{
		if (currentElapsedMinutes < 0 || maxElapsedMinutes < currentElapsedMinutes)
		{
			GD.PushError($"TimeSkipPanel.Open: 时间范围不合法（当前 {currentElapsedMinutes}，上限 {maxElapsedMinutes}）");
			return;
		}

		_currentElapsed = currentElapsedMinutes;
		_maxElapsed = maxElapsedMinutes;
		_targetElapsed = currentElapsedMinutes;
		Refresh();
	}

	/// <summary>按权值移动目标时刻；只有落在「本轮循环时间范围」内才生效。</summary>
	private void Step(int weight)
	{
		int candidate = _targetElapsed + weight;
		if (!IsReachable(candidate))
		{
			GD.PushError($"TimeSkipPanel.Step: 目标时刻 {candidate} 越界，对应箭头本应处于禁用状态");
			return;
		}

		_targetElapsed = candidate;
		Refresh();
	}

	/// <summary>候选时刻是否落在本轮可选的时间范围 [循环开始, 循环结束] 内。</summary>
	private bool IsReachable(int candidate)
	{
		return candidate >= 0 && candidate <= _maxElapsed;
	}

	/// <summary>刷新四位数显、每个箭头的可用性以及确定按钮的可用性。</summary>
	private void Refresh()
	{
		int minuteOfDay = GameState.LoopStartMinuteOfDay + _targetElapsed;
		int hour = minuteOfDay / 60;
		int minute = minuteOfDay % 60;

		_digitValues[0].Text = (hour / 10).ToString();
		_digitValues[1].Text = (hour % 10).ToString();
		_digitValues[2].Text = (minute / 10).ToString();
		_digitValues[3].Text = (minute % 10).ToString();

		for (int i = 0; i < DigitWeights.Length; i++)
		{
			_digitUps[i].Disabled = !IsReachable(_targetElapsed + DigitWeights[i]);
			_digitDowns[i].Disabled = !IsReachable(_targetElapsed - DigitWeights[i]);
		}

		_confirmButton.Disabled = _targetElapsed <= _currentElapsed;
	}

	/// <summary>点击确定：目标时刻必须晚于当前时刻，否则说明按钮禁用状态失效。</summary>
	private void OnConfirmPressed()
	{
		if (!IsReachable(_targetElapsed) || _targetElapsed <= _currentElapsed)
		{
			GD.PushError($"TimeSkipPanel: 确认了不合法的目标时刻 {_targetElapsed}（当前 {_currentElapsed}，上限 {_maxElapsed}）");
			return;
		}

		Confirmed?.Invoke(_targetElapsed);
	}
}

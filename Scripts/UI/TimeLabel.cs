using Godot;

/// <summary>屏幕右上角的游戏时间：显示当前循环内的时钟（0 = 13:00）。</summary>
public partial class TimeLabel : Label
{
	private GameState gameState;

	public override void _Ready()
	{
		gameState = GameState.Instance;
		if (gameState == null)
		{
			GD.PushError("TimeLabel: GameState 未就绪");
			return;
		}

		gameState.TimeChanged += OnTimeChanged;
		Text = gameState.GetClockText();
	}

	public override void _ExitTree()
	{
		if (GodotObject.IsInstanceValid(gameState))
		{
			gameState.TimeChanged -= OnTimeChanged;
		}
	}

	private void OnTimeChanged(int elapsedMinutes)
	{
		if (gameState == null) return;
		Text = gameState.GetClockText();
	}
}

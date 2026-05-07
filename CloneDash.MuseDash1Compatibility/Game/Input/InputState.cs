namespace CloneDash.Game.Input
{
	public struct InputState
	{
		/// <summary>
		/// How many times has the top key been clicked this frame
		/// </summary>
		public int TopClicked;
		/// <summary>
		/// How many times has the bottom key been clicked this frame
		/// </summary>
		public int BottomClicked;

		/// <summary>
		/// Is the top key held?
		/// </summary>
		public bool TopHeld => TopHeldCount > 0;
		/// <summary>
		/// Is the bottom key held?
		/// </summary>
		public bool BottomHeld => BottomHeldCount > 0;

		/// <summary>
		/// Is the top key held?
		/// </summary>
		public int TopHeldCount;
		/// <summary>
		/// Is the bottom key held?
		/// </summary>
		public int BottomHeldCount;

		/// <summary>
		/// Was the fever key pressed?
		/// </summary>
		public bool TryFever;

		/// <summary>
		/// Was the pause button pressed?
		/// </summary>
		public bool PauseButton;

		public InputState() {
			Reset();
		}

		public void Reset() {
			TopClicked = 0;
			BottomClicked = 0;
			TopHeldCount = 0;
			BottomHeldCount = 0;
			TryFever = false;
			PauseButton = false;
		}
	}
}

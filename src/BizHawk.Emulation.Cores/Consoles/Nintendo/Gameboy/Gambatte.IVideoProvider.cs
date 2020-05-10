namespace BizHawk.Emulation.Cores.Nintendo.Gameboy
{
	public partial class Gameboy
	{
		/// <summary>
		/// stored image of most recent frame
		/// </summary>
		private readonly int[] VideoBuffer = new int[160 * 144];

		public int[] GetVideoBuffer()
		{
			return VideoBuffer;
		}

		public int VirtualWidth => 160; // only sgb changes this, which we don't emulate here

		public int VirtualHeight => 144;

		public int BufferWidth => 160;

		public int BufferHeight => 144;

		public int BackgroundColor => 0;

		public int VsyncNumerator => 262144;

		public int VsyncDenominator => 4389;
	}
}

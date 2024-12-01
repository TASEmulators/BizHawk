namespace BizHawk.Emulation.Cores.Nintendo.GBA
{
	public partial class MGBAHawk : IGBAGPUViewable
	{
		public GBAGPUMemoryAreas GetMemoryAreas()
			=> _gpumem;

		public void SetScanlineCallback(Action callback, int scanline)
			=> _scanlinecb = callback;

		private Action _scanlinecb;

		private GBAGPUMemoryAreas _gpumem;
	}
}

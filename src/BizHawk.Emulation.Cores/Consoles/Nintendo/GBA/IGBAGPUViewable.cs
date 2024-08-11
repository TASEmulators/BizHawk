using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.GBA
{
	public interface IGBAGPUViewable : IEmulatorService
	{
		GBAGPUMemoryAreas GetMemoryAreas();

		/// <summary>
		/// calls correspond to entering hblank (maybe) and in a regular frame, the sequence of calls will be 160, 161, ..., 227, 0, ..., 159
		/// </summary>
		void SetScanlineCallback(Action callback, int scanline);
	}

	public class GBAGPUMemoryAreas
	{
		// the pointers are assumed to stay valid as long as the IEmulator is valid, maybe
		// this will need some change for a managed core (lifecycle management, etc)
		public IntPtr vram;
		public IntPtr oam;
		public IntPtr mmio;
		public IntPtr palram;
	}
}

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Sega.MasterSystem
{
	public interface ISmsGpuView : IEmulatorService
	{
		byte[] PatternBuffer { get; }
		int FrameHeight { get; }
		byte[] VRAM { get; }
		int[] Palette { get; }

		int CalcNameTableBase();
	}
}

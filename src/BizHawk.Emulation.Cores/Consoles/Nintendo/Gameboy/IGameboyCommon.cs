using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.Gameboy
{
	/// <param name="lcdc">current value of register $ff40 (LCDC)</param>
	public delegate void ScanlineCallback(byte lcdc);

	/// <param name="image">The image data</param>
	/// <param name="height">How tall an image is, in pixels. Image is only valid up to that height and must be assumed to be garbage below that.</param>
	/// <param name="top_margin">The top margin of blank pixels. Just form feeds the printer a certain amount at the top.</param>
	/// <param name="bottom_margin">The bottom margin of blank pixels. Just form feeds the printer a certain amount at the bottom.</param>
	/// <param name="exposure">The darkness/intensity of the print job. What the exact values mean is somewhat subjective but 127 is the most exposed/darkest value.</param>
	public delegate void PrinterCallback(IntPtr image, byte height, byte top_margin, byte bottom_margin, byte exposure);

	public interface ILinkedGameBoyCommon : ISpecializedEmulatorService
	{
		/// <remarks>really just for RetroAchivements; can be changed to a list later</remarks>
		IGameboyCommon First { get; }
	}

	public interface IGameboyCommon : ISpecializedEmulatorService
	{
		/// <value><see langword="true"/> iff the emulator is currently emulating CGB</value>
		bool IsCGBMode { get; }

		/// <value><see langword="true"/> iff the emulator is currently emulating CGB in DMG compatibility mode</value>
		/// <remarks>NOTE: this mode does not take affect until the bootrom unmaps itself</remarks>
		bool IsCGBDMGMode { get; }

		/// <summary>
		/// Acquire GPU memory for inspection.  The returned object must be disposed as soon as the frontend
		/// tool is done inspecting it, and the pointers become invalid once it is disposed.
		/// </summary>
		IGPUMemoryAreas LockGPU();

		/// <summary>
		/// set up callback
		/// </summary>
		/// <param name="line">scanline. -1 = end of frame, -2 = RIGHT NOW</param>
		void SetScanlineCallback(ScanlineCallback callback, int line);

		/// <summary>
		/// Set up printer callback
		/// </summary>
		/// <param name="callback">The callback to get the image. Setting this to non-null also "connects" the printer as the serial device.</param>
		void SetPrinterCallback(PrinterCallback callback);
	}

	public interface IGPUMemoryAreas : IDisposable
	{
		IntPtr Vram { get; }
		IntPtr Oam { get; }
		IntPtr Sppal { get; }
		IntPtr Bgpal { get; }
	}
}

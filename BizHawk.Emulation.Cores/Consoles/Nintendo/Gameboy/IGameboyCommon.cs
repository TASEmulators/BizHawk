using BizHawk.Common;
using BizHawk.Emulation.Common;
using System;

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

	public interface IGameboyCommon : ISpecializedEmulatorService
	{
		bool IsCGBMode();
		GPUMemoryAreas GetGPU();

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

	public class GPUMemoryAreas : IMonitor
	{
		public IntPtr Vram { get; }
		public IntPtr Oam { get; }
		public IntPtr Sppal { get; }
		public IntPtr Bgpal { get; }

		private readonly IMonitor _monitor;

		public GPUMemoryAreas(IntPtr vram, IntPtr oam, IntPtr sppal, IntPtr bgpal, IMonitor monitor = null)
		{
			Vram = vram;
			Oam = oam;
			Sppal = sppal;
			Bgpal = bgpal;
			_monitor = monitor;
		}

		public void Enter()
		{
			_monitor?.Enter();
		}

		public void Exit()
		{
			_monitor?.Exit();
		}
	}
}

using BizHawk.Common;
using BizHawk.Emulation.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.Gameboy
{
	/// <summary>
	/// </summary>
	/// <param name="lcdc">current value of register $ff40 (LCDC)</param>
	public delegate void ScanlineCallback(byte lcdc);

	public interface IGameboyCommon : ISpecializedEmulatorService
	{
		bool IsCGBMode();
		GPUMemoryAreas GetGPU();

		/// <summary>
		/// set up callback
		/// </summary>
		/// <param name="line">scanline. -1 = end of frame, -2 = RIGHT NOW</param>
		void SetScanlineCallback(ScanlineCallback callback, int line);
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	/// <summary>
	/// supports the PPU and NT viewers.  do not modify any returned arrays!
	/// </summary>
	public interface INESPPUViewable
	{
		/// <summary>
		/// get the 512 color overall palette in use
		/// </summary>
		/// <returns></returns>
		int[] GetPalette();

		/// <summary>
		/// true if bg tile indexes start at 0x1000 instead of 0x0000
		/// </summary>
		bool BGBaseHigh { get; }

		/// <summary>
		/// get the first 0x3000 bytes of ppu data
		/// </summary>
		/// <returns></returns>
		byte[] GetPPUBus();

		/// <summary>
		/// get the 32 byte palette ram
		/// </summary>
		/// <returns></returns>
		byte[] GetPalRam();

		/// <summary>
		/// return one byte of PPU bus data
		/// </summary>
		/// <param name="addr"></param>
		/// <returns></returns>
		byte PeekPPU(int addr);

		/// <summary>
		/// get MMC5 extile source data
		/// </summary>
		/// <returns></returns>
		byte[] GetExTiles();

		/// <summary>
		/// true if MMC5 and ExAttr mode is active
		/// </summary>
		bool ExActive { get; }

		/// <summary>
		/// get MMC5 exram for exattr mode
		/// </summary>
		/// <returns></returns>
		byte[] GetExRam();
	}
}

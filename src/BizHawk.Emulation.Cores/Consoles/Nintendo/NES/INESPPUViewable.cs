using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	/// <summary>
	/// supports the PPU and NT viewers.  do not modify any returned arrays!
	/// </summary>
	public interface INESPPUViewable : IEmulatorService
	{
		/// <summary>
		/// get the 512 color overall palette in use
		/// </summary>
		int[] GetPalette();

		/// <summary>
		/// true if bg tile indexes start at 0x1000 instead of 0x0000
		/// </summary>
		bool BGBaseHigh { get; }

		/// <summary>
		/// true if sp tile indexes start at 0x1000 instead of 0x0000 (8x8 mode only)
		/// </summary>
		bool SPBaseHigh { get; }

		/// <summary>
		/// true if sprites are 8x16
		/// </summary>
		bool SPTall { get; }

		/// <summary>
		/// get the first 0x3000 bytes of ppu data
		/// </summary>
		byte[] GetPPUBus();

		/// <summary>
		/// get the 32 byte palette ram
		/// </summary>
		byte[] GetPalRam();

		/// <summary>
		/// returns the object attribute memory
		/// </summary>
		byte[] GetOam();

		/// <summary>
		/// return one byte of PPU bus data
		/// </summary>
		byte PeekPPU(int addr);

		/// <summary>
		/// get MMC5 extile source data
		/// </summary>
		byte[] GetExTiles();

		/// <summary>
		/// true if MMC5 and ExAttr mode is active
		/// </summary>
		bool ExActive { get; }

		/// <summary>
		/// get MMC5 exram for exattr mode
		/// </summary>
		byte[] GetExRam();

		/// <summary>
		/// get memory domain for chr rom; return null if RAM or other N/A.  for direct viewing of ROM tiles.
		/// </summary>
		MemoryDomain GetCHRROM();

		/// <summary>
		/// install a callback to run at a particular scanline
		/// </summary>
		void InstallCallback1(Action cb, int sl);
		/// <summary>
		/// install a callback to run at a particular scanline
		/// </summary>
		void InstallCallback2(Action cb, int sl);

		/// <summary>
		/// remove previously installed callback
		/// </summary>
		void RemoveCallback1();
		/// <summary>
		/// remove previously installed callback
		/// </summary>
		void RemoveCallback2();
	}
}

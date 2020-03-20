using System.Collections.Generic;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	/// <summary>
	/// All information necessary for a board to set itself up
	/// </summary>
	public class CartInfo
	{
		public GameInfo DB_GameInfo;
		public string name;

		public int trainer_size;
		public int chr_size;
		public int prg_size;
		public int wram_size, vram_size;
		public byte pad_h, pad_v;
		public bool wram_battery;
		public bool bad;
		/// <summary>in [0,3]; combination of bits 0 and 3 of flags6.  try not to use; will be null for bootgod-identified roms always</summary>
		public int? inesmirroring;

		public string board_type;
		public string pcb;

		public string sha1;
		public string system;
		public List<string> chips = new List<string>();

		public string palette; // Palette override for VS system
		public byte vs_security; // for VS system games that do a ppu dheck

		public override string ToString() => string.Join(",",
			$"pr={prg_size}",
			$"ch={chr_size}",
			$"wr={wram_size}",
			$"vr={vram_size}",
			$"ba={(wram_battery ? 1 : 0)}",
			$"pa={pad_h}|{pad_v}",
			$"brd={board_type}",
			$"sys={system}");
	}
}

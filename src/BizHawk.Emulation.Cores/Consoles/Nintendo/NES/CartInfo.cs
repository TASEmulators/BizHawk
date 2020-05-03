using System.Collections.Generic;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	/// <summary>
	/// All information necessary for a board to set itself up
	/// </summary>
	public class CartInfo
	{
		public GameInfo GameInfo { get; set; }
		public string Name { get; set; }

		public int TrainerSize { get; set; }
		public int ChrSize { get; set; }
		public int PrgSize { get; set; }
		public int WramSize { get; set; }
		public int VramSize { get; set; }
		public byte PadH { get; set; }
		public byte PadV { get; set; }
		public bool WramBattery { get; set; }
		public bool Bad { get; set; }

		/// <summary>
		/// in [0,3]; combination of bits 0 and 3 of flags6.
		/// try not to use; will be null for BootGod-identified roms always
		/// </summary>
		public int? InesMirroring { get; set; }

		public string BoardType { get; set; }
		public string Pcb { get; set; }

		public string Sha1 { get; set; }
		public string System { get; set; }
		public List<string> Chips { get; set; } = new List<string>();

		public string Palette { get; set; } // Palette override for VS system
		public byte VsSecurity { get; set; } // for VS system games that do a ppu check

		public override string ToString() => string.Join(",",
			$"pr={PrgSize}",
			$"ch={ChrSize}",
			$"wr={WramSize}",
			$"vr={VramSize}",
			$"ba={(WramBattery ? 1 : 0)}",
			$"pa={PadH}|{PadV}",
			$"brd={BoardType}",
			$"sys={System}");
	}
}

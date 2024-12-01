using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	//generally mapper 3

	//Solomon's Key
	//Arkanoid
	//Arkista's Ring
	//Bump 'n' Jump
	//Cybernoid

	[NesBoardImplPriority]
	internal sealed class CNROM : NesBoardBase
	{
		//configuration
		private int prg_byte_mask, chr_mask;
		private bool copyprotection = false;
		private bool bus_conflict;
		private bool seicross;

		//state
		private int chr;
		private bool chr_enabled = true;

		public override bool Configure(EDetectionOrigin origin)
		{
			//configure
			switch (Cart.BoardType)
			{
				case "MAPPER185":
				case "HVC-CNROM+SECURITY":
				case "HVC-CNROM-256K-01":
					copyprotection = true;
					bus_conflict = true;
					AssertPrg(16, 32); AssertChr(8);
					break;
				case "MAPPER003":
					//we assume no bus conflicts for generic unknown cases.
					//this was done originally to support Colorful Dragon (Unl) (Sachen) which bugs out if bus conflicts are emulated
					//Games which behave otherwise will force us to start entering these in the game DB
					//Licensed titles below are more likely to have used the same original discrete logic design and so suffer from the conflicts
					bus_conflict = false;
					AssertPrg(8, 16, 32);
					break;
				case "Sachen_CNROM":
					bus_conflict = false;
					AssertPrg(16, 32);
					break;
				case "NES-CNROM": //adventure island
				case "UNIF_NES-CNROM": // some of these should be bus_conflict = false because UNIF is bad
				case "HVC-CNROM":
				case "TAITO-CNROM":
				case "BANDAI-CNROM":
				case "BANDAI-74*161/32": // untested
					bus_conflict = true;
					AssertPrg(16, 32);
					break;
				case "KONAMI-CNROM": //gradius (J)
					bus_conflict = true;
					AssertPrg(32); AssertChr(32);
					break;
				case "AVE-74*161":
					bus_conflict = true;
					AssertPrg(32); AssertChr(64);
					break;
				case "NTDEC-N715062": // untested
				case "NTDEC-N715061": // untested
					bus_conflict = true;
					AssertPrg(16, 32); AssertChr(8, 16, 32);
					break;

				case "BANDAI-PT-554": // untested
					// as seen on nescartdb, this board clearly has one of those audio sample chips on it,
					// but we don't implement that
					AssertPrg(32); AssertChr(32);
					bus_conflict = true;
					break;

				case "NAMCOT-CNROM+WRAM": // untested
					AssertPrg(32); AssertChr(32); AssertWram(2);
					break;
				default:
					return false;
			}
			if (Cart.Pcb == "9011-N02") // othello
				copyprotection = true;
			prg_byte_mask = Cart.PrgSize * 1024 - 1;
			chr_mask = (Cart.ChrSize / 8) - 1;
			SetMirrorType(Cart.PadH, Cart.PadV);

			if (Cart.Sha1 == NES.RomChecksums.SeicrossRev2) seicross = true;

			return true;
		}

		public override void WritePrg(int addr, byte value)
		{
			if (bus_conflict)
				value = HandleNormalPRGConflict(addr, value);

			chr = value & chr_mask;

			if (copyprotection)
			{
				if (seicross)
				{
					if (value != 0x21)
					{
						chr_enabled = true;
						Console.WriteLine("chr enabled");
					}
					else
					{
						chr_enabled = false;
						Console.WriteLine("chr disabled");
					}
				}
				else
				{
					if ((value & 0x0F) > 0 && (value != 0x13))
					{
						chr_enabled = true;
						Console.WriteLine("chr enabled");
					}
					else
					{
						chr_enabled = false;
						Console.WriteLine("chr disabled");
					}
				}
				
			}
		}

		public override byte ReadPpu(int addr)
		{
			if (!chr_enabled)
			{
				return 0x12;
			}
			if (addr < 0x2000)
			{
				return Vrom[addr + (chr << 13)];
			}
			else
			{
				return base.ReadPpu(addr);
			}
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync(nameof(chr), ref chr);
			ser.Sync(nameof(seicross), ref seicross);
			ser.Sync(nameof(chr_enabled), ref chr_enabled);
		}

		public override byte ReadPrg(int addr)
		{
			return Rom[addr & prg_byte_mask];
		}
	}
}

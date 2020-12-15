using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	//AKA mapper 071
	//TODO - apparently this mapper contains good nes timing test cases
	internal sealed class Camerica_Mapper071 : NesBoardBase
	{
		//configuration
		private int prg_bank_mask_16k;
		private bool mirror_control_enabled;

		//state
		private int[] prg_banks_16k = new int[2];

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync(nameof(prg_banks_16k), ref prg_banks_16k, false);
		}

		public override bool Configure(EDetectionOrigin origin)
		{
			//configure
			switch (Cart.BoardType)
			{
				case "MAPPER071":
					break;
				case "CAMERICA-ALGN": //Linus Spacehead's Cosmic Crusade (U)
					AssertPrg(128,256); AssertChr(0); AssertWram(0); 
					AssertVram(8,16); //zero 22-mar-2012 - added 16 here as a hack to make micro machines (aladdin) load. should remove this after it is fixed in the DB
					break;
				case "CAMERICA-BF9093": //Big Nose Freaks Out (U)
					AssertPrg(64,128,256); AssertChr(0); AssertWram(0); AssertVram(8);
					break;
				case "CAMERICA-BF9097": //Fire Hawk
					AssertPrg(128); AssertChr(0); AssertWram(0); AssertVram(8);
					mirror_control_enabled = true;
					break;
				case "CODEMASTERS-NR8N": // untested
					AssertPrg(256); AssertChr(0); AssertWram(0); AssertVram(8);
					break;
				default:
					return false;
			}

			prg_bank_mask_16k = Cart.PrgSize / 16 - 1;

			prg_banks_16k[0] = 0x00;
			prg_banks_16k[1] = 0xFF & prg_bank_mask_16k;

			SetMirrorType(Cart.PadH, Cart.PadV);

			return true;
		}

		public override void WritePrg(int addr, byte value)
		{
			addr &= 0x7000;
			switch (addr)
			{
				//$8000-9FFF:  [...M ....]  Mirroring (for Fire Hawk only!)
				case 0x0000:
				case 0x1000:
					if(mirror_control_enabled)
						SetMirrorType(value.Bit(4) ? EMirrorType.OneScreenB : EMirrorType.OneScreenA);
					break;
				
				//$C000-FFFF:  PRG Select (16k @ $8000)
				case 0x4000: case 0x5000:
				case 0x6000: case 0x7000:
					prg_banks_16k[0] = value & prg_bank_mask_16k;
					break;
			}
		}


		public override byte ReadPrg(int addr)
		{
			int bank_16k = addr >> 14;
			int ofs = addr & ((1 << 14) - 1);
			bank_16k = prg_banks_16k[bank_16k];
			addr = (bank_16k << 14) | ofs;
			return Rom[addr];
		}
	}

	//AKA mapper 232
	internal class Camerica_Mapper232 : NesBoardBase
	{
		//configuration
		private int prg_bank_mask_16k;

		//state
		private int[] prg_banks_16k = new int[2];
		private int prg_block, prg_page;

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync(nameof(prg_banks_16k), ref prg_banks_16k, false);
			ser.Sync(nameof(prg_block), ref prg_block);
			ser.Sync(nameof(prg_page), ref prg_page);
		}

		public override bool Configure(EDetectionOrigin origin)
		{
			//configure
			switch (Cart.BoardType)
			{
				case "MAPPER232":
					break;
				case "CAMERICA-ALGQ": //Quattro Adventure (U)
				case "CAMERICA-BF9096": //Quattro Arcade (U)
					AssertPrg(256); AssertChr(0); AssertWram(0); AssertVram(8);
					break;
				default:
					return false;
			}

			prg_bank_mask_16k = Cart.PrgSize / 16 - 1;
			
			SetMirrorType(Cart.PadH, Cart.PadV);
			SyncPRG();


			return true;
		}

		public override void WritePrg(int addr, byte value)
		{
			addr &= 0x4000;
			switch (addr)
			{
				case 0x0000:
					prg_block = (value>>3)&3;
					SyncPRG();
					break;
				case 0x4000:
					prg_page = value & 3;
					SyncPRG();
					break;
			}
		}

		private void SyncPRG()
		{
			prg_banks_16k[0] = (prg_block << 2) | prg_page;
			prg_banks_16k[1] = (prg_block << 2) | 3;
			prg_banks_16k[0] &= prg_bank_mask_16k;
			prg_banks_16k[1] &= prg_bank_mask_16k;
		}

		public override byte ReadPrg(int addr)
		{
			int bank_16k = addr >> 14;
			int ofs = addr & ((1 << 14) - 1);
			bank_16k = prg_banks_16k[bank_16k];
			addr = (bank_16k<<14) | ofs;
			return Rom[addr];
		}
	}

}
//TODO - emulation of mirroring is all bolloxed.

using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	internal sealed class Mapper116 : NesBoardBase
	{
		[NesBoardImplCancel]
		private class MMC3_CustomBoard : MMC3Board_Base
		{
			public override void WritePrg(int addr, byte value)
			{
				base.WritePrg(addr, value);
				SetMirrorType(mmc3.MirrorType);  //often redundant, but gets the job done
			}

			public override bool Configure(EDetectionOrigin origin)
			{
				BaseSetup();
				return true;
			}

			public MMC3_CustomBoard(Mapper116 master)
			{
				this.master = master;
			}

			public override void SyncIRQ(bool flag)
			{
				master.SyncIRQ(flag);
			}

			private readonly Mapper116 master;

		}

		//configuration

		//state
		private int mode;
		private SxROM mmc1;
		private MMC3_CustomBoard mmc3;
		private VRC2_4 vrc2;

		public override void SyncState(Serializer ser)
		{
			ser.Sync("mod", ref mode);
			ser.BeginSection("M116MMC1");
			mmc1.SyncState(ser);
			ser.EndSection();
			ser.BeginSection("M116MMC3");
			mmc3.SyncState(ser);
			ser.EndSection();
			ser.BeginSection("M116VRC2");
			vrc2.SyncState(ser);
			ser.EndSection();
			base.SyncState(ser);
			SyncIRQ(mmc3.mmc3.irq_pending);
		}

		public override bool Configure(EDetectionOrigin origin)
		{
			string oldBoardType = Cart.BoardType;

			//configure
			switch (Cart.BoardType)
			{
				case "MAPPER116":
					break;
				default:
					return false;
			}
			
			SetMirrorType(Cart.PadH, Cart.PadV);

			Cart.BoardType = "MAPPER116_HACKY";

			vrc2 = new VRC2_4();
			vrc2.Create(NES);
			vrc2.Configure(origin);
			//not exactly the same as fceu-mm. is it important?
			for(int i=0;i<16;i++)
				vrc2.chr_bank_reg_1k[i] = 0xFF;
			vrc2.SyncCHR();

			mmc3 = new MMC3_CustomBoard(this);
			mmc3.Create(NES);
			mmc3.Configure(origin);

			//is this important? not sure.
			mmc3.mmc3.regs[0] = 0;
			mmc3.mmc3.regs[1] = 2;
			mmc3.mmc3.regs[2] = 3;
			mmc3.mmc3.regs[3] = 4;
			mmc3.mmc3.regs[4] = 5;
			mmc3.mmc3.regs[5] = 7;
			mmc3.mmc3.Sync();

			mmc1 = new SxROM();
			mmc1.Create(NES);
			mmc1.Configure(origin);
			mmc1_reset();

			Cart.BoardType = oldBoardType;

			mode = 0;
			Sync();
			
			return true;
		}

		public override void PostConfigure()
		{
			SyncRoms();
			base.PostConfigure();
		}

		public override void SyncIRQ(bool flag)
		{
			if(mode == 1)
				base.SyncIRQ(flag);
		}

		private void SyncRoms()
		{
			foreach (var board in new NesBoardBase[] { mmc3, vrc2, mmc1 })
			{
				board.Rom = Rom;
				board.Vrom = Vrom;
				board.Vram = Vram;
				board.Wram = Wram;
			}
		}

		private void Sync()
		{
		}

		private void mmc1_reset()
		{
			mmc1.mmc1.StandardReset();
		}

		private void WriteModeControl(int addr, byte value)
		{
			if ((addr & 0x4100) != 0x4100)  return;
			mode = value & 3;
			bool chr_base = value.Bit(2);

			vrc2.extra_vrom = mmc3.extra_vrom = chr_base ? 256 * 1024 : 0;

			//fceu-mm has special "hacky hacky" logic here to initialize mmc1 a special way sometimes. read about it on the nesdevwiki. not sure how important it is
			if ((addr & 1) == 1)
			{
				mmc1_reset();
			}

			Sync();
			if(mode == 1) SyncIRQ(mmc3.mmc3.irq_pending);
			Console.Write("MODE: {0} ",mode);
			if (mode == 0) Console.WriteLine("(vrc2)");
			if (mode == 1) Console.WriteLine("(mmc3)");
			if (mode == 2) Console.WriteLine("(mmc1)");
			if (mode == 3) Console.WriteLine("(mmc1)");
		}

		public override void WriteExp(int addr, byte value)
		{
			WriteModeControl(addr + 0x4000, value);
		}

		public override void WritePpu(int addr, byte value)
		{
			switch (mode)
			{
				case 0: vrc2.WritePpu(addr, value); break;
				case 1: mmc3.WritePpu(addr, value); break;
				case 2:
				case 3: mmc1.WritePpu(addr, value); break;
			}
		}

		public override byte ReadPpu(int addr)
		{
			switch (mode)
			{
				case 0: return vrc2.ReadPpu(addr);
				case 1: return mmc3.ReadPpu(addr);
				case 2:
				case 3: return mmc1.ReadPpu(addr);

			}
			return 0;
		}

		public override void WritePrg(int addr, byte value)
		{
			Console.WriteLine("{0:X4} = {1:X2}", addr+0x8000, value);
			switch (mode)
			{
				case 0:
					addr += 0x8000;
					if((addr & 0xF000) < 0xB000) addr &= 0xF000; //Garou Densetsu Special depends on this
					addr -= 0x8000;

					vrc2.WritePrg(addr, value); 
					break;
				case 1: mmc3.WritePrg(addr, value); break;
				case 2:
				case 3: mmc1.WritePrg(addr, value); break;
			}
		}

		public override byte ReadPrg(int addr)
		{
			switch (mode)
			{
				case 0: return vrc2.ReadPrg(addr);
				case 1: return mmc3.ReadPrg(addr);
				case 2:
				case 3: return mmc1.ReadPrg(addr);
			}
			return 0;
		}

		public override void AddressPpu(int addr)
		{
			mmc3.AddressPpu(addr);
		}

		public override void ClockPpu()
		{
			switch (mode)
			{
				case 0: break;
				case 1: mmc3.ClockPpu(); break;
				case 2:
				case 3: mmc1.ClockPpu(); break;

			}
		}
	}
}

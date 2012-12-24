//TODO - emulation of mirroring is all bolloxed.

using System;
using System.IO;
using System.Diagnostics;

namespace BizHawk.Emulation.Consoles.Nintendo
{
	class Mapper116 : NES.NESBoardBase
	{
		[NES.INESBoardImplCancel]
		class MMC3_CustomBoard : MMC3Board_Base
		{
			public override void WritePRG(int addr, byte value)
			{
				base.WritePRG(addr, value);
				SetMirrorType(mmc3.MirrorType);  //often redundant, but gets the job done
			}

			public override bool Configure(NES.EDetectionOrigin origin)
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

			Mapper116 master;

		}

		//configuration

		//state
		int mode;
		SxROM mmc1;
		MMC3_CustomBoard mmc3;
		VRC2_4 vrc2;

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

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			string oldBoardType = Cart.board_type;

			//configure
			switch (Cart.board_type)
			{
				case "MAPPER116":
					break;
				default:
					return false;
			}
			
			SetMirrorType(Cart.pad_h, Cart.pad_v);

			Cart.board_type = "MAPPER116_HACKY";

			vrc2 = new VRC2_4();
			vrc2.Create(NES);
			vrc2.Configure(origin);
			//not exactly the same as fceu-mm. is it important?
			for(int i=0;i<16;i++)
				vrc2.chr_bank_reg_1k[i] = 0x0F;
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

			Cart.board_type = oldBoardType;

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
	
		void SyncRoms()
		{
			foreach (var board in new NES.NESBoardBase[] { mmc3, vrc2, mmc1 })
			{
				board.ROM = ROM;
				board.VROM = VROM;
				board.VRAM = VRAM;
				board.WRAM = WRAM;
			}
		}

		void Sync()
		{
		}

		void mmc1_reset()
		{
			mmc1.mmc1.StandardReset();
		}

		void WriteModeControl(int addr, byte value)
		{
			if ((addr & 0x4100) != 0x4100) return;
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

		public override void WriteEXP(int addr, byte value)
		{
			WriteModeControl(addr + 0x4000, value);
		}

		public override void WritePPU(int addr, byte value)
		{
			switch (mode)
			{
				case 0: vrc2.WritePPU(addr, value); break;
				case 1: mmc3.WritePPU(addr, value); break;
				case 2:
				case 3: mmc1.WritePPU(addr, value); break;
			}
		}

		public override byte ReadPPU(int addr)
		{
			switch (mode)
			{
				case 0: return vrc2.ReadPPU(addr);
				case 1: return mmc3.ReadPPU(addr);
				case 2:
				case 3: return mmc1.ReadPPU(addr);

			}
			return 0;
		}

		public override void WritePRG(int addr, byte value)
		{
			Console.WriteLine("{0:X4} = {1:X2}", addr+0x8000, value);
			switch (mode)
			{
				case 0:
					addr += 0x8000;
					if((addr & 0xF000) < 0xB000) addr &= 0xF000; //Garou Densetsu Special depends on this
					addr -= 0x8000;

					vrc2.WritePRG(addr, value); 
					break;
				case 1: mmc3.WritePRG(addr, value); break;
				case 2:
				case 3: mmc1.WritePRG(addr, value); break;
			}
		}

		public override byte ReadPRG(int addr)
		{
			switch (mode)
			{
				case 0: return vrc2.ReadPRG(addr);
				case 1: return mmc3.ReadPRG(addr);
				case 2:
				case 3: return mmc1.ReadPRG(addr);
			}
			return 0;
		}

		public override void AddressPPU(int addr)
		{
			mmc3.AddressPPU(addr);
		}

		public override void ClockPPU()
		{
			mmc3.ClockPPU();
		}
	}
}

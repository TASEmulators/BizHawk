using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public partial class NES : IStatable
	{
		public bool BinarySaveStatesPreferred
		{
			get { return false; }
		}

		public void SaveStateText(TextWriter writer)
		{
			SyncState(Serializer.CreateTextWriter(writer));
		}

		public void LoadStateText(TextReader reader)
		{
			SyncState(Serializer.CreateTextReader(reader));
			SetupMemoryDomains(); // resync the memory domains
		}

		public void SaveStateBinary(BinaryWriter bw)
		{
			SyncState(Serializer.CreateBinaryWriter(bw));
		}

		public void LoadStateBinary(BinaryReader br)
		{
			SyncState(Serializer.CreateBinaryReader(br));
			SetupMemoryDomains(); // resync the memory domains
		}

		public byte[] SaveStateBinary()
		{
			MemoryStream ms = new MemoryStream();
			BinaryWriter bw = new BinaryWriter(ms);
			SaveStateBinary(bw);
			bw.Flush();
			return ms.ToArray();
		}

		private void SyncState(Serializer ser)
		{
			int version = 4;
			ser.BeginSection("NES");
			ser.Sync("version", ref version);
			ser.Sync("Frame", ref _frame);
			ser.Sync("Lag", ref _lagcount);
			ser.Sync("IsLag", ref islag);
			cpu.SyncState(ser);
			ser.Sync("ram", ref ram, false);
			ser.Sync("CIRAM", ref CIRAM, false);
			ser.Sync("cpu_accumulate", ref cpu_accumulate);
			ser.Sync("_irq_apu", ref _irq_apu);
			ser.Sync("sprdma_countdown", ref sprdma_countdown);
			ser.Sync("cpu_step", ref cpu_step);
			ser.Sync("cpu_stepcounter", ref cpu_stepcounter);
			ser.Sync("cpu_deadcounter", ref cpu_deadcounter);

			//oam related
			ser.Sync("Oam_Dma_Index", ref oam_dma_index);
			ser.Sync("Oam_Dma_Exec", ref oam_dma_exec);
			ser.Sync("Oam_Dma_Addr", ref oam_dma_addr);
			ser.Sync("Oam_Dma_Byte", ref oam_dma_byte);
			ser.Sync("Dmc_Dma_Exec", ref dmc_dma_exec);
			ser.Sync("dmc_realign", ref dmc_realign);
			ser.Sync("IRQ_delay", ref IRQ_delay);
			ser.Sync("special_case_delay", ref special_case_delay);
			ser.Sync("do_the_reread", ref do_the_reread);

			//VS related
			ser.Sync("VS", ref _isVS);
			ser.Sync("VS_2c05", ref _isVS2c05);
			ser.Sync("VS_CHR", ref VS_chr_reg);
			ser.Sync("VS_PRG", ref VS_prg_reg);
			ser.Sync("VS_DIPS", ref VS_dips, false);
			ser.Sync("VS_Service", ref VS_service);
			ser.Sync("VS_Coin", ref VS_coin_inserted);
			ser.Sync("VS_ROM_Control", ref VS_ROM_control);

		ser.BeginSection("Board");
			Board.SyncState(ser);
			if (Board is NESBoardBase && !((NESBoardBase)Board).SyncStateFlag)
				throw new InvalidOperationException("the current NES mapper didnt call base.SyncState");
			ser.EndSection();
			ppu.SyncState(ser);
			apu.SyncState(ser);

			if (version >= 2)
			{
				ser.Sync("DB", ref DB);
			}
			if (version >= 3)
			{
				ser.Sync("latched4016", ref latched4016);
				ser.BeginSection("ControllerDeck");
				ControllerDeck.SyncState(ser);
				ser.EndSection();
			}
			if (version >= 4)
			{
				ser.Sync("resetSignal", ref resetSignal);
				ser.Sync("hardResetSignal", ref hardResetSignal);
			}

			ser.EndSection();
		}
	}
}

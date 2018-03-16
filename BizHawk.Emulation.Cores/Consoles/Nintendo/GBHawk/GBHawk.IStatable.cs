using System.IO;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.GBHawk
{
	public partial class GBHawk : IStatable
	{
		public bool BinarySaveStatesPreferred => true;

		public void SaveStateText(TextWriter writer)
		{
			SyncState(new Serializer(writer));
		}

		public void LoadStateText(TextReader reader)
		{
			SyncState(new Serializer(reader));
		}

		public void SaveStateBinary(BinaryWriter bw)
		{
			SyncState(new Serializer(bw));
		}

		public void LoadStateBinary(BinaryReader br)
		{
			SyncState(new Serializer(br));
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
			byte[] core = null;
			if (ser.IsWriter)
			{
				var ms = new MemoryStream();
				ms.Close();
				core = ms.ToArray();
			}
			cpu.SyncState(ser);
			mapper.SyncState(ser);
			timer.SyncState(ser);
			ppu.SyncState(ser);
			serialport.SyncState(ser);
			audio.SyncState(ser);

			ser.BeginSection("Gameboy");
			ser.Sync("core", ref core, false);
			ser.Sync("Lag", ref _lagcount);
			ser.Sync("Frame", ref _frame);
			ser.Sync("IsLag", ref _islag);
			_controllerDeck.SyncState(ser);

			ser.Sync("controller_state", ref controller_state);
			ser.Sync("in_vblank", ref in_vblank);
			ser.Sync("in_vblank_old", ref in_vblank_old);	
			ser.Sync("vblank_rise", ref vblank_rise);
			ser.Sync("GB_bios_register", ref GB_bios_register);
			ser.Sync("input_register", ref input_register);

			ser.Sync("REG_FFFF", ref REG_FFFF);
			ser.Sync("REG_FF0F", ref REG_FF0F);
			ser.Sync("enable_VBL", ref enable_VBL);
			ser.Sync("enable_LCDC", ref enable_PRS);
			ser.Sync("enable_TIMO", ref enable_TIMO);
			ser.Sync("enable_SER", ref enable_SER);
			ser.Sync("enable_STAT", ref enable_STAT);

			// memory domains
			ser.Sync("RAM", ref RAM, false);
			ser.Sync("ZP_RAM", ref ZP_RAM, false);
			ser.Sync("CHR_RAM", ref CHR_RAM, false);
			ser.Sync("BG_map_1", ref BG_map_1, false);
			ser.Sync("BG_map_2", ref BG_map_2, false);
			ser.Sync("OAM", ref OAM, false);

			ser.Sync("Use_RTC", ref Use_RTC);

			// probably a better way to do this
			if (cart_RAM != null)
			{
				ser.Sync("cart_RAM", ref cart_RAM, false);
			}



		ser.EndSection();
		}
	}
}

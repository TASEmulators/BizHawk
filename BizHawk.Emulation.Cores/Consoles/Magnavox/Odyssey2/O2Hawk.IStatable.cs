using System.IO;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.O2Hawk
{
	public partial class O2Hawk : IStatable
	{
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
			using var ms = new MemoryStream();
			using var bw = new BinaryWriter(ms);
			SaveStateBinary(bw);
			bw.Flush();
			return ms.ToArray();
		}

		private void SyncState(Serializer ser)
		{
			byte[] core = null;
			if (ser.IsWriter)
			{
				using var ms = new MemoryStream();
				ms.Close();
				core = ms.ToArray();
			}
			cpu.SyncState(ser);
			mapper.SyncState(ser);
			ppu.SyncState(ser);
			serialport.SyncState(ser);

			ser.BeginSection("Odyssey2");
			ser.Sync(nameof(core), ref core, false);
			ser.Sync("Lag", ref _lagcount);
			ser.Sync("Frame", ref _frame);
			ser.Sync("IsLag", ref _islag);
			_controllerDeck.SyncState(ser);

			ser.Sync(nameof(controller_state_1), ref controller_state_1);
			ser.Sync(nameof(controller_state_2), ref controller_state_2);
			ser.Sync(nameof(in_vblank), ref in_vblank);
			ser.Sync(nameof(in_vblank_old), ref in_vblank_old);	
			ser.Sync(nameof(vblank_rise), ref vblank_rise);

			ser.Sync(nameof(RAM_en), ref RAM_en);
			ser.Sync(nameof(ppu_en), ref ppu_en);
			ser.Sync(nameof(cart_b0), ref cart_b0);
			ser.Sync(nameof(cart_b1), ref cart_b1);
			ser.Sync(nameof(lum_en), ref lum_en);
			ser.Sync(nameof(copy_en), ref copy_en);
			ser.Sync(nameof(kybrd_en), ref kybrd_en);
			ser.Sync(nameof(rom_bank), ref rom_bank);

			// memory domains
			ser.Sync(nameof(RAM), ref RAM, false);
			ser.Sync(nameof(_bios), ref _bios, false);
			ser.Sync(nameof(addr_latch), ref addr_latch);
			ser.Sync(nameof(kb_byte), ref kb_byte);
			ser.Sync(nameof(kb_state_row), ref kb_state_row);
			ser.Sync(nameof(kb_state_col), ref kb_state_col);

			ser.Sync(nameof(frame_buffer), ref frame_buffer, false);
			ser.Sync(nameof(_vidbuffer), ref _vidbuffer, false);

			// probably a better way to do this
			if (cart_RAM != null)
			{
				ser.Sync(nameof(cart_RAM), ref cart_RAM, false);
			}

			ser.EndSection();
		}
	}
}

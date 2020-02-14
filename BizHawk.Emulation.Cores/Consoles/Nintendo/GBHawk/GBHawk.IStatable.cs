using System.IO;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.GBHawk
{
	public partial class GBHawk : IStatable
	{
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
			timer.SyncState(ser);
			ppu.SyncState(ser);
			serialport.SyncState(ser);
			audio.SyncState(ser);

			ser.BeginSection("Gameboy");
			ser.Sync(nameof(core), ref core, false);
			ser.Sync("Lag", ref _lagcount);
			ser.Sync("Frame", ref _frame);
			ser.Sync("IsLag", ref _islag);
			_controllerDeck.SyncState(ser);

			ser.Sync(nameof(controller_state), ref controller_state);
			ser.Sync(nameof(Acc_X_state), ref Acc_X_state);
			ser.Sync(nameof(Acc_Y_state), ref Acc_Y_state);
			ser.Sync(nameof(in_vblank), ref in_vblank);
			ser.Sync(nameof(in_vblank_old), ref in_vblank_old);	
			ser.Sync(nameof(vblank_rise), ref vblank_rise);
			ser.Sync(nameof(GB_bios_register), ref GB_bios_register);
			ser.Sync(nameof(input_register), ref input_register);

			ser.Sync(nameof(REG_FFFF), ref REG_FFFF);
			ser.Sync(nameof(REG_FF0F), ref REG_FF0F);
			ser.Sync(nameof(REG_FF0F_OLD), ref REG_FF0F_OLD);

			// memory domains
			ser.Sync(nameof(RAM), ref RAM, false);
			ser.Sync(nameof(ZP_RAM), ref ZP_RAM, false);
			ser.Sync(nameof(VRAM), ref VRAM, false);
			ser.Sync(nameof(OAM), ref OAM, false);

			ser.Sync(nameof(_bios), ref _bios, false);

			ser.Sync(nameof(RAM_Bank), ref RAM_Bank);
			ser.Sync(nameof(VRAM_Bank), ref VRAM_Bank);
			ser.Sync(nameof(is_GBC), ref is_GBC);
			ser.Sync(nameof(GBC_compat), ref GBC_compat);
			ser.Sync(nameof(double_speed), ref double_speed);
			ser.Sync(nameof(speed_switch), ref speed_switch);
			ser.Sync(nameof(HDMA_transfer), ref HDMA_transfer);

			ser.Sync(nameof(IR_reg), ref IR_reg);
			ser.Sync(nameof(IR_mask), ref IR_mask);
			ser.Sync(nameof(IR_signal), ref IR_signal);
			ser.Sync(nameof(IR_receive), ref IR_receive);
			ser.Sync(nameof(IR_self), ref IR_self);
			ser.Sync(nameof(IR_write), ref IR_write);

			ser.Sync(nameof(undoc_6C), ref undoc_6C);
			ser.Sync(nameof(undoc_72), ref undoc_72);
			ser.Sync(nameof(undoc_73), ref undoc_73);
			ser.Sync(nameof(undoc_74), ref undoc_74);
			ser.Sync(nameof(undoc_75), ref undoc_75);
			ser.Sync(nameof(undoc_76), ref undoc_76);
			ser.Sync(nameof(undoc_77), ref undoc_77);

			ser.Sync(nameof(Use_MT), ref Use_MT);
			ser.Sync(nameof(addr_access), ref addr_access);

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

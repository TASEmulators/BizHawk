using System.IO;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Vectrex
{
	public partial class VectrexHawk : IStatable
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
			ppu.SyncState(ser);
			serialport.SyncState(ser);
			audio.SyncState(ser);

			ser.BeginSection("VECTREX");

			ser.Sync(nameof(RAM), ref RAM, false);

			ser.Sync(nameof(dir_dac), ref dir_dac);
			ser.Sync(nameof(dir_ctrl), ref dir_ctrl);

			ser.Sync(nameof(portB_ret), ref portB_ret);
			ser.Sync(nameof(portA_ret), ref portA_ret);
			ser.Sync(nameof(reg_A), ref reg_A);
			ser.Sync(nameof(reg_B), ref reg_B);

			ser.Sync(nameof(t1_low), ref t1_low);
			ser.Sync(nameof(t1_high), ref t1_high);
			ser.Sync(nameof(t1_counter), ref t1_counter);
			ser.Sync(nameof(t1_shot_go), ref t1_shot_go);
			ser.Sync(nameof(PB7), ref PB7);
			ser.Sync(nameof(PB7_prev), ref PB7_prev);

			ser.Sync(nameof(t2_low), ref t2_low);
			ser.Sync(nameof(t2_high), ref t2_high);
			ser.Sync(nameof(t2_counter), ref t2_counter);
			ser.Sync(nameof(t2_shot_go), ref t2_shot_go);
			ser.Sync(nameof(PB6), ref PB6);
			ser.Sync(nameof(PB6_prev), ref PB6_prev);

			ser.Sync(nameof(int_en), ref int_en);
			ser.Sync(nameof(int_fl), ref int_fl);
			ser.Sync(nameof(aux_ctrl), ref aux_ctrl);
			ser.Sync(nameof(prt_ctrl), ref prt_ctrl);

			ser.Sync(nameof(sw), ref sw);
			ser.Sync(nameof(sel0), ref sel0);
			ser.Sync(nameof(sel1), ref sel1);
			ser.Sync(nameof(bc1), ref bc1);
			ser.Sync(nameof(bdir), ref bdir);
			ser.Sync(nameof(compare), ref compare);

			ser.Sync(nameof(_frame), ref _frame);
			ser.Sync(nameof(_lagcount), ref _lagcount);
			ser.Sync(nameof(_islag), ref _islag);

			ser.Sync(nameof(shift_start), ref shift_start);
			ser.Sync(nameof(shift_reg), ref shift_reg);
			ser.Sync(nameof(shift_reg_wait), ref shift_reg_wait);
			ser.Sync(nameof(shift_count), ref shift_count);

			ser.Sync(nameof(frame_end), ref frame_end);
			ser.Sync(nameof(PB7_undriven), ref PB7_undriven);
			ser.Sync(nameof(pot_val), ref pot_val);

			ser.Sync(nameof(joy1_LR), ref joy1_LR);
			ser.Sync(nameof(joy1_UD), ref joy1_UD);
			ser.Sync(nameof(joy2_LR), ref joy2_LR);
			ser.Sync(nameof(joy2_UD), ref joy2_UD);

			ser.Sync(nameof(_framebuffer), ref _framebuffer, false);
			ser.Sync(nameof(_vidbuffer), ref _vidbuffer, false);

			ser.EndSection();
		}
	}
}

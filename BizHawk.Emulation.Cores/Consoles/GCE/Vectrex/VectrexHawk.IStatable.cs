using System.IO;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Vectrex
{
	public partial class VectrexHawk : IStatable
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

			ser.Sync(nameof(sw), ref sw);
			ser.Sync(nameof(sel0), ref sel0);
			ser.Sync(nameof(sel1), ref sel1);
			ser.Sync(nameof(bc1), ref bc1);
			ser.Sync(nameof(bdir), ref bdir);
			ser.Sync(nameof(compare), ref compare);
			ser.Sync(nameof(ramp), ref ramp);

			ser.Sync(nameof(_frame), ref _frame);
			ser.Sync(nameof(_lagcount), ref _lagcount);
			ser.Sync(nameof(_islag), ref _islag);



			// probably a better way to do this
			if (cart_RAM != null)
			{
				ser.Sync(nameof(cart_RAM), ref cart_RAM, false);
			}

			ser.EndSection();
		}
	}
}

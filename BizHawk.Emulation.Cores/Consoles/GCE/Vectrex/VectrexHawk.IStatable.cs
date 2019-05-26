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

			ser.Sync(nameof(t1_low), ref t1_low);
			ser.Sync(nameof(t1_high), ref t1_high);
			ser.Sync(nameof(t1_counter), ref t1_counter);
			ser.Sync(nameof(t1_on), ref t1_on);
			ser.Sync(nameof(t1_shot_done), ref t1_shot_done);
			ser.Sync(nameof(PB7), ref PB7);

			ser.Sync(nameof(int_en), ref int_en);
			ser.Sync(nameof(int_fl), ref int_fl);
			ser.Sync(nameof(aux_ctrl), ref aux_ctrl);

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

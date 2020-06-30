using System.IO;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.GBHawkLink4x
{
	public partial class GBHawkLink4x : IStatable
	{
		private readonly IStatable _aStates;
		private readonly IStatable _bStates;
		private readonly IStatable _cStates;
		private readonly IStatable _dStates;

		public void SaveStateBinary(BinaryWriter bw)
		{
			_aStates.SaveStateBinary(bw);
			_bStates.SaveStateBinary(bw);
			_cStates.SaveStateBinary(bw);
			_dStates.SaveStateBinary(bw);
			// other variables
			SyncState(new Serializer(bw));
		}

		public void LoadStateBinary(BinaryReader br)
		{
			_aStates.LoadStateBinary(br);
			_bStates.LoadStateBinary(br);
			_cStates.LoadStateBinary(br);
			_dStates.LoadStateBinary(br);
			// other variables
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
			ser.Sync("Lag", ref _lagcount);
			ser.Sync("Frame", ref _frame);
			ser.Sync("IsLag", ref _islag);
			ser.Sync(nameof(_cableconnected_UD), ref _cableconnected_UD);
			ser.Sync(nameof(_cableconnected_LR), ref _cableconnected_LR);
			ser.Sync(nameof(_cableconnected_X), ref _cableconnected_X);
			ser.Sync(nameof(_cableconnected_4x), ref _cableconnected_4x);
			ser.Sync(nameof(do_2_next_1), ref do_2_next_1);
			ser.Sync(nameof(do_2_next_2), ref do_2_next_2);
			ser.Sync(nameof(A_controller), ref A_controller);
			ser.Sync(nameof(B_controller), ref B_controller);
			ser.Sync(nameof(C_controller), ref C_controller);
			ser.Sync(nameof(D_controller), ref D_controller);

			ser.Sync(nameof(is_pinging), ref is_pinging);
			ser.Sync(nameof(is_transmitting), ref is_transmitting);
			ser.Sync(nameof(status_byte), ref status_byte);
			ser.Sync(nameof(x4_clock), ref x4_clock);
			ser.Sync(nameof(ping_player), ref ping_player);
			ser.Sync(nameof(ping_byte), ref ping_byte);
			ser.Sync(nameof(bit_count), ref bit_count);
			ser.Sync(nameof(received_byte), ref received_byte);
			ser.Sync(nameof(begin_transmitting_cnt), ref begin_transmitting_cnt);
			ser.Sync(nameof(transmit_speed), ref transmit_speed);
			ser.Sync(nameof(num_bytes_transmit), ref num_bytes_transmit);
			ser.Sync(nameof(time_out_check), ref time_out_check);
			ser.Sync(nameof(ready_to_transmit), ref ready_to_transmit);
			ser.Sync(nameof(transmit_byte), ref transmit_byte);
			ser.Sync(nameof(x4_buffer), ref x4_buffer, false);
			ser.Sync(nameof(buffer_parity), ref buffer_parity);
			ser.Sync(nameof(pre_transmit), ref pre_transmit);
			ser.Sync(nameof(temp1_rec), ref temp1_rec);
			ser.Sync(nameof(temp2_rec), ref temp2_rec);
			ser.Sync(nameof(temp3_rec), ref temp3_rec);
			ser.Sync(nameof(temp4_rec), ref temp4_rec);

			_controllerDeck.SyncState(ser);

			if (ser.IsReader)
			{
				FillVideoBuffer();
			}
		}
	}
}

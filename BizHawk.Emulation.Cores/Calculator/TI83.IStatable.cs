using System.IO;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Calculators
{
	public partial class TI83 : ITextStatable
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
			if (ser.IsWriter)
			{
				var ms = new MemoryStream();
				ms.Close();
				ms.ToArray();
			}
			_cpu.SyncState(ser);

			ser.BeginSection(nameof(TI83));
			ser.Sync("RAM", ref _ram, false);
			ser.Sync("romPageLow3Bits", ref _romPageLow3Bits);
			ser.Sync("romPageHighBit", ref _romPageHighBit);
			ser.Sync("disp_mode", ref _displayMode);
			ser.Sync("disp_move", ref _displayMove);
			ser.Sync("disp_x", ref _displayX);
			ser.Sync("disp_y", ref _displayY);
			ser.Sync("m_CursorMoved", ref _cursorMoved);
			ser.Sync("maskOn", ref _maskOn);
			ser.Sync("onPressed", ref _onPressed);
			ser.Sync("keyboardMask", ref _keyboardMask);
			ser.Sync("m_LinkOutput", ref _linkOutput);
			ser.Sync("VRAM", ref _vram, false);
			ser.Sync("Frame", ref _frame);
			ser.Sync("LagCount", ref _lagCount);
			ser.Sync("IsLag", ref _isLag);
			ser.Sync(nameof(ON_key_int), ref ON_key_int);
			ser.Sync(nameof(ON_key_int_EN), ref ON_key_int_EN);
			ser.Sync(nameof(TIM_1_int), ref TIM_1_int);
			ser.Sync(nameof(TIM_1_int_EN), ref TIM_1_int_EN);
			ser.Sync(nameof(TIM_frq), ref TIM_frq);
			ser.Sync(nameof(TIM_mult), ref TIM_mult);
			ser.Sync(nameof(TIM_count), ref TIM_count);
			ser.Sync(nameof(TIM_hit), ref TIM_hit);

			ser.EndSection();

			if (ser.IsReader)
			{
				SyncAllByteArrayDomains();
			}
		}
	}
}

using System;
using System.IO;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Libretro
{
	public partial class LibretroEmulator : IStatable
	{
		private byte[] _stateBuf;
		private long _stateLen;

		public void SaveStateBinary(BinaryWriter writer)
		{
			UpdateCallbackHandler();

			_stateLen = api.retro_serialize_size();
			if (_stateBuf.LongLength != _stateLen)
			{
				_stateBuf = new byte[_stateLen];
			}

			var d = new RetroData(_stateBuf, _stateLen);
			api.retro_serialize(d.PinnedData, d.Length);
			writer.Write(_stateBuf.Length);
			writer.Write(_stateBuf);
			// other variables
			writer.Write(Frame);
			writer.Write(LagCount);
			writer.Write(IsLagFrame);
		}

		public void LoadStateBinary(BinaryReader reader)
		{
			UpdateCallbackHandler();

			var newlen = reader.ReadInt32();
			if (newlen > _stateBuf.Length)
			{
				throw new Exception("Unexpected buffer size");
			}

			reader.Read(_stateBuf, 0, newlen);
			var d = new RetroData(_stateBuf, _stateLen);
			api.retro_unserialize(d.PinnedData, d.Length);
			// other variables
			Frame = reader.ReadInt32();
			LagCount = reader.ReadInt32();
			IsLagFrame = reader.ReadBoolean();
		}
	}
}

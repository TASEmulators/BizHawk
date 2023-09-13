using System;
using System.IO;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Libretro
{
	// not all Libretro cores implement savestates
	// we use this so we can optionally register IStatable
	// todo: this can probably be genericized
	public class StatableLibretro : IStatable
	{
		private readonly LibretroHost _host;
		private readonly LibretroApi _api;
		private readonly byte[] _stateBuf;

		public StatableLibretro(LibretroHost host, LibretroApi api, int maxSize)
		{
			_host = host;
			_api = api;
			_stateBuf = new byte[maxSize];
		}

		public bool AvoidRewind => false;

		public void SaveStateBinary(BinaryWriter writer)
		{
			var len = checked((int)_api.retro_serialize_size());
			if (len > _stateBuf.Length)
			{
				throw new Exception("Core attempted to grow state size. This is not allowed per the libretro API.");
			}

			_api.retro_serialize(_stateBuf, len);
			writer.Write(len);
			writer.Write(_stateBuf, 0, len);

			// host variables
			writer.Write(_host.Frame);
			writer.Write(_host.LagCount);
			writer.Write(_host.IsLagFrame);
		}

		public void LoadStateBinary(BinaryReader reader)
		{
			var len = reader.ReadInt32();
			if (len > _stateBuf.Length)
			{
				throw new Exception("State buffer size exceeded the core's maximum state size!");
			}

			reader.Read(_stateBuf, 0, len);
			_api.retro_unserialize(_stateBuf, len);

			// host variables
			_host.Frame = reader.ReadInt32();
			_host.LagCount = reader.ReadInt32();
			_host.IsLagFrame = reader.ReadBoolean();
		}
	}
}

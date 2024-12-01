using System.IO;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Libretro
{
	// note: Not all Libretro cores implement savestates
	public partial class LibretroHost : IStatable
	{
		private byte[] _stateBuf = [ ];

		public bool AvoidRewind => false;

		public void SaveStateBinary(BinaryWriter writer)
		{
			var len = checked((int)api.retro_serialize_size());
			if (len > _stateBuf.Length)
			{
				throw new Exception("Core attempted to grow state size. This is not allowed per the libretro API.");
			}

			api.retro_serialize(_stateBuf, len);
			writer.Write(len);
			writer.Write(_stateBuf, 0, len);

			// host variables
			writer.Write(Frame);
			writer.Write(LagCount);
			writer.Write(IsLagFrame);
		}

		public void LoadStateBinary(BinaryReader reader)
		{
			var len = reader.ReadInt32();
			if (len > _stateBuf.Length)
			{
				throw new Exception("State buffer size exceeded the core's maximum state size!");
			}

			_ = reader.Read(_stateBuf, 0, len);
			api.retro_unserialize(_stateBuf, len);

			// host variables
			Frame = reader.ReadInt32();
			LagCount = reader.ReadInt32();
			IsLagFrame = reader.ReadBoolean();
		}
	}
}

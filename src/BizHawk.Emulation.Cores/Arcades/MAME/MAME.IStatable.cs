using System;
using System.IO;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Arcades.MAME
{
	public partial class MAME : IStatable
	{
		private byte[] _mameSaveBuffer;
		private byte[] _hawkSaveBuffer;

		public void SaveStateBinary(BinaryWriter writer)
		{
			writer.Write(_mameSaveBuffer.Length);

			LibMAME.SaveError err = LibMAME.mame_save_buffer(_mameSaveBuffer, out int length);

			if (length != _mameSaveBuffer.Length)
			{
				throw new InvalidOperationException("Savestate buffer size mismatch!");
			}

			if (err != LibMAME.SaveError.NONE)
			{
				throw new InvalidOperationException("MAME LOADSTATE ERROR: " + err.ToString());
			}

			writer.Write(_mameSaveBuffer);
			writer.Write(Frame);
			writer.Write(LagCount);
			writer.Write(IsLagFrame);
		}

		public void LoadStateBinary(BinaryReader reader)
		{
			int length = reader.ReadInt32();

			if (length != _mameSaveBuffer.Length)
			{
				throw new InvalidOperationException("Savestate buffer size mismatch!");
			}

			reader.Read(_mameSaveBuffer, 0, _mameSaveBuffer.Length);
			LibMAME.SaveError err = LibMAME.mame_load_buffer(_mameSaveBuffer, _mameSaveBuffer.Length);

			if (err != LibMAME.SaveError.NONE)
			{
				throw new InvalidOperationException("MAME SAVESTATE ERROR: " + err.ToString());
			}

			Frame = reader.ReadInt32();
			LagCount = reader.ReadInt32();
			IsLagFrame = reader.ReadBoolean();
		}
	}
}
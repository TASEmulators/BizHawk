using System;
using System.IO;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.Sameboy
{
	public partial class Sameboy : IStatable
	{
		private readonly byte[] _stateBuf;

		public void SaveStateBinary(BinaryWriter writer)
		{
			LibSameboy.sameboy_savestate(SameboyState, _stateBuf);

			writer.Write(_stateBuf.Length);
			writer.Write(_stateBuf);

			// other variables
			writer.Write(IsLagFrame);
			writer.Write(LagCount);
			writer.Write(Frame);
			writer.Write(IsCgb);
			writer.Write(CycleCount);
		}

		public void LoadStateBinary(BinaryReader reader)
		{
			Directory.CreateDirectory("sameboy_debug");
			int len = LibSameboy.sameboy_corelen(SameboyState);
			byte[] bytes = new byte[len];
			unsafe
			{
				byte* core = (byte*)SameboyState;
				for (int i = 0; i < len; i++)
				{
					bytes[i] = core[i];
				}
			}

			int length = reader.ReadInt32();
			if (length != _stateBuf.Length)
			{
				throw new InvalidOperationException("Savestate buffer size mismatch!");
			}

			reader.Read(_stateBuf, 0, _stateBuf.Length);

			if (LibSameboy.sameboy_loadstate(SameboyState, _stateBuf, _stateBuf.Length))
			{
				throw new Exception($"{nameof(LibSameboy.sameboy_loadstate)}() returned true");
			}

			// other variables
			IsLagFrame = reader.ReadBoolean();
			LagCount = reader.ReadInt32();
			Frame = reader.ReadInt32();
			IsCgb = reader.ReadBoolean();
			CycleCount = reader.ReadInt64();

			int num = 0;
			while (File.Exists($"sameboy_debug/gameboy_gb_t{Frame}_{num}_preloadstate.bin"))
			{
				num++;
			}

			File.WriteAllBytes($"sameboy_debug/gameboy_gb_t{Frame}_{num}_preloadstate.bin", bytes);

			unsafe
			{
				byte* core = (byte*)SameboyState;
				for (int i = 0; i < len; i++)
				{
					bytes[i] = core[i];
				}
			}
			File.WriteAllBytes($"sameboy_debug/gameboy_gb_t{Frame}_{num}_postloadstate.bin", bytes);
		}
	}
}

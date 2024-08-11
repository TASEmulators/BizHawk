using System.IO;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.Sameboy
{
	public partial class Sameboy : IStatable
	{
		private readonly byte[] _stateBuf;

		public bool AvoidRewind => false;

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
			writer.Write(theta);
			writer.Write(phi);
			writer.Write(theta_prev);
			writer.Write(phi_prev);
			writer.Write(phi_prev_2);
			writer.Write(_curTrack);
			writer.Write(_switchingTrack);
		}

		public void LoadStateBinary(BinaryReader reader)
		{
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
			theta = reader.ReadDouble();
			phi = reader.ReadDouble();
			theta_prev = reader.ReadDouble();
			phi_prev = reader.ReadDouble();
			phi_prev_2 = reader.ReadDouble();
			_curTrack = reader.ReadInt32();
			_switchingTrack = reader.ReadBoolean();
		}

		public void DebugSameBoyState()
		{
			LibSameboy.sameboy_savestate(SameboyState, _stateBuf);
			Directory.CreateDirectory("sameboy_debug");
			File.WriteAllBytes($"sameboy_debug/debug_state{Frame}.bin", _stateBuf);
		}
	}
}

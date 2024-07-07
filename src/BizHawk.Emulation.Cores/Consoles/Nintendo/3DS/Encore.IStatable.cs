using System.IO;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.N3DS
{
	public partial class Encore : IStatable
	{
		private byte[] _stateBuf = Array.Empty<byte>();

		public bool AvoidRewind => true;

		public void SaveStateBinary(BinaryWriter writer)
		{
			var stateLen = _core.Encore_StartSaveState(_context);
			writer.Write(stateLen);

			if (stateLen > _stateBuf.Length)
			{
				_stateBuf = new byte[stateLen];
			}

			_core.Encore_FinishSaveState(_context, _stateBuf);
			writer.Write(_stateBuf, 0, stateLen);

			// other variables
			writer.Write(IsLagFrame);
			writer.Write(LagCount);
			writer.Write(Frame);
			// motion emu state
			writer.Write(_motionEmu.IsTilting);
			writer.Write(_motionEmu.TiltOrigin.X);
			writer.Write(_motionEmu.TiltOrigin.Y);
			writer.Write(_motionEmu.TiltDirection.X);
			writer.Write(_motionEmu.TiltDirection.Y);
			writer.Write(_motionEmu.TiltAngle);
			writer.Write(_motionEmu.PrevTiltQuaternion.X);
			writer.Write(_motionEmu.PrevTiltQuaternion.Y);
			writer.Write(_motionEmu.PrevTiltQuaternion.Z);
			writer.Write(_motionEmu.PrevTiltQuaternion.W);
			writer.Write(_motionEmu.Gravity.X);
			writer.Write(_motionEmu.Gravity.Y);
			writer.Write(_motionEmu.Gravity.Z);
			writer.Write(_motionEmu.AngularRate.X);
			writer.Write(_motionEmu.AngularRate.Y);
			writer.Write(_motionEmu.AngularRate.Z);
		}

		public void LoadStateBinary(BinaryReader reader)
		{
			var stateLen = reader.ReadInt32();

			if (stateLen > _stateBuf.Length)
			{
				_stateBuf = new byte[stateLen];
			}

			reader.Read(_stateBuf, 0, stateLen);
			_core.Encore_LoadState(_context, _stateBuf, stateLen);

			// other variables
			IsLagFrame = reader.ReadBoolean();
			LagCount = reader.ReadInt32();
			Frame = reader.ReadInt32();
			// motion emu state
			_motionEmu.IsTilting = reader.ReadBoolean();
			_motionEmu.TiltOrigin.X = reader.ReadSingle();
			_motionEmu.TiltOrigin.Y = reader.ReadSingle();
			_motionEmu.TiltDirection.X = reader.ReadSingle();
			_motionEmu.TiltDirection.Y = reader.ReadSingle();
			_motionEmu.TiltAngle = reader.ReadSingle();
			_motionEmu.PrevTiltQuaternion.X = reader.ReadSingle();
			_motionEmu.PrevTiltQuaternion.Y = reader.ReadSingle();
			_motionEmu.PrevTiltQuaternion.Z = reader.ReadSingle();
			_motionEmu.PrevTiltQuaternion.W = reader.ReadSingle();
			_motionEmu.Gravity.X = reader.ReadSingle();
			_motionEmu.Gravity.Y = reader.ReadSingle();
			_motionEmu.Gravity.Z = reader.ReadSingle();
			_motionEmu.AngularRate.X = reader.ReadSingle();
			_motionEmu.AngularRate.Y = reader.ReadSingle();
			_motionEmu.AngularRate.Z = reader.ReadSingle();

			// memory domain pointers are no longer valid, reset them
			WireMemoryDomains();
		}
	}
}
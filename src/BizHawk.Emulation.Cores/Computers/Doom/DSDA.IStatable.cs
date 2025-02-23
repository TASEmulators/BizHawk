using System.IO;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.Doom
{
	public partial class DSDA : IStatable
	{
		public bool AvoidRewind => false;

		public void LoadStateBinary(BinaryReader reader)
		{
			_elf.LoadStateBinary(reader);
			
			// Getting last mouse positions
			_lastMouseRunningValues[0] = reader.ReadInt32();
			_lastMouseTurningValues[0] = reader.ReadInt32();
			_lastMouseRunningValues[1] = reader.ReadInt32();
			_lastMouseTurningValues[1] = reader.ReadInt32();
			_lastMouseRunningValues[2] = reader.ReadInt32();
			_lastMouseTurningValues[2] = reader.ReadInt32();
			_lastMouseRunningValues[3] = reader.ReadInt32();
			_lastMouseTurningValues[3] = reader.ReadInt32();

			Frame = reader.ReadInt32();
			// any managed pointers that we sent to the core need to be resent now!
			//Core.stella_set_input_callback(_inputCallback);
			UpdateVideo();
		}

		public void SaveStateBinary(BinaryWriter writer)
		{
			_elf.SaveStateBinary(writer);

			// Writing last mouse positions
			writer.Write(_lastMouseRunningValues[0]);
			writer.Write(_lastMouseTurningValues[0]);
			writer.Write(_lastMouseRunningValues[1]);
			writer.Write(_lastMouseTurningValues[1]);
			writer.Write(_lastMouseRunningValues[2]);
			writer.Write(_lastMouseTurningValues[2]);
			writer.Write(_lastMouseRunningValues[3]);
			writer.Write(_lastMouseTurningValues[3]);

			writer.Write(Frame);
		}
	}
}

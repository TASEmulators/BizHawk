using System.IO;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.SNES
{
	public unsafe partial class LibsnesCore : IStatable
	{
		public void SaveStateBinary(BinaryWriter writer)
		{
			Api.SaveStateBinary(writer);
			writer.Write(IsLagFrame);
			writer.Write(LagCount);
			writer.Write(Frame);
		}

		public void LoadStateBinary(BinaryReader reader)
		{
			Api.LoadStateBinary(reader);
			IsLagFrame = reader.ReadBoolean();
			LagCount = reader.ReadInt32();
			Frame = reader.ReadInt32();
			// refresh all callbacks now
			Api.QUERY_set_video_refresh(snes_video_refresh);
			Api.QUERY_set_input_poll(snes_input_poll);
			Api.QUERY_set_input_state(snes_input_state);
			Api.QUERY_set_input_notify(snes_input_notify);
			Api.QUERY_set_audio_sample(_soundcb);
		}

		public byte[] SaveStateBinary()
		{
			using var ms = new MemoryStream();
			using var bw = new BinaryWriter(ms);
			SaveStateBinary(bw);
			bw.Flush();
			return ms.ToArray();
		}
	}
}

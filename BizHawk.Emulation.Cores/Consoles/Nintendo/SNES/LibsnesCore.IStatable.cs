using System;
using System.IO;

using BizHawk.Common.BufferExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.SNES
{
	public unsafe partial class LibsnesCore : IStatable
	{
		public bool BinarySaveStatesPreferred => true;

		public void SaveStateText(TextWriter writer)
		{
			var temp = SaveStateBinary();
			temp.SaveAsHexFast(writer);
			writer.WriteLine("Frame {0}", Frame); // we don't parse this, it's only for the client to use
			writer.WriteLine("Profile {0}", CurrentProfile);
		}

		public void LoadStateText(TextReader reader)
		{
			string hex = reader.ReadLine();
			byte[] state = new byte[hex.Length / 2];
			state.ReadFromHexFast(hex);
			LoadStateBinary(new BinaryReader(new MemoryStream(state)));
			reader.ReadLine(); // Frame #
			var profile = reader.ReadLine().Split(' ')[1];
			ValidateLoadstateProfile(profile);
		}

		public void SaveStateBinary(BinaryWriter writer)
		{
			writer.Write(DeterministicEmulation ? _savestatebuff : CoreSaveState());

			// other variables
			writer.Write(IsLagFrame);
			writer.Write(LagCount);
			writer.Write(Frame);
			writer.Write(CurrentProfile);

			writer.Flush();
		}

		public void LoadStateBinary(BinaryReader reader)
		{
			int size = Api.QUERY_serialize_size();
			byte[] buf = reader.ReadBytes(size);
			CoreLoadState(buf);

			if (DeterministicEmulation) // deserialize controller and fast-foward now
			{
				// reconstruct savestatebuff at the same time to avoid a costly core serialize
				var ms = new MemoryStream();
				var bw = new BinaryWriter(ms);
				bw.Write(buf);
				bool framezero = reader.ReadBoolean();
				bw.Write(framezero);
				if (!framezero)
				{
					var ssc = new SaveController(ControllerDefinition);
					ssc.DeSerialize(reader);
					IController tmp = Controller;
					Controller = ssc;
					_nocallbacks = true;
					FrameAdvance(false, false);
					_nocallbacks = false;
					Controller = tmp;
					ssc.Serialize(bw);
				}
				else // hack: dummy controller info
				{
					bw.Write(reader.ReadBytes(536));
				}

				bw.Close();
				_savestatebuff = ms.ToArray();
			}

			// other variables
			IsLagFrame = reader.ReadBoolean();
			LagCount = reader.ReadInt32();
			Frame = reader.ReadInt32();
			var profile = reader.ReadString();
			ValidateLoadstateProfile(profile);
		}

		public byte[] SaveStateBinary()
		{
			var ms = new MemoryStream();
			var bw = new BinaryWriter(ms);
			SaveStateBinary(bw);
			bw.Flush();
			return ms.ToArray();
		}

		// handle the unmanaged part of loadstating
		private void CoreLoadState(byte[] data)
		{
			int size = Api.QUERY_serialize_size();
			if (data.Length != size)
			{
				throw new Exception("Libsnes internal savestate size mismatch!");
			}

			Api.CMD_init();

			// zero 01-sep-2014 - this approach isn't being used anymore, it's too slow!
			// LoadCurrent(); //need to make sure chip roms are reloaded
			fixed (byte* pbuf = &data[0])
				Api.CMD_unserialize(new IntPtr(pbuf), size);
		}


		// handle the unmanaged part of savestating
		private byte[] CoreSaveState()
		{
			int size = Api.QUERY_serialize_size();
			byte[] buf = new byte[size];
			fixed (byte* pbuf = &buf[0])
				Api.CMD_serialize(new IntPtr(pbuf), size);
			return buf;
		}

		private void ValidateLoadstateProfile(string profile)
		{
			if (profile != CurrentProfile)
			{
				throw new InvalidOperationException($"You've attempted to load a savestate made using a different SNES profile ({profile}) than your current configuration ({CurrentProfile}). We COULD automatically switch for you, but we havent done that yet. This error is to make sure you know that this isnt going to work right now.");
			}
		}

		// most recent internal savestate, for deterministic mode ONLY
		private byte[] _savestatebuff;
	}
}

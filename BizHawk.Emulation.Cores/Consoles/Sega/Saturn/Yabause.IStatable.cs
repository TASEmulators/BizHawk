using System;
using System.IO;

using BizHawk.Common.BufferExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Sega.Saturn
{
	public partial class Yabause : IStatable
	{
		public bool BinarySaveStatesPreferred { get { return true; } }

		// these next 5 functions are all exact copy paste from gambatte.
		// if something's wrong here, it's probably wrong there too

		public void SaveStateText(TextWriter writer)
		{
			var temp = SaveStateBinary();
			temp.SaveAsHexFast(writer);
			// write extra copy of stuff we don't use
			writer.WriteLine("Frame {0}", Frame);
		}

		public void LoadStateText(TextReader reader)
		{
			string hex = reader.ReadLine();
			byte[] state = new byte[hex.Length / 2];
			state.ReadFromHexFast(hex);
			LoadStateBinary(new BinaryReader(new MemoryStream(state)));
		}

		public void SaveStateBinary(BinaryWriter writer)
		{
			byte[] data = SaveCoreBinary();

			writer.Write(data.Length);
			writer.Write(data);

			// other variables
			writer.Write(IsLagFrame);
			writer.Write(LagCount);
			writer.Write(Frame);
		}

		public void LoadStateBinary(BinaryReader reader)
		{
			int length = reader.ReadInt32();
			byte[] data = reader.ReadBytes(length);

			LoadCoreBinary(data);

			// other variables
			IsLagFrame = reader.ReadBoolean();
			LagCount = reader.ReadInt32();
			Frame = reader.ReadInt32();
		}

		public byte[] SaveStateBinary()
		{
			MemoryStream ms = new MemoryStream();
			BinaryWriter bw = new BinaryWriter(ms);
			SaveStateBinary(bw);
			bw.Flush();
			return ms.ToArray();
		}

		/// <summary>
		/// does a save, load, save combo, and checks the two saves for identicalness.
		/// </summary>
		private void CheckStates()
		{
			byte[] s1 = SaveStateBinary();
			LoadStateBinary(new BinaryReader(new MemoryStream(s1, false)));
			byte[] s2 = SaveStateBinary();
			if (s1.Length != s2.Length)
				throw new Exception(string.Format("CheckStates: Length {0} != {1}", s1.Length, s2.Length));
			unsafe
			{
				fixed (byte* b1 = &s1[0], b2 = &s2[0])
				{
					for (int i = 0; i < s1.Length; i++)
					{
						if (b1[i] != b2[i])
						{
							File.WriteAllBytes("save1.raw", s1);
							File.WriteAllBytes("save2.raw", s2);
							throw new Exception(string.Format("CheckStates s1[{0}] = {1}, s2[{0}] = {2}", i, b1[i], b2[i]));
						}
					}
				}
			}
		}

		private void LoadCoreBinary(byte[] data)
		{
			var fp = new FilePiping();
			fp.Offer(data);

			//loadstate can trigger GL work
			ActivateGL();

			bool succeed = LibYabause.libyabause_loadstate(fp.GetPipeNameNative());

			DeactivateGL();

			fp.Finish();
			if (!succeed)
				throw new Exception("libyabause_loadstate() failed");
		}

		private byte[] SaveCoreBinary()
		{
			var ms = new MemoryStream();
			var fp = new FilePiping();
			fp.Get(ms);
			bool succeed = LibYabause.libyabause_savestate(fp.GetPipeNameNative());
			fp.Finish();
			var ret = ms.ToArray();
			ms.Close();
			if (!succeed)
				throw new Exception("libyabause_savestate() failed");
			return ret;
		}
	}
}

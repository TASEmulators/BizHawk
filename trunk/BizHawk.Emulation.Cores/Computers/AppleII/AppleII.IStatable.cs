using BizHawk.Emulation.Common;
using System.IO;
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BizHawk.Emulation.Cores.Computers.AppleII
{
	public partial class AppleII : IStatable
	{
		public bool BinarySaveStatesPreferred { get { return false; } }

		private class OtherData
		{
			public int Frame;
			public int LagCount;
			public bool IsLagFrame;
			public int CurrentDisk;
			public JObject Core;
		}
		private JsonSerializer ser = new JsonSerializer();

		[FeatureNotImplemented]
		public void SaveStateText(TextWriter writer)
		{
			var w = new JTokenWriter();
			_machine.Serialize(w);

			var o = new OtherData
			{
				Frame = Frame,
				LagCount = LagCount,
				IsLagFrame = IsLagFrame,
				CurrentDisk = CurrentDisk,
				Core = (JObject)w.Token,
			};

			var jw = new JsonTextWriter(writer) { Formatting = Newtonsoft.Json.Formatting.Indented };
			ser.Serialize(jw, o);
		}

		public void LoadStateText(TextReader reader)
		{
			var o = (OtherData)ser.Deserialize(reader, typeof(OtherData));
			Frame = o.Frame;
			LagCount = o.LagCount;
			IsLagFrame = o.IsLagFrame;
			CurrentDisk = o.CurrentDisk;

			var r = new JTokenReader(o.Core);
			try
			{
				_machine = Jellyfish.Virtu.Machine.Deserialize(r);
			}
			catch (Exception e)
			{
				Console.WriteLine(e.ToString());
				throw;
			}
			// should not be needed.
			// InitDisk();
		}

		public void SaveStateBinary(BinaryWriter writer)
		{
			/*
			writer.Write(Frame);
			writer.Write(LagCount);
			writer.Write(IsLagFrame);
			writer.Write(CurrentDisk);
			_machine.SaveState(writer);
			*/
		}

		public void LoadStateBinary(BinaryReader reader)
		{
			/*
			Frame = reader.ReadInt32();
			LagCount = reader.ReadInt32();
			IsLagFrame = reader.ReadBoolean();
			CurrentDisk = reader.ReadInt32();
			InitDisk();
			_machine.LoadState(reader);
			*/
		}

		public byte[] SaveStateBinary()
		{
			return new byte[16];

			if (_stateBuffer == null)
			{
				var stream = new MemoryStream();
				var writer = new BinaryWriter(stream);
				SaveStateBinary(writer);
				_stateBuffer = stream.ToArray();
				writer.Close();
				return _stateBuffer;
			}
			else
			{
				var stream = new MemoryStream(_stateBuffer);
				var writer = new BinaryWriter(stream);
				SaveStateBinary(writer);
				writer.Close();
				return _stateBuffer;
			}
		}

		private byte[] _stateBuffer;
	}
}

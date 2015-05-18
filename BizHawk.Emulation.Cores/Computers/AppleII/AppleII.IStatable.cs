using BizHawk.Emulation.Common;
using System.IO;
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Jellyfish.Virtu;

namespace BizHawk.Emulation.Cores.Computers.AppleII
{
	public partial class AppleII : IStatable
	{
		private class CoreConverter : JsonConverter
		{
			public override bool CanConvert(Type objectType)
			{
				return objectType == typeof(Machine);
			}

			public override bool CanRead { get { return true; } }
			public override bool CanWrite { get { return false; } }

			public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
			{
				// uses its own serialization context: intentional
				return Machine.Deserialize(reader);
			}

			public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
			{
				throw new NotImplementedException();
			}
		}

		public bool BinarySaveStatesPreferred { get { return false; } }

		private void SerializeEverything(JsonWriter w)
		{
			// this is much faster than other possibilities for serialization
			w.WriteStartObject();
			w.WritePropertyName("Frame");
			w.WriteValue(Frame);
			w.WritePropertyName("LagCount");
			w.WriteValue(LagCount);
			w.WritePropertyName("IsLagFrame");
			w.WriteValue(IsLagFrame);
			w.WritePropertyName("CurrentDisk");
			w.WriteValue(CurrentDisk);
			w.WritePropertyName("Core");
			_machine.Serialize(w);
			w.WriteEndObject();
		}

		private void DeserializeEverything(JsonReader r)
		{
			var o = (OtherData)ser.Deserialize(r, typeof(OtherData));
			Frame = o.Frame;
			LagCount = o.LagCount;
			IsLagFrame = o.IsLagFrame;
			CurrentDisk = o.CurrentDisk;
			_machine = o.Core;

			// should not be needed.
			// InitDisk();
		}

		private class OtherData
		{
			public int Frame;
			public int LagCount;
			public bool IsLagFrame;
			public int CurrentDisk;
			public Machine Core;
		}

		private void InitSaveStates()
		{
			ser.Converters.Add(new CoreConverter());
		}

		private JsonSerializer ser = new JsonSerializer();

		public void SaveStateText(TextWriter writer)
		{
			SerializeEverything(new JsonTextWriter(writer) { Formatting = Formatting.None });
		}

		public void LoadStateText(TextReader reader)
		{
			DeserializeEverything(new JsonTextReader(reader));
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

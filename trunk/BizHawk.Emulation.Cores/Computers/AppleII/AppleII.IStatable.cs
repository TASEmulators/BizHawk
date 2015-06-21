using BizHawk.Emulation.Common;
using System.IO;
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Jellyfish.Virtu;
using Newtonsoft.Json.Bson;

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

		public bool BinarySaveStatesPreferred { get { return true; } }

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
			w.WritePropertyName("PreviousDiskPressed");
			w.WriteValue(_prevPressed);
			w.WritePropertyName("NextDiskPressed");
			w.WriteValue(_nextPressed);
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
			_prevPressed = o.PreviousDiskPressed;
			_nextPressed = o.NextDiskPressed;
			// since _machine was replaced, we need to reload settings from frontend
			PutSettings(_settings);
		}

		public class OtherData
		{
			public int Frame;
			public int LagCount;
			public bool IsLagFrame;
			public int CurrentDisk;
			public bool PreviousDiskPressed;
			public bool NextDiskPressed;
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

		/*
		 * These are horrible; the LoadStateBinary() takes over 10x as long as LoadStateText()
		 * Until we figure out why JSON.NET's BSONwriter sucks and how to fix it, stick with text-as-binary
		public void SaveStateBinary(BinaryWriter writer)
		{
			SerializeEverything(new BsonWriter(writer));
		}

		public void LoadStateBinary(BinaryReader reader)
		{
			DeserializeEverything(new BsonReader(reader));
		}
		*/
		/*
		public void SaveStateBinary(BinaryWriter writer)
		{
			var tw = new StreamWriter(writer.BaseStream, new System.Text.UTF8Encoding(false));
			SaveStateText(tw);
			tw.Flush();
		}

		public void LoadStateBinary(BinaryReader reader)
		{
			var tr = new StreamReader(reader.BaseStream, System.Text.Encoding.UTF8);
			LoadStateText(tr);
		}*/

		// these homemade classes edge out the stock ones slightly, but need BufferedStream to not be bad
		public void SaveStateBinary(BinaryWriter writer)
		{
			var buffer = new BufferedStream(writer.BaseStream, 16384);
			var bw2 = new BinaryWriter(buffer);
			SerializeEverything(new LBW(bw2));
			bw2.Flush();
			buffer.Flush();
		}

		public void LoadStateBinary(BinaryReader reader)
		{
			var buffer = new BufferedStream(reader.BaseStream, 16384);
			var br2 = new BinaryReader(buffer);
			DeserializeEverything(new LBR(br2));
		}

		public byte[] SaveStateBinary()
		{
			// our savestate array can be of varying sizes, so this can't be too clever
			var stream = new MemoryStream();
			var writer = new BinaryWriter(stream);
			SaveStateBinary(writer);
			writer.Flush();
			return stream.ToArray();
		}
	}
}

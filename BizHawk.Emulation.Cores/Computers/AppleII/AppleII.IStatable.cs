using System;
using System.IO;

using BizHawk.Emulation.Common;
using Jellyfish.Virtu;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace BizHawk.Emulation.Cores.Computers.AppleII
{
	public partial class AppleII : IStatable
	{
		private class CoreConverter : JsonConverter
		{
			public override bool CanConvert(Type objectType)
			{
				return objectType == typeof(Components);
			}

			public override bool CanRead => true;

			public override bool CanWrite => false;

			public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
			{
				return CreateSerializer().Deserialize<Components>(reader);
			}

			public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
			{
				throw new NotImplementedException();
			}
		}

		private void SerializeEverything(JsonWriter w)
		{
			// this is much faster than other possibilities for serialization
			w.WriteStartObject();
			w.WritePropertyName(nameof(Frame));
			w.WriteValue(Frame);
			w.WritePropertyName(nameof(LagCount));
			w.WriteValue(LagCount);
			w.WritePropertyName(nameof(IsLagFrame));
			w.WriteValue(IsLagFrame);
			w.WritePropertyName(nameof(CurrentDisk));
			w.WriteValue(CurrentDisk);
			w.WritePropertyName("PreviousDiskPressed");
			w.WriteValue(_prevPressed);
			w.WritePropertyName("NextDiskPressed");
			w.WriteValue(_nextPressed);
			w.WritePropertyName("Core");
			CreateSerializer().Serialize(w, _machine);
			w.WriteEndObject();
		}

		private void DeserializeEverything(JsonReader r)
		{
			var o = (OtherData)_ser.Deserialize(r, typeof(OtherData));
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
			public Components Core;
		}

		private void InitSaveStates()
		{
			_ser.Converters.Add(new CoreConverter());
		}

		private readonly JsonSerializer _ser = new JsonSerializer();

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

		private static JsonSerializer CreateSerializer()
		{
			// TODO: converters could be cached for speedup

			var ser = new JsonSerializer
			{
				TypeNameHandling = TypeNameHandling.Auto,
				PreserveReferencesHandling = PreserveReferencesHandling.All, // leaving out Array is a very important problem, and means that we can't rely on a directly shared array to work.
				ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
			};

			ser.Converters.Add(new TypeTypeConverter(new[]
			{
				// all expected Types to convert are either in this assembly or mscorlib
				typeof(Memory).Assembly,
				typeof(object).Assembly
			}));

			ser.Converters.Add(new DelegateConverter());
			ser.Converters.Add(new ArrayConverter());

			var cr = new DefaultContractResolver();
			cr.DefaultMembersSearchFlags |= System.Reflection.BindingFlags.NonPublic;
			ser.ContractResolver = cr;

			return ser;
		}
	}
}

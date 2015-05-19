using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.IO;
using Newtonsoft.Json.Linq;

namespace BizHawk.Emulation.Cores.Computers.AppleII
{
	// barebones classes for writing and reading a simple bson-like format, used to gain a bit of speed in Apple II savestates

	internal enum LBTOK : byte
	{
		Null,
		Undefined,
		StartArray,
		EndArray,
		StartObject,
		EndObject,
		Property,
		S8,
		U8,
		S16,
		U16,
		S32,
		U32,
		S64,
		U64,
		False,
		True,
		String,
		F32,
		F64,
		ByteArray,
	}

	public class LBR : JsonReader
	{
		public LBR(BinaryReader r)
		{
			this.r = r;
		}
		private BinaryReader r;
		public override void Close()
		{
		}
		// as best as I can tell, the serializers refer to depth, but don't actually need to work except when doing certain error recovery
		public override int Depth { get { return 0; } }
		public override string Path { get { throw new NotImplementedException(); } }
		public override Type ValueType { get { return v != null ? v.GetType() : null; } }
		public override JsonToken TokenType { get { return t; } }
		public override object Value { get { return v; } }
		private object v;
		private JsonToken t;

		public override bool Read()
		{
			LBTOK l = (LBTOK)r.ReadByte();
			switch (l)
			{
				case LBTOK.StartArray: t = JsonToken.StartArray; v = null; break;
				case LBTOK.EndArray: t = JsonToken.EndArray; v = null; break;
				case LBTOK.StartObject: t = JsonToken.StartObject; v = null; break;
				case LBTOK.EndObject: t = JsonToken.EndObject; v = null; break;
				case LBTOK.Null: t = JsonToken.Null; v = null; break;
				case LBTOK.False: t = JsonToken.Boolean; v = false; break;
				case LBTOK.True: t = JsonToken.Boolean; v = true; break;
				case LBTOK.Property: t = JsonToken.PropertyName; v = r.ReadString(); break;
				case LBTOK.Undefined: t = JsonToken.Undefined; v = null; break;
				case LBTOK.S8: t = JsonToken.Integer; v = r.ReadSByte(); break;
				case LBTOK.U8: t = JsonToken.Integer; v = r.ReadByte(); break;
				case LBTOK.S16: t = JsonToken.Integer; v = r.ReadInt16(); break;
				case LBTOK.U16: t = JsonToken.Integer; v = r.ReadUInt16(); break;
				case LBTOK.S32: t = JsonToken.Integer; v = r.ReadInt32(); break;
				case LBTOK.U32: t = JsonToken.Integer; v = r.ReadUInt32(); break;
				case LBTOK.S64: t = JsonToken.Integer; v = r.ReadInt64(); break;
				case LBTOK.U64: t = JsonToken.Integer; v = r.ReadUInt64(); break;
				case LBTOK.String: t = JsonToken.String; v = r.ReadString(); break;
				case LBTOK.F32: t = JsonToken.Float; v = r.ReadSingle(); break;
				case LBTOK.F64: t = JsonToken.Float; v = r.ReadDouble(); break;
				case LBTOK.ByteArray: t = JsonToken.Bytes; v = r.ReadBytes(r.ReadInt32()); break;

				default:
					throw new InvalidOperationException();
			}
			return true;
		}

		public override byte[] ReadAsBytes()
		{
			if (!Read() || t != JsonToken.Bytes)
				return null;
			return (byte[])v;
		}

		public override DateTime? ReadAsDateTime()
		{
			throw new NotImplementedException();
		}

		public override DateTimeOffset? ReadAsDateTimeOffset()
		{
			throw new NotImplementedException();
		}

		public override decimal? ReadAsDecimal()
		{
			throw new NotImplementedException();
		}

		public override int? ReadAsInt32()
		{
			// TODO: speed this up if needed
			if (!Read())
				return null;

			switch (t)
			{
				case JsonToken.Null:
					return null;
				case JsonToken.Integer:
				case JsonToken.Float:
					return Convert.ToInt32(v);
				case JsonToken.String:
					int i;
					if (int.TryParse(v.ToString(), out i))
						return i;
					else
						return null;
				default:
					return null;
			}
		}

		public override string ReadAsString()
		{
			if (!Read())
				return null;

			switch (t)
			{
				case JsonToken.Null:
					return null;
				case JsonToken.Float:
				case JsonToken.Integer:
				case JsonToken.Boolean:
				case JsonToken.String:
					return v.ToString();
				default:
					return null;
			}
		}
	}

	public class LBW : JsonWriter
	{
		private void WT(LBTOK t)
		{
			w.Write((byte)t);
		}

		public LBW(BinaryWriter w)
		{
			this.w = w;
		}
		private BinaryWriter w;

		public override void Flush()
		{
			w.Flush();
		}

		public override void Close()
		{
		}

		public override void WriteValue(bool value) { WT(value ? LBTOK.True : LBTOK.False); }

		public override void WriteValue(sbyte value) { WT(LBTOK.S8); w.Write(value); }
		public override void WriteValue(byte value) { WT(LBTOK.U8); w.Write(value); }
		public override void WriteValue(short value) { WT(LBTOK.S16); w.Write(value); }
		public override void WriteValue(ushort value) { WT(LBTOK.U16); w.Write(value); }
		public override void WriteValue(int value) { WT(LBTOK.S32); w.Write(value); }
		public override void WriteValue(uint value) { WT(LBTOK.U32); w.Write(value); }
		public override void WriteValue(long value) { WT(LBTOK.S64); w.Write(value); }
		public override void WriteValue(ulong value) { WT(LBTOK.U64); w.Write(value); }

		public override void WriteStartArray() { WT(LBTOK.StartArray); }
		public override void WriteEndArray() { WT(LBTOK.EndArray); }
		public override void WriteStartObject() { WT(LBTOK.StartObject); }
		public override void WriteEndObject() { WT(LBTOK.EndObject); }
		public override void WriteNull() { WT(LBTOK.Null); }
		public override void WriteUndefined() { WT(LBTOK.Undefined); }

		public override void WriteValue(float value) { WT(LBTOK.F32); w.Write(value); }
		public override void WriteValue(double value) { WT(LBTOK.F64); w.Write(value); }

		public override void WriteValue(byte[] value) { WT(LBTOK.ByteArray); w.Write(value.Length); w.Write(value); }

		public override void WriteComment(string text) { throw new NotImplementedException(); }
		public override void WriteWhitespace(string ws) { throw new NotImplementedException(); }
		protected override void WriteIndent() { throw new NotImplementedException(); }
		protected override void WriteIndentSpace() { throw new NotImplementedException(); }
		public override void WriteEnd() { throw new NotImplementedException(); }
		protected override void WriteEnd(JsonToken token) { throw new NotImplementedException(); }
		public override void WriteRaw(string json) { throw new NotImplementedException(); }
		public override void WriteRawValue(string json) { throw new NotImplementedException(); }
		public override void WriteStartConstructor(string name) { throw new NotImplementedException(); }
		public override void WriteEndConstructor() { throw new NotImplementedException(); }
		protected override void WriteValueDelimiter() { throw new NotImplementedException(); }

		public override void WritePropertyName(string name) { WT(LBTOK.Property); w.Write(name); }
		public override void WriteValue(string value) { WT(LBTOK.String); w.Write(value); }
		public override void WritePropertyName(string name, bool escape) { WT(LBTOK.Property); w.Write(name); } // no escaping required

		public override void WriteValue(char value) { throw new NotImplementedException(); }
		public override void WriteValue(DateTime value) { throw new NotImplementedException(); }
		public override void WriteValue(DateTimeOffset value) { throw new NotImplementedException(); }
		public override void WriteValue(decimal value) { throw new NotImplementedException(); }
		public override void WriteValue(Guid value) { throw new NotImplementedException(); }
		public override void WriteValue(TimeSpan value) { throw new NotImplementedException(); }
		public override void WriteValue(Uri value) { throw new NotImplementedException(); }
	}

}

using System;
using System.IO;

using Newtonsoft.Json;

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
		private readonly BinaryReader _r;
		private object _v;
		private JsonToken _t;

		public LBR(BinaryReader reader)
		{
			_r = reader;
		}

		public override void Close()
		{
		}

		// as best as I can tell, the serializers refer to depth, but don't actually need to work except when doing certain error recovery
		public override int Depth => 0;

		public override string Path => throw new NotImplementedException();

		public override Type ValueType => _v?.GetType();

		public override JsonToken TokenType => _t;

		public override object Value => _v;

		public override bool Read()
		{
			LBTOK l = (LBTOK)_r.ReadByte();
			switch (l)
			{
				case LBTOK.StartArray: _t = JsonToken.StartArray; _v = null; break;
				case LBTOK.EndArray: _t = JsonToken.EndArray; _v = null; break;
				case LBTOK.StartObject: _t = JsonToken.StartObject; _v = null; break;
				case LBTOK.EndObject: _t = JsonToken.EndObject; _v = null; break;
				case LBTOK.Null: _t = JsonToken.Null; _v = null; break;
				case LBTOK.False: _t = JsonToken.Boolean; _v = false; break;
				case LBTOK.True: _t = JsonToken.Boolean; _v = true; break;
				case LBTOK.Property: _t = JsonToken.PropertyName; _v = _r.ReadString(); break;
				case LBTOK.Undefined: _t = JsonToken.Undefined; _v = null; break;
				case LBTOK.S8: _t = JsonToken.Integer; _v = _r.ReadSByte(); break;
				case LBTOK.U8: _t = JsonToken.Integer; _v = _r.ReadByte(); break;
				case LBTOK.S16: _t = JsonToken.Integer; _v = _r.ReadInt16(); break;
				case LBTOK.U16: _t = JsonToken.Integer; _v = _r.ReadUInt16(); break;
				case LBTOK.S32: _t = JsonToken.Integer; _v = _r.ReadInt32(); break;
				case LBTOK.U32: _t = JsonToken.Integer; _v = _r.ReadUInt32(); break;
				case LBTOK.S64: _t = JsonToken.Integer; _v = _r.ReadInt64(); break;
				case LBTOK.U64: _t = JsonToken.Integer; _v = _r.ReadUInt64(); break;
				case LBTOK.String: _t = JsonToken.String; _v = _r.ReadString(); break;
				case LBTOK.F32: _t = JsonToken.Float; _v = _r.ReadSingle(); break;
				case LBTOK.F64: _t = JsonToken.Float; _v = _r.ReadDouble(); break;
				case LBTOK.ByteArray: _t = JsonToken.Bytes; _v = _r.ReadBytes(_r.ReadInt32()); break;

				default:
					throw new InvalidOperationException();
			}

			return true;
		}

		public override byte[] ReadAsBytes()
		{
			if (!Read() || _t != JsonToken.Bytes)
			{
				return null;
			}

			return (byte[])_v;
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
			{
				return null;
			}

			switch (_t)
			{
				case JsonToken.Null:
					return null;
				case JsonToken.Integer:
				case JsonToken.Float:
					return Convert.ToInt32(_v);
				case JsonToken.String:
					int i;
					if (int.TryParse(_v.ToString(), out i))
					{
						return i;
					}

					return null;
				default:
					return null;
			}
		}

		public override string ReadAsString()
		{
			if (!Read())
			{
				return null;
			}

			switch (_t)
			{
				case JsonToken.Null:
					return null;
				case JsonToken.Float:
				case JsonToken.Integer:
				case JsonToken.Boolean:
				case JsonToken.String:
					return _v.ToString();
				default:
					return null;
			}
		}
	}

	public class LBW : JsonWriter
	{
		private readonly BinaryWriter w;

		private void WT(LBTOK t)
		{
			w.Write((byte)t);
		}

		public LBW(BinaryWriter w)
		{
			this.w = w;
		}

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

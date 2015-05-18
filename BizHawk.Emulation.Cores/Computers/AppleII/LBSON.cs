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
	// test classes to see if our own lightweight BSON can be fast
	// these don't derive JsonReader and JsonWriter because those classes are difficult to derive from,
	// so unfortunately everything has to be loaded into memory.

	public class LBSONReader
	{
		private enum LToken : byte
		{
			Null,
			Array,
			Object,
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
			F64
		}

		private LToken ReadToken()
		{
			return (LToken)r.ReadByte();
		}

		public LBSONReader(BinaryReader r)
		{
			this.r = r;
		}

		private BinaryReader r;

		public JToken Read()
		{
			var t = ReadToken();
			switch (t)
			{
				case LToken.Null: return JValue.CreateNull();
				case LToken.Array: return ReadArray();
				case LToken.Object: return ReadObject();
				case LToken.S8: return new JValue(r.ReadSByte());
				case LToken.U8: return new JValue(r.ReadByte());
				case LToken.S16: return new JValue(r.ReadInt16());
				case LToken.U16: return new JValue(r.ReadUInt16());
				case LToken.S32: return new JValue(r.ReadInt32());
				case LToken.U32: return new JValue(r.ReadUInt32());
				case LToken.S64: return new JValue(r.ReadInt64());
				case LToken.U64: return new JValue(r.ReadUInt64());
				case LToken.False: return new JValue(false);
				case LToken.True: return new JValue(true);
				case LToken.String: return new JValue(r.ReadString());
				case LToken.F32: return new JValue(r.ReadSingle());
				case LToken.F64: return new JValue(r.ReadDouble());
				default: throw new InvalidOperationException();
			}
		}

		private JArray ReadArray()
		{
			int l = r.ReadInt32();
			var ret = new JArray();
			for (int i = 0; i < l; i++)
			{
				ret.Add(Read());
			}
			return ret;
		}

		private JObject ReadObject()
		{
			int l = r.ReadInt32();
			var ret = new JObject();
			for (int i = 0; i < l; i++)
			{
				ret.Add(r.ReadString(), Read());
			}
			return ret;
		}
	}

	public class LBSONWriter
	{
		private enum LToken : byte
		{
			Null,
			Array,
			Object,
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
			F64
		}
		private void WriteToken(LToken t)
		{
			w.Write((byte)t);
		}

		public LBSONWriter(BinaryWriter w)
		{
			this.w = w;
		}

		private BinaryWriter w;

		private void WriteArray(JArray j)
		{
			WriteToken(LToken.Array);
			w.Write(j.Count);
			foreach (var jj in j)
			{
				Write(jj);
			}
		}
		private void WriteObject(JObject j)
		{
			WriteToken(LToken.Object);
			w.Write(j.Count);
			foreach (var jj in j)
			{
				w.Write(jj.Key);
				Write(jj.Value);
			}
		}

		private void WriteValue(object o)
		{
			switch (o.GetType().ToString())
			{
				case "System.SByte": WriteToken(LToken.S8); w.Write((sbyte)o); return;
				case "System.Byte": WriteToken(LToken.U8); w.Write((byte)o); return;
				case "System.Int16": WriteToken(LToken.S16); w.Write((short)o); return;
				case "System.UInt16": WriteToken(LToken.U16); w.Write((ushort)o); return;
				case "System.Int32": WriteToken(LToken.S32); w.Write((int)o); return;
				case "System.UInt32": WriteToken(LToken.U32); w.Write((uint)o); return;
				case "System.Int64": WriteToken(LToken.S64); w.Write((long)o); return;
				case "System.UInt64": WriteToken(LToken.U64); w.Write((ulong)o); return;

				case "System.Boolean": WriteToken((bool)o ? LToken.True : LToken.False); return;

				case "System.Single": WriteToken(LToken.F32); w.Write((float)o); return;
				case "System.Double": WriteToken(LToken.F64); w.Write((double)o); return;

				case "System.String": WriteToken(LToken.String); w.Write((string)o); return;

				default:
					throw new NotImplementedException();
			}
		}

		public void Write(JToken j)
		{
			switch (j.Type)
			{
				case JTokenType.Array:
					WriteArray((JArray)j);
					return;
				case JTokenType.Object:
					WriteObject((JObject)j);
					return;
				case JTokenType.Boolean:
				case JTokenType.Bytes:
				case JTokenType.Date:
				case JTokenType.Float:
				case JTokenType.Guid:
				case JTokenType.Integer:
				case JTokenType.String:
					WriteValue(((JValue)j).Value);
					return;
					
				case JTokenType.Null:
					WriteToken(LToken.Null);
					return;

				default:
					throw new NotImplementedException();
			}
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Common
{
	public class CodeDataLog : Dictionary<string, byte[]>
	{
		public CodeDataLog()
		{
		}

		/// <summary>
		/// This is just a hook, if needed, to readily suspend logging, without having to rewire the core
		/// </summary>
		public bool Active = true;

		public string SubType;
		public int SubVer;

		/// <summary>
		/// Tests whether the other CodeDataLog is structurally identical
		/// </summary>
		public bool Check(CodeDataLog other)
		{
			if (SubType != other.SubType)
				return false;
			if (SubVer != other.SubVer)
				return false;

			if (this.Count != other.Count)
				return false;
			foreach (var kvp in this)
			{
				if (!other.ContainsKey(kvp.Key))
					return false;
				var oval = other[kvp.Key];
				if (oval.Length != kvp.Value.Length)
					return false;
			}

			return true;
		}

		public void LogicalOrFrom(CodeDataLog other)
		{
			if (this.Count != other.Count)
				throw new InvalidDataException("Dictionaries must have the same number of keys!");

			foreach (var kvp in other)
			{
				byte[] fromdata = kvp.Value;
				byte[] todata = this[kvp.Key];

				if (fromdata.Length != todata.Length)
					throw new InvalidDataException("Memory regions must be the same size!");

				for (int i = 0; i < todata.Length; i++)
					todata[i] |= fromdata[i];
			}
		}

		public void ClearData()
		{
			foreach (byte[] data in Values)
				Array.Clear(data, 0, data.Length);
		}

		public void Save(Stream s)
		{
			var w = new BinaryWriter(s);
			w.Write("BIZHAWK-CDL-2");
			w.Write(SubType.PadRight(15));
			w.Write(Count);
			foreach (var kvp in this)
			{
				w.Write(kvp.Key);
				w.Write(kvp.Value.Length);
				w.Write(kvp.Value);
			}
			w.Flush();
		}

		public void Load(Stream s)
		{
			var br = new BinaryReader(s);
			string id = br.ReadString();
			if (id == "BIZHAWK-CDL-1")
				SubType = "PCE";
			else if (id == "BIZHAWK-CDL-2")
				SubType = br.ReadString().TrimEnd(' ');
			else
				throw new InvalidDataException("File is not a Bizhawk CDL file!");

			int count = br.ReadInt32();
			for (int i = 0; i < count; i++)
			{
				string key = br.ReadString();
				int len = br.ReadInt32();
				byte[] data = br.ReadBytes(len);
				this[key] = data;
			}

		}
	
	}
}

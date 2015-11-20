﻿using System;
using System.Runtime.InteropServices;
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
		/// Pins the managed arrays. Not that we expect them to be allocated, but in case we do, seeing thish ere will remind us to check for the pin condition and abort
		/// </summary>
		public void Pin()
		{
			if (Pins.Count != 0)
				throw new InvalidOperationException("incremental astrological examination");
			foreach (var kvp in this)
				Pins[kvp.Key] = GCHandle.Alloc(kvp.Value, GCHandleType.Pinned);
		}

		/// <summary>
		/// Unpins the managed arrays, to be paired with calls to Pin()
		/// </summary>
		public void Unpin()
		{
			foreach (var pin in Pins.Values)
				pin.Free();
			Pins.Clear();
		}

		/// <summary>
		/// Retrieves the pointer to a managed array
		/// </summary>
		public IntPtr GetPin(string key)
		{
			return Pins[key].AddrOfPinnedObject();
		}

		/// <summary>
		/// Pinned managed arrays
		/// </summary>
		Dictionary<string, GCHandle> Pins = new Dictionary<string, GCHandle>();

		/// <summary>
		/// Whether the CDL is tracking a block with the given name
		/// </summary>
		public bool Has(string blockname) { return ContainsKey(blockname); }

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
			_Save(s, true);
		}
		
		Dictionary<string, long> _Save(Stream s, bool forReal)
		{
			var ret = new Dictionary<string, long>();
			var w = new BinaryWriter(s);
			w.Write("BIZHAWK-CDL-2");
			w.Write(SubType.PadRight(15));
			w.Write(Count);
			w.Flush();
			long addr = s.Position;
			if (forReal)
			{
				foreach (var kvp in this)
				{
					w.Write(kvp.Key);
					w.Write(kvp.Value.Length);
					w.Write(kvp.Value);
				}
			}
			else
			{
				foreach (var kvp in this)
				{
					addr += kvp.Key.Length + 1; //assumes shortly-encoded key names
					addr += 4;
					ret[kvp.Key] = addr;
					addr += kvp.Value.Length;
				}
			}
			w.Flush();
			return ret;
		}

		public Dictionary<string, long> GetBlockMap()
		{
			return _Save(new MemoryStream(), false);
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


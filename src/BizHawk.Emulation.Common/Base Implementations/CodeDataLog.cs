using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace BizHawk.Emulation.Common
{
	/// <summary>
	/// The base implementation of ICodeDataLog
	/// </summary>
	/// <seealso cref="ICodeDataLogger" /> 
	/// <seealso cref="ICodeDataLog" /> 
	public class CodeDataLog : Dictionary<string, byte[]>, ICodeDataLog
	{
		public CodeDataLog()
		{
			Active = true;
		}

		/// <summary>Pins the managed arrays. Not that we expect them to be allocated, but in case we do, seeing this here will remind us to check for the pin condition and abort</summary>
		/// <exception cref="InvalidOperationException">if called more than once per instantiation</exception>
		public void Pin()
		{
			if (_pins.Count != 0)
			{
				throw new InvalidOperationException("incremental astrological examination");
			}

			foreach (var kvp in this)
			{
				_pins[kvp.Key] = GCHandle.Alloc(kvp.Value, GCHandleType.Pinned);
			}
		}

		/// <summary>
		/// Unpins the managed arrays, to be paired with calls to Pin()
		/// </summary>
		public void Unpin()
		{
			foreach (var pin in _pins.Values)
			{
				pin.Free();
			}

			_pins.Clear();
		}

		/// <summary>
		/// Retrieves the pointer to a managed array
		/// </summary>
		public IntPtr GetPin(string key)
		{
			return _pins[key].AddrOfPinnedObject();
		}

		/// <summary>
		/// Pinned managed arrays
		/// </summary>
		private readonly Dictionary<string, GCHandle> _pins = new Dictionary<string, GCHandle>();

		/// <summary>
		/// Whether the CDL is tracking a block with the given name
		/// </summary>
		public bool Has(string blockName) => ContainsKey(blockName);

		/// <summary>
		/// This is just a hook, if needed, to readily suspend logging, without having to rewire the core
		/// </summary>
		public bool Active { get; set; }

		public string SubType { get; set; }
		public int SubVer { get; set; }

		/// <summary>
		/// Tests whether the other CodeDataLog is structurally identical
		/// </summary>
		public bool Check(ICodeDataLog other)
		{
			if (SubType != other.SubType)
			{
				return false;
			}

			if (SubVer != other.SubVer)
			{
				return false;
			}

			if (Count != other.Count)
			{
				return false;
			}

			foreach (var kvp in this)
			{
				if (!other.ContainsKey(kvp.Key))
				{
					return false;
				}

				var oval = other[kvp.Key];
				if (oval.Length != kvp.Value.Length)
				{
					return false;
				}
			}

			return true;
		}

		/// <exception cref="InvalidDataException">
		/// <paramref name="other"/> is not the same length as <see langword="this"/>, or
		/// any value differs in size from the corresponding value in <paramref name="other"/>
		/// </exception>
		public void LogicalOrFrom(ICodeDataLog other)
		{
			if (Count != other.Count)
			{
				throw new InvalidDataException("Dictionaries must have the same number of keys!");
			}

			foreach (var kvp in other)
			{
				byte[] fromData = kvp.Value;
				byte[] toData = this[kvp.Key];

				if (fromData.Length != toData.Length)
				{
					throw new InvalidDataException("Memory regions must be the same size!");
				}

				for (int i = 0; i < toData.Length; i++)
				{
					toData[i] |= fromData[i];
				}
			}
		}

		public void ClearData()
		{
			foreach (byte[] data in Values)
			{
				Array.Clear(data, 0, data.Length);
			}
		}

		public void Save(Stream s)
		{
			SaveInternal(s, true);
		}
		
		private Dictionary<string, long> SaveInternal(Stream s, bool forReal)
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
			return SaveInternal(new MemoryStream(), false);
		}

		/// <exception cref="InvalidDataException">contents of <paramref name="s"/> do not begin with valid file header</exception>
		public void Load(Stream s)
		{
			var br = new BinaryReader(s);
			string id = br.ReadString();
			SubType = id switch
			{
				"BIZHAWK-CDL-1" => "PCE",
				"BIZHAWK-CDL-2" => br.ReadString().TrimEnd(' '),
				_ => throw new InvalidDataException("File is not a BizHawk CDL file!"),
			};
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


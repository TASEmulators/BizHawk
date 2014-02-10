using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace BizHawk.Emulation.Cores.Components.H6280
{
	public class CodeDataLog : Dictionary<string, byte[]>
	{
		private CodeDataLog()
			:base()
		{
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
			w.Write(Count);
			foreach (var kvp in this)
			{
				w.Write(kvp.Key);
				w.Write(kvp.Value.Length);
				w.Write(kvp.Value);
			}
			w.Flush();
		}

		public static CodeDataLog Load(Stream s)
		{
			var t = new CodeDataLog();
			var r = new BinaryReader(s);
			int count = r.ReadInt32();
			for (int i = 0; i < count; i++)
			{
				string key = r.ReadString();
				int len = r.ReadInt32();
				byte[] data = r.ReadBytes(len);
				t[key] = data;
			}
			return t;
		}

		public static CodeDataLog Create(IEnumerable<HuC6280.MemMapping> mm)
		{
			var t = new CodeDataLog();

			Dictionary<string, int> sizes = new Dictionary<string, int>();
			foreach (var m in mm)
			{
				if (!sizes.ContainsKey(m.Name) || m.MaxOffs >= sizes[m.Name])
					sizes[m.Name] = m.MaxOffs;
			}

			foreach (var kvp in sizes)
			{
				// becase we were looking at offsets, and each bank is 8192 big, we need to add that size
				t[kvp.Key] = new byte[kvp.Value + 8192];
			}
			return t;
		}
	}

	public partial class HuC6280
	{
		public struct MemMapping
		{
			public string Name;
			public int Offs;
			public int VOffs; // if non-zero, specifies a larger potential offset
			public int MaxOffs { get { return Math.Max(Offs, VOffs); } }
		}

		public MemMapping[] Mappings; // = new MemMapping[256];

		public CodeDataLog CDL = null;

		public bool CDLLoggingActive = false;

		[Flags]
		enum CDLUsage : byte
		{
			// was fetched as an opcode
			Code = 0x01,
			// was read or written as data
			Data = 0x02,
			// was read and used as a pointer to data via indirect addressing
			DataPtr = 0x04,
			// was read or written as stack
			Stack = 0x08,
			// was read or written as data via indirect addressing
			IndirectData = 0x10,
			// was read and used as function pointer
			// NB: there is no "IndirectCode"; all code is marked simply as code regardless of how it is reached
			FcnPtr = 0x20,
			// was used as a source (either initial or during the loop) of a block xfer
			BlockFrom = 0x40,
			// was used as a destination (either initial or during the loop) of a block xfer
			BlockTo = 0x80
		}

		void Mark(ushort addr, CDLUsage flag)
		{
			var m = Mappings[MPR[addr >> 13]];
			CDL[m.Name][addr & 0x1fff | m.Offs] |= (byte)flag;
		}

		// mark addr as having been fetched for execute
		void MarkCode(int addr_)
		{
			ushort addr = (ushort)addr_;
			Mark(addr, CDLUsage.Code);
		}

		// mark addr as having been seen as data
		void MarkAddr(int addr_)
		{
			ushort addr = (ushort)addr_;
			Mark(addr, CDLUsage.Data);
		}

		// convert address to zero-page, then mark as data
		void MarkZP(int addr_)
		{
			ushort addr = (ushort)(addr_ & 0xff | 0x2000);
			Mark(addr, CDLUsage.Data);
		}

		// convert address to zero-page, then return the pointer stored there
		ushort GetIndirect(int addr_)
		{
			ushort addr = (ushort)(addr_ & 0xff | 0x2000);
			return ReadWordPageWrap(addr);
		}

		// convert address to zero-page, then mark as pointer (two bytes)
		void MarkZPPtr(int addr_)
		{
			ushort addr = (ushort)(addr_ & 0xff | 0x2000);
			ushort addr2 = (ushort)(addr & 0xff00 | (addr + 1) & 0x00ff);
			Mark(addr, CDLUsage.DataPtr);
			Mark(addr2, CDLUsage.DataPtr);
		}

		// mark address as destination data of an indirect pointer
		void MarkIndirect(int addr_)
		{
			ushort addr = (ushort)addr_;
			Mark(addr, CDLUsage.IndirectData);
		}

		// mark stack space
		void MarkPush(int n)
		{
			for (int i = 0; i < n; i++)
			{
				ushort addr = (ushort)(S - i);
				Mark(addr, CDLUsage.Stack);
			}
		}

		void MarkPop(int n)
		{
			for (int i = 0; i < n; i++)
			{
				ushort addr = (ushort)(S + i + 1);
				Mark(addr, CDLUsage.Stack);
			}
		}

		// mark addr as function pointer (2 bytes)
		void MarkFptr(int addr_)
		{
			ushort addr = (ushort)addr_;
			ushort addr2 = (ushort)(addr & 0xff00 | (addr + 1) & 0x00ff);
			Mark(addr, CDLUsage.FcnPtr);
			Mark(addr2, CDLUsage.FcnPtr);
		}

		// block transfer "from"
		void MarkBTFrom(int addr_)
		{
			ushort addr = (ushort)addr_;
			Mark(addr, CDLUsage.BlockFrom);
		}

		// block transfer "to"
		void MarkBTTo(int addr_)
		{
			ushort addr = (ushort)addr_;
			Mark(addr, CDLUsage.BlockTo);
		}
	}
}

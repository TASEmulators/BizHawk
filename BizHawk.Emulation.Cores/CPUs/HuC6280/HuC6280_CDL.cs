using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Components.H6280
{
	public partial class HuC6280
	{
		public void DisassembleCDL(Stream s, ICodeDataLog cdl, IMemoryDomains mem)
		{
			var w = new StreamWriter(s);
			w.WriteLine("; Bizhawk CDL Disassembly");
			w.WriteLine();
			foreach (var kvp in cdl)
			{
				w.WriteLine(".\"{0}\" size=0x{1:x8}", kvp.Key, kvp.Value.Length);

				byte[] cd = kvp.Value;
				var md = mem[kvp.Key];

				for (int i = 0; i < kvp.Value.Length; i++)
				{
					if ((kvp.Value[i] & (byte)HuC6280.CDLUsage.Code) != 0)
					{
						int unused;
						string dis = HuC6280.DisassembleExt(
							0,
							out unused,
							delegate(ushort addr)
							{
								return md.PeekByte(addr + i);
							},
							delegate(ushort addr)
							{
								return md.PeekUshort(addr + i, false);
							}
						);
						w.WriteLine("0x{0:x8}: {1}", i, dis);
					}
				}
				w.WriteLine();
			}
			w.WriteLine("; EOF");
			w.Flush();
		}

		private static Dictionary<string, int> SizesFromHuMap(IEnumerable<HuC6280.MemMapping> mm)
		{
			Dictionary<string, int> sizes = new Dictionary<string, int>();
			foreach (var m in mm)
			{
				if (!sizes.ContainsKey(m.Name) || m.MaxOffs >= sizes[m.Name])
				sizes[m.Name] = m.MaxOffs;
			}

			List<string> keys = new List<string>(sizes.Keys);
			foreach (var key in keys)
			{
				// becase we were looking at offsets, and each bank is 8192 big, we need to add that size
				sizes[key] += 8192;
			}
			return sizes;
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

		public ICodeDataLog CDL = null;

		[Flags]
		public enum CDLUsage : byte
		{
			// was fetched as an opcode first byte
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
			// was used as a source or destination (either initial or during the loop) of a block xfer
			BlockXFer = 0x40,
			// was fetched as an operand byte to an opcode
			CodeOperand = 0x80
		}

		void Mark(ushort addr, CDLUsage flag)
		{
			var m = Mappings[MPR[addr >> 13]];
			CDL[m.Name][addr & 0x1fff | m.Offs] |= (byte)flag;
		}

		// mark addr as having been fetched for execute
		void MarkCode(int addr_, int n)
		{
			for (int i = 0; i < n; i++)
			{
				ushort addr = (ushort)(addr_ + i);
				Mark(addr, i == 0 ? CDLUsage.Code : CDLUsage.CodeOperand);
			}
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
			Mark(addr, CDLUsage.BlockXFer);
		}

		// block transfer "to"
		void MarkBTTo(int addr_)
		{
			ushort addr = (ushort)addr_;
			Mark(addr, CDLUsage.BlockXFer);
		}
	}
}

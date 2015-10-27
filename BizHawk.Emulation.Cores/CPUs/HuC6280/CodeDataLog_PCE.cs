using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Components.H6280
{
	public class CodeDataLog_PCE : CodeDataLog
	{
		public static CodeDataLog_PCE Create(IEnumerable<HuC6280.MemMapping> mm)
		{
			var t = new CodeDataLog_PCE();
			foreach (var kvp in SizesFromHuMap(mm))
			{
				t[kvp.Key] = new byte[kvp.Value];
			}
			return t;
		}

		public override string SubType { get { return "PCE"; } }
		public override int SubVer { get { return 0; } }

		public override void Disassemble(Stream s, IMemoryDomains mem)
		{
			var w = new StreamWriter(s);
			w.WriteLine("; Bizhawk CDL Disassembly");
			w.WriteLine();
			foreach (var kvp in this)
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
								return md.PeekWord(addr + i, false);
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

		public bool CheckConsistency(object arg)
		{
			var mm = (IEnumerable<HuC6280.MemMapping>)arg;
			var sizes = SizesFromHuMap(mm);
			if (sizes.Count != Count)
				return false;
			foreach (var kvp in sizes)
			{
				if (!ContainsKey(kvp.Key))
					return false;
				if (this[kvp.Key].Length != kvp.Value)
					return false;
			}
			return true;
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
}

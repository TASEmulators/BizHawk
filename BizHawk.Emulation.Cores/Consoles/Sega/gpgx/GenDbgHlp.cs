using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;

namespace BizHawk.Emulation.Cores.Consoles.Sega.gpgx
{
	/*
	 * how to use:
	 * 0) get https://code.google.com/p/pdbparse/
	 * 1) set modulename to the name of the dll file.
	 * 2) set symbolname to the name of a file that you produced by executing the following command:
	 *    pdb_print_gvars.py [module pdb file] 0x00000000 > [output file]
	 * 3) set start to an address (relative to the beginning of the dll) to start scanning
	 * 4) set length to the byte length of the scan area
	 * 5) instantiate a GenDbWind, and use it to control the scanner while you manipulate the dll into various configurations.
	 * 
	 * ideas for modification:
	 * 1) unhardcode config parameters and allow modifying them through the interface
	 * 2) read section sizes and positions from the dll itself instead of the start\length params
	 * 3) support an ignore list of symbols
	 */

	public class GenDbgHlp : IDisposable
	{
		private static class Win32
		{
			[DllImport("kernel32.dll")]
			public static extern IntPtr LoadLibrary(string dllToLoad);
			[DllImport("kernel32.dll")]
			public static extern IntPtr GetProcAddress(IntPtr hModule, string procedureName);
			[DllImport("kernel32.dll")]
			public static extern bool FreeLibrary(IntPtr hModule);
		}

		// config
		const string modulename = "libgenplusgx.dll";
		const string symbolname = @"D:\encodes\bizhawksrc\genplus-gx\libretro\msvc\Debug\vars.txt";
		const int start = 0x0c7d8000 - 0x0c540000;
		const int length = 0x01082000;

		bool disposed = false;

		public void Dispose()
		{
			if (!disposed)
			{
				Win32.FreeLibrary(DllBase);
				DllBase = IntPtr.Zero;
				disposed = true;
			}
		}

		IntPtr DllBase;

		List<Symbol> SymbolsByAddr = new List<Symbol>();
		Dictionary<string, Symbol> SymbolsByName = new Dictionary<string, Symbol>();

		byte[][] data = new byte[10][];

		public void SaveState(int statenum)
		{
			if (disposed) throw new ObjectDisposedException(this.GetType().ToString());

			if (data[statenum] == null)
				data[statenum] = new byte[length];

			Marshal.Copy(DllBase + start, data[statenum], 0, length);
			Console.WriteLine("State {0} saved", statenum);
		}


		unsafe public void Cmp(int statex, int statey)
		{
			if (disposed) throw new ObjectDisposedException(this.GetType().ToString());
			List<Tuple<int, int>> bads = new List<Tuple<int, int>>();

			byte[] x = data[statex];
			byte[] y = data[statey];

			if (x == null || y == null)
			{
				Console.WriteLine("Missing State!");
				return;
			}

			bool inrange = false;
			int startsec = 0;

			fixed (byte* p0 = &x[0])
			fixed (byte* p1 = &y[0])
			{
				for (int i = 0; i < length; i++)
				{
					if (!inrange)
					{
						if (p0[i] != p1[i])
						{
							startsec = i;
							inrange = true;
						}
					}
					else
					{
						if (p0[i] == p1[i])
						{
							bads.Add(new Tuple<int, int>(startsec, i));
							inrange = false;
						}
					}
				}
			}
			if (inrange)
				bads.Add(new Tuple<int, int>(startsec, length));

			for (int i = 0; i < bads.Count; i++)
			{
				IntPtr addr = (IntPtr)(bads[i].Item1 + start);
				int len = bads[i].Item2 - bads[i].Item1;

				var ss = Find(addr, len);
				Console.WriteLine("0x{0:X8}[0x{1}]", (int)addr, len);
				foreach (var sym in ss)
					Console.WriteLine(sym);
				Console.WriteLine();
			}
			if (bads.Count == 0)
				Console.WriteLine("Clean!");
		}



		public GenDbgHlp()
		{
			using (StreamReader sr = new StreamReader(symbolname))
			{
				string line;
				while ((line = sr.ReadLine()) != null)
				{
					Symbol sym = Symbol.FromString(line);
					SymbolsByAddr.Add(sym);
					SymbolsByName.Add(sym.name, sym);
				}
				SymbolsByAddr.Sort();
			}

			DllBase = Win32.LoadLibrary(modulename);
			if (DllBase == IntPtr.Zero)
				throw new Exception();
		}

		public List<Symbol> Find(IntPtr addr, int length)
		{
			if (disposed) throw new ObjectDisposedException(this.GetType().ToString());
			Symbol min = new Symbol { addr = addr };
			Symbol max = new Symbol { addr = addr + length };

			int minidx = SymbolsByAddr.BinarySearch(min);
			if (minidx < 0)
			{
				minidx = ~minidx;
				// inexact matches return the first larger value, so find the next smallset one
				if (minidx > 0)
					minidx--;
			}
			int maxidx = SymbolsByAddr.BinarySearch(max);
			if (maxidx < 0)
			{
				maxidx = ~maxidx;
				if (maxidx > 0)
					maxidx--;
			}
			return SymbolsByAddr.GetRange(minidx, maxidx - minidx + 1);
		}


		public struct Symbol : IComparable<Symbol>
		{
			public IntPtr addr;
			public string section;
			public string name;

			public static Symbol FromString(string s)
			{
				string[] ss = s.Split(',');
				if (ss.Length != 4)
					throw new Exception();
				if (!ss[1].StartsWith("0x"))
					throw new Exception();
				Symbol ret = new Symbol
				{
					addr = (IntPtr)int.Parse(ss[1].Substring(2), System.Globalization.NumberStyles.AllowHexSpecifier),
					section = ss[3],
					name = ss[0]
				};
				return ret;
			}

			public int CompareTo(Symbol other)
			{
				return (int)this.addr - (int)other.addr;
			}

			public override string ToString()
			{
				return string.Format("0x{0:X8} {1} ({2})", (int)addr, name, section);
			}
		}


	}
}

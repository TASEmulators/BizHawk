using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using ELFSharp.ELF;
using ELFSharp.ELF.Sections;
using ELFSharp.ELF.Segments;
using System.Reflection;

namespace BizHawk.Emulation.Cores
{
	public sealed class ElfRunner : IDisposable
	{
		// TODO: a lot of things only work with our elves and aren't fully generalized

		private ELF<long> _elf;
		private MemoryBlock _base;
		private long _loadoffset;
		private Dictionary<string, SymbolEntry<long>> _symdict;
		private List<SymbolEntry<long>> _symlist;

		public ElfRunner(string filename)
		{
			// todo: hack up this baby to take Streams
			_elf = ELFReader.Load<long>(filename);

			var loadsegs = _elf.Segments.Where(s => s.Type == SegmentType.Load);

			long orig_start = loadsegs.Min(s => s.Address);
			orig_start &= ~(Environment.SystemPageSize - 1);
			long orig_end = loadsegs.Max(s => s.Address + s.Size);
			_base = new MemoryBlock(UIntPtr.Zero, orig_end - orig_start);
			_loadoffset = (long)_base.Start - orig_start;

			try
			{
				_base.Set(_base.Start, _base.Size, MemoryBlock.Protection.RW);

				foreach (var seg in loadsegs)
				{
					var data = seg.GetContents();
					Marshal.Copy(data, 0, (IntPtr)(seg.Address + _loadoffset), data.Length);
				}
				RegisterSymbols();
				ProcessRelocations();


				_base.Set(_base.Start, _base.Size, MemoryBlock.Protection.R);

				foreach (var sec in _elf.Sections.Where(s => s.Flags.HasFlag(SectionFlags.Allocatable)))
				{
					if (sec.Flags.HasFlag(SectionFlags.Executable))
						_base.Set((UIntPtr)(sec.LoadAddress + _loadoffset), sec.Size, MemoryBlock.Protection.RX);
					else if (sec.Flags.HasFlag(SectionFlags.Writable))
						_base.Set((UIntPtr)(sec.LoadAddress + _loadoffset), sec.Size, MemoryBlock.Protection.RW);
				}

				//FixupGOT();
				ConnectAllClibPatches();
				Console.WriteLine("Loaded {0}@{1:X16}", filename, (long)_base.Start);
			}
			catch
			{
				_base.Dispose();
				throw;
			}
		}

		private class Elf32_Rel
		{
			public long Address;
			public byte Type;
			public int SymbolIdx;
			public long Addend;

			public Elf32_Rel(byte[] data, int start, int len)
			{
				if (len == 8 || len == 12)
				{
					Address = BitConverter.ToInt32(data, start);
					Type = data[start + 4];
					SymbolIdx = (int)(BitConverter.ToUInt32(data, start + 4) >> 8);
					Addend = data.Length == 12 ? BitConverter.ToInt32(data, start + 8) : 0;
				}
				else
				{
					throw new InvalidOperationException();
				}
			}
		}

		// elfsharp does not read relocation tables, so there
		private void ProcessRelocations()
		{
			foreach (var rel in _elf.Sections.Where(s => s.Name.StartsWith(".rel")))
			{
				byte[] data = rel.GetContents();
				var symbols = Enumerable.Range(0, data.Length / 8)
					.Select(i => new Elf32_Rel(data, i * 8, 8));
				foreach (var symbol in symbols)
				{
					ApplyRelocation(symbol);
				}
			}
		}
		private void ApplyRelocation(Elf32_Rel rel)
		{
			// http://flint.cs.yale.edu/cs422/doc/ELF_Format.pdf
			// this is probably mostly wrong

			long val = 0;
			long A = rel.Addend;
			// since all symbols were moved by the same amount, just add _loadoffset here
			long S = _symlist[rel.SymbolIdx].Value + _loadoffset;
			long B = _loadoffset;
			switch (rel.Type)
			{
				case 0: val = 0; break;
				case 1:  val = S + A; break;
				case 2: throw new NotImplementedException();
				case 3: throw new NotImplementedException();
				case 4: throw new NotImplementedException();
				case 5: val = 0; break;
				case 6: val = S; break;
				case 7: val = S; break;
				case 8: val = B + A; break;
				case 9: throw new NotImplementedException();
				case 10: throw new NotImplementedException();
				default: throw new InvalidOperationException();
			}
			byte[] tmp = new byte[4];
			Marshal.Copy((IntPtr)(rel.Address + _loadoffset), tmp, 0, 4);
			long currentVal = BitConverter.ToUInt32(tmp, 0);
			tmp = BitConverter.GetBytes((uint)(currentVal + val));
			Marshal.Copy(tmp, 0, (IntPtr)(rel.Address + _loadoffset), 4);
		}

		private void RegisterSymbols()
		{
			var symbols = ((ISymbolTable)_elf.GetSection(".symtab"))
			.Entries
			.Cast<SymbolEntry<long>>();

			// when there are duplicate names, don't register either in the dictionary
			_symdict = symbols
			.GroupBy(e => e.Name)
			.Where(g => g.Count() == 1)
			.ToDictionary(g => g.Key, g => g.First());

			_symlist = symbols.ToList();
		}

		public void PopulateInterface(object o)
		{
			var fields = o.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public)
				.Where(fi => typeof(Delegate).IsAssignableFrom(fi.FieldType))
				.Where(fi => fi.FieldType.GetCustomAttributes(typeof(UnmanagedFunctionPointerAttribute), false).Length > 0);

			foreach (var fi in fields)
			{
				var sym = _symdict[fi.Name]; // TODO: allow some sort of EntryPoint attribute
				if (sym.Type != SymbolType.Function)
					throw new InvalidOperationException("Unexpected symbol type for alleged function!");
				IntPtr ptr = (IntPtr)(sym.Value + _loadoffset);
				fi.SetValue(o, Marshal.GetDelegateForFunctionPointer(ptr, fi.FieldType));
			}
		}

		public void Dispose()
		{
			Dispose(true);
			//GC.SuppressFinalize(this);
		}

		//~ElfRunner()
		//{
		//	Dispose(false);
		//}

		private void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (_base != null)
				{
					_base.Dispose();
					_base = null;
				}
				if (_heaps != null)
				{
					foreach (var b in _heaps.Values)
					{
						b.Dispose();
					}
					_heaps = null;
				}
			}
		}


		#region clib monkeypatches
		// our clib expects a few function pointers to be defined for it

		/// <summary>
		/// heap free callback
		/// </summary>
		/// <param name="p">ptr</param>
		/// <param name="size">bytesize</param>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void FreeMem_D(IntPtr p, IntPtr size);
		/// <summary>
		/// heap alloc callback
		/// </summary>
		/// <param name="size">bytesize</param>
		/// <returns></returns>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate IntPtr AllocMem_D(IntPtr size);
		/// <summary>
		/// exit() callback
		/// </summary>
		/// <param name="status">desired exit code</param>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void Exit_D(int status);

		[CLibPatch("_ZZFree")]
		private void FreeMem(IntPtr p, IntPtr size)
		{
			MemoryBlock block;
			if (_heaps.TryGetValue(p, out block))
			{
				_heaps.Remove(p);
				block.Dispose();
			}
		}
		[CLibPatch("_ZZAlloc")]
		private IntPtr AllocMem(IntPtr size)
		{
			var block = new MemoryBlock((long)size);
			var p = (IntPtr)(long)block.Start;
			_heaps[p] = block;
			block.Set(block.Start, block.Size, MemoryBlock.Protection.RW);
			Console.WriteLine("AllocMem: {0:X8}@{1:X16}", (long)size, (long)block.Start);
			return p;
		}
		[CLibPatch("_ZZExit")]
		private void Exit(int status)
		{
			throw new InvalidOperationException("Client code called exit()");
		}

		/// <summary>
		/// list of memoryblocks that need to be cleaned up
		/// </summary>
		private Dictionary<IntPtr, MemoryBlock> _heaps = new Dictionary<IntPtr, MemoryBlock>();


		/// <summary>
		/// list of delegates that need to not be GCed
		/// </summary>
		private List<Delegate> _delegates = new List<Delegate>();

		private void ConnectAllClibPatches()
		{
			var methods = GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
				.Where(mi => mi.GetCustomAttributes(typeof(CLibPatchAttribute), false).Length > 0);
			foreach (var mi in methods)
			{
				var delegateType = GetType().GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic)
					.Single(t => t.Name == mi.Name + "_D");
				var del = Delegate.CreateDelegate(delegateType, this, mi);
				IntPtr ptr = Marshal.GetFunctionPointerForDelegate(del);
				_delegates.Add(del);
				var sym = _symdict[((CLibPatchAttribute)mi.GetCustomAttributes(typeof(CLibPatchAttribute), false)[0]).NativeName];
				if (sym.Size != IntPtr.Size)
					throw new InvalidOperationException("Unexpected function pointer size patching clib!");
				Marshal.Copy(new[] { ptr }, 0, (IntPtr)(sym.Value + _loadoffset), 1);
			}
		}

		[AttributeUsage(AttributeTargets.Method)]
		private class CLibPatchAttribute : Attribute
		{
			public string NativeName { get; private set; }
			public CLibPatchAttribute(string nativeName)
			{
				NativeName = nativeName;
			}
		}

		#endregion
	}
}

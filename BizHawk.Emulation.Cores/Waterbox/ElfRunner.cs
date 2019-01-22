using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using ELFSharp.ELF;
using ELFSharp.ELF.Sections;
using ELFSharp.ELF.Segments;
using System.Reflection;
using BizHawk.Common;
using System.Security.Cryptography;
using System.IO;
using System.Collections.Concurrent;
using System.Threading;
using BizHawk.Emulation.Common;
using BizHawk.Common.BizInvoke;

namespace BizHawk.Emulation.Cores.Waterbox
{
	public sealed class ElfRunner : Swappable, IImportResolver, IBinaryStateable
	{
		// TODO: a lot of things only work with our elves and aren't fully generalized

		private ELF<long> _elf;
		private byte[] _elfhash;

		/// <summary>
		/// executable is loaded here
		/// </summary>
		private MemoryBlock _base;
		/// <summary>
		/// standard malloc() heap
		/// </summary>
		private Heap _heap;

		/// <summary>
		/// sealed heap (writable only during init)
		/// </summary>
		private Heap _sealedheap;

		/// <summary>
		/// invisible heap (not savestated, use with care)
		/// </summary>
		private Heap _invisibleheap;

		private long _loadoffset;
		private Dictionary<string, SymbolEntry<long>> _symdict;
		private List<SymbolEntry<long>> _symlist;

		/// <summary>
		/// everything to clean up at dispose time
		/// </summary>
		private List<IDisposable> _disposeList = new List<IDisposable>();

		private ulong GetHeapStart(ulong prevend)
		{
			// if relocatable, we won't have constant pointers, so put the heap anywhere
			// otherwise, put the heap at a canonical location aligned 1MB from the end of the elf, then incremented 16MB
			ulong heapstart = HasRelocations() ? 0 : ((prevend - 1) | 0xfffff) + 0x1000001;
			return heapstart;
		}

		public ElfRunner(string filename, long heapsize, long sealedheapsize, long invisibleheapsize)
		{
			using (var fs = new FileStream(filename, FileMode.Open, FileAccess.Read))
			{
				_elfhash = WaterboxUtils.Hash(fs);
			}

			// todo: hack up this baby to take Streams
			_elf = ELFReader.Load<long>(filename);

			var loadsegs = _elf.Segments.Where(s => s.Type == SegmentType.Load);

			long orig_start = loadsegs.Min(s => s.Address);
			orig_start &= ~(Environment.SystemPageSize - 1);
			long orig_end = loadsegs.Max(s => s.Address + s.Size);
			if (HasRelocations())
			{
				_base = MemoryBlock.PlatformConstructor((ulong) (orig_end - orig_start));
				_loadoffset = (long)_base.Start - orig_start;
				Initialize(0);
			}
			else
			{
				Initialize((ulong)orig_start);
				_base = MemoryBlock.PlatformConstructor((ulong) orig_start, (ulong) (orig_end - orig_start));
				_loadoffset = 0;
				Enter();
			}

			try
			{
				_disposeList.Add(_base);
				AddMemoryBlock(_base, "elf");
				_base.Activate();
				_base.Protect(_base.Start, _base.Size, MemoryBlock.Protection.RW);

				foreach (var seg in loadsegs)
				{
					var data = seg.GetContents();
					Marshal.Copy(data, 0, Z.SS(seg.Address + _loadoffset), data.Length);
				}
				RegisterSymbols();
				ProcessRelocations();

				_base.Protect(_base.Start, _base.Size, MemoryBlock.Protection.R);

				foreach (var sec in _elf.Sections.Where(s => (s.Flags & SectionFlags.Allocatable) != 0))
				{
					if ((sec.Flags & SectionFlags.Executable) != 0)
						_base.Protect((ulong)(sec.LoadAddress + _loadoffset), (ulong)sec.Size, MemoryBlock.Protection.RX);
					else if ((sec.Flags & SectionFlags.Writable) != 0)
						_base.Protect((ulong)(sec.LoadAddress + _loadoffset), (ulong)sec.Size, MemoryBlock.Protection.RW);
				}

				ulong end = _base.End;

				if (heapsize > 0)
				{
					_heap = new Heap(GetHeapStart(end), (ulong)heapsize, "sbrk-heap");
					_heap.Memory.Activate();
					end = _heap.Memory.End;
					_disposeList.Add(_heap);
					AddMemoryBlock(_heap.Memory, "sbrk - heap");
				}

				if (sealedheapsize > 0)
				{
					_sealedheap = new Heap(GetHeapStart(end), (ulong)sealedheapsize, "sealed-heap");
					_sealedheap.Memory.Activate();
					end = _sealedheap.Memory.End;
					_disposeList.Add(_sealedheap);
					AddMemoryBlock(_sealedheap.Memory, "sealed-heap");
				}

				if (invisibleheapsize > 0)
				{
					_invisibleheap = new Heap(GetHeapStart(end), (ulong)invisibleheapsize, "invisible-heap");
					_invisibleheap.Memory.Activate();
					end = _invisibleheap.Memory.End;
					_disposeList.Add(_invisibleheap);
					AddMemoryBlock(_invisibleheap.Memory, "invisible-heap");
				}

				ConnectAllClibPatches();
				Console.WriteLine("Loaded {0}@{1:X16}", filename, _base.Start);
				foreach (var sec in _elf.Sections.Where(s => s.LoadAddress != 0))
				{
					Console.WriteLine("  {0}@{1:X16}, size {2}", sec.Name.PadLeft(20), sec.LoadAddress + _loadoffset, sec.Size.ToString().PadLeft(12));
				}

				PrintTopSavableSymbols();
			}
			catch
			{
				Dispose();
				throw;
			}
			finally
			{
				Exit();
			}
		}

		private void PrintTopSavableSymbols()
		{
			Console.WriteLine("Top savestate symbols:");
			foreach (var text in _symlist
				.Where(s => s.PointedSection != null && (s.PointedSection.Flags & SectionFlags.Writable) != 0)
				.OrderByDescending(s => s.Size)
				.Take(30)
				.Select(s => string.Format("{0} size {1}", s.Name, s.Size)))
			{
				Console.WriteLine(text);
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

		private bool HasRelocations()
		{
			return _elf.Sections.Any(s => s.Name.StartsWith(".rel"));
		}

		// elfsharp does not read relocation tables, so there
		private void ProcessRelocations()
		{
			// todo: amd64
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
				case 1: val = S + A; break;
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

		public void Seal()
		{
			Enter();
			try
			{
				_sealedheap.Seal();
			}
			finally
			{
				Exit();
			}
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			if (disposing)
			{
				foreach (var d in _disposeList)
					d.Dispose();
				_disposeList.Clear();
				PurgeMemoryBlocks();
				_base = null;
				_heap = null;
				_sealedheap = null;
				_invisibleheap = null;
			}
		}

		#region clib monkeypatches

		// our clib expects a few function pointers to be defined for it

		/// <summary>
		/// abort() / other abnormal situation
		/// </summary>
		/// <param name="status">desired exit code</param>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void Trap_D();

		/// <summary>
		/// expand heap
		/// </summary>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate IntPtr Sbrk_D(UIntPtr n);

		/// <summary>
		/// output a string
		/// </summary>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void DebugPuts_D(string s);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate IntPtr SbrkSealed_D(UIntPtr n);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate IntPtr SbrkInvisible_D(UIntPtr n);

		[CLibPatch("_ecl_trap")]
		private void Trap()
		{
			throw new InvalidOperationException("Waterbox code trapped!");
		}

		[CLibPatch("_ecl_sbrk")]
		private IntPtr Sbrk(UIntPtr n)
		{
			return Z.US(_heap.Allocate((ulong)n, 1));
		}

		[CLibPatch("_ecl_debug_puts")]
		private void DebugPuts(string s)
		{
			Console.WriteLine("Waterbox debug puts: {0}", s);
		}

		[CLibPatch("_ecl_sbrk_sealed")]
		private IntPtr SbrkSealed(UIntPtr n)
		{
			return Z.US(_sealedheap.Allocate((ulong)n, 16));
		}

		[CLibPatch("_ecl_sbrk_invisible")]
		private IntPtr SbrkInvisible(UIntPtr n)
		{
			return Z.US(_invisibleheap.Allocate((ulong)n, 16));
		}

		/// <summary>
		/// list of delegates that need to not be GCed
		/// </summary>
		private List<Delegate> _delegates = new List<Delegate>();

		private void ConnectAllClibPatches()
		{
			_delegates.Clear(); // in case we're reconnecting

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
				IntPtr dest = Z.SS(sym.Value + _loadoffset);
				Marshal.Copy(new[] { ptr }, 0, dest, 1);
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

		public IntPtr Resolve(string entryPoint)
		{
			SymbolEntry<long> sym;
			if (_symdict.TryGetValue(entryPoint, out sym))
			{
				return Z.SS(sym.Value + _loadoffset);
			}
			else
			{
				return IntPtr.Zero;
			}
		}

		#region state

		const ulong MAGIC = 0xb00b1e5b00b1e569;

		public void SaveStateBinary(BinaryWriter bw)
		{
			Enter();
			try
			{
				bw.Write(MAGIC);
				bw.Write(_elfhash);
				bw.Write(_loadoffset);
				foreach (var sec in _elf.Sections.Where(s => (s.Flags & SectionFlags.Writable) != 0))
				{
					var ms = _base.GetStream((ulong)(sec.LoadAddress + _loadoffset), (ulong)sec.Size, false);
					bw.Write(sec.Size);
					ms.CopyTo(bw.BaseStream);
				}

				if (_heap != null) _heap.SaveStateBinary(bw);
				if (_sealedheap != null) _sealedheap.SaveStateBinary(bw);
				bw.Write(MAGIC);
			}
			finally
			{
				Exit();
			}
		}

		public void LoadStateBinary(BinaryReader br)
		{
			Enter();
			try
			{
				if (br.ReadUInt64() != MAGIC)
					throw new InvalidOperationException("Magic not magic enough!");
				if (!br.ReadBytes(_elfhash.Length).SequenceEqual(_elfhash))
					throw new InvalidOperationException("Elf changed disguise!");
				if (br.ReadInt64() != _loadoffset)
					throw new InvalidOperationException("Trickys elves moved on you!");

				foreach (var sec in _elf.Sections.Where(s => (s.Flags & SectionFlags.Writable) != 0))
				{
					var len = br.ReadInt64();
					if (sec.Size != len)
						throw new InvalidOperationException("Unexpected section size for " + sec.Name);
					var ms = _base.GetStream((ulong)(sec.LoadAddress + _loadoffset), (ulong)sec.Size, true);
					WaterboxUtils.CopySome(br.BaseStream, ms, len);
				}

				if (_heap != null) _heap.LoadStateBinary(br);
				if (_sealedheap != null) _sealedheap.LoadStateBinary(br);
				if (br.ReadUInt64() != MAGIC)
					throw new InvalidOperationException("Magic not magic enough!");

				// the syscall trampolines were overwritten in loadstate (they're in .bss), and if we're cross-session,
				// are no longer valid.  cores must similiarly resend any external pointers they gave the core.
				ConnectAllClibPatches();
			}
			finally
			{
				Exit();
			}
		}

		#endregion

		#region utils

		private byte[] HashSection(ulong ptr, ulong len)
		{
			using (var h = SHA1.Create())
			{
				var ms = _base.GetStream(ptr, len, false);
				return h.ComputeHash(ms);
			}
		}

		#endregion
	}
}

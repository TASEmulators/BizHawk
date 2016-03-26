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

namespace BizHawk.Emulation.Cores
{
	public sealed class ElfRunner : IImportResolver, IDisposable, IMonitor
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

		/// <summary>
		/// _base.Start, or 0 if we were relocated and so don't need to be swapped
		/// </summary>
		private ulong _lockkey;

		private long _loadoffset;
		private Dictionary<string, SymbolEntry<long>> _symdict;
		private List<SymbolEntry<long>> _symlist;

		/// <summary>
		/// everything to clean up at dispose time
		/// </summary>
		private List<IDisposable> _disposeList = new List<IDisposable>();

		/// <summary>
		/// everything to swap in for context switches
		/// </summary>
		private List<MemoryBlock> _memoryBlocks = new List<MemoryBlock>();

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
				_elfhash = Hash(fs);
			}

			// todo: hack up this baby to take Streams
			_elf = ELFReader.Load<long>(filename);

			var loadsegs = _elf.Segments.Where(s => s.Type == SegmentType.Load);

			long orig_start = loadsegs.Min(s => s.Address);
			orig_start &= ~(Environment.SystemPageSize - 1);
			long orig_end = loadsegs.Max(s => s.Address + s.Size);
			if (HasRelocations())
			{
				_base = new MemoryBlock((ulong)(orig_end - orig_start));
				_loadoffset = (long)_base.Start - orig_start;
				_lockkey = 0;
			}
			else
			{
				_lockkey = (ulong)orig_start;
				_base = new MemoryBlock(_lockkey, (ulong)(orig_end - orig_start));
				_loadoffset = 0;
				Enter();
			}

			try
			{
				_disposeList.Add(_base);
				_memoryBlocks.Add(_base);
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
					_memoryBlocks.Add(_heap.Memory);
				}

				if (sealedheapsize > 0)
				{
					_sealedheap = new Heap(GetHeapStart(end), (ulong)sealedheapsize, "sealed-heap");
					_sealedheap.Memory.Activate();
					end = _sealedheap.Memory.End;
					_disposeList.Add(_sealedheap);
					_memoryBlocks.Add(_sealedheap.Memory);
				}

				if (invisibleheapsize > 0)
				{
					_invisibleheap = new Heap(GetHeapStart(end), (ulong)invisibleheapsize, "invisible-heap");
					_invisibleheap.Memory.Activate();
					end = _invisibleheap.Memory.End;
					_disposeList.Add(_invisibleheap);
					_memoryBlocks.Add(_invisibleheap.Memory);
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

		public void Dispose()
		{
			// we don't need to activate to dispose
			Dispose(true);
			//GC.SuppressFinalize(this);
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

		//~ElfRunner()
		//{
		//	Dispose(false);
		//}

		private void Dispose(bool disposing)
		{
			if (disposing)
			{
				foreach (var d in _disposeList)
					d.Dispose();
				_disposeList.Clear();
				_memoryBlocks.Clear();
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

		/// <summary>
		/// true if the IMonitor should be used for native calls
		/// </summary>
		public bool ShouldMonitor { get { return _lockkey != 0; } }

		// any ElfRunner is assumed to conflict with any other ElfRunner at the same base address,
		// but not any other starting address.  so don't put them too close together!

		private class LockInfo
		{
			public object Sync;
			public ElfRunner Loaded;
		}

		private static readonly ConcurrentDictionary<ulong, LockInfo> LockInfos = new ConcurrentDictionary<ulong, LockInfo>();

		static ElfRunner()
		{
			LockInfos.GetOrAdd(0, new LockInfo()); // any errant attempt to lock when ShouldMonitor == false will result in NRE
		}

		/// <summary>
		/// acquire lock and swap this into memory
		/// </summary>
		public void Enter()
		{
			var li = LockInfos.GetOrAdd(_lockkey, new LockInfo { Sync = new object() });
			Monitor.Enter(li.Sync);
			if (li.Loaded != this)
			{
				if (li.Loaded != null)
					li.Loaded.DeactivateInternal();
				li.Loaded = null;
				ActivateInternal();
				li.Loaded = this;
			}
		}

		/// <summary>
		/// release lock
		/// </summary>
		public void Exit()
		{
			var li = LockInfos.GetOrAdd(_lockkey, new LockInfo { Sync = new object() });
			Monitor.Exit(li.Sync);
		}

		private void DeactivateInternal()
		{
			Console.WriteLine("ElfRunner DeactivateInternal {0}", GetHashCode());
			foreach (var m in _memoryBlocks)
				m.Deactivate();
		}

		private void ActivateInternal()
		{
			Console.WriteLine("ElfRunner ActivateInternal {0}", GetHashCode());
			foreach (var m in _memoryBlocks)
				m.Activate();
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
					CopySome(br.BaseStream, ms, len);
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

		private static void CopySome(Stream src, Stream dst, long len)
		{
			var buff = new byte[4096];
			while (len > 0)
			{
				int r = src.Read(buff, 0, (int)Math.Min(len, 4096));
				dst.Write(buff, 0, r);
				len -= r;
			}
		}

		private static byte[] Hash(byte[] data)
		{
			using (var h = SHA1.Create())
			{
				return h.ComputeHash(data);
			}
		}

		private static byte[] Hash(Stream s)
		{
			using (var h = SHA1.Create())
			{
				return h.ComputeHash(s);
			}
		}

		private byte[] HashSection(ulong ptr, ulong len)
		{
			using (var h = SHA1.Create())
			{
				var ms = _base.GetStream(ptr, len, false);
				return h.ComputeHash(ms);
			}
		}

		/// <summary>
		/// a simple grow-only fixed max size heap
		/// </summary>
		private sealed class Heap : IDisposable
		{
			public MemoryBlock Memory { get; private set; }
			/// <summary>
			/// name, used in identifying errors
			/// </summary>
			public string Name { get; private set; }
			/// <summary>
			/// total number of bytes used
			/// </summary>
			public ulong Used { get; private set; }

			/// <summary>
			/// true if the heap has been sealed, preventing further changes
			/// </summary>
			public bool Sealed { get; private set; }

			private byte[] _hash;

			public Heap(ulong start, ulong size, string name)
			{
				Memory = new MemoryBlock(start, size);
				Used = 0;
				Name = name;
			}

			private void EnsureAlignment(int align)
			{
				if (align > 1)
				{
					ulong newused = ((Used - 1) | (ulong)(align - 1)) + 1;
					if (newused > Memory.Size)
					{
						throw new InvalidOperationException(string.Format("Failed to meet alignment {0} on heap {1}", align, Name));
					}
					Used = newused;
				}
			}

			public ulong Allocate(ulong size, int align)
			{
				if (Sealed)
					throw new InvalidOperationException(string.Format("Attempt made to allocate from sealed heap {0}", Name));

				EnsureAlignment(align);

				ulong newused = Used + size;
				if (newused > Memory.Size)
				{
					throw new InvalidOperationException(string.Format("Failed to allocate {0} bytes from heap {1}", size, Name));
				}
				ulong ret = Memory.Start + Used;
				Memory.Protect(ret, newused - Used, MemoryBlock.Protection.RW);
				Used = newused;
				Console.WriteLine("Allocated {0} bytes on {1}", size, Name);
				return ret;
			}

			public void Seal()
			{
				if (!Sealed)
				{
					Memory.Protect(Memory.Start, Memory.Size, MemoryBlock.Protection.R);
					_hash = Hash(Memory.GetStream(Memory.Start, Used, false));
					Sealed = true;
				}
				else
				{
					throw new InvalidOperationException(string.Format("Attempt to reseal heap {0}", Name));
				}
			}

			public void SaveStateBinary(BinaryWriter bw)
			{
				bw.Write(Name);
				bw.Write(Used);
				if (!Sealed)
				{
					var ms = Memory.GetStream(Memory.Start, Used, false);
					ms.CopyTo(bw.BaseStream);
				}
				else
				{
					bw.Write(_hash);
				}
			}

			public void LoadStateBinary(BinaryReader br)
			{
				var name = br.ReadString();
				if (name != Name)
					throw new InvalidOperationException(string.Format("Name did not match for heap {0}", Name));
				var used = br.ReadUInt64();
				if (used > Memory.Size)
					throw new InvalidOperationException(string.Format("Heap {0} used {1} larger than available {2}", Name, used, Memory.Size));
				if (!Sealed)
				{
					Memory.Protect(Memory.Start, Memory.Size, MemoryBlock.Protection.None);
					Memory.Protect(Memory.Start, used, MemoryBlock.Protection.RW);
					var ms = Memory.GetStream(Memory.Start, used, true);
					CopySome(br.BaseStream, ms, (long)used);
					Used = used;
				}
				else
				{
					var hash = br.ReadBytes(_hash.Length);
					if (!hash.SequenceEqual(_hash))
					{
						throw new InvalidOperationException(string.Format("Hash did not match for heap {0}.  Is this the same rom?"));
					}
				}
			}

			public void Dispose()
			{
				if (Memory != null)
				{
					Memory.Dispose();
					Memory = null;
				}
			}
		}

		#endregion
	}
}

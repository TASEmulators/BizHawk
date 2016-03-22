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

namespace BizHawk.Emulation.Cores
{
	public sealed class ElfRunner : IImportResolver, IDisposable
	{
		// TODO: a lot of things only work with our elves and aren't fully generalized

		private ELF<long> _elf;
		private byte[] _elfhash;

		private MemoryBlock _base;
		private MemoryBlock _heap;
		private ulong _heapused;

		private long _loadoffset;
		private Dictionary<string, SymbolEntry<long>> _symdict;
		private List<SymbolEntry<long>> _symlist;

		public ElfRunner(string filename, long heapsize)
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
			}
			else
			{
				_base = new MemoryBlock((ulong)orig_start, (ulong)(orig_end - orig_start));
				_loadoffset = 0;
			}

			try
			{
				_base.Set(_base.Start, _base.Size, MemoryBlock.Protection.RW);

				foreach (var seg in loadsegs)
				{
					var data = seg.GetContents();
					Marshal.Copy(data, 0, Z.SS(seg.Address + _loadoffset), data.Length);
				}
				RegisterSymbols();
				ProcessRelocations();


				_base.Set(_base.Start, _base.Size, MemoryBlock.Protection.R);

				foreach (var sec in _elf.Sections.Where(s => (s.Flags & SectionFlags.Allocatable) != 0))
				{
					if ((sec.Flags & SectionFlags.Executable) != 0)
						_base.Set((ulong)(sec.LoadAddress + _loadoffset), (ulong)sec.Size, MemoryBlock.Protection.RX);
					else if ((sec.Flags & SectionFlags.Writable) != 0)
						_base.Set((ulong)(sec.LoadAddress + _loadoffset), (ulong)sec.Size, MemoryBlock.Protection.RW);
				}

				if (HasRelocations())
				{
					_heap = new MemoryBlock((ulong)heapsize);
				}
				else
				{
					// for nonrelocatable, create a canonical heap origin starting at the next 16MB
					ulong heapstart = ((_base.End - 1) | 0xffffff) + 1;
					_heap = new MemoryBlock(heapstart, (ulong)heapsize);
				}
				_heapused = 0;

				//FixupGOT();
				ConnectAllClibPatches();
				Console.WriteLine("Loaded {0}@{1:X16}", filename, _base.Start);
				foreach (var sec in _elf.Sections.Where(s => s.LoadAddress != 0))
				{
					Console.WriteLine("  {0}@{1:X16}, size {2}", sec.Name.PadLeft(20), sec.LoadAddress + _loadoffset, sec.Size.ToString().PadLeft(12));
				}
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
				if (_heap != null)
				{
					_heap.Dispose();
					_heap = null;
				}
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

		[CLibPatch("_ecl_trap")]
		private void Trap()
		{
			throw new InvalidOperationException("Waterbox code trapped!");
		}

		[CLibPatch("_ecl_sbrk")]
		private IntPtr Sbrk(UIntPtr n)
		{
			ulong newused = _heapused + (ulong)n;
			if (newused > _heap.Size)
			{
				throw new InvalidOperationException("Waterbox sbrk will fail!");
			}
			Console.WriteLine("Expanding waterbox heap from {0} to {1} bytes", _heapused, newused);
			var ret = _heap.Start + _heapused;
			_heap.Set(ret, newused - _heapused, MemoryBlock.Protection.RW);
			_heapused = newused;
			return Z.US(ret);
		}

		[CLibPatch("_ecl_debug_puts")]
		private void DebugPuts(string s)
		{
			Console.WriteLine("Waterbox debug puts: {0}", s);
		}

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
			bw.Write(MAGIC);
			bw.Write(_elfhash);
			bw.Write(_loadoffset);
			foreach (var sec in _elf.Sections.Where(s => (s.Flags & SectionFlags.Writable) != 0))
			{
				var ms = _base.GetStream((ulong)(sec.LoadAddress + _loadoffset), (ulong)sec.Size, false);
				bw.Write(sec.Size);
				ms.CopyTo(bw.BaseStream);
			}

			{
				var ms = _heap.GetStream(_heap.Start, _heapused, false);
				bw.Write(_heapused);
				ms.CopyTo(bw.BaseStream);
			}
		}

		public void LoadStateBinary(BinaryReader br)
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

			{
				var len = br.ReadInt64();
				if (len > (long)_heap.Size)
					throw new InvalidOperationException("Heap size mismatch");
				var ms = _heap.GetStream(_heap.Start, (ulong)len, true);
				CopySome(br.BaseStream, ms, len);
				_heapused = (ulong)len;
			}
		}

		#endregion

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
	}
}

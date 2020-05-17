using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using BizHawk.BizInvoke;
using BizHawk.Common;
using BizHawk.Emulation.Common;
using ELFSharp.ELF;
using ELFSharp.ELF.Sections;
using ELFSharp.ELF.Segments;

namespace BizHawk.Emulation.Cores.Waterbox
{
	public class ElfLoader : IImportResolver, IDisposable, IBinaryStateable
	{
		private readonly ELF<ulong> _elf;
		private readonly byte[] _elfHash;

		private readonly List<SymbolEntry<ulong>> _allSymbols;
		private readonly Dictionary<string, SymbolEntry<ulong>> _visibleSymbols;
		private readonly Dictionary<string, Section<ulong>> _sectionsByName;
		private readonly List<SymbolEntry<ulong>> _importSymbols;

		private readonly Section<ulong> _imports;
		private readonly Section<ulong> _sealed;
		private readonly Section<ulong> _invisible;

		private readonly List<Section<ulong>> _savedSections;

		private readonly bool _skipCoreConsistencyCheck;
		private readonly bool _skipMemoryConsistencyCheck;

		private bool _everythingSealed;

		public MemoryBlockBase Memory { get; private set; }

		public string ModuleName { get; }

		public ElfLoader(string moduleName, byte[] fileData, ulong assumedStart, bool skipCoreConsistencyCheck, bool skipMemoryConsistencyCheck)
		{
			ModuleName = moduleName;
			_skipCoreConsistencyCheck = skipCoreConsistencyCheck;
			_skipMemoryConsistencyCheck = skipMemoryConsistencyCheck;

			_elfHash = WaterboxUtils.Hash(fileData);
			_elf = ELFReader.Load<ulong>(new MemoryStream(fileData, false), true);

			var loadsegs = _elf.Segments.Where(s => s.Type == SegmentType.Load);
			var start = loadsegs.Min(s => s.Address);
			start = WaterboxUtils.AlignDown(start);
			var end = loadsegs.Max(s => s.Address + s.Size);
			end = WaterboxUtils.AlignUp(end);
			var size = end - start;

			if (start != assumedStart)
				throw new InvalidOperationException($"{nameof(assumedStart)} did not match actual origin in elf file");
			
			if (_elf.Sections.Any(s => s.Name.StartsWith(".rel")))
				throw new InvalidOperationException("Elf has relocations!");

			_allSymbols = ((ISymbolTable)_elf.GetSection(".symtab"))
				.Entries
				.Cast<SymbolEntry<ulong>>()
				.ToList();

			_sectionsByName = _elf.Sections
				.ToDictionary(s => s.Name);
			
			_sectionsByName.TryGetValue(".wbxsyscall", out _imports);
			_sectionsByName.TryGetValue(".sealed", out _sealed);
			_sectionsByName.TryGetValue(".invis", out _invisible);

			_savedSections = _elf.Sections
				.Where(s => (s.Flags & SectionFlags.Allocatable) != 0 && (s.Flags & SectionFlags.Writable) != 0)
				.Where(s => !IsSpecialReadonlySection(s) && s != _invisible)
				.OrderBy(s => s.LoadAddress)
				.ToList();

			_visibleSymbols = _allSymbols
				.Where(s => s.Binding == SymbolBinding.Global && s.Visibility == SymbolVisibility.Default)
				.ToDictionary(s => s.Name);
			
			_importSymbols = _visibleSymbols.Values
				.Where(s => s.PointedSection == _imports)
				.ToList();


			Memory = MemoryBlock.CallPlatformCtor(start, size);
			Memory.Activate();
			Memory.Protect(Memory.Start, Memory.Size, MemoryBlockBase.Protection.RW);

			foreach (var seg in loadsegs)
			{
				var data = seg.GetFileContents();
				Marshal.Copy(data, 0, Z.US(seg.Address), Math.Min((int)seg.Size, (int)seg.FileSize));
			}

			PrintSections();
			PrintGdbData();
			PrintTopSavableSymbols();
			Protect();
		}

		private void PrintGdbData()
		{
			Console.WriteLine("GDB Symbol Load:");
			Console.WriteLine($"  add-sym {ModuleName}");
		}

		private void PrintSections()
		{
			Console.WriteLine($"Mounted `{ModuleName}` @{Memory.Start:x16}");
			foreach (var s in _elf.Sections.OrderBy(s => s.LoadAddress))
			{
				Console.WriteLine("  @{0:x16} {1}{2}{3} `{4}` {5} bytes",
					s.LoadAddress,
					(s.Flags & SectionFlags.Allocatable) != 0 ? "R" : " ",
					(s.Flags & SectionFlags.Writable) != 0 ? "W" : " ",
					(s.Flags & SectionFlags.Executable) != 0 ? "X" : " ",
					s.Name,
					s.Size);
			}
		}

		private void PrintTopSavableSymbols()
		{
			Console.WriteLine("Top savestate symbols:");
			var tops = _allSymbols
				.Where(s => s.PointedSection != null && (s.PointedSection.Flags & SectionFlags.Writable) != 0)
				.OrderByDescending(s => s.Size)
				.Take(30)
				.Select(s => $"  {s.Name} size {s.Size}");

			foreach (var text in tops)
			{
				Console.WriteLine(text);
			}
		}

		/// <summary>
		/// Returns true if section is readonly after init
		/// </summary>
		private bool IsSpecialReadonlySection(Section<ulong> sec)
		{
			return sec.Name.Contains(".rel.ro")
				|| sec.Name.StartsWith(".got")
				|| sec == _imports
				|| sec == _sealed;
		}

		/// <summary>
		/// Set normal (post-seal) memory protections
		/// </summary>
		private void Protect()
		{
			Memory.Protect(Memory.Start, Memory.Size, MemoryBlockBase.Protection.R);
			foreach (var sec in _elf.Sections.Where(s => (s.Flags & SectionFlags.Allocatable) != 0 && !IsSpecialReadonlySection(s)))
			{
				if ((sec.Flags & SectionFlags.Executable) != 0)
					Memory.Protect(sec.LoadAddress, sec.Size, MemoryBlockBase.Protection.RX);
				else if ((sec.Flags & SectionFlags.Writable) != 0)
					Memory.Protect(sec.LoadAddress, sec.Size, MemoryBlockBase.Protection.RW);
			}
		}

		// connect all of the .wbxsyscall stuff
		public void ConnectSyscalls(IImportResolver syscalls)
		{
			Memory.Protect(Memory.Start, Memory.Size, MemoryBlockBase.Protection.RW);

			var tmp = new IntPtr[1];

			foreach (var s in _importSymbols)
			{
				if (s.Size == 8)
				{
					var p = syscalls.GetProcAddrOrThrow(s.Name);
					tmp[0] = p;
					Marshal.Copy(tmp, 0, Z.US(s.Value), 1);
				}
				else
				{
					if ((s.Size & 7) != 0)
						throw new InvalidOperationException($"Symbol {s.Name} has unexpected size");
					var count = (int)(s.Size >> 3);
					for (var i = 0; i < count; i++)
					{
						var p = syscalls.GetProcAddrOrThrow($"{s.Name}[{i}]");
						tmp[0] = p;
						Marshal.Copy(tmp, 0, Z.US(s.Value + (ulong)(i * 8)), 1);
					}
				}
			}

			Protect();
		}

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void ElfEntryDelegate();

		public void RunNativeInit()
		{
			CallingConventionAdapters.Waterbox.GetDelegateForFunctionPointer<ElfEntryDelegate>(Z.US(_elf.EntryPoint))();
		}

		public void SealImportsAndTakeXorSnapshot()
		{
			if (_everythingSealed)
				throw new InvalidOperationException($"{nameof(ElfLoader)} already sealed!");

			// save import values, then zero them all (for hash purposes), then take our snapshot, then load them again,
			// then set the .idata area to read only
			byte[] impData = null;
			if (_imports != null)
			{
				impData = new byte[_imports.Size];
				Marshal.Copy(Z.US(_imports.LoadAddress), impData, 0, (int)_imports.Size);
				WaterboxUtils.ZeroMemory(Z.US(_imports.LoadAddress), (long)_imports.Size);
			}
			else
			{
				throw new Exception("Call natt (_imports??)");
			}
			byte[] invData = null;
			if (_invisible != null)
			{
				invData = new byte[_invisible.Size];
				Marshal.Copy(Z.US(_invisible.LoadAddress), invData, 0, (int)_invisible.Size);
				WaterboxUtils.ZeroMemory(Z.US(_invisible.LoadAddress), (long)_invisible.Size);
			}
			Memory.SaveXorSnapshot();
			if (_imports != null)
			{
				Marshal.Copy(impData, 0, Z.US(_imports.LoadAddress), (int)_imports.Size);
			}
			if (_invisible != null)
			{
				Marshal.Copy(invData, 0, Z.US(_invisible.LoadAddress), (int)_invisible.Size);
			}

			Protect();
			_everythingSealed = true;
		}

		private bool _disposed = false;

		public void Dispose()
		{
			if (!_disposed)
			{
				Memory.Dispose();
				Memory = null;
				_disposed = true;
			}
		}

		public IntPtr GetProcAddrOrZero(string entryPoint)
		{
			if (_visibleSymbols.TryGetValue(entryPoint, out var sym))
			{
				return Z.US(sym.Value);
			}
			else
			{
				return IntPtr.Zero;
			}
		}

		public IntPtr GetProcAddrOrThrow(string entryPoint)
		{
			if (_visibleSymbols.TryGetValue(entryPoint, out var sym))
			{
				return Z.US(sym.Value);
			}
			else
			{
				throw new InvalidOperationException($"Couldn't find {nameof(entryPoint)} {entryPoint} in {ModuleName}");
			}
		}

		const ulong MAGIC = 0x6018ab7df99310ca;

		public void SaveStateBinary(BinaryWriter bw)
		{
			if (!_everythingSealed)
				throw new InvalidOperationException(".idata sections must be closed before saving state");

			bw.Write(MAGIC);
			bw.Write(_elfHash);
			bw.Write(Memory.XorHash);

			foreach (var s in _savedSections)
			{
				var ms = Memory.GetXorStream(s.LoadAddress, s.Size, false);
				bw.Write(s.Size);
				ms.CopyTo(bw.BaseStream);
			}
		}

		public void LoadStateBinary(BinaryReader br)
		{
			if (!_everythingSealed)
				// operations happening in the wrong order.  probable cause: internal logic error.  make sure frontend calls Seal
				throw new InvalidOperationException(".idata sections must be closed before loading state");

			if (br.ReadUInt64() != MAGIC)
				// file id is missing.  probable cause:  garbage savestate
				throw new InvalidOperationException("Savestate corrupted!");

			var elfHash = br.ReadBytes(_elfHash.Length);
			if (_skipCoreConsistencyCheck)
			{
				throw new InvalidOperationException("We decided that the core consistency check should always run");
			}
			else
			{
				if (!elfHash.SequenceEqual(_elfHash))
					// the .dll file that is loaded now has a different hash than the .dll that created the savestate
					throw new InvalidOperationException("Core consistency check failed.  Is this a savestate from a different version?");
			}

			var xorHash = br.ReadBytes(Memory.XorHash.Length);
			if (!_skipMemoryConsistencyCheck)
			{
				if (!xorHash.SequenceEqual(Memory.XorHash))
					// the post-Seal memory state is different. probable cause:  different rom or different version of rom,
					// different syncsettings
					throw new InvalidOperationException("Memory consistency check failed.  Is this savestate from different SyncSettings?");
			}

			Memory.Protect(Memory.Start, Memory.Size, MemoryBlockBase.Protection.RW);

			foreach (var s in _savedSections)
			{
				if (br.ReadUInt64() != s.Size)
					throw new InvalidOperationException("Unexpected section size for " + s.Name);

				var ms = Memory.GetXorStream(s.LoadAddress, s.Size, true);
				WaterboxUtils.CopySome(br.BaseStream, ms, (long)s.Size);
			}

			Protect();
		}
	}
}

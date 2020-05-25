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

		private readonly bool _skipCoreConsistencyCheck;
		private readonly bool _skipMemoryConsistencyCheck;

		private bool _everythingSealed;

		public MemoryBlock Memory { get; private set; }

		public string ModuleName { get; }

		/// <summary>
		/// Where writable data begins
		/// </summary>
		private ulong _writeStart;
		/// <summary>
		/// Where writiable data begins after seal
		/// </summary>
		private ulong _postSealWriteStart;
		/// <summary>
		/// Where the saveable program data begins
		/// </summary>
		private ulong _saveStart;
		private ulong _execEnd;

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
			if (_imports == null)
			{
				// Likely cause:  This is a valid elf file, but it was not compiled by our toolchain at all
				throw new InvalidOperationException("Missing .wbxsyscall section!");
			}
			_sectionsByName.TryGetValue(".sealed", out _sealed);
			_sectionsByName.TryGetValue(".invis", out _invisible);

			_visibleSymbols = _allSymbols
				.Where(s => s.Binding == SymbolBinding.Global && s.Visibility == SymbolVisibility.Default)
				.ToDictionary(s => s.Name);

			_importSymbols = _allSymbols
				// TODO: No matter what attributes I provide, I seem to end up with Local and/or Hidden symbols in
				// .wbxsyscall a lot of the time on heavily optimized release builds.
				// Fortunately, there's nothing else in .wbxsyscall so we can just not filter at all.
				.Where(s => s.PointedSection == _imports)
				.ToList();

			Memory = MemoryBlock.Create(start, size);
			Memory.Activate();
			Memory.Protect(Memory.Start, Memory.Size, MemoryBlock.Protection.RW);

			foreach (var seg in loadsegs)
			{
				var data = seg.GetFileContents();
				Marshal.Copy(data, 0, Z.US(seg.Address), Math.Min((int)seg.Size, (int)seg.FileSize));
			}

			{
				// Compute RW boundaries

				var allocated = _elf.Sections
					.Where(s => (s.Flags & SectionFlags.Allocatable) != 0);
				var writable = allocated
					.Where(s => (s.Flags & SectionFlags.Writable) != 0);
				var postSealWritable = writable
					.Where(s => !IsSpecialReadonlySection(s));
				var saveable = postSealWritable
					.Where(s => s != _invisible);
				var executable = allocated
					.Where(s => (s.Flags & SectionFlags.Executable) != 0);

				_writeStart = WaterboxUtils.AlignDown(writable.Min(s => s.LoadAddress));
				_postSealWriteStart = WaterboxUtils.AlignDown(postSealWritable.Min(s => s.LoadAddress));
				_saveStart = WaterboxUtils.AlignDown(saveable.Min(s => s.LoadAddress));
				_execEnd = WaterboxUtils.AlignUp(executable.Max(s => s.LoadAddress + s.Size));

				// validate; this may require linkscript cooperation
				// due to the segment limitations, the only thing we'd expect to catch is a custom eventually readonly section
				// in the wrong place (because the linkscript doesn't know "eventually readonly")
				if (_execEnd > _writeStart)
					throw new InvalidOperationException($"ElfLoader: Executable data to {_execEnd:X16} overlaps writable data from {_writeStart}");
				
				var actuallySaved = allocated.Where(a => a.LoadAddress + a.Size > _saveStart);
				var oopsSaved = actuallySaved.Except(saveable);
				foreach (var s in oopsSaved)
				{
					Console.WriteLine($"ElfLoader: Section {s.Name} will be saved, but that was not expected");
				}
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
				Console.WriteLine("  @{0:x16} {1}{2}{3}{4} `{5}` {6} bytes",
					s.LoadAddress,
					(s.Flags & SectionFlags.Allocatable) != 0 ? "R" : " ",
					(s.Flags & SectionFlags.Writable) != 0 ? "W" : " ",
					(s.Flags & SectionFlags.Executable) != 0 ? "X" : " ",
					s.LoadAddress + s.Size > _saveStart ? "V" : " ",
					s.Name,
					s.Size);
			}
		}

		private void PrintTopSavableSymbols()
		{
			var tops = _allSymbols
				.Where(s => s.Value + s.Size > _saveStart)
				.OrderByDescending(s => s.Size)
				.Where(s => s.Size >= 20 * 1024)
				.Take(30)
				.Select(s => $"  {s.Name} {s.Size / 1024}kiB")
				.ToList();

			if (tops.Count > 0)
			{
				Console.WriteLine("Top savestate symbols:");
				foreach (var text in tops)
				{
					Console.WriteLine(text);
				}
			}
		}

		/// <summary>
		/// Returns true if section is readonly after init
		/// </summary>
		private bool IsSpecialReadonlySection(Section<ulong> sec)
		{
			// TODO: I don't think there are any more relro sections, right?
			return sec.Name.Contains(".rel.ro")
				|| sec.Name.StartsWith(".got")
				|| sec.Name == ".init_array"
				|| sec.Name == ".fini_array"
				|| sec.Name == ".tbss"
				|| sec == _imports
				|| sec == _sealed;
		}

		/// <summary>
		/// Set normal (post-seal) memory protections
		/// </summary>
		private void Protect()
		{
			var writeStart = _everythingSealed ? _postSealWriteStart : _writeStart;

			Memory.Protect(Memory.Start, _execEnd - Memory.Start, MemoryBlock.Protection.RX);
			Memory.Protect(_execEnd, writeStart - _execEnd, MemoryBlock.Protection.R);
			Memory.Protect(writeStart, Memory.EndExclusive - writeStart, MemoryBlock.Protection.RW);
		}

		// connect all of the .wbxsyscall stuff
		public void ConnectSyscalls(IImportResolver syscalls)
		{
			Memory.Protect(Memory.Start, Memory.Size, MemoryBlock.Protection.RW);

			var tmp = new IntPtr[1];
			var ptrSize = (ulong)IntPtr.Size;

			foreach (var s in _importSymbols)
			{
				if (s.Size == ptrSize)
				{
					var p = syscalls.GetProcAddrOrThrow(s.Name);
					tmp[0] = p;
					Marshal.Copy(tmp, 0, Z.US(s.Value), 1);
				}
				else
				{
					if (s.Size % ptrSize != 0)
					{
						// They're supposed to be arrays of pointers, so uhhh yeah?
						throw new InvalidOperationException($"Symbol {s.Name} has unexpected size");
					}
					var count = (int)(s.Size / ptrSize);
					for (var i = 0; i < count; i++)
					{
						var p = syscalls.GetProcAddrOrThrow($"{s.Name}[{i}]");
						tmp[0] = p;
						Marshal.Copy(tmp, 0, Z.US(s.Value + ((ulong)i * ptrSize)), 1);
					}
				}
			}

			Protect();
		}

		public void RunNativeInit()
		{
			CallingConventionAdapters.Waterbox.GetDelegateForFunctionPointer<Action>(Z.US(_elf.EntryPoint))();
		}

		public void SealImportsAndTakeXorSnapshot()
		{
			if (_everythingSealed)
				throw new InvalidOperationException($"{nameof(ElfLoader)} already sealed!");

			// save import values, then zero them all (for hash purposes), then take our snapshot, then load them again,
			// then set the .wbxsyscall area to read only
			byte[] impData = null;
			impData = new byte[_imports.Size];
			Marshal.Copy(Z.US(_imports.LoadAddress), impData, 0, (int)_imports.Size);
			WaterboxUtils.ZeroMemory(Z.US(_imports.LoadAddress), (long)_imports.Size);

			byte[] invData = null;
			if (_invisible != null)
			{
				invData = new byte[_invisible.Size];
				Marshal.Copy(Z.US(_invisible.LoadAddress), invData, 0, (int)_invisible.Size);
				WaterboxUtils.ZeroMemory(Z.US(_invisible.LoadAddress), (long)_invisible.Size);
			}

			Memory.SaveXorSnapshot();

			Marshal.Copy(impData, 0, Z.US(_imports.LoadAddress), (int)_imports.Size);

			if (_invisible != null)
			{
				Marshal.Copy(invData, 0, Z.US(_invisible.LoadAddress), (int)_invisible.Size);
			}

			_everythingSealed = true;
			Protect();
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
				throw new InvalidOperationException(".wbxsyscall section must be closed before saving state");

			bw.Write(MAGIC);
			bw.Write(_elfHash);
			bw.Write(Memory.XorHash);

			var len = Memory.EndExclusive - _saveStart;
			var ms = Memory.GetXorStream(_saveStart, len, false);
			bw.Write(len);
			ms.CopyTo(bw.BaseStream);
		}

		public void LoadStateBinary(BinaryReader br)
		{
			if (!_everythingSealed)
				// operations happening in the wrong order.  probable cause: internal logic error.  make sure frontend calls Seal
				throw new InvalidOperationException(".wbxsyscall section must be closed before loading state");

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

			var len = Memory.EndExclusive - _saveStart;
			if (br.ReadUInt64() != len)
				throw new InvalidOperationException("Unexpected saved length");
			var ms = Memory.GetXorStream(_saveStart, len, true);
			WaterboxUtils.CopySome(br.BaseStream, ms, (long)len);
		}
	}
}

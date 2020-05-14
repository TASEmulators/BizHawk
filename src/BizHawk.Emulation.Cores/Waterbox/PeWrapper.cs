using BizHawk.Common;
using BizHawk.BizInvoke;
using BizHawk.Emulation.Common;
using PeNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace BizHawk.Emulation.Cores.Waterbox
{
	/// <summary>
	/// represents one PE file.  used in PeRunner
	/// </summary>
	internal class PeWrapper : IImportResolver, IBinaryStateable, IDisposable
	{
		public Dictionary<int, IntPtr> ExportsByOrdinal { get; } = new Dictionary<int, IntPtr>();
		/// <summary>
		/// ordinal only exports will not show up in this list!
		/// </summary>
		public Dictionary<string, IntPtr> ExportsByName { get; } = new Dictionary<string, IntPtr>();

		public Dictionary<string, Dictionary<string, IntPtr>> ImportsByModule { get; } =
			new Dictionary<string, Dictionary<string, IntPtr>>();

		private class Section
		{
			public string Name { get; set; }
			public ulong Start { get; set; }
			public ulong Size { get; set; }
			public ulong SavedSize { get; set; }
			public bool W { get; set; }
			public bool R { get; set; }
			public bool X { get; set; }
			public MemoryBlockBase.Protection Prot { get; set; }
			public ulong DiskStart { get; set; }
			public ulong DiskSize { get; set; }
		}

		private readonly Dictionary<string, Section> _sectionsByName = new Dictionary<string, Section>();
		private readonly List<Section> _sections = new List<Section>();
		private Section _imports;
		private Section _sealed;
		private Section _invisible;

		public string ModuleName { get; }

		private readonly PeFile _pe;
		private readonly byte[] _fileHash;

		private bool _skipCoreConsistencyCheck;
		private bool _skipMemoryConsistencyCheck;

		public ulong Size { get; }
		public ulong Start { get; }

		public long LoadOffset { get; }

		public MemoryBlockBase Memory { get; private set; }

		public IntPtr EntryPoint { get; }

		/// <summary>
		/// for midipix-built PEs, pointer to the constructors to run during init
		/// </summary>
		public IntPtr CtorList { get; }
		/// <summary>
		/// for midipix-build PEs, pointer to the destructors to run during fini
		/// </summary>
		public IntPtr DtorList { get; }

		// true if the seal process has completed, including .idata and .sealed set to readonly,
		// xorstate taken
		private bool _everythingSealed = false;

		/*[UnmanagedFunctionPointer(CallingConvention.Winapi)]
		private delegate bool DllEntry(IntPtr instance, int reason, IntPtr reserved);
		[UnmanagedFunctionPointer(CallingConvention.Winapi)]
		private delegate void ExeEntry();*/
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void GlobalCtor();

		/*public bool RunDllEntry()
		{
			var entryThunk = (DllEntry)CallingConventionAdapters.Waterbox.GetDelegateForFunctionPointer(EntryPoint, typeof(DllEntry));
			return entryThunk(Z.US(Start), 1, IntPtr.Zero); // DLL_PROCESS_ATTACH
		}
		public void RunExeEntry()
		{
			var entryThunk = (ExeEntry)CallingConventionAdapters.Waterbox.GetDelegateForFunctionPointer(EntryPoint, typeof(ExeEntry));
			entryThunk();
		}*/
		public unsafe void RunGlobalCtors()
		{
			int did = 0;
			if (CtorList != IntPtr.Zero)
			{
				IntPtr* p = (IntPtr*)CtorList;
				IntPtr f;
				while ((f = *++p) != IntPtr.Zero) // skip 0th dummy pointer
				{
					var ctorThunk = (GlobalCtor)CallingConventionAdapters.Waterbox.GetDelegateForFunctionPointer(f, typeof(GlobalCtor));
					//Console.WriteLine(f);
					//System.Diagnostics.Debugger.Break();
					ctorThunk();
					did++;
				}
			}

			if (did > 0)
			{
				Console.WriteLine($"Did {did} global ctors for {ModuleName}");
			}
			else
			{
				Console.WriteLine($"Warn: no global ctors for {ModuleName}; possibly no C++?");
			}
		}

		public PeWrapper(string moduleName, byte[] fileData, ulong destAddress, bool skipCoreConsistencyCheck, bool skipMemoryConsistencyCheck)
		{
			ModuleName = moduleName;
			_pe = new PeFile(fileData);
			Size = _pe.ImageNtHeaders.OptionalHeader.SizeOfImage;
			Start = destAddress;
			this._skipCoreConsistencyCheck = skipCoreConsistencyCheck;
			this._skipMemoryConsistencyCheck = skipMemoryConsistencyCheck;

			if (Size < _pe.ImageSectionHeaders.Max(s => (ulong)s.VirtualSize + s.VirtualAddress))
			{
				throw new InvalidOperationException("Image not Big Enough");
			}

			_fileHash = WaterboxUtils.Hash(fileData);

			foreach (var s in _pe.ImageSectionHeaders)
			{
				ulong start = Start + s.VirtualAddress;
				ulong length = s.VirtualSize;

				MemoryBlockBase.Protection prot;
				var r = (s.Characteristics & (uint)Constants.SectionFlags.IMAGE_SCN_MEM_READ) != 0;
				var w = (s.Characteristics & (uint)Constants.SectionFlags.IMAGE_SCN_MEM_WRITE) != 0;
				var x = (s.Characteristics & (uint)Constants.SectionFlags.IMAGE_SCN_MEM_EXECUTE) != 0;
				if (w && x)
				{
					throw new InvalidOperationException("Write and Execute not allowed");
				}

				prot = x ? MemoryBlockBase.Protection.RX : w ? MemoryBlockBase.Protection.RW : MemoryBlockBase.Protection.R;

				var section = new Section
				{
					// chop off possible null padding from name
					Name = Encoding.ASCII.GetString(s.Name, 0,
						(s.Name.Select((v, i) => new { v, i }).FirstOrDefault(a => a.v == 0) ?? new { v = (byte)0, i = s.Name.Length }).i),
					Start = start,
					Size = length,
					SavedSize = WaterboxUtils.AlignUp(length),
					R = r,
					W = w,
					X = x,
					Prot = prot,
					DiskStart = s.PointerToRawData,
					DiskSize = s.SizeOfRawData
				};

				_sections.Add(section);
				_sectionsByName.Add(section.Name, section);
			}
			_sectionsByName.TryGetValue(".idata", out _imports);
			_sectionsByName.TryGetValue(".sealed", out _sealed);
			_sectionsByName.TryGetValue(".invis", out _invisible);

			// OK, NOW MOUNT

			LoadOffset = (long)Start - (long)_pe.ImageNtHeaders.OptionalHeader.ImageBase;
			Memory = MemoryBlockBase.CallPlatformCtor(Start, Size);
			Memory.Activate();
			Memory.Protect(Start, Size, MemoryBlockBase.Protection.RW);

			// copy headers
			Marshal.Copy(fileData, 0, Z.US(Start), (int)_pe.ImageNtHeaders.OptionalHeader.SizeOfHeaders);

			// copy sections
			foreach (var s in _sections)
			{
				ulong datalength = Math.Min(s.Size, s.DiskSize);
				Marshal.Copy(fileData, (int)s.DiskStart, Z.US(s.Start), (int)datalength);
				WaterboxUtils.ZeroMemory(Z.US(s.Start + datalength), (long)(s.SavedSize - datalength));
			}

			// apply relocations
			var n32 = 0;
			var n64 = 0;
			foreach (var rel in _pe.ImageRelocationDirectory)
			{
				foreach (var to in rel.TypeOffsets)
				{
					ulong address = Start + rel.VirtualAddress + to.Offset;

					switch (to.Type)
					{
						// there are many other types of relocation specified,
						// but the only that are used is 0 (does nothing), 3 (32 bit standard), 10 (64 bit standard)

						case 3: // IMAGE_REL_BASED_HIGHLOW
							{
								byte[] tmp = new byte[4];
								Marshal.Copy(Z.US(address), tmp, 0, 4);
								uint val = BitConverter.ToUInt32(tmp, 0);
								tmp = BitConverter.GetBytes((uint)(val + LoadOffset));
								Marshal.Copy(tmp, 0, Z.US(address), 4);
								n32++;
								break;
							}

						case 10: // IMAGE_REL_BASED_DIR64
							{
								byte[] tmp = new byte[8];
								Marshal.Copy(Z.US(address), tmp, 0, 8);
								long val = BitConverter.ToInt64(tmp, 0);
								tmp = BitConverter.GetBytes(val + LoadOffset);
								Marshal.Copy(tmp, 0, Z.US(address), 8);
								n64++;
								break;
							}
					}
				}
			}
			if (IntPtr.Size == 8 && n32 > 0)
			{
				// check mcmodel, etc
				throw new InvalidOperationException("32 bit relocations found in 64 bit dll!  This will fail.");
			}
			Console.WriteLine($"Processed {n32} 32 bit and {n64} 64 bit relocations");

			ProtectMemory();

			// publish exports
			EntryPoint = Z.US(Start + _pe.ImageNtHeaders.OptionalHeader.AddressOfEntryPoint);
			foreach (var export in _pe.ExportedFunctions)
			{
				if (export.Name != null)
					ExportsByName.Add(export.Name, Z.US(Start + export.Address));
				ExportsByOrdinal.Add(export.Ordinal, Z.US(Start + export.Address));
			}

			// collect information about imports
			// NB: Hints are not the same as Ordinals
			foreach (var import in _pe.ImportedFunctions)
			{
				if (!ImportsByModule.TryGetValue(import.DLL, out var module))
				{
					module = new Dictionary<string, IntPtr>();
					ImportsByModule.Add(import.DLL, module);
				}
				var dest = Start + import.Thunk;
				if (_imports == null || dest >= _imports.Start + _imports.Size || dest < _imports.Start)
					throw new InvalidOperationException("Import record outside of .idata!");

				module.Add(import.Name, Z.US(dest));
			}

			if (_sectionsByName.TryGetValue(".midipix", out Section midipix))
			{
				var dataOffset = midipix.DiskStart;
				CtorList = Z.SS(BitConverter.ToInt64(fileData, (int)(dataOffset + 0x30)) + LoadOffset);
				DtorList = Z.SS(BitConverter.ToInt64(fileData, (int)(dataOffset + 0x38)) + LoadOffset);
			}

			Console.WriteLine($"Mounted `{ModuleName}` @{Start:x16}");
			foreach (var s in _sections.OrderBy(s => s.Start))
			{
				Console.WriteLine("  @{0:x16} {1}{2}{3} `{4}` {5} bytes",
					s.Start,
					s.R ? "R" : " ",
					s.W ? "W" : " ",
					s.X ? "X" : " ",
					s.Name,
					s.Size);
			}
			Console.WriteLine("GDB Symbol Load:");
			var symload = $"add-sym {ModuleName} {_sectionsByName[".text"].Start}";
			if (_sectionsByName.ContainsKey(".data"))
				symload += $" -s .data {_sectionsByName[".data"].Start}";
			if (_sectionsByName.ContainsKey(".bss"))
				symload += $" -s .bss {_sectionsByName[".bss"].Start}";
			Console.WriteLine(symload);
		}

		/// <summary>
		/// set memory protections.
		/// </summary>
		private void ProtectMemory()
		{
			Memory.Protect(Memory.Start, Memory.Size, MemoryBlockBase.Protection.R);

			foreach (var s in _sections)
			{
				Memory.Protect(s.Start, s.Size, s.Prot);
			}
		}

		public IntPtr? GetProcAddrOrNull(string entryPoint) => ExportsByName.TryGetValue(entryPoint, out var ret) ? ret : (IntPtr?) null;

		public IntPtr GetProcAddrOrThrow(string entryPoint) => GetProcAddrOrNull(entryPoint) ?? throw new InvalidOperationException($"could not find {entryPoint} in exports");

		public void ConnectImports(string moduleName, IImportResolver module)
		{
			// this is called once internally when bootstrapping, and externally
			// when we need to restore a savestate from another run.  so imports might or might not be sealed

			if (_everythingSealed && _imports != null)
				Memory.Protect(_imports.Start, _imports.Size, MemoryBlockBase.Protection.RW);

			if (ImportsByModule.TryGetValue(moduleName, out var imports))
			{
				foreach (var kvp in imports)
				{
					var valueArray = new IntPtr[] { module.GetProcAddrOrThrow(kvp.Key) };
					Marshal.Copy(valueArray, 0, kvp.Value, 1);
				}
			}

			if (_everythingSealed && _imports != null)
				Memory.Protect(_imports.Start, _imports.Size, _imports.Prot);
		}

		public void SealImportsAndTakeXorSnapshot()
		{
			if (_everythingSealed)
				throw new InvalidOperationException($"{nameof(PeWrapper)} already sealed!");

			// save import values, then zero them all (for hash purposes), then take our snapshot, then load them again,
			// then set the .idata area to read only
			byte[] impData = null;
			if (_imports != null)
			{
				impData = new byte[_imports.Size];
				Marshal.Copy(Z.US(_imports.Start), impData, 0, (int)_imports.Size);
				WaterboxUtils.ZeroMemory(Z.US(_imports.Start), (long)_imports.Size);
			}
			byte[] invData = null;
			if (_invisible != null)
			{
				invData = new byte[_invisible.Size];
				Marshal.Copy(Z.US(_invisible.Start), invData, 0, (int)_invisible.Size);
				WaterboxUtils.ZeroMemory(Z.US(_invisible.Start), (long)_invisible.Size);
			}
			Memory.SaveXorSnapshot();
			if (_imports != null)
			{
				Marshal.Copy(impData, 0, Z.US(_imports.Start), (int)_imports.Size);
				_imports.W = false;
				Memory.Protect(_imports.Start, _imports.Size, _imports.Prot);
			}
			if (_invisible != null)
			{
				Marshal.Copy(invData, 0, Z.US(_invisible.Start), (int)_invisible.Size);
			}
			if (_sealed != null)
			{
				_sealed.W = false;
				Memory.Protect(_sealed.Start, _sealed.Size, _sealed.Prot);
			}

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

		const ulong MAGIC = 0x420cccb1a2e17420;

		public void SaveStateBinary(BinaryWriter bw)
		{
			if (!_everythingSealed)
				throw new InvalidOperationException(".idata sections must be closed before saving state");

			bw.Write(MAGIC);
			bw.Write(_fileHash);
			bw.Write(Memory.XorHash);
			bw.Write(Start);

			foreach (var s in _sections)
			{
				if (!s.W || s == _invisible)
					continue;

				var ms = Memory.GetXorStream(s.Start, s.SavedSize, false);
				bw.Write(s.SavedSize);
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

			var fileHash = br.ReadBytes(_fileHash.Length);
			if (_skipCoreConsistencyCheck)
			{
				throw new InvalidOperationException("We decided that the core consistency check should always run");
			}
			else
			{
				if (!fileHash.SequenceEqual(_fileHash))
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

			if (br.ReadUInt64() != Start)
				// dll loaded somewhere else.  probable cause: internal logic error.
				// unlikely to get this far if the previous checks pssed
				throw new InvalidOperationException("Trickys elves moved on you!");

			Memory.Protect(Memory.AddressRange.Start, Memory.Size, MemoryBlockBase.Protection.RW);

			foreach (var s in _sections)
			{
				if (!s.W || s == _invisible)
					continue;

				if (br.ReadUInt64() != s.SavedSize)
					throw new InvalidOperationException("Unexpected section size for " + s.Name);

				var ms = Memory.GetXorStream(s.Start, s.SavedSize, true);
				WaterboxUtils.CopySome(br.BaseStream, ms, (long)s.SavedSize);
			}

			ProtectMemory();
		}
	}
}

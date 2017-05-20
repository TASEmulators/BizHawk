using PeNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace BizHawk.Emulation.Cores.Waterbox
{
	public class PeRunner
	{
		private static readonly ulong CanonicalStart = 0x0000036f00000000;

		public class PeWrapper
		{
			public Dictionary<int, IntPtr> ExportsByOrdinal { get; } = new Dictionary<int, IntPtr>();
			/// <summary>
			/// ordinal only exports will not show up in this list!
			/// </summary>
			public Dictionary<string, IntPtr> ExportsByName { get; } = new Dictionary<string, IntPtr>();

			public string ModuleName { get; }

			private readonly byte[] _fileData;
			private readonly PeFile _pe;

			public ulong Size { get; }
			public ulong Start { get; private set; }

			public long LoadOffset { get; private set; }

			public MemoryBlock Memory { get; private set; }

			public IntPtr EntryPoint { get; private set; }

			public PeWrapper(string moduleName, byte[] fileData)
			{
				ModuleName = moduleName;
				_fileData = fileData;
				_pe = new PeFile(fileData);
				Size = _pe.ImageNtHeaders.OptionalHeader.SizeOfImage;

				if (Size < _pe.ImageSectionHeaders.Max(s => (ulong)s.VirtualSize + s.VirtualAddress))
				{
					throw new InvalidOperationException("Image not Big Enough");
				}
			}

			/// <summary>
			/// set memory protections, finishing the Mount process
			/// </summary>
			public void FinishMount()
			{
				foreach (var s in _pe.ImageSectionHeaders)
				{
					ulong start = Start + s.VirtualAddress;
					ulong length = s.VirtualSize;

					MemoryBlock.Protection prot;
					var r = (s.Characteristics & (uint)Constants.SectionFlags.IMAGE_SCN_MEM_READ) != 0;
					var w = (s.Characteristics & (uint)Constants.SectionFlags.IMAGE_SCN_MEM_WRITE) != 0;
					var x = (s.Characteristics & (uint)Constants.SectionFlags.IMAGE_SCN_MEM_EXECUTE) != 0;
					if (w && x)
					{
						throw new InvalidOperationException("Write and Execute not allowed");
					}

					prot = x ? MemoryBlock.Protection.RX : w ? MemoryBlock.Protection.RW : MemoryBlock.Protection.R;

					Memory.Protect(start, length, prot);
				}
			}

			/// <summary>
			/// load the PE into memory
			/// </summary>
			/// <param name="org">start address</param>
			public void Mount(ulong org)
			{
				Start = org;
				LoadOffset = (long)Start - (long)_pe.ImageNtHeaders.OptionalHeader.ImageBase;
				Memory = new MemoryBlock(Start, Size);
				Memory.Activate();

				// copy headers
				{
					ulong length = _pe.ImageNtHeaders.OptionalHeader.SizeOfHeaders;
					Memory.Protect(Start, length, MemoryBlock.Protection.RW);
					Marshal.Copy(_fileData, 0, Z.US(Start), (int)length);
					Memory.Protect(Start, length, MemoryBlock.Protection.R);
				}

				// copy sections
				foreach (var s in _pe.ImageSectionHeaders)
				{
					ulong start = Start + s.VirtualAddress;
					ulong length = s.VirtualSize;

					Memory.Protect(start, length, MemoryBlock.Protection.RW);
					Marshal.Copy(_fileData, (int)s.PointerToRawData, Z.US(start), (int)s.SizeOfRawData);
					WaterboxUtils.ZeroMemory(Z.US(start + s.SizeOfRawData), (long)(length - s.SizeOfRawData));
				}

				// apply relocations
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
									break;
								}

							case 10: // IMAGE_REL_BASED_DIR64
								{
									byte[] tmp = new byte[8];
									Marshal.Copy(Z.US(address), tmp, 0, 8);
									long val = BitConverter.ToInt64(tmp, 0);
									tmp = BitConverter.GetBytes(val + LoadOffset);
									Marshal.Copy(tmp, 0, Z.US(address), 8);
									break;
								}
						}
					}
				}

				// publish exports
				EntryPoint = Z.US(Start + _pe.ImageNtHeaders.OptionalHeader.AddressOfEntryPoint);
				foreach (var export in _pe.ExportedFunctions)
				{
					if (export.Name != null)
						ExportsByName.Add(export.Name, Z.US(Start + export.Address));
					ExportsByOrdinal.Add(export.Ordinal, Z.US(Start + export.Address));
				}

				// collect information about imports
				// NB: Hints are not the same as Ordinals.  Off by 1??
				foreach (var import in _pe.ImportedFunctions)
				{
					
				}
			}
		}
	}
}

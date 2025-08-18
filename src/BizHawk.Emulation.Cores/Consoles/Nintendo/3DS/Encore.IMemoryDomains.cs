using System.Buffers.Binary;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.N3DS
{
	public partial class Encore
	{
		private IMemoryDomains _memoryDomains;

		private MemoryDomainIntPtr _fcram;
		private MemoryDomainIntPtr _vram;
		private MemoryDomainIntPtr _dspRam;
		private MemoryDomainIntPtr _n3dsExRam;

		private void InitMemoryDomains()
		{
			var domains = new List<MemoryDomain>()
			{
				(_fcram = new("FCRAM", MemoryDomain.Endian.Little, IntPtr.Zero, 0, true, 4)),
				(_vram = new("VRAM", MemoryDomain.Endian.Little, IntPtr.Zero, 0, true, 4)),
				(_dspRam = new("DSP RAM", MemoryDomain.Endian.Little, IntPtr.Zero, 0, true, 4)),
			};

			_n3dsExRam = new("N3DS Extra RAM", MemoryDomain.Endian.Little, IntPtr.Zero, 0, true, 4);
			if (_syncSettings.IsNew3ds)
			{
				domains.Add(_n3dsExRam);
			}

			// extra domain for virtual memory (important for dealing with pointers!)
			domains.Add(new EncoreMMU(_context));

			_memoryDomains = new MemoryDomainList(domains);
			_serviceProvider.Register(_memoryDomains);
			WireMemoryDomains();
		}

		private void WireMemoryDomains()
		{
			void WireDomain(LibEncore.MemoryRegion region, MemoryDomainIntPtr domain)
			{
				_core.Encore_GetMemoryRegion(_context, region, out var ptr, out var size);
				domain.Data = ptr;
				domain.SetSize(size);
			}

			WireDomain(LibEncore.MemoryRegion.FCRAM, _fcram);
			WireDomain(LibEncore.MemoryRegion.VRAM, _vram);
			WireDomain(LibEncore.MemoryRegion.DSP, _dspRam);
			WireDomain(LibEncore.MemoryRegion.N3DS, _n3dsExRam);
		}

		private class EncoreMMU : MemoryDomain
		{
			private const uint ENCORE_PAGE_SIZE = 0x1000;
			private const uint ENCORE_PAGE_MASK = ENCORE_PAGE_SIZE - 1;

			private readonly IntPtr _context;

			public EncoreMMU(IntPtr context)
			{
				Name = "System Bus";
				Size = 1L << 32;
				WordSize = 4;
				EndianType = Endian.Little;
				Writable = true;
				_context = context;
			}

			private Span<byte> GetPage(uint addr)
				=> Util.UnsafeSpanFromPointer(
					ptr: _core.Encore_GetPagePointer(_context, addr: addr),
					length: (int) (ENCORE_PAGE_SIZE - (addr & ENCORE_PAGE_MASK)));

			public override byte PeekByte(long addr)
			{
				var page = GetPage((uint)addr);
				return page.IsEmpty ? (byte)0 : page[0];
			}

			public override ushort PeekUshort(long addr, bool bigEndian)
			{
				// if we cross a page boundary, we need to read multiple pages
				if ((addr & ENCORE_PAGE_MASK) > ENCORE_PAGE_MASK - 1)
				{
					return base.PeekUshort(addr, bigEndian);
				}

				var page = GetPage((uint)addr);
				if (page.IsEmpty)
				{
					return 0;
				}

				return bigEndian
					? BinaryPrimitives.ReadUInt16BigEndian(page)
					: BinaryPrimitives.ReadUInt16LittleEndian(page);
			}

			public override uint PeekUint(long addr, bool bigEndian)
			{
				// if we cross a page boundary, we need to read multiple pages
				if ((addr & ENCORE_PAGE_MASK) > ENCORE_PAGE_MASK - 3)
				{
					return base.PeekUint(addr, bigEndian);
				}

				var page = GetPage((uint)addr);
				if (page.IsEmpty)
				{
					return 0;
				}

				return bigEndian
					? BinaryPrimitives.ReadUInt32BigEndian(page)
					: BinaryPrimitives.ReadUInt32LittleEndian(page);
			}

			public override void PokeByte(long addr, byte val)
			{
				var page = GetPage((uint)addr);
				if (page.IsEmpty)
				{
					return;
				}

				page[0] = val;
			}

			public override void PokeUshort(long addr, ushort val, bool bigEndian)
			{
				// if we cross a page boundary, we need to write to multiple pages
				if ((addr & ENCORE_PAGE_MASK) > ENCORE_PAGE_MASK - 1)
				{
					base.PokeUshort(addr, val, bigEndian);
					return;
				}

				var page = GetPage((uint)addr);
				if (page.IsEmpty)
				{
					return;
				}

				if (bigEndian)
				{
					BinaryPrimitives.WriteUInt16BigEndian(page, val);
				}
				else
				{
					BinaryPrimitives.WriteUInt16LittleEndian(page, val);
				}
			}

			public override void PokeUint(long addr, uint val, bool bigEndian)
			{
				// if we cross a page boundary, we need to write to multiple pages
				if ((addr & ENCORE_PAGE_MASK) > ENCORE_PAGE_MASK - 3)
				{
					base.PokeUint(addr, val, bigEndian);
					return;
				}

				var page = GetPage((uint)addr);
				if (page.IsEmpty)
				{
					return;
				}

				if (bigEndian)
				{
					BinaryPrimitives.WriteUInt32BigEndian(page, val);
				}
				else
				{
					BinaryPrimitives.WriteUInt32LittleEndian(page, val);
				}
			}

			private void BulkPeekByte(uint startAddr, Span<byte> values)
			{
				while (!values.IsEmpty)
				{
					var page = GetPage(startAddr);
					var numBytes = Math.Min(values.Length, (int)(ENCORE_PAGE_SIZE - (startAddr & ENCORE_PAGE_MASK)));
					if (page.IsEmpty)
					{
						values[..numBytes].Clear();
					}
					else
					{
						page[..numBytes].CopyTo(values);
					}

					values = values[numBytes..];
					startAddr += (uint)numBytes;
				}
			}

			public override void BulkPeekByte(Range<long> addresses, byte[] values)
			{
				if (addresses is null) throw new ArgumentNullException(paramName: nameof(addresses));
				if (values is null) throw new ArgumentNullException(paramName: nameof(values));

				if ((long)addresses.Count() != values.Length)
				{
					throw new InvalidOperationException("Invalid length of values array");
				}

				BulkPeekByte((uint)addresses.Start, values);
			}

			public override void BulkPeekUshort(Range<long> addresses, bool bigEndian, ushort[] values)
			{
				if (addresses is null) throw new ArgumentNullException(paramName: nameof(addresses));
				if (values is null) throw new ArgumentNullException(paramName: nameof(values));

				var start = addresses.Start;
				var end = addresses.EndInclusive + 1;

				if ((start & 1) != 0 || (end & 1) != 0)
					throw new InvalidOperationException("The API contract doesn't define what to do for unaligned reads and writes!");

				if (values.LongLength * 2 != end - start)
				{
					// a longer array could be valid, but nothing needs that so don't support it for now
					throw new InvalidOperationException("Invalid length of values array");
				}

				BulkPeekByte((uint)addresses.Start, MemoryMarshal.AsBytes(values.AsSpan()));

				if (bigEndian)
				{
					for (var i = 0; i < values.Length; i++)
					{
						values[i] = BinaryPrimitives.ReverseEndianness(values[i]);
					}
				}
			}

			public override void BulkPeekUint(Range<long> addresses, bool bigEndian, uint[] values)
			{
				if (addresses is null) throw new ArgumentNullException(paramName: nameof(addresses));
				if (values is null) throw new ArgumentNullException(paramName: nameof(values));

				var start = addresses.Start;
				var end = addresses.EndInclusive + 1;

				if ((start & 3) != 0 || (end & 3) != 0)
					throw new InvalidOperationException("The API contract doesn't define what to do for unaligned reads and writes!");

				if (values.LongLength * 4 != end - start)
				{
					// a longer array could be valid, but nothing needs that so don't support it for now
					throw new InvalidOperationException("Invalid length of values array");
				}

				BulkPeekByte((uint)addresses.Start, MemoryMarshal.AsBytes(values.AsSpan()));

				if (bigEndian)
				{
					for (var i = 0; i < values.Length; i++)
					{
						values[i] = BinaryPrimitives.ReverseEndianness(values[i]);
					}
				}
			}
		}
	}
}
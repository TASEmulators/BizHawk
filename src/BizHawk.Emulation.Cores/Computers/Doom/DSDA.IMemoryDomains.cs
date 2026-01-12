using System.Buffers.Binary;
using System.Collections.Generic;

using BizHawk.Common;
using BizHawk.Common.CollectionExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Waterbox;

namespace BizHawk.Emulation.Cores.Computers.Doom
{
	public partial class DSDA
	{
		private IMemoryDomains MemoryDomains;

		private void SetupMemoryDomains()
		{
			var domains = new List<MemoryDomain>
			{
				new MemoryDomainDelegate(
					"Players",
					0x10000,
					MemoryDomain.Endian.Little,
					addr =>
					{
						if (addr > 0xFFFF)
						{
							throw new ArgumentOutOfRangeException(
								paramName: nameof(addr),
								addr, message: "address out of range");
						}
						return _core.dsda_read_memory_array(LibDSDA.MemoryArrayType.Players, (uint) addr);
					},
					null,
					1),
				new MemoryDomainDelegate(
					"Things",
					0x10000000,
					MemoryDomain.Endian.Little,
					addr =>
					{
						if (addr > 0xFFFFFFF)
						{
							throw new ArgumentOutOfRangeException(
								paramName: nameof(addr),
								addr, message: "address out of range");
						}
						return _core.dsda_read_memory_array(LibDSDA.MemoryArrayType.Things, (uint) addr);
					},
					null,
					1),
				new MemoryDomainDelegate(
					"Lines",
					0x1000000,
					MemoryDomain.Endian.Little,
					addr =>
					{
						if (addr > 0xFFFFFF)
						{
							throw new ArgumentOutOfRangeException(
								paramName: nameof(addr),
								addr, message: "address out of range");
						}
						return _core.dsda_read_memory_array(LibDSDA.MemoryArrayType.Lines, (uint) addr);
					},
					null,
					1),
				new MemoryDomainDelegate(
					"Sectors",
					0x1000000,
					MemoryDomain.Endian.Little,
					addr =>
					{
						if (addr > 0xFFFFFF)
						{
							throw new ArgumentOutOfRangeException(
								paramName: nameof(addr),
								addr, message: "address out of range");
						}
						return _core.dsda_read_memory_array(LibDSDA.MemoryArrayType.Sectors, (uint) addr);
					},
					null,
					1),
				new InternalWbxMemory(_elf),
				_elf.GetPagesDomain(),
			};

			MemoryDomains = new MemoryDomainList(domains);
			((BasicServiceProvider)ServiceProvider).Register(MemoryDomains);
		}

		private class InternalWbxMemory : MemoryDomain
		{
			private const long WBX_BASE_ADDR = 0x36F_0000_0000;
			private const uint WBX_PAGE_SIZE = 0x1000;
			private const uint WBX_PAGE_MASK = WBX_PAGE_SIZE - 1;
			private const byte WBX_PAGE_READABLE_BITFLAG = 1 << 0;

			private readonly WaterboxHost _exe;
			private readonly MemoryDomain _pagesDomain;

			public InternalWbxMemory(WaterboxHost exe)
			{
				_exe = exe;
				_pagesDomain = exe.GetPagesDomain();

				Name = "System Bus";
				Size = _pagesDomain.Size * WBX_PAGE_SIZE;
				WordSize = 4;
				EndianType = Endian.Little;
				Writable = false;
			}

			private ReadOnlySpan<byte> GetPage(uint addr)
			{
				var pageNum = addr / WBX_PAGE_SIZE;
				if ((_pagesDomain.PeekByte(pageNum) & WBX_PAGE_READABLE_BITFLAG)
					!= WBX_PAGE_READABLE_BITFLAG)
				{
					return [ ];
				}

				return Util.UnsafeSpanFromPointer(
					ptr: (IntPtr) (WBX_BASE_ADDR + addr),
					length: (int) (WBX_PAGE_SIZE - (addr & WBX_PAGE_MASK)));
			}

			public override byte PeekByte(long addr)
			{
				var page = GetPage((uint) addr);
				if (page.IsEmpty)
				{
					return 0;
				}

				using (_exe.EnterExit())
				{
					return page[0];
				}
			}

			public override ushort PeekUshort(long addr, bool bigEndian)
			{
				// if we cross a page boundary, we need to read multiple pages
				if ((addr & WBX_PAGE_MASK) > WBX_PAGE_MASK - 1)
				{
					return base.PeekUshort(addr, bigEndian);
				}

				var page = GetPage((uint) addr);
				if (page.IsEmpty)
				{
					return 0;
				}

				using (_exe.EnterExit())
				{
					return bigEndian
						? BinaryPrimitives.ReadUInt16BigEndian(page)
						: BinaryPrimitives.ReadUInt16LittleEndian(page);
				}
			}

			public override uint PeekUint(long addr, bool bigEndian)
			{
				// if we cross a page boundary, we need to read multiple pages
				if ((addr & WBX_PAGE_MASK) > WBX_PAGE_MASK - 3)
				{
					return base.PeekUint(addr, bigEndian);
				}

				var page = GetPage((uint) addr);
				if (page.IsEmpty)
				{
					return 0;
				}

				using (_exe.EnterExit())
				{
					return bigEndian
						? BinaryPrimitives.ReadUInt32BigEndian(page)
						: BinaryPrimitives.ReadUInt32LittleEndian(page);
				}
			}

			private void BulkPeekByte(uint startAddr, Span<byte> values)
			{
				using (_exe.EnterExit())
				{
					while (!values.IsEmpty)
					{
						var page = GetPage(startAddr);
						var numBytes = Math.Min(
							values.Length,
							(int) (WBX_PAGE_SIZE - (startAddr & WBX_PAGE_MASK)));
						if (page.IsEmpty)
						{
							values[..numBytes].Clear();
						}
						else
						{
							page[..numBytes].CopyTo(values);
						}

						values = values[numBytes..];
						startAddr += (uint) numBytes;
					}
				}
			}

			public override void BulkPeekByte(Range<long> addresses, byte[] values)
			{
				if (addresses is null)
					throw new ArgumentNullException(paramName: nameof(addresses));
				if (values is null)
					throw new ArgumentNullException(paramName: nameof(values));

				if ((long) addresses.Count() != values.Length)
				{
					throw new InvalidOperationException("Invalid length of values array");
				}

				BulkPeekByte((uint) addresses.Start, values);
			}

			public override void BulkPeekUshort(Range<long> addresses, bool bigEndian, ushort[] values)
			{
				if (addresses is null)
					throw new ArgumentNullException(paramName: nameof(addresses));
				if (values is null)
					throw new ArgumentNullException(paramName: nameof(values));

				var start = addresses.Start;
				var end = addresses.EndInclusive + 1;

				if ((start & 1) != 0 || (end & 1) != 0)
					throw new InvalidOperationException("The API contract doesn't define what to do for unaligned reads and writes!");

				if (values.LongLength * 2 != end - start)
				{
					// a longer array could be valid, but nothing needs that so don't support it for now
					throw new InvalidOperationException("Invalid length of values array");
				}

				BulkPeekByte((uint) addresses.Start, values.BytesSpan());

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
				if (addresses is null)
					throw new ArgumentNullException(paramName: nameof(addresses));
				if (values is null)
					throw new ArgumentNullException(paramName: nameof(values));

				var start = addresses.Start;
				var end = addresses.EndInclusive + 1;

				if ((start & 3) != 0 || (end & 3) != 0)
					throw new InvalidOperationException("The API contract doesn't define what to do for unaligned reads and writes!");

				if (values.LongLength * 4 != end - start)
				{
					// a longer array could be valid, but nothing needs that so don't support it for now
					throw new InvalidOperationException("Invalid length of values array");
				}

				BulkPeekByte((uint) addresses.Start, values.BytesSpan());

				if (bigEndian)
				{
					for (var i = 0; i < values.Length; i++)
					{
						values[i] = BinaryPrimitives.ReverseEndianness(values[i]);
					}
				}
			}

			public override void PokeByte(long addr, byte val)
				=> FailPokingNotAllowed();
		}
	}
}

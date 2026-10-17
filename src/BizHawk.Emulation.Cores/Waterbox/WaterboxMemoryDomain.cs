using System.Runtime.InteropServices;

using BizHawk.BizInvoke;
using BizHawk.Common;
using BizHawk.Emulation.Common;

using static BizHawk.Emulation.Cores.Waterbox.LibWaterboxCore;

namespace BizHawk.Emulation.Cores.Waterbox
{
	public abstract class WaterboxMemoryDomain : MemoryDomain
	{
		protected readonly IntPtr _data;
		protected readonly IMonitor _monitor;
		protected readonly long _addressMangler;

		public MemoryArea Definition { get; }

		public static WaterboxMemoryDomain Create(MemoryArea m, WaterboxHost monitor)
		{
			if (m.Flags.HasFlag(MemoryDomainFlags.FunctionHook))
				return new WaterboxMemoryDomainFunc(m, monitor);
			else if (m.Flags.HasFlag(MemoryDomainFlags.SizedFunctionHooks))
				return new WaterboxMemoryDomainSizedFuncs(m, monitor);
			else
				return new WaterboxMemoryDomainPointer(m, monitor);
		}

		protected WaterboxMemoryDomain(MemoryArea m, IMonitor monitor)
		{
			Name = Mershul.PtrToStringUtf8(m.Name);
			EndianType = (m.Flags & MemoryDomainFlags.YugeEndian) != 0 ? Endian.Big : Endian.Little;
			_data = m.Data;
			Size = m.Size;
			Writable = (m.Flags & MemoryDomainFlags.Writable) != 0;
			if ((m.Flags & MemoryDomainFlags.WordSize1) != 0)
				WordSize = 1;
			else if ((m.Flags & MemoryDomainFlags.WordSize2) != 0)
				WordSize = 2;
			else if ((m.Flags & MemoryDomainFlags.WordSize4) != 0)
				WordSize = 4;
			else if ((m.Flags & MemoryDomainFlags.WordSize8) != 0)
				WordSize = 8;
			else
				throw new InvalidOperationException("Unknown word size for memory domain");
			_monitor = monitor;
			if ((m.Flags & MemoryDomainFlags.Swapped) != 0 && EndianType == Endian.Big)
			{
				_addressMangler = WordSize - 1;
			}
			else
			{
				_addressMangler = 0;
			}
			Definition = m;
		}

		public override void Enter()
			=> _monitor.Enter();

		public override void Exit()
			=> _monitor.Exit();
	}

	public unsafe class WaterboxMemoryDomainPointer : WaterboxMemoryDomain
	{
		internal WaterboxMemoryDomainPointer(MemoryArea m, IMonitor monitor)
			: base(m, monitor)
		{
			if (m.Flags.HasFlag(MemoryDomainFlags.FunctionHook))
				throw new InvalidOperationException();
		}

		public override byte PeekByte(long addr)
		{
			if ((ulong)addr < (ulong)Size)
			{
				using (_monitor.EnterExit())
				{
					return ((byte*)_data)[addr ^ _addressMangler];
				}
			}

			throw new ArgumentOutOfRangeException(nameof(addr));
		}

		public override void PokeByte(long addr, byte val)
		{
			if (Writable)
			{
				if ((ulong)addr < (ulong)Size)
				{
					using (_monitor.EnterExit())
					{
						((byte*)_data)[addr ^ _addressMangler] = val;
					}
				}
				else
				{
					throw new ArgumentOutOfRangeException(nameof(addr));
				}
			}
		}

		public override void BulkPeekByte(long startAddress, Span<byte> values)
		{
			if (_addressMangler != 0)
			{
				base.BulkPeekByte(startAddress, values);
				return;
			}

			var start = (ulong)startAddress;

			if (startAddress < Size && (start + (ulong)values.Length) <= (ulong)Size)
			{
				using (_monitor.EnterExit())
				{
					Span<byte> data = new(Z.US((ulong)_data + start).ToPointer(), values.Length);
					data.CopyTo(values);
				}
			}
			else if (startAddress < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(startAddress));
			}
			else
			{
				throw new ArgumentOutOfRangeException(nameof(values));
			}
		}
	}

	/// <summary>
	/// For private use only!  Don't touch
	/// </summary>
	public abstract class MemoryDomainAccessStub
	{
		[BizImport(CallingConvention.Cdecl)]
		public abstract void Access(IntPtr buffer, long address, long count, bool write);

		private class StubResolver : IImportResolver
		{
			private readonly IntPtr _p;
			public StubResolver(IntPtr p)
			{
				_p = p;
			}
			public IntPtr GetProcAddrOrThrow(string entryPoint) => _p;
			public IntPtr GetProcAddrOrZero(string entryPoint) => _p;
		}

		public static MemoryDomainAccessStub Create(IntPtr p, WaterboxHost host)
		{
			return BizInvoker.GetInvoker<MemoryDomainAccessStub>(
				new StubResolver(p), host, CallingConventionAdapters.MakeWaterboxDepartureOnly(host));
		}
	}
	public abstract class MemoryDomainSizedAccessStub
	{
		[BizImport(CallingConvention.Cdecl)]
		public abstract void Access(IntPtr buffer, long address, long count);

		private class StubResolver : IImportResolver
		{
			private readonly IntPtr _p;

			public StubResolver(IntPtr p)
			{
				_p = p;
			}

			public IntPtr GetProcAddrOrThrow(string entryPoint) => _p;
			public IntPtr GetProcAddrOrZero(string entryPoint) => _p;
		}

		public static MemoryDomainSizedAccessStub Create(IntPtr p, WaterboxHost host)
		{
			return BizInvoker.GetInvoker<MemoryDomainSizedAccessStub>(
				new StubResolver(p),
				host,
				CallingConventionAdapters.MakeWaterboxDepartureOnly(host));
		}
	}


	public unsafe class WaterboxMemoryDomainFunc : WaterboxMemoryDomain
	{
		private readonly MemoryDomainAccessStub _access;

		internal WaterboxMemoryDomainFunc(MemoryArea m, WaterboxHost monitor)
			: base(m, monitor)
		{
			if (!m.Flags.HasFlag(MemoryDomainFlags.FunctionHook))
				throw new InvalidOperationException();
			_access = MemoryDomainAccessStub.Create(m.Data, monitor);
		}

		public override byte PeekByte(long addr)
		{
			if ((ulong)addr < (ulong)Size)
			{
				byte ret = 0;
				_access.Access((IntPtr)(&ret), addr, 1, false);
				return ret;
			}

			throw new ArgumentOutOfRangeException(nameof(addr));
		}

		public override void PokeByte(long addr, byte val)
		{
			if (Writable)
			{
				if ((ulong)addr < (ulong)Size)
				{
					_access.Access((IntPtr)(&val), addr, 1, true);
				}
				else
				{
					throw new ArgumentOutOfRangeException(nameof(addr));
				}
			}
		}

		public override void BulkPeekByte(long startAddress, Span<byte> values)
		{
			if (_addressMangler != 0)
			{
				base.BulkPeekByte(startAddress, values);
				return;
			}

			var start = (ulong)startAddress;

			if (startAddress < Size && (start + (ulong)values.Length) <= (ulong)Size)
			{
				fixed (byte* p = values)
					_access.Access((IntPtr)p, (long)start, values.Length, false);
			}
			else if (startAddress < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(startAddress));
			}
			else
			{
				throw new ArgumentOutOfRangeException(nameof(values));
			}
		}
	}

	/// <summary>
	/// A memory domain for things like a system bus, where the size of the read/write may affect the result.
	/// Endianness is ignored; we are writing numbers not multi-byte representations of numbers.
	/// </summary>
	public unsafe class WaterboxMemoryDomainSizedFuncs : WaterboxMemoryDomain
	{
		private readonly MemoryDomainSizedAccessStub _read8 = null;
		private readonly MemoryDomainSizedAccessStub _write8 = null;
		private readonly MemoryDomainSizedAccessStub _read16 = null;
		private readonly MemoryDomainSizedAccessStub _write16 = null;
		private readonly MemoryDomainSizedAccessStub _read32 = null;
		private readonly MemoryDomainSizedAccessStub _write32 = null;

		private string AddressRangeError => string.Format("Address must be in the range [0, 0x{0:x}]", Size - 1);

		internal WaterboxMemoryDomainSizedFuncs(MemoryArea m, WaterboxHost monitor)
			: base(m, monitor)
		{
			if (!m.Flags.HasFlag(MemoryDomainFlags.SizedFunctionHooks))
				throw new InvalidOperationException();

			using (monitor.EnterExit())
			{
				IntPtr* functionPointers = (IntPtr*)m.Data;
				for (int i = 0; i < 6; i++)
				{
					if (functionPointers[i] == IntPtr.Zero) throw new Exception("All access functions must have implementations.");
				}
				_read8 = MemoryDomainSizedAccessStub.Create(functionPointers[0], monitor);
				_write8 = MemoryDomainSizedAccessStub.Create(functionPointers[1], monitor);

				_read16 = MemoryDomainSizedAccessStub.Create(functionPointers[2], monitor);
				_write16 = MemoryDomainSizedAccessStub.Create(functionPointers[3], monitor);

				_read32 = MemoryDomainSizedAccessStub.Create(functionPointers[4], monitor);
				_write32 = MemoryDomainSizedAccessStub.Create(functionPointers[5], monitor);
			}
		}

		public override byte PeekByte(long addr)
		{
			if ((ulong)addr >= (ulong)Size || addr < 0) throw new ArgumentOutOfRangeException(nameof(addr), message: AddressRangeError);

			byte ret = 0;
			_read8.Access((IntPtr)(&ret), addr, 1);
			return ret;
		}

		public override void PokeByte(long addr, byte val)
		{
			if ((ulong)addr >= (ulong)Size || addr < 0) throw new ArgumentOutOfRangeException(nameof(addr), message: AddressRangeError);

			_write8.Access((IntPtr)(&val), addr, 1);
		}

		public override ushort PeekUshort(long addr, bool bigEndian)
		{
			if ((ulong)addr + 1 >= (ulong)Size || addr < 0) throw new ArgumentOutOfRangeException(nameof(addr), message: AddressRangeError);

			ushort ret = 0;
			_read16.Access((IntPtr)(&ret), addr, 1);
			return ret;
		}

		public override void PokeUshort(long addr, ushort val, bool bigEndian)
		{
			if ((ulong)addr + 1 >= (ulong)Size || addr < 0) throw new ArgumentOutOfRangeException(nameof(addr), message: AddressRangeError);

			_write16.Access((IntPtr)(&val), addr, 1);
		}

		public override uint PeekUint(long addr, bool bigEndian)
		{
			if ((ulong)addr + 3 >= (ulong)Size || addr < 0) throw new ArgumentOutOfRangeException(nameof(addr), message: AddressRangeError);

			uint ret = 0;
			_read32.Access((IntPtr)(&ret), addr, 1);
			return ret;
		}

		public override void PokeUint(long addr, uint val, bool bigEndian)
		{
			if ((ulong)addr + 3 >= (ulong)Size || addr < 0) throw new ArgumentOutOfRangeException(nameof(addr), message: AddressRangeError);

			_write32.Access((IntPtr)(&val), addr, 1);
		}

		public override void BulkPeekByte(long startAddress, Span<byte> values)
		{
			if ((ulong)startAddress + (ulong)values.Length > (ulong)Size) throw new ArgumentOutOfRangeException(nameof(values), message: AddressRangeError);
			else if (startAddress < 0) throw new ArgumentOutOfRangeException(nameof(startAddress), message: AddressRangeError);

			fixed (byte* p = values)
				_read8.Access((IntPtr)p, startAddress, values.Length);
		}

		public override void BulkPokeByte(long addr, ReadOnlySpan<byte> values)
		{
			if ((ulong)addr + (ulong)values.Length > (ulong)Size || addr < 0) throw new ArgumentOutOfRangeException(nameof(addr), message: AddressRangeError);

			fixed (byte* p = values)
				_write8.Access((IntPtr)p, addr, values.Length);
		}

		public override void BulkPeekUshort(long startAddress, Span<ushort> values, bool bigEndian)
		{
			if ((ulong)startAddress + (ulong)values.Length * sizeof(ushort) > (ulong)Size) throw new ArgumentOutOfRangeException(nameof(values), message: AddressRangeError);
			else if (startAddress < 0) throw new ArgumentOutOfRangeException(nameof(startAddress), message: AddressRangeError);

			fixed (ushort* p = values)
				_read16.Access((IntPtr)p, startAddress, values.Length);
		}

		public override void BulkPokeUshort(long addr, ReadOnlySpan<ushort> values, bool bigEndian)
		{
			if ((ulong)addr + (ulong)values.Length * sizeof(ushort) > (ulong)Size || addr < 0) throw new ArgumentOutOfRangeException(nameof(addr), message: AddressRangeError);

			fixed (ushort* p = values)
				_write16.Access((IntPtr)p, addr, values.Length);
		}

		public override void BulkPeekUint(long startAddress, Span<uint> values, bool bigEndian)
		{
			if ((ulong)startAddress + (ulong)values.Length * sizeof(uint) > (ulong)Size) throw new ArgumentOutOfRangeException(nameof(values), message: AddressRangeError);
			else if (startAddress < 0) throw new ArgumentOutOfRangeException(nameof(startAddress), message: AddressRangeError);

			fixed (uint* p = values)
				_read32.Access((IntPtr)p, startAddress, values.Length);
		}

		public override void BulkPokeUint(long addr, ReadOnlySpan<uint> values, bool bigEndian)
		{
			if ((ulong)addr + (ulong)values.Length * sizeof(uint) > (ulong)Size || addr < 0) throw new ArgumentOutOfRangeException(nameof(addr), message: AddressRangeError);

			fixed (uint* p = values)
				_write32.Access((IntPtr)p, addr, values.Length);
		}
	}
}

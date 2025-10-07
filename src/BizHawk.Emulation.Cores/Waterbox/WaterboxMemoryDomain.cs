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
			return m.Flags.HasFlag(MemoryDomainFlags.FunctionHook)
				? new WaterboxMemoryDomainFunc(m, monitor)
				: new WaterboxMemoryDomainPointer(m, monitor);
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
			if ((ulong) Size <= (ulong) addr) throw new ArgumentOutOfRangeException(paramName: nameof(addr), addr, message: string.Format(ERR_FMT_STR_ADDR_OOR, Size));
			using (_monitor.EnterExit()) return ((byte*) _data)[addr ^ _addressMangler];
		}

		public override void PokeByte(long addr, byte val)
		{
			if (!Writable)
			{
				FailPokingNotAllowed();
				return;
			}
			if ((ulong) Size <= (ulong) addr) throw new ArgumentOutOfRangeException(paramName: nameof(addr), addr, message: string.Format(ERR_FMT_STR_ADDR_OOR, Size));
			using (_monitor.EnterExit()) ((byte*) _data)[addr ^ _addressMangler] = val;
		}

		public override void BulkPeekByte(Range<long> addresses, byte[] values)
		{
			if (_addressMangler != 0)
			{
				base.BulkPeekByte(addresses, values);
				return;
			}
			//TODO why are we casting `long`s to `ulong` here?
			var start = (ulong)addresses.Start;
			var count = addresses.Count();
			if ((ulong) Size <= start) throw new ArgumentOutOfRangeException(paramName: nameof(addresses), start, message: string.Format(ERR_FMT_STR_START_OOR, Size));
			var endExcl = start + count;
			if ((ulong) Size < endExcl) throw new ArgumentOutOfRangeException(paramName: nameof(addresses), endExcl, message: string.Format(ERR_FMT_STR_END_OOR, Size));
			using (_monitor.EnterExit()) Marshal.Copy(source: Z.US((ulong) _data + start), destination: values, startIndex: 0, length: (int) count);
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
			//TODO why are we casting `long`s to `ulong` here?
			if ((ulong) Size <= (ulong) addr) throw new ArgumentOutOfRangeException(paramName: nameof(addr), addr, message: string.Format(ERR_FMT_STR_ADDR_OOR, Size));
			byte ret = 0;
			_access.Access((IntPtr) (&ret), address: addr, count: 1, write: false);
			return ret;
		}

		public override void PokeByte(long addr, byte val)
		{
			if (!Writable)
			{
				FailPokingNotAllowed();
				return;
			}
			//TODO why are we casting `long`s to `ulong` here?
			if ((ulong) Size <= (ulong) addr) throw new ArgumentOutOfRangeException(paramName: nameof(addr), addr, message: string.Format(ERR_FMT_STR_ADDR_OOR, Size));
			_access.Access((IntPtr) (&val), address: addr, count: 1, write: true);
		}

		public override void BulkPeekByte(Range<long> addresses, byte[] values)
		{
			if (_addressMangler != 0)
			{
				base.BulkPeekByte(addresses, values);
				return;
			}
			//TODO why are we casting `long`s to `ulong` here?
			var start = (ulong)addresses.Start;
			var count = addresses.Count();
			if ((ulong) Size <= start) throw new ArgumentOutOfRangeException(paramName: nameof(addresses), start, message: string.Format(ERR_FMT_STR_START_OOR, Size));
			var endExcl = start + count;
			if ((ulong) Size < endExcl) throw new ArgumentOutOfRangeException(paramName: nameof(addresses), endExcl, message: string.Format(ERR_FMT_STR_END_OOR, Size));
			fixed (byte* p = values) _access.Access((IntPtr) p, address: addresses.Start, count: (long) count, write: false);
		}
	}
}

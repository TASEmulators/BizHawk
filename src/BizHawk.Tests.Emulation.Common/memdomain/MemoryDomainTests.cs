using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices;

using BizHawk.Emulation.Common;

namespace BizHawk.Tests.Emulation.Common
{
	public abstract class MemoryDomainTestsBase<TOORE>
		where TOORE : Exception
	{
		protected const MemoryDomain.Endian BIG_ENDIAN = MemoryDomain.Endian.Big;

		private const byte SENTINEL = 0xE3;

		protected const int SIZE = 6;

		protected static readonly ImmutableArray<byte> InitPattern = [ 0xFE, 0xDC, 0xBA, 0x98, 0x76, 0x54, 0x32, 0x10 ];

		protected MemoryDomain _domain = null!;

		private string SubclassName
#pragma warning disable BHI1101 // this is a simple way to disambiguate test cases in the log
			=> GetType().Name;
#pragma warning restore BHI1101

		protected abstract void AssertUnchanged();

		[TestMethod]
		public void TestPeek1oAfterEnd()
		{
			Assert.Throws<TOORE>(() => _domain.PeekByte(6L), $"peeking byte at [^0] in {SubclassName}");
			AssertUnchanged();
		}

		[TestMethod]
		public void TestPeek1oBeforeStart()
		{
			Assert.Throws<TOORE>(() => _domain.PeekByte(-1L), $"peeking byte at [-1] in {SubclassName}");
			AssertUnchanged();
		}

		[TestMethod]
		public void TestPeek1oFirst()
		{
			Assert.AreEqual(0xDC, _domain.PeekByte(0L), $"peeking byte at [0] in {SubclassName}");
			AssertUnchanged();
		}

		[TestMethod]
		public void TestPeek1oFourth()
		{
			Assert.AreEqual(0x76, _domain.PeekByte(3L), $"peeking byte at [3] in {SubclassName}");
			AssertUnchanged();
		}

		[TestMethod]
		public void TestPeek1oLast()
		{
			Assert.AreEqual(0x32, _domain.PeekByte(5L), $"peeking byte at [5] in {SubclassName}");
			AssertUnchanged();
		}

		//TODO peek 2o

		//TODO peek 4o

		[TestMethod]
		public void TestPoke1oAfterEnd()
		{
			Assert.Throws<TOORE>(() => _domain.PokeByte(6L, SENTINEL), $"poking byte at [^0] in {SubclassName}");
			AssertUnchanged();
		}

		[TestMethod]
		public void TestPoke1oBeforeStart()
		{
			Assert.Throws<TOORE>(() => _domain.PokeByte(-1L, SENTINEL), $"poking byte at [-1] in {SubclassName}");
			AssertUnchanged();
		}

		[TestMethod]
		public void TestPoke1oFirst()
		{
			Assert.Inconclusive("TODO");
		}

		[TestMethod]
		public void TestPoke1oFourth()
		{
			Assert.Inconclusive("TODO");
		}

		[TestMethod]
		public void TestPoke1oLast()
		{
			Assert.Inconclusive("TODO");
		}

		//TODO poke 2o

		//TODO poke 4o
	}

	[TestClass]
	public sealed class MemoryDomainByteArrayTests : MemoryDomainTestsBase<IndexOutOfRangeException>
	{
#if true
		private byte[] _buf = null!;

		protected override void AssertUnchanged()
			=> CollectionAssert.AreEqual(InitPattern.Slice(start: 1, length: SIZE), _buf);

		[TestInitialize]
		public void TestInitialize()
		{
			_buf = InitPattern.Slice(start: 1, length: SIZE).ToArray();
			_domain = new MemoryDomainByteArray("RAM", BIG_ENDIAN, _buf, writable: true, wordSize: 1);
		}
#else
		[StructLayout(LayoutKind.Sequential)]
		private struct Buffer
		{
			public byte Before;

			public unsafe fixed byte Buf[6];

			public byte After;
		}

		private Buffer _buf;

		protected override void AssertUnchanged()
			=> CollectionAssert.AreEqual(InitPattern, MemoryMarshal.AsBytes([ _buf ]).ToArray());

		[TestInitialize]
		public void TestInitialize()
		{
			_buf = MemoryMarshal.Read<Buffer>(InitPattern.ToArray());
			byte[] usable = _buf.Buf; //TODO doesn't work--this expression's type is a pointer, not an array
			// we may not be able to catch buffer under/overreads for this impl., though of course there's no need to since it's completely managed and the runtime ensures a bounds check
			// maybe this struct idea could be used for `MemoryDomainIntPtr*` instead?
			_domain = new MemoryDomainByteArray("RAM", BIG_ENDIAN, usable, writable: true, wordSize: 1);
		}
#endif
	}

	[TestClass]
	public sealed class MemoryDomainDelegateTests : MemoryDomainTestsBase<ArgumentOutOfRangeException>
	{
		private List<byte> _buf = null!;

		protected override void AssertUnchanged()
			=> CollectionAssert.AreEqual(InitPattern, _buf);

		private byte PeekByte(long addr)
			=> _buf[1 + unchecked((int) addr)];

		private void PokeByte(long addr, byte value)
			=> _buf[1 + unchecked((int) addr)] = value;

		[TestInitialize]
		public void TestInitialize()
		{
			_buf = InitPattern.ToList();
			_domain = new MemoryDomainDelegate("RAM", size: SIZE, BIG_ENDIAN, PeekByte, PokeByte, wordSize: 1);
		}
	}

#if false
	[TestClass]
	public sealed class MemoryDomainIntPtrTests : MemoryDomainTestsBase<ArgumentOutOfRangeException>
	{
		protected override void AssertUnchanged()
			=> throw new NotImplementedException("TODO");

		[TestInitialize]
		public void TestInitialize()
		{
			throw new NotImplementedException("TODO");
		}
	}

	[TestClass]
	public sealed class MemoryDomainIntPtrMonitorTests : MemoryDomainTestsBase<ArgumentOutOfRangeException>
	{
		protected override void AssertUnchanged()
			=> throw new NotImplementedException("TODO");

		[TestInitialize]
		public void TestInitialize()
		{
			throw new NotImplementedException("TODO");
		}
	}

	[TestClass]
	public sealed class MemoryDomainIntPtrSwap16Tests : MemoryDomainTestsBase<ArgumentOutOfRangeException>
	{
		protected override void AssertUnchanged()
			=> throw new NotImplementedException("TODO");

		[TestInitialize]
		public void TestInitialize()
		{
			throw new NotImplementedException("TODO");
		}
	}

	[TestClass]
	public sealed class MemoryDomainIntPtrSwap16MonitorTests : MemoryDomainTestsBase<ArgumentOutOfRangeException>
	{
		protected override void AssertUnchanged()
			=> throw new NotImplementedException("TODO");

		[TestInitialize]
		public void TestInitialize()
		{
			throw new NotImplementedException("TODO");
		}
	}
#endif

	[TestClass]
	public sealed class MemoryDomainUshortArrayTests : MemoryDomainTestsBase<IndexOutOfRangeException>
	{
		private ushort[] _buf = null!;

		protected override void AssertUnchanged()
			=> CollectionAssert.AreEqual(CloneInitPattern(), _buf);

		private ushort[] CloneInitPattern()
			=> MemoryMarshal.Cast<byte, ushort>(InitPattern.Slice(start: 1, length: SIZE).AsSpan()).ToArray();

		[TestInitialize]
		public void TestInitialize()
		{
			_buf = CloneInitPattern();
			// see note in `MemoryDomainByteArrayTests` re: buffer under/overread
			_domain = new MemoryDomainUshortArray("RAM", BIG_ENDIAN, _buf, writable: true);
		}
	}
}

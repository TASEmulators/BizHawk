using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace BizHawk.Tests.Common;

// test for RamSearchEngine.ReinterpretAsF32
[TestClass]
public class ConversionTests
{
	private static float ReinterpretAsF32Unsafe(long l)
	{
		return Unsafe.As<long, float>(ref l);
	}

	private static readonly byte[] ScratchSpace = new byte[8];

	private static float ReinterpretAsF32BitConverter(long l)
	{
		BinaryPrimitives.WriteInt64LittleEndian(ScratchSpace, l);
		return BitConverter.ToSingle(ScratchSpace, startIndex: 0);
	}

	[TestMethod]
	[DoNotParallelize] // old implementation is not thread safe
	[DataRow(0, 0)]
	[DataRow(1, 1.401298E-45F)]
	[DataRow(1109917696, 42)]
	[DataRow(1123477881, 123.456F)]
	[DataRow(3212836864, -1)]
	[DataRow(0x7fffffffbf800000, -1)]
	public void TestReinterpretAsF32(long input, float expected)
	{
		float f32BitConverter = ReinterpretAsF32BitConverter(input);
		float f32Unsafe = ReinterpretAsF32Unsafe(input);

		Assert.AreEqual(expected, f32BitConverter);
		Assert.AreEqual(expected, f32Unsafe);
	}
}

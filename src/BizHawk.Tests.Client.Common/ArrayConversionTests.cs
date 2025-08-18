using BizHawk.Common;

namespace BizHawk.Tests.Client.Common;

[TestClass]
public class ArrayConversionTests
{
	[TestMethod]
	public void TestFromByteArrayConversion()
	{
		byte[] controlArray = [ 1, 250, 128, 127, 0, 42, 100, 200, 15 ];
		byte[] testArray = [ 1, 250, 128, 127, 0, 42, 100, 200, 15 ];

		bool[] convertedBoolArray = testArray.ToBoolBuffer();
		CollectionAssert.AreEqual(controlArray, testArray); // ensure testArray hasn't been altered
		CollectionAssert.AreEqual(new[] { true, true, true, true, false, true, true, true, true }, convertedBoolArray);

		short[] convertedShortArray = testArray.ToShortBuffer();
		CollectionAssert.AreEqual(controlArray, testArray); // ensure testArray hasn't been altered
		CollectionAssert.AreEqual(new short[] { -1535, 32640, 10752, -14236 }, convertedShortArray);

		ushort[] convertedUShortArray = testArray.ToUShortBuffer();
		CollectionAssert.AreEqual(controlArray, testArray); // ensure testArray hasn't been altered
		CollectionAssert.AreEqual(new ushort[] { 64001, 32640, 10752, 51300 }, convertedUShortArray);

		int[] convertedIntArray = testArray.ToIntBuffer();
		CollectionAssert.AreEqual(controlArray, testArray); // ensure testArray hasn't been altered
		CollectionAssert.AreEqual(new[] { 2139159041, -932959744 }, convertedIntArray);

		uint[] convertedUIntArray = testArray.ToUIntBuffer();
		CollectionAssert.AreEqual(controlArray, testArray); // ensure testArray hasn't been altered
		CollectionAssert.AreEqual(new uint[] { 2139159041, 3362007552 }, convertedUIntArray);

		float[] convertedFloatArray = testArray.ToFloatBuffer();
		CollectionAssert.AreEqual(controlArray, testArray); // ensure testArray hasn't been altered
		CollectionAssert.AreEqual(new[] { float.NaN, -233640 }, convertedFloatArray);

		double[] convertedDoubleArray = testArray.ToDoubleBuffer();
		CollectionAssert.AreEqual(controlArray, testArray); // ensure testArray hasn't been altered
		CollectionAssert.AreEqual(new[] { -5.4891820002610661E+40 }, convertedDoubleArray);
	}

	[TestMethod]
	public void TestFromBoolArrayConversion()
	{
		bool[] controlArray = [ true, false, false, false, true, true ];
		bool[] testArray = [ true, false, false, false, true, true ];

		byte[] convertedArray = testArray.ToUByteBuffer();
		CollectionAssert.AreEqual(controlArray, testArray); // ensure the array hasn't been altered
		CollectionAssert.AreEqual(new byte[] { 1, 0, 0, 0, 1, 1 }, convertedArray);
	}

	[TestMethod]
	public void TestFromShortArrayConversion()
	{
		short[] controlArray = [ -1000, 32500, -20123 ];
		short[] testArray = [ -1000, 32500, -20123 ];

		byte[] convertedArray = testArray.ToUByteBuffer();
		CollectionAssert.AreEqual(controlArray, testArray); // ensure the array hasn't been altered
		CollectionAssert.AreEqual(new byte[] { 24, 252, 244, 126, 101, 177 }, convertedArray);
	}

	[TestMethod]
	public void TestFromUShortArrayConversion()
	{
		ushort[] controlArray = [ 65325, 12345, 200 ];
		ushort[] testArray = [ 65325, 12345, 200 ];

		byte[] convertedArray = testArray.ToUByteBuffer();
		CollectionAssert.AreEqual(controlArray, testArray); // ensure the array hasn't been altered
		CollectionAssert.AreEqual(new byte[] { 45, 255, 57, 48, 200, 0 }, convertedArray);
	}

	[TestMethod]
	public void TestFromIntArrayConversion()
	{
		int[] controlArray = [ -66810, 14046021 ];
		int[] testArray = [ -66810, 14046021 ];

		byte[] convertedArray = testArray.ToUByteBuffer();
		CollectionAssert.AreEqual(controlArray, testArray); // ensure the array hasn't been altered
		CollectionAssert.AreEqual(new byte[] { 6, 251, 254, 255, 69, 83, 214, 0 }, convertedArray);
	}

	[TestMethod]
	public void TestFromUIntArrayConversion()
	{
		uint[] controlArray = [ 2452209956, 1728072531 ];
		uint[] testArray = [ 2452209956, 1728072531 ];

		byte[] convertedArray = testArray.ToUByteBuffer();
		CollectionAssert.AreEqual(controlArray, testArray); // ensure the array hasn't been altered
		CollectionAssert.AreEqual(new byte[] { 36, 193, 41, 146, 83, 75, 0, 103 }, convertedArray);
	}

	[TestMethod]
	public void TestFromFloatArrayConversion()
	{
		float[] controlArray = [ 42714, -0.9182739F ];
		float[] testArray = [ 42714, -0.9182739F ];

		byte[] convertedArray = testArray.ToUByteBuffer();
		CollectionAssert.AreEqual(controlArray, testArray); // ensure the array hasn't been altered
		CollectionAssert.AreEqual(new byte[] { 00, 218, 38, 71, 0, 20, 107, 191 }, convertedArray);
	}

	[TestMethod]
	public void TestFromDoubleArrayConversion()
	{
		double[] controlArray = [ -9.811838412096858E-8, 3.7674985642899561e+300 ];
		double[] testArray = [ -9.811838412096858E-8, 3.7674985642899561e+300 ];

		byte[] convertedArray = testArray.ToUByteBuffer();
		CollectionAssert.AreEqual(controlArray, testArray); // ensure the array hasn't been altered
		CollectionAssert.AreEqual(new byte[] { 244, 86, 42, 222, 164, 86, 122, 190, 19, 75, 242, 94, 186, 128, 86, 126 }, convertedArray);
	}
}

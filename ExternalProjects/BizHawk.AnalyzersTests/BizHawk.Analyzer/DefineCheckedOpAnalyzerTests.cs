namespace BizHawk.Tests.Analyzers;

using Verify = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
	BizHawk.Analyzers.DefineCheckedOpAnalyzer,
	Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

[TestClass]
public sealed class DefineCheckedOpAnalyzerTests
{
	[TestMethod]
	public Task CheckMisuseOfCheckedOperatorOverloading()
		=> Verify.VerifyAnalyzerAsync("""
			/* very much a sample; real numeric types should follow these rules according to [the docs](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/arithmetic-operators#user-defined-checked-operators):
			 * - A checked operator throws an OverflowException.
			 * - An operator without the checked modifier returns an instance representing a truncated result.
			 * I didn't do any of that, I just came up with something that threw and something that didn't and called it a day --yoshi
			 */
			using System;
			public struct GF7 {
				private static readonly int[] Inverses = [ -1, 1, 4, 5, 2, 3, 6 ];
				public static GF7 operator checked -(GF7 x) => new(-x._value); // the most useless but here for completeness
				{|BHI1300:public static GF7 operator /*unchecked*/ -(GF7 x) => new(-x._value % 7);|}
				public static GF7 operator checked ++(GF7 x) => new(x._value + 1);
				{|BHI1300:public static GF7 operator /*unchecked*/ ++(GF7 x) => new((x._value + 1) % 7);|}
				public static GF7 operator checked --(GF7 x) => new(x._value - 1);
				{|BHI1300:public static GF7 operator /*unchecked*/ --(GF7 x) => new((x._value - 1) % 7);|}
				public static GF7 operator checked +(GF7 x, GF7 y) => new(x._value + y._value);
				{|BHI1300:public static GF7 operator /*unchecked*/ +(GF7 x, GF7 y) => new((x._value + y._value) % 7);|}
				public static GF7 operator checked +(GF7 x, int y) => new(x._value + y);
				{|BHI1300:public static GF7 operator /*unchecked*/ +(GF7 x, int y) => new((x._value + y) % 7);|}
				public static GF7 operator checked -(GF7 minuend, int subtrahend) => new(minuend._value - subtrahend);
				{|BHI1300:public static GF7 operator /*unchecked*/ -(GF7 minuend, int subtrahend) => new((minuend._value - subtrahend) % 7);|}
				public static GF7 operator checked *(GF7 x, int y) => new(x._value * y);
				{|BHI1300:public static GF7 operator /*unchecked*/ *(GF7 x, int y) => new((x._value * y) % 7);|}
				public static GF7 operator checked /(GF7 dividend, GF7 divisor) => new((dividend._value * Inverses[divisor._value]) % 7);
				{|BHI1300:public static GF7 operator /*unchecked*/ /(GF7 dividend, GF7 divisor) => divisor._value is 0 ? default : new((dividend._value * Inverses[divisor._value]) % 7);|}
				public static explicit operator checked GF7(int n) => new(n);
				{|BHI1300:public static explicit operator /*unchecked*/ GF7(int n) => new(n % 7);|}
				private byte _value;
				public byte Value {
					get => _value;
					set {
						if (value < 0 || 6 < value) throw new ArgumentOutOfRangeException(paramName: nameof(value), value, message: "must be in 0..<7");
						_value = value;
					}
				}
				public GF7(byte value) => Value = value;
				private GF7(int value) => Value = unchecked((byte) value);
			}
		""");

	[TestMethod]
	public Task CheckMisuseOfUncheckedOperatorOverloading()
		=> Verify.VerifyAnalyzerAsync("""
			using System;
			public struct GF7 {
				private static readonly int[] Inverses = [ -1, 1, 4, 5, 2, 3, 6 ];
				{|BHI1300:public static GF7 operator -(GF7 x) => new(-x._value % 7);|}
				{|BHI1300:public static GF7 operator ++(GF7 x) => new((x._value + 1) % 7);|}
				{|BHI1300:public static GF7 operator --(GF7 x) => new((x._value - 1) % 7);|}
				{|BHI1300:public static GF7 operator +(GF7 x, GF7 y) => new((x._value + y._value) % 7);|}
				{|BHI1300:public static GF7 operator +(GF7 x, int y) => new((x._value + y) % 7);|}
				{|BHI1300:public static GF7 operator -(GF7 minuend, int subtrahend) => new((minuend._value - subtrahend) % 7);|}
				{|BHI1300:public static GF7 operator *(GF7 x, int y) => new((x._value * y) % 7);|}
				{|BHI1300:public static GF7 operator /(GF7 dividend, GF7 divisor) => divisor._value is 0 ? default : new((dividend._value * Inverses[divisor._value]) % 7);|}
				{|BHI1300:public static explicit operator GF7(int n) => new(n % 7);|}
				private byte _value;
				public byte Value {
					get => _value;
					set {
						if (value < 0 || 6 < value) throw new ArgumentOutOfRangeException(paramName: nameof(value), value, message: "must be in 0..<7");
						_value = value;
					}
				}
				public GF7(byte value) => Value = value;
				private GF7(int value) => Value = unchecked((byte) value);
			}
		""");
}

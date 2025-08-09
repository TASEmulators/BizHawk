using System.Linq;

using BizHawk.Common.StringExtensions;

using SE = BizHawk.Common.StringExtensions.StringExtensions;

namespace BizHawk.Tests.Common.StringExtensions
{
	[TestClass]
	public class StringExtensionTests
	{
		private const string abcdef = "abcdef";

		private const string qrs = "qrs";

		[TestMethod]
		public void In_CaseInsensitive()
		{
			var strArray = new[] { "Hello World" };
			var actual = "hello world".In(strArray);
			Assert.IsTrue(actual);
		}

		[TestMethod]
		public void TestRemovePrefix()
		{
			Assert.AreEqual("bcdef", abcdef.RemovePrefix('a', qrs));
			Assert.AreEqual(string.Empty, "a".RemovePrefix('a', qrs));
			Assert.AreEqual(qrs, abcdef.RemovePrefix('c', qrs));
			Assert.AreEqual(qrs, abcdef.RemovePrefix('x', qrs));
			Assert.AreEqual(qrs, string.Empty.RemovePrefix('a', qrs));

			Assert.AreEqual("def", abcdef.RemovePrefix("abc", qrs));
			Assert.AreEqual("bcdef", abcdef.RemovePrefix("a", qrs));
			Assert.AreEqual(abcdef, abcdef.RemovePrefix(string.Empty, qrs));
			Assert.AreEqual(string.Empty, abcdef.RemovePrefix(abcdef, qrs));
			Assert.AreEqual(string.Empty, "a".RemovePrefix("a", qrs));
			Assert.AreEqual(qrs, abcdef.RemovePrefix("c", qrs));
			Assert.AreEqual(qrs, abcdef.RemovePrefix("x", qrs));
			Assert.AreEqual(qrs, string.Empty.RemovePrefix("abc", qrs));
		}

		[TestMethod]
		public void TestRemoveSuffix()
		{
			Assert.AreEqual("abc", abcdef.RemoveSuffix("def", qrs));
			Assert.AreEqual("abcde", abcdef.RemoveSuffix("f", qrs));
			Assert.AreEqual(abcdef, abcdef.RemoveSuffix(string.Empty, qrs));
			Assert.AreEqual(string.Empty, abcdef.RemoveSuffix(abcdef, qrs));
			Assert.AreEqual(string.Empty, "f".RemoveSuffix("f", qrs));
			Assert.AreEqual(qrs, abcdef.RemoveSuffix("d", qrs));
			Assert.AreEqual(qrs, abcdef.RemoveSuffix("x", qrs));
			Assert.AreEqual(qrs, string.Empty.RemoveSuffix("def", qrs));
		}

		[DataRow(0, null)]
		[DataRow(0x2D2816FE, /*string.Empty*/"")]
		[DataRow(0x35EA16C9, "Hello, world!")]
		[DataRow(0x35EA16B9, "Hello, world1")]
		[DataRow(0x360B16C9, "Hello, world!!")]
		[DataRow(unchecked((int) 0xC74DE200), "Hello, world!!!")]
		[TestMethod]
		public void TestStableStringHashCases(int expected, string? str)
			=> Assert.AreEqual(expected, str!.StableStringHash());

		[TestMethod]
		public void TestStableStringHashInjective()
		{
			// it's rather bad as a hash as you can see above, but fine as a checksum
			// taken from .NET 9 source, MIT-licensed, specifically https://github.com/dotnet/msbuild/blob/v17.12.6/src/Build.UnitTests/Evaluation/Expander_Tests.cs#L3907-L3940
			string[] stringsToHash = [ "cat1s", "cat1z", "bat1s", "cut1s", "cat1so", "cats1", "acat1s", "cat12s", "cat1s" ];
			var hashes = stringsToHash.Select(SE.StableStringHash).ToArray();
			for (var i = 0; i < hashes.Length; i++)
			{
				var a = hashes[i];
				for (var j = i; j < hashes.Length; j++)
				{
					var b = hashes[j];
					if (stringsToHash[i] == stringsToHash[j]) Assert.AreEqual(a, b, "Identical strings should hash to the same value.");
					else Assert.AreNotEqual(a, b, "Different strings should not hash to the same value.");
				}
			}
		}

		[TestMethod]
		public void TestSubstringAfter()
		{
			Assert.AreEqual("def", abcdef.SubstringAfter("bc", qrs));
			Assert.AreEqual(abcdef, abcdef.SubstringAfter(string.Empty, qrs));
			Assert.AreEqual(string.Empty, abcdef.SubstringAfter(abcdef, qrs));
			Assert.AreEqual(string.Empty, abcdef.SubstringAfter("f", qrs));
			Assert.AreEqual(string.Empty, "f".SubstringAfter("f", qrs));
			Assert.AreEqual("abcdab", "abcdabcdab".SubstringAfter("cd", qrs));
			Assert.AreEqual(qrs, abcdef.SubstringAfter("x", qrs));
			Assert.AreEqual(qrs, string.Empty.SubstringAfter("abc", qrs));
		}

		[TestMethod]
		public void TestSubstringAfterLast()
		{
			// fewer tests for SubstringAfterLast as its implementation should match SubstringAfter, save for using LastIndexOf

			Assert.AreEqual("ab", "abcdabcdab".SubstringAfterLast('d', qrs));
			Assert.AreEqual(qrs, "abcdabcdab".SubstringAfterLast('x', qrs));
		}

		[TestMethod]
		public void TestSubstringBefore()
		{
			Assert.AreEqual("abc", abcdef.SubstringBefore('d', qrs));
			Assert.AreEqual(string.Empty, abcdef.SubstringBefore('a', qrs));
			Assert.AreEqual(string.Empty, "a".SubstringBefore('a', qrs));
			Assert.AreEqual("abc", "abcdabcdab".SubstringBefore('d', qrs));
			Assert.AreEqual(qrs, abcdef.SubstringBefore('x', qrs));
			Assert.AreEqual(qrs, string.Empty.SubstringBefore('d', qrs));

			// fewer tests for SubstringBeforeLast as its implementation should match SubstringBefore, save for using LastIndexOf

			Assert.AreEqual("abcdabc", "abcdabcdab".SubstringBeforeLast('d', qrs));
			Assert.AreEqual(qrs, "abcdabcdab".SubstringBeforeLast('x', qrs));
		}
	}
}

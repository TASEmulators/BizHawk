using Microsoft.VisualStudio.TestTools.UnitTesting;

﻿using BizHawk.Common.StringExtensions;

namespace BizHawk.Common.Test
{
	[TestClass]
	public class SubstringExtensionTests
	{
		private const string abcdef = "abcdef";

		private const string qrs = "qrs";

#if false
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

		// no tests for RemovePrefixOrEmpty as its implementation should match RemovePrefix

		// no tests for RemovePrefixOrNull as its implementation should match RemovePrefix

		[TestMethod]
		public void TestRemoveSuffix()
		{
			Assert.AreEqual("abcde", abcdef.RemoveSuffix('f', qrs));
			Assert.AreEqual(string.Empty, "f".RemoveSuffix('f', qrs));
			Assert.AreEqual(qrs, abcdef.RemoveSuffix('d', qrs));
			Assert.AreEqual(qrs, abcdef.RemoveSuffix('x', qrs));
			Assert.AreEqual(qrs, string.Empty.RemoveSuffix('f', qrs));

			Assert.AreEqual("abc", abcdef.RemoveSuffix("def", qrs));
			Assert.AreEqual("abcde", abcdef.RemoveSuffix("f", qrs));
			Assert.AreEqual(abcdef, abcdef.RemoveSuffix(string.Empty, qrs));
			Assert.AreEqual(string.Empty, abcdef.RemoveSuffix(abcdef, qrs));
			Assert.AreEqual(string.Empty, "f".RemoveSuffix("f", qrs));
			Assert.AreEqual(qrs, abcdef.RemoveSuffix("d", qrs));
			Assert.AreEqual(qrs, abcdef.RemoveSuffix("x", qrs));
			Assert.AreEqual(qrs, string.Empty.RemoveSuffix("def", qrs));
		}

		// no tests for RemoveSuffixOrEmpty as its implementation should match RemoveSuffix

		// no tests for RemoveSuffixOrNull as its implementation should match RemoveSuffix

		[TestMethod]
		public void TestSubstringAfter()
		{
			Assert.AreEqual("def", abcdef.SubstringAfter('c', qrs));
			Assert.AreEqual(string.Empty, abcdef.SubstringAfter('f', qrs));
			Assert.AreEqual(string.Empty, "f".SubstringAfter('f', qrs));
			Assert.AreEqual("abcdab", "abcdabcdab".SubstringAfter('d', qrs));
			Assert.AreEqual(qrs, abcdef.SubstringAfter('x', qrs));
			Assert.AreEqual(qrs, string.Empty.SubstringAfter('c', qrs));

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

			Assert.AreEqual("ab", "abcdabcdab".SubstringAfterLast("cd", qrs));
			Assert.AreEqual(qrs, "abcdabcdab".SubstringAfterLast("x", qrs));
		}

		// no tests for SubstringAfterLastOrEmpty as its implementation should match SubstringAfterLast

		// no tests for SubstringAfterLastOrNull as its implementation should match SubstringAfterLast

		// no tests for SubstringAfterOrEmpty as its implementation should match SubstringAfter

		// no tests for SubstringAfterOrNull as its implementation should match SubstringAfter

		[TestMethod]
		public void TestSubstringBefore()
		{
			Assert.AreEqual("abc", abcdef.SubstringBefore('d', qrs));
			Assert.AreEqual(string.Empty, abcdef.SubstringBefore('a', qrs));
			Assert.AreEqual(string.Empty, "a".SubstringBefore('a', qrs));
			Assert.AreEqual("abc", "abcdabcdab".SubstringBefore('d', qrs));
			Assert.AreEqual(qrs, abcdef.SubstringBefore('x', qrs));
			Assert.AreEqual(qrs, string.Empty.SubstringBefore('d', qrs));

			Assert.AreEqual("abc", abcdef.SubstringBefore("de", qrs));
			Assert.AreEqual(string.Empty, abcdef.SubstringBefore(string.Empty, qrs));
			Assert.AreEqual(string.Empty, abcdef.SubstringBefore(abcdef, qrs));
			Assert.AreEqual(string.Empty, abcdef.SubstringBefore("a", qrs));
			Assert.AreEqual(string.Empty, "a".SubstringBefore("a", qrs));
			Assert.AreEqual("ab", "abcdabcdab".SubstringBefore("cd", qrs));
			Assert.AreEqual(qrs, abcdef.SubstringBefore("x", qrs));
			Assert.AreEqual(qrs, string.Empty.SubstringBefore("def", qrs));
		}

		[TestMethod]
		public void TestSubstringBeforeLast()
		{
			// fewer tests for SubstringBeforeLast as its implementation should match SubstringBefore, save for using LastIndexOf

			Assert.AreEqual("abcdabc", "abcdabcdab".SubstringBeforeLast('d', qrs));
			Assert.AreEqual(qrs, "abcdabcdab".SubstringBeforeLast('x', qrs));

			Assert.AreEqual("abcdab", "abcdabcdab".SubstringBeforeLast("cd", qrs));
			Assert.AreEqual(qrs, "abcdabcdab".SubstringBeforeLast("x", qrs));
		}

		// no tests for SubstringBeforeLastOrEmpty as its implementation should match SubstringBeforeLast

		// no tests for SubstringBeforeLastOrNull as its implementation should match SubstringBeforeLast

		// no tests for SubstringBeforeOrEmpty as its implementation should match SubstringBefore

		// no tests for SubstringBeforeOrNull as its implementation should match SubstringBefore
#endif
	}
}

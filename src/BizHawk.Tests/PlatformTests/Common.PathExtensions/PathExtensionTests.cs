using BizHawk.Common;
using BizHawk.Common.PathExtensions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using PE = BizHawk.Common.PathExtensions.PathExtensions;

namespace BizHawk.Tests.Common.PathExtensions
{
	[TestClass]
	public class PathExtensionTests
	{
		[TestMethod]
		public void TestNullability()
		{
			var p = OSTailoredCode.IsUnixHost ? "/" : @"C:\";

			Assert.IsFalse(PE.IsSubfolderOf(childPath: null, parentPath: p));
			Assert.IsFalse(PE.IsSubfolderOf(childPath: p, parentPath: null));
			Assert.IsFalse(PE.IsSubfolderOf(childPath: null, parentPath: null));

			Assert.IsNull(PE.GetRelativePath(fromPath: null, toPath: p));
			Assert.IsNull(PE.GetRelativePath(fromPath: p, toPath: null));
			Assert.IsNull(PE.GetRelativePath(fromPath: null, toPath: null));

			Assert.IsNull(PE.MakeRelativeTo(absolutePath: null, basePath: p));
			Assert.IsNull(PE.MakeRelativeTo(absolutePath: p, basePath: null));
			Assert.IsNull(PE.MakeRelativeTo(absolutePath: null, basePath: null));
		}

		[TestMethod]
		[DataRow(true, "/usr/share", "/usr")]
		[DataRow(false, "/usr/share", "/bin")]
		[DataRow(false, "/usr", "/usr/share")]
		[DataRow(true, "/usr", "/usr")]
		[DataRow(true, "/usr", "/usr/")]
		[DataRow(false, "/etc/rmdir", "/etc/rm")] // not naive StartsWith; these don't exist but the implementation uses `realpath -m` so they will be classed as two real and distinct dirs
		[DataRow(true, "/usr/lib64", "/usr/lib")] // symlink to same dir
		[DataRow(true, "/usr/lib64/gconv", "/usr/lib")] // same symlink, checking child
		public void TestIsSubfolderOfUnix(bool expectedIsSubfolder, string childPath, string parentPath)
		{
			PlatformTestUtils.OnlyRunOnRealUnix();

			Assert.AreEqual(expectedIsSubfolder, childPath.IsSubfolderOf(parentPath));
		}

		[TestMethod]
		[DataRow(true, @"C:\Users\Public", @"C:\Users")]
		[DataRow(false, @"C:\Users\Public", @"C:\Program Files")]
		[DataRow(false, @"C:\Users", @"C:\Users\Public")]
		[DataRow(true, @"C:\Users", @"C:\Users")]
		[DataRow(true, @"C:\Users", @"C:\Users\")]
		[DataRow(false, @"C:\Program Files (x86)", @"C:\Program Files")] // not naive StartsWith
		public void TestIsSubfolderOfWindows(bool expectedIsSubfolder, string childPath, string parentPath)
		{
			PlatformTestUtils.OnlyRunOnWindows();

			Assert.AreEqual(expectedIsSubfolder, childPath.IsSubfolderOf(parentPath));
		}

		[TestMethod]
		[DataRow("./share", "/usr/share", "/usr")]
		[DataRow(".", "/usr", "/usr")]
		[DataRow("..", "/usr", "/usr/share")]
		[DataRow("../bin", "/usr/bin", "/usr/share")]
		[DataRow("../../etc", "/etc", "/usr/share")]
		public void TestGetRelativePathUnix(string expectedRelPath, string toPath, string fromPath) // swapped here instead of in data
		{
			PlatformTestUtils.OnlyRunOnRealUnix();

			Assert.AreEqual(expectedRelPath, PE.GetRelativePath(fromPath: fromPath, toPath: toPath));
		}

		[TestMethod]
		[DataRow("./share", "/usr/share", "/usr")]
		[DataRow(".", "/usr", "/usr")]
		[DataRow("/usr", "/usr", "/usr/share")] // not `..`
		[DataRow("/usr/bin", "/usr/bin", "/usr/share")] // not `../bin`
		[DataRow("/etc", "/etc", "/usr/share")] // not `../../etc`
		public void TestMakeRelativeToUnix(string expectedRelPath, string absolutePath, string basePath)
		{
			PlatformTestUtils.OnlyRunOnRealUnix();

			Assert.AreEqual(expectedRelPath, absolutePath.MakeRelativeTo(basePath));
		}

		[TestMethod]
		[DataRow(@".\SysWOW64", @"C:\Windows\SysWOW64", @"C:\Windows")]
		[DataRow(@".", @"C:\Windows", @"C:\Windows")]
		[DataRow(@"..", @"C:\Windows", @"C:\Windows\SysWOW64")]
		[DataRow(@"..\System32", @"C:\Windows\System32", @"C:\Windows\SysWOW64")]
		[DataRow(@"..\..\Program Files", @"C:\Program Files", @"C:\Windows\SysWOW64")]
		public void TestGetRelativePathWindows(string expectedRelPath, string toPath, string fromPath) // swapped here instead of in data
		{
			PlatformTestUtils.OnlyRunOnWindows();

			Assert.AreEqual(expectedRelPath, PE.GetRelativePath(fromPath: fromPath, toPath: toPath));
		}

		[TestMethod]
		[DataRow(@".\SysWOW64", @"C:\Windows\SysWOW64", @"C:\Windows")]
		[DataRow(@".", @"C:\Windows", @"C:\Windows")]
		[DataRow(@"C:\Windows", @"C:\Windows", @"C:\Windows\SysWOW64")] // not `..`
		[DataRow(@"C:\Windows\System32", @"C:\Windows\System32", @"C:\Windows\SysWOW64")] // not `..\System32`
		[DataRow(@"C:\Program Files", @"C:\Program Files", @"C:\Windows\SysWOW64")] // not `..\..\Program Files`
		public void TestMakeRelativeToWindows(string expectedRelPath, string absolutePath, string basePath)
		{
			PlatformTestUtils.OnlyRunOnWindows();

			Assert.AreEqual(expectedRelPath, absolutePath.MakeRelativeTo(basePath));
		}
	}
}

using BizHawk.Common;
using BizHawk.Common.PathExtensions;

using PE = BizHawk.Common.PathExtensions.PathExtensions;

namespace BizHawk.Tests.Common.PathExtensions
{
	[TestClass]
	public class PathExtensionTests
	{
		[TestMethod]
		public void TestNullability()
		{
			PlatformTestUtils.RunEverywhere();

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
#if false // don't work on NixOS and probably other distros, presumably all 32-bit distros
		[DataRow(true, "/usr/lib64", "/usr/lib")] // symlink to same dir
		[DataRow(true, "/usr/lib64/gconv", "/usr/lib")] // same symlink, checking child
#endif
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
		[DataRow("./share", true, "/usr/share", "/usr")]
		[DataRow(".", true, "/usr", "/usr")]
		[DataRow("..", false, "/usr", "/usr/share")]
		[DataRow("../bin", false, "/usr/bin", "/usr/share")]
		[DataRow("../../etc", false, "/etc", "/usr/share")]
		public void TestGetRelativeUnix(string expectedRelPath, bool isChild, string absolutePath, string basePath)
		{
			PlatformTestUtils.OnlyRunOnRealUnix();

			Assert.AreEqual(expectedRelPath, PE.GetRelativePath(fromPath: basePath, toPath: absolutePath)); // params swapped w.r.t. `MakeRelativeTo`
			// `MakeRelativeTo` is supposed to return an absolute path (the receiver is assumed to be absolute) iff the receiver isn't a child of the given base path
			Assert.AreEqual(isChild ? expectedRelPath : absolutePath, absolutePath.MakeRelativeTo(basePath));
		}

		[TestMethod]
		[DataRow(@".\SysWOW64", true, @"C:\Windows\SysWOW64", @"C:\Windows")]
		[DataRow(@".", true, @"C:\Windows", @"C:\Windows")]
		[DataRow(@"..", false, @"C:\Windows", @"C:\Windows\SysWOW64")]
		[DataRow(@"..\System32", false, @"C:\Windows\System32", @"C:\Windows\SysWOW64")]
		[DataRow(@"..\..\Program Files", false, @"C:\Program Files", @"C:\Windows\SysWOW64")]
		public void TestGetRelativeWindows(string expectedRelPath, bool isChild, string absolutePath, string basePath)
		{
			PlatformTestUtils.OnlyRunOnWindows();

			Assert.AreEqual(expectedRelPath, PE.GetRelativePath(fromPath: basePath, toPath: absolutePath)); // params swapped w.r.t. `MakeRelativeTo`
			// `MakeRelativeTo` is supposed to return an absolute path (the receiver is assumed to be absolute) iff the receiver isn't a child of the given base path
			Assert.AreEqual(isChild ? expectedRelPath : absolutePath, absolutePath.MakeRelativeTo(basePath));
		}
	}
}

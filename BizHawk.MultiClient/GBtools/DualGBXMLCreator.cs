using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace BizHawk.MultiClient.GBtools
{
	public partial class DualGBXMLCreator : Form
	{
		public DualGBXMLCreator()
		{
			InitializeComponent();
		}

		// http://stackoverflow.com/questions/275689/how-to-get-relative-path-from-absolute-path
		public static string GetRelativePath(string fromPath, string toPath)
		{
			Win32.FileAttributes fromAttr = GetPathAttribute(fromPath);
			Win32.FileAttributes toAttr = GetPathAttribute(toPath);

			StringBuilder path = new StringBuilder(260); // MAX_PATH
			if (Win32.PathRelativePathTo(
				path,
				fromPath,
				fromAttr,
				toPath,
				toAttr) == false)
			{
				throw new ArgumentException("Paths must have a common prefix");
			}
			return path.ToString();
		}

		private static Win32.FileAttributes GetPathAttribute(string path)
		{
			DirectoryInfo di = new DirectoryInfo(path);
			if (di.Exists)
			{
				return Win32.FileAttributes.Directory;
			}

			FileInfo fi = new FileInfo(path);
			if (fi.Exists)
			{
				return Win32.FileAttributes.Normal;
			}

			throw new FileNotFoundException();
		}

		bool Recalculate()
		{
			string PathLeft = dualGBFileSelector1.GetName();
			string PathRight = dualGBFileSelector2.GetName();
			string Name = textBoxName.Name;

			if (string.IsNullOrWhiteSpace(PathLeft) || string.IsNullOrWhiteSpace(PathRight) || string.IsNullOrWhiteSpace(Name))
				return false;

			List<char> NewPathL = new List<char>();

			for (int i = 0; i < PathLeft.Length && i < PathRight.Length; i++)
			{
				if (PathLeft[i] == PathRight[i])
					NewPathL.Add(PathLeft[i]);
				else
					break;
			}
			string BasePath = new string(NewPathL.ToArray());
			if (string.IsNullOrWhiteSpace(BasePath))
				return false;
			BasePath = System.IO.Path.GetDirectoryName(BasePath);
			PathLeft = GetRelativePath(BasePath, PathLeft);
			PathRight = GetRelativePath(BasePath, PathRight);

			StringWriter XML = new StringWriter();
			XML.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\" ?>");
			XML.WriteLine("<BizHawk-XMLGame System=\"DGB\" Name=\"{0}\">", Name);
			XML.WriteLine("  <LoadAssets>");
			XML.WriteLine("    <LeftRom FileName=\"{0}\"/>", PathLeft);
			XML.WriteLine("    <RightRom FileName=\"{0}\"/>", PathRight);
			XML.WriteLine("  </LoadAssets>");
			XML.WriteLine("</BizHawk-XMLGame>");

			textBoxOutputDir.Text = BasePath;
			textBoxXML.Text = XML.ToString();
			return true;
		}
	}
}

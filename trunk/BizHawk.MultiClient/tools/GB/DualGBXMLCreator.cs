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
			try
			{
				string PathLeft = dualGBFileSelector1.GetName();
				string PathRight = dualGBFileSelector2.GetName();
				string Name = textBoxName.Text;

				if (string.IsNullOrWhiteSpace(PathLeft) || string.IsNullOrWhiteSpace(PathRight) || string.IsNullOrWhiteSpace(Name))
					throw new Exception("Blank Names");

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
					throw new Exception("Common path?");
				BasePath = Path.GetDirectoryName(BasePath);
				PathLeft = GetRelativePath(BasePath, PathLeft);
				PathRight = GetRelativePath(BasePath, PathRight);

				BasePath = Path.Combine(BasePath, Name) + ".xml";

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
				buttonOK.Enabled = true;
				return true;
			}
			catch (Exception e)
			{
				textBoxOutputDir.Text = "";
				textBoxXML.Text = "Failed!\n" + e.ToString();
				buttonOK.Enabled = false;
				return false;
			}
		}

		private void textBoxName_TextChanged(object sender, EventArgs e)
		{
			Recalculate();
		}

		private void dualGBFileSelector1_NameChanged(object sender, EventArgs e)
		{
			Recalculate();
		}

		private void dualGBFileSelector2_NameChanged(object sender, EventArgs e)
		{
			Recalculate();
		}

		private void buttonOK_Click(object sender, EventArgs e)
		{
			if (Recalculate())
			{
				using (var sw = new StreamWriter(textBoxOutputDir.Text))
				{
					sw.Write(textBoxXML.Text);
				}
				DialogResult = System.Windows.Forms.DialogResult.OK;
				Close();
			}
		}
	}
}

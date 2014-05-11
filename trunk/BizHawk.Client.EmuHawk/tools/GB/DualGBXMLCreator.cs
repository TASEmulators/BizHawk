using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using BizHawk.Client.Common;
using BizHawk.Emulation.Cores.Nintendo.Gameboy;

namespace BizHawk.Client.EmuHawk
{
	public partial class DualGBXMLCreator : Form
	{
		private bool _suspendRecalculate = false;

		public DualGBXMLCreator()
		{
			InitializeComponent();
		}

		// http://stackoverflow.com/questions/275689/how-to-get-relative-path-from-absolute-path
		public static string GetRelativePath(string fromPath, string toPath)
		{
			Win32.FileAttributes fromAttr = GetPathAttribute(fromPath);
			Win32.FileAttributes toAttr = GetPathAttribute(toPath);

			var path = new StringBuilder(260); // MAX_PATH
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
			var di = new DirectoryInfo(path.Split('|').First());
			if (di.Exists)
			{
				return Win32.FileAttributes.Directory;
			}

			var fi = new FileInfo(path.Split('|').First());
			if (fi.Exists)
			{
				return Win32.FileAttributes.Normal;
			}

			throw new FileNotFoundException();
		}

		private bool Recalculate()
		{
			if (_suspendRecalculate)
			{
				return false;
			}

			try
			{
				var PathLeft = dualGBFileSelector1.GetName();
				var PathRight = dualGBFileSelector2.GetName();
				var Name = textBoxName.Text;

				if (string.IsNullOrWhiteSpace(PathLeft) ||
					string.IsNullOrWhiteSpace(PathRight) ||
					string.IsNullOrWhiteSpace(Name))
				{
					throw new Exception("Blank Names");
				}

				var NewPathL = new List<char>();

				for (int i = 0; i < PathLeft.Length && i < PathRight.Length; i++)
				{
					if (PathLeft[i] == PathRight[i])
					{
						NewPathL.Add(PathLeft[i]);
					}
					else
					{
						break;
					}
				}

				var BasePath = new string(NewPathL.ToArray());
				if (string.IsNullOrWhiteSpace(BasePath))
				{
					throw new Exception("Common path?");
				}

				BasePath = Path.GetDirectoryName(BasePath.Split('|').First());
				PathLeft = GetRelativePath(BasePath, PathLeft);
				PathRight = GetRelativePath(BasePath, PathRight);

				BasePath = Path.Combine(BasePath, Name) + ".xml";

				var XML = new StringWriter();
				XML.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\" ?>");
				XML.WriteLine("<BizHawk-XMLGame System=\"DGB\" Name=\"{0}\">", Name);
				XML.WriteLine("  <LoadAssets>");
				XML.WriteLine("    <LeftRom FileName=\"{0}\"/>", PathLeft);
				XML.WriteLine("    <RightRom FileName=\"{0}\"/>", PathRight);
				XML.WriteLine("  </LoadAssets>");
				XML.WriteLine("</BizHawk-XMLGame>");

				textBoxOutputDir.Text = BasePath;
				textBoxXML.Text = XML.ToString();
				SaveRunButton.Enabled = true;
				return true;
			}
			catch (Exception e)
			{
				textBoxOutputDir.Text = string.Empty;
				textBoxXML.Text = "Failed!\n" + e.ToString();
				SaveRunButton.Enabled = false;
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

		private void DualGBXMLCreator_Load(object sender, EventArgs e)
		{
			CurrentForAllButton.Enabled = Global.Emulator != null && // For the designer
				(Global.Emulator is Gameboy) &&
				!string.IsNullOrEmpty(GlobalWin.MainForm.CurrentlyOpenRom) &&
				!GlobalWin.MainForm.CurrentlyOpenRom.Contains('|') && // Can't be archive
				!GlobalWin.MainForm.CurrentlyOpenRom.Contains(".xml"); // Can't already be an xml
		}

		private void SaveRunButton_Click(object sender, EventArgs e)
		{
			if (Recalculate())
			{
				using (var sw = new StreamWriter(textBoxOutputDir.Text))
				{
					sw.Write(textBoxXML.Text);
				}

				DialogResult = DialogResult.OK;
				Close();
				GlobalWin.MainForm.LoadRom(textBoxOutputDir.Text);
			}
		}

		private void CurrentForAllButton_Click(object sender, EventArgs e)
		{
			_suspendRecalculate = true;
			dualGBFileSelector1.SetName(GlobalWin.MainForm.CurrentlyOpenRom);
			dualGBFileSelector2.SetName(GlobalWin.MainForm.CurrentlyOpenRom);

			textBoxName.Text = Path.GetFileNameWithoutExtension(GlobalWin.MainForm.CurrentlyOpenRom);
			_suspendRecalculate = false;

			Recalculate();
		}

		private void buttonCancel_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			Close();
		}
	}
}

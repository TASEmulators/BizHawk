using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using BizHawk.Common;
using System.IO;

namespace BizHawk.Client.DBMan
{
	public partial class DATConverter : Form
	{
		public DATConverter()
		{
			InitializeComponent();

			var systems = Enum.GetValues(typeof(SystemType)).Cast<SystemType>().OrderBy(a => a.ToString()).ToList();

			comboBoxSystemSelect.DataSource = systems;

			textBoxOutputFolder.Text = System.IO.Path.GetDirectoryName(Application.ExecutablePath);
		}

		/// <summary>
		/// Choose output directory
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void button2_Click(object sender, EventArgs e)
		{
			var fbd = new FolderBrowserDialog();
			fbd.ShowNewFolderButton = true;
			fbd.Description = "Choose a new output folder";
			if (fbd.ShowDialog() == DialogResult.OK)
			{
				textBoxOutputFolder.Text = fbd.SelectedPath;
			}
		}

		/// <summary>
		/// Add import files to the list box
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void buttonAddFiles_Click(object sender, EventArgs e)
		{
			var ofd = new OpenFileDialog();
			ofd.CheckFileExists = true;
			ofd.CheckPathExists = true;
			ofd.InitialDirectory = System.IO.Path.GetDirectoryName(Application.ExecutablePath);
			ofd.Multiselect = true;
			
			if (ofd.ShowDialog() == DialogResult.OK)
			{
				foreach (var f in ofd.FileNames)
				{
					if (!listBoxFiles.Items.Contains((f)))
					{
						listBoxFiles.Items.Add(f);
					}
				}
			}
		}

		/// <summary>
		/// Removes selected input files from the listbox
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void buttonRemove_Click(object sender, EventArgs e)
		{
			List<string> files = new List<string>();
			foreach (var s in listBoxFiles.SelectedItems)
			{
				files.Add(s.ToString());
			}

			if (files.Count > 0)
			{
				foreach (var s in files)
					listBoxFiles.Items.Remove(s);
			}
		}

		/// <summary>
		/// Attempt to process all selected files
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void buttonStartProcessing_Click(object sender, EventArgs e)
		{
			// initial checks
			var checkedBtn = groupImportTypes.Controls.OfType<RadioButton>().FirstOrDefault(r => r.Checked);
			if (checkedBtn == null)
			{
				MessageBox.Show("You need to select an import type.");
				return;
			}

			if (!Directory.Exists(textBoxOutputFolder.Text))
			{
				MessageBox.Show("Chosen output folder is not valid");
				return;
			}

			if (listBoxFiles.Items.Count == 0)
			{
				MessageBox.Show("No files chosen for input");
				return;
			}

			List<string> files = new List<string>();

			foreach (var s in listBoxFiles.Items)
			{
				if (s.ToString().Trim() == "")
				{
					MessageBox.Show("The selected file: " + s.ToString() + "Cannot be found.\n\nSort this out and try again");
					return;
				}

				files.Add((string)s);
			}

			string res = "";

			if (radioTOSEC.Checked)
			{
				DATParser tp = new TOSECParser((SystemType)Enum.Parse(typeof(SystemType), comboBoxSystemSelect.SelectedValue.ToString()));
				res = tp.ParseDAT(files.ToArray());
			}
			else if (radioNOINTRO.Checked)
			{
				DATParser dp = new NOINTROParser((SystemType)Enum.Parse(typeof(SystemType), comboBoxSystemSelect.SelectedValue.ToString()));
				res = dp.ParseDAT(files.ToArray());
			}

			string fName = "gamedb_" + 
				GameDB.GetSystemCode((SystemType)Enum.Parse(typeof(SystemType), comboBoxSystemSelect.SelectedValue.ToString())) +
				"_DevExport_" + DateTime.UtcNow.ToString("yyyy-MM-dd_HH_mm_ss") + ".txt";

			try
			{
				File.WriteAllText(Path.Combine(textBoxOutputFolder.Text, fName), res);
			}
			catch (Exception ex)
			{
				MessageBox.Show("Error writing file: " + fName + "\n\n" + ex.Message);
			}

		}
	}
}

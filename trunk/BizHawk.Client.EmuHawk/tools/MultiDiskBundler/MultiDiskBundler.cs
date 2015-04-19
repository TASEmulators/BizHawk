using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace BizHawk.Client.EmuHawk
{
	public partial class MultiDiskBundler : Form, IToolFormAutoConfig
	{
		public MultiDiskBundler()
		{
			InitializeComponent();
		}

		private void MultiGameCreator_Load(object sender, EventArgs e)
		{

		}

		#region IToolForm

		public void UpdateValues()
		{

		}

		public void FastUpdate()
		{

		}

		public void Restart()
		{

		}

		public bool AskSaveChanges()
		{
			return true;
		}

		public bool UpdateBefore
		{
			get { return true; }
		}

		#endregion

		private void buttonCancel_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			Close();
		}

		private void SaveRunButton_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.OK;
			Close();
		}

		private void AddButton_Click(object sender, EventArgs e)
		{
			int start = 5 + (FileSelectorPanel.Controls.Count * 43);

			var groupBox = new GroupBox
			{
				Text = "",
				Location = new Point(5, start),
				Size = new Size(435, 38),
				Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
			};

			groupBox.Controls.Add(new DualGBFileSelector
			{
				Location = new Point(5, 8)
			});

			FileSelectorPanel.Controls.Add(groupBox);
		}

		
	}
}

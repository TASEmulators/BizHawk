using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using BizHawk.DiscSystem;

namespace BizHawk
{
	public partial class MainDiscoForm : Form
	{
		public MainDiscoForm()
		{
			InitializeComponent();
		}

		private class DiscRecord
		{
			public Disc Disc;
			public string BaseName;
		}

		private void MainDiscoForm_Load(object sender, EventArgs e)
		{

		}

		private void ExitButton_Click(object sender, EventArgs e)
		{
			this.Close();
		}

		private void lblMagicDragArea_DragDrop(object sender, DragEventArgs e)
		{

		}

		private void lblMagicDragArea_DragEnter(object sender, DragEventArgs e)
		{

		}
	}
}

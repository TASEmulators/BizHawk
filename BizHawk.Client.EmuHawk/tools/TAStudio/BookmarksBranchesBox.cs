using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace BizHawk.Client.EmuHawk
{
	public partial class BookmarksBranchesBox : UserControl
	{
		public TAStudio Tastudio { get; set; }

		public BookmarksBranchesBox()
		{
			InitializeComponent();
			BranchView.QueryItemText += QueryItemText;
			BranchView.QueryItemBkColor += QueryItemBkColor;
		}

		private void QueryItemText(int index, int column, out string text)
		{
			text = string.Empty;
		}

		private void QueryItemBkColor(int index, int column, ref Color color)
		{
			
		}
	}
}

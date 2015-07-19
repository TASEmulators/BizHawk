using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using BizHawk.Emulation.Common;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class BookmarksBranchesBox : UserControl
	{
		public TAStudio Tastudio { get; set; }

		public TasBranchCollection Branches
		{
			get { return Tastudio.CurrentTasMovie.TasBranches; }
		}

		public BookmarksBranchesBox()
		{
			InitializeComponent();
			BranchView.QueryItemText += QueryItemText;
			BranchView.QueryItemBkColor += QueryItemBkColor;
		}

		public TasBranch SelectedBranch
		{
			get
			{
				if (BranchView.SelectedIndices.Count > 0)
				{
					return Branches[BranchView.SelectedIndices[0]];
				}

				return null;
			}
		}

		private void QueryItemText(int index, int column, out string text)
		{
			text = string.Empty;

			var columnName = BranchView.Columns[column].Name;

			if (index >= Tastudio.CurrentTasMovie.TasBranches.Count)
			{
				return;
			}

			switch (column)
			{
				case 0: // BranchNumberColumn
					text = index.ToString();
					break;
				case 1: // FrameColumn
					text = Branches[index].Frame.ToString();
					break;
				case 2: // TimeColumn
					text = "TODO";
					break;
			}
		}

		private void QueryItemBkColor(int index, int column, ref Color color)
		{
			
		}

		private void AddContextMenu_Click(object sender, EventArgs e)
		{
			// TODO: don't use Global.Emulator
			var branch = new TasBranch
			{
				Frame = Global.Emulator.Frame,
				CoreData = (Global.Emulator as IStatable).SaveStateBinary(),
				InputLog = Tastudio.CurrentTasMovie.InputLog.ToList(),
				OSDFrameBuffer = GlobalWin.MainForm.CurrentFrameBuffer(captureOSD: true)
			};

			Branches.Add(branch);
			BranchView.ItemCount = Branches.Count;
		}

		private void BranchView_MouseDoubleClick(object sender, MouseEventArgs e)
		{
			if (SelectedBranch != null)
			{
				LoadBranch(SelectedBranch);
			}
		}

		private void BranchesContextMenu_Opening(object sender, CancelEventArgs e)
		{
			RemoveBranchContextMenuItem.Enabled = SelectedBranch != null;
		}

		private void RemoveBranchContextMenuItem_Click(object sender, EventArgs e)
		{
			if (SelectedBranch != null)
			{
				Branches.Remove(SelectedBranch);
				BranchView.ItemCount = Branches.Count;
			}
		}

		private void LoadBranch(TasBranch branch)
		{
			MessageBox.Show("TODO: load this branch");
		}
	}
}

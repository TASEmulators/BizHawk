using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace BizHawk.MultiClient
{
	public partial class ArchiveChooser : Form
	{
		public ArchiveChooser(HawkFile hawkfile)
		{
			InitializeComponent();
			foreach (var item in hawkfile.ArchiveItems)
			{
				var lvi = new ListViewItem();
				lvi.Tag = item;
				lvi.SubItems.Add(new ListViewItem.ListViewSubItem());
				lvi.Text = Util.FormatFileSize(item.size);
				lvi.SubItems[1].Text = item.name;

				if (IsVerifiedUSA(item.name))
				{
					lvMembers.Items.Insert(0, lvi);
				}
				else
				{
					lvMembers.Items.Add(lvi);
				}
				
			}
		}

		private bool IsVerifiedUSA(string name)
		{
			if (name.Contains("(U)") && name.Contains("[!]"))
				return true;
			else
				return false;
		}

		public int SelectedMemberIndex
		{
			get
			{
				if (lvMembers.SelectedIndices.Count == 0) return -1;
				var ai = lvMembers.SelectedItems[0].Tag as HawkFile.ArchiveItem;
				return ai.index;
			}
		}

		private void btnOK_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.OK;
			Close();
		}

		private void btnCancel_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
		}

		private void lvMembers_ItemActivate(object sender, EventArgs e)
		{
			DialogResult = DialogResult.OK;
			Close();
		}

		private void ArchiveChooser_Load(object sender, EventArgs e)
		{
			lvMembers.Items[0].Selected = true;
		}
	}
}

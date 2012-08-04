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
				lvMembers.Items.Add(lvi);
			}

			SortItems();
		}

		private bool IsVerifiedRegion(string name, string region)
		{
			if (name.Contains(region) && name.Contains("[!]"))
				return true;
			else
				return false;
		}

		private bool IsUnverifiedRegion(string name, string region)
		{
			if (name.Contains(region) && !(name.Contains("[!]")))
				return true;
			else
				return false;
		}

		private bool IsNotRegion(string name)
		{
			if (name.Contains("(W)")) return false;
			if (name.Contains("(JU)")) return false;
			if (name.Contains("(U)")) return false;
			if (name.Contains("(J)")) return false;
			if (name.Contains("(E)")) return false;
			return true;
		}

		private void SortItems()
		{
			List<ListViewItem> lvitems = new List<ListViewItem>();
			foreach(ListViewItem item in lvMembers.Items)
			{
				lvitems.Add(item);
			}

			List<ListViewItem> sorteditems = new List<ListViewItem>();

			sorteditems.AddRange(lvitems.Where(x => IsVerifiedRegion(x.SubItems[1].Text, "(W)")).OrderBy(x => x.Name).ToList());
			sorteditems.AddRange(lvitems.Where(x => IsUnverifiedRegion(x.SubItems[1].Text, "(W)")).OrderBy(x => x.Name).ToList());

			sorteditems.AddRange(lvitems.Where(x => IsVerifiedRegion(x.SubItems[1].Text, "(JU)")).OrderBy(x => x.Name).ToList());
			sorteditems.AddRange(lvitems.Where(x => IsUnverifiedRegion(x.SubItems[1].Text, "(JU)")).OrderBy(x => x.Name).ToList());

			sorteditems.AddRange(lvitems.Where(x => IsVerifiedRegion(x.SubItems[1].Text, "(U)")).OrderBy(x => x.Name).ToList());
			sorteditems.AddRange(lvitems.Where(x => IsUnverifiedRegion(x.SubItems[1].Text, "(U)")).OrderBy(x => x.Name).ToList());

			sorteditems.AddRange(lvitems.Where(x => IsVerifiedRegion(x.SubItems[1].Text, "(J)")).OrderBy(x => x.Name).ToList());
			sorteditems.AddRange(lvitems.Where(x => IsUnverifiedRegion(x.SubItems[1].Text, "(J)")).OrderBy(x => x.Name).ToList());

			sorteditems.AddRange(lvitems.Where(x => IsVerifiedRegion(x.SubItems[1].Text, "(E)")).OrderBy(x => x.Name).ToList());
			sorteditems.AddRange(lvitems.Where(x => IsUnverifiedRegion(x.SubItems[1].Text, "(E)")).OrderBy(x => x.Name).ToList());

			sorteditems.AddRange(lvitems.Where(x => IsNotRegion(x.SubItems[1].Text)).ToList());

			lvMembers.Items.Clear();
			foreach (ListViewItem i in sorteditems)
			{
				lvMembers.Items.Add(i);
			}
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

		private void lvMembers_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.D1) SetItem(1);
			if (e.KeyCode == Keys.D2) SetItem(2);
			if (e.KeyCode == Keys.D3) SetItem(3);
			if (e.KeyCode == Keys.D4) SetItem(4);
			if (e.KeyCode == Keys.D5) SetItem(5);
			if (e.KeyCode == Keys.D6) SetItem(6);
			if (e.KeyCode == Keys.D7) SetItem(7);
			if (e.KeyCode == Keys.D8) SetItem(8);
			if (e.KeyCode == Keys.D9) SetItem(9);
		}

		private void SetItem(int num)
		{
			if (num <= lvMembers.Items.Count)
			{
				foreach (ListViewItem item in lvMembers.SelectedItems)
				{
					item.Selected = false;
				}
				lvMembers.Items[num - 1].Selected = true;
			}
		}
	}
}

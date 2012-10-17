using System;
using System.IO;
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
				lvi.Text = item.name;
				long size = item.size;
				if (size % 1024 == 16 && Path.GetExtension(item.name).ToUpper() == ".NES")
					size -= 16;
				lvi.SubItems[1].Text = Util.FormatFileSize(size);
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

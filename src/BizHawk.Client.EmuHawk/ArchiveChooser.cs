using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

using BizHawk.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class ArchiveChooser : Form
	{
		private readonly IList<ListViewItem> _archiveItems = new List<ListViewItem>();
		private readonly ToolTip _errorBalloon = new ToolTip();

		private static bool _useRegEx;
		private static bool _matchWhileTyping = true;

		public ArchiveChooser(HawkFile hawkFile)
		{
			InitializeComponent();

			_errorBalloon.IsBalloon = true;
			_errorBalloon.InitialDelay = 0;
			radRegEx.Checked = _useRegEx;
			cbInstantFilter.Checked = _matchWhileTyping;

			var items = hawkFile.ArchiveItems;
			for (int i = 0; i < items.Count; i++)
			{
				var item = items[i];
				var lvi = new ListViewItem { Tag = i };
				lvi.SubItems.Add(new ListViewItem.ListViewSubItem());
				lvi.Text = item.Name;
				long size = item.Size;
				var extension = Path.GetExtension(item.Name);
				if (extension != null && size % 1024 == 16 && extension.ToUpperInvariant() == ".NES")
					size -= 16;
				lvi.SubItems[1].Text = Util.FormatFileSize(size);
				_archiveItems.Add(lvi);
			}

			InitializeFileView();
		}

		private void InitializeFileView()
		{
			lvMembers.BeginUpdate();
			try
			{
				lvMembers.Items.Clear();
				foreach (ListViewItem i in _archiveItems.OrderBy(x => x.Name))
				{
					lvMembers.Items.Add(i);
				}
			}
			finally
			{
				lvMembers.EndUpdate();
			}
		}

		public int SelectedMemberIndex
		{
			get
			{
				if (lvMembers.SelectedIndices.Count == 0) return -1;
				int? ai = lvMembers.SelectedItems[0].Tag as int?;
				return ai ?? -1;
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
			tbFilter.Select();
		}

		private void btnSearch_Click(object sender, EventArgs e)
		{
			StartMatching(tbSearch, DoSearch);
		}

		private void cbInstantFilter_CheckedChanged(object sender, EventArgs e)
		{
			_matchWhileTyping = cbInstantFilter.Checked;
		}

		private void radRegEx_CheckedChanged(object sender, EventArgs e)
		{
			_useRegEx = radRegEx.Checked;
		}

		private void tbFilter_TextChanged(object sender, EventArgs e)
		{
			if (cbInstantFilter.Checked)
			{
				btnFilter_Click(sender, e);
			}
		}

		private void btnFilter_Click(object sender, EventArgs e)
		{
			StartMatching(tbFilter, DoFilter);
		}

		private void StartMatching(TextBox tb, Action<IMatcher> func)
		{
			try
			{
				_errorBalloon.Hide(tb);
				var searchMatcher = CreateMatcher(tb.Text);
				if (searchMatcher != null)
				{
					func(searchMatcher);
				}
			}
			catch (ArgumentException ex)
			{
				string errMsg = ex.Message;
				errMsg = errMsg.Substring(startIndex: errMsg.IndexOf('-') + 2);

				// Balloon is bugged on first invocation
				_errorBalloon.Show($"Error parsing RegEx: {errMsg}", tb);
				_errorBalloon.Show($"Error parsing RegEx: {errMsg}", tb);
			}
		}

		private void DoSearch(IMatcher searchMatcher)
		{
			int count = lvMembers.Items.Count;
			int searchStartIdx = 0;
			if (lvMembers.SelectedItems.Count > 0)
			{
				searchStartIdx = (lvMembers.SelectedIndices[0] + 1) % count;
			}
			int? searchResultIdx = null;

			for (int i = 0; i < count; ++i)
			{
				int curIdx = (searchStartIdx + i) % count;
				if (searchMatcher.Matches(lvMembers.Items[curIdx]))
				{
					searchResultIdx = curIdx;
					break;
				}
			}
			if (searchResultIdx != null)
			{
				lvMembers.Select();
				lvMembers.Items[searchResultIdx.Value].Selected = true;
			}
			else
			{
				// Balloon is bugged on first invocation
				_errorBalloon.Show("Could not find search text", tbSearch);
				_errorBalloon.Show("Could not find search text", tbSearch);
			}
		}

		private void DoFilter(IMatcher searchMatcher)
		{
			lvMembers.BeginUpdate();
			try
			{
				lvMembers.Items.Clear();
				foreach (ListViewItem item in _archiveItems)
				{
					if (searchMatcher.Matches(item))
					{
						lvMembers.Items.Add(item);
					}
				}
			}
			finally
			{
				lvMembers.EndUpdate();
			}
		}

		private interface IMatcher
		{
			bool Matches(ListViewItem value);
		}

		private class SimpleMatcher : IMatcher
		{
			public string[] Keys { get; set; }
			public bool Matches(ListViewItem value)
			{
				string searchedStr = value.Text.ToLowerInvariant();
				foreach (string key in Keys)
				{
					if (!searchedStr.Contains(key))
					{
						return false;
					}
				}

				return true;
			}
		}

		private class RegExMatcher : IMatcher
		{
			public Regex Matcher { get; set; }
			public bool Matches(ListViewItem value)
			{
				return Matcher.IsMatch(value.Text);
			}
		}

		private IMatcher CreateMatcher(string searchKey)
		{
			if (radSimple.Checked)
			{
				return new SimpleMatcher
				{
					Keys = searchKey.ToLowerInvariant().Split(Array.Empty<char>(), StringSplitOptions.RemoveEmptyEntries) // splits on all whitespace chars
				};
			}
			else
			{
				return new RegExMatcher { Matcher = new Regex(searchKey, RegexOptions.IgnoreCase) };
			}
		}
	}
}

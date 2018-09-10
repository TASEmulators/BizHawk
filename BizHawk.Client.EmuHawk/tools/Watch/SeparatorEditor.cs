using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Client.Common;
using Emu = BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class SeparatorEditor : Form
	{
		public enum Mode { New, Duplicate, Edit };

		private readonly List<Watch> _watchList = new List<Watch>();

		private bool _loading = true;

		public List<Watch> Watches { get { return _watchList; } }
		public Point InitialLocation = new Point(0, 0);

		public SeparatorEditor()
		{
			InitializeComponent();
		}

		private void RamWatchNewWatch_Load(object sender, EventArgs e)
		{
			if (InitialLocation.X > 0 || InitialLocation.Y > 0)
			{
				Location = InitialLocation;
			}
			_loading = false;

            NotesBox.Text = _watchList[0].Notes;            
		}

		public void SetWatch(IEnumerable<Watch> watches = null)
		{
			if (watches != null)
			{
				_watchList.AddRange(watches);
			}
		}

		#region Events

		private void Cancel_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			Close();
		}

		private void Ok_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.OK;
            DoEdit();
            Close();
		}

		private void DoEdit()
		{
			if (_watchList.Count == 1)
			{
				_watchList[0].Notes = NotesBox.Text;
			}
		}
		
		#endregion
	}
}

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace BizHawk.Client.EmuHawk
{
	/// <summary>
	/// Shows a list of a user's current leaderboards
	/// </summary>
	public partial class RCheevosLeaderboardListForm : Form
	{
		private RCheevos.LBoard[] _lboards = [ ];
		private RCheevosLeaderboardForm[] _lboardForms = [ ];

		private readonly int _controlHeight;

		public RCheevosLeaderboardListForm()
		{
			using var temp = new RCheevosLeaderboardForm();
			_controlHeight = temp.Height + temp.Margin.Bottom + temp.Margin.Top;

			InitializeComponent();
			FormClosing += RCheevosLeaderboardListForm_FormClosing;
			flowLayoutPanel1.BoundScrollBar = vScrollBar1;
		}

		private void DisposeLboardForms()
		{
			foreach (var lboardForm in _lboardForms)
			{
				lboardForm.Dispose();
			}
		}

		public void Restart(IEnumerable<RCheevos.LBoard> lboards)
		{
			_lboards = lboards.ToArray();
			flowLayoutPanel1.Controls.Clear();

			RCheevosLeaderboardListForm_SizeChanged(this, EventArgs.Empty);
			vScrollBar1.Value = 0;
			vScrollBar1.Maximum = _controlHeight * _lboards.Length;
		}

		public void OnFrameAdvance()
		{
			foreach (var lb in _lboardForms)
			{
				lb.OnFrameAdvance();
			}
		}

		private void RCheevosLeaderboardListForm_FormClosing(object sender, FormClosingEventArgs e)
		{
			Hide();
			e.Cancel = true;
		}

		private void UpdateForms()
		{
			int firstIndex = vScrollBar1.Value / _controlHeight;
			int indexOffset = vScrollBar1.Value % _controlHeight;
			while (firstIndex > _lboards.Length - _lboardForms.Length)
			{
				firstIndex--;
				indexOffset += _controlHeight;
			}
			flowLayoutPanel1.SuspendDrawing();
			flowLayoutPanel1.SuspendLayout();
			bool refresh = flowLayoutPanel1.AutoScrollPosition.Y != -indexOffset;
			for (int i = 0; i < _lboardForms.Length; i++)
			{
				refresh |= _lboardForms[i].UpdateLBoard(_lboards[firstIndex + i]);
			}
			flowLayoutPanel1.AutoScrollPosition = new Point(0, indexOffset);
			flowLayoutPanel1.ResumeLayout();
			flowLayoutPanel1.ResumeDrawing();
			if (refresh)
			{
				Refresh();
			}
		}

		private void vScrollBar1_ValueChanged(object sender, EventArgs e) => UpdateForms();

		private int DisplayedItems()
		{
			return Math.Min((int) Math.Ceiling((double) flowLayoutPanel1.Height / _controlHeight) + 1, _lboards.Length);
		}

		private void RCheevosLeaderboardListForm_SizeChanged(object sender, EventArgs e)
		{
			if (flowLayoutPanel1.Controls.Count != DisplayedItems())
			{
				flowLayoutPanel1.Controls.Clear();
				DisposeLboardForms();
				_lboardForms = new RCheevosLeaderboardForm[DisplayedItems()];
				for (int i = 0; i < DisplayedItems(); i++)
				{
					_lboardForms[i] = new RCheevosLeaderboardForm();
				}
				flowLayoutPanel1.Controls.AddRange(_lboardForms);
			}

			UpdateForms();
		}
	}
}


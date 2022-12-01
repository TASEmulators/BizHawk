using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace BizHawk.Client.EmuHawk
{
	/// <summary>
	/// Shows a list of a user's current leaderboards
	/// </summary>
	public partial class RCheevosLeaderboardListForm : Form
	{
		public bool IsShown { get; private set; }

		private RCheevosLeaderboardForm[] _lboardForms;
		private int _updateCooldown;

		public RCheevosLeaderboardListForm()
		{
			InitializeComponent();
			FormClosing += RCheevosLeaderboardListForm_FormClosing;
			Shown += (_, _) => IsShown = true;
			_lboardForms = Array.Empty<RCheevosLeaderboardForm>();
			_updateCooldown = 5; // only update every 5 frames / 12 fps (as this is rather expensive to update)
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
			flowLayoutPanel1.Controls.Clear();
			DisposeLboardForms();
			var lboardForms = new List<RCheevosLeaderboardForm>();
			foreach (var lboard in lboards)
			{
				lboardForms.Add(new(lboard));
			}
			_lboardForms = lboardForms.ToArray();
			flowLayoutPanel1.Controls.AddRange(_lboardForms);
		}

		public void OnFrameAdvance(bool forceUpdate = false)
		{
			_updateCooldown--;
			if (_updateCooldown == 0 || forceUpdate)
			{
				_updateCooldown = 5;

				for (int i = 0; i < _lboardForms.Length; i++)
				{
					_lboardForms[i].OnFrameAdvance();
				}
			}
		}

		private void RCheevosLeaderboardListForm_FormClosing(object sender, FormClosingEventArgs e)
		{
			Hide();
			e.Cancel = true;
			IsShown = false;
		}
	}
}


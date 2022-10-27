using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace BizHawk.Client.EmuHawk
{
	/// <summary>
	/// Shows a list of a user's current achievements
	/// </summary>
	public partial class RCheevosAchievementListForm : Form
	{
		public bool IsShown { get; private set; }

		private RCheevosAchievementForm[] _cheevoForms;
		private int _updateCooldown;

		public RCheevosAchievementListForm()
		{
			InitializeComponent();
			FormClosing += RCheevosAchievementListForm_FormClosing;
			Shown += (_, _) => IsShown = true;
			_updateCooldown = 5; // only update every 5 frames / 12 fps (as this is rather expensive to update)
		}

		public void Restart(IEnumerable<RCheevos.Cheevo> cheevos, Func<int, string> getCheevoProgress)
		{
			flowLayoutPanel1.Controls.Clear();
			var cheevoForms = new List<RCheevosAchievementForm>();
			foreach (var cheevo in cheevos)
			{
				cheevoForms.Add(new(cheevo, getCheevoProgress));
			}
			_cheevoForms = cheevoForms.OrderByDescending(f => f.OrderByKey()).ToArray();
			flowLayoutPanel1.Controls.AddRange(_cheevoForms);
		}

		public void OnFrameAdvance(Func<int, RCheevos.Cheevo> getCheevoById, bool hardcore, bool forceUpdate = false)
		{
			_updateCooldown--;
			if (_updateCooldown == 0 || forceUpdate)
			{
				_updateCooldown = 5;

				for (int i = 0; i < _cheevoForms.Length; i++)
				{
					_cheevoForms[i].OnFrameAdvance(getCheevoById(_cheevoForms[i].CheevoID), hardcore);
				}
			}
		}

		private void RCheevosAchievementListForm_FormClosing(object sender, FormClosingEventArgs e)
		{
			Hide();
			e.Cancel = true;
			IsShown = false;
		}
	}
}


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
			_cheevoForms = Array.Empty<RCheevosAchievementForm>();
			_updateCooldown = 5; // only update every 5 frames / 12 fps (as this is rather expensive to update)
		}

		private void DisposeCheevoForms()
		{
			foreach (var cheevoForm in _cheevoForms)
			{
				cheevoForm.Dispose();
			}
		}

		public void Restart(IEnumerable<RCheevos.Cheevo> cheevos, Func<uint, string> getCheevoProgress)
		{
			flowLayoutPanel1.Controls.Clear();
			DisposeCheevoForms();
			var cheevoForms = new List<RCheevosAchievementForm>();
			foreach (var cheevo in cheevos)
			{
				cheevoForms.Add(new(cheevo, getCheevoProgress));
			}
			_cheevoForms = cheevoForms.OrderByDescending(f => f.OrderByKey()).ToArray();
			flowLayoutPanel1.Controls.AddRange(_cheevoForms);
		}

		public void OnFrameAdvance(bool hardcore, bool forceUpdate = false)
		{
			_updateCooldown--;
			if (_updateCooldown == 0 || forceUpdate)
			{
				_updateCooldown = 5;

				foreach (var form in _cheevoForms)
				{
					form.OnFrameAdvance(hardcore);
				}

				var reorderedForms = _cheevoForms.OrderByDescending(f => f.OrderByKey()).ToArray();

				for (var i = 0; i < _cheevoForms.Length; i++)
				{
					if (_cheevoForms[i] != reorderedForms[i])
					{
						flowLayoutPanel1.Controls.SetChildIndex(reorderedForms[i], i);
					}
				}

				_cheevoForms = reorderedForms;
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


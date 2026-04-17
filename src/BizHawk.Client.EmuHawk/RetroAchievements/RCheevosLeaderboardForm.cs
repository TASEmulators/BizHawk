using System.Windows.Forms;

namespace BizHawk.Client.EmuHawk
{
	/// <summary>
	/// Shows information about a specific leaderboard
	/// </summary>
	public partial class RCheevosLeaderboardForm : Form
	{
		private RCheevos.LBoard _lboard;

		public RCheevosLeaderboardForm()
		{
			InitializeComponent();
			TopLevel = false;
			Show();
		}

		public bool UpdateLBoard(RCheevos.LBoard lboard)
		{
			bool updated = _lboard != lboard;

			_lboard = lboard;

			titleBox.Text = lboard.Title;
			descriptionBox.Text = lboard.Description;
			lowerIsBetterBox.Checked = lboard.LowerIsBetter;

			if (scoreBox.Text != _lboard.Score)
			{
				scoreBox.Text = _lboard.Score;
				updated = true;
			}

			return updated;
		}

		public void OnFrameAdvance()
		{
			scoreBox.Text = _lboard.Score;
		}
	}
}


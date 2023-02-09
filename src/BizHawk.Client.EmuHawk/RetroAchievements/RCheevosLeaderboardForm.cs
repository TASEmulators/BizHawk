using System.Windows.Forms;

namespace BizHawk.Client.EmuHawk
{
	/// <summary>
	/// Shows information about a specific leaderboard
	/// </summary>
	public partial class RCheevosLeaderboardForm : Form
	{
		private readonly RCheevos.LBoard _lboard;

		public RCheevosLeaderboardForm(RCheevos.LBoard lboard)
		{
			InitializeComponent();
			titleBox.Text = lboard.Title;
			descriptionBox.Text = lboard.Description;
			scoreBox.Text = lboard.Score;
			lowerIsBetterBox.Checked = lboard.LowerIsBetter;
			_lboard = lboard;
			TopLevel = false;
			Show();
		}

		public void OnFrameAdvance()
		{
			scoreBox.Text = _lboard.Score;
		}
	}
}


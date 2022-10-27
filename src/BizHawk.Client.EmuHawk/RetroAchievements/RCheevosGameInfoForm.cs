using System.Drawing;
using System.Windows.Forms;

namespace BizHawk.Client.EmuHawk
{
	/// <summary>
	/// Shows current RetroAchievements game info
	/// </summary>
	public partial class RCheevosGameInfoForm : Form
	{
		public bool IsShown { get; private set; }

		private bool _iconLoaded;

		public RCheevosGameInfoForm()
		{
			InitializeComponent();
			FormClosing += RCheevosGameInfoForm_FormClosing;
			Shown += (_, _) => IsShown = true;
		}

		public void Restart(string gameTitle, string gameHash, int totalPoints)
		{
			titleTextBox.Text = gameTitle;
			hashBox.Text = gameHash;
			totalPointsBox.Text = totalPoints.ToString();
			currentLboardBox.Text = "N/A";
			_iconLoaded = false;
		}

		public void OnFrameAdvance(Bitmap gameIcon, int totalPoints, string lboardStr)
		{
			// probably bad idea to set this every frame, so
			if (!_iconLoaded && gameIcon is not null)
			{
				gameIconBox.Image = gameIcon;
				_iconLoaded = true;
			}

			totalPointsBox.Text = totalPoints.ToString();
			currentLboardBox.Text = lboardStr;
		}

		private void RCheevosGameInfoForm_FormClosing(object sender, FormClosingEventArgs e)
		{
			Hide();
			e.Cancel = true;
			IsShown = false;
		}
	}
}


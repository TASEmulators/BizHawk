using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace BizHawk.Client.EmuHawk
{
	/// <summary>
	/// Shows information about a specific achievement
	/// </summary>
	public partial class RCheevosAchievementForm : Form
	{
		public int OrderByKey()
		{
			var ret = 0;
			ret += hcUnlockedCheckBox.Checked ? 3 : 0;
			ret += scUnlockedCheckBox.Checked ? 2 : 0;
			ret += primedCheckBox.Checked ? 1 : 0;
			ret += string.IsNullOrEmpty(progressBox.Text) ? 0 : 1;
			ret += unofficialCheckBox.Checked ? -10 : 0;
			return ret;
		}

		private Bitmap _unlockedBadge, _lockedBadge;
		private readonly RCheevos.Cheevo _cheevo;
		private readonly Func<uint, string> _getCheevoProgress;

		public RCheevosAchievementForm(RCheevos.Cheevo cheevo, Func<uint, string> getCheevoProgress)
		{
			InitializeComponent();
			titleBox.Text = cheevo.Title;
			descriptionBox.Text = cheevo.Description;
			pointsBox.Text = cheevo.Points.ToString();
			progressBox.Text = getCheevoProgress(cheevo.ID);
			unofficialCheckBox.Checked = !cheevo.IsOfficial;
			hcUnlockedCheckBox.Checked = cheevo.IsHardcoreUnlocked;
			primedCheckBox.Checked = cheevo.IsPrimed;
			scUnlockedCheckBox.Checked = cheevo.IsSoftcoreUnlocked;
			_cheevo = cheevo;
			_getCheevoProgress = getCheevoProgress;
			TopLevel = false;
			Show();
		}

		private static Bitmap UpscaleBadge(Bitmap src)
		{
			var ret = new Bitmap(120, 120);
			using var g = Graphics.FromImage(ret);
			g.InterpolationMode = InterpolationMode.NearestNeighbor;
			g.PixelOffsetMode = PixelOffsetMode.Half;
			g.DrawImage(src, 0, 0, 120, 120);
			return ret;
		}

		public void OnFrameAdvance(bool hardcore)
		{
			var unlockedBadge = _cheevo.BadgeUnlocked;
			if (_unlockedBadge is null && unlockedBadge is not null)
			{
				_unlockedBadge = UpscaleBadge(unlockedBadge);
			}

			var lockedBadge = _cheevo.BadgeLocked;
			if (_lockedBadge is null && lockedBadge is not null)
			{
				_lockedBadge = UpscaleBadge(lockedBadge);
			}

			var badge = _cheevo.IsUnlocked(hardcore) ? _unlockedBadge : _lockedBadge;

			if (cheevoBadgeBox.Image != badge)
			{
				cheevoBadgeBox.Image = badge;
			}

			pointsBox.Text = _cheevo.Points.ToString();
			progressBox.Text = _getCheevoProgress(_cheevo.ID);
			hcUnlockedCheckBox.Checked = _cheevo.IsHardcoreUnlocked;
			primedCheckBox.Checked = _cheevo.IsPrimed;
			scUnlockedCheckBox.Checked = _cheevo.IsSoftcoreUnlocked;
		}
	}
}


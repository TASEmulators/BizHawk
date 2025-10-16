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
		private Bitmap _unlockedBadge, _lockedBadge;
		private RCheevos.Cheevo _cheevo;
		private readonly Func<uint, string> _getCheevoProgress;

		public RCheevosAchievementForm(Func<uint, string> getCheevoProgress)
		{
			InitializeComponent();
			_getCheevoProgress = getCheevoProgress;
			TopLevel = false;
			Show();
		}

		public bool UpdateCheevo(RCheevos.Cheevo cheevo, bool isHardcodeMode)
		{
			bool updated = _cheevo != cheevo;

			_cheevo = cheevo;

			titleBox.Text = cheevo.Title;
			descriptionBox.Text = cheevo.Description;
			pointsBox.Text = cheevo.Points.ToString();
			unofficialCheckBox.Checked = !cheevo.IsOfficial;

			// badges are lazy loaded so we need to make sure they are updated even when _cheevo == cheevo
			if (updated)
			{
				_unlockedBadge = null;
				_lockedBadge = null;
				cheevoBadgeBox.Image = null;
			}
			_unlockedBadge ??= UpscaleBadge(cheevo.BadgeUnlocked);
			_lockedBadge ??= UpscaleBadge(cheevo.BadgeLocked);

			var badge = _cheevo.IsUnlocked(isHardcodeMode) ? _unlockedBadge : _lockedBadge;

			if (cheevoBadgeBox.Image != badge)
			{
				cheevoBadgeBox.Image = badge;
				updated = true;
			}
			progressBox.Text = _getCheevoProgress(cheevo.ID);
			hcUnlockedCheckBox.Checked = cheevo.IsHardcoreUnlocked;
			primedCheckBox.Checked = cheevo.IsPrimed;
			scUnlockedCheckBox.Checked = cheevo.IsSoftcoreUnlocked;

			return updated;
		}

		private static Bitmap UpscaleBadge(Bitmap src)
		{
			if (src is null) return null;

			var ret = new Bitmap(120, 120);
			using var g = Graphics.FromImage(ret);
			g.InterpolationMode = InterpolationMode.NearestNeighbor;
			g.PixelOffsetMode = PixelOffsetMode.Half;
			g.DrawImage(src, 0, 0, 120, 120);
			return ret;
		}

		public void OnFrameAdvance(bool hardcore)
		{
			var badge = _cheevo.IsUnlocked(hardcore) ? _unlockedBadge : _lockedBadge;

			if (cheevoBadgeBox.Image != badge)
			{
				cheevoBadgeBox.Image = badge;
			}

			progressBox.Text = _getCheevoProgress(_cheevo.ID);
			hcUnlockedCheckBox.Checked = _cheevo.IsHardcoreUnlocked;
			primedCheckBox.Checked = _cheevo.IsPrimed;
			scUnlockedCheckBox.Checked = _cheevo.IsSoftcoreUnlocked;
		}
	}
}


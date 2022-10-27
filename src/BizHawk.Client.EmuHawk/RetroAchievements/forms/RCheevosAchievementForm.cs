using System;
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
			ret += unofficialCheckBox.Checked ? -10 : 0;
			return ret;
		}

		public int CheevoID { get; }

		private Bitmap _unlockedBadge, _lockedBadge;
		private readonly Func<int, string> _getCheevoProgress;

		public RCheevosAchievementForm(RCheevos.Cheevo cheevo, Func<int, string> getCheevoProgress)
		{
			InitializeComponent();
			CheevoID = cheevo.ID;
			titleBox.Text = cheevo.Title;
			descriptionBox.Text = cheevo.Description;
			pointsBox.Text = cheevo.Points.ToString();
			progressBox.Text = getCheevoProgress(CheevoID);
			unofficialCheckBox.Checked = !cheevo.IsOfficial;
			hcUnlockedCheckBox.Checked = cheevo.IsHardcoreUnlocked;
			primedCheckBox.Checked = cheevo.IsPrimed;
			scUnlockedCheckBox.Checked = cheevo.IsSoftcoreUnlocked;
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

		public void OnFrameAdvance(RCheevos.Cheevo cheevo, bool hardcore)
		{
			var unlockedBadge = cheevo.BadgeUnlocked;
			if (_unlockedBadge is null && unlockedBadge is not null)
			{
				_unlockedBadge = UpscaleBadge(unlockedBadge);
			}

			var lockedBadge = cheevo.BadgeLocked;
			if (_lockedBadge is null && lockedBadge is not null)
			{
				_lockedBadge = UpscaleBadge(lockedBadge);
			}

			var badge = cheevo.IsUnlocked(hardcore) ? _unlockedBadge : _lockedBadge;

			if (cheevoBadgeBox.Image != badge)
			{
				cheevoBadgeBox.Image = badge;
			}

			pointsBox.Text = cheevo.Points.ToString();
			progressBox.Text = _getCheevoProgress(CheevoID);
			hcUnlockedCheckBox.Checked = cheevo.IsHardcoreUnlocked;
			primedCheckBox.Checked = cheevo.IsPrimed;
			scUnlockedCheckBox.Checked = cheevo.IsSoftcoreUnlocked;
		}
	}
}


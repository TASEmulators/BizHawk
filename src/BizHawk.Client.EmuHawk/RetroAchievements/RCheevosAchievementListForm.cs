using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Client.EmuHawk
{
	/// <summary>
	/// Shows a list of a user's current achievements
	/// </summary>
	public partial class RCheevosAchievementListForm : Form
	{
		private RCheevos.Cheevo[] _cheevos;
		private RCheevosAchievementForm[] _cheevoForms;
		private int _updateCooldown;
		private Func<uint, string> _getCheevoProgress;
		private Func<bool> _isHardcodeMode;

		private readonly int _controlHeight;

		public RCheevosAchievementListForm()
		{
			InitializeComponent();
			FormClosing += RCheevosAchievementListForm_FormClosing;
			_cheevoForms = Array.Empty<RCheevosAchievementForm>();
			_updateCooldown = 5; // only update every 5 frames / 12 fps (as this is rather expensive to update)
			using var temp = new RCheevosAchievementForm(null);
			_controlHeight = temp.Height + temp.Margin.Bottom + temp.Margin.Top;
		}

		private void DisposeCheevoForms()
		{
			foreach (var cheevoForm in _cheevoForms)
			{
				cheevoForm.Dispose();
			}
		}

		public void Restart(IEnumerable<RCheevos.Cheevo> cheevos, Func<uint, string> getCheevoProgress, Func<bool> isHardcodeMode)
		{
			_getCheevoProgress = getCheevoProgress;
			_isHardcodeMode = isHardcodeMode;
			flowLayoutPanel1.Controls.Clear();
			DisposeCheevoForms();
			_cheevos = cheevos.ToArray();
			_cheevoForms = new RCheevosAchievementForm[DisplayedItems()];
			for (int i = 0; i < DisplayedItems(); i++)
			{
				_cheevoForms[i] = new RCheevosAchievementForm(getCheevoProgress);
			}
			flowLayoutPanel1.Controls.AddRange(_cheevoForms);
			vScrollBar1.Maximum = _controlHeight * _cheevos.Length;
			vScrollBar1.Value = 0;
			UpdateForms();
		}

		public void OnFrameAdvance(bool hardcore, bool forceUpdate = false)
		{
			_updateCooldown--;
			if (_updateCooldown == 0 || forceUpdate)
			{
				_updateCooldown = 5;

				var reorderedCheevos = _cheevos.OrderByDescending(f => f.OrderByKey(_getCheevoProgress)).ToArray();
				_cheevos = reorderedCheevos;

				UpdateForms();

				foreach (var form in _cheevoForms)
				{
					form.OnFrameAdvance(hardcore);
				}
			}
		}

		private void RCheevosAchievementListForm_FormClosing(object sender, FormClosingEventArgs e)
		{
			Hide();
			e.Cancel = true;
		}

		private void UpdateForms()
		{
			int firstIndex = vScrollBar1.Value / _controlHeight;
			int indexOffset = vScrollBar1.Value % _controlHeight;
			while (firstIndex > _cheevos.Length - _cheevoForms.Length)
			{
				firstIndex--;
				indexOffset += _controlHeight;
			}
			flowLayoutPanel1.SuspendDrawing();
			flowLayoutPanel1.SuspendLayout();
			bool refresh = flowLayoutPanel1.AutoScrollPosition.Y != -indexOffset;
			for (int i = 0; i < _cheevoForms.Length; i++)
			{
				refresh |= _cheevoForms[i].UpdateCheevo(_cheevos[firstIndex + i], _isHardcodeMode());
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

		public void flowLayoutPanel1_MouseWheel(object sender, MouseEventArgs e)
		{
			vScrollBar1.Value = (vScrollBar1.Value - e.Delta).Clamp(vScrollBar1.Minimum, vScrollBar1.Maximum - vScrollBar1.LargeChange + 1);
		}

		private int DisplayedItems()
		{
			return Math.Min((int) Math.Ceiling((double) flowLayoutPanel1.Height / _controlHeight) + 1, _cheevos.Length);
		}
	}

	public class DoubleBufferedFlowLayoutPanel : FlowLayoutPanel
	{
		protected override void OnMouseWheel(MouseEventArgs e)
		{
			(Parent as RCheevosAchievementListForm)?.flowLayoutPanel1_MouseWheel(this, e);
		}

		public void SuspendDrawing()
		{
			WmImports.SendMessageW(this.Handle, 11, (IntPtr)0, IntPtr.Zero);
		}

		public void ResumeDrawing()
		{
			WmImports.SendMessageW(this.Handle, 11, (IntPtr) 1, IntPtr.Zero);
		}
	}
}

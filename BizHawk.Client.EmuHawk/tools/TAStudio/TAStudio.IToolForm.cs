using System.Windows.Forms;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class TAStudio : IToolForm
	{
		public bool UpdateBefore { get { return false; } }

		public void UpdateValues()
		{
			if (!IsHandleCreated || IsDisposed || _currentTasMovie == null)
			{
				return;
			}

			SetVisibleIndex();
			RefreshDialog();
		}

		public void FastUpdate()
		{
			if (!IsHandleCreated || IsDisposed || _currentTasMovie == null)
			{
				return;
			}

			TasView.RowCount = _currentTasMovie.InputLogLength + 1;
			SetVisibleIndex();
		}

		public void Restart()
		{
			if (!IsHandleCreated || IsDisposed)
			{
				return;
			}

			if (_currentTasMovie != null)
			{
				RefreshDialog();
			}
		}


		public bool AskSaveChanges()
		{
			if (_currentTasMovie != null && _currentTasMovie.Changes)
			{
				GlobalWin.Sound.StopSound();
				var result = MessageBox.Show(
					"Save Changes?",
					"Tastudio",
					MessageBoxButtons.YesNoCancel,
					MessageBoxIcon.Question,
					MessageBoxDefaultButton.Button3);

				GlobalWin.Sound.StartSound();
				if (result == DialogResult.Yes)
				{
					SaveTasMenuItem_Click(null, null);
				}
				else if (result == DialogResult.No)
				{
					_currentTasMovie.ClearChanges();
					return true;
				}
				else if (result == DialogResult.Cancel)
				{
					return false;
				}
			}

			return true;
		}

		private void SetVisibleIndex(int? indexThatMustBeVisible = null)
		{
			if (!indexThatMustBeVisible.HasValue)
			{
				indexThatMustBeVisible = _currentTasMovie.IsRecording
					? _currentTasMovie.InputLogLength
					: Global.Emulator.Frame + 1;
			}

			if (!TasView.IsVisible(indexThatMustBeVisible.Value))
			{
				TasView.LastVisibleRow = indexThatMustBeVisible.Value;
			}
		}
	}
}

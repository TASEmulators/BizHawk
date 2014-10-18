using System.Windows.Forms;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class TAStudio : IToolForm
	{
		private bool _hackyDontUpdate;
		private bool _initializing; // If true, will bypass restart logic, this is necessary since loading projects causes a movie to load which causes a rom to reload causing dialogs to restart

		public bool UpdateBefore { get { return false; } }

		public void UpdateValues()
		{
			if (!IsHandleCreated || IsDisposed || _currentTasMovie == null)
			{
				return;
			}

			if (_hackyDontUpdate)
			{
				return;
			}

			if (TasPlaybackBox.FollowCursor)
			{
				SetVisibleIndex();
			}

			RefreshDialog();
		}

		public void FastUpdate()
		{
			if (!IsHandleCreated || IsDisposed || _currentTasMovie == null)
			{
				return;
			}

			TasView.RowCount = _currentTasMovie.InputLogLength + 1;

			if (TasPlaybackBox.FollowCursor)
			{
				SetVisibleIndex();
			}
		}

		public void Restart()
		{
			if (!IsHandleCreated || IsDisposed)
			{
				return;
			}

			if (_initializing)
			{
				return;
			}

			if (_currentTasMovie != null)
			{
				if (Global.Game.Hash != _currentTasMovie.Hash)
				{
					TastudioToStopMovie();
					TasView.AllColumns.Clear();
					NewDefaultProject();
					SetUpColumns();
					TasView.Refresh();
				}
				else
				{
					RefreshDialog();
				}
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

		public void SetVisibleIndex(int? indexThatMustBeVisible = null)
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

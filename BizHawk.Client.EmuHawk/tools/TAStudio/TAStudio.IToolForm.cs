using System.Windows.Forms;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class TAStudio : IToolForm
	{
		public void UpdateValues()
		{
			if (!IsHandleCreated || IsDisposed || _currentTasMovie == null)
			{
				return;
			}

			if (_currentTasMovie.IsRecording)
			{
				TasView.LastVisibleRow = _currentTasMovie.InputLogLength - 1;
			}
			else
			{
				TasView.LastVisibleRow = Global.Emulator.Frame;
			}

			RefreshDialog();
		}

		public void FastUpdate()
		{
			if (!IsHandleCreated || IsDisposed || _currentTasMovie == null)
			{
				return;
			}

			if (_currentTasMovie.IsRecording)
			{
				TasView.LastVisibleRow = _currentTasMovie.InputLogLength - 1;
			}
			else
			{
				TasView.LastVisibleRow = Global.Emulator.Frame;
			}
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

		public bool UpdateBefore { get { return false; } }
	}
}

using System.Windows.Forms;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class TAStudio : IToolForm
	{
		public void UpdateValues()
		{
			if (!IsHandleCreated || IsDisposed)
			{
				return;
			}

			if (_currentTasMovie.IsRecording)
			{
				TasView.ensureVisible(_currentTasMovie.InputLogLength - 1);
			}
			else
			{
				TasView.ensureVisible(Global.Emulator.Frame);
			}

			RefreshDialog();
		}

		public void FastUpdate()
		{
			// TODO: think more about this
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

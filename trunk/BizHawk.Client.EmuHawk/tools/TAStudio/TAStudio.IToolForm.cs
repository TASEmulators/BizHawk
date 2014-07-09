using System.Windows.Forms;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class TAStudio : IToolForm
	{
		public bool UpdateBefore { get { return false; } }

		public void UpdateValues()
		{
			SetUpColumns();

			if (!IsHandleCreated || IsDisposed)
			{
				return;
			}

			TasView.ItemCount = _tas.InputLogLength;
			if (_tas.IsRecording)
			{
				TasView.ensureVisible(_tas.InputLogLength - 1);
			}
			else
			{
				TasView.ensureVisible(Global.Emulator.Frame - 1);
			}
		}

		public void Restart()
		{
			if (!IsHandleCreated || IsDisposed)
			{
				return;
			}
		}

		public bool AskSave()
		{
			if (_tas != null && _tas.Changes)
			{
				GlobalWin.Sound.StopSound();
				var result = MessageBox.Show("Save Changes?", "Tastudio", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button3);
				GlobalWin.Sound.StartSound();
				if (result == DialogResult.Yes)
				{
					SaveTasMenuItem_Click(null, null);
				}
				else if (result == DialogResult.No)
				{
					return true;
				}
				else if (result == DialogResult.Cancel)
				{
					return false;
				}
			}

			return true;
		}
	}
}

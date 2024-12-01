using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Computers.SinclairSpectrum;

namespace BizHawk.Client.EmuHawk
{
	public partial class ZxSpectrumJoystickSettings : Form, IDialogParent
	{
		private readonly ISettingsAdapter _settable;

		private readonly ZXSpectrum.ZXSpectrumSyncSettings _syncSettings;
		private string[] _possibleControllers;

		public IDialogController DialogController { get; }

		public ZxSpectrumJoystickSettings(IDialogController dialogController, ISettingsAdapter settable)
		{
			_settable = settable;
			_syncSettings = (ZXSpectrum.ZXSpectrumSyncSettings) _settable.GetSyncSettings();
			DialogController = dialogController;
			InitializeComponent();
			Icon = Properties.Resources.GameControllerIcon;
		}

		private void IntvControllerSettings_Load(object sender, EventArgs e)
		{
			_possibleControllers = Enum.GetNames(typeof(JoystickType));

			foreach (var val in _possibleControllers)
			{
				Port1ComboBox.Items.Add(val);
				Port2ComboBox.Items.Add(val);
				Port3ComboBox.Items.Add(val);
			}

			Port1ComboBox.SelectedItem = _syncSettings.JoystickType1.ToString();
			Port2ComboBox.SelectedItem = _syncSettings.JoystickType2.ToString();
			Port3ComboBox.SelectedItem = _syncSettings.JoystickType3.ToString();
		}

		private void OkBtn_Click(object sender, EventArgs e)
		{
			bool changed =
				_syncSettings.JoystickType1.ToString() != Port1ComboBox.SelectedItem.ToString()
				|| _syncSettings.JoystickType2.ToString() != Port2ComboBox.SelectedItem.ToString()
				|| _syncSettings.JoystickType3.ToString() != Port3ComboBox.SelectedItem.ToString();

			if (changed)
			{
				// enforce unique joystick selection

				bool selectionValid = true;

				var j1 = Port1ComboBox.SelectedItem.ToString();
				if (j1 != _possibleControllers[0])
				{
					if (j1 == Port2ComboBox.SelectedItem.ToString())
					{
						Port2ComboBox.SelectedItem = _possibleControllers[0];
						selectionValid = false;
					}
					if (j1 == Port3ComboBox.SelectedItem.ToString())
					{
						Port3ComboBox.SelectedItem = _possibleControllers[0];
						selectionValid = false;
					}
				}

				var j2 = Port2ComboBox.SelectedItem.ToString();
				if (j2 != _possibleControllers[0])
				{
					if (j2 == Port1ComboBox.SelectedItem.ToString())
					{
						Port1ComboBox.SelectedItem = _possibleControllers[0];
						selectionValid = false;
					}
					if (j2 == Port3ComboBox.SelectedItem.ToString())
					{
						Port3ComboBox.SelectedItem = _possibleControllers[0];
						selectionValid = false;
					}
				}

				var j3 = Port3ComboBox.SelectedItem.ToString();
				if (j3 != _possibleControllers[0])
				{
					if (j3 == Port1ComboBox.SelectedItem.ToString())
					{
						Port1ComboBox.SelectedItem = _possibleControllers[0];
						selectionValid = false;
					}
					if (j3 == Port2ComboBox.SelectedItem.ToString())
					{
						Port2ComboBox.SelectedItem = _possibleControllers[0];
						selectionValid = false;
					}
				}

				if (selectionValid)
				{
					_syncSettings.JoystickType1 = (JoystickType)Enum.Parse(typeof(JoystickType), Port1ComboBox.SelectedItem.ToString());
					_syncSettings.JoystickType2 = (JoystickType)Enum.Parse(typeof(JoystickType), Port2ComboBox.SelectedItem.ToString());
					_syncSettings.JoystickType3 = (JoystickType)Enum.Parse(typeof(JoystickType), Port3ComboBox.SelectedItem.ToString());

					_settable.PutCoreSyncSettings(_syncSettings);

					DialogResult = DialogResult.OK;
					Close();
				}
				else
				{
					DialogController.ShowMessageBox("Invalid joystick configuration. \nDuplicates have automatically been changed to NULL.\n\nPlease review the configuration");
				}
			}
			else
			{
				DialogResult = DialogResult.OK;
				Close();
			}
		}

		private void CancelBtn_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			Close();
		}
	}
}

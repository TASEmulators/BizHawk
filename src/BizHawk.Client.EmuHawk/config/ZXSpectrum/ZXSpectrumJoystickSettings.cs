using System;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Emulation.Cores.Computers.SinclairSpectrum;

using EnumsNET;

namespace BizHawk.Client.EmuHawk
{
	public partial class ZxSpectrumJoystickSettings : Form
	{
		private readonly IMainFormForConfig _mainForm;
		private readonly ZXSpectrum.ZXSpectrumSyncSettings _syncSettings;
		private string[] _possibleControllers;

		public ZxSpectrumJoystickSettings(
			IMainFormForConfig mainForm,
			ZXSpectrum.ZXSpectrumSyncSettings syncSettings)
		{
			_mainForm = mainForm;
			_syncSettings = syncSettings;
			InitializeComponent();
			Icon = Properties.Resources.GameControllerIcon;
		}

		private void IntvControllerSettings_Load(object sender, EventArgs e)
		{
			_possibleControllers = Enums.GetNames<JoystickType>().ToArray();

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
				if (j1 != _possibleControllers.First())
				{
					if (j1 == Port2ComboBox.SelectedItem.ToString())
					{
						Port2ComboBox.SelectedItem = _possibleControllers.First();
						selectionValid = false;
					}
					if (j1 == Port3ComboBox.SelectedItem.ToString())
					{
						Port3ComboBox.SelectedItem = _possibleControllers.First();
						selectionValid = false;
					}
				}

				var j2 = Port2ComboBox.SelectedItem.ToString();
				if (j2 != _possibleControllers.First())
				{
					if (j2 == Port1ComboBox.SelectedItem.ToString())
					{
						Port1ComboBox.SelectedItem = _possibleControllers.First();
						selectionValid = false;
					}
					if (j2 == Port3ComboBox.SelectedItem.ToString())
					{
						Port3ComboBox.SelectedItem = _possibleControllers.First();
						selectionValid = false;
					}
				}   

				var j3 = Port3ComboBox.SelectedItem.ToString();
				if (j3 != _possibleControllers.First())
				{
					if (j3 == Port1ComboBox.SelectedItem.ToString())
					{
						Port1ComboBox.SelectedItem = _possibleControllers.First();
						selectionValid = false;
					}
					if (j3 == Port2ComboBox.SelectedItem.ToString())
					{
						Port2ComboBox.SelectedItem = _possibleControllers.First();
						selectionValid = false;
					}
				}

				if (selectionValid)
				{
					_syncSettings.JoystickType1 = Enums.Parse<JoystickType>(Port1ComboBox.SelectedItem.ToString());
					_syncSettings.JoystickType2 = Enums.Parse<JoystickType>(Port2ComboBox.SelectedItem.ToString());
					_syncSettings.JoystickType3 = Enums.Parse<JoystickType>(Port3ComboBox.SelectedItem.ToString());

					_mainForm.PutCoreSyncSettings(_syncSettings);

					DialogResult = DialogResult.OK;
					Close();
				}
				else
				{
					MessageBox.Show("Invalid joystick configuration. \nDuplicates have automatically been changed to NULL.\n\nPlease review the configuration");
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
			_mainForm.AddOnScreenMessage("Joystick settings aborted");
			DialogResult = DialogResult.Cancel;
			Close();
		}
	}
}

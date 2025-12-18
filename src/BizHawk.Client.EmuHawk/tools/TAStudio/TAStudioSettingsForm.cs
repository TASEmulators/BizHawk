#nullable enable

using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class TAStudioSettingsForm : Form
	{
		private TAStudio.AllSettings _settings;
		private ControllerDefinition _controllerDef;

		private Action<TAStudio.AllSettings> _saveCallback;

		private bool _changedStateManagerSettings = false;
		private IStateManagerSettings _stateManagerSettings;

		private bool _changedByUser = true;

		private Font _font;
		private TAStudioPalette _palette;

		public TAStudioSettingsForm(
			TAStudio.AllSettings editorSettings,
			ControllerDefinition controllerDefinition,
			Action<TAStudio.AllSettings> saveCallback)
		{
			InitializeComponent();
			Icon = Properties.Resources.TAStudioIcon;

			_settings = editorSettings;
			_controllerDef = controllerDefinition;
			_saveCallback = saveCallback;

			_stateManagerSettings = _settings.CurrentStateManagerSettings.Clone();

			_font = _settings.GeneralClientSettings.TasViewFont;
			_palette = _settings.GeneralClientSettings.Palette;

			_boolPatterns = new AutoPatternBool[_settings.MovieSettings.BoolPatterns.Length];
			_axisPatterns = new AutoPatternAxis[_settings.MovieSettings.AxisPatterns.Length];
			for (int i = 0; i < _boolPatterns.Length; i++) _boolPatterns[i] = _settings.MovieSettings.BoolPatterns[i].Clone();
			for (int i = 0; i < _axisPatterns.Length; i++) _axisPatterns[i] = _settings.MovieSettings.AxisPatterns[i].Clone();
		}

		private void TAStudioSettingsForm_Load(object sender, EventArgs e)
		{
			// appearance
			MarkerColorCheckbox.Checked = _settings.GeneralClientSettings.DenoteMarkersWithBGColor;
			MarkerIconsCheckbox.Checked = _settings.GeneralClientSettings.DenoteMarkersWithIcons;
			StateColorCheckbox.Checked = _settings.GeneralClientSettings.DenoteStatesWithBGColor;
			StateIconsCheckbox.Checked = _settings.GeneralClientSettings.DenoteStatesWithIcons;
			HideLagNum.Value = _settings.MovieSettings.InputRollSettings.LagFramesToHide;
			HideWasLagCheckbox.Checked = _settings.MovieSettings.InputRollSettings.HideWasLagFrames;
			RotateCheckbox.Checked = _settings.MovieSettings.InputRollSettings.HorizontalOrientation;

			// autosave
			AutosaveBackupCheckbox.Checked = _settings.GeneralClientSettings.AutosaveAsBackupFile;
			AutosaveBk2Checkbox.Checked = _settings.GeneralClientSettings.AutosaveAsBk2;
			AutosaveIntervalNum.Value = _settings.GeneralClientSettings.AutosaveInterval / 1000;
			BackupOnSaveCheckbox.Checked = _settings.GeneralClientSettings.BackupPerFileSave;

			// misc
			BindMarkersCheckbox.Checked = _settings.GeneralClientSettings.BindMarkersToInput;
			AutopauseCheckbox.Checked = _settings.GeneralClientSettings.AutoPause;
			BranchDoubleClickCheckbox.Checked = _settings.GeneralClientSettings.LoadBranchOnDoubleClick;
			IncludeFrameNumberCheckbox.Checked = _settings.GeneralClientSettings.CopyIncludesFrameNo;
			AlwaysScrollCheckbox.Checked =  _settings.GeneralClientSettings.FollowCursorAlwaysScroll;
			UndoCountNum.Value = _settings.GeneralClientSettings.MaxUndoSteps;
			RewindNum.Value = _settings.GeneralClientSettings.RewindStep;
			FastRewindNum.Value = _settings.GeneralClientSettings.RewindStepFast;
			ScrollSpeedNum.Value = _settings.GeneralClientSettings.ScrollSpeed;
			RadioButton scrollMethodRadio = _settings.GeneralClientSettings.FollowCursorScrollMethod switch
			{
				"near" => ScrollToViewRadio,
				"top" => ScrollToTopRadio,
				"bottom" => ScrollToBottomRadio,
				"center" => ScrollToCenterRadio,
				_ => ScrollToViewRadio,
			};
			scrollMethodRadio.Checked = true;

			// patterns
			foreach (var button in _controllerDef.BoolButtons)
			{
				ButtonBox.Items.Add(button);
			}

			foreach (var button in _controllerDef.Axes.Keys)
			{
				ButtonBox.Items.Add(button);
			}
			ButtonBox.SelectedIndex = 0;
			RadioButton patternPaintRadio = _settings.GeneralClientSettings.PatternPaintMode switch
			{
				TAStudio.TAStudioSettings.PatternPaintModeEnum.Never => PatternPaintNeverRadioButton,
				TAStudio.TAStudioSettings.PatternPaintModeEnum.AutoFireOnly => PatternPaintAutoColumnsOnlyRadioButton,
				TAStudio.TAStudioSettings.PatternPaintModeEnum.Always => PatternPaintAlwaysRadioButton,
				_ => PatternPaintNeverRadioButton,
			};
			patternPaintRadio.Checked = true;
			RadioButton patternSelectionRadio = _settings.GeneralClientSettings.PatternSelection switch
			{
				TAStudio.TAStudioSettings.PatternSelectionEnum.Hold => PatternHoldRadioButton,
				TAStudio.TAStudioSettings.PatternSelectionEnum.AutoFire => PatternAutoFireRadioButton,
				TAStudio.TAStudioSettings.PatternSelectionEnum.Custom => PatternCustomRadioButton,
				_ => PatternHoldRadioButton,
			};
			patternSelectionRadio.Checked = true;

			// state history
			ManagerSettingsPropertyGrid.SelectedObject = _stateManagerSettings;
			ManagerSettingsPropertyGrid.PropertyValueChanged += (s, e) =>
			{
				_changedStateManagerSettings = true;
				DefaultManagerSettingsAppliedLabel.Visible = false;
			};
			_changedByUser = false;
			if (_stateManagerSettings is ZwinderStateManagerSettings)
				StrategyBox.SelectedIndex = 0;
			else
				StrategyBox.SelectedIndex = 1;
			_changedByUser = true;
		}

		private readonly List<int> _patternCounts = new List<int>();
		private readonly List<string> _patternValues = new List<string>();
		private int _patternLoopAt;
		private bool _updatingPatternList;
		private AutoPatternBool[] _boolPatterns;
		private AutoPatternAxis[] _axisPatterns;

		private string SelectedButton => ButtonBox.Text;

		private bool IsBool => _controllerDef.BoolButtons.Contains(SelectedButton);

		private void ButtonBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			GetPattern();
			UpdateDisplay();

			if (IsBool)
			{
				OnOffBox.Visible = true;
				ValueNum.Visible = false;
			}
			else
			{
				ValueNum.Visible = true;
				OnOffBox.Visible = false;
			}

			CountNum.Value = _patternCounts[0];
		}

		private void PatternList_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (!_updatingPatternList)
			{
				UpdateDisplay();
			}
		}

		private void InsertButton_Click(object sender, EventArgs e)
		{
			_patternCounts.Insert(PatternList.SelectedIndex, 1);
			string defaultStr = "false";
			if (!IsBool)
			{
				defaultStr = _controllerDef.Axes[SelectedButton].Neutral.ToString();
			}

			_patternValues.Insert(PatternList.SelectedIndex, defaultStr);

			UpdatePattern();
			UpdateDisplay();
		}

		private void DeleteButton_Click(object sender, EventArgs e)
		{
			if (PatternList.SelectedIndex >= _patternCounts.Count)
			{
				return;
			}
			_patternCounts.RemoveAt(PatternList.SelectedIndex);
			_patternValues.RemoveAt(PatternList.SelectedIndex);
			UpdatePattern();
			UpdateDisplay();
		}

		private void LagBox_CheckedChanged(object sender, EventArgs e)
		{
			UpdatePattern();
		}

		private void ValueNum_ValueChanged(object sender, EventArgs e)
		{
			if (_updatingPatternList || PatternList.SelectedIndex == -1 || PatternList.SelectedIndex >= _patternCounts.Count)
			{
				return;
			}

			_patternValues[PatternList.SelectedIndex] = ((int)ValueNum.Value).ToString(NumberFormatInfo.InvariantInfo);
			UpdatePattern();
			UpdateDisplay();
		}

		private void OnOffBox_CheckedChanged(object sender, EventArgs e)
		{
			if (_updatingPatternList || PatternList.SelectedIndex == -1 || PatternList.SelectedIndex >= _patternCounts.Count)
			{
				return;
			}

			_patternValues[PatternList.SelectedIndex] = OnOffBox.Checked.ToString();
			UpdatePattern();
			UpdateDisplay();
		}

		private void CountNum_ValueChanged(object sender, EventArgs e)
		{
			if (_updatingPatternList || PatternList.SelectedIndex == -1 || PatternList.SelectedIndex > _patternCounts.Count)
			{
				return;
			}

			if (PatternList.SelectedIndex == _patternCounts.Count)
			{
				_patternLoopAt = (int)CountNum.Value;
			}
			else
			{
				// repeating zero times is not allowed
				if ((int)CountNum.Value == 0)
				{
					CountNum.Value = 1;
				}

				_patternCounts[PatternList.SelectedIndex] = (int)CountNum.Value;
			}

			UpdatePattern();
			UpdateDisplay();
		}

		private void UpdateDisplay()
		{
			_updatingPatternList = true;
			PatternList.SuspendLayout();

			int oldIndex = PatternList.SelectedIndex;
			if (oldIndex == -1)
			{
				oldIndex = 0;
			}

			PatternList.Items.Clear();
			int index = 0;
			for (int i = 0; i < _patternCounts.Count; i++)
			{
				string str = $"{index}: ";
				if (IsBool)
				{
					str += _patternValues[i][0] == 'T' ? "On" : "Off";
				}
				else
				{
					str += _patternValues[i];
				}

				PatternList.Items.Add($"{str}\t(x{_patternCounts[i]})");
				index += _patternCounts[i];
			}

			PatternList.Items.Add($"Loop to: {_patternLoopAt}");

			if (oldIndex >= PatternList.Items.Count)
			{
				oldIndex = PatternList.Items.Count - 1;
			}

			PatternList.SelectedIndex = oldIndex;

			if (PatternList.SelectedIndex != -1 && PatternList.SelectedIndex < _patternValues.Count)
			{
				index = _controllerDef.BoolButtons.IndexOf(SelectedButton);

				if (index != -1)
				{
					LagBox.Checked = _boolPatterns[index].SkipsLag;
					OnOffBox.Checked = _patternValues[PatternList.SelectedIndex][0] == 'T';
					CountNum.Value = _patternCounts[PatternList.SelectedIndex];
				}
				else
				{
					index = _controllerDef.Axes.IndexOf(SelectedButton);

					LagBox.Checked = _axisPatterns[index].SkipsLag;
					ValueNum.Value = int.Parse(_patternValues[PatternList.SelectedIndex]);
					CountNum.Value = _patternCounts[PatternList.SelectedIndex];
				}
			}
			else if (PatternList.SelectedIndex == _patternValues.Count)
			{
				CountNum.Value = _patternLoopAt;
			}

			PatternList.ResumeLayout();
			_updatingPatternList = false;
		}

		private void UpdatePattern()
		{
			int index = _controllerDef.BoolButtons.IndexOf(SelectedButton);

			if (index != -1)
			{
				var p = new List<bool>();
				for (int i = 0; i < _patternCounts.Count; i++)
				{
					for (int c = 0; c < _patternCounts[i]; c++)
					{
						p.Add(Convert.ToBoolean(_patternValues[i]));
					}
				}

				_boolPatterns[index] = new AutoPatternBool(p.ToArray(), LagBox.Checked, 0, _patternLoopAt);
			}
			else
			{
				index = _controllerDef.Axes.IndexOf(SelectedButton);

				var p = new List<int>();
				for (int i = 0; i < _patternCounts.Count; i++)
				{
					for (int c = 0; c < _patternCounts[i]; c++)
					{
						p.Add(int.Parse(_patternValues[i]));
					}
				}

				_axisPatterns[index] = new AutoPatternAxis(p.ToArray(), LagBox.Checked, 0, _patternLoopAt);
			}
		}

		private void GetPattern()
		{
			int index = _controllerDef.BoolButtons.IndexOf(SelectedButton);

			if (index != -1)
			{
				bool[] p = _boolPatterns[index].Pattern;
				bool lastValue = p[0];
				_patternCounts.Clear();
				_patternValues.Clear();
				_patternCounts.Add(1);
				_patternValues.Add(lastValue.ToString());
				for (int i = 1; i < p.Length; i++)
				{
					if (p[i] == lastValue)
					{
						_patternCounts[_patternCounts.Count - 1]++;
					}
					else
					{
						_patternCounts.Add(1);
						_patternValues.Add(p[i].ToString());
						lastValue = p[i];
					}
				}

				_patternLoopAt = _boolPatterns[index].Loop;
			}
			else
			{
				index = _controllerDef.Axes.IndexOf(SelectedButton);

				var p = _axisPatterns[index].Pattern;
				var lastValue = p[0];
				_patternCounts.Clear();
				_patternValues.Clear();
				_patternCounts.Add(1);
				_patternValues.Add(lastValue.ToString());
				for (int i = 1; i < p.Length; i++)
				{
					if (p[i] == lastValue)
					{
						_patternCounts[_patternCounts.Count - 1]++;
					}
					else
					{
						_patternCounts.Add(1);
						_patternValues.Add(p[i].ToString());
						lastValue = p[i];
					}
				}

				_patternLoopAt = _axisPatterns[index].Loop;
			}
		}

		private void StrategyBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (!_changedByUser) return;

			if (StrategyBox.SelectedIndex == 0)
			{
				_stateManagerSettings = new ZwinderStateManagerSettings();
			}
			else
			{
				_stateManagerSettings = new PagedStateManager.PagedSettings();
			}
			ManagerSettingsPropertyGrid.SelectedObject = _stateManagerSettings;
			_changedStateManagerSettings = true;
		}

		private void DefaultStateSettingsButton_Click(object sender, EventArgs e)
		{
			_stateManagerSettings = _settings.DefaultStateManagerSettings.Clone();
			ManagerSettingsPropertyGrid.SelectedObject = _stateManagerSettings;
			_changedStateManagerSettings = true;
		}

		private void SetDefaultStateSettingsButton_Click(object sender, EventArgs e)
		{
			_settings.DefaultStateManagerSettings = _stateManagerSettings.Clone();
			DefaultManagerSettingsAppliedLabel.Visible = true;
		}

		private void ApplyButton_Click(object sender, EventArgs e)
		{
			// stuff we keep copies of
			_settings.GeneralClientSettings.Palette = _palette;
			_settings.GeneralClientSettings.TasViewFont = _font;
			_settings.MovieSettings.BoolPatterns = _boolPatterns;
			_settings.MovieSettings.AxisPatterns = _axisPatterns;
			if (_changedStateManagerSettings)
				_settings.CurrentStateManagerSettings = _stateManagerSettings;

			// all the controls
			_settings.GeneralClientSettings.DenoteMarkersWithBGColor = MarkerColorCheckbox.Checked;
			_settings.GeneralClientSettings.DenoteMarkersWithIcons = MarkerIconsCheckbox.Checked;
			_settings.GeneralClientSettings.DenoteStatesWithBGColor = StateColorCheckbox.Checked;
			_settings.GeneralClientSettings.DenoteStatesWithIcons = StateIconsCheckbox.Checked;
			_settings.MovieSettings.InputRollSettings.LagFramesToHide = (int)HideLagNum.Value;
			_settings.MovieSettings.InputRollSettings.HideWasLagFrames = HideWasLagCheckbox.Checked;
			_settings.MovieSettings.InputRollSettings.HorizontalOrientation = RotateCheckbox.Checked;

			_settings.GeneralClientSettings.AutosaveAsBackupFile = AutosaveBackupCheckbox.Checked;
			_settings.GeneralClientSettings.AutosaveAsBk2 = AutosaveBk2Checkbox.Checked;
			_settings.GeneralClientSettings.AutosaveInterval = (uint)AutosaveIntervalNum.Value * 1000;
			_settings.GeneralClientSettings.BackupPerFileSave = BackupOnSaveCheckbox.Checked;

			_settings.GeneralClientSettings.BindMarkersToInput = BindMarkersCheckbox.Checked;
			_settings.GeneralClientSettings.AutoPause = AutopauseCheckbox.Checked;
			_settings.GeneralClientSettings.LoadBranchOnDoubleClick = BranchDoubleClickCheckbox.Checked;
			_settings.GeneralClientSettings.CopyIncludesFrameNo = IncludeFrameNumberCheckbox.Checked;
			_settings.GeneralClientSettings.FollowCursorAlwaysScroll = AlwaysScrollCheckbox.Checked;
			_settings.GeneralClientSettings.MaxUndoSteps = (int)UndoCountNum.Value;
			_settings.GeneralClientSettings.RewindStep = (int)RewindNum.Value;
			_settings.GeneralClientSettings.RewindStepFast = (int)FastRewindNum.Value;
			_settings.GeneralClientSettings.ScrollSpeed = (int)ScrollSpeedNum.Value;

			if (ScrollToViewRadio.Checked) _settings.GeneralClientSettings.FollowCursorScrollMethod = "near";
			else if (ScrollToTopRadio.Checked) _settings.GeneralClientSettings.FollowCursorScrollMethod = "top";
			else if (ScrollToBottomRadio.Checked) _settings.GeneralClientSettings.FollowCursorScrollMethod = "bottom";
			else if (ScrollToCenterRadio.Checked) _settings.GeneralClientSettings.FollowCursorScrollMethod = "center";

			if (PatternPaintNeverRadioButton.Checked) _settings.GeneralClientSettings.PatternPaintMode = TAStudio.TAStudioSettings.PatternPaintModeEnum.Never;
			else if (PatternPaintAutoColumnsOnlyRadioButton.Checked) _settings.GeneralClientSettings.PatternPaintMode = TAStudio.TAStudioSettings.PatternPaintModeEnum.AutoFireOnly;
			else if (PatternPaintAlwaysRadioButton.Checked) _settings.GeneralClientSettings.PatternPaintMode = TAStudio.TAStudioSettings.PatternPaintModeEnum.Always;

			if (PatternHoldRadioButton.Checked) _settings.GeneralClientSettings.PatternSelection = TAStudio.TAStudioSettings.PatternSelectionEnum.Hold;
			else if (PatternAutoFireRadioButton.Checked) _settings.GeneralClientSettings.PatternSelection = TAStudio.TAStudioSettings.PatternSelectionEnum.AutoFire;
			else if (PatternCustomRadioButton.Checked) _settings.GeneralClientSettings.PatternSelection = TAStudio.TAStudioSettings.PatternSelectionEnum.Custom;

			_saveCallback(_settings);

			Close();
		}

		private void FontButton_Click(object sender, EventArgs e)
		{
			using var fontDialog = new FontDialog
			{
				ShowColor = false,
				Font = _font,
			};
			if (fontDialog.ShowDialog() != DialogResult.Cancel)
			{
				_font = fontDialog.Font;
			}
		}

		private void ColorsButton_Click(object sender, EventArgs e)
		{
			using TAStudioColorSettingsForm form = new(_palette, p => _palette = p)
			{
				Owner = this,
				StartPosition = FormStartPosition.Manual,
				Location = this.ChildPointToScreen(ColorsButton),
			};
			form.ShowDialogOnScreen();
		}

		private void SettingsCancelButton_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void PatternCustomRadioButton_CheckedChanged(object sender, EventArgs e)
		{
			CustomPatternsGroupBox.Enabled = PatternCustomRadioButton.Checked;
		}
	}
}

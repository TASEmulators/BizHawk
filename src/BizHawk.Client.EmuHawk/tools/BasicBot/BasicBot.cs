using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using BizHawk.Client.EmuHawk.ToolExtensions;

using BizHawk.Emulation.Common;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class BasicBot : ToolFormBase, IToolFormAutoConfig
	{
		private const string DialogTitle = "Basic Bot";

		private string _currentFileName = "";

		private string CurrentFileName
		{
			get => _currentFileName;
			set
			{
				_currentFileName = value;

				Text = !string.IsNullOrWhiteSpace(_currentFileName)
					? $"{DialogTitle} - {Path.GetFileNameWithoutExtension(_currentFileName)}"
					: DialogTitle;
			}

		}

		private bool _isBotting;
		private long _attempts = 1;
		private long _frames;
		private int _targetFrame;
		private bool _oldCountingSetting;
		private BotAttempt _currentBotAttempt;
		private BotAttempt _bestBotAttempt;
		private readonly BotAttempt _comparisonBotAttempt;
		private bool _replayMode;
		private int _startFrame;
		private string _lastRom = "";
		private int _lastFrameAdvanced;

		private bool _doNotUpdateValues;

		private MemoryDomain _currentDomain;
		private bool _bigEndian;
		private int _dataSize;

		private Dictionary<string, double> _cachedControlProbabilities;
		private ILogEntryGenerator _logGenerator;
		
		private bool _previousDisplayMessage;
		private bool _previousInvisibleEmulation;

		#region Services and Settings

		[RequiredService]
		private IEmulator Emulator { get; set; }

		// Unused, due to the use of MainForm to loadstate, but this needs to be kept here in order to establish an IStatable dependency
		[RequiredService]
		private IStatable StatableCore { get; set; }

		[RequiredService]
		private IMemoryDomains MemoryDomains { get; set; }

		[ConfigPersist]
		public BasicBotSettings Settings { get; set; }

		public class BasicBotSettings
		{
			public RecentFiles RecentBotFiles { get; set; } = new RecentFiles();
			public bool TurboWhenBotting { get; set; } = true;
			public bool InvisibleEmulation { get; set; }
		}

		#endregion

		public BasicBot()
		{
			InitializeComponent();
			Text = DialogTitle;
			Settings = new BasicBotSettings();

			_comparisonBotAttempt = new BotAttempt();
			MainOperator.SelectedItem = ">=";
		}

		private void BasicBot_Load(object sender, EventArgs e)
		{
			_previousInvisibleEmulation = InvisibleEmulationCheckBox.Checked = Settings.InvisibleEmulation;
			_previousDisplayMessage = Config.DisplayMessages;
		}

		#region UI Bindings

		private Dictionary<string, double> ControlProbabilities =>
			ControlProbabilityPanel.Controls
				.OfType<BotControlsRow>()
				.ToDictionary(tkey => tkey.ButtonName, tvalue => tvalue.Probability);
		
		private string SelectedSlot
		{
			get
			{
				char num = StartFromSlotBox.SelectedItem
					.ToString()
					.Last();

				return $"QuickSave{num}";
			}
		}

		private long Attempts
		{
			get => _attempts;
			set
			{
				_attempts = value;
				AttemptsLabel.Text = _attempts.ToString();
			}
		}

		private long Frames
		{
			get => _frames;
			set
			{
				_frames = value;
				FramesLabel.Text = _frames.ToString();
			}
		}

		private int FrameLength
		{
			get => (int)FrameLengthNumeric.Value;
			set => FrameLengthNumeric.Value = value;
		}

		public int MaximizeAddress
		{
			get => MaximizeAddressBox.ToRawInt() ?? 0;
			set => MaximizeAddressBox.SetFromRawInt(value);
		}

		public int MaximizeValue
		{
			get
			{
				int? addr = MaximizeAddressBox.ToRawInt();
				return addr.HasValue ? GetRamValue(addr.Value) : 0;
			}
		}

		public int TieBreaker1Address
		{
			get => TieBreaker1Box.ToRawInt() ?? 0;
			set => TieBreaker1Box.SetFromRawInt(value);
		}

		public int TieBreaker1Value
		{
			get
			{
				int? addr = TieBreaker1Box.ToRawInt();
				return addr.HasValue ? GetRamValue(addr.Value) : 0;
			}
		}

		public int TieBreaker2Address
		{
			get => TieBreaker2Box.ToRawInt() ?? 0;
			set => TieBreaker2Box.SetFromRawInt(value);
		}

		public int TieBreaker2Value
		{
			get
			{
				int? addr = TieBreaker2Box.ToRawInt();
				return addr.HasValue ? GetRamValue(addr.Value) : 0;
			}
		}

		public int TieBreaker3Address
		{
			get => TieBreaker3Box.ToRawInt() ?? 0;
			set => TieBreaker3Box.SetFromRawInt(value);
		}

		public int TieBreaker3Value
		{
			get
			{
				int? addr = TieBreaker3Box.ToRawInt();
				return addr.HasValue ? GetRamValue(addr.Value) : 0;
			}
		}

		public byte MainComparisonType
		{
			get => (byte)MainOperator.SelectedIndex;
			set => MainOperator.SelectedIndex = value < 5 ? value : 0;
		}

		public byte Tie1ComparisonType
		{
			get => (byte)Tiebreak1Operator.SelectedIndex;
			set => Tiebreak1Operator.SelectedIndex = value < 5 ? value : 0;
		}

		public byte Tie2ComparisonType
		{
			get => (byte)Tiebreak2Operator.SelectedIndex;
			set => Tiebreak2Operator.SelectedIndex = value < 5 ? value : 0;
		}

		public byte Tie3ComparisonType
		{
			get => (byte)Tiebreak3Operator.SelectedIndex;
			set => Tiebreak3Operator.SelectedIndex = value < 5 ? value : 0;
		}

		public string FromSlot
		{
			get => StartFromSlotBox.SelectedItem != null
				? StartFromSlotBox.SelectedItem.ToString()
				: "";

			set
			{
				var item = StartFromSlotBox.Items
					.OfType<object>()
					.FirstOrDefault(o => o.ToString() == value);

				StartFromSlotBox.SelectedItem = item;
			}
		}


		// Upon Load State, TAStudio uses GlobalWin.Tools.UpdateBefore(); as well as GlobalWin.Tools.UpdateAfter(); 
		// Both of which will Call UpdateValues() and Update() which both end up in the Update() function.  Calling Update() will cause the Log to add an additional log.  
		// By not handling both of those calls the _currentBotAttempt.Log.Count will be 2 more than expected.
		// However this also causes a problem with RamWatch not being up to date since that TOO gets called.
		// Need to find out if having RamWatch open while TasStudio is open causes issues.
		// there appears to be  "hack"(?) line in ToolManager.UpdateBefore that seems to refresh the RamWatch.  Not sure that is causing any issue since it does look like the RamWatch is ahead too much..
		
		#endregion

		#region IToolForm Implementation

		public bool UpdateBefore => true;

		protected override void UpdateValuesBefore()
		{
			Update(fast: false);
		}

		public void FastUpdate()
		{
			Update(fast: true);
		}

		public void Restart()
		{
			if (_currentDomain == null ||
				MemoryDomains.Contains(_currentDomain))
			{
				_currentDomain = MemoryDomains.MainMemory;
				_bigEndian = _currentDomain.EndianType == MemoryDomain.Endian.Big;
				_dataSize = 1;
			}

			if (_isBotting)
			{
				StopBot();
			}
			else if (_replayMode)
			{
				FinishReplay();
			}


			if (_lastRom != MainForm.CurrentlyOpenRom)
			{
				_lastRom = MainForm.CurrentlyOpenRom;
				SetupControlsAndProperties();
			}
		}

		#endregion

		#region Control Events

		#region FileMenu

		private void FileSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			SaveMenuItem.Enabled = !string.IsNullOrWhiteSpace(CurrentFileName);
		}

		private void RecentSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			RecentSubMenu.DropDownItems.Clear();
			RecentSubMenu.DropDownItems.AddRange(Settings.RecentBotFiles.RecentMenu(LoadFileFromRecent, "Bot Parameters"));
		}

		private void NewMenuItem_Click(object sender, EventArgs e)
		{
			CurrentFileName = "";
			_bestBotAttempt = null;

			foreach (var cp in ControlProbabilityPanel.Controls.OfType<BotControlsRow>())
			{
				cp.Probability = 0;
			}

			FrameLength = 0;
			MaximizeAddress = 0;
			TieBreaker1Address = 0;
			TieBreaker2Address = 0;
			TieBreaker3Address = 0;
			StartFromSlotBox.SelectedIndex = 0;
			MainOperator.SelectedIndex = 0;
			Tiebreak1Operator.SelectedIndex = 0;
			Tiebreak2Operator.SelectedIndex = 0;
			Tiebreak3Operator.SelectedIndex = 0;
			MainBestRadio.Checked = true;
			MainValueNumeric.Value = 0;
			TieBreak1Numeric.Value = 0;
			TieBreak2Numeric.Value = 0;
			TieBreak3Numeric.Value = 0;
			TieBreak1BestRadio.Checked = true;
			TieBreak2BestRadio.Checked = true;
			TieBreak3BestRadio.Checked = true;

			UpdateBestAttempt();
			UpdateComparisonBotAttempt();
		}

		private void OpenMenuItem_Click(object sender, EventArgs e)
		{
			var file = OpenFileDialog(
					CurrentFileName,
					Config.PathEntries.ToolsAbsolutePath(),
					"Bot files",
					"bot");

			if (file != null)
			{
				LoadBotFile(file.FullName);
			}
		}

		private void SaveMenuItem_Click(object sender, EventArgs e)
		{
			if (!string.IsNullOrWhiteSpace(CurrentFileName))
			{
				SaveBotFile(CurrentFileName);
			}
		}

		private void SaveAsMenuItem_Click(object sender, EventArgs e)
		{
			var file = SaveFileDialog(
					CurrentFileName,
					Config.PathEntries.ToolsAbsolutePath(),
					"Bot files",
					"bot");

			if (file != null)
			{
				SaveBotFile(file.FullName);
				_currentFileName = file.FullName;
			}
		}

		private void ExitMenuItem_Click(object sender, EventArgs e)
		{
			Close();
		}

		#endregion

		#region Options Menu

		private void OptionsSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			TurboWhileBottingMenuItem.Checked = Settings.TurboWhenBotting;
			BigEndianMenuItem.Checked = _bigEndian;
		}

		private void MemoryDomainsMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			MemoryDomainsMenuItem.DropDownItems.Clear();
			MemoryDomainsMenuItem.DropDownItems.AddRange(
				MemoryDomains.MenuItems(SetMemoryDomain, _currentDomain.Name)
				.ToArray());
		}

		private void BigEndianMenuItem_Click(object sender, EventArgs e)
		{
			_bigEndian ^= true;
		}

		private void DataSizeMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			_1ByteMenuItem.Checked = _dataSize == 1;
			_2ByteMenuItem.Checked = _dataSize == 2;
			_4ByteMenuItem.Checked = _dataSize == 4;
		}

		private void _1ByteMenuItem_Click(object sender, EventArgs e)
		{
			_dataSize = 1;
		}

		private void _2ByteMenuItem_Click(object sender, EventArgs e)
		{
			_dataSize = 2;
		}

		private void _4ByteMenuItem_Click(object sender, EventArgs e)
		{
			_dataSize = 4;
		}

		private void TurboWhileBottingMenuItem_Click(object sender, EventArgs e)
		{
			Settings.TurboWhenBotting ^= true;
		}

		#endregion

		private void RunBtn_Click(object sender, EventArgs e)
		{
			StartBot();
		}

		private void StopBtn_Click(object sender, EventArgs e)
		{
			StopBot();
		}

		private void ClearBestButton_Click(object sender, EventArgs e)
		{
			_bestBotAttempt = null;
			Attempts = 0;
			Frames = 0;
			UpdateBestAttempt();
			UpdateComparisonBotAttempt();
		}

		private void PlayBestButton_Click(object sender, EventArgs e)
		{
			StopBot();
			_replayMode = true;
			_doNotUpdateValues = true;
			MainForm.LoadQuickSave(SelectedSlot, false, true); // Triggers an UpdateValues call
			_doNotUpdateValues = false;
			_startFrame = Emulator.Frame;
			SetNormalSpeed();
			UpdateBotStatusIcon();
			MessageLabel.Text = "Replaying";
			MainForm.UnpauseEmulator();
		}

		private void FrameLengthNumeric_ValueChanged(object sender, EventArgs e)
		{
			AssessRunButtonStatus();
		}

		private void ClearStatsContextMenuItem_Click(object sender, EventArgs e)
		{
			Attempts = 0;
			Frames = 0;
		}

		#endregion

		#region Classes

		private class BotAttempt
		{
			public long Attempt { get; set; }
			public int Maximize { get; set; }
			public int TieBreak1 { get; set; }
			public int TieBreak2 { get; set; }
			public int TieBreak3 { get; set; }
			public byte ComparisonTypeMain { get; set; }
			public byte ComparisonTypeTie1 { get; set; }
			public byte ComparisonTypeTie2 { get; set; }
			public byte ComparisonTypeTie3 { get; set; }

			public List<string> Log { get; } = new List<string>();
		}

		private class BotData
		{
			public BotAttempt Best { get; set; }
			public Dictionary<string, double> ControlProbabilities { get; set; }
			public int Maximize { get; set; }
			public int TieBreaker1 { get; set; }
			public int TieBreaker2 { get; set; }
			public int TieBreaker3 { get; set; }
			public byte ComparisonTypeMain { get; set; }
			public byte ComparisonTypeTie1 { get; set; }
			public byte ComparisonTypeTie2 { get; set; }
			public byte ComparisonTypeTie3 { get; set; }
			public bool MainCompareToBest { get; set; } = true;
			public bool TieBreaker1CompareToBest { get; set; } = true;
			public bool TieBreaker2CompareToBest { get; set; } = true;
			public bool TieBreaker3CompareToBest { get; set; } = true;
			public int MainCompareToValue { get; set; }
			public int TieBreaker1CompareToValue { get; set; }
			public int TieBreaker2CompareToValue { get; set; }
			public int TieBreaker3CompareToValue { get; set; }
			public int FrameLength { get; set; }
			public string FromSlot { get; set; }
			public long Attempts { get; set; }
			public long Frames { get; set; }

			public string MemoryDomain { get; set; }
			public bool BigEndian { get; set; }
			public int DataSize { get; set; }
		}

		#endregion

		#region File Handling

		private void LoadFileFromRecent(string path)
		{
			var result = LoadBotFile(path);
			if (!result)
			{
				Settings.RecentBotFiles.HandleLoadError(path);
			}
		}

		private bool LoadBotFile(string path)
		{
			var file = new FileInfo(path);
			if (!file.Exists)
			{
				return false;
			}

			var json = File.ReadAllText(path);
			var botData = (BotData)ConfigService.LoadWithType(json);

			_bestBotAttempt = botData.Best;

			var probabilityControls = ControlProbabilityPanel.Controls
					.OfType<BotControlsRow>()
					.ToList();

			foreach (var kvp in botData.ControlProbabilities)
			{
				var control = probabilityControls.Single(c => c.ButtonName == kvp.Key);
				control.Probability = kvp.Value;
			}

			MaximizeAddress = botData.Maximize;
			TieBreaker1Address = botData.TieBreaker1;
			TieBreaker2Address = botData.TieBreaker2;
			TieBreaker3Address = botData.TieBreaker3;
			try
			{
				MainComparisonType = botData.ComparisonTypeMain;
				Tie1ComparisonType = botData.ComparisonTypeTie1;
				Tie2ComparisonType = botData.ComparisonTypeTie2;
				Tie3ComparisonType = botData.ComparisonTypeTie3;

				MainBestRadio.Checked = botData.MainCompareToBest;
				TieBreak1BestRadio.Checked = botData.TieBreaker1CompareToBest;
				TieBreak2BestRadio.Checked = botData.TieBreaker2CompareToBest;
				TieBreak3BestRadio.Checked = botData.TieBreaker3CompareToBest;
				MainValueRadio.Checked = !botData.MainCompareToBest;
				TieBreak1ValueRadio.Checked = !botData.TieBreaker1CompareToBest;
				TieBreak2ValueRadio.Checked = !botData.TieBreaker2CompareToBest;
				TieBreak3ValueRadio.Checked = !botData.TieBreaker3CompareToBest;

				MainValueNumeric.Value = botData.MainCompareToValue;
				TieBreak1Numeric.Value = botData.TieBreaker1CompareToValue;
				TieBreak2Numeric.Value = botData.TieBreaker2CompareToValue;
				TieBreak3Numeric.Value = botData.TieBreaker3CompareToValue;
			}
			catch
			{
				MainComparisonType = 0;
				Tie1ComparisonType = 0;
				Tie2ComparisonType = 0;
				Tie3ComparisonType = 0;

				MainBestRadio.Checked = true;
				TieBreak1BestRadio.Checked = true;
				TieBreak2BestRadio.Checked = true;
				TieBreak3BestRadio.Checked = true;
				MainBestRadio.Checked = false;
				TieBreak1BestRadio.Checked = false;
				TieBreak2BestRadio.Checked = false;
				TieBreak3BestRadio.Checked = false;

				MainValueNumeric.Value = 0;
				TieBreak1Numeric.Value = 0;
				TieBreak2Numeric.Value = 0;
				TieBreak3Numeric.Value = 0;
			}
			FrameLength = botData.FrameLength;
			FromSlot = botData.FromSlot;
			Attempts = botData.Attempts;
			Frames = botData.Frames;

			_currentDomain = !string.IsNullOrWhiteSpace(botData.MemoryDomain)
					? MemoryDomains[botData.MemoryDomain]
					: MemoryDomains.MainMemory;

			_bigEndian = botData.BigEndian;
			_dataSize = botData.DataSize > 0 ? botData.DataSize : 1;

			UpdateBestAttempt();
			UpdateComparisonBotAttempt();

			if (_bestBotAttempt != null)
			{
				PlayBestButton.Enabled = true;
			}

			CurrentFileName = path;
			Settings.RecentBotFiles.Add(CurrentFileName);
			MessageLabel.Text = $"{Path.GetFileNameWithoutExtension(path)} loaded";

			AssessRunButtonStatus();
			return true;
		}

		private void SaveBotFile(string path)
		{
			var data = new BotData
			{
				Best = _bestBotAttempt,
				ControlProbabilities = ControlProbabilities,
				Maximize = MaximizeAddress,
				TieBreaker1 = TieBreaker1Address,
				TieBreaker2 = TieBreaker2Address,
				TieBreaker3 = TieBreaker3Address,
				ComparisonTypeMain = MainComparisonType,
				ComparisonTypeTie1 = Tie1ComparisonType,
				ComparisonTypeTie2 = Tie2ComparisonType,
				ComparisonTypeTie3 = Tie3ComparisonType,
				MainCompareToBest = MainBestRadio.Checked,
				TieBreaker1CompareToBest = TieBreak1BestRadio.Checked,
				TieBreaker2CompareToBest = TieBreak2BestRadio.Checked,
				TieBreaker3CompareToBest = TieBreak3BestRadio.Checked,
				MainCompareToValue = (int)MainValueNumeric.Value,
				TieBreaker1CompareToValue = (int)TieBreak1Numeric.Value,
				TieBreaker2CompareToValue = (int)TieBreak2Numeric.Value,
				TieBreaker3CompareToValue = (int)TieBreak3Numeric.Value,
				FromSlot = FromSlot,
				FrameLength = FrameLength,
				Attempts = Attempts,
				Frames = Frames,
				MemoryDomain = _currentDomain.Name,
				BigEndian = _bigEndian,
				DataSize = _dataSize
			};

			var json = ConfigService.SaveWithType(data);

			File.WriteAllText(path, json);
			CurrentFileName = path;
			Settings.RecentBotFiles.Add(CurrentFileName);
			MessageLabel.Text = $"{Path.GetFileName(CurrentFileName)} saved";
		}

		#endregion

		public bool HasFrameAdvanced()
		{
			// If the emulator frame is different from the last time it tried calling
			// the function then we can continue, otherwise we need to stop.
			return _lastFrameAdvanced != Emulator.Frame;
		}
		private void SetupControlsAndProperties()
		{
			MaximizeAddressBox.SetHexProperties(_currentDomain.Size);
			TieBreaker1Box.SetHexProperties(_currentDomain.Size);
			TieBreaker2Box.SetHexProperties(_currentDomain.Size);
			TieBreaker3Box.SetHexProperties(_currentDomain.Size);

			StartFromSlotBox.SelectedIndex = 0;

			const int startY = 0;
			const int lineHeight = 30;
			const int marginLeft = 15;
			int accumulatedY = 0;
			int count = 0;

			ControlProbabilityPanel.SuspendLayout();
			ControlProbabilityPanel.Controls.Clear();
			foreach (var button in Emulator.ControllerDefinition.BoolButtons)
			{
				var control = new BotControlsRow
				{
					ButtonName = button,
					Probability = 0.0,
					Location = new Point(marginLeft, startY + accumulatedY),
					TabIndex = count + 1,
					ProbabilityChangedCallback = AssessRunButtonStatus
				};
				control.Scale(UIHelper.AutoScaleFactor);

				ControlProbabilityPanel.Controls.Add(control);
				accumulatedY += lineHeight;
				count++;
			}

			ControlProbabilityPanel.ResumeLayout();

			if (Settings.RecentBotFiles.AutoLoad)
			{
				LoadFileFromRecent(Settings.RecentBotFiles.MostRecent);
			}

			UpdateBotStatusIcon();
		}

		private void SetMemoryDomain(string name)
		{
			_currentDomain = MemoryDomains[name];
			_bigEndian = MemoryDomains[name].EndianType == MemoryDomain.Endian.Big;

			MaximizeAddressBox.SetHexProperties(_currentDomain.Size);
			TieBreaker1Box.SetHexProperties(_currentDomain.Size);
			TieBreaker2Box.SetHexProperties(_currentDomain.Size);
			TieBreaker3Box.SetHexProperties(_currentDomain.Size);
		}

		private int GetRamValue(int addr)
		{
			var val = _dataSize switch
			{
				1 => _currentDomain.PeekByte(addr),
				2 => _currentDomain.PeekUshort(addr, _bigEndian),
				4 => (int) _currentDomain.PeekUint(addr, _bigEndian),
				_ => _currentDomain.PeekByte(addr)
			};

			return val;
		}

		private void Update(bool fast)
		{
			if (_doNotUpdateValues)
			{
				return;
			}

			if (!HasFrameAdvanced())
			{
				return;
			}

			if (_replayMode)
			{
				int index = Emulator.Frame - _startFrame;

				if (index < _bestBotAttempt.Log.Count)
				{
					var logEntry = _bestBotAttempt.Log[index];
					var controller = MovieSession.GenerateMovieController();
					controller.SetFromMnemonic(logEntry);

					foreach (var button in controller.Definition.BoolButtons)
					{
						// TODO: make an input adapter specifically for the bot?
						Global.InputManager.ButtonOverrideAdapter.SetButton(button, controller.IsPressed(button));
					}
				}
				else
				{
					FinishReplay();
				}
			}
			else if (_isBotting)
			{

				if (Emulator.Frame >= _targetFrame)
				{
					Attempts++;
					Frames += FrameLength;

					_currentBotAttempt.Maximize = MaximizeValue;
					_currentBotAttempt.TieBreak1 = TieBreaker1Value;
					_currentBotAttempt.TieBreak2 = TieBreaker2Value;
					_currentBotAttempt.TieBreak3 = TieBreaker3Value;
					PlayBestButton.Enabled = true;

					if (_bestBotAttempt == null || IsBetter(_bestBotAttempt, _currentBotAttempt))
					{
						_bestBotAttempt = _currentBotAttempt;
						UpdateBestAttempt();
					}

					_currentBotAttempt = new BotAttempt { Attempt = Attempts };
					MainForm.LoadQuickSave(SelectedSlot, false, true);
				}

				// Before this would have 2 additional hits before the frame even advanced, making the amount of inputs greater than the number of frames to test.
				if (_currentBotAttempt.Log.Count < FrameLength) //aka do not Add more inputs than there are Frames to test
				{
					PressButtons();
					_lastFrameAdvanced = Emulator.Frame;
				}
			}
		}

		private void FinishReplay()
		{
			MainForm.PauseEmulator();
			_startFrame = 0;
			_replayMode = false;
			UpdateBotStatusIcon();
			MessageLabel.Text = "Replay stopped";
		}

		private bool IsBetter(BotAttempt comparison, BotAttempt current)
		{
			if (!TestValue(MainComparisonType, current.Maximize, comparison.Maximize))
			{
				return false;
			}

			if (current.Maximize == comparison.Maximize)
			{
				if (!TestValue(Tie1ComparisonType, current.TieBreak1, comparison.TieBreak1))
				{
					return false;
				}

				if (current.TieBreak1 == comparison.TieBreak1)
				{
					if (!TestValue(Tie2ComparisonType, current.TieBreak2, comparison.TieBreak2))
					{
						return false;
					}

					if (current.TieBreak2 == comparison.TieBreak2)
					{
						if (!TestValue(Tie3ComparisonType, current.TieBreak3, current.TieBreak3))
						{
							return false;
						}
					}
				}
			}

			return true;
		}

		private bool TestValue(byte operation, int currentValue, int bestValue)
		{
			return operation switch
			{
				0 => (currentValue > bestValue),
				1 => (currentValue >= bestValue),
				2 => (currentValue == bestValue),
				3 => (currentValue <= bestValue),
				4 => (currentValue < bestValue),
				_ => false
			};
		}

		private void UpdateBestAttempt()
		{
			if (_bestBotAttempt != null)
			{
				ClearBestButton.Enabled = true;
				BestAttemptNumberLabel.Text = _bestBotAttempt.Attempt.ToString();
				BestMaximizeBox.Text = _bestBotAttempt.Maximize.ToString();
				BestTieBreak1Box.Text = _bestBotAttempt.TieBreak1.ToString();
				BestTieBreak2Box.Text = _bestBotAttempt.TieBreak2.ToString();
				BestTieBreak3Box.Text = _bestBotAttempt.TieBreak3.ToString();

				var sb = new StringBuilder();
				foreach (var logEntry in _bestBotAttempt.Log)
				{
					sb.AppendLine(logEntry);
				}
				BestAttemptLogLabel.Text = sb.ToString();
				PlayBestButton.Enabled = true;
			}
			else
			{
				ClearBestButton.Enabled = false;
				BestAttemptNumberLabel.Text = "";
				BestMaximizeBox.Text = "";
				BestTieBreak1Box.Text = "";
				BestTieBreak2Box.Text = "";
				BestTieBreak3Box.Text = "";
				BestAttemptLogLabel.Text = "";
				PlayBestButton.Enabled = false;
			}
		}

		private void PressButtons()
		{
			var rand = new Random((int)DateTime.Now.Ticks);

			foreach (var button in Emulator.ControllerDefinition.BoolButtons)
			{
				double probability = _cachedControlProbabilities[button];
				bool pressed = !(rand.Next(100) < probability);

				Global.InputManager.ClickyVirtualPadController.SetBool(button, pressed);
			}

			_currentBotAttempt.Log.Add(_logGenerator.GenerateLogEntry());
		}

		private void StartBot()
		{
			var message = CanStart();
			if (!string.IsNullOrWhiteSpace(message))
			{
				MessageBox.Show(message);
				return;
			}

			_isBotting = true;
			ControlsBox.Enabled = false;
			StartFromSlotBox.Enabled = false;
			RunBtn.Visible = false;
			StopBtn.Visible = true;
			GoalGroupBox.Enabled = false;
			_currentBotAttempt = new BotAttempt { Attempt = Attempts };

			if (MovieSession.Movie.IsRecording())
			{
				_oldCountingSetting = MovieSession.Movie.IsCountingRerecords;
				MovieSession.Movie.IsCountingRerecords = false;
			}

			_doNotUpdateValues = true;
			MainForm.LoadQuickSave(SelectedSlot, false, true); // Triggers an UpdateValues call
			_doNotUpdateValues = false;

			_targetFrame = Emulator.Frame + (int)FrameLengthNumeric.Value;

			_previousDisplayMessage = Config.DisplayMessages;
			Config.DisplayMessages = false;

			MainForm.UnpauseEmulator();
			if (Settings.TurboWhenBotting)
			{
				SetMaxSpeed();
			}

			if (InvisibleEmulationCheckBox.Checked)
			{
				_previousInvisibleEmulation = MainForm.InvisibleEmulation;
				MainForm.InvisibleEmulation = true;
			}

			UpdateBotStatusIcon();
			MessageLabel.Text = "Running...";
			_cachedControlProbabilities = ControlProbabilities;
			_logGenerator = MovieSession.Movie.LogGeneratorInstance(Global.InputManager.ClickyVirtualPadController);
		}

		private string CanStart()
		{
			if (!ControlProbabilities.Any(cp => cp.Value > 0))
			{
				return "At least one control must have a probability greater than 0.";
			}

			if (!MaximizeAddressBox.ToRawInt().HasValue)
			{
				return "A main value address is required.";
			}

			if (FrameLengthNumeric.Value == 0)
			{
				return "A frame count greater than 0 is required";
			}

			return null;
		}

		private void StopBot()
		{
			RunBtn.Visible = true;
			StopBtn.Visible = false;
			_isBotting = false;
			_targetFrame = 0;
			ControlsBox.Enabled = true;
			StartFromSlotBox.Enabled = true;
			_targetFrame = 0;
			_currentBotAttempt = null;
			GoalGroupBox.Enabled = true;

			if (MovieSession.Movie.IsRecording())
			{
				MovieSession.Movie.IsCountingRerecords = _oldCountingSetting;
			}

			Config.DisplayMessages = _previousDisplayMessage;
			MainForm.InvisibleEmulation = _previousInvisibleEmulation;
			MainForm.PauseEmulator();
			SetNormalSpeed();
			UpdateBotStatusIcon();
			MessageLabel.Text = "Bot stopped";
		}

		private void UpdateBotStatusIcon()
		{
			if (_replayMode)
			{
				BotStatusButton.Image = Properties.Resources.Play;
				BotStatusButton.ToolTipText = "Replaying best result";
			}
			else if (_isBotting)
			{
				BotStatusButton.Image = Properties.Resources.RecordHS;
				BotStatusButton.ToolTipText = "Botting in progress";
			}
			else
			{
				BotStatusButton.Image = Properties.Resources.Pause;
				BotStatusButton.ToolTipText = "Bot is currently not running";
			}
		}

		private void SetMaxSpeed()
		{
			MainForm.Unthrottle();
		}

		private void SetNormalSpeed()
		{
			MainForm.Throttle();
		}

		private void AssessRunButtonStatus()
		{
			RunBtn.Enabled =
				FrameLength > 0
				&& !string.IsNullOrWhiteSpace(MaximizeAddressBox.Text)
				&& ControlProbabilities.Any(kvp => kvp.Value > 0);
		}

		/// <summary>
		/// Updates comparison bot attempt with current best bot attempt values for values where the "best" radio button is selected
		/// </summary>
		private void UpdateComparisonBotAttempt()
		{
			if (_bestBotAttempt == null)
			{
				if (MainBestRadio.Checked)
				{
					_comparisonBotAttempt.Maximize = 0;
				}

				if (TieBreak1BestRadio.Checked)
				{
					_comparisonBotAttempt.TieBreak1 = 0;
				}

				if (TieBreak2BestRadio.Checked)
				{
					_comparisonBotAttempt.TieBreak2 = 0;
				}

				if (TieBreak3BestRadio.Checked)
				{
					_comparisonBotAttempt.TieBreak3 = 0;
				}
			}
			else
			{
				if (MainBestRadio.Checked && _bestBotAttempt.Maximize != _comparisonBotAttempt.Maximize)
				{
					_comparisonBotAttempt.Maximize = _bestBotAttempt.Maximize;
				}

				if (TieBreak1BestRadio.Checked && _bestBotAttempt.TieBreak1 != _comparisonBotAttempt.TieBreak1)
				{
					_comparisonBotAttempt.TieBreak1 = _bestBotAttempt.TieBreak1;
				}

				if (TieBreak2BestRadio.Checked && _bestBotAttempt.TieBreak2 != _comparisonBotAttempt.TieBreak2)
				{
					_comparisonBotAttempt.TieBreak2 = _bestBotAttempt.TieBreak2;
				}

				if (TieBreak3BestRadio.Checked && _bestBotAttempt.TieBreak3 != _comparisonBotAttempt.TieBreak3)
				{
					_comparisonBotAttempt.TieBreak3 = _bestBotAttempt.TieBreak3;
				}
			}
		}

		private void MainBestRadio_CheckedChanged(object sender, EventArgs e)
		{
			if (sender is RadioButton radioButton && radioButton.Checked)
			{
				MainValueNumeric.Enabled = false;
				_comparisonBotAttempt.Maximize = _bestBotAttempt?.Maximize ?? 0;
			}
		}

		private void Tiebreak1BestRadio_CheckedChanged(object sender, EventArgs e)
		{
			if (sender is RadioButton radioButton && radioButton.Checked)
			{
				TieBreak1Numeric.Enabled = false;
				_comparisonBotAttempt.TieBreak1 = _bestBotAttempt?.TieBreak1 ?? 0;
			}
		}

		private void Tiebreak2BestRadio_CheckedChanged(object sender, EventArgs e)
		{
			if (sender is RadioButton radioButton && radioButton.Checked)
			{
				TieBreak2Numeric.Enabled = false;
				_comparisonBotAttempt.TieBreak2 = _bestBotAttempt?.TieBreak2 ?? 0;
			}
		}

		private void Tiebreak3BestRadio_CheckedChanged(object sender, EventArgs e)
		{
			if (sender is RadioButton radioButton && radioButton.Checked)
			{
				TieBreak3Numeric.Enabled = false;
				_comparisonBotAttempt.TieBreak3 = _bestBotAttempt?.TieBreak3 ?? 0;
			}
		}

		private void MainValueRadio_CheckedChanged(object sender, EventArgs e)
		{
			if (sender is RadioButton radioButton && radioButton.Checked)
			{
				MainValueNumeric.Enabled = true;
				_comparisonBotAttempt.Maximize = (int)MainValueNumeric.Value;
			}
		}

		private void TieBreak1ValueRadio_CheckedChanged(object sender, EventArgs e)
		{
			if (sender is RadioButton radioButton && radioButton.Checked)
			{
				TieBreak1Numeric.Enabled = true;
				_comparisonBotAttempt.TieBreak1 = (int)TieBreak1Numeric.Value;
			}
		}

		private void TieBreak2ValueRadio_CheckedChanged(object sender, EventArgs e)
		{
			if (sender is RadioButton radioButton && radioButton.Checked)
			{
				TieBreak2Numeric.Enabled = true;
				_comparisonBotAttempt.TieBreak2 = (int)TieBreak2Numeric.Value;
			}
		}

		private void TieBreak3ValueRadio_CheckedChanged(object sender, EventArgs e)
		{
			if (sender is RadioButton radioButton && radioButton.Checked)
			{
				TieBreak3Numeric.Enabled = true;
				_comparisonBotAttempt.TieBreak3 = (int)TieBreak3Numeric.Value;
			}
		}

		private void MainValueNumeric_ValueChanged(object sender, EventArgs e)
		{
			NumericUpDown numericUpDown = (NumericUpDown)sender;
			_comparisonBotAttempt.Maximize = (int)numericUpDown.Value;
		}

		private void TieBreak1Numeric_ValueChanged(object sender, EventArgs e)
		{
			NumericUpDown numericUpDown = (NumericUpDown)sender;
			_comparisonBotAttempt.TieBreak1 = (int)numericUpDown.Value;
		}

		private void TieBreak2Numeric_ValueChanged(object sender, EventArgs e)
		{
			NumericUpDown numericUpDown = (NumericUpDown)sender;
			_comparisonBotAttempt.TieBreak2 = (int)numericUpDown.Value;
		}

		private void TieBreak3Numeric_ValueChanged(object sender, EventArgs e)
		{
			NumericUpDown numericUpDown = (NumericUpDown)sender;
			_comparisonBotAttempt.TieBreak3 = (int)numericUpDown.Value;
		}

		// Copy to Clipboard
		private void btnCopyBestInput_Click(object sender, EventArgs e)
		{
			Clipboard.SetText(BestAttemptLogLabel.Text);
		}

		private void HelpToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Process.Start("http://tasvideos.org/Bizhawk/BasicBot.html");
		}

		private void InvisibleEmulationCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			Settings.InvisibleEmulation ^= true;
		}

		private void MaximizeAddressBox_TextChanged(object sender, EventArgs e)
		{
			AssessRunButtonStatus();
		}
	}
}

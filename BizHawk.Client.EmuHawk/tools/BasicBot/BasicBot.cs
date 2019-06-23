using System;
using System.Collections.Generic;
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
	public partial class BasicBot : ToolFormBase , IToolFormAutoConfig
	{
		private const string DialogTitle = "Basic Bot";

		private string _currentFileName = "";

		private string CurrentFileName
		{
			get { return _currentFileName; }
			set
			{
				_currentFileName = value;

				if (!string.IsNullOrWhiteSpace(_currentFileName))
				{
					Text = $"{DialogTitle} - {Path.GetFileNameWithoutExtension(_currentFileName)}";
				}
				else
				{
					Text = DialogTitle;
				}
			}

		}

		private bool _isBotting = false;
		private long _attempts = 1;
		private long _frames = 0;
		private int _targetFrame = 0;
		private bool _oldCountingSetting = false;
		private BotAttempt _currentBotAttempt = null;
		private BotAttempt _bestBotAttempt = null;
		private BotAttempt _comparisonBotAttempt = null;
		private bool _replayMode = false;
		private int _startFrame = 0;
		private string _lastRom = "";

		private bool _dontUpdateValues = false;

		private MemoryDomain _currentDomain;
		private bool _bigEndian;
		private int _dataSize;

		private Dictionary<string, double> _cachedControlProbabilities;
		private ILogEntryGenerator _logGenerator;

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
			public BasicBotSettings()
			{
				RecentBotFiles = new RecentFiles();
				TurboWhenBotting = true;
			}

			public RecentFiles RecentBotFiles { get; set; }
			public bool TurboWhenBotting { get; set; }
		}

		#endregion

		#region Initialize

		public BasicBot()
		{
			InitializeComponent();
			Text = DialogTitle;
			Settings = new BasicBotSettings();

			_comparisonBotAttempt = new BotAttempt();
		}

		private void BasicBot_Load(object sender, EventArgs e)
		{

		}

		#endregion

		#region UI Bindings

		private Dictionary<string, double> ControlProbabilities
		{
			get
			{
				return ControlProbabilityPanel.Controls
					.OfType<BotControlsRow>()
					.ToDictionary(tkey => tkey.ButtonName, tvalue => tvalue.Probability);
			}
		}

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
			get { return _attempts; }
			set
			{
				_attempts = value;
				AttemptsLabel.Text = _attempts.ToString();
			}
		}

		private long Frames
		{
			get { return _frames; }
			set
			{
				_frames = value;
				FramesLabel.Text = _frames.ToString();
			}
		}

		private int FrameLength
		{
			get { return (int)FrameLengthNumeric.Value; }
			set { FrameLengthNumeric.Value = value; }
		}

		public int MaximizeAddress
		{
			get
			{
				int? addr = MaximizeAddressBox.ToRawInt();
				if (addr.HasValue)
				{
					return addr.Value;
				}

				return 0;
			}

			set
			{
				MaximizeAddressBox.SetFromRawInt(value);
			}
		}

		public int MaximizeValue
		{
			get
			{
				int? addr = MaximizeAddressBox.ToRawInt();
				if (addr.HasValue)
				{
					return GetRamvalue(addr.Value);
				}

				return 0;
			}
		}

		public int TieBreaker1Address
		{
			get
			{
				int? addr = TieBreaker1Box.ToRawInt();
				if (addr.HasValue)
				{
					return addr.Value;
				}

				return 0;
			}

			set
			{
				TieBreaker1Box.SetFromRawInt(value);
			}
		}

		public int TieBreaker1Value
		{
			get
			{
				int? addr = TieBreaker1Box.ToRawInt();
				if (addr.HasValue)
				{
					return GetRamvalue(addr.Value);
				}

				return 0;
			}
		}

		public int TieBreaker2Address
		{
			get
			{
				int? addr = TieBreaker2Box.ToRawInt();
				if (addr.HasValue)
				{
					return addr.Value;
				}

				return 0;
			}

			set
			{
				TieBreaker2Box.SetFromRawInt(value);
			}
		}

		public int TieBreaker2Value
		{
			get
			{
				int? addr = TieBreaker2Box.ToRawInt();
				if (addr.HasValue)
				{
					return GetRamvalue(addr.Value);
				}

				return 0;
			}
		}

		public int TieBreaker3Address
		{
			get
			{
				int? addr = TieBreaker3Box.ToRawInt();
				if (addr.HasValue)
				{
					return addr.Value;
				}

				return 0;
			}

			set
			{
				TieBreaker3Box.SetFromRawInt(value);
			}
		}

		public int TieBreaker3Value
		{
			get
			{
				int? addr = TieBreaker3Box.ToRawInt();
				if (addr.HasValue)
				{
					return GetRamvalue(addr.Value);
				}

				return 0;
			}
		}

		public byte MainComparisonType
		{
			get
			{
				return (byte)MainOperator.SelectedIndex;
			}
			set
			{
				if (value < 5) MainOperator.SelectedIndex = value;
				else MainOperator.SelectedIndex = 0;
			}
		}

		public byte Tie1ComparisonType
		{
			get
			{
				return (byte)Tiebreak1Operator.SelectedIndex;
			}
			set
			{
				if (value < 5) Tiebreak1Operator.SelectedIndex = value;
				else Tiebreak1Operator.SelectedIndex = 0;
			}
		}

		public byte Tie2ComparisonType
		{
			get
			{
				return (byte)Tiebreak2Operator.SelectedIndex;
			}
			set
			{
				if (value < 5) Tiebreak2Operator.SelectedIndex = value;
				else Tiebreak2Operator.SelectedIndex = 0;
			}
		}

		public byte Tie3ComparisonType
		{
			get
			{
				return (byte)Tiebreak3Operator.SelectedIndex;
			}
			set
			{
				if (value < 5) Tiebreak3Operator.SelectedIndex = value;
				else Tiebreak3Operator.SelectedIndex = 0;
			}
		}

		public string FromSlot
		{
			get
			{
				return StartFromSlotBox.SelectedItem != null 
					? StartFromSlotBox.SelectedItem.ToString()
					: "";
			}

			set
			{
				var item = StartFromSlotBox.Items
					.OfType<object>()
					.FirstOrDefault(o => o.ToString() == value);

				if (item != null)
				{
					StartFromSlotBox.SelectedItem = item;
				}
				else
				{
					StartFromSlotBox.SelectedItem = null;
				}
			}
		}

		#endregion

		#region IToolForm Implementation

		public bool UpdateBefore { get { return true; } }

		public void NewUpdate(ToolFormUpdateType type) { }

		public void UpdateValues()
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


			if (_lastRom != GlobalWin.MainForm.CurrentlyOpenRom)
			{
				_lastRom = GlobalWin.MainForm.CurrentlyOpenRom;
				SetupControlsAndProperties();
			}
		}

		public bool AskSaveChanges()
		{
			return true;
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
			RecentSubMenu.DropDownItems.AddRange(
				Settings.RecentBotFiles.RecentMenu(LoadFileFromRecent, true));
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
					PathManager.MakeAbsolutePath(Global.Config.PathEntries.ToolsPathFragment, null),
					"Bot files",
					"bot"
				);

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
					PathManager.MakeAbsolutePath(Global.Config.PathEntries.ToolsPathFragment, null),
					"Bot files",
					"bot"
				);

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
			_dontUpdateValues = true;
			GlobalWin.MainForm.LoadQuickSave(SelectedSlot, false, true); // Triggers an UpdateValues call
			_dontUpdateValues = false;
			_startFrame = Emulator.Frame;
			SetNormalSpeed();
			UpdateBotStatusIcon();
			MessageLabel.Text = "Replaying";
			GlobalWin.MainForm.UnpauseEmulator();
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
			public BotAttempt()
			{
				Log = new List<string>();
			}

			public long Attempt { get; set; }
			public int Maximize { get; set; }
			public int TieBreak1 { get; set; }
			public int TieBreak2 { get; set; }
			public int TieBreak3 { get; set; }
			public byte ComparisonTypeMain { get; set; }
			public byte ComparisonTypeTie1 { get; set; }
			public byte ComparisonTypeTie2 { get; set; }
			public byte ComparisonTypeTie3 { get; set; }

			public List<string> Log { get; set; }
		}

		private class BotData
		{
			public BotData()
			{
				MainCompareToBest = true;
				TieBreaker1CompareToBest = true;
				TieBreaker2CompareToBest = true;
				TieBreaker3CompareToBest = true;
			}

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
			public bool MainCompareToBest { get; set; }
			public bool TieBreaker1CompareToBest { get; set; }
			public bool TieBreaker2CompareToBest { get; set; }
			public bool TieBreaker3CompareToBest { get; set; }
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

		private void SetupControlsAndProperties()
		{
			MaximizeAddressBox.SetHexProperties(_currentDomain.Size);
			TieBreaker1Box.SetHexProperties(_currentDomain.Size);
			TieBreaker2Box.SetHexProperties(_currentDomain.Size);
			TieBreaker3Box.SetHexProperties(_currentDomain.Size);

			StartFromSlotBox.SelectedIndex = 0;

			int starty = 0;
			int accumulatedy = 0;
			int lineHeight = 30;
			int marginLeft = 15;
			int count = 0;

			ControlProbabilityPanel.Controls.Clear();

			foreach (var button in Emulator.ControllerDefinition.BoolButtons)
			{
				var control = new BotControlsRow
				{
					ButtonName = button,
					Probability = 0.0,
					Location = new Point(marginLeft, starty + accumulatedy),
					TabIndex = count + 1,
					ProbabilityChangedCallback = AssessRunButtonStatus
				};

				ControlProbabilityPanel.Controls.Add(control);
				accumulatedy += lineHeight;
				count++;
			}

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

		private int GetRamvalue(int addr)
		{
			int val;
			switch (_dataSize)
			{
				default:
				case 1:
					val = _currentDomain.PeekByte(addr);
					break;
				case 2:
					val = _currentDomain.PeekUshort(addr, _bigEndian);
					break;
				case 4:
					val = (int)_currentDomain.PeekUint(addr, _bigEndian);
					break;
			}

			return val;
		}

		private void Update(bool fast)
		{
			if (_dontUpdateValues)
			{
				return;
			}

			if (_replayMode)
			{
				int index = Emulator.Frame - _startFrame;

				if (index < _bestBotAttempt.Log.Count)
				{
					var logEntry = _bestBotAttempt.Log[index];
					var lg = Global.MovieSession.MovieControllerInstance();
					lg.SetControllersAsMnemonic(logEntry);

					foreach (var button in lg.Definition.BoolButtons)
					{
						// TODO: make an input adapter specifically for the bot?
						Global.LuaAndAdaptor.SetButton(button, lg.IsPressed(button));
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
					GlobalWin.MainForm.LoadQuickSave(SelectedSlot, false, true);
				}

				PressButtons();
			}
		}

		private void FinishReplay()
		{
			GlobalWin.MainForm.PauseEmulator();
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
			else if (current.Maximize == comparison.Maximize)
			{
				if (!TestValue(Tie1ComparisonType, current.TieBreak1, comparison.TieBreak1))
				{
					return false;
				}
				else if (current.TieBreak1 == comparison.TieBreak1)
				{
					if (!TestValue(Tie2ComparisonType, current.TieBreak2, comparison.TieBreak2))
					{
						return false;
					}
					else if (current.TieBreak2 == comparison.TieBreak2)
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
			switch (operation)
			{
				case 0:
					return currentValue > bestValue;
				case 1:
					return currentValue >= bestValue;
				case 2:
					return currentValue == bestValue;
				case 3:
					return currentValue <= bestValue;
				case 4:
					return currentValue < bestValue;
			}
			return false;
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

			var buttonLog = new Dictionary<string, bool>();

			foreach (var button in Emulator.ControllerDefinition.BoolButtons)
			{
				double probability = _cachedControlProbabilities[button];
				bool pressed = !(rand.Next(100) < probability);

				buttonLog.Add(button, pressed);
				Global.ClickyVirtualPadController.SetBool(button, pressed);
			}

			_currentBotAttempt.Log.Add(_logGenerator.GenerateLogEntry());
		}

		private void StartBot()
		{
			if (!CanStart())
			{
				MessageBox.Show("Unable to run with current settings");
				return;
			}

			_isBotting = true;
			ControlsBox.Enabled = false;
			StartFromSlotBox.Enabled = false;
			RunBtn.Visible = false;
			StopBtn.Visible = true;
			GoalGroupBox.Enabled = false;
			_currentBotAttempt = new BotAttempt { Attempt = Attempts };

			if (Global.MovieSession.Movie.IsRecording)
			{
				_oldCountingSetting = Global.MovieSession.Movie.IsCountingRerecords;
				Global.MovieSession.Movie.IsCountingRerecords = false;
			}

			_dontUpdateValues = true;
			GlobalWin.MainForm.LoadQuickSave(SelectedSlot, false, true); // Triggers an UpdateValues call
			_dontUpdateValues = false;

			_targetFrame = Emulator.Frame + (int)FrameLengthNumeric.Value;

			GlobalWin.MainForm.UnpauseEmulator();
			if (Settings.TurboWhenBotting)
			{
				SetMaxSpeed();
			}

			UpdateBotStatusIcon();
			MessageLabel.Text = "Running...";
			_cachedControlProbabilities = ControlProbabilities;
			_logGenerator = Global.MovieSession.LogGeneratorInstance();
			_logGenerator.SetSource(Global.ClickyVirtualPadController);
		}

		private bool CanStart()
		{
			if (!ControlProbabilities.Any(cp => cp.Value > 0))
			{
				return false;
			}

			if (!MaximizeAddressBox.ToRawInt().HasValue)
			{
				return false;
			}

			if (FrameLengthNumeric.Value == 0)
			{
				return false;
			}

			return true;
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

			if (Global.MovieSession.Movie.IsRecording)
			{
				Global.MovieSession.Movie.IsCountingRerecords = _oldCountingSetting;
			}

			GlobalWin.MainForm.PauseEmulator();
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
			GlobalWin.MainForm.Unthrottle();
		}

		private void SetNormalSpeed()
		{
			GlobalWin.MainForm.Throttle();
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
			if(_bestBotAttempt == null)
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
					_comparisonBotAttempt.TieBreak2= 0;
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
			RadioButton radioButton = (RadioButton)sender;
			if (radioButton.Checked)
			{
				this.MainValueNumeric.Enabled = false;
				_comparisonBotAttempt.Maximize = _bestBotAttempt == null ? 0 : _bestBotAttempt.Maximize;
			}
		}

		private void Tiebreak1BestRadio_CheckedChanged(object sender, EventArgs e)
		{
			RadioButton radioButton = (RadioButton)sender;
			if (radioButton.Checked)
			{
				this.TieBreak1Numeric.Enabled = false;
				_comparisonBotAttempt.TieBreak1 = _bestBotAttempt == null ? 0 : _bestBotAttempt.TieBreak1;
			}
		}

		private void Tiebreak2BestRadio_CheckedChanged(object sender, EventArgs e)
		{
			RadioButton radioButton = (RadioButton)sender;
			if (radioButton.Checked)
			{
				this.TieBreak2Numeric.Enabled = false;
				_comparisonBotAttempt.TieBreak2 = _bestBotAttempt == null ? 0 : _bestBotAttempt.TieBreak2;
			}
		}

		private void Tiebreak3BestRadio_CheckedChanged(object sender, EventArgs e)
		{
			RadioButton radioButton = (RadioButton)sender;
			if (radioButton.Checked)
			{
				this.TieBreak3Numeric.Enabled = false;
				_comparisonBotAttempt.TieBreak3 = _bestBotAttempt == null ? 0 : _bestBotAttempt.TieBreak3;
			}
		}

		private void MainValueRadio_CheckedChanged(object sender, EventArgs e)
		{
			RadioButton radioButton = (RadioButton)sender;
			if (radioButton.Checked)
			{
				this.MainValueNumeric.Enabled = true;
				_comparisonBotAttempt.Maximize = (int)this.MainValueNumeric.Value;
			}
		}

		private void TieBreak1ValueRadio_CheckedChanged(object sender, EventArgs e)
		{
			RadioButton radioButton = (RadioButton)sender;
			if (radioButton.Checked)
			{
				this.TieBreak1Numeric.Enabled = true;
				_comparisonBotAttempt.TieBreak1 = (int)this.TieBreak1Numeric.Value;
			}
		}

		private void TieBreak2ValueRadio_CheckedChanged(object sender, EventArgs e)
		{
			RadioButton radioButton = (RadioButton)sender;
			if (radioButton.Checked)
			{
				this.TieBreak2Numeric.Enabled = true;
				_comparisonBotAttempt.TieBreak2 = (int)this.TieBreak2Numeric.Value;
			}
		}

		private void TieBreak3ValueRadio_CheckedChanged(object sender, EventArgs e)
		{
			RadioButton radioButton = (RadioButton)sender;
			if (radioButton.Checked)
			{
				this.TieBreak3Numeric.Enabled = true;
				_comparisonBotAttempt.TieBreak3 = (int)this.TieBreak3Numeric.Value;
			}
		}

		private void MainValueNumeric_ValueChanged(object sender, EventArgs e)
		{
			NumericUpDown numericUpDown = (NumericUpDown)sender;
			this._comparisonBotAttempt.Maximize = (int)numericUpDown.Value;
		}

		private void TieBreak1Numeric_ValueChanged(object sender, EventArgs e)
		{
			NumericUpDown numericUpDown = (NumericUpDown)sender;
			this._comparisonBotAttempt.TieBreak1 = (int)numericUpDown.Value;
		}

		private void TieBreak2Numeric_ValueChanged(object sender, EventArgs e)
		{
			NumericUpDown numericUpDown = (NumericUpDown)sender;
			this._comparisonBotAttempt.TieBreak2 = (int)numericUpDown.Value;
		}

		private void TieBreak3Numeric_ValueChanged(object sender, EventArgs e)
		{
			NumericUpDown numericUpDown = (NumericUpDown)sender;
			this._comparisonBotAttempt.TieBreak3 = (int)numericUpDown.Value;
		}

	}
}

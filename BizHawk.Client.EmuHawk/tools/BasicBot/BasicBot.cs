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
	public partial class BasicBot : Form , IToolFormAutoConfig
	{
		private const string DialogTitle = "Basic Bot";

		private string _currentFileName = string.Empty;

		private string CurrentFileName
		{
			get { return _currentFileName; }
			set
			{
				_currentFileName = value;

				if (!string.IsNullOrWhiteSpace(_currentFileName))
				{
					Text = DialogTitle + " - " + Path.GetFileNameWithoutExtension(_currentFileName);
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
		private bool _replayMode = false;
		private int _startFrame = 0;

		private bool _dontUpdateValues = false;

		private MemoryDomain _currentDomain;

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
		}

		private void BasicBot_Load(object sender, EventArgs e)
		{
			MaximizeAddressBox.SetHexProperties(MemoryDomains.MainMemory.Size);
			TieBreaker1Box.SetHexProperties(MemoryDomains.MainMemory.Size);
			TieBreaker2Box.SetHexProperties(MemoryDomains.MainMemory.Size);
			TieBreaker3Box.SetHexProperties(MemoryDomains.MainMemory.Size);

			StartFromSlotBox.SelectedIndex = 0;

			int starty = 0;
			int accumulatedy = 0;
			int lineHeight = 30;
			int marginLeft = 15;
			int count = 0;
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

				return "QuickSave" + num;
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

		public string FromSlot
		{
			get
			{
				return StartFromSlotBox.SelectedItem != null 
					? StartFromSlotBox.SelectedItem.ToString()
					: string.Empty;
			}

			set
			{
				var item = StartFromSlotBox.Items.
					OfType<object>()
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
			}

			// TODO restart logic
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
			CurrentFileName = string.Empty;
			_bestBotAttempt = null;

			ControlProbabilityPanel.Controls
				.OfType<BotControlsRow>()
				.ToList()
				.ForEach(cp => cp.Probability = 0);

			FrameLength = 0;
			MaximizeAddress = 0;
			TieBreaker1Address = 0;
			TieBreaker2Address = 0;
			TieBreaker3Address = 0;
			StartFromSlotBox.SelectedIndex = 0;

			UpdateBestAttempt();
		}

		private void OpenMenuItem_Click(object sender, EventArgs e)
		{
			var file = ToolHelpers.OpenFileDialog(
					CurrentFileName,
					PathManager.GetRomsPath(Global.Game.System), // TODO: bot path
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
			var file = ToolHelpers.SaveFileDialog(
					CurrentFileName,
					PathManager.GetRomsPath(Global.Game.System), // TODO: bot path
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
		}

		private void MemoryDomainsMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			MemoryDomainsMenuItem.DropDownItems.Clear();
			MemoryDomainsMenuItem.DropDownItems.AddRange(
				MemoryDomains.MenuItems(SetMemoryDomain, _currentDomain.Name)
				.ToArray());
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
			UpdateBestAttempt();
		}

		private void PlayBestButton_Click(object sender, EventArgs e)
		{
			StopBot();
			_replayMode = true;
			_dontUpdateValues = true;
			GlobalWin.MainForm.LoadQuickSave(SelectedSlot); // Triggers an UpdateValues call
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
			public List<string> Log { get; set; }
		}

		private class BotData
		{
			public BotAttempt Best { get; set; }
			public Dictionary<string, double> ControlProbabilities { get; set; }
			public int Maximize { get; set; }
			public int TieBreaker1 { get; set; }
			public int TieBreaker2 { get; set; }
			public int TieBreaker3 { get; set; }
			public int FrameLength { get; set; }
			public string FromSlot { get; set; }
			public long Attempts { get; set; }
			public long Frames { get; set; }
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
			FrameLength = botData.FrameLength;
			FromSlot = botData.FromSlot;
			Attempts = botData.Attempts;
			Frames = botData.Frames;

			UpdateBestAttempt();

			if (_bestBotAttempt != null)
			{
				PlayBestButton.Enabled = true;
			}

			CurrentFileName = path;
			Settings.RecentBotFiles.Add(CurrentFileName);
			MessageLabel.Text = Path.GetFileNameWithoutExtension(path) + " loaded";

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
				FromSlot = FromSlot,
				FrameLength = FrameLength,
				Attempts = Attempts,
				Frames = Frames
			};

			var json = ConfigService.SaveWithType(data);

			File.WriteAllText(path, json);
			CurrentFileName = path;
			Settings.RecentBotFiles.Add(CurrentFileName);
			MessageLabel.Text = Path.GetFileName(CurrentFileName) + " saved";
		}

		#endregion

		private void SetMemoryDomain(string name)
		{
			_currentDomain = MemoryDomains[name];
		}

		private int GetRamvalue(int addr)
		{
			// TODO: ability to pick byte size/display type/endian
			return _currentDomain.PeekByte(addr);
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

					foreach (var button in lg.Type.BoolButtons)
					{
						// TODO: make an input adapter specifically for the bot?
						Global.LuaAndAdaptor.SetButton(button, lg.IsPressed(button));
					}
				}
				else // Finished
				{
					GlobalWin.MainForm.PauseEmulator();
					_startFrame = 0;
					_replayMode = false;
					UpdateBotStatusIcon();
					MessageLabel.Text = "Replay stopped";
				}
			}
			else if (_isBotting)
			{
				if (Global.Emulator.Frame >= _targetFrame)
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
					GlobalWin.MainForm.LoadQuickSave(SelectedSlot);
				}

				PressButtons();
			}
		}

		private bool IsBetter(BotAttempt best, BotAttempt current)
		{
			if (current.Maximize > best.Maximize)
			{
				return true;
			}
			else if (current.Maximize == best.Maximize)
			{
				if (current.TieBreak1 > best.TieBreak1)
				{
					return true;
				}
				else if (current.TieBreak1 == best.TieBreak1)
				{
					if (current.TieBreak2 > best.TieBreak2)
					{
						return true;
					}
					else if (current.TieBreak2 == best.TieBreak2)
					{
						if (current.TieBreak3 > current.TieBreak3)
						{
							return true;
						}
					}
				}
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
				BestAttemptNumberLabel.Text = string.Empty;
				BestMaximizeBox.Text = string.Empty;
				BestTieBreak1Box.Text = string.Empty;
				BestTieBreak2Box.Text = string.Empty;
				BestTieBreak3Box.Text = string.Empty;
				BestAttemptLogLabel.Text = string.Empty;
				PlayBestButton.Enabled = false;
			}
		}

		private void PressButtons()
		{
			var rand = new Random((int)DateTime.Now.Ticks);

			var buttonLog = new Dictionary<string, bool>();

			foreach (var button in Emulator.ControllerDefinition.BoolButtons)
			{
				double probability = ControlProbabilities[button];
				bool pressed = !(rand.Next(100) < probability);

				buttonLog.Add(button, pressed);
				Global.ClickyVirtualPadController.SetBool(button, pressed);
			}

			var lg = Global.MovieSession.LogGeneratorInstance();
			lg.SetSource(Global.ClickyVirtualPadController);
			_currentBotAttempt.Log.Add(lg.GenerateLogEntry());
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
			GlobalWin.MainForm.LoadQuickSave(SelectedSlot); // Triggers an UpdateValues call
			_dontUpdateValues = false;

			_targetFrame = Global.Emulator.Frame + (int)FrameLengthNumeric.Value;

			GlobalWin.MainForm.UnpauseEmulator();
			if (Settings.TurboWhenBotting)
			{
				SetMaxSpeed();
			}

			UpdateBotStatusIcon();
			MessageLabel.Text = "Running...";
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
	}
}

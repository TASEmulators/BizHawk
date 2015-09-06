using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using BizHawk.Emulation.Common;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class BasicBot : Form , IToolFormAutoConfig
	{
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

		}

		#endregion

		#region Initialize

		public BasicBot()
		{
			InitializeComponent();
		}

		private void BasicBot_Load(object sender, EventArgs e)
		{
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
					TabIndex = count + 1
				};

				ControlProbabilityPanel.Controls.Add(control);
				accumulatedy += lineHeight;
				count++;
			}
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
			// TODO
		}

		public bool AskSaveChanges()
		{
			return true; // TODO
		}

		#endregion

		#region Control Events

		private void exitToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Close();
		}

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
			_replayMode = true;
			_dontUpdateValues = true;
			GlobalWin.MainForm.LoadQuickSave(SelectedSlot); // Triggers an UpdateValues call
			_dontUpdateValues = false;
			_startFrame = Emulator.Frame;
			GlobalWin.MainForm.UnpauseEmulator();
		}

		#endregion

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

		private int GetRamvalue(int addr)
		{
			// TODO: ability to pick memory domain
			// TODO: ability to pick byte size/display type
			return MemoryDomains.MainMemory.PeekByte(addr);
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
				MessageBox.Show("Please fill out all the things!");
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

			if (GlobalWin.MainForm.EmulatorPaused)
			{
				GlobalWin.MainForm.UnpauseEmulator();
				// TODO: speed!
			}
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
			_attempts = 1;
			_frames = 0;
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
		}
	}
}

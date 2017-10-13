using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Linq;
using System.Text;
using Newtonsoft.Json;


using System.Windows.Forms;


using BizHawk.Client.EmuHawk.ToolExtensions;

using BizHawk.Emulation.Common;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class GyroscopeBot : ToolFormBase , IToolFormAutoConfig
	{
		private const string DialogTitle = "Gyroscope Bot";

		private string _currentFileName = "";

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


		private bool _replayMode = false;
		private string _lastRom = "";

		private bool _dontUpdateValues = false;

		private MemoryDomain _currentDomain;
		private bool _bigEndian;
		private int _dataSize;

		private int _wins = 0;
		private int _losses = 0;
		private string _lastResult = "Unknown";
		private float _winsToLosses = 0;
		private int _totalGames = 0;
		private int _OSDMessageTimeInSeconds = 15;

		private ILogEntryGenerator _logGenerator;
		private TcpClient client;
		
		#region Services and Settings

		[RequiredService]
		private IEmulator Emulator { get; set; }

		// Unused, due to the use of MainForm to loadstate, but this needs to be kept here in order to establish an IStatable dependency
		[RequiredService]
		private IStatable StatableCore { get; set; }

		[RequiredService]
		private IMemoryDomains MemoryDomains { get; set; }

		[ConfigPersist]
		public GyroscopeBotSettings Settings { get; set; }

		public class GyroscopeBotSettings
		{
			public GyroscopeBotSettings()
			{
				RecentBotFiles = new RecentFiles();
				TurboWhenBotting = true;
			}

			public RecentFiles RecentBotFiles { get; set; }
			public bool TurboWhenBotting { get; set; }
		}

		#endregion

		#region sockethandling
		private TcpClient CreateTCPClient(string IP, int port)
		{
			return new TcpClient(IP, port);
		}
		
		private ControllerCommand SendEmulatorGameStateToController()
		{
			ControllerCommand cc = new ControllerCommand();
			try
			{

				
				NetworkStream stream = this.client.GetStream();
				byte[] bytes = new byte[1024];
				// Encode the data string into a byte array. 
				GameState gs = GetCurrentState();
				string data = JsonConvert.SerializeObject(gs);

				byte[] msg = Encoding.ASCII.GetBytes(data);
				stream.Write(msg, 0, msg.Length);


				StringBuilder myCompleteMessage = new StringBuilder();
				if (stream.CanRead)
				{
					byte[] myReadBuffer = new byte[1024];
					int numberOfBytesRead = 0;
					// Incoming message may be larger than the buffer size.
					do
					{
						numberOfBytesRead = stream.Read(myReadBuffer, 0, myReadBuffer.Length);
						myCompleteMessage.AppendFormat("{0}", Encoding.ASCII.GetString(myReadBuffer, 0, numberOfBytesRead));
					}
					while (stream.DataAvailable);
				}
				cc = JsonConvert.DeserializeObject<ControllerCommand>(myCompleteMessage.ToString());
			}
			catch (ArgumentNullException ane)
			{
				cc.type = "__err__" + ane.ToString();
			}
			catch (SocketException se)
			{
				cc.type = "__err__" + se.ToString();
			}
			catch (Exception e)
			{
				cc.type = "__err__" + e.ToString();
			}
			return cc;
		}
		
		#endregion

		#region Initialize

		public GyroscopeBot()
		{
			InitializeComponent();
			Text = DialogTitle;
			Settings = new GyroscopeBotSettings();		
		}

		private void GyroscopeBot_Load(object sender, EventArgs e)
		{
			StartBot();
		}

		#endregion

		#region streetfighter

		public int get_framecount()
		{
			return Emulator.Frame;
		}

		public int get_p1_health()
		{
			return _currentDomain.PeekByte(0x000530);
		}

		public int get_p2_health()
		{
			return _currentDomain.PeekByte(0x000730);
		}

		public int get_p1_character()
		{
			return _currentDomain.PeekByte(0x0005D1);
		}

		public int get_p2_character()
		{
			return _currentDomain.PeekByte(0x0007D1);
		}

		public int get_p1_x()
		{
			// make sure we are little endian
			return _currentDomain.PeekUshort(0x000022, _bigEndian);
		}

		public int get_p2_x()
		{
			return _currentDomain.PeekUshort(0x000026, _bigEndian);
		}

		public int get_p1_y()
		{
			return _currentDomain.PeekByte(0x00050A);
		}

		public int get_p2_y()
		{
			return _currentDomain.PeekByte(0x00070A);
		}

		public bool is_p1_jumping()
		{
			return _currentDomain.PeekByte(0x0005EA) == 1;
		}

		public bool is_p2_jumping()
		{
			return _currentDomain.PeekByte(0x0007EA) == 1;
		}

		public int p_height_delta()
		{
			return _currentDomain.PeekByte(0x0005ED);
		}

		public bool is_p1_crouching()
		{
			return _currentDomain.PeekByte(0x000544) == 1;
		}

		public bool is_p2_crouching()
		{
			return _currentDomain.PeekByte(0x00744) == 1;
		}

		public int get_timer()
		{
			return _currentDomain.PeekByte(0x0018F3);
		}

		public bool is_round_started()
		{
			return get_timer() > 0 && get_timer() <= 152;
		}

		private bool is_round_over()
		{
			return get_p1_health() == 255 || get_p2_health() == 255 || get_timer() <= 0;
		}

		private string get_round_result()
		{
			if (get_p1_health() == 255)
			{
				return "P2";
			}
			else if (get_p2_health() == 255)
			{
				return "P1";
			}
			else
			{
				return "NOT_OVER";
			}
		}

		public Dictionary<string, bool> GetJoypadButtons(int? controller = null)
		{
			var buttons = new Dictionary<string, bool>();
			var adaptor = Global.AutofireStickyXORAdapter;
			foreach (var button in adaptor.Source.Definition.BoolButtons)
			{
				if (!controller.HasValue)
				{
					buttons[button] = adaptor.IsPressed(button);
				}
				else if (button.Length >= 3 && button.Substring(0, 2) == "P" + controller)
				{
					buttons[button.Substring(3)] = adaptor.IsPressed("P" + controller + " " + button.Substring(3));
				}
			}
			return buttons;
		}
		
		public void SetJoypadButtons(Dictionary<string,bool> buttons, int? controller = null)
		{
			try
			{
				foreach (var button in buttons.Keys)
				{
					var invert = false;
					bool? theValue;
					var theValueStr = buttons[button].ToString();

					if (!string.IsNullOrWhiteSpace(theValueStr))
					{
						if (theValueStr.ToLower() == "false")
						{
							theValue = false;
						}
						else if (theValueStr.ToLower() == "true")
						{
							theValue = true;
						}
						else
						{
							invert = true;
							theValue = null;
						}
					}
					else
					{
						theValue = null;
					}

					var toPress = button.ToString();
					if (controller.HasValue)
					{
						toPress = "P" + controller + " " + button;
					}

					if (!invert)
					{
						if (theValue.HasValue) // Force
						{
							Global.LuaAndAdaptor.SetButton(toPress, theValue.Value);
							Global.ActiveController.Overrides(Global.LuaAndAdaptor);
						}
						else // Unset
						{
							Global.LuaAndAdaptor.UnSet(toPress);
							Global.ActiveController.Overrides(Global.LuaAndAdaptor);
						}
					}
					else // Inverse
					{
						Global.LuaAndAdaptor.SetInverse(toPress);
						Global.ActiveController.Overrides(Global.LuaAndAdaptor);
					}
				}
			}
			catch
			{
				/*Eat it*/
			}
		}
		private class PlayerState
		{
			public PlayerState()
			{
			}
			public int character { get; set; }
			public int health { get; set; }
			public int x { get; set; }
			public int y { get; set; }
			public bool jumping { get; set; }
			public bool crouching { get; set; }
			public Dictionary<string, bool> buttons { get; set; }


		}
		private class GameState
		{
			public GameState()
			{
			}
			public PlayerState p1 { get; set; }
			public PlayerState p2 { get; set; }
			public int frame { get; set; }
			public int timer { get; set; }
			public string result { get; set; }
			public bool round_started { get; set; }
			public bool round_over { get; set; }
			public int height_delta { get; set; }
		}

		private GameState GetCurrentState()
		{
			PlayerState p1 = new PlayerState();
			PlayerState p2 = new PlayerState();
			GameState gs = new GameState();
			p1.health = get_p1_health();
			p1.x = get_p1_x();
			p1.y = get_p1_y();
			p1.jumping = is_p1_jumping();
			p1.crouching = is_p1_crouching();
			p1.character = get_p1_character();
			p1.buttons = GetJoypadButtons(1);


			p2.health = get_p2_health();
			p2.x = get_p2_x();
			p2.y = get_p2_y();
			p2.jumping = is_p2_jumping();
			p2.crouching = is_p2_crouching();
			p2.character = get_p2_character();
			p2.buttons = GetJoypadButtons(2);

			gs.p1 = p1;
			gs.p2 = p2;
			gs.result = get_round_result();
			gs.frame = Emulator.Frame;
			gs.timer = get_timer();
			gs.round_started = is_round_started();
			gs.round_over = is_round_over();
			gs.height_delta = p_height_delta();

			return gs;
		}
		#endregion

		#region UI Bindings



		



		





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





	
		private void ClearStatsContextMenuItem_Click(object sender, EventArgs e)
		{
		
		}

		#endregion

		#region Classes

		private class ControllerCommand
		{
			public ControllerCommand() { }
			public string type { get; set; }
			public Dictionary<string, bool> p1 { get; set; }
			public Dictionary<string, bool> p2 { get; set; }
			public int player_count { get; set; }
			public string savegamepath { get; set; }

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
	
			return true;
		}

		private void SaveBotFile(string path)
		{

		}

		#endregion

		private void SetupControlsAndProperties()
		{
			UpdateBotStatusIcon();
		}

		private void SetMemoryDomain(string name)
		{
			_currentDomain = MemoryDomains[name];
			_bigEndian = MemoryDomains[name].EndianType == MemoryDomain.Endian.Big;
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

			if (_isBotting)
			{
				if (is_round_over())
				{
					_totalGames = _totalGames + 1;
					if (get_round_result() == "P1")
					{
						_wins = _wins + 1;
						_lastResult = "Win";

					}
					else
					{
						_losses = _losses + 1;
						_lastResult = "Loss";
					}

					_winsToLosses = (float)_wins / _totalGames;
					GlobalWin.OSD.ClearGUIText();
					GlobalWin.OSD.AddMessageForTime("Game #: " + _totalGames + " Wins: " + _wins + " Losses: " + _losses + " last result: " + _lastResult + " ratio: " + _winsToLosses, _OSDMessageTimeInSeconds);
				}
				string command_type = "";
				do
				{
					// send over the current game state
					ControllerCommand command = SendEmulatorGameStateToController();
					command_type = command.type;
					// get a command back
					// act on the command
					if (command_type == "reset")
					{
						GlobalWin.MainForm.LoadState(command.savegamepath, Path.GetFileName(command.savegamepath));
					}
					else if (command_type == "processing")
					{
						// just do nothing, we're waiting for feedback from the controller.
						// XXX how do we tell the emulator to not advance the frame?

					}
					else
					{
						SetJoypadButtons(command.p1, 1);
						if (command.player_count == 2)
						{
							SetJoypadButtons(command.p2, 2);
						}
					}
				} while (command_type == "processing");
				



				// press the buttons if need be
				//PressButtons();
			}
		}


		private void StartBot()
		{
			if (!CanStart())
			{
				MessageBox.Show("Unable to run with current settings");
				return;
			}

			_isBotting = true;
			RunBtn.Visible = false;
			StopBtn.Visible = true;
			this.client = CreateTCPClient(Global.Config.controller_ip, Global.Config.controller_port);


			Global.Config.SoundEnabled = false;
			GlobalWin.MainForm.UnpauseEmulator();
			SetMaxSpeed();
			GlobalWin.MainForm.ClickSpeedItem(6399);
			//if (Settings.TurboWhenBotting)
			//{
			//	SetMaxSpeed();
			//}

			UpdateBotStatusIcon();
			MessageLabel.Text = "Running...";
			_logGenerator = Global.MovieSession.LogGeneratorInstance();
			_logGenerator.SetSource(Global.ClickyVirtualPadController);
			GlobalWin.OSD.AddMessageForTime(" Wins: " + _wins + " Losses: " + _losses + " last result: " + _lastResult + " ratio: " + _winsToLosses, _OSDMessageTimeInSeconds);

		}

		private bool CanStart()
		{
		

			return true;
		}

		private void StopBot()
		{
			RunBtn.Visible = true;
			StopBtn.Visible = false;
			_isBotting = false;
	
			

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


		

	}
}

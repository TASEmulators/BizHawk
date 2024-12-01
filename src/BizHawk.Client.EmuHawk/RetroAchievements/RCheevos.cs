using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

using BizHawk.BizInvoke;
using BizHawk.Common;
using BizHawk.Common.IOExtensions;
using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

#pragma warning disable BHI1007 // target-typed Exception TODO don't

namespace BizHawk.Client.EmuHawk
{
	public partial class RCheevos : RetroAchievements
	{
		internal static readonly LibRCheevos _lib;

		static RCheevos()
		{
			var resolver = new DynamicLibraryImportResolver(
				OSTailoredCode.IsUnixHost ? "librcheevos.so" : "librcheevos.dll", hasLimitedLifetime: false);
			_lib = BizInvoker.GetInvoker<LibRCheevos>(resolver, CallingConventionAdapters.Native);

			var version = Marshal.PtrToStringAnsi(_lib.rc_version_string());
			Console.WriteLine($"Loaded RCheevos v{version}");

			// init message callbacks
			_errorMessageCallback = ErrorMessageCallback;
			_verboseMessageCallback = VerboseMessageCallback;
			_lib.rc_hash_init_error_message_callback(_errorMessageCallback);
			_lib.rc_hash_init_verbose_message_callback(_verboseMessageCallback);

			// init readers
			_filereader = new(OpenFileCallback, SeekFileCallback, TellFileCallback, ReadFileCallback, CloseFileCallback);
			_cdreader = new(OpenTrackCallback, ReadSectorCallback, CloseTrackCallback, FirstTrackSectorCallback);
			_lib.rc_hash_init_custom_filereader(in _filereader);
			_lib.rc_hash_init_custom_cdreader(in _cdreader);

			_http.DefaultRequestHeaders.UserAgent.ParseAdd(VersionInfo.UserAgentEscaped);
		}

		private IntPtr _runtime;

		private readonly LibRCheevos.rc_runtime_event_handler_t _eventcb;
		private readonly LibRCheevos.rc_runtime_peek_t _peekcb;
		private readonly LibRCheevos.rc_runtime_validate_address_t _validatecb;

		private byte[] _readMap = Array.Empty<byte>();

		private ToolStripMenuItem _hardcoreModeMenuItem;
		private bool _hardcoreMode;

		private bool HardcoreMode
		{
			get => _hardcoreMode;
			set => _hardcoreMode = _hardcoreModeMenuItem.Checked = value;
		}

		private bool _firstRestart = true;

		private void BuildMenu(ToolStripItemCollection raDropDownItems)
		{
			raDropDownItems.Clear();

			var shutDownRAItem = new ToolStripMenuItem("Shutdown RetroAchievements");
			shutDownRAItem.Click += (_, _) => _shutdownRACallback();
			raDropDownItems.Add(shutDownRAItem);

			var autoStartRAItem = new ToolStripMenuItem("Autostart RetroAchievements")
			{
				Checked = _getConfig().RAAutostart,
				CheckOnClick = true,
			};
			autoStartRAItem.CheckedChanged += (_, _) =>
			{
				var config = _getConfig();
				config.RAAutostart = !config.RAAutostart;
			};
			raDropDownItems.Add(autoStartRAItem);

			var loginItem = new ToolStripMenuItem("Login")
			{
				Visible = !LoggedIn
			};
			loginItem.Click += (_, _) =>
			{
				Login();
				_firstRestart = true; // kinda
				Restart();
				LoginStatusChanged();
			};
			raDropDownItems.Add(loginItem);

			var logoutItem = new ToolStripMenuItem("Logout")
			{
				Visible = LoggedIn
			};
			logoutItem.Click += (_, _) =>
			{
				Logout();
				LoginStatusChanged();
			};
			raDropDownItems.Add(logoutItem);

			LoginStatusChanged += () => loginItem.Visible = !LoggedIn;
			LoginStatusChanged += () => logoutItem.Visible = LoggedIn;

			var tss = new ToolStripSeparator();
			raDropDownItems.Add(tss);

			var enableCheevosItem = new ToolStripMenuItem("Enable Achievements")
			{
				Checked = CheevosActive,
				CheckOnClick = true
			};
			enableCheevosItem.CheckedChanged += (_, _) => CheevosActive = !CheevosActive;
			raDropDownItems.Add(enableCheevosItem);

			var enableLboardsItem = new ToolStripMenuItem("Enable Leaderboards")
			{
				Checked = LBoardsActive,
				CheckOnClick = true,
				Enabled = HardcoreMode
			};
			enableLboardsItem.CheckedChanged += (_, _) => LBoardsActive = !LBoardsActive;
			raDropDownItems.Add(enableLboardsItem);

			var enableRichPresenceItem = new ToolStripMenuItem("Enable Rich Presence")
			{
				Checked = RichPresenceActive,
				CheckOnClick = true
			};
			enableRichPresenceItem.CheckedChanged += (_, _) => RichPresenceActive = !RichPresenceActive;
			raDropDownItems.Add(enableRichPresenceItem);

			var enableHardcoreItem = new ToolStripMenuItem("Enable Hardcore Mode")
			{
				Checked = HardcoreMode,
				CheckOnClick = true
			};
			enableHardcoreItem.CheckedChanged += (_, _) =>
			{
				_hardcoreMode = !_hardcoreMode;
				if (HardcoreMode)
				{
					_hardcoreMode = _mainForm.RebootCore(); // unset hardcore mode if we fail to reboot core somehow
				}
				else
				{
					ToSoftcoreMode();
				}

				enableLboardsItem.Enabled = HardcoreMode;
			};
			raDropDownItems.Add(enableHardcoreItem);

			_hardcoreModeMenuItem = enableHardcoreItem;

			var enableSoundEffectsItem = new ToolStripMenuItem("Enable Sound Effects")
			{
				Checked = EnableSoundEffects,
				CheckOnClick = true
			};
			enableSoundEffectsItem.CheckedChanged += (_, _) => EnableSoundEffects = !EnableSoundEffects;
			raDropDownItems.Add(enableSoundEffectsItem);

			var enableUnofficialCheevosItem = new ToolStripMenuItem("Test Unofficial Achievements")
			{
				Checked = AllowUnofficialCheevos,
				CheckOnClick = true
			};
			enableUnofficialCheevosItem.CheckedChanged += (_, _) => ToggleUnofficialCheevos();
			raDropDownItems.Add(enableUnofficialCheevosItem);

			tss = new ToolStripSeparator();
			raDropDownItems.Add(tss);

			var viewGameInfoItem = new ToolStripMenuItem("View Game Info");
			viewGameInfoItem.Click += (_, _) =>
			{
				_gameInfoForm.OnFrameAdvance(_gameData.GameBadge, _gameData.TotalCheevoPoints(HardcoreMode),
					CurrentLboard is null ? "N/A" : $"{CurrentLboard.Description} ({CurrentLboard.Score})",
					CurrentRichPresence ?? "N/A");

				_gameInfoForm.Show();
			};
			raDropDownItems.Add(viewGameInfoItem);

			var viewCheevoListItem = new ToolStripMenuItem("View Achievement List");
			viewCheevoListItem.Click += (_, _) =>
			{
				_cheevoListForm.OnFrameAdvance(HardcoreMode, true);
				_cheevoListForm.Show();
			};
			raDropDownItems.Add(viewCheevoListItem);

#if false
			var viewLboardListItem = new ToolStripMenuItem("View Leaderboard List");
			viewLboardListItem.Click += (_, _) =>
			{
				_lboardListForm.OnFrameAdvance(true);
				_lboardListForm.Show();
			};
			raDropDownItems.Add(viewLboardListItem);
#endif
		}

		protected override void HandleHardcoreModeDisable(string reason)
		{
			_dialogParent.ModalMessageBox(
				caption: "Warning",
				icon: EMsgBoxIcon.Warning,
				text: $"{reason} Disabling hardcore mode.");
			HardcoreMode = false;
		}

		public RCheevos(
			MainForm mainForm,
			InputManager inputManager,
			ToolManager tools,
			Func<Config> getConfig,
			Action<Stream> playWavFile,
			ToolStripItemCollection raDropDownItems,
			Action shutdownRACallback)
				: base(mainForm, inputManager, tools, getConfig, raDropDownItems, shutdownRACallback)
		{
			_playWavFileCallback = playWavFile;

			_isActive = true;
			_httpThread = new(HttpRequestThreadProc) { IsBackground = true, Priority = ThreadPriority.BelowNormal, Name = "RCheevos HTTP Thread" };
			_httpThread.Start();

			_runtime = _lib.rc_runtime_alloc();
			if (_runtime == IntPtr.Zero)
			{
				throw new("rc_runtime_alloc returned NULL!");
			}

			_eventcb = EventHandlerCallback;
			_peekcb = PeekCallback;
			_validatecb = ValidateCallback;

			var config = _getConfig();
			CheevosActive = config.RACheevosActive;
			LBoardsActive = config.RALBoardsActive;
			RichPresenceActive = config.RARichPresenceActive;
			_hardcoreMode = config.RAHardcoreMode;
			EnableSoundEffects = config.RASoundEffects;
			AllowUnofficialCheevos = config.RAAllowUnofficialCheevos;

			Login();
			BuildMenu(raDropDownItems);
		}

		public override void Dispose()
		{
			_isActive = false;
			_threadThrottle.Set(); // wakeup the thread
			_httpThread.Join(); // note: the http thread handles disposing requests
			_threadThrottle.Dispose();

			_lib.rc_runtime_destroy(_runtime);
			_runtime = IntPtr.Zero;
			Stop();
			_gameInfoForm.Dispose();
			_cheevoListForm.Dispose();
#if false
			_lboardListForm.Dispose();
#endif
			_mainForm.QuicksaveLoad -= QuickLoadCallback;
		}

		public override void OnSaveState(string path)
		{
			if (!LoggedIn)
			{
				return;
			}

			OneShotActivateActiveModeCheevos();

			var size = _lib.rc_runtime_progress_size(_runtime, IntPtr.Zero);
			if (size > 0)
			{
				var buffer = new byte[(int)size];
				_lib.rc_runtime_serialize_progress_sized(buffer, (uint)buffer.Length, _runtime, IntPtr.Zero);
				using var file = File.Create(path + ".rap");
				file.Write(buffer, 0, buffer.Length);
			}
		}

		public override void OnLoadState(string path)
		{
			if (!LoggedIn)
			{
				return;
			}

			if (HardcoreMode)
			{
				HandleHardcoreModeDisable("Loading savestates is not allowed in hardcore mode.");
			}

			OneShotActivateActiveModeCheevos();

			_lib.rc_runtime_reset(_runtime);

			if (!File.Exists(path + ".rap")) return;

			using var file = File.OpenRead(path + ".rap");
			var buffer = file.ReadAllBytes();
			_lib.rc_runtime_deserialize_progress_sized(_runtime, buffer, (uint)buffer.Length, IntPtr.Zero);
		}
		
		private void QuickLoadCallback(object _, BeforeQuickLoadEventArgs e)
		{
			if (HardcoreMode)
			{
				e.Handled = _dialogParent.ModalMessageBox2(
					caption: "Warning",
					icon: EMsgBoxIcon.Warning,
					text: "Loading a quicksave is not allowed in hardcode mode. Abort loading state?");
			}
		}

		// not sure if we really need to do anything here...
		// nice way to ensure config is written back every so often (and on close)
		public override void Stop()
		{
			var config = _getConfig();
			config.RACheevosActive = CheevosActive;
			config.RALBoardsActive = LBoardsActive;
			config.RARichPresenceActive = RichPresenceActive;
			config.RAHardcoreMode = HardcoreMode;
			config.RASoundEffects = EnableSoundEffects;
			config.RAAllowUnofficialCheevos = AllowUnofficialCheevos;
		}

		private bool ValidateCallback(uint address)
			=> address < _readMap.Length && _readMap[address] != 0xFF;

		public override void Restart()
		{
			if (_firstRestart)
			{
				_firstRestart = false;
				if (HardcoreMode)
				{
					HardcoreMode = _mainForm.RebootCore(); // unset hardcore mode if we fail to reboot core somehow
					if (HardcoreMode && _mainForm.CurrentlyOpenRomArgs is not null)
					{
						// if we aren't hardcore anymore, we failed to reboot the core (and didn't call Restart probably)
						// if CurrentlyOpenRomArgs is null, then Restart won't be called (as RebootCore returns true immediately), so
						return;
					}
				}
			}

			_activeModeCheevosOnceActivated = false;

			if (!LoggedIn)
			{
				return;
			}

			// reinit the runtime
			_lib.rc_runtime_destroy(_runtime);
			_runtime = _lib.rc_runtime_alloc();
			if (_runtime == IntPtr.Zero)
			{
				throw new("rc_runtime_alloc returned NULL!");
			}

			// get console id
			_consoleId = SystemIdToConsoleId();

			// init the read map
			_readMap = [ ];

			if (Emu.HasMemoryDomains())
			{
				_memFunctions = CreateMemoryBanks(_consoleId, Domains);
				if (_memFunctions.Count > 255)
				{
					throw new InvalidOperationException("_memFunctions must have less than 256 memory banks");
				}

				// this is kind of poop, it would prevent having >2GiB total banksize
				// but no system needs that right now, the largest is just New 3DS at 256MiB
				_readMap = new byte[_memFunctions.Sum(mfun => mfun.BankSize)];

				uint addr = 0;
				for (var i = 0; i < _memFunctions.Count; i++)
				{
					_memFunctions[i].StartAddress = addr;

					var mapValue = _memFunctions[i].ReadFunc is not null ? i : 0xFF;
					for (var j = 0; j < _memFunctions[i].BankSize; j++)
					{
						_readMap[addr + j] = (byte)mapValue;
					}

					addr = checked(addr + _memFunctions[i].BankSize);
				}
			}

			// verify and init whatever is loaded
			AllGamesVerified = true;
			_gameHash = null; // will be set by first IdentifyHash

			if (_mainForm.CurrentlyOpenRomArgs is not null)
			{
				var ids = GetRAGameIds(_mainForm.CurrentlyOpenRomArgs.OpenAdvanced, _consoleId);

				AllGamesVerified = !ids.Contains(0u);

				var gameId = ids.Count > 0 ? ids[0] : 0;
				_gameData = new();

				if (gameId != 0)
				{
					_gameData = _cachedGameDatas.TryGetValue(gameId, out var cachedGameData)
						? new(cachedGameData, () => AllowUnofficialCheevos)
						: GetGameData(gameId);
				}

				// this check seems redundant, but it covers the case where GetGameData failed somehow
				if (_gameData.GameID != 0)
				{
					StartGameSession();

					_cachedGameDatas.Remove(gameId);
					_cachedGameDatas.Add(gameId, _gameData);

					InitGameData();
				}
				else
				{
					_activeModeUnlocksRequest = _inactiveModeUnlocksRequest = FailedRCheevosRequest.Singleton;
				}
			}
			else
			{
				_gameData = new();
				_activeModeUnlocksRequest = _inactiveModeUnlocksRequest = FailedRCheevosRequest.Singleton;
			}

			// validate addresses now that we have cheevos init
			_lib.rc_runtime_validate_addresses(_runtime, _eventcb, _validatecb);

			_gameInfoForm.Restart(_gameData.Title, _gameData.TotalCheevoPoints(HardcoreMode), CurrentRichPresence ?? "N/A");
			_cheevoListForm.Restart(_gameData.GameID == 0 ? Array.Empty<Cheevo>() : _gameData.CheevoEnumerable, GetCheevoProgress);
#if false
			_lboardListForm.Restart(_gameData.GameID == 0 ? Array.Empty<LBoard>() : _gameData.LBoardEnumerable);
#endif

			Update();

			// note: this can only catch quicksaves (probably only case of accidential use from hotkeys)
			_mainForm.QuicksaveLoad += QuickLoadCallback;
		}

		public override void Update()
		{
			if (!LoggedIn)
			{
				return;
			}

			if (_gameData.GameID != 0)
			{
				if (HardcoreMode)
				{
					CheckHardcoreModeConditions();
				}

				CheckPing();
			}
		}

		private unsafe void EventHandlerCallback(IntPtr runtime_event)
		{
			var evt = (LibRCheevos.rc_runtime_event_t*)runtime_event;
			switch (evt->type)
			{
				case LibRCheevos.rc_runtime_event_type_t.RC_RUNTIME_EVENT_ACHIEVEMENT_TRIGGERED:
					{
						if (!CheevosActive) return;

						var cheevo = _gameData.GetCheevoById(evt->id);
						if (cheevo.IsEnabled)
						{
							_lib.rc_runtime_deactivate_achievement(_runtime, evt->id);

							cheevo.SetUnlocked(HardcoreMode, true);
							var prefix = HardcoreMode ? "[HARDCORE] " : "";
							_dialogParent.AddOnScreenMessage($"{prefix}Achievement Unlocked!");
							_dialogParent.AddOnScreenMessage(cheevo.Description);
							PlaySound(_unlockSound);

							if (cheevo.IsOfficial)
							{
								PushRequest(new CheevoUnlockRequest(Username, ApiToken, evt->id, HardcoreMode, _gameHash));
							}
						}

						break;
					}
				case LibRCheevos.rc_runtime_event_type_t.RC_RUNTIME_EVENT_ACHIEVEMENT_PRIMED:
					{
						if (!CheevosActive) return;

						var cheevo = _gameData.GetCheevoById(evt->id);
						if (cheevo.IsEnabled)
						{
							cheevo.IsPrimed = true;
							var prefix = HardcoreMode ? "[HARDCORE] " : "";
							_dialogParent.AddOnScreenMessage($"{prefix}Achievement Primed!");
							_dialogParent.AddOnScreenMessage(cheevo.Description);
							PlaySound(_infoSound);
						}

						break;
					}
				case LibRCheevos.rc_runtime_event_type_t.RC_RUNTIME_EVENT_LBOARD_STARTED:
					{
						if (!LBoardsActive || !HardcoreMode) return;

						var lboard = _gameData.GetLboardById(evt->id);
						if (!lboard.Invalid)
						{
							lboard.SetScore(evt->value);

							if (!lboard.Hidden)
							{
								CurrentLboard = lboard;
								_dialogParent.AddOnScreenMessage("Leaderboard Attempt Started!");
								_dialogParent.AddOnScreenMessage(lboard.Description);
								PlaySound(_lboardStartSound);
							}
						}

						break;
					}
				case LibRCheevos.rc_runtime_event_type_t.RC_RUNTIME_EVENT_LBOARD_CANCELED:
					{
						if (!LBoardsActive || !HardcoreMode) return;

						var lboard = _gameData.GetLboardById(evt->id);
						if (!lboard.Invalid)
						{
							if (!lboard.Hidden)
							{
								if (lboard == CurrentLboard)
								{
									CurrentLboard = null;
								}

								_dialogParent.AddOnScreenMessage($"Leaderboard Attempt Failed! ({lboard.Score})");
								_dialogParent.AddOnScreenMessage(lboard.Description);
								PlaySound(_lboardFailedSound);
							}

							lboard.SetScore(0);
						}
						
						break;
					}
				case LibRCheevos.rc_runtime_event_type_t.RC_RUNTIME_EVENT_LBOARD_UPDATED:
					{
						if (!LBoardsActive || !HardcoreMode) return;

						var lboard = _gameData.GetLboardById(evt->id);
						if (!lboard.Invalid)
						{
							lboard.SetScore(evt->value);
						}

						break;
					}
				case LibRCheevos.rc_runtime_event_type_t.RC_RUNTIME_EVENT_LBOARD_TRIGGERED:
					{
						if (!LBoardsActive || !HardcoreMode) return;

						var lboard = _gameData.GetLboardById(evt->id);
						if (!lboard.Invalid)
						{
							PushRequest(new LboardTriggerRequest(Username, ApiToken, evt->id, evt->value, _gameHash));

							if (!lboard.Hidden)
							{
								if (lboard == CurrentLboard)
								{
									CurrentLboard = null;
								}

								_dialogParent.AddOnScreenMessage($"Leaderboard Attempt Complete! ({lboard.Score})");
								_dialogParent.AddOnScreenMessage(lboard.Description);
								PlaySound(_unlockSound);
							}
						}

						break;
					}
				case LibRCheevos.rc_runtime_event_type_t.RC_RUNTIME_EVENT_ACHIEVEMENT_DISABLED:
					{
						var cheevo = _gameData.GetCheevoById(evt->id);
						cheevo.Invalid = true;
						break;
					}
				case LibRCheevos.rc_runtime_event_type_t.RC_RUNTIME_EVENT_LBOARD_DISABLED:
					{
						var lboard = _gameData.GetLboardById(evt->id);
						lboard.Invalid = true;
						break;
					}
				case LibRCheevos.rc_runtime_event_type_t.RC_RUNTIME_EVENT_ACHIEVEMENT_UNPRIMED:
					{
						var cheevo = _gameData.GetCheevoById(evt->id);
						if (cheevo.IsEnabled)
						{
							cheevo.IsPrimed = false;
							var prefix = HardcoreMode ? "[HARDCORE] " : "";
							_dialogParent.AddOnScreenMessage($"{prefix}Achievement Unprimed!");
							_dialogParent.AddOnScreenMessage(cheevo.Description);
							PlaySound(_infoSound);
						}

						break;
					}
			}
		}

		private uint Peek(uint address)
		{
			if (address < _readMap.Length && _readMap[address] != 0xFF)
			{
				var memFuncs = _memFunctions[_readMap[address]];
				return memFuncs.ReadFunc(address - memFuncs.StartAddress);
			}

			return 0;
		}

		private uint PeekCallback(uint address, uint num_bytes, IntPtr ud) => num_bytes switch
		{
			1 => Peek(address),
			2 => Peek(address) | (Peek(address + 1) << 8),
			4 => Peek(address) | (Peek(address + 1) << 8) | (Peek(address + 2) << 16) | (Peek(address + 3) << 24),
			_ => throw new InvalidOperationException($"Requested {num_bytes} in {nameof(PeekCallback)}"),
		};

		public override void OnFrameAdvance()
		{
			if (!LoggedIn || !_activeModeUnlocksRequest.IsCompleted)
			{
				return;
			}

			OneShotActivateActiveModeCheevos();

			var input = _inputManager.ControllerOutput;
			if (input.Definition.BoolButtons.Any(b => (b.Contains("Power") || b.Contains("Reset")) && input.IsPressed(b)))
			{
				_lib.rc_runtime_reset(_runtime);
			}

			if (Emu.HasMemoryDomains())
			{
				// we want to EnterExit to prevent wbx host spam when peeks are spammed
				using (Domains.MainMemory.EnterExit())
				{
					_lib.rc_runtime_do_frame(_runtime, _eventcb, _peekcb, IntPtr.Zero, IntPtr.Zero);
				}
			}
			else
			{
				_lib.rc_runtime_do_frame(_runtime, _eventcb, _peekcb, IntPtr.Zero, IntPtr.Zero);
			}

			if (_gameInfoForm.IsShown)
			{
				_gameInfoForm.OnFrameAdvance(_gameData.GameBadge, _gameData.TotalCheevoPoints(HardcoreMode),
					CurrentLboard is null ? "N/A" : $"{CurrentLboard.Description} ({CurrentLboard.Score})",
					CurrentRichPresence ?? "N/A");
			}

			if (_cheevoListForm.IsShown)
			{
				_cheevoListForm.OnFrameAdvance(HardcoreMode);
			}

#if false
			if (_lboardListForm.IsShown)
			{
				_lboardListForm.OnFrameAdvance();
			}
#endif
		}
	}
}

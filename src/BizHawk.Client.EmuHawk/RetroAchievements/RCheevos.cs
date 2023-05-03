using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

using BizHawk.BizInvoke;
using BizHawk.Common;
using BizHawk.Common.IOExtensions;
using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class RCheevos : RetroAchievements
	{
		private static readonly LibRCheevos _lib;

		static RCheevos()
		{
			var resolver = new DynamicLibraryImportResolver(
				OSTailoredCode.IsUnixHost ? "librcheevos.so" : "librcheevos.dll", hasLimitedLifetime: false);
			_lib = BizInvoker.GetInvoker<LibRCheevos>(resolver, CallingConventionAdapters.Native);
		}

		private IntPtr _runtime;

		private readonly LibRCheevos.rc_runtime_event_handler_t _eventcb;
		private readonly LibRCheevos.rc_peek_t _peekcb;

		private readonly Dictionary<int, (ReadMemoryFunc Func, int Start)> _readMap = new();

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
			autoStartRAItem.CheckedChanged += (_, _) => _getConfig().RAAutostart ^= true;
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
			enableCheevosItem.CheckedChanged += (_, _) => CheevosActive ^= true;
			raDropDownItems.Add(enableCheevosItem);

			var enableLboardsItem = new ToolStripMenuItem("Enable Leaderboards")
			{
				Checked = LBoardsActive,
				CheckOnClick = true,
				Enabled = HardcoreMode
			};
			enableLboardsItem.CheckedChanged += (_, _) => LBoardsActive ^= true;
			raDropDownItems.Add(enableLboardsItem);

			var enableRichPresenceItem = new ToolStripMenuItem("Enable Rich Presence")
			{
				Checked = RichPresenceActive,
				CheckOnClick = true
			};
			enableRichPresenceItem.CheckedChanged += (_, _) => RichPresenceActive ^= true;
			raDropDownItems.Add(enableRichPresenceItem);

			var enableHardcoreItem = new ToolStripMenuItem("Enable Hardcore Mode")
			{
				Checked = HardcoreMode,
				CheckOnClick = true
			};
			enableHardcoreItem.CheckedChanged += (_, _) =>
			{
				_hardcoreMode ^= true;

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
			enableSoundEffectsItem.CheckedChanged += (_, _) => EnableSoundEffects ^= true;
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
			_mainForm.ShowMessageBox(null, $"{reason} Disabling hardcore mode.", "Warning", EMsgBoxIcon.Warning);
			HardcoreMode = false;
		}

		public RCheevos(IMainFormForRetroAchievements mainForm, InputManager inputManager, ToolManager tools,
			Func<Config> getConfig, ToolStripItemCollection raDropDownItems, Action shutdownRACallback)
			: base(mainForm, inputManager, tools, getConfig, raDropDownItems, shutdownRACallback)
		{
			_isActive = true;
			_httpThread = new(HttpRequestThreadProc) { IsBackground = true, Priority = ThreadPriority.BelowNormal };
			_httpThread.Start();

			_runtime = _lib.rc_runtime_alloc();
			if (_runtime == IntPtr.Zero)
			{
				throw new("rc_runtime_alloc returned NULL!");
			}
			Login();

			_eventcb = EventHandlerCallback;
			_peekcb = PeekCallback;

			var config = _getConfig();
			CheevosActive = config.RACheevosActive;
			LBoardsActive = config.RALBoardsActive;
			RichPresenceActive = config.RARichPresenceActive;
			_hardcoreMode = config.RAHardcoreMode;
			EnableSoundEffects = config.RASoundEffects;
			AllowUnofficialCheevos = config.RAAllowUnofficialCheevos;

			BuildMenu(raDropDownItems);
		}

		public override void Dispose()
		{
			while (!_inactiveHttpRequests.IsEmpty)
			{
				// wait until all pending http requests are enqueued
			}

			_isActive = false;
			_httpThread.Join();

			_lib.rc_runtime_destroy(_runtime);
			_runtime = IntPtr.Zero;
			Stop();
			_gameInfoForm.Dispose();
			_cheevoListForm.Dispose();
#if false
			_lboardListForm.Dispose();
#endif
			_mainForm.EmuClient.BeforeQuickLoad -= QuickLoadCallback;
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
				_lib.rc_runtime_serialize_progress(buffer, _runtime, IntPtr.Zero);
				using var file = File.OpenWrite(path + ".rap");
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
			_lib.rc_runtime_deserialize_progress(_runtime, buffer, IntPtr.Zero);
		}
		
		private void QuickLoadCallback(object _, BeforeQuickLoadEventArgs e)
		{
			if (HardcoreMode)
			{
				e.Handled = _mainForm.ShowMessageBox2(null, "Loading a quicksave is not allowed in hardcode mode. Abort loading state?", "Warning", EMsgBoxIcon.Warning);
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
			_readMap.Clear();

			if (Emu.HasMemoryDomains())
			{
				_memFunctions = CreateMemoryBanks(_consoleId, Domains, Emu.CanDebug() ? Emu.AsDebuggable() : null);

				var addr = 0;
				foreach (var memFunctions in _memFunctions)
				{
					if (memFunctions.ReadFunc is not null)
					{
						for (var i = 0; i < memFunctions.BankSize; i++)
						{
							_readMap.Add(addr + i, (memFunctions.ReadFunc, addr));
						}
					}

					addr += memFunctions.BankSize;
				}
			}

			// verify and init whatever is loaded
			AllGamesVerified = true;
			_gameHash = null; // will be set by first IdentifyHash

			if (_mainForm.CurrentlyOpenRomArgs is not null)
			{
				var ids = GetRAGameIds(_mainForm.CurrentlyOpenRomArgs.OpenAdvanced, _consoleId);

				AllGamesVerified = !ids.Contains(0);

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
			// ReSharper disable once ConvertToLocalFunction
			LibRCheevos.rc_runtime_validate_address_t peekcb = address => _readMap.ContainsKey(address);
			_lib.rc_runtime_validate_addresses(_runtime, _eventcb, peekcb);

			_gameInfoForm.Restart(_gameData.Title, _gameData.TotalCheevoPoints(HardcoreMode), CurrentRichPresence ?? "N/A");
			_cheevoListForm.Restart(_gameData.GameID == 0 ? Array.Empty<Cheevo>() : _gameData.CheevoEnumerable, GetCheevoProgress);
#if false
			_lboardListForm.Restart(_gameData.GameID == 0 ? Array.Empty<LBoard>() : _gameData.LBoardEnumerable);
#endif

			Update();

			// note: this can only catch quicksaves (probably only case of accidential use from hotkeys)
			_mainForm.EmuClient.BeforeQuickLoad += QuickLoadCallback;
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
							_mainForm.AddOnScreenMessage($"{prefix}Achievement Unlocked!");
							_mainForm.AddOnScreenMessage(cheevo.Description);
							if (EnableSoundEffects) _unlockSound.PlayNoExceptions();

							if (cheevo.IsOfficial)
							{
								_inactiveHttpRequests.Push(new CheevoUnlockRequest(Username, ApiToken, evt->id, HardcoreMode, _gameHash));
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
							_mainForm.AddOnScreenMessage($"{prefix}Achievement Primed!");
							_mainForm.AddOnScreenMessage(cheevo.Description);
							if (EnableSoundEffects) _infoSound.PlayNoExceptions();
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
								_mainForm.AddOnScreenMessage($"Leaderboard Attempt Started!");
								_mainForm.AddOnScreenMessage(lboard.Description);
								if (EnableSoundEffects) _lboardStartSound.PlayNoExceptions();
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

								_mainForm.AddOnScreenMessage($"Leaderboard Attempt Failed! ({lboard.Score})");
								_mainForm.AddOnScreenMessage(lboard.Description);
								if (EnableSoundEffects) _lboardFailedSound.PlayNoExceptions();
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
							_inactiveHttpRequests.Push(new LboardTriggerRequest(Username, ApiToken, evt->id, evt->value, _gameHash));

							if (!lboard.Hidden)
							{
								if (lboard == CurrentLboard)
								{
									CurrentLboard = null;
								}

								_mainForm.AddOnScreenMessage($"Leaderboard Attempt Complete! ({lboard.Score})");
								_mainForm.AddOnScreenMessage(lboard.Description);
								if (EnableSoundEffects) _unlockSound.PlayNoExceptions();
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
							_mainForm.AddOnScreenMessage($"{prefix}Achievement Unprimed!");
							_mainForm.AddOnScreenMessage(cheevo.Description);
							if (EnableSoundEffects) _infoSound.PlayNoExceptions();
						}

						break;
					}
			}
		}

		private int PeekCallback(int address, int num_bytes, IntPtr ud)
		{
			byte Peek(int addr)
				=> _readMap.TryGetValue(addr, out var reader) ? reader.Func(addr - reader.Start) : (byte)0;

			return num_bytes switch
			{
				1 => Peek(address),
				2 => Peek(address) | (Peek(address + 1) << 8),
				4 => Peek(address) | (Peek(address + 1) << 8) | (Peek(address + 2) << 16) | (Peek(address + 3) << 24),
				_ => throw new InvalidOperationException($"Requested {num_bytes} in {nameof(PeekCallback)}"),
			};
		}

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
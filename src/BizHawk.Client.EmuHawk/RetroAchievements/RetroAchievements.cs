using System;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Common;
using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class RetroAchievements
	{
		private static RAInterface RA;
		public static bool IsAvailable => RA != null;

		static RetroAchievements()
		{
			try
			{
				if (OSTailoredCode.IsUnixHost)
				{
					throw new NotSupportedException("RetroAchievements is Windows only!");
				}

				AttachDll();
			}
			catch
			{
				DetachDll();
			}
		}

		private readonly MainForm _mainForm; // todo: encapsulate MainForm in an interface
		private readonly InputManager _inputManager;

		private IEmulator Emu => _mainForm.Emulator;
		private IMemoryDomains Domains => Emu.AsMemoryDomains();
		private IGameInfo Game => _mainForm.Game;
		private IMovieSession MovieSession => _mainForm.MovieSession;
		private Config Config => _mainForm.Config;
		private ToolManager Tools => _mainForm.Tools;

		public RetroAchievements(MainForm mainForm, InputManager inputManager, Func<ToolStripItemCollection> getRADropDownItems, Action shutdownRACallback)
		{
			// hack around winforms message pumping screwing over RA's forms
			_dialogThreadActive = true;
			_dialogThread = new(DialogThreadProc) { IsBackground = true };
			_dialogThread.Start();

			_mainForm = mainForm;
			_inputManager = inputManager;
			_getRADropDownItems = getRADropDownItems;
			_shutdownRACallback = shutdownRACallback;

			RA.InitClient(_mainForm.Handle, "BizHawk", VersionInfo.GetEmuVersion());

			_isActive = IsActiveCallback;
			_unpause = UnpauseCallback;
			_pause = PauseCallback;
			_rebuildMenu = RebuildMenuCallback;
			_estimateTitle = EstimateTitleCallback;
			_resetEmulator = ResetEmulatorCallback;
			_loadROM = LoadROMCallback;

			RA.InstallSharedFunctionsExt(_isActive, _unpause, _pause, _rebuildMenu, _estimateTitle, _resetEmulator, _loadROM);

			RA.AttemptLogin(true);
		}

		public void OnSaveState(string path)
			=> RA.OnSaveState(path);

		public void OnLoadState(string path)
		{
			if (RA.HardcoreModeIsActive())
			{
				HandleHardcoreModeDisable("Loading savestates is not allowed in hardcore mode.");
			}

			RA.OnLoadState(path);
		}

		// call this before closing the emulator
		public void Stop()
		{
			RA.ClearMemoryBanks();
			RA.ActivateGame(0);
		}

		public void Restart()
		{
			var consoleId = SystemIdToConsoleId();
			RA.SetConsoleID(consoleId);

			RA.ClearMemoryBanks();

			if (Emu.HasMemoryDomains())
			{
				_memFunctions = CreateMemoryBanks(consoleId, Domains, Emu.CanDebug() ? Emu.AsDebuggable() : null);

				for (int i = 0; i < _memFunctions.Count; i++)
				{
					RA.InstallMemoryBank(i, _memFunctions[i].ReadFunc, _memFunctions[i].WriteFunc, _memFunctions[i].BankSize);
					RA.InstallMemoryBankBlockReader(i, _memFunctions[i].ReadBlockFunc);
				}
			}

			AllGamesVerified = true;

			if (_mainForm.CurrentlyOpenRomArgs is not null)
			{
				var ids = GetRAGameIds(_mainForm.CurrentlyOpenRomArgs.OpenAdvanced, consoleId);

				AllGamesVerified = !ids.Contains(0);

				RA.ActivateGame(ids.Count > 0 ? ids[0] : 0);
			}
			else
			{
				RA.ActivateGame(0);
			}

			Update();
			RebuildMenu();

			// workaround a bug in RA which will cause the window title to be changed despite us not calling UpdateAppTitle
			_mainForm.UpdateWindowTitle();

			// note: this can only catch quicksaves (probably only case of accidential use from hotkeys)
			_mainForm.EmuClient.BeforeQuickLoad += (_, e) =>
			{
				if (RA.HardcoreModeIsActive())
				{
					e.Handled = !RA.WarnDisableHardcore("load a quicksave");
				}
			};
		}

		public void Update()
		{
			if (RA.HardcoreModeIsActive())
			{
				CheckHardcoreModeConditions();
			}

			if (_inputManager.ClientControls["Open RA Overlay"])
			{
				RA.SetPaused(true);
			}

			if (RA.IsOverlayFullyVisible())
			{
				var ci = new RAInterface.ControllerInput
				{
					UpPressed = _inputManager.ClientControls["RA Up"],
					DownPressed = _inputManager.ClientControls["RA Down"],
					LeftPressed = _inputManager.ClientControls["RA Left"],
					RightPressed = _inputManager.ClientControls["RA Right"],
					ConfirmPressed = _inputManager.ClientControls["RA Confirm"],
					CancelPressed = _inputManager.ClientControls["RA Cancel"],
					QuitPressed = _inputManager.ClientControls["RA Quit"],
				};

				RA.NavigateOverlay(ref ci);

				// todo: suppress user inputs with overlay active?
			}

			HandleNextDelegate();
		}

		public void OnFrameAdvance()
		{
			var input = _inputManager.ControllerOutput;
			foreach (var resetButton in input.Definition.BoolButtons.Where(b => b.Contains("Power") || b.Contains("Reset")))
			{
				if (input.IsPressed(resetButton))
				{
					RA.OnReset();
					break;
				}
			}

			if (Emu.HasMemoryDomains())
			{
				// do this to prevent wbx host spam when peeks are spammed
				using (Domains.MainMemory.EnterExit())
				{
					RA.DoAchievementsFrame();
				}
			}
			else
			{
				RA.DoAchievementsFrame();
			}
		}
	}
}

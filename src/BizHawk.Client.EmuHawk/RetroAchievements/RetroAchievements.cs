using System;
using System.Collections.Generic;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public abstract partial class RetroAchievements : IRetroAchievements
	{
		protected readonly MainForm _mainForm; // todo: encapsulate MainForm in an interface
		protected readonly InputManager _inputManager;
		protected readonly ToolStripItemCollection _raDropDownItems;
		protected readonly Action _shutdownRACallback;

		protected IEmulator Emu => _mainForm.Emulator;
		protected IMemoryDomains Domains => Emu.AsMemoryDomains();
		protected IGameInfo Game => _mainForm.Game;
		protected IMovieSession MovieSession => _mainForm.MovieSession;
		protected Config Config => _mainForm.Config;
		protected ToolManager Tools => _mainForm.Tools;


		protected IReadOnlyList<MemFunctions> _memFunctions;

		protected RetroAchievements(MainForm mainForm, InputManager inputManager, ToolStripItemCollection raDropDownItems, Action shutdownRACallback)
		{
			_mainForm = mainForm;
			_inputManager = inputManager;
			_raDropDownItems = raDropDownItems;
			_shutdownRACallback = shutdownRACallback;
		}

		public static IRetroAchievements CreateImpl(Config config, MainForm mainForm, InputManager inputManager, ToolStripItemCollection raDropDownItems, Action shutdownRACallback)
		{
			if (RAIntegration.IsAvailable)
			{
				if (config.SkipRATelemetryWarning || mainForm.ShowMessageBox2(
					owner: null,
					text: "In order to use RetroAchievements, some information needs to be sent to retroachievements.org:\n" +
					"\n\u2022 Your RetroAchievements username and password (first login) or token (subsequent logins)." +
					"\n\u2022 The hash of the game(s) you have loaded into BizHawk. (for game identification + achievement unlock + leaderboard submission)" +
					"\n\u2022 The RetroAchievements game ID(s) of the game(s) you have loaded into BizHawk. (for game information + achievement definitions + leaderboard definitions + rich presence definitions + code notes + achievement badges + user unlocks + leaderboard submission + ticket submission)" +
					"\n\u2022 Rich presence data (periodically sent, derived from emulated game memory)." +
					"\n\u2022 Whether or not you are currently in \"Hardcore Mode\" (for achievement unlock)." +
					"\n\u2022 Ticket submission type and message (when submitting tickets)." +
					"\n\nDo you agree to send this information to retroachievements.org?",
					caption: "Notice",
					icon: EMsgBoxIcon.Question,
					useOKCancel: false))
				{
					if (RAIntegration.CheckUpdateRA(mainForm))
					{
						var ret = new RAIntegration(mainForm, inputManager, raDropDownItems, shutdownRACallback);

						// note: this can't occur in the ctor, as this may reboot the core, and RA is null during the ctor
						ret.Restart();

						config.SkipRATelemetryWarning = true;

						return ret;
					}
				}
			}

			return null;
		}

		public abstract void Update();

		public abstract void OnFrameAdvance();

		public abstract void Restart();

		public abstract void Stop();

		public abstract void OnSaveState(string path);

		public abstract void OnLoadState(string path);

		public abstract void Dispose();
	}
}

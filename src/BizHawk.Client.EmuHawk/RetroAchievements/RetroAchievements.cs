using System;
using System.Collections.Generic;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public abstract partial class RetroAchievements : IRetroAchievements
	{
		protected readonly IMainFormForRetroAchievements _mainForm;
		protected readonly InputManager _inputManager;
		protected readonly ToolManager _tools;
		protected readonly Func<Config> _getConfig;
		protected readonly ToolStripItemCollection _raDropDownItems;
		protected readonly Action _shutdownRACallback;

		protected IEmulator Emu => _mainForm.Emulator;
		protected IMemoryDomains Domains => Emu.AsMemoryDomains();
		protected IGameInfo Game => _mainForm.Game;
		protected IMovieSession MovieSession => _mainForm.MovieSession;

		protected IReadOnlyList<MemFunctions> _memFunctions;

		protected RetroAchievements(IMainFormForRetroAchievements mainForm, InputManager inputManager, ToolManager tools, 
			Func<Config> getConfig, ToolStripItemCollection raDropDownItems, Action shutdownRACallback)
		{
			_mainForm = mainForm;
			_inputManager = inputManager;
			_tools = tools;
			_getConfig = getConfig;
			_raDropDownItems = raDropDownItems;
			_shutdownRACallback = shutdownRACallback;
		}

		public static IRetroAchievements CreateImpl(IMainFormForRetroAchievements mainForm, InputManager inputManager, ToolManager tools,
			Func<Config> getConfig, ToolStripItemCollection raDropDownItems, Action shutdownRACallback)
		{
			if (getConfig().SkipRATelemetryWarning || mainForm.ShowMessageBox2(
				owner: null,
				text: "In order to use RetroAchievements, some information needs to be sent to retroachievements.org:\n" +
				"\n\u2022 Your RetroAchievements username and password (first login) or token (subsequent logins)." +
				"\n\u2022 The hash of the game(s) you have loaded into BizHawk. (for game identification + achievement unlock + leaderboard submission)" +
				"\n\u2022 The RetroAchievements game ID(s) of the game(s) you have loaded into BizHawk. (for game information + achievement definitions + leaderboard definitions + rich presence definitions + code notes + achievement badges + user unlocks + leaderboard submission + ticket submission)" +
				"\n\u2022 Rich presence data (periodically sent, derived from emulated game memory)." +
				"\n\u2022 Whether or not you are currently in \"Hardcore Mode\" (for achievement unlock)." +
				"\n\u2022 Ticket submission type and message (when submitting tickets with RAIntegration)." + // todo: add this to our impl? doesn't seem to be supported in rcheevos...
				"\n\nDo you agree to send this information to retroachievements.org?",
				caption: "Notice",
				icon: EMsgBoxIcon.Question,
				useOKCancel: false))
			{
				getConfig().SkipRATelemetryWarning = true;

				if (RAIntegration.IsAvailable && RAIntegration.CheckUpdateRA(mainForm) && false)
				{
					return new RAIntegration(mainForm, inputManager, tools, getConfig, raDropDownItems, shutdownRACallback);
				}
				else
				{
					return new RCheevos(mainForm, inputManager, tools, getConfig, raDropDownItems, shutdownRACallback);
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

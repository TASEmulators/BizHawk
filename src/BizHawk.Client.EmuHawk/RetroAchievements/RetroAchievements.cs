using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public abstract partial class RetroAchievements : IRetroAchievements
	{
		protected readonly IDialogParent _dialogParent;

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
		protected CheatCollection CheatList => _mainForm.CheatList;

		protected IReadOnlyList<MemFunctions> _memFunctions;

		protected RetroAchievements(
			MainForm mainForm,
			InputManager inputManager,
			ToolManager tools, 
			Func<Config> getConfig,
			ToolStripItemCollection raDropDownItems,
			Action shutdownRACallback)
		{
			_dialogParent = mainForm;
			_mainForm = mainForm;
			_inputManager = inputManager;
			_tools = tools;
			_getConfig = getConfig;
			_raDropDownItems = raDropDownItems;
			_shutdownRACallback = shutdownRACallback;

			_getCiaNormalKeyFunc = GetCiaNormalKeyFunc;
			_getNcchNormalKeysFunc = GetNcchNormalKeysFunc;
			RCheevos._lib.rc_hash_init_3ds_get_cia_normal_key_func(_getCiaNormalKeyFunc);
			RCheevos._lib.rc_hash_init_3ds_get_ncch_normal_keys_func(_getNcchNormalKeysFunc);
		}

		public static IRetroAchievements CreateImpl(
			MainForm mainForm,
			InputManager inputManager,
			ToolManager tools,
			Func<Config> getConfig,
			Action<Stream> playWavFile,
			ToolStripItemCollection raDropDownItems,
			Action shutdownRACallback)
		{
			var config = getConfig();
			if (config.SkipRATelemetryWarning || mainForm.ModalMessageBox2(
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
				config.SkipRATelemetryWarning = true;

				if (RAIntegration.IsAvailable && RAIntegration.CheckUpdateRA(mainForm))
				{
					return new RAIntegration(mainForm, inputManager, tools, getConfig, raDropDownItems, shutdownRACallback);
				}
				else
				{
					return new RCheevos(mainForm, inputManager, tools, getConfig, playWavFile, raDropDownItems, shutdownRACallback);
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

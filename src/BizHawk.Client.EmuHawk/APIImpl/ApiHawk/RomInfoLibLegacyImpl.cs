#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

using BizHawk.API.ApiHawk;
using BizHawk.API.Base;
using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk.APIImpl.ApiHawk
{
	internal sealed class RomInfoLibLegacyImpl : LibBase<GlobalsAccessAPIEnvironment>, IRomInfoLib, IGameInfo
	{
		public LoadedRomInfo? LoadedRom { get; private set; }

		public RomInfoLibLegacyImpl(out Action<GlobalsAccessAPIEnvironment> updateEnv) : base(out updateEnv) {}

		[LegacyApiHawk]
		public string GetBoardType() => LoadedRom?.MapperName ?? string.Empty;

		[LegacyApiHawk]
		public Dictionary<string, string> GetOptions() => LoadedRom?.Options as Dictionary<string, string> ?? new Dictionary<string, string>();

		[LegacyApiHawk]
		public string GetRomHash() => LoadedRom?.Hash ?? string.Empty;

		[LegacyApiHawk]
		public string GetRomName() => LoadedRom?.Name ?? string.Empty;

		[LegacyApiHawk]
		public string? GetStatus() => (LoadedRom?.Status)?.ToString();

		[LegacyApiHawk]
		public bool InDatabase() => LoadedRom?.IsInDatabase == true;

		[LegacyApiHawk]
		public bool IsStatusBad() => (LoadedRom?.Status)?.IsBad() != false;

		protected override void PostEnvUpdate()
		{
			if (Env.GlobalGame == null)
			{
				LoadedRom = null;
				return;
			}
			LoadedRom = new LoadedRomInfo(
				hash: Env.GlobalGame.Hash,
				isInDatabase: !Env.GlobalGame.NotInDatabase,
				mapperName: Env.BoardInfo?.BoardName,
				name: Env.GlobalGame.Name,
				options: Env.GlobalGame.GetOptionsDict().ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
				status: Env.GlobalGame.Status
			);
		}
	}
}

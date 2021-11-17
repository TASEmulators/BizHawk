using System;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.GBA
{
	public partial class MGBALink : ISettable<MGBALink.MGBALinkSettings, MGBALink.MGBALinkSyncSettings>
	{
		private readonly MGBALinkSettings _settings;
		private readonly MGBALinkSyncSettings _syncSettings;

		public class MGBALinkSettings
		{
			public MGBAHawk.Settings[] _linkedSettings;

			public MGBALinkSettings Clone()
			{
				return new MGBALinkSettings(_linkedSettings[0].Clone(), _linkedSettings[1].Clone(), _linkedSettings[2].Clone(), _linkedSettings[3].Clone());
			}

			public MGBALinkSettings()
			{
				_linkedSettings = new MGBAHawk.Settings[MAX_PLAYERS] { new(), new(), new(), new() };
			}

			public MGBALinkSettings(MGBAHawk.Settings one, MGBAHawk.Settings two, MGBAHawk.Settings three, MGBAHawk.Settings four)
			{
				_linkedSettings = new MGBAHawk.Settings[MAX_PLAYERS] { one, two, three, four };
			}
		}

		public class MGBALinkSyncSettings
		{
			public MGBAHawk.SyncSettings[] _linkedSyncSettings;

			public MGBALinkSyncSettings Clone()
			{
				return new MGBALinkSyncSettings(_linkedSyncSettings[P1].Clone(), _linkedSyncSettings[P2].Clone(), _linkedSyncSettings[P3].Clone(), _linkedSyncSettings[P4].Clone());
			}

			public MGBALinkSyncSettings()
			{
				_linkedSyncSettings = new MGBAHawk.SyncSettings[MAX_PLAYERS] { new(), new(), new(), new() };
			}

			public MGBALinkSyncSettings(MGBAHawk.SyncSettings one, MGBAHawk.SyncSettings two, MGBAHawk.SyncSettings three, MGBAHawk.SyncSettings four)
			{
				_linkedSyncSettings = new MGBAHawk.SyncSettings[MAX_PLAYERS] { one, two, three, four };
			}
		}

		public MGBALinkSettings GetSettings()
		{
			return _settings.Clone();
		}

		public MGBALinkSyncSettings GetSyncSettings()
		{
			return _syncSettings.Clone();
		}

		public PutSettingsDirtyBits PutSettings(MGBALinkSettings o)
		{
			var ret = PutSettingsDirtyBits.None;
			for (int i = 0; i < _numCores; i++)
			{
				ret |= _linkedCores[i].PutSettings(o._linkedSettings[i]);
			}
			return ret;
		}

		public PutSettingsDirtyBits PutSyncSettings(MGBALinkSyncSettings o)
		{
			var ret = PutSettingsDirtyBits.None;
			for (int i = 0; i < _numCores; i++)
			{
				ret |= _linkedCores[i].PutSyncSettings(o._linkedSyncSettings[i]);
			}
			return ret;
		}
	}
}
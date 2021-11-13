using System;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.GBA
{
	public partial class MGBALink : ISettable<MGBALink.MGBALinkSettings, MGBALink.MGBALinkSyncSettings>
	{
		public class MGBALinkSettings
		{
			public MGBAHawk.Settings[] _linkedSettings;

			public MGBALinkSettings Clone()
			{
				return new MGBALinkSettings(_linkedSettings[0].Clone(), _linkedSettings[1].Clone(), _linkedSettings[2].Clone(), _linkedSettings[3].Clone());
			}

			public MGBALinkSettings()
			{
				_linkedSettings = new MGBAHawk.Settings[4] { new(), new(), new(), new() };
			}

			public MGBALinkSettings(MGBAHawk.Settings one, MGBAHawk.Settings two, MGBAHawk.Settings three, MGBAHawk.Settings four)
			{
				_linkedSettings = new MGBAHawk.Settings[4] { one, two, three, four };
			}
		}

		public class MGBALinkSyncSettings
		{
			public MGBAHawk.SyncSettings[] _linkedSyncSettings;

			public MGBALinkSyncSettings Clone()
			{
				return new MGBALinkSyncSettings(_linkedSyncSettings[0].Clone(), _linkedSyncSettings[1].Clone(), _linkedSyncSettings[2].Clone(), _linkedSyncSettings[3].Clone());
			}

			public MGBALinkSyncSettings()
			{
				_linkedSyncSettings = new MGBAHawk.SyncSettings[4] { new(), new(), new(), new() };
			}

			public MGBALinkSyncSettings(MGBAHawk.SyncSettings one, MGBAHawk.SyncSettings two, MGBAHawk.SyncSettings three, MGBAHawk.SyncSettings four)
			{
				_linkedSyncSettings = new MGBAHawk.SyncSettings[4] { one, two, three, four };
			}
		}

		public MGBALinkSettings GetSettings()
		{
			return _numCores switch
			{
				2 => new MGBALinkSettings(_linkedCores[0].GetSettings(), _linkedCores[1].GetSettings(), null, null),
				3 => new MGBALinkSettings(_linkedCores[0].GetSettings(), _linkedCores[1].GetSettings(), _linkedCores[2].GetSettings(), null),
				4 => new MGBALinkSettings(_linkedCores[0].GetSettings(), _linkedCores[1].GetSettings(), _linkedCores[2].GetSettings(), _linkedCores[3].GetSettings()),
				_ => throw new Exception()
			};
		}

		public MGBALinkSyncSettings GetSyncSettings()
		{
			return _numCores switch
			{
				2 => new MGBALinkSyncSettings(_linkedCores[0].GetSyncSettings(), _linkedCores[1].GetSyncSettings(), null, null),
				3 => new MGBALinkSyncSettings(_linkedCores[0].GetSyncSettings(), _linkedCores[1].GetSyncSettings(), _linkedCores[2].GetSyncSettings(), null),
				4 => new MGBALinkSyncSettings(_linkedCores[0].GetSyncSettings(), _linkedCores[1].GetSyncSettings(), _linkedCores[2].GetSyncSettings(), _linkedCores[3].GetSyncSettings()),
				_ => throw new Exception()
			};
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
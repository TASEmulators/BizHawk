using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.Gameboy
{
	public partial class GambatteLink : ISettable<GambatteLink.GambatteLinkSettings, GambatteLink.GambatteLinkSyncSettings>
	{
		private GambatteLinkSettings _settings;
		private GambatteLinkSyncSettings _syncSettings;

		public GambatteLinkSettings GetSettings()
		{
			return _settings.Clone();
		}

		public GambatteLinkSyncSettings GetSyncSettings()
		{
			return _syncSettings.Clone();
		}

		public PutSettingsDirtyBits PutSettings(GambatteLinkSettings o)
		{
			var ret = PutSettingsDirtyBits.None;
			for (int i = 0; i < _numCores; i++)
			{
				ret |= _linkedCores[i].PutSettings(o._linkedSettings[i]);
			}
			_settings = o;
			// prevent garbage output in case one side is just muted
			Array.Clear(SampleBuffer, 0, SampleBuffer.Length);
			return ret;
		}

		public PutSettingsDirtyBits PutSyncSettings(GambatteLinkSyncSettings o)
		{
			var ret = PutSettingsDirtyBits.None;
			for (int i = 0; i < _numCores; i++)
			{
				ret |= _linkedCores[i].PutSyncSettings(o._linkedSyncSettings[i]);
			}
			_syncSettings = o;
			return ret;
		}

		public class GambatteLinkSettings
		{
			public Gameboy.GambatteSettings[] _linkedSettings;

			public GambatteLinkSettings()
			{
				_linkedSettings = new Gameboy.GambatteSettings[MAX_PLAYERS] { new(), new(), new(), new() };
			}

			public GambatteLinkSettings(Gameboy.GambatteSettings one, Gameboy.GambatteSettings two, Gameboy.GambatteSettings three, Gameboy.GambatteSettings four)
			{
				_linkedSettings = new Gameboy.GambatteSettings[MAX_PLAYERS] { one, two, three, four };
			}

			public GambatteLinkSettings Clone()
			{
				return new GambatteLinkSettings(_linkedSettings[P1].Clone(), _linkedSettings[P2].Clone(), _linkedSettings[P3].Clone(), _linkedSettings[P4].Clone());
			}
		}

		public class GambatteLinkSyncSettings
		{
			public Gameboy.GambatteSyncSettings[] _linkedSyncSettings;

			public GambatteLinkSyncSettings()
			{
				_linkedSyncSettings = new Gameboy.GambatteSyncSettings[MAX_PLAYERS] { new(), new(), new(), new() };
			}

			public GambatteLinkSyncSettings(Gameboy.GambatteSyncSettings one, Gameboy.GambatteSyncSettings two, Gameboy.GambatteSyncSettings three, Gameboy.GambatteSyncSettings four)
			{
				_linkedSyncSettings = new Gameboy.GambatteSyncSettings[MAX_PLAYERS] { one, two, three, four };
			}

			public GambatteLinkSyncSettings Clone()
			{
				return new GambatteLinkSyncSettings(_linkedSyncSettings[P1].Clone(), _linkedSyncSettings[P2].Clone(), _linkedSyncSettings[P3].Clone(), _linkedSyncSettings[P4].Clone());
			}
		}
	}
}

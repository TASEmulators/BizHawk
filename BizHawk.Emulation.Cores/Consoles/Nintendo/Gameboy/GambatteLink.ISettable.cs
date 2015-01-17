using System;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.Gameboy
{
	public partial class GambatteLink : ISettable<GambatteLink.GambatteLinkSettings, GambatteLink.GambatteLinkSyncSettings>
	{
		public GambatteLinkSettings GetSettings()
		{
			return new GambatteLinkSettings
			(
				L.GetSettings(),
				R.GetSettings()
			);
		}
		public GambatteLinkSyncSettings GetSyncSettings()
		{
			return new GambatteLinkSyncSettings
			(
				L.GetSyncSettings(),
				R.GetSyncSettings()
			);
		}

		public bool PutSettings(GambatteLinkSettings o)
		{
			return L.PutSettings(o.L) || R.PutSettings(o.R);
		}

		public bool PutSyncSettings(GambatteLinkSyncSettings o)
		{
			return L.PutSyncSettings(o.L) || R.PutSyncSettings(o.R);
		}

		public class GambatteLinkSettings
		{
			public Gameboy.GambatteSettings L;
			public Gameboy.GambatteSettings R;

			public GambatteLinkSettings()
			{
				L = new Gameboy.GambatteSettings();
				R = new Gameboy.GambatteSettings();
			}

			public GambatteLinkSettings(Gameboy.GambatteSettings L, Gameboy.GambatteSettings R)
			{
				this.L = L;
				this.R = R;
			}

			public GambatteLinkSettings Clone()
			{
				return new GambatteLinkSettings(L.Clone(), R.Clone());
			}
		}

		public class GambatteLinkSyncSettings
		{
			public Gameboy.GambatteSyncSettings L;
			public Gameboy.GambatteSyncSettings R;

			public GambatteLinkSyncSettings()
			{
				L = new Gameboy.GambatteSyncSettings();
				R = new Gameboy.GambatteSyncSettings();
			}

			public GambatteLinkSyncSettings(Gameboy.GambatteSyncSettings L, Gameboy.GambatteSyncSettings R)
			{
				this.L = L;
				this.R = R;
			}

			public GambatteLinkSyncSettings Clone()
			{
				return new GambatteLinkSyncSettings(L.Clone(), R.Clone());
			}
		}
	}
}

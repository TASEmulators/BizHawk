using System;
using System.ComponentModel;

using Newtonsoft.Json;

using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Nintendo.GBHawk;

namespace BizHawk.Emulation.Cores.Nintendo.GBHawkLink
{
	public partial class GBHawkLink : IEmulator, IStatable, ISettable<GBHawkLink.GBLinkSettings, GBHawkLink.GBLinkSyncSettings>
	{
		public GBLinkSettings GetSettings()
		{
			return new GBLinkSettings
			(
				L.GetSettings(),
				R.GetSettings()
			);
		}

		public GBLinkSyncSettings GetSyncSettings()
		{
			return new GBLinkSyncSettings
			(
				L.GetSyncSettings(),
				R.GetSyncSettings()
			);
		}

		public bool PutSettings(GBLinkSettings o)
		{
			return L.PutSettings(o.L) || R.PutSettings(o.R);
		}

		public bool PutSyncSettings(GBLinkSyncSettings o)
		{
			return L.PutSyncSettings(o.L) || R.PutSyncSettings(o.R);
		}

		private GBLinkSettings _settings = new GBLinkSettings();
		public GBLinkSyncSettings _syncSettings = new GBLinkSyncSettings();

		public class GBLinkSettings
		{
			public GBHawk.GBHawk.GBSettings L;
			public GBHawk.GBHawk.GBSettings R;

			public GBLinkSettings()
			{
				L = new GBHawk.GBHawk.GBSettings();
				R = new GBHawk.GBHawk.GBSettings();
			}

			public GBLinkSettings(GBHawk.GBHawk.GBSettings L, GBHawk.GBHawk.GBSettings R)
			{
				this.L = L;
				this.R = R;
			}

			public GBLinkSettings Clone()
			{
				return new GBLinkSettings(L.Clone(), R.Clone());
			}
		}

		public class GBLinkSyncSettings
		{
			public GBHawk.GBHawk.GBSyncSettings L;
			public GBHawk.GBHawk.GBSyncSettings R;

			public GBLinkSyncSettings()
			{
				L = new GBHawk.GBHawk.GBSyncSettings();
				R = new GBHawk.GBHawk.GBSyncSettings();
			}

			public GBLinkSyncSettings(GBHawk.GBHawk.GBSyncSettings L, GBHawk.GBHawk.GBSyncSettings R)
			{
				this.L = L;
				this.R = R;
			}

			public GBLinkSyncSettings Clone()
			{
				return new GBLinkSyncSettings(L.Clone(), R.Clone());
			}
		}
	}
}

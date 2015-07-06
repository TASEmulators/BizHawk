using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BizHawk.Emulation.Common;
using System.ComponentModel;

namespace BizHawk.Emulation.Cores.Computers.AppleII
{
	partial class AppleII : ISettable<AppleII.Settings, object>
	{
		private Settings _settings;

		public class Settings
		{
			[DefaultValue(false)]
			[Description("Choose a monochrome monitor.")]
			public bool Monochrome { get; set; }

			public Settings Clone()
			{
				return (Settings)MemberwiseClone();
			}
		}

		public AppleII.Settings GetSettings()
		{
			return _settings.Clone();
		}

		public object GetSyncSettings()
		{
			return null;
		}

		public bool PutSettings(AppleII.Settings o)
		{
			_settings = o;
			_machine.Video.IsMonochrome = _settings.Monochrome;

			setCallbacks();

			return false;
		}

		public bool PutSyncSettings(object o)
		{
			return false;
		}
	}
}

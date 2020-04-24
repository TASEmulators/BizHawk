using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.N64
{
	public partial class N64 : ISettable<N64Settings, N64SyncSettings>
	{
		public N64Settings GetSettings()
		{
			return _settings.Clone();
		}

		public N64SyncSettings GetSyncSettings()
		{
			return _syncSettings.Clone();
		}

		public PutSettingsDirtyBits PutSettings(N64Settings o)
		{
			_settings = o;
			return PutSettingsDirtyBits.RebootCore;
		}

		public PutSettingsDirtyBits PutSyncSettings(N64SyncSettings o)
		{
			_syncSettings = o;
			SetControllerButtons();
			return PutSettingsDirtyBits.RebootCore;
		}

		private void SetControllerButtons()
		{
			ControllerDefinition.BoolButtons.Clear();
			ControllerDefinition.AxisControls.Clear();

			ControllerDefinition.BoolButtons.AddRange(new[]
			{
				"Reset",
				"Power"
			});

			for (int i = 0; i < 4; i++)
			{
				if (_syncSettings.Controllers[i].IsConnected)
				{
					ControllerDefinition.BoolButtons.AddRange(new[]
					{
						"P" + (i + 1) + " A Up",
						"P" + (i + 1) + " A Down",
						"P" + (i + 1) + " A Left",
						"P" + (i + 1) + " A Right",
						"P" + (i + 1) + " DPad U",
						"P" + (i + 1) + " DPad D",
						"P" + (i + 1) + " DPad L",
						"P" + (i + 1) + " DPad R",
						"P" + (i + 1) + " Start",
						"P" + (i + 1) + " Z",
						"P" + (i + 1) + " B",
						"P" + (i + 1) + " A",
						"P" + (i + 1) + " C Up",
						"P" + (i + 1) + " C Down",
						"P" + (i + 1) + " C Right",
						"P" + (i + 1) + " C Left",
						"P" + (i + 1) + " L",
						"P" + (i + 1) + " R", 
					});

					ControllerDefinition.AxisControls.AddRange(new[]
					{
						"P" + (i + 1) + " X Axis",
						"P" + (i + 1) + " Y Axis",
					});
				}
			}
		}
	}
}

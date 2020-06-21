using BizHawk.Emulation.Common;

using static BizHawk.Emulation.Common.ControllerDefinition;

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
			static void AddN64StandardController(ControllerDefinition def, int player)
			{
				def.BoolButtons.AddRange(new[] { $"P{player} A Up", $"P{player} A Down", $"P{player} A Left", $"P{player} A Right", $"P{player} DPad U", $"P{player} DPad D", $"P{player} DPad L", $"P{player} DPad R", $"P{player} Start", $"P{player} Z", $"P{player} B", $"P{player} A", $"P{player} C Up", $"P{player} C Down", $"P{player} C Right", $"P{player} C Left", $"P{player} L", $"P{player} R" });
				def.AddXYPair(
					$"P{player} {{0}} Axis",
					AxisPairOrientation.RightAndUp,
					-128,
					0,
					127,
					new AxisConstraint
					{
						Class = "Natural Circle",
						Type = AxisConstraintType.Circular,
						Params = new object[] { $"P{player} X Axis", $"P{player} Y Axis", 127.0f }
					}
				);
			}

			ControllerDefinition.BoolButtons.Clear();
			ControllerDefinition.Axes.Clear();

			ControllerDefinition.BoolButtons.AddRange(new[] { "Reset", "Power" });
			for (var i = 1; i <= 4; i++)
			{
				if (_syncSettings.Controllers[i - 1].IsConnected) AddN64StandardController(ControllerDefinition, i);
			}
		}
	}
}

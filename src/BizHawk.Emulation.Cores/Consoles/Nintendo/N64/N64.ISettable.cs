using BizHawk.Common;
using BizHawk.Common.CollectionExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.N64
{
	public partial class N64 : ISettable<N64Settings, N64SyncSettings>
	{
		public N64Settings GetSettings()
		{
			return _settings with { };
		}

		public N64SyncSettings GetSyncSettings()
		{
			return _syncSettings.Clone();
		}

		public PutSettingsDirtyBits PutSettings(N64Settings o)
		{
			bool changed = o != _settings;
			_settings = o;
			return changed ? PutSettingsDirtyBits.RebootCore : PutSettingsDirtyBits.None;
		}

		public PutSettingsDirtyBits PutSyncSettings(N64SyncSettings o)
		{
			SetControllerButtons(_syncSettings = o);
			return PutSettingsDirtyBits.RebootCore;
		}

		private void SetControllerButtons(N64SyncSettings syncSettings)
		{
			static void AddN64StandardController(ControllerDefinition def, int player, bool hasRumblePak)
			{
				def.BoolButtons.AddRange(new[] { $"P{player} A Up", $"P{player} A Down", $"P{player} A Left", $"P{player} A Right", $"P{player} DPad U", $"P{player} DPad D", $"P{player} DPad L", $"P{player} DPad R", $"P{player} Start", $"P{player} Z", $"P{player} B", $"P{player} A", $"P{player} C Up", $"P{player} C Down", $"P{player} C Left", $"P{player} C Right", $"P{player} L", $"P{player} R" });
				def.AddXYPair(
					$"P{player} {{0}} Axis",
					AxisPairOrientation.RightAndUp,
					(-128).RangeTo(127),
					0,
					new CircularAxisConstraint("Natural Circle", $"P{player} Y Axis", 127.0f)
				);
				if (hasRumblePak) def.HapticsChannels.Add($"P{player} Rumble Pak");
			}

			ControllerDefinition = new("Nintendo 64 Controller");
			ControllerDefinition.BoolButtons.AddRange(new[] { "Reset", "Power" });
			for (var i = 0; i < 4; i++)
			{
				if (_syncSettings.Controllers[i].IsConnected)
				{
					AddN64StandardController(
						ControllerDefinition,
						i + 1,
						syncSettings.Controllers[i].PakType == N64SyncSettings.N64ControllerSettings.N64ControllerPakType.RUMBLE_PAK);
				}
			}
			ControllerDefinition.MakeImmutable();
		}
	}
}

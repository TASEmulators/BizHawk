using System;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Waterbox;
using BizHawk.Common.BizInvoke;
using System.Runtime.InteropServices;
using System.IO;
using BizHawk.Common.BufferExtensions;
using System.ComponentModel;
using BizHawk.Common;
using System.Collections.Generic;
using System.Linq;

namespace BizHawk.Emulation.Cores.Nintendo.SNES9X
{
	[Core("Snes9x", "", true, true,
		"5e0319ab3ef9611250efb18255186d0dc0d7e125", "https://github.com/snes9xgit/snes9x", false)]
	[ServiceNotApplicable(typeof(IDriveLight))]
	public class Snes9x : WaterboxCore, 
		ISettable<Snes9x.Settings, Snes9x.SyncSettings>, IRegionable
	{
		private LibSnes9x _core;

		[CoreConstructor("SNES")]
		public Snes9x(CoreComm comm, byte[] rom, Settings settings, SyncSettings syncSettings)
			:base(comm, new Configuration
			{
				DefaultWidth = 256,
				DefaultHeight = 224,
				MaxWidth = 512,
				MaxHeight = 480,
				MaxSamples = 8192,
				SystemId = "SNES"
			})
		{
			settings ??= new Settings();
			syncSettings ??= new SyncSettings();

			_core = PreInit<LibSnes9x>(new PeRunnerOptions
			{
				Filename = "snes9x.wbx",
				SbrkHeapSizeKB = 1024,
				SealedHeapSizeKB = 12 * 1024,
				InvisibleHeapSizeKB = 6 * 1024,
				PlainHeapSizeKB = 64
			});

			if (!_core.biz_init())
				throw new InvalidOperationException("Init() failed");
			if (!_core.biz_load_rom(rom, rom.Length))
				throw new InvalidOperationException("LoadRom() failed");

			PostInit();

			if (_core.biz_is_ntsc())
			{
				Console.WriteLine("NTSC rom loaded");
				VsyncNumerator = 21477272;
				VsyncDenominator = 357366;
				Region = DisplayType.NTSC;
			}
			else
			{
				Console.WriteLine("PAL rom loaded");
				VsyncNumerator = 21281370;
				VsyncDenominator = 425568;
				Region = DisplayType.PAL;
			}

			_syncSettings = syncSettings;
			InitControllers();
			PutSettings(settings);
		}

		#region controller

		private readonly short[] _inputState = new short[16 * 8];
		private List<ControlDefUnMerger> _cdums;
		private readonly List<IControlDevice> _controllers = new List<IControlDevice>();

		private void InitControllers()
		{
			_core.biz_set_port_devices(_syncSettings.LeftPort, _syncSettings.RightPort);

			switch (_syncSettings.LeftPort)
			{
				case LibSnes9x.LeftPortDevice.Joypad:
					_controllers.Add(new Joypad());
					break;
			}
			switch (_syncSettings.RightPort)
			{
				case LibSnes9x.RightPortDevice.Joypad:
					_controllers.Add(new Joypad());
					break;
				case LibSnes9x.RightPortDevice.Multitap:
					_controllers.Add(new Joypad());
					_controllers.Add(new Joypad());
					_controllers.Add(new Joypad());
					_controllers.Add(new Joypad());
					break;
				case LibSnes9x.RightPortDevice.Mouse:
					_controllers.Add(new Mouse());
					break;
				case LibSnes9x.RightPortDevice.SuperScope:
					_controllers.Add(new SuperScope());
					break;
				case LibSnes9x.RightPortDevice.Justifier:
					_controllers.Add(new Justifier());
					break;
			}

			_controllerDefinition = ControllerDefinitionMerger.GetMerged(
				_controllers.Select(c => c.Definition), out _cdums);

			// add buttons that the core itself will handle
			_controllerDefinition.BoolButtons.Add("Reset");
			_controllerDefinition.BoolButtons.Add("Power");
			_controllerDefinition.Name = "SNES Controller";
		}

		private void UpdateControls(IController c)
		{
			Array.Clear(_inputState, 0, 16 * 8);
			for (int i = 0, offset = 0; i < _controllers.Count; i++, offset += 16)
			{
				_controllers[i].ApplyState(_cdums[i].UnMerge(c), _inputState, offset);
			}
		}

		private interface IControlDevice
		{
			ControllerDefinition Definition { get; }
			void ApplyState(IController controller, short[] input, int offset);
		}

		private class Joypad : IControlDevice
		{
			private static readonly string[] Buttons =
			{
				"0B",
				"0Y",
				"0Select",
				"0Start",
				"0Up",
				"0Down",
				"0Left",
				"0Right",
				"0A",
				"0X",
				"0L",
				"0R"
			};

			private static int ButtonOrder(string btn)
			{
				var order = new Dictionary<string, int>
				{
					["0Up"] = 0,
					["0Down"] = 1,
					["0Left"] = 2,
					["0Right"] = 3,

					["0Select"] = 4,
					["0Start"] = 5,

					["0Y"] = 6,
					["0B"] = 7,

					["0X"] = 8,
					["0A"] = 9,

					["0L"] = 10,
					["0R"] = 11
				};

				return order[btn];
			}

			private static readonly ControllerDefinition _definition = new ControllerDefinition
			{
				BoolButtons = Buttons.OrderBy(ButtonOrder).ToList()
			};

			public ControllerDefinition Definition { get; } = _definition;

			public void ApplyState(IController controller, short[] input, int offset)
			{
				for (int i = 0; i < Buttons.Length; i++)
					input[offset + i] = (short)(controller.IsPressed(Buttons[i]) ? 1 : 0);
			}
		}

		private abstract class Analog : IControlDevice
		{
			public abstract ControllerDefinition Definition { get; }

			public void ApplyState(IController controller, short[] input, int offset)
			{
				foreach (var s in Definition.FloatControls)
					input[offset++] = (short)(controller.GetFloat(s));
				foreach (var s in Definition.BoolButtons)
					input[offset++] = (short)(controller.IsPressed(s) ? 1 : 0);
			}
		}

		private class Mouse : Analog
		{
			private static readonly ControllerDefinition _definition = new ControllerDefinition
			{
				BoolButtons = new List<string>
				{
					"0Mouse Left",
					"0Mouse Right"
				},
				FloatControls =
				{
					"0Mouse X",
					"0Mouse Y"
				},
				FloatRanges =
				{
					new[] { -127f, 0f, 127f },
					new[] { -127f, 0f, 127f }
				}
			};

			public override ControllerDefinition Definition => _definition;
		}

		private class SuperScope : Analog
		{
			private static readonly ControllerDefinition _definition = new ControllerDefinition
			{
				BoolButtons = new List<string>
				{
					"0Trigger",
					"0Cursor",
					"0Turbo",
					"0Pause"
				},
				FloatControls =
				{
					"0Scope X",
					"0Scope Y"
				},
				FloatRanges =
				{
					// snes9x is always in 224 mode
					new[] { 0f, 128f, 256f },
					new[] { 0f, 0f, 240f }
				}
			};

			public override ControllerDefinition Definition => _definition;
		}

		private class Justifier : Analog
		{
			private static readonly ControllerDefinition _definition = new ControllerDefinition
			{
				BoolButtons = new List<string>
				{
					"0Trigger",
					"0Start",
				},
				FloatControls =
				{
					"0Justifier X",
					"0Justifier Y",
				},
				FloatRanges =
				{
					// snes9x is always in 224 mode
					new[] { 0f, 128f, 256f },
					new[] { 0f, 0f, 240f },
				}
			};

			public override ControllerDefinition Definition => _definition;
		}

		private ControllerDefinition _controllerDefinition;
		public override ControllerDefinition ControllerDefinition => _controllerDefinition;

		#endregion

		public DisplayType Region { get; }

		protected override LibWaterboxCore.FrameInfo FrameAdvancePrep(IController controller, bool render, bool rendersound)
		{
			if (controller.IsPressed("Power"))
				_core.biz_hard_reset();
			else if (controller.IsPressed("Reset"))
				_core.biz_soft_reset();
			UpdateControls(controller);
			_core.SetButtons(_inputState);

			return new LibWaterboxCore.FrameInfo();
		}

		public override int VirtualWidth => BufferWidth == 256 && BufferHeight <= 240 ? 293 : 587;
		public override int VirtualHeight => BufferHeight <= 240 && BufferWidth == 512 ? BufferHeight * 2 : BufferHeight;

		#region IStatable

		protected override void LoadStateBinaryInternal(BinaryReader reader)
		{
			_core.biz_post_load_state();
		}

		#endregion

		#region settings

		private Settings _settings;
		private SyncSettings _syncSettings;

		public Settings GetSettings()
		{
			return _settings.Clone();
		}

		public SyncSettings GetSyncSettings()
		{
			return _syncSettings.Clone();
		}

		public bool PutSettings(Settings o)
		{
			_settings = o;
			int s = 0;
			if (o.PlaySound0) s |= 1;
			if (o.PlaySound0) s |= 2;
			if (o.PlaySound0) s |= 4;
			if (o.PlaySound0) s |= 8;
			if (o.PlaySound0) s |= 16;
			if (o.PlaySound0) s |= 32;
			if (o.PlaySound0) s |= 64;
			if (o.PlaySound0) s |= 128;
			_core.biz_set_sound_channels(s);
			int l = 0;
			if (o.ShowBg0) l |= 1;
			if (o.ShowBg1) l |= 2;
			if (o.ShowBg2) l |= 4;
			if (o.ShowBg3) l |= 8;
			if (o.ShowWindow) l |= 32;
			if (o.ShowTransparency) l |= 64;
			if (o.ShowSprites0) l |= 256;
			if (o.ShowSprites1) l |= 512;
			if (o.ShowSprites2) l |= 1024;
			if (o.ShowSprites3) l |= 2048;
			_core.biz_set_layers(l);

			return false; // no reboot needed
		}

		public bool PutSyncSettings(SyncSettings o)
		{
			var ret = SyncSettings.NeedsReboot(_syncSettings, o);
			_syncSettings = o;
			return ret;
		}

		public class Settings
		{
			[DefaultValue(true)]
			[DisplayName("Enable Sound Channel 1")]
			public bool PlaySound0 { get; set; }

			[DefaultValue(true)]
			[DisplayName("Enable Sound Channel 2")]
			public bool PlaySound1 { get; set; }

			[DefaultValue(true)]
			[DisplayName("Enable Sound Channel 3")]
			public bool PlaySound2 { get; set; }

			[DefaultValue(true)]
			[DisplayName("Enable Sound Channel 4")]
			public bool PlaySound3 { get; set; }

			[DefaultValue(true)]
			[DisplayName("Enable Sound Channel 5")]
			public bool PlaySound4 { get; set; }

			[DefaultValue(true)]
			[DisplayName("Enable Sound Channel 6")]
			public bool PlaySound5 { get; set; }

			[DefaultValue(true)]
			[DisplayName("Enable Sound Channel 7")]
			public bool PlaySound6 { get; set; }

			[DefaultValue(true)]
			[DisplayName("Enable Sound Channel 8")]
			public bool PlaySound7 { get; set; }

			[DefaultValue(true)]
			[DisplayName("Enable Background Layer 1")]
			public bool ShowBg0 { get; set; }

			[DefaultValue(true)]
			[DisplayName("Enable Background Layer 2")]
			public bool ShowBg1 { get; set; }

			[DefaultValue(true)]
			[DisplayName("Enable Background Layer 3")]
			public bool ShowBg2 { get; set; }

			[DefaultValue(true)]
			[DisplayName("Enable Background Layer 4")]
			public bool ShowBg3 { get; set; }

			[DefaultValue(true)]
			[DisplayName("Show Sprites Priority 1")]
			public bool ShowSprites0 { get; set; }

			[DefaultValue(true)]
			[DisplayName("Show Sprites Priority 2")]
			public bool ShowSprites1 { get; set; }

			[DefaultValue(true)]
			[DisplayName("Show Sprites Priority 3")]
			public bool ShowSprites2 { get; set; }

			[DefaultValue(true)]
			[DisplayName("Show Sprites Priority 4")]
			public bool ShowSprites3 { get; set; }

			[DefaultValue(true)]
			[DisplayName("Show Window")]
			public bool ShowWindow { get; set; }

			[DefaultValue(true)]
			[DisplayName("Show Transparency")]
			public bool ShowTransparency { get; set; }

			public Settings()
			{
				SettingsUtil.SetDefaultValues(this);
			}

			public Settings Clone()
			{
				return (Settings)MemberwiseClone();
			}
		}

		public class SyncSettings
		{
			[DefaultValue(LibSnes9x.LeftPortDevice.Joypad)]
			[DisplayName("Left Port")]
			[Description("Specifies the controller type plugged into the left controller port on the console")]
			public LibSnes9x.LeftPortDevice LeftPort { get; set; }

			[DefaultValue(LibSnes9x.RightPortDevice.Joypad)]
			[DisplayName("Right Port")]
			[Description("Specifies the controller type plugged into the right controller port on the console")]
			public LibSnes9x.RightPortDevice RightPort { get; set; }

			public SyncSettings()
			{
				SettingsUtil.SetDefaultValues(this);
			}

			public SyncSettings Clone()
			{
				return (SyncSettings)MemberwiseClone();
			}

			public static bool NeedsReboot(SyncSettings x, SyncSettings y)
			{
				// the core can handle dynamic plugging and unplugging, but that changes
				// the controllerdefinition, and we're not ready for that
				return !DeepEquality.DeepEquals(x, y);
			}
		}

		#endregion
	}
}

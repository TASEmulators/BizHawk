using BizHawk.Common;
using BizHawk.Common.CollectionExtensions;
using BizHawk.Common.StringExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Waterbox;

using System;
using System.Collections.Generic;

namespace BizHawk.Emulation.Cores.Computers.Amiga
{
	[PortedCore(
		name: CoreNames.PUAE,
		author: "UAE Team",
		portedVersion: "5.0.0",
		portedUrl: "https://github.com/libretro/libretro-uae")]
	public partial class PUAE : WaterboxCore
	{
		private LibPUAE _puae;
		private List<string> _args;
		private static string _chipsetCompatible = "";
		
		public const int PAL_WIDTH   = 720;
		public const int PAL_HEIGHT  = 576;
		public const int NTSC_WIDTH  = 720;
		public const int NTSC_HEIGHT = 480;
		public const int FASTMEM_AUTO = -1;
		public const int MAX_FLOPPIES = 4;

		[CoreConstructor(VSystemID.Raw.Amiga)]
		public PUAE(CoreLoadParameters<object, PUAESyncSettings> lp)
			: base(lp.Comm, new Configuration
			{
				DefaultWidth          = PAL_WIDTH,
				DefaultHeight         = PAL_HEIGHT,
				MaxWidth              = PAL_WIDTH,
				MaxHeight             = PAL_HEIGHT,
				MaxSamples            = 2 * 1024,
				SystemId              = VSystemID.Raw.Amiga,
				DefaultFpsNumerator   = 50,
				DefaultFpsDenominator = 1
			})
		{
			_syncSettings = lp.SyncSettings ?? new();
			var filesToRemove = new List<string>();
			CreateArguments(_syncSettings);
			ControllerDefinition = InitInput();

			_puae = PreInit<LibPUAE>(new WaterboxOptions
			{
				Filename                   = "puae.wbx",
				SbrkHeapSizeKB             = 5 * 512,
				SealedHeapSizeKB           = 10 * 1024,
				InvisibleHeapSizeKB        = 10 * 1024,
				PlainHeapSizeKB            = 10 * 1024,
				MmapHeapSizeKB             = 40 * 1024,
				SkipCoreConsistencyCheck   = lp.Comm.CorePreferences.HasFlag(CoreComm.CorePreferencesFlags.WaterboxCoreConsistencyCheck),
				SkipMemoryConsistencyCheck = lp.Comm.CorePreferences.HasFlag(CoreComm.CorePreferencesFlags.WaterboxMemoryConsistencyCheck),
			});

			for (var index = 0; index < Math.Min(Math.Min(
				lp.Roms.Count,
				MAX_FLOPPIES),
				_syncSettings.FloppyDrives
			); index++)
			{
				_exe.AddReadonlyFile(lp.Roms[index].FileData, "disk" + index);
				filesToRemove.Add("disk" + index);
				AppendSetting($"floppy{ index }=disk{ index }");
				AppendSetting($"floppy{ index }type={ (int)DriveType.DRV_35_DD }");
			}

			var (kickstartData, kickstartInfo) = CoreComm.CoreFileProvider.GetFirmwareWithGameInfoOrThrow(
				new(VSystemID.Raw.Amiga, _chipsetCompatible),
				"Firmware files are usually required and may stop your game from loading");
			_exe.AddReadonlyFile(kickstartData, kickstartInfo.Name);
			filesToRemove.Add(kickstartInfo.Name);
			_args.AddRange(new List<string>
			{
				"-r", kickstartInfo.Name
			});

			if (!_puae.Init(_args.Count, _args.ToArray()))
				throw new InvalidOperationException("Core rejected the rom!");

			foreach (var s in filesToRemove)
			{
				//_exe.RemoveReadonlyFile(s);
			}

			PostInit();
		}

		private static ControllerDefinition InitInput()
		{
			var controller = new ControllerDefinition("Amiga Controller");

			foreach (var b in Enum.GetValues(typeof(LibPUAE.PUAEJoystick)))
			{
				var name = Enum.GetName(typeof(LibPUAE.PUAEJoystick), b).Replace('_', ' ');
				controller.BoolButtons.Add(name);
				controller.CategoryLabels[name] = "Joystick";
			}

			controller.BoolButtons.AddRange(new List<string>
			{
				"Mouse Left Button", "Mouse Middle Button", "Mouse Right Button"
			});

			controller
				.AddAxis("Mouse X", (0).RangeTo(PAL_WIDTH),  PAL_WIDTH  / 2)
				.AddAxis("Mouse Y", (0).RangeTo(PAL_HEIGHT), PAL_HEIGHT / 2);

			foreach (var b in controller.BoolButtons)
			{
				if (b.StartsWithOrdinal("Mouse"))
				{
					controller.CategoryLabels[b] = "Mouse";
				}
			}

			foreach (var b in Enum.GetValues(typeof(LibPUAE.PUAEKeyboard)))
			{
				var name = Enum.GetName(typeof(LibPUAE.PUAEKeyboard), b).Replace('_', ' ');
				controller.BoolButtons.Add(name);
				controller.CategoryLabels[name] = "Keyboard";
			}

			return controller.MakeImmutable();
		}

		protected override LibWaterboxCore.FrameInfo FrameAdvancePrep(IController controller, bool render, bool rendersound)
		{
			var fi = new LibPUAE.FrameInfo
			{
				MouseButtons = 0
			};

			foreach (var b in Enum.GetValues(typeof(LibPUAE.PUAEJoystick)))
			{
				if (controller.IsPressed(Enum.GetName(typeof(LibPUAE.PUAEJoystick), b).Replace('_', ' ')))
				{
					fi.JoystickState |= (LibPUAE.PUAEJoystick)b;
				}
			}
			
			if (controller.IsPressed("Mouse Left Button"))
			{
				fi.MouseButtons |= 1 << 0;
			}
			if (controller.IsPressed("Mouse Right Button"))
			{
				fi.MouseButtons |= 1 << 1;
			}
			if (controller.IsPressed("Mouse Middle Button"))
			{
				fi.MouseButtons |= 1 << 2;
			}

			fi.MouseX = controller.AxisValue("Mouse X");
			fi.MouseY = controller.AxisValue("Mouse Y");
			
			foreach (var b in Enum.GetValues(typeof(LibPUAE.PUAEKeyboard)))
			{
				var name = Enum.GetName(typeof(LibPUAE.PUAEKeyboard), b);
				var value = (int)Enum.Parse(typeof(LibPUAE.PUAEKeyboard), name);
				if (controller.IsPressed(name.Replace('_', ' ')))
				{
					unsafe
					{
						fi.Keys.Buffer[value] = 1;
					}
				}
			}

			return fi;
		}
	}
}
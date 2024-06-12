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

		[CoreConstructor(VSystemID.Raw.Amiga)]
		public PUAE(CoreLoadParameters<object, PUAESyncSettings> lp)
			: base(lp.Comm, new Configuration
			{
				DefaultWidth          = LibPUAE.PAL_WIDTH,
				DefaultHeight         = LibPUAE.PAL_HEIGHT,
				MaxWidth              = LibPUAE.PAL_WIDTH,
				MaxHeight             = LibPUAE.PAL_HEIGHT,
				MaxSamples            = 2 * 1024,
				SystemId              = VSystemID.Raw.Amiga,
				DefaultFpsNumerator   = 50,
				DefaultFpsDenominator = 1
			})
		{
			_syncSettings = lp.SyncSettings ?? new();

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

			var filesToRemove = new List<string>();
			
			CreateArguments(_syncSettings);

			_exe.AddReadonlyFile(lp.Roms[0].FileData, "romfile");
			filesToRemove.Add("romfile");

			var (kickstartData, kickstartInfo) = CoreComm.CoreFileProvider.GetFirmwareWithGameInfoOrThrow(
				new(VSystemID.Raw.Amiga, _chipsetCompatible),
				"Firmware files are usually required and may stop your game from loading");
			_exe.AddReadonlyFile(kickstartData, kickstartInfo.Name);
			filesToRemove.Add(kickstartInfo.Name);
			_args.AddRange(new List<string>
			{
				"-r", kickstartInfo.Name
			});

			ControllerDefinition = InitInput();

			if (!_puae.Init(_args.Count, _args.ToArray()))
				throw new InvalidOperationException("Core rejected the rom!");

			foreach (var s in filesToRemove)
			{
				//_exe.RemoveReadonlyFile(s);
			}

			PostInit();
		}

		private void CreateArguments(PUAESyncSettings settings)
		{
			_args = new List<string>
			{
				"puae",
				"-0", "romfile",
			};

			switch(settings.MachineConfig)
			{
				case MachineConfig.A500_OCS_130_512K_512K:
					_chipsetCompatible = Enum.GetName(typeof(ChipsetCompatible), ChipsetCompatible.A500);
					AppendSetting(new List<string>
					{
						"cpu_model=" + (int)CpuModel._68000,
						"chipset=" + Chipset.OCS,
						"chipset_compatible=" + _chipsetCompatible,
						"chipmem_size=" + (int)ChipMemory.KB_512,
						"bogomem_size=" + (int)SlowMemory.KB_512,
						"fastmem_size=0",
					});
					EnableCycleExact();
					break;
				case MachineConfig.A600_ECS_205_2M:
					_chipsetCompatible = Enum.GetName(typeof(ChipsetCompatible), ChipsetCompatible.A600);
					AppendSetting(new List<string>
					{
						"cpu_model=" + (int)CpuModel._68000,
						"chipset=" + Chipset.ECS,
						"chipset_compatible=" + _chipsetCompatible,
						"chipmem_size=" + (int)ChipMemory.MB_2,
						"bogomem_size=" + (int)SlowMemory.KB_0,
						"fastmem_size=0",
					});
					EnableCycleExact();
					break;
				case MachineConfig.A1200_AGA_310_2M_8M:
					_chipsetCompatible = Enum.GetName(typeof(ChipsetCompatible), ChipsetCompatible.A1200);
					AppendSetting(new List<string>
					{
						"cpu_model=" + (int)CpuModel._68020,
						"chipset=" + Chipset.AGA,
						"chipset_compatible=" + _chipsetCompatible,
						"chipmem_size=" + (int)ChipMemory.MB_2,
						"bogomem_size=" + (int)SlowMemory.KB_0,
						"fastmem_size=8",
					});
					EnableCycleExact();
					break;
				case MachineConfig.A4000_AGA_310_2M_8M:
					_chipsetCompatible = Enum.GetName(typeof(ChipsetCompatible), ChipsetCompatible.A4000);
					AppendSetting(new List<string>
					{
						"cpu_model=" + (int)CpuModel._68040,
						"fpu_model=68040",
						"mmu_model=68040",
						"chipset=" + Chipset.AGA,
						"chipset_compatible=" + _chipsetCompatible,
						"chipmem_size=" + (int)ChipMemory.MB_2,
						"bogomem_size=" + (int)SlowMemory.KB_0,
						"fastmem_size=8",
					});
					break;
			}

			if (settings.CpuModel != CpuModel.Auto)
			{
				AppendSetting("cpu_model=" + (int)settings.CpuModel);

				if (settings.CpuModel < CpuModel._68030)
				{
					EnableCycleExact();
				}
			}

			if (settings.Chipset != Chipset.Auto)
			{
				AppendSetting("chipset=" + (int)settings.Chipset);
			}

			if (settings.ChipsetCompatible != ChipsetCompatible.Auto)
			{
				AppendSetting("chipset_compatible="
					+ Enum.GetName(typeof(ChipsetCompatible), settings.ChipsetCompatible));
			}

			if (settings.ChipMemory != ChipMemory.Auto)
			{
				AppendSetting("chipmem_size=" + (int)settings.ChipMemory);
			}

			if (settings.SlowMemory != SlowMemory.Auto)
			{
				AppendSetting("bogomem_size=" + (int)settings.SlowMemory);
			}

			if (settings.FastMemory != FASTMEM_AUTO)
			{
				AppendSetting("fastmem_size=" + settings.FastMemory);
			}

			AppendSetting("input.mouse_speed=" + settings.MouseSpeed);
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
				.AddAxis("Mouse X", (0).RangeTo(LibPUAE.PAL_WIDTH),  LibPUAE.PAL_WIDTH  / 2)
				.AddAxis("Mouse Y", (0).RangeTo(LibPUAE.PAL_HEIGHT), LibPUAE.PAL_HEIGHT / 2);

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
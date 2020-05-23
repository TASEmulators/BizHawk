using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Waterbox
{
	public unsafe abstract class NymaCore : WaterboxCore
	{
		protected NymaCore(GameInfo game, byte[] rom, CoreComm comm, Configuration c)
			: base(comm, c)
		{
		}

		private LibNymaCore _nyma;
		private ControllerAdapter _controllerAdapter;
		private readonly byte[] _inputPortData = new byte[16 * 16];

		protected T DoInit<T>(GameInfo game, byte[] rom, string filename, string extension)
			where T : LibNymaCore
		{
			var t = PreInit<T>(new WaterboxOptions
			{
				// TODO cfg and stuff
				Filename = filename,
				SbrkHeapSizeKB = 1024 * 16,
				SealedHeapSizeKB = 1024 * 16,
				InvisibleHeapSizeKB = 1024 * 16,
				PlainHeapSizeKB = 1024 * 16,
				MmapHeapSizeKB = 1024 * 16,
				StartAddress = WaterboxHost.CanonicalStart,
				SkipCoreConsistencyCheck = CoreComm.CorePreferences.HasFlag(CoreComm.CorePreferencesFlags.WaterboxCoreConsistencyCheck),
				SkipMemoryConsistencyCheck = CoreComm.CorePreferences.HasFlag(CoreComm.CorePreferencesFlags.WaterboxMemoryConsistencyCheck),
			});
			_nyma = t;

			using (_exe.EnterExit())
			{
				var fn = game.FilesystemSafeName();

				_exe.AddReadonlyFile(rom, fn);

				var didInit = _nyma.Init(new LibNymaCore.InitData
				{
					// TODO: Set these as some cores need them
					FileNameBase = "",
					FileNameExt = extension.Trim('.').ToLowerInvariant(),
					FileNameFull = fn
				});

				if (!didInit)
					throw new InvalidOperationException("Core rejected the rom!");

				_exe.RemoveReadonlyFile(fn);

				var info = _nyma.GetSystemInfo();
				_videoBuffer = new int[info.MaxWidth * info.MaxHeight];
				BufferWidth = info.NominalWidth;
				BufferHeight = info.NominalHeight;
				switch (info.VideoSystem)
				{
					// TODO: There seriously isn't any region besides these?
					case LibNymaCore.VideoSystem.PAL:
					case LibNymaCore.VideoSystem.SECAM:
						Region = DisplayType.PAL;
						break;
					case LibNymaCore.VideoSystem.PAL_M:
						Region = DisplayType.Dendy; // sort of...
						break;
					default:
						Region = DisplayType.NTSC;
						break;
				}
				VsyncNumerator = info.FpsFixed;
				VsyncDenominator = 1 << 24;

				_controllerAdapter = new ControllerAdapter(_nyma, new string[0]);
				_nyma.SetInputDevices(_controllerAdapter.Devices);

				PostInit();
			}

			return t;
		}

		// todo: bleh
		private GCHandle _frameAdvanceInputLock;

		protected override LibWaterboxCore.FrameInfo FrameAdvancePrep(IController controller, bool render, bool rendersound)
		{
			_controllerAdapter.SetBits(controller, _inputPortData);
			_frameAdvanceInputLock = GCHandle.Alloc(_inputPortData, GCHandleType.Pinned);
			var ret = new LibNymaCore.FrameInfo
			{
				SkipRendering = render ? 0 : 1,
				Command = LibNymaCore.CommandType.NONE,
				InputPortData = (byte*)_frameAdvanceInputLock.AddrOfPinnedObject()
			};
			return ret;
		}
		protected override void FrameAdvancePost()
		{
			_frameAdvanceInputLock.Free();
		}

		protected delegate void ControllerThunk(IController c, byte[] b);

		protected class ControllerAdapter
		{
			public string[] Devices { get; }
			public ControllerDefinition Definition { get; }
			public ControllerAdapter(LibNymaCore core, string[] config)
			{
				var ret = new ControllerDefinition
				{
					Name = "TODO"
				};

				var finalDevices = new List<string>();

				var numPorts = core.GetNumPorts();
				for (uint i = 0, devByteStart = 0; i < numPorts; i++)
				{
					var port = *core.GetPort(i);
					var devName = i < config.Length ? config[i] : port.DefaultDeviceShortName;
					finalDevices.Add(devName);

					var devices = Enumerable.Range(0, (int)port.NumDevices)
						.Select(j => new { Index = (uint)j, Device = *core.GetDevice(i, (uint)j) })
						.ToList();
					
					var device = devices.FirstOrDefault(a => a.Device.ShortName == devName);
					if (device == null)
					{
						Console.WriteLine($"Warn: unknown controller device {devName}");
						device = devices.FirstOrDefault(a => a.Device.ShortName == port.DefaultDeviceShortName);
						if (device == null)
							throw new InvalidOperationException($"Fail: unknown controller device {port.DefaultDeviceShortName}");
					}

					var dev = device.Device;
					var category = port.FullName + " - " + dev.FullName;

					var inputs = Enumerable.Range(0, (int)dev.NumInputs)
						.Select(iix => new { Index = iix, Data = *core.GetInput(i, device.Index, (uint)iix) })
						.OrderBy(a => a.Data.ConfigOrder);

					foreach (var inputzz in inputs)
					{
						var input = inputzz.Data;
						var bitSize = (int)input.BitSize;
						var bitOffset = (int)input.BitOffset;
						var byteStart = devByteStart + bitOffset / 8;
						bitOffset %= 8;
						var name = input.Name;
						switch (input.Type)
						{
							case LibNymaCore.InputType.PADDING:
							{
								break;
							}
							case LibNymaCore.InputType.BUTTON:
							case LibNymaCore.InputType.BUTTON_CAN_RAPID:
							{
								var data = *core.GetButton(i, device.Index, (uint)inputzz.Index);
								// TODO: Wire up data.ExcludeName
								ret.BoolButtons.Add(name);
								_thunks.Add((c, b) =>
								{
									if (c.IsPressed(name))
										b[byteStart] |= (byte)(1 << bitOffset);
								});
								break;
							}
							case LibNymaCore.InputType.SWITCH:
							{
								var data = *core.GetSwitch(i, device.Index, (uint)inputzz.Index);
								// TODO: Possibly bulebutton for 2 states?
								ret.AxisControls.Add(name);
								ret.AxisRanges.Add(new ControllerDefinition.AxisRange(
									0, (int)data.DefaultPosition, (int)data.NumPositions - 1));
								_thunks.Add((c, b) =>
								{
									var val = (int)Math.Round(c.AxisValue(name));
									b[byteStart] |= (byte)(1 << bitOffset);
								});								
								break;
							}
							case LibNymaCore.InputType.AXIS:
							{
								var data = core.GetAxis(i, device.Index, (uint)inputzz.Index);
								ret.AxisControls.Add(name);
								ret.AxisRanges.Add(new ControllerDefinition.AxisRange(
									0, 0x8000, 0xffff, (input.Flags & LibNymaCore.AxisFlags.INVERT_CO) != 0
								));
								_thunks.Add((c, b) =>
								{
									var val = (ushort)Math.Round(c.AxisValue(name));
									b[byteStart] = (byte)val;
									b[byteStart + 1] = (byte)(val >> 8);
								});									
								break;
							}
							case LibNymaCore.InputType.AXIS_REL:
							{
								var data = core.GetAxis(i, device.Index, (uint)inputzz.Index);
								ret.AxisControls.Add(name);
								ret.AxisRanges.Add(new ControllerDefinition.AxisRange(
									-0x8000, 0, 0x7fff, (input.Flags & LibNymaCore.AxisFlags.INVERT_CO) != 0
								));
								_thunks.Add((c, b) =>
								{
									var val = (short)Math.Round(c.AxisValue(name));
									b[byteStart] = (byte)val;
									b[byteStart + 1] = (byte)(val >> 8);
								});									
								break;							
							}
							case LibNymaCore.InputType.POINTER_X:
							{
								throw new Exception("TODO: Axis ranges are ints????");
								// ret.AxisControls.Add(name);
								// ret.AxisRanges.Add(new ControllerDefinition.AxisRange(0, 0.5, 1));
								// break;
							}
							case LibNymaCore.InputType.POINTER_Y:
							{
								throw new Exception("TODO: Axis ranges are ints????");
								// ret.AxisControls.Add(name);
								// ret.AxisRanges.Add(new ControllerDefinition.AxisRange(0, 0.5, 1, true));
								// break;
							}
							// TODO: wire up statuses to something (not controller, of course)
							default:
								throw new NotImplementedException($"Unimplemented button type {input.Type}");
						}
						ret.CategoryLabels[name] = category;
					}

					devByteStart += dev.ByteLength;
				}
				Definition = ret;
				Devices = finalDevices.ToArray();
			}

			private readonly List<Action<IController, byte[]>> _thunks = new List<Action<IController, byte[]>>();

			public void SetBits(IController src, byte[] dest)
			{
				Array.Clear(dest, 0, dest.Length);
				foreach (var t in _thunks)
					t(src, dest);
			}
		}

		public DisplayType Region { get; protected set; }

		/// <summary>
		/// Gets a string array of valid layers to pass to SetLayers, or null if that method should not be called
		/// </summary>
		private string[] GetLayerData()
		{
			using (_exe.EnterExit())
			{
				var p = _nyma.GetLayerData();
				if (p == null)
					return null;
				var ret = new List<string>();
				var q = p;
				while (true)
				{
					if (*q == 0)
					{
						if (q > p)
							ret.Add(Marshal.PtrToStringAnsi((IntPtr)p));
						else
							break;
						p = ++q;
					}
					q++;
				}
				return ret.ToArray();
			}
		}
	}
}

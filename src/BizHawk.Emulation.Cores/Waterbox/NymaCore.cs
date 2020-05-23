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

		protected T DoInit<T>(GameInfo game, byte[] rom, string filename)
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
					FileNameExt = "",
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

				_controllerAdapter = new ControllerAdapter(_nyma.GetInputDevices().Infos, new string[0]);
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

		private static T ControllerData<T>(byte[] data)
		{
			fixed(byte *p = data)
			{
				return (T)Marshal.PtrToStructure((IntPtr)p, typeof(T));
			}
		}

		protected delegate void ControllerThunk(IController c, byte[] b);

		protected class ControllerAdapter
		{
			public string[] Devices { get; }
			public ControllerDefinition Definition { get; }
			public ControllerAdapter(LibNymaCore.NPortInfo[] portInfos, string[] devices)
			{
				var ret = new ControllerDefinition
				{
					Name = "TODO"
				};

				var finalDevices = new List<string>();

				for (int i = 0, devByteStart = 0; i < portInfos.Length && portInfos[i].ShortName != null; i++)
				{
					var port = portInfos[i];
					var devName = i < devices.Length ? devices[i] : port.DefaultDeviceShortName;
					finalDevices.Add(devName);

					var dev = port.Devices.SingleOrDefault(d => d.ShortName == devName);
					var category = port.FullName + " - " + dev.FullName;

					foreach (var input in dev.Inputs.OrderBy(i => i.ConfigOrder))
					{
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
								var data = ControllerData<LibNymaCore.NPortInfo.NDeviceInfo.NInput.Button>(input.UnionData);
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
								var data = ControllerData<LibNymaCore.NPortInfo.NDeviceInfo.NInput.Switch>(input.UnionData);
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
								var data = ControllerData<LibNymaCore.NPortInfo.NDeviceInfo.NInput.Axis>(input.UnionData);
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
								var data = ControllerData<LibNymaCore.NPortInfo.NDeviceInfo.NInput.Axis>(input.UnionData);
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
							default:
								throw new NotImplementedException($"Unimplemented button type {input.Type}");
						}
						ret.CategoryLabels[name] = category;
					}

					devByteStart += (int)dev.ByteLength;
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
	}
}

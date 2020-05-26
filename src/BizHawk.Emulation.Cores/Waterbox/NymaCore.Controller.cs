using System;
using System.Collections.Generic;
using System.Linq;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Waterbox
{
	unsafe partial class NymaCore
	{
		private ControllerAdapter _controllerAdapter;
		private readonly byte[] _inputPortData = new byte[16 * 16];
		private readonly string _controllerDeckName;

		private void InitControls()
		{
			_controllerAdapter = new ControllerAdapter(_nyma, _syncSettingsActual.PortDevices);
			_nyma.SetInputDevices(_controllerAdapter.Devices);
			ControllerDefinition = _controllerAdapter.Definition;
		}
		protected delegate void ControllerThunk(IController c, byte[] b);

		protected class ControllerAdapter
		{
			/// <summary>
			/// allowed number of input ports.  must match native
			/// </summary>
			private const int MAX_PORTS = 16;
			/// <summary>
			/// total maximum bytes on each input port.  must match native
			/// </summary>
			private const int MAX_PORT_DATA = 16;

			/// <summary>
			/// Device list suitable to pass back to the core
			/// </summary>
			public string[] Devices { get; }
			public ControllerDefinition Definition { get; }
			public ControllerAdapter(LibNymaCore core, IList<string> config)
			{
				var ret = new ControllerDefinition
				{
					Name = "Mednafen Controller"
				};

				var finalDevices = new List<string>();

				var numPorts = core.GetNumPorts();
				if (numPorts > MAX_PORTS)
					throw new InvalidOperationException($"Too many input ports");
				for (uint port = 0, devByteStart = 0; port < numPorts; port++, devByteStart += MAX_PORT_DATA)
				{
					var portInfo = *core.GetPort(port);
					var deviceName = port < config.Count ? config[(int)port] : portInfo.DefaultDeviceShortName;
					finalDevices.Add(deviceName);

					var devices = Enumerable.Range(0, (int)portInfo.NumDevices)
						.Select(i => new { Index = (uint)i, Device = *core.GetDevice(port, (uint)i) })
						.ToList();
					
					var device = devices.FirstOrDefault(a => a.Device.ShortName == deviceName);
					if (device == null)
					{
						Console.WriteLine($"Warn: unknown controller device {deviceName}");
						device = devices.FirstOrDefault(a => a.Device.ShortName == portInfo.DefaultDeviceShortName);
						if (device == null)
							throw new InvalidOperationException($"Fail: unknown controller device {portInfo.DefaultDeviceShortName}");
					}

					var deviceInfo = device.Device;
					if (deviceInfo.ByteLength > MAX_PORT_DATA)
						throw new InvalidOperationException($"Input device {deviceInfo.ShortName} uses more than {MAX_PORT_DATA} bytes");
					var category = portInfo.FullName + " - " + deviceInfo.FullName;

					var inputs = Enumerable.Range(0, (int)deviceInfo.NumInputs)
						.Select(i => new { Index = i, Data = *core.GetInput(port, device.Index, (uint)i) })
						.OrderBy(a => a.Data.ConfigOrder);

					foreach (var input in inputs)
					{
						var inputInfo = input.Data;
						var bitSize = (int)inputInfo.BitSize;
						var bitOffset = (int)inputInfo.BitOffset;
						var byteStart = devByteStart + bitOffset / 8;
						bitOffset %= 8;
						var name = $"P{port + 1} {inputInfo.Name}";
						switch (inputInfo.Type)
						{
							case LibNymaCore.InputType.PADDING:
							{
								break;
							}
							case LibNymaCore.InputType.BUTTON:
							case LibNymaCore.InputType.BUTTON_CAN_RAPID:
							{
								// var data = *core.GetButton(port, device.Index, (uint)input.Index);
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
								var data = *core.GetSwitch(port, device.Index, (uint)input.Index);
								var zzhacky = (int)data.DefaultPosition;
								// TODO: Possibly bulebutton for 2 states?
								ret.AxisControls.Add(name);
								ret.AxisRanges.Add(new ControllerDefinition.AxisRange(
									0, (int)data.DefaultPosition, (int)data.NumPositions - 1));
								_thunks.Add((c, b) =>
								{
									// HACK: Silently discard this until bizhawk fixes its shit
								 	// var val = (int)Math.Round(c.AxisValue(name));
									var val = zzhacky;
								 	b[byteStart] |= (byte)(val << bitOffset);
								});
								break;
							}
							case LibNymaCore.InputType.AXIS:
							{
								// var data = core.GetAxis(port, device.Index, (uint)input.Index);
								ret.AxisControls.Add(name);
								ret.AxisRanges.Add(new ControllerDefinition.AxisRange(
									0, 0x8000, 0xffff, (inputInfo.Flags & LibNymaCore.AxisFlags.INVERT_CO) != 0
								));
								_thunks.Add((c, b) =>
								{
									var val = c.AxisValue(name);
									b[byteStart] = (byte)val;
									b[byteStart + 1] = (byte)(val >> 8);
								});									
								break;
							}
							case LibNymaCore.InputType.AXIS_REL:
							{
								// var data = core.GetAxis(port, device.Index, (uint)input.Index);
								ret.AxisControls.Add(name);
								ret.AxisRanges.Add(new ControllerDefinition.AxisRange(
									-0x8000, 0, 0x7fff, (inputInfo.Flags & LibNymaCore.AxisFlags.INVERT_CO) != 0
								));
								_thunks.Add((c, b) =>
								{
									var val = c.AxisValue(name);
									b[byteStart] = (byte)val;
									b[byteStart + 1] = (byte)(val >> 8);
								});									
								break;							
							}
							case LibNymaCore.InputType.POINTER_X:
							{
								throw new NotImplementedException("TODO: Support Pointer");
								// I think the core expects to be sent some sort of 16 bit integer, but haven't investigated much
								// ret.AxisControls.Add(name);
								// ret.AxisRanges.Add(new ControllerDefinition.AxisRange(0, ????, ????));
								// break;
							}
							case LibNymaCore.InputType.POINTER_Y:
							{
								throw new Exception("TODO: Support Pointer");
								// I think the core expects to be sent some sort of 16 bit integer, but haven't investigated much
								// ret.AxisControls.Add(name);
								// ret.AxisRanges.Add(new ControllerDefinition.AxisRange(0, ????, ????, true));
								// break;
							}
							// TODO: wire up statuses to something (not controller, of course)
							default:
								throw new NotImplementedException($"Unimplemented button type {inputInfo.Type}");
						}
						ret.CategoryLabels[name] = category;
					}
				}
				Definition = ret;
				finalDevices.Add(null);
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
	}
}

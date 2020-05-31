using System;
using System.Collections.Generic;
using System.Linq;
using BizHawk.Emulation.Common;
using NymaTypes;

namespace BizHawk.Emulation.Cores.Waterbox
{
	unsafe partial class NymaCore
	{
		private ControllerAdapter _controllerAdapter;
		private readonly byte[] _inputPortData = new byte[16 * 16];
		private readonly string _controllerDeckName;

		private void InitControls(List<NPortInfoT> allPorts)
		{
			_controllerAdapter = new ControllerAdapter(allPorts, _syncSettingsActual.PortDevices, ButtonNameOverrides);
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
			public ControllerAdapter(List<NPortInfoT> allPorts, IDictionary<int, string> config, IDictionary<string, string> overrides)
			{
				var ret = new ControllerDefinition
				{
					Name = "Mednafen Controller",
					CategoryLabels =
					{
						{ "Power", "System" },
						{ "Reset", "System" },
					}
				};

				var finalDevices = new List<string>();

				if (allPorts.Count > MAX_PORTS)
					throw new InvalidOperationException($"Too many input ports");
				for (int port = 0, devByteStart = 0; port < allPorts.Count; port++, devByteStart += MAX_PORT_DATA)
				{
					var portInfo = allPorts[port];
					var deviceName = config.ContainsKey(port) ? config[port] : portInfo.DefaultDeviceShortName;
					finalDevices.Add(deviceName);

					var devices = portInfo.Devices;
					
					var device = devices.FirstOrDefault(a => a.ShortName == deviceName);
					if (device == null)
					{
						Console.WriteLine($"Warn: unknown controller device {deviceName}");
						device = devices.FirstOrDefault(a => a.ShortName == portInfo.DefaultDeviceShortName);
						if (device == null)
							throw new InvalidOperationException($"Fail: unknown controller device {portInfo.DefaultDeviceShortName}");
					}

					var deviceInfo = device;
					if (deviceInfo.ByteLength > MAX_PORT_DATA)
						throw new InvalidOperationException($"Input device {deviceInfo.ShortName} uses more than {MAX_PORT_DATA} bytes");
					var category = portInfo.FullName + " - " + deviceInfo.FullName;

					var inputs = deviceInfo.Inputs
						.OrderBy(a => a.ConfigOrder);

					foreach (var input in inputs)
					{
						var inputInfo = input;
						var bitSize = (int)inputInfo.BitSize;
						var bitOffset = (int)inputInfo.BitOffset;
						var byteStart = devByteStart + bitOffset / 8;
						bitOffset %= 8;
						var baseName = inputInfo.Name;
						if (overrides.ContainsKey(baseName))
							baseName = overrides[baseName];
						var name = $"P{port + 1} {baseName}";
						switch (inputInfo.Type)
						{
							case InputType.Padding:
							{
								break;
							}
							case InputType.Button:
							case InputType.ButtonCanRapid:
							{
								// var data = inputInfo.Extra.AsButton();
								// TODO: Wire up data.ExcludeName
								ret.BoolButtons.Add(name);
								_thunks.Add((c, b) =>
								{
									if (c.IsPressed(name))
										b[byteStart] |= (byte)(1 << bitOffset);
								});
								break;
							}
							case InputType.Switch:
							{
								var data = inputInfo.Extra.AsSwitch();
								var zzhacky = (int)data.DefaultPosition;
								// TODO: Possibly bulebutton for 2 states?
								// TODO: Motorcycle shift if we can't get sticky correct?
								ret.AxisControls.Add(name);
								ret.AxisRanges.Add(new ControllerDefinition.AxisRange(
									0, (int)data.DefaultPosition, (int)data.Positions.Count - 1));
								_thunks.Add((c, b) =>
								{
									// HACK: Silently discard this until bizhawk fixes its shit
								 	// var val = (int)Math.Round(c.AxisValue(name));
									var val = zzhacky;
								 	b[byteStart] |= (byte)(val << bitOffset);
								});
								break;
							}
							case InputType.Axis:
							{
								// var data = inputInfo.Extra.AsAxis();
								ret.AxisControls.Add(name);
								ret.AxisRanges.Add(new ControllerDefinition.AxisRange(
									0, 0x8000, 0xffff, (inputInfo.Flags & AxisFlags.InvertCo) != 0
								));
								_thunks.Add((c, b) =>
								{
									var val = c.AxisValue(name);
									b[byteStart] = (byte)val;
									b[byteStart + 1] = (byte)(val >> 8);
								});									
								break;
							}
							case InputType.AxisRel:
							{
								// var data = inputInfo.Extra.AsAxis();
								ret.AxisControls.Add(name);
								ret.AxisRanges.Add(new ControllerDefinition.AxisRange(
									-0x8000, 0, 0x7fff, (inputInfo.Flags & AxisFlags.InvertCo) != 0
								));
								_thunks.Add((c, b) =>
								{
									var val = c.AxisValue(name);
									b[byteStart] = (byte)val;
									b[byteStart + 1] = (byte)(val >> 8);
								});									
								break;							
							}
							case InputType.PointerX:
							{
								throw new NotImplementedException("TODO: Support Pointer");
								// I think the core expects to be sent some sort of 16 bit integer, but haven't investigated much
								// ret.AxisControls.Add(name);
								// ret.AxisRanges.Add(new ControllerDefinition.AxisRange(0, ????, ????));
								// break;
							}
							case InputType.PointerY:
							{
								throw new Exception("TODO: Support Pointer");
								// I think the core expects to be sent some sort of 16 bit integer, but haven't investigated much
								// ret.AxisControls.Add(name);
								// ret.AxisRanges.Add(new ControllerDefinition.AxisRange(0, ????, ????, true));
								// break;
							}
							// TODO: wire up statuses to something (not controller, of course)
							default:
							{
								throw new NotImplementedException($"Unimplemented button type {inputInfo.Type}");
							}
						}
						ret.CategoryLabels[name] = category;
					}
				}
				ret.BoolButtons.Add("Power");
				ret.BoolButtons.Add("Reset");
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

		protected virtual IDictionary<string, string> ButtonNameOverrides { get; }= new Dictionary<string, string>();
	}
}

using System;
using System.Collections.Generic;
using System.IO;
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

		private void InitControls(List<NPortInfoT> allPorts, bool hasCds)
		{
			_controllerAdapter = new ControllerAdapter(
				allPorts, _syncSettingsActual.PortDevices, ButtonNameOverrides, hasCds, ComputeHiddenPorts());
			_nyma.SetInputDevices(_controllerAdapter.Devices);
			ControllerDefinition = _controllerAdapter.Definition;
		}
		protected delegate void ControllerThunk(IController c, byte[] b);

		protected class ControllerAdapter : IBinaryStateable
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
			public ControllerAdapter(
				List<NPortInfoT> allPorts,
				IDictionary<int, string> config,
				IDictionary<string, string> overrides,
				bool hasCds,
				HashSet<string> hiddenPorts)
			{
				var ret = new ControllerDefinition
				{
					Name = "Mednafen Controller",
					CategoryLabels =
					{
						{ "Power", "System" },
						{ "Reset", "System" },
						{ "Previous Disk", "System" },
						{ "Next Disk", "System" },
					}
				};

				var finalDevices = new List<string>();

				if (allPorts.Count > MAX_PORTS)
					throw new InvalidOperationException($"Too many input ports");
				var switchPreviousFrame = new List<byte>();
				for (int port = 0, devByteStart = 0; port < allPorts.Count; port++, devByteStart += MAX_PORT_DATA)
				{
					var portInfo = allPorts[port];
					var deviceName = config.ContainsKey(port) ? config[port] : portInfo.DefaultDeviceShortName;
					finalDevices.Add(deviceName);

					if (hiddenPorts.Contains(portInfo.ShortName))
						continue;

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
						if (input.Type == InputType.ResetButton)
							System.Diagnostics.Debugger.Break();
						if (input.Type == InputType.Padding)
							continue;

						var bitSize = (int)input.BitSize;
						var bitOffset = (int)input.BitOffset;
						var byteStart = devByteStart + bitOffset / 8;
						bitOffset %= 8;
						var baseName = input.Name;
						if (baseName != null && overrides.ContainsKey(baseName))
							baseName = overrides[baseName];
						var name = input.Type == InputType.ResetButton ? "Reset" : $"P{port + 1} {baseName}";

						switch (input.Type)
						{
							case InputType.ResetButton:
							case InputType.Button:
							case InputType.ButtonCanRapid:
							{
								// var data = inputInfo.Extra.AsButton();
								// TODO: Wire up data.ExcludeName
								if (input.Type != InputType.ResetButton)
								{
									ret.BoolButtons.Add(name);
									ret.CategoryLabels[name] = category;
								}
								_thunks.Add((c, b) =>
								{
									if (c.IsPressed(name))
										b[byteStart] |= (byte)(1 << bitOffset);
								});
								break;
							}
							case InputType.Switch:
							{
								var data = input.Extra.AsSwitch();
								if (data.Positions.Count > 8)
									throw new NotImplementedException("Need code changes to support Mdfn switch with more than 8 positions");
								
								// fake switches as a series of push downs that select each state
								// imagine the "gear" selector on a Toyota Prius

								var si = switchPreviousFrame.Count;
								// [si]: position of this switch on the previous frame
								switchPreviousFrame.Add((byte)data.DefaultPosition);
								// [si + 1]: bit array of the previous state of each selector button
								switchPreviousFrame.Add(0);

								var names = data.Positions.Select(p => $"{name}: Set {p.Name}").ToArray();
								foreach (var n in names)
								{
									ret.BoolButtons.Add(n);
									ret.CategoryLabels[n] = category;
								}

								_thunks.Add((c, b) =>
								{
									var val = _switchPreviousFrame[si];
									var allOldPressed = _switchPreviousFrame[si + 1];
									byte allNewPressed = 0;
									for (var i = 0; i < names.Length; i++)
									{
										var mask = (byte)(1 << i);
										var oldPressed = allOldPressed & mask;
										var newPressed = c.IsPressed(names[i]) ? mask : (byte)0;
										if (newPressed > oldPressed)
											val = (byte)i;
										allNewPressed |= newPressed;
									}
									_switchPreviousFrame[si] = val;
									_switchPreviousFrame[si + 1] = allNewPressed;
								 	b[byteStart] |= (byte)(val << bitOffset);
								});
								break;
							}
							case InputType.Axis:
							{
								var data = input.Extra.AsAxis();
								var fullName = $"{name} {input.Extra.AsAxis().NameNeg} / {input.Extra.AsAxis().NamePos}";

								ret.AxisControls.Add(fullName);
								ret.CategoryLabels[fullName] = category;
								ret.AxisRanges.Add(new ControllerDefinition.AxisRange(
									0, 0x8000, 0xffff, (input.Flags & AxisFlags.InvertCo) != 0
								));
								_thunks.Add((c, b) =>
								{
									var val = c.AxisValue(fullName);
									b[byteStart] = (byte)val;
									b[byteStart + 1] = (byte)(val >> 8);
								});									
								break;
							}
							case InputType.AxisRel:
							{
								var data = input.Extra.AsAxis();
								var fullName = $"{name} {input.Extra.AsAxis().NameNeg} / {input.Extra.AsAxis().NamePos}";

								ret.AxisControls.Add(fullName);
								ret.CategoryLabels[fullName] = category;
								ret.AxisRanges.Add(new ControllerDefinition.AxisRange(
									// TODO: Mednafen docs say this range should be [-32768, 32767], and inspecting the code
									// reveals that a 16 bit value is read, but using anywhere near this full range makes
									// PCFX mouse completely unusable.  Maybe this is some TAS situation where average users
									// will want a 1/400 multiplier on sensitivity but TASers might want one frame screenwide movement?
									-127, 0, 127, (input.Flags & AxisFlags.InvertCo) != 0
								));
								_thunks.Add((c, b) =>
								{
									var val = c.AxisValue(fullName);
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
								throw new NotImplementedException($"Unimplemented button type {input.Type}");
							}
						}
					}
				}
				ret.BoolButtons.Add("Power");
				ret.BoolButtons.Add("Reset");
				if (hasCds)
				{
					ret.BoolButtons.Add("Previous Disk");
					ret.BoolButtons.Add("Next Disk");
				}
				Definition = ret;
				finalDevices.Add(null);
				Devices = finalDevices.ToArray();
				_switchPreviousFrame = switchPreviousFrame.ToArray();
			}

			private byte[] _switchPreviousFrame;

			private readonly List<Action<IController, byte[]>> _thunks = new List<Action<IController, byte[]>>();

			public void SetBits(IController src, byte[] dest)
			{
				Array.Clear(dest, 0, dest.Length);
				foreach (var t in _thunks)
					t(src, dest);
			}

			private const ulong MAGIC = 9569546739673486731;

			public void SaveStateBinary(BinaryWriter writer)
			{
				writer.Write(MAGIC);
				writer.Write(_switchPreviousFrame.Length);
				writer.Write(_switchPreviousFrame);
			}

			public void LoadStateBinary(BinaryReader reader)
			{
				if (reader.ReadUInt64() != MAGIC || reader.ReadInt32() != _switchPreviousFrame.Length)
					throw new InvalidOperationException("Savestate corrupted!");
				reader.Read(_switchPreviousFrame, 0, _switchPreviousFrame.Length);
			}
		}

		protected virtual IDictionary<string, string> ButtonNameOverrides { get; } = new Dictionary<string, string>();
		/// <summary>
		/// On some cores, some controller ports are not relevant when certain settings are off (like multitap).
		/// Override this if your core has such an issue
		/// </summary>
		protected virtual HashSet<string> ComputeHiddenPorts()
		{
			return new HashSet<string>();
		}
	}
}

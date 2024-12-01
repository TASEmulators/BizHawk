using System.Collections.Generic;
using System.IO;
using System.Linq;

using BizHawk.Common;
using BizHawk.Common.NumberExtensions;
using BizHawk.Common.StringExtensions;
using BizHawk.Emulation.Common;
using NymaTypes;

using static BizHawk.Emulation.Cores.Waterbox.LibNymaCore;

namespace BizHawk.Emulation.Cores.Waterbox
{
	public partial class NymaCore
	{
		private const int MAX_INPUT_DATA = 256;

		private ControllerAdapter _controllerAdapter;
		private readonly byte[] _inputPortData = new byte[MAX_INPUT_DATA];
		private readonly string _controllerDeckName;

		protected delegate void AddAxisHook(
			ControllerDefinition ret,
			string name,
			bool isReversed,
			ref ControllerThunk thunk,
			int thunkWriteOffset);

		protected virtual void AddAxis(
			ControllerDefinition ret,
			string name,
			bool isReversed,
			ref ControllerThunk thunk,
			int thunkWriteOffset)
				=> ret.AddAxis(name, 0.RangeTo(0xFFFF), 0x8000, isReversed);

		private string GetInputDeviceOverride(int port)
			=> Mershul.PtrToStringUtf8(_nyma.GetInputDeviceOverride(port));

		private void InitControls(List<NPortInfoT> allPorts, int numCds, ref SystemInfo si)
		{
			_controllerAdapter = new ControllerAdapter(
				allPorts,
				_syncSettingsActual.PortDevices,
				OverrideButtonName,
				numCds,
				ref si,
				GetInputDeviceOverride,
				ComputeHiddenPorts(),
				AddAxis,
				_controllerDeckName);
			_nyma.SetInputDevices(_controllerAdapter.Devices);
			ControllerDefinition = _controllerAdapter.Definition;
		}

		protected delegate void ControllerThunk(IController c, byte[] b);

		protected class ControllerAdapter : IStatable
		{
			/// <summary>
			/// Device list suitable to pass back to the core
			/// </summary>
			public string[] Devices { get; }
			public ControllerDefinition Definition { get; }
			public List<PortResult> ActualPortData { get; set; } = new List<PortResult>();
			public ControllerAdapter(
				List<NPortInfoT> allPorts,
				IDictionary<int, string> config,
				Func<string, string> overrideName,
				int numCds,
				ref SystemInfo systemInfo,
				Func<int, string> getInputDeviceOverride,
				HashSet<string> hiddenPorts,
				AddAxisHook addAxisHook,
				string controllerDeckName)
			{
				ControllerDefinition ret = new(controllerDeckName)
				{
					CategoryLabels =
					{
						{ "Power", "System" },
						{ "Reset", "System" },
						{ "Insert Coin", "System" },
						{ "Open Tray", "System" },
						{ "Close Tray", "System" },
						{ "Disk Index", "System" },
					}
				};

				var finalDevices = new List<string>();

				var switchPreviousFrame = new List<byte>();
				for (int port = 0, devByteStart = 0; port < allPorts.Count; port++)
				{
					var portInfo = allPorts[port];
					var deviceName = getInputDeviceOverride(port);
					if (deviceName == null)
					{
						if (!config.TryGetValue(port, out deviceName))
						{
							deviceName = portInfo.DefaultDeviceShortName;
						}
					}

					finalDevices.Add(deviceName);

					if (hiddenPorts.Contains(portInfo.ShortName))
						continue;

					var devices = portInfo.Devices;
					
					var device = devices.Find(a => a.ShortName == deviceName);
					if (device == null)
					{
						Console.WriteLine($"Warn: unknown controller device {deviceName}");
						device = devices.Find(a => a.ShortName == portInfo.DefaultDeviceShortName)
							?? throw new InvalidOperationException($"Fail: unknown controller device {portInfo.DefaultDeviceShortName}");
					}

					ActualPortData.Add(new PortResult
					{
						Port = portInfo,
						Device = device
					});

					var deviceInfo = device;
					var category = portInfo.FullName + " - " + deviceInfo.FullName;

					var inputs = deviceInfo.Inputs
						.OrderBy(a => a.ConfigOrder);

					foreach (var input in inputs)
					{
						if (input.Type == InputType.Padding0)
							continue;

						var bitSize = (int)input.BitSize;
						var bitOffset = (int)input.BitOffset;
						var byteStart = devByteStart + bitOffset / 8;
						bitOffset %= 8;
						var baseName = input.Name;
						if (baseName != null)
							baseName = overrideName(baseName);
						var name = input.Type == InputType.ResetButton ? "Reset" : $"P{port + 1} {baseName}";

						switch (input.Type)
						{
							case InputType.Padding1:
							{
								// padding with set bits
								_thunks.Add((_, b) =>
								{
									var val = (byte)(1 << bitOffset);
									var byteOffset = byteStart;
									for (var i = 0; i < bitSize; i++)
									{
										b[byteOffset] |= val;
										val <<= 1;
										if (val == 0)
										{
											val = 1;
											byteOffset++;
										}
									}
								});

								break;
							}
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
								if (!input.Name.StartsWithOrdinal("AF ") && !input.Name.EndsWithOrdinal(" AF") && !input.Name.StartsWithOrdinal("Autofire ")) // hack: don't support some devices
								{
									foreach (var n in names)
									{
										{
											ret.BoolButtons.Add(n);
											ret.CategoryLabels[n] = category;
										}
									}
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
								var fullName = $"{name} {overrideName(data.NameNeg)} / {overrideName(data.NamePos)}";
								ControllerThunk thunk = (c, b) =>
								{
									var val = c.AxisValue(fullName);
									b[byteStart] = (byte)val;
									b[byteStart + 1] = (byte)(val >> 8);
								};
								addAxisHook(ret, fullName, (input.Flags & AxisFlags.InvertCo) is not 0, ref thunk, byteStart);
								ret.CategoryLabels[fullName] = category;
								_thunks.Add(thunk);
								break;
							}
							case InputType.AxisRel:
							{
								var data = input.Extra.AsAxis();
								var fullName = $"{name} {input.Extra.AsAxis().NameNeg} / {input.Extra.AsAxis().NamePos}";

								// TODO: Mednafen docs say this range should be [-32768, 32767], and inspecting the code
								// reveals that a 16 bit value is read, but using anywhere near this full range makes
								// PCFX mouse completely unusable.  Maybe this is some TAS situation where average users
								// will want a 1/400 multiplier on sensitivity but TASers might want one frame screenwide movement?
								ret.AddAxis(fullName, (-127).RangeTo(127), 0, (input.Flags & AxisFlags.InvertCo) != 0);
								ret.CategoryLabels[fullName] = category;
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
								// I think the core expects to be sent some sort of 16 bit integer, but haven't investigated much
								var minX = systemInfo.PointerOffsetX;
								var maxX = systemInfo.PointerOffsetX + systemInfo.PointerScaleX;
								ret.AddAxis(name, minX.RangeTo(maxX), (minX + maxX) / 2);
								_thunks.Add((c, b) =>
								{
									var val = c.AxisValue(name);
									b[byteStart] = (byte)val;
									b[byteStart + 1] = (byte)(val >> 8);
								});
								break;
							}
							case InputType.PointerY:
							{
								// I think the core expects to be sent some sort of 16 bit integer, but haven't investigated much
								var minY = systemInfo.PointerOffsetY;
								var maxY = systemInfo.PointerOffsetY + systemInfo.PointerScaleY;
								ret.AddAxis(name, minY.RangeTo(maxY), (minY + maxY) / 2);
								_thunks.Add((c, b) =>
								{
									var val = c.AxisValue(name);
									b[byteStart] = (byte)val;
									b[byteStart + 1] = (byte)(val >> 8);
								});
								break;
							}
							case InputType.ButtonAnalog:
							{
								ret.AddAxis(name, 0.RangeTo(0xFFFF), 0);
								ret.CategoryLabels[name] = category;
								_thunks.Add((c, b) =>
								{
									var val = c.AxisValue(name);
									b[byteStart] = (byte)val;
									b[byteStart + 1] = (byte)(val >> 8);
								});
								break;
							}
							case InputType.Status:
								// TODO: wire up statuses to something (not controller, of course)
								break;
							case InputType.Rumble:
								//TODO Does this apply to all Mednafen's systems? (This is for PSX.) Might need to pass more metadata through to here
								var nameLeft = $"{name} Left (strong)";
								var nameRight = $"{name} Right (weak)";
								ret.HapticsChannels.Add(nameLeft);
								ret.HapticsChannels.Add(nameRight);
								// this is a special case, we treat b here as output rather than input
								// so these thunks are called after the frame has advanced
								_rumblers.Add((c, b) =>
								{
									const double SCALE_FACTOR = (double)int.MaxValue / byte.MaxValue;
									static int Scale(byte b)
										=> (b * SCALE_FACTOR).RoundToInt();
									//TODO double-check order
									c.SetHapticChannelStrength(nameRight, Scale(b[byteStart]));
									c.SetHapticChannelStrength(nameLeft, Scale(b[byteStart + 1]));
								});
								break;
							default:
							{
								throw new NotImplementedException($"Unimplemented button type {input.Type}");
							}
						}
					}
					devByteStart += (int)deviceInfo.ByteLength;
					if (devByteStart > MAX_INPUT_DATA)
						throw new NotImplementedException($"More than {MAX_INPUT_DATA} input data bytes");
				}
				ret.BoolButtons.Add("Power");
				ret.BoolButtons.Add("Reset");
				if (systemInfo.GameType == GameMediumTypes.GMT_ARCADE)
				{
					ret.BoolButtons.Add("Insert Coin");
				}
				if (numCds > 0)
				{
					ret.BoolButtons.Add("Open Tray");
					ret.BoolButtons.Add("Close Tray");
					ret.AddAxis("Disk Index", (-1).RangeTo(numCds - 1), 0);
				}
				Definition = ret.MakeImmutable();
				finalDevices.Add(null);
				Devices = finalDevices.ToArray();
				_switchPreviousFrame = switchPreviousFrame.ToArray();
			}

			private readonly byte[] _switchPreviousFrame;

			private readonly List<ControllerThunk> _thunks = new();
			private readonly List<ControllerThunk> _rumblers = new();

			public void SetBits(IController src, byte[] dest)
			{
				Array.Clear(dest, 0, dest.Length);
				foreach (var t in _thunks)
					t(src, dest);
			}

			public void DoRumble(IController dest, byte[] src)
			{
				foreach (var r in _rumblers)
					r(dest, src);
			}

			private const ulong MAGIC = 9569546739673486731;

			public bool AvoidRewind => false;

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

		/// <summary>
		/// On some cores, some controller ports are not relevant when certain settings are off (like multitap).
		/// Override this if your core has such an issue
		/// </summary>
		protected virtual HashSet<string> ComputeHiddenPorts()
		{
			return new HashSet<string>();
		}

		public class PortResult
		{
			/// <summary>
			/// The port, together with all of its potential contents
			/// </summary>
			public NPortInfoT Port { get; set; }
			/// <summary>
			/// What was actually plugged into the port
			/// </summary>
			public NDeviceInfoT Device { get; set; }
		}

		/// <summary>
		/// In a fully initialized core, holds information about what was actually plugged in.  Please do not mutate it.
		/// </summary>
		/// <value></value>
		public List<PortResult> ActualPortData => _controllerAdapter.ActualPortData;
	}
}

using System.Collections.Generic;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.AppleII
{
	public partial class AppleII : IDebuggable
	{
		public IDictionary<string, RegisterValue> GetCpuFlagsAndRegisters()
		{
			var regs = new Dictionary<string, int>
			{
				["A"] = _machine.Cpu.RA,
				["X"] = _machine.Cpu.RX,
				["Y"] = _machine.Cpu.RY,
				["S"] = _machine.Cpu.RS,
				["PC"] = _machine.Cpu.RPC,
				["Flag C"] = _machine.Cpu.FlagC ? 1 : 0,
				["Flag Z"] = _machine.Cpu.FlagZ ? 1 : 0,
				["Flag I"] = _machine.Cpu.FlagI ? 1 : 0,
				["Flag D"] = _machine.Cpu.FlagD ? 1 : 0,
				["Flag B"] = _machine.Cpu.FlagB ? 1 : 0,
				["Flag V"] = _machine.Cpu.FlagV ? 1 : 0,
				["Flag N"] = _machine.Cpu.FlagN ? 1 : 0,
				["Flag T"] = _machine.Cpu.FlagT ? 1 : 0
			};

			var dic = new Dictionary<string, RegisterValue>();

			foreach (var reg in regs)
			{
				dic.Add(
					reg.Key,
					reg.Key.Contains("Flag")
						? reg.Value > 0
						: GetRegisterValue(reg));
			}

			return dic;
		}

		public void SetCpuRegister(string register, int value)
		{
			switch (register)
			{
				default:
					throw new InvalidOperationException();
				case "A":
					_machine.Cpu.RA = (byte)value;
					break;
				case "X":
					_machine.Cpu.RX = (byte)value;
					break;
				case "Y":
					_machine.Cpu.RY = (byte)value;
					break;
				case "S":
					_machine.Cpu.RS = (byte)value;
					break;
				case "PC":
					_machine.Cpu.RPC = (ushort)value;
					break;
				case "Flag C":
					_machine.Cpu.FlagC = value > 0;
					break;
				case "Flag Z":
					_machine.Cpu.FlagZ = value > 0;
					break;
				case "Flag I":
					_machine.Cpu.FlagI = value > 0;
					break;
				case "Flag D":
					_machine.Cpu.FlagD = value > 0;
					break;
				case "Flag B":
					_machine.Cpu.FlagB = value > 0;
					break;
				case "Flag T":
					_machine.Cpu.FlagT = value > 0;
					break;
				case "Flag V":
					_machine.Cpu.FlagV = value > 0;
					break;
				case "Flag N":
					_machine.Cpu.FlagV = value > 0;
					break;
			}
		}

		public IMemoryCallbackSystem MemoryCallbacks { get; } = new MemoryCallbackSystem(new[] { "System Bus" });

		public bool CanStep(StepType type)
		{
			switch (type)
			{
				case StepType.Into:
				case StepType.Over:
				case StepType.Out:
					return true;
				default:
					return false;
			}
		}

		public void Step(StepType type) 
		{
			switch (type)
			{
				case StepType.Into:
					StepInto();
					break;
				case StepType.Out:
					StepOut();
					break;
				case StepType.Over:
					StepOver();
					break;
			}
		}

		public long TotalExecutedCycles => _machine.Cpu.Cycles;

		private RegisterValue GetRegisterValue(KeyValuePair<string, int> reg)
		{
			switch (reg.Key)
			{
				case "A":
				case "X":
				case "Y":
				case "S":
					return (byte)reg.Value;
				case "PC":
					return (ushort)reg.Value;
				default:
					return reg.Value;
			}
		}

		private void StepInto()
		{
			if (_tracer.IsEnabled())
			{
				_machine.Cpu.TraceCallback = TracerWrapper;
			}
			else
			{
				_machine.Cpu.TraceCallback = null;
			}

			var machineInVblank = _machine.Video.IsVBlank;


			_machine.Events.HandleEvents(_machine.Cpu.Execute());

			if (!machineInVblank && _machine.Video.IsVBlank) // Check if a frame has passed while stepping
			{
				Frame++;
				if (_machine.Memory.Lagged)
				{
					LagCount++;
				}

				_machine.Memory.Lagged = true;
				_machine.Memory.DiskIIController.DriveLight = false;
			}
		}

		private void StepOver()
		{
			var instruction = _machine.Memory.Read(_machine.Cpu.RPC);

			if (instruction == Jsr)
			{
				var destination = _machine.Cpu.RPC + JsrSize;
				while (_machine.Cpu.RPC != destination)
				{
					StepInto();
				}
			}
			else
			{
				StepInto();
			}
		}

		private void StepOut()
		{
			var instr = _machine.Memory.Read(_machine.Cpu.RPC);

			_jsrCount = instr == Jsr ? 1 : 0;

			var bailOutFrame = Frame + 1;

			while (true)
			{
				StepInto();
				instr = _machine.Memory.Read(_machine.Cpu.RPC);
				if (instr == Jsr)
				{
					_jsrCount++;
				}
				else if (instr == Rts && _jsrCount <= 0)
				{
					StepInto();
					_jsrCount = 0;
					break;
				}
				else if (instr == Rts)
				{
					_jsrCount--;
				}
				else // Emergency Bailout Logic
				{
					if (Frame == bailOutFrame)
					{
						break;
					}
				}
			}
		}

		private int _jsrCount;

		private const byte Jsr = 0x20;
		private const byte Rts = 0x60;
		private const byte JsrSize = 3;
	}
}

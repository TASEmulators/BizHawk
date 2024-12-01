using System.Collections.Generic;

using BizHawk.Emulation.Common;
using static BizHawk.Emulation.Cores.Nintendo.BSNES.BsnesApi;

namespace BizHawk.Emulation.Cores.Nintendo.BSNES
{
	public partial class BsnesCore : IDebuggable
	{
		public IDictionary<string, RegisterValue> GetCpuFlagsAndRegisters()
		{
			CpuRegisters registers = default;
			Api.core.snes_get_cpu_registers(ref registers);

			var flags = (RegisterFlags) registers.p;

			bool fc = (flags & RegisterFlags.C) != 0;
			bool fz = (flags & RegisterFlags.Z) != 0;
			bool fi = (flags & RegisterFlags.I) != 0;
			bool fd = (flags & RegisterFlags.D) != 0;
			bool fx = (flags & RegisterFlags.X) != 0;
			bool fm = (flags & RegisterFlags.M) != 0;
			bool fv = (flags & RegisterFlags.V) != 0;
			bool fn = (flags & RegisterFlags.N) != 0;

			return new Dictionary<string, RegisterValue>
			{
				["PC"] = registers.pc,
				["A"] = registers.a,
				["X"] = registers.x,
				["Y"] = registers.y,
				["Z"] = registers.z,
				["S"] = registers.s,
				["D"] = registers.d,
				["B"] = registers.b,
				["P"] = registers.p,
				["E"] = registers.e,
				["Flag C"] = fc,
				["Flag Z"] = fz,
				["Flag I"] = fi,
				["Flag D"] = fd,
				["Flag X"] = fx,
				["Flag M"] = fm,
				["Flag V"] = fv,
				["Flag N"] = fn,
				["MDR"] = registers.mdr,
				["V"] = registers.v,
				["H"] = registers.h
			};
		}

		public void SetCpuRegister(string register, int value)
		{
			Api.core.snes_set_cpu_register(register, (uint) value);
		}

		public IMemoryCallbackSystem MemoryCallbacks { get; } = new MemoryCallbackSystem(null);

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
			_framePassed = false;
			switch (type)
			{
				case StepType.Into:
					StepInto();
					break;
				case StepType.Over:
					StepOver();
					break;
				case StepType.Out:
					StepOut();
					break;
				default:
					throw new NotImplementedException();
			}
		}

		public long TotalExecutedCycles => Api.core.snes_get_executed_cycles();

		private void StepInto()
		{
			_framePassed = Api.core.snes_cpu_step();
			if (_framePassed)
			{
				Frame++;
				if (IsLagFrame) LagCount++;
			}
		}

		private void StepOver()
		{
			CpuRegisters registers = default;
			Api.core.snes_get_cpu_registers(ref registers);
			byte opcode = Api.core.snes_bus_read(registers.pc);

			if (IsSubroutineCall(opcode))
			{
				uint destination = registers.pc + JumpInstructionLength(opcode);
				do
				{
					StepInto();
					Api.core.snes_get_cpu_registers(ref registers);
				} while (registers.pc != destination && !_framePassed);
			}
			else
			{
				StepInto();
			}
		}

		private void StepOut()
		{
			CpuRegisters registers = default;
			Api.core.snes_get_cpu_registers(ref registers);
			byte opcode = Api.core.snes_bus_read(registers.pc);

			while (!IsReturn(opcode) && !_framePassed)
			{
				StepOver();
				Api.core.snes_get_cpu_registers(ref registers);
				opcode = Api.core.snes_bus_read(registers.pc);
			}

			if (!_framePassed)
			{
				StepInto();
			}
		}

		private bool _framePassed;
		private static bool IsSubroutineCall(byte opcode) => opcode is 0x20 or 0x22 or 0xfc;
		private static bool IsReturn(byte opcode) => opcode is 0x60 or 0x6b;

		private static uint JumpInstructionLength(byte opcode)
		{
			return opcode switch
			{
				0x20 or 0xfc => 3,
				0x22 => 4,
				_ => throw new InvalidOperationException()
			};
		}
	}
}

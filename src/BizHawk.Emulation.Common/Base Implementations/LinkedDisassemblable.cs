using System;
using System.Collections.Generic;

namespace BizHawk.Emulation.Common
{
	/// <summary>
	/// A generic linked implementation of IDisassemblable that can be used by any link core
	/// </summary>
	/// <seealso cref="IDisassemblable" />
	public class LinkedDisassemblable : VerifiedDisassembler
	{
		private readonly IDisassemblable _baseDisassembler;

		public LinkedDisassemblable(IDisassemblable baseDisassembler, int numCores)
		{
			_baseDisassembler = baseDisassembler;
			string[] cpus = new string[numCores];
			for (int i = 0; i < numCores; i++)
			{
				cpus[i] = $"P{i + 1} " + baseDisassembler.Cpu;
			}
			AvailableCpus = cpus;
		}

		public override IEnumerable<string> AvailableCpus { get; }

		public override string PCRegisterName => Cpu.Substring(0, 2) + " PC";

		public override string Disassemble(MemoryDomain m, uint addr, out int length) => _baseDisassembler.Disassemble(m, addr, out length);
	}
}

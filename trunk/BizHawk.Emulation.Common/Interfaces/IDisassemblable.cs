using System.Collections.Generic;

namespace BizHawk.Emulation.Common
{
	public interface IDisassemblable : IEmulator, IEmulatorService
	{
		/// <summary>
		/// Gets or sets the Cpu that will be used to disassemble
		/// Only values returned from AvailableCpus will be supported when Set
		/// </summary>
		string Cpu { get; set; }

		/// <summary>
		/// Gets a list of Cpus that can be used when setting the Cpu property
		/// </summary>
		IEnumerable<string> AvailableCpus { get; set; }

		/// <summary>
		/// returns a disassembly starting at addr lasting for length, using the given domain
		/// </summary>
		string Disassemble(MemoryDomain m, uint addr, out int length);
	}
}

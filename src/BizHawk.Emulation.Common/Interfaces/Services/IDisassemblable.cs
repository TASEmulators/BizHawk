using System.Linq;
using System.Collections.Generic;

namespace BizHawk.Emulation.Common
{
	/// <summary>
	/// This service provides the means to generate disassembly by the core for a given CPU and memory domain
	/// Tools such the debugger use this, but also LUA scripting, and tools like trace logging and code data logging can make use of this tool
	/// If unavailable the debugger tool will still be available but disable the disassembly window but still be available if the <see cref="IDebuggable"/> service is available
	/// </summary>
	public interface IDisassemblable : IEmulatorService
	{
		/// <summary>
		/// Gets or sets the CPUS that will be used to disassemble
		/// Only values returned from <see cref="AvailableCpus"/> will be supported when Set
		/// </summary>
		string Cpu { get; set; }

		/// <summary>
		/// Gets the name of the Program Counter Register for the current CPU
		/// </summary>
		string PCRegisterName { get; }

		/// <summary>
		/// Gets a list of CPUs that can be used when setting the CPU property
		/// </summary>
		IEnumerable<string> AvailableCpus { get; }

		/// <summary>
		/// Returns a disassembly starting at address lasting for length, using the given domain
		/// </summary>
		string Disassemble(MemoryDomain m, uint addr, out int length);
	}

	/// <summary>
	/// does sanity checking on CPU parameters
	/// </summary>
	public abstract class VerifiedDisassembler : IDisassemblable
	{
		private string? _cpu;

		/// <exception cref="ArgumentException">(from setter) <paramref name="value"/> isn't the name of an available CPU</exception>
		public virtual string Cpu
		{
			get => _cpu ??= AvailableCpus.First();
			set
			{
				if (!AvailableCpus.Contains(value)) throw new ArgumentException(message: $"must be the name of a CPU with disassembly available (see {nameof(AvailableCpus)})", paramName: nameof(value));
				_cpu = value;
			}
		}

		public abstract IEnumerable<string> AvailableCpus { get; }

		public abstract string PCRegisterName { get; }

		public abstract string Disassemble(MemoryDomain m, uint addr, out int length);
	}
}

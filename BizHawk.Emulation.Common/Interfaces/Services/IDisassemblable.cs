using System;
using System.Linq;
using System.Collections.Generic;

namespace BizHawk.Emulation.Common
{
	/// <summary>
	/// This service provides the means to generate disassembly by the core for a given cpu and memory domain
	/// Tools such the debugger use this, but also lua scripting, and tools like trace logging and code data logging can make use of this tool
	/// If unavailable the debugger tool will still be avilable but disable the disassembly window but still be available if the IDebuggable service is available
	/// </summary>
	public interface IDisassemblable : IEmulatorService
	{
		/// <summary>
		/// Gets or sets the Cpu that will be used to disassemble
		/// Only values returned from AvailableCpus will be supported when Set
		/// </summary>
		string Cpu { get; set; }

		/// <summary>
		/// Returns the name of the Program Counter Register for the current Cpu
		/// </summary>
		string PCRegisterName { get; }

		/// <summary>
		/// Gets a list of Cpus that can be used when setting the Cpu property
		/// </summary>
		IEnumerable<string> AvailableCpus { get; }

		/// <summary>
		/// returns a disassembly starting at addr lasting for length, using the given domain
		/// </summary>
		string Disassemble(MemoryDomain m, uint addr, out int length);
	}

	/// <summary>
	/// does santiy checking on Cpu parameters
	/// </summary>
	public abstract class VerifiedDisassembler : IDisassemblable
	{
		protected string _cpu;

		public virtual string Cpu
		{
			get
			{
				return _cpu;
			}
			set
			{
				if (!AvailableCpus.Contains(value))
				{
					throw new ArgumentException();
				}

				_cpu = value;
			}
		}

		public abstract IEnumerable<string> AvailableCpus { get; }

		public abstract string PCRegisterName { get; }

		public abstract string Disassemble(MemoryDomain m, uint addr, out int length);

		public VerifiedDisassembler()
		{
			_cpu = AvailableCpus.First();
		}
	}
}

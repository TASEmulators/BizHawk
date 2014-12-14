using System;
using System.Linq;
using System.Collections.Generic;

namespace BizHawk.Emulation.Common
{
	public interface IDisassemblable : IEmulatorService
	{
		/// <summary>
		/// Gets or sets the Cpu that will be used to disassemble
		/// Only values returned from AvailableCpus will be supported when Set
		/// </summary>
		string Cpu { get; set; }

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
					throw new ArgumentException();
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

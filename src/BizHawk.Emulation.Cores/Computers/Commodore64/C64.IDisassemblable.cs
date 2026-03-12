using System.Collections.Generic;
using System.Linq;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.Commodore64
{
	public partial class C64 : IDisassemblable
	{
		private IDisassemblable _selectedDisassemblable;

		private IEnumerable<IDisassemblable> GetAvailableDisassemblables()
			=> _board.DiskDrive is IDisassemblable dd ? [ _board.Cpu, dd ] : [ _board.Cpu ];

		private void SetDefaultDisassemblable()
		{
			_selectedDisassemblable = GetAvailableDisassemblables().First();
		}

		public string Cpu
		{
			get
			{
				if (_selectedDisassemblable == null)
				{
					SetDefaultDisassemblable();
				}

				return _selectedDisassemblable.Cpu;
			}

			set
			{
				var currentSelectedDisassemblable = _selectedDisassemblable;
				_selectedDisassemblable = GetAvailableDisassemblables().FirstOrDefault(d => d.Cpu == value) ?? currentSelectedDisassemblable;
				if (_selectedDisassemblable is IDebuggable debuggable)
				{
					_selectedDebuggable = debuggable;
				}
			}
		}

		public string PCRegisterName
		{
			get
			{
				if (_selectedDisassemblable == null)
				{
					SetDefaultDisassemblable();
				}

				return _selectedDisassemblable.PCRegisterName;
			}
		}

		public IEnumerable<string> AvailableCpus
		{
			get { return GetAvailableDisassemblables().SelectMany(d => d.AvailableCpus); }
		}

		public string Disassemble(MemoryDomain m, uint addr, out int length)
		{
			if (_selectedDisassemblable == null)
			{
				SetDefaultDisassemblable();
			}

			return _selectedDisassemblable.Disassemble(m, addr, out length);
		}
	}
}

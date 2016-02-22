using System;
using System.Collections.Generic;
using System.Linq;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.Commodore64
{
	public partial class C64 : IDisassemblable
	{
        [SaveState.DoNotSave] private IDisassemblable _selectedDisassemblable;

	    private IEnumerable<IDisassemblable> GetAvailableDisassemblables()
	    {
	        yield return _board.Cpu;
	        if (_board.DiskDrive != null)
	        {
	            yield return _board.DiskDrive;
	        }
	    }

	    private void SetDefaultDisassemblable()
	    {
	        _selectedDisassemblable = GetAvailableDisassemblables().First();
	    }

	    [SaveState.DoNotSave]
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
	            if (_selectedDisassemblable is IDebuggable)
	            {
	                _selectedDebuggable = _selectedDisassemblable as IDebuggable;
	            }
	        }
	    }

        [SaveState.DoNotSave]
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

        [SaveState.DoNotSave]
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

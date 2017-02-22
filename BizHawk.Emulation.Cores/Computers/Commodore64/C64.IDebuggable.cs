using System;
using System.Collections.Generic;
using System.Linq;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.Commodore64
{
	public partial class C64 : IDebuggable
	{
        [SaveState.DoNotSave] private IDebuggable _selectedDebuggable;

        private IEnumerable<IDebuggable> GetAvailableDebuggables()
        {
            yield return _board.Cpu;
            if (_board.DiskDrive != null)
            {
                yield return _board.DiskDrive;
            }
        }

        private void SetDefaultDebuggable()
        {
            _selectedDebuggable = GetAvailableDebuggables().First();
        }

        IDictionary<string, RegisterValue> IDebuggable.GetCpuFlagsAndRegisters()
		{
            if (_selectedDebuggable == null)
            {
                SetDefaultDebuggable();
            }
            return _selectedDebuggable.GetCpuFlagsAndRegisters();
		}

		void IDebuggable.SetCpuRegister(string register, int value)
		{
		    if (_selectedDebuggable == null)
		    {
		        SetDefaultDebuggable();
		    }
		    _selectedDebuggable.SetCpuRegister(register, value);
		}

		bool IDebuggable.CanStep(StepType type)
		{
            if (_selectedDebuggable == null)
            {
                SetDefaultDebuggable();
            }
		    return _selectedDebuggable.CanStep(type);
		}


        void IDebuggable.Step(StepType type)
		{
            if (_selectedDebuggable == null)
            {
                SetDefaultDebuggable();
            }
            _selectedDebuggable.Step(type);
		}

		[FeatureNotImplemented]
		public int TotalExecutedCycles
		{
			get { return _selectedDebuggable.TotalExecutedCycles; }
		}

        [SaveState.DoNotSave]
        private readonly IMemoryCallbackSystem _memoryCallbacks;

        [SaveState.DoNotSave]
        IMemoryCallbackSystem IDebuggable.MemoryCallbacks { get { return _memoryCallbacks; } }
	}
}

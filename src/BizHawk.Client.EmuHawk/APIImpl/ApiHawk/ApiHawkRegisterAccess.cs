#nullable enable

using System;

using BizHawk.API.ApiHawk;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk.APIImpl.ApiHawk
{
	public class ApiHawkRegisterAccess : IRegisterAccess
	{
		private readonly CommonServicesAPIEnvironment _env;

		public ApiHawkRegisterAccess(CommonServicesAPIEnvironment env) => _env = env;

		public ulong? this[string register]
		{
			get
			{
				try
				{
					if (_env.DebuggableCore == null) throw new NotImplementedException();
					return _env.DebuggableCore.GetCpuFlagsAndRegisters().TryGetValue(register, out var rv) ? rv.Value : (ulong?) null;
				}
				catch (NotImplementedException)
				{
					_env.LogCallback($"Error: {_env.EmuCore.Attributes().CoreName} does not yet implement {nameof(IDebuggable.GetCpuFlagsAndRegisters)}()");
					return null;
				}
			}
			set
			{
				try
				{
					if (_env.DebuggableCore == null) throw new NotImplementedException();
					_env.DebuggableCore.SetCpuRegister(register, (int) (value ?? throw new NullReferenceException()));
				}
				catch (NotImplementedException)
				{
					_env.LogCallback($"Error: {_env.EmuCore.Attributes().CoreName} does not yet implement {nameof(IDebuggable.SetCpuRegister)}()");
				}
			}
		}
	}
}

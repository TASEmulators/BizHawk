using System;

namespace BizHawk.Common
{
	public interface IInstanceDll : IDisposable
	{
		IntPtr GetProcAddress(string procName);
		void Dispose();
	}
}


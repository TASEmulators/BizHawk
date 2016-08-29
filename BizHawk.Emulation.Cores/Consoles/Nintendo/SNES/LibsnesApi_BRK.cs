using System;

using BizHawk.Common;
using System.Runtime.InteropServices;

namespace BizHawk.Emulation.Cores.Nintendo.SNES
{
	unsafe partial class LibsnesApi
	{
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		delegate void snesScanlineStart_t(int line);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		delegate void snesHook_t(uint addr);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		delegate void snesHookWrite_t(uint addr, byte value);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		delegate void SetSnesScanlineStart_t(snesScanlineStart_t f);
		SetSnesScanlineStart_t SetSnesScanlineStart;

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		delegate void SetSnesHookExec_t(snesHook_t f);
		SetSnesHookExec_t SetSnesHookExec;

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		delegate void SetSnesHookWrite_t(snesHookWrite_t f);
		SetSnesHookWrite_t SetSnesHookWrite;

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		delegate void SetSnesHookRead_t(snesHook_t f);
		SetSnesHookRead_t SetSnesHookRead;

		void snesScanlineStart(int line)
		{
			scanlineStart?.Invoke(line);
		}
		void snesHookExec(uint addr)
		{
			ExecHook(addr);
		}
		void snesHookRead(uint addr)
		{
			ReadHook(addr);
		}
		void snesHookWrite(uint addr, byte value)
		{
			WriteHook(addr, value);
		}

		void InitBrkFunctions()
		{
			instanceDll.Retrieve(out SetSnesScanlineStart, "SetSnesScanlineStart");
			instanceDll.Retrieve(out SetSnesHookExec, "SetSnesHookExec");
			instanceDll.Retrieve(out SetSnesHookWrite, "SetSnesHookWrite");
			instanceDll.Retrieve(out SetSnesHookRead, "SetSnesHookRead");
			SetSnesScanlineStart(Keep<snesScanlineStart_t>(snesScanlineStart));
			SetSnesHookExec(Keep<snesHook_t>(snesHookExec));
			SetSnesHookWrite(Keep<snesHookWrite_t>(snesHookWrite));
			SetSnesHookRead(Keep<snesHook_t>(snesHookRead));
		}
	}
}
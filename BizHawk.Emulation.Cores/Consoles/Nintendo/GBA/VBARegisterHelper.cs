using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.GBA
{
	public unsafe class VBARegisterHelper
	{
		IntPtr _origin;
		Dictionary<string, IntPtr> _locs = new Dictionary<string, IntPtr>();

		public VBARegisterHelper(IntPtr Core)
		{
			_origin = LibVBANext.GetRegisters(Core);
			foreach (var field in typeof(LibVBANext.Registers).GetFields())
			{
				var ofs = Marshal.OffsetOf(typeof(LibVBANext.Registers), field.Name);
				_locs[field.Name] = IntPtr.Add(_origin, (int)ofs);
			}
		}

		public int GetRegister(string name)
		{
			int* p = (int*)_locs[name];
			return *p;
		}
		public void SetRegister(string name, int val)
		{
			int* p = (int*)_locs[name];
			*p = val;
		}
		public Dictionary<string, RegisterValue> GetAllRegisters()
		{
			var ret = new Dictionary<string, RegisterValue>();
			foreach (var kvp in _locs)
			{
				ret[kvp.Key] = GetRegister(kvp.Key);
			}
			return ret;
		}

		public string TraceString()
		{
			var sb = new StringBuilder();
			int* p = (int*)_origin;
			for (int i = 0; i < 17; i++)
			{
				sb.Append(string.Format("r{0}:{1:X8}", i, p[i]));
				if (i != 16)
					sb.Append(' ');
			}
			return sb.ToString();
		}
	}
}

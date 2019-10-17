using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.NDS
{
	partial class MelonDS : IInputPollable
	{
		public int LagCount { get => GetLagFrameCount(); set => throw new NotImplementedException(); }
		public bool IsLagFrame { get => _IsLagFrame(); set => throw new NotImplementedException(); }

		public IInputCallbackSystem InputCallbacks => throw new NotImplementedException();

		[DllImport(dllPath, EntryPoint = "IsLagFrame")]
		private static extern bool _IsLagFrame();

		[DllImport(dllPath)]
		private static extern int GetLagFrameCount();
	}
}

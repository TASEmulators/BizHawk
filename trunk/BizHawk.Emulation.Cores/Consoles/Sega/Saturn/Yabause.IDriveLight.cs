using System;
using System.Collections.Generic;
using System.Linq;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Sega.Saturn
{
	public partial class Yabause : IDriveLight
	{
		public bool DriveLightEnabled { get; private set; }
		public bool DriveLightOn { get; private set; }
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Common
{
	public interface IMonitor
	{
		void Enter();
		void Exit();
	}
}

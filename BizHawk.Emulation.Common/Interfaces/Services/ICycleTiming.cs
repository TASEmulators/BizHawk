using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BizHawk.Emulation.Common
{
	public interface ICycleTiming
	{
		/// <summary>
		/// Total elapsed emulation time relative to <see cref="ClockRate"/>
		/// </summary>
		long CycleCount { get; }
		/// <summary>
		/// Clock Rate in hz for <see cref="CycleCount"/>
		/// </summary>
		double ClockRate { get; }
	}
}

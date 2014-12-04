using System;
using System.Collections.Generic;

namespace BizHawk.Emulation.Common
{
	// TODO: This isn't a CoreService, it is a sub class of a core service, it would be nice to make that clear
	public interface IInputCallbackSystem : IList<Action>
	{
		/// <summary>
		/// Will iterate and call every callback
		/// </summary>
		void Call();

		/// <summary>
		/// Will remove the given list of callbacks
		/// </summary>
		void RemoveAll(IEnumerable<Action> actions);
	}
}

using System;
using System.Collections.Generic;

namespace BizHawk.Emulation.Common
{
	public class InputCallbackSystem : List<Action>, IInputCallbackSystem
	{
		public void Call()
		{
			foreach (var action in this)
			{
				action();
			}
		}

		public void RemoveAll(IEnumerable<Action> actions)
		{
			foreach (var action in actions)
			{
				this.Remove(action);
			}
		}
	}
}

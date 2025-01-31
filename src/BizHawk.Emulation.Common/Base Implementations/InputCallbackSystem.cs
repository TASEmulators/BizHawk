using System.Collections.Generic;

namespace BizHawk.Emulation.Common
{
	/// <summary>
	/// This is a generic implementation of IInputCallbackSystem that can be used
	/// by any core
	/// </summary>
	/// <seealso cref="IInputCallbackSystem" />
	public class InputCallbackSystem : List<Action>, IInputCallbackSystem
	{
		public void Call()
		{
			foreach (var action in this)
			{
				action();
			}
		}

		// TODO: these just happen to be all the add/remove methods the client uses, to be thorough the others should be overriden as well
		public void RemoveAll(IEnumerable<Action> actions)
		{
			var hadAny = Count is not 0;
			foreach (var action in actions)
			{
				Remove(action);
			}
			var hasAny = Count is not 0;
			Changes(hadAny, hasAny);
		}

		public new void Add(Action item)
		{
			var hadAny = Count is not 0;
			base.Add(item);
			var hasAny = Count is not 0;
			Changes(hadAny, hasAny);
		}

		public new bool Remove(Action item)
		{
			var hadAny = Count is not 0;
			var result = base.Remove(item);
			var hasAny = Count is not 0;
			Changes(hadAny, hasAny);

			return result;
		}

		public delegate void ActiveChangedEventHandler();
		public event ActiveChangedEventHandler? ActiveChanged;

		private void Changes(bool hadAny, bool hasAny)
		{
			if ((hadAny && !hasAny) || (!hadAny && hasAny))
			{
				ActiveChanged?.Invoke();
			}
		}
	}
}

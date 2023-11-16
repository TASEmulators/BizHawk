using System;
using System.Diagnostics;
using System.Collections.Generic;

namespace NLua.Method
{
	/// <summary>
	/// We keep track of what delegates we have auto attached to an event - to allow us to cleanly exit a NLua session
	/// </summary>
	internal class EventHandlerContainer : IDisposable
	{
		private readonly Dictionary<Delegate, RegisterEventHandler> _dict = new();

		public void Add(Delegate handler, RegisterEventHandler eventInfo)
			=> _dict.Add(handler, eventInfo);

		public void Remove(Delegate handler)
		{
			var found = _dict.Remove(handler);
			Debug.Assert(found);
		}

		/// <summary>
		/// Remove any still registered handlers
		/// </summary>
		public void Dispose()
		{
			foreach (var pair in _dict)
			{
				pair.Value.RemovePending(pair.Key);
			}

			_dict.Clear();
		}
	}
}
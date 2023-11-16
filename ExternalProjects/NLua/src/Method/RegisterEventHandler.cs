using System;
using System.Reflection;

using NLua.GenerateEventAssembly;

namespace NLua.Method
{
	internal class RegisterEventHandler
	{
		private readonly EventHandlerContainer _pendingEvents;
		private readonly EventInfo _eventInfo;
		private readonly object _target;

		public RegisterEventHandler(EventHandlerContainer pendingEvents, object target, EventInfo eventInfo)
		{
			_target = target;
			_eventInfo = eventInfo;
			_pendingEvents = pendingEvents;
		}

		/// <summary>
		/// Adds a new event handler, called from Lua
		/// </summary>
		// ReSharper disable once UnusedMember.Global
		public Delegate Add(LuaFunction function)
		{
			var handlerDelegate = CodeGeneration.Instance.GetDelegate(_eventInfo.EventHandlerType, function);
			return Add(handlerDelegate);
		}

		public Delegate Add(Delegate handlerDelegate)
		{
			_eventInfo.AddEventHandler(_target, handlerDelegate);
			_pendingEvents.Add(handlerDelegate, this);
			return handlerDelegate;
		}

		/// <summary>
		/// Removes an existing event handler, called from Lua
		/// </summary>
		// ReSharper disable once UnusedMember.Global
		public void Remove(Delegate handlerDelegate)
		{
			RemovePending(handlerDelegate);
			_pendingEvents.Remove(handlerDelegate);
		}

		/// <summary>
		/// Removes an existing event handler (without updating the pending handlers list)
		/// </summary>
		internal void RemovePending(Delegate handlerDelegate)
			=> _eventInfo.RemoveEventHandler(_target, handlerDelegate);
	}
}
using System;
using System.Collections.Generic;
using System.Linq;

namespace Jellyfish.Virtu
{
	public enum EventCallbacks
	{
		FlushOutput,
		FlushRow,
		LeaveVBlank,
		ResetVsync,
		InverseText
	}

	internal sealed class MachineEvent
	{
		public MachineEvent(int delta, EventCallbacks type)
		{
			Delta = delta;
			Type = type;
		}

		public int Delta { get; set; }
		public EventCallbacks Type { get; set; }
	}

	public sealed class MachineEvents
	{
		private Dictionary<EventCallbacks, Action> _eventDelegates = new Dictionary<EventCallbacks, Action>();

		// ReSharper disable once FieldCanBeMadeReadOnly.Local
		private LinkedList<MachineEvent> _used = new LinkedList<MachineEvent>();

		// ReSharper disable once FieldCanBeMadeReadOnly.Local
		private LinkedList<MachineEvent> _free = new LinkedList<MachineEvent>();

		public void Sync(IComponentSerializer ser)
		{
			if (ser.IsReader)
			{
				int[] usedDelta = new int[0];
				int[] usedType = new int[0];
				int[] freeDelta = new int[0];
				int[] freeType = new int[0];

				ser.Sync("UsedDelta", ref usedDelta, false);
				ser.Sync("UsedType", ref usedType, false);
				ser.Sync("FreeDelta", ref freeDelta, false);
				ser.Sync("FreeType", ref freeType, false);

				_used.Clear();
				for (int i = 0; i < usedDelta.Length; i++)
				{
					var e = new MachineEvent(usedDelta[i], (EventCallbacks)usedType[i]);
					_used.AddLast(new LinkedListNode<MachineEvent>(e));
				}

				_free.Clear();
				for (int i = 0; i < freeDelta.Length; i++)
				{
					var e = new MachineEvent(freeDelta[i], (EventCallbacks)freeType[i]);
					_free.AddLast(new LinkedListNode<MachineEvent>(e));
				}
			}
			else
			{
				var usedDelta = _used.Select(u => u.Delta).ToArray();
				var usedType = _used.Select(u => (int)u.Type).ToArray();
				var freeDelta = _free.Select(f => f.Delta).ToArray();
				var freeType = _free.Select(f => (int)f.Type).ToArray();

				ser.Sync("UsedDelta", ref usedDelta, false);
				ser.Sync("UsedType", ref usedType, false);
				ser.Sync("FreeDelta", ref freeDelta, false);
				ser.Sync("FreeType", ref freeType, false);
			}
		}

		public void AddEventDelegate(EventCallbacks type, Action action)

		{
			_eventDelegates[type] = action;
		}

		public void AddEvent(int delta, EventCallbacks type)
		{
			var node = _used.First;
			for (; node != null; node = node.Next)
			{
				if (delta < node.Value.Delta)
				{
					node.Value.Delta -= delta;
					break;
				}
				if (node.Value.Delta > 0)
				{
					delta -= node.Value.Delta;
				}
			}

			var newNode = _free.First;
			if (newNode != null)
			{
				_free.RemoveFirst();
				newNode.Value.Delta = delta;
				newNode.Value.Type = type;
			}
			else
			{
				newNode = new LinkedListNode<MachineEvent>(new MachineEvent(delta, type));
			}

			if (node != null)
			{
				_used.AddBefore(node, newNode);
			}
			else
			{
				_used.AddLast(newNode);
			}
		}

		public int FindEvent(EventCallbacks type)
		{
			int delta = 0;

			for (var node = _used.First; node != null; node = node.Next)
			{
				delta += node.Value.Delta;

				var other = node.Value.Type;

				if (other == type)
				{
					return delta;
				}
			}

			return 0;
		}

		// ReSharper disable once UnusedMember.Global
		public void HandleEvents(int delta)
		{
			var node = _used.First;
			node.Value.Delta -= delta;

			while (node.Value.Delta <= 0)
			{
				_eventDelegates[node.Value.Type]();
				RemoveEvent(node);
				node = _used.First;
			}
		}

		private void RemoveEvent(LinkedListNode<MachineEvent> node)
		{
			if (node.Next != null)
			{
				node.Next.Value.Delta += node.Value.Delta;
			}

			_used.Remove(node);
			_free.AddFirst(node); // cache node; avoids garbage
		}
	}
}

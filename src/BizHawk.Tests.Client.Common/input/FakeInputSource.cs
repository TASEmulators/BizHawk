using System.Collections.Generic;

using BizHawk.Client.Common;

namespace BizHawk.Tests.Client.Common.input
{
	internal class FakeInputSource : IPhysicalInputSource
	{
		private List<InputEvent> _events = new();
		private int _nextEventId;
		public InputEvent? DequeueEvent() => _nextEventId < _events.Count ? _events[_nextEventId++] : null;

		private KeyValuePair<string, int>[] _axisValues = [ ];
		public KeyValuePair<string, int>[] GetAxisValues() => _axisValues;

		public void AddInputEvent(InputEvent ie) => _events.Add(ie);
	}
}

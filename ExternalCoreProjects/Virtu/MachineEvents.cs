using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Jellyfish.Virtu
{
    public sealed class MachineEvent
    {
        public MachineEvent(int delta, Action action)
        {
            Delta = delta;
            Action = action;
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "Delta = {0} Action = {{{1}.{2}}}", Delta, Action.Method.DeclaringType.Name, Action.Method.Name);
        }

        public int Delta { get; set; }
        public Action Action { get; set; }
    }

    public sealed class MachineEvents
    {
        public void AddEvent(int delta, Action action)
        {
			//Console.WriteLine("+{0} @ {1}", action.Method.Name, delta);

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
                newNode.Value.Action = action;
            }
            else
            {
                newNode = new LinkedListNode<MachineEvent>(new MachineEvent(delta, action));
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

        public int FindEvent(Action action)
        {
            int delta = 0;

            for (var node = _used.First; node != null; node = node.Next)
            {
                delta += node.Value.Delta;

				var other = node.Value.Action;

				if (other.Method == action.Method && other.Target == action.Target) 
				{
					//Console.WriteLine("={0} @ {1}", action.Method.Name, delta);
					return delta;
				}

				// our delegate serializer doesn't preserve reference equality
                //if (object.ReferenceEquals(node.Value.Action, action)) // assumes delegate cached
                //{
                //    return delta;
                //}
            }

			//Console.WriteLine("=???? @ 0");
			return 0;
        }

        public void HandleEvents(int delta)
        {
			//Console.WriteLine("[{0}]", delta);

            var node = _used.First;
            node.Value.Delta -= delta;

            while (node.Value.Delta <= 0)
            {
				//Console.WriteLine("!{0} @ {1}", node.Value.Action.Method.Name, node.Value.Delta);
				node.Value.Action();
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

        private LinkedList<MachineEvent> _used = new LinkedList<MachineEvent>();
        private LinkedList<MachineEvent> _free = new LinkedList<MachineEvent>();
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using Newtonsoft.Json;

namespace BizHawk.Emulation.Cores.Nintendo.Gameboy
{
	public class TextState
	{
		public class Node
		{
			public Dictionary<string, byte[]> Data = new Dictionary<string, byte[]>();
			public Dictionary<string, Node> Objects = new Dictionary<string, Node>();
		}

		public Node Root = new Node();

		[JsonIgnore]
		Stack<Node> Nodes;
		[JsonIgnore]
		Node Current { get { return Nodes.Peek(); } }

		public void Prepare()
		{
			Nodes = new Stack<Node>();
			Nodes.Push(Root);
		}

		public void Save(IntPtr data, int length, string name)
		{
			byte[] d = new byte[length];
			Marshal.Copy(data, d, 0, length);
			Current.Data.Add(name, d);
		}
		public void Load(IntPtr data, int length, string name)
		{
			byte[] d = Current.Data[name];
			Marshal.Copy(d, 0, data, length);
		}
		public void EnterSection(string name)
		{
			Node next = null;
			Current.Objects.TryGetValue(name, out next);
			if (next == null)
			{
				next = new Node();
				Current.Objects.Add(name, next);
			}
			Nodes.Push(next);
		}
		public void ExitSection(string name)
		{
			Node last = Nodes.Pop();
			if (Current.Objects[name] != last)
				throw new InvalidOperationException();
		}

		// other data besides the core
		public int Frame;
		public int LagCount;
		public bool IsLagFrame;
		public ulong _cycleCount;
		public uint frameOverflow;

	}
}

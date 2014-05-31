using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using Newtonsoft.Json;

namespace BizHawk.Common
{
	// managed counterpart to unmanaged serialization code in GB and WSWAN cores

	// T is a class that has any other data you want to serialize in it
	public class TextState<T>
		where T: new()
	{
		public TextState()
		{
			ExtraData = new T();
		}

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
		public T ExtraData;

		public TextStateFPtrs GetFunctionPointers()
		{
			return new TextStateFPtrs
			{
				Save = new TextStateFPtrs.DataFunction(Save),
				Load = new TextStateFPtrs.DataFunction(Load),
				EnterSection = new TextStateFPtrs.SectionFunction(EnterSection),
				ExitSection = new TextStateFPtrs.SectionFunction(ExitSection)
			};
		}
	}
	[StructLayout(LayoutKind.Sequential)]
	public struct TextStateFPtrs
	{
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void DataFunction(IntPtr data, int length, string name);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void SectionFunction(string name);

		public DataFunction Save;
		public DataFunction Load;
		public SectionFunction EnterSection;
		public SectionFunction ExitSection;
	}


}

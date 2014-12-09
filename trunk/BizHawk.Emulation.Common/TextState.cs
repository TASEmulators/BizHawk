using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using Newtonsoft.Json;

namespace BizHawk.Emulation.Common
{
	// managed counterpart to unmanaged serialization code in GB and WSWAN cores

	// T is a class that has any other data you want to serialize in it
	public class TextState<T>
		where T : new()
	{
		public TextState()
		{
			ExtraData = new T();
		}

		public class Node
		{
			public Dictionary<string, byte[]> Data = new Dictionary<string, byte[]>();
			public Dictionary<string, Node> Objects = new Dictionary<string, Node>();

			// methods named "ShouldSerialize*" are detected and dynamically invoked by JSON.NET
			// if they return false during serialization, the field/prop is omitted from the created json
			public bool ShouldSerializeData()
			{
				return Data.Count > 0;
			}
			public bool ShouldSerializeObjects()
			{
				return Objects.Count > 0;
			}
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
			Current.Data.Add(name, d); // will except for us if the key is already present
		}
		public void Load(IntPtr data, int length, string name)
		{
			byte[] d = Current.Data[name];
			if (length != d.Length)
				throw new InvalidOperationException();
			Marshal.Copy(d, 0, data, length);
		}
		public void EnterSectionSave(string name)
		{
			Node next = new Node();
			Current.Objects.Add(name, next);
			Nodes.Push(next);
		}
		public void EnterSectionLoad(string name)
		{
			Node next = Current.Objects[name];
			Nodes.Push(next);
		}
		public void EnterSection(string name)
		{
			// works for either save or load, but as a consequence cannot report intelligent
			// errors about section name mismatches
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

		public TextStateFPtrs GetFunctionPointersSave()
		{
			return new TextStateFPtrs
			{
				Save = new TextStateFPtrs.DataFunction(Save),
				Load = null,
				EnterSection = new TextStateFPtrs.SectionFunction(EnterSectionSave),
				ExitSection = new TextStateFPtrs.SectionFunction(ExitSection)
			};
		}
		public TextStateFPtrs GetFunctionPointersLoad()
		{
			return new TextStateFPtrs
			{
				Save = null,
				Load = new TextStateFPtrs.DataFunction(Load),
				EnterSection = new TextStateFPtrs.SectionFunction(EnterSectionLoad),
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

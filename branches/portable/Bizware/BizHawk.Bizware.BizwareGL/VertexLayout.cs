using System;
using System.Collections.Generic;

using OpenTK.Graphics.OpenGL;

namespace BizHawk.Bizware.BizwareGL
{
	//TEMP until its in bizhawk main
	public class WorkingDictionary<K, V> : Dictionary<K, V> where V : new()
	{
		public new V this[K key]
		{
			get
			{
				V temp;
				if (!TryGetValue(key, out temp))
				{
					temp = this[key] = new V();
				}

				return temp;
			}

			set
			{
				base[key] = value;
			}
		}

		public WorkingDictionary() { }

	}

	public class VertexLayout : IDisposable
	{
		//TODO - could refactor to use vertex array objects? check opengl profile requirements (answer: 3.0. dont want to do this.)

		public VertexLayout(IGL owner, IntPtr id)
		{
			Owner = owner;
			Id = id;
			Items = new MyDictionary();
		}

		public void Dispose()
		{
			//nothing to do yet..
		}

		public void DefineVertexAttribute(string name, int index, int components, VertexAttribPointerType attribType, bool normalized, int stride, int offset = 0)
		{
			if (Closed)
				throw new InvalidOperationException("Type is Closed and is now immutable.");
			Items[index] = new LayoutItem { Name = name, Components = components, AttribType = attribType, Normalized = normalized, Stride = stride, Offset = offset };
		}

		/// <summary>
		/// finishes this VertexLayout and renders it immutable
		/// </summary>
		public void Close()
		{
			Closed = true;
		}

		public class LayoutItem
		{
			public string Name { get; internal set; }
			public int Components { get; internal set; }
			public VertexAttribPointerType AttribType { get; internal set; }
			public bool Normalized { get; internal set; }
			public int Stride { get; internal set; }
			public int Offset { get; internal set; }
		}

		public class MyDictionary : WorkingDictionary<int, LayoutItem>
		{
			public new LayoutItem this[int key]
			{
				get
				{
					return base[key];
				}
				set
				{
					base[key] = value;
				}
			}
		}

		public MyDictionary Items { get; private set; }
		bool Closed = false;

		public IGL Owner { get; private set; }
		public IntPtr Id { get; private set; }
	}
}
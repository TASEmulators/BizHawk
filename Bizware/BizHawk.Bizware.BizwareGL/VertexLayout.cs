using System;
using System.Collections.Generic;

using BizHawk.Common;

using OpenTK.Graphics.OpenGL;

namespace BizHawk.Bizware.BizwareGL
{
	/// <summary>
	/// Represents a vertex layout, really a kind of a peer of the vertex and fragment shaders.
	///	It isnt IDisposable because itll be lifecycle-managed by the IGL (disposed when all dependent pipelines are disposed)
	/// But if you want to be sure to save it for later, use AddRef
	/// </summary>
	public class VertexLayout
	{
		//TODO - could refactor to use vertex array objects? check opengl profile requirements (answer: 3.0. dont want to do this.)

		public VertexLayout(IGL owner, object opaque)
		{
			Owner = owner;
			Opaque = opaque;
			Items = new MyDictionary();
		}

		public object Opaque { get; private set; }
		public IGL Owner { get; private set; }

		int RefCount;

		public void Release()
		{
			RefCount--;
			if (RefCount <= 0)
			{
				//nothing like this yet
				//Available = false;
			}
		}

		public void AddRef()
		{
			RefCount++;
		}

		public void DefineVertexAttribute(string name, int index, int components, VertexAttribPointerType attribType, AttributeUsage usage, bool normalized, int stride, int offset = 0)
		{
			if (Closed)
				throw new InvalidOperationException("Type is Closed and is now immutable.");
			Items[index] = new LayoutItem { Name = name, Components = components, AttribType = attribType, Usage = usage, Normalized = normalized, Stride = stride, Offset = offset };
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
			public AttributeUsage Usage { get; internal set; }
		}

		public class MyDictionary : WorkingDictionary<int, LayoutItem>
		{
			public new LayoutItem this[int key]
			{
				get
				{
					return base[key];
				}

				internal set
				{
					base[key] = value;
				}
			}
		}

		public MyDictionary Items { get; private set; }
		bool Closed = false;

	}
}
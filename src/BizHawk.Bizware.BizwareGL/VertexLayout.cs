using System;
using BizHawk.Common;

using OpenTK.Graphics.OpenGL;

namespace BizHawk.Bizware.BizwareGL
{
	/// <summary>
	/// Represents a vertex layout, really a kind of a peer of the vertex and fragment shaders.
	///	It isn't IDisposable because it'll be lifecycle-managed by the IGL (disposed when all dependent pipelines are disposed)
	/// But if you want to be sure to save it for later, use AddRef
	/// </summary>
	public class VertexLayout
	{
		//TODO - could refactor to use vertex array objects? check opengl profile requirements (answer: 3.0. don't want to do this.)

		public VertexLayout(IGL owner, object opaque)
		{
			Owner = owner;
			Opaque = opaque;
			Items = new MyDictionary();
		}

		public object Opaque { get; }
		public IGL Owner { get; }

		private int RefCount;

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

		/// <exception cref="InvalidOperationException">already closed (by call to <see cref="Close"/>)</exception>
		public void DefineVertexAttribute(string name, int index, int components, VertexAttribPointerType attribType, AttribUsage usage, bool normalized, int stride, int offset = 0)
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
			public AttribUsage Usage { get; internal set; }
		}

		public class MyDictionary : WorkingDictionary<int, LayoutItem>
		{
			public new LayoutItem this[int key]
			{
				get => base[key];
				internal set => base[key] = value;
			}
		}

		public MyDictionary Items { get; }
		private bool Closed = false;

	}
}
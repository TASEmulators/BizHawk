using System;

using BizHawk.Common;

namespace BizHawk.Bizware.Graphics
{
	/// <summary>
	/// Represents a vertex layout, really a kind of a peer of the vertex and fragment shaders.
	/// Only can be held by 1 pipeline at a time
	/// </summary>
	public class VertexLayout : IDisposable
	{
		public VertexLayout(IGL owner, object opaque)
		{
			Owner = owner;
			Opaque = opaque;
			Items = new();
		}

		public object Opaque { get; }
		public IGL Owner { get; }

		public void Dispose()
		{
			Owner.Internal_FreeVertexLayout(this);
		}

		/// <exception cref="InvalidOperationException">already closed (by call to <see cref="Close"/>)</exception>
		public void DefineVertexAttribute(string name, int index, int components, VertexAttribPointerType attribType, AttribUsage usage, bool normalized, int stride, int offset = 0)
		{
			if (Closed)
			{
				throw new InvalidOperationException("Type is Closed and is now immutable.");
			}

			Items[index] = new() { Name = name, Components = components, AttribType = attribType, Usage = usage, Normalized = normalized, Stride = stride, Offset = offset };
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

		public class LayoutItemWorkingDictionary : WorkingDictionary<int, LayoutItem>
		{
			public new LayoutItem this[int key]
			{
				get => base[key];
				internal set => base[key] = value;
			}
		}

		public LayoutItemWorkingDictionary Items { get; }
		private bool Closed;
	}
}
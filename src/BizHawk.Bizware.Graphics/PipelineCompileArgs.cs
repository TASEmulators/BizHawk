using System;
using System.Collections.Generic;

namespace BizHawk.Bizware.Graphics
{
	/// <summary>
	/// Defines arguments for compiling a pipeline
	/// </summary>
	public class PipelineCompileArgs
	{
		/// <summary>
		/// Defines an item within a vertex layout
		/// This is currently restricted to float types
		/// </summary>
		/// <param name="Components">Number of components in the item (e.g. float2 has 2 components). Only 1-4 components is valid</param>
		/// <param name="Offset">Byte offset within the vertex buffer to the item</param>
		/// <param name="Usage">Semantic usage</param>
		public readonly record struct VertexLayoutItem(string Name, int Components, int Offset, AttribUsage Usage);

		/// <summary>
		/// Defines arguments for compiling a shader
		/// This currently assumes the native shader format
		/// </summary>
		/// <param name="Source">Source code for the shader</param>
		/// <param name="Entry">Entrypoint for this shader</param>
		public readonly record struct ShaderCompileArgs(string Source, string Entry);

		internal readonly IReadOnlyList<VertexLayoutItem> VertexLayout;
		internal readonly int VertexLayoutStride;
		internal readonly ShaderCompileArgs VertexShaderArgs;
		internal readonly ShaderCompileArgs FragmentShaderArgs;
		internal readonly string FragmentOutputName;

		public PipelineCompileArgs(IReadOnlyList<VertexLayoutItem> vertexLayoutItems,
			ShaderCompileArgs vertexShaderArgs, ShaderCompileArgs fragmentShaderArgs,
			string fragmentOutputName)
		{
			VertexLayout = vertexLayoutItems;

			foreach (var item in VertexLayout)
			{
				if (item.Components is < 1 or > 4)
				{
					throw new InvalidOperationException("A vertex layout item must have 1-4 components");
				}

				VertexLayoutStride += item.Components * 4;
			}

			VertexShaderArgs = vertexShaderArgs;
			FragmentShaderArgs = fragmentShaderArgs;
			FragmentOutputName = fragmentOutputName;
		}
	}
}

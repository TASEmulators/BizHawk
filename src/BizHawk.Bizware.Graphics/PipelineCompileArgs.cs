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
		/// This is currently restricted to to either 4 byte floats, or a 32 bit unsigned normalized integer type
		/// If this is a 32 bit unsigned normalized type, it must have 4 components
		/// </summary>
		/// <param name="Name">Name of the item</param>
		/// <param name="Components">Number of components in the item (e.g. float2 has 2 components). Only 1-4 components is valid (unless Integer is true, then only 4 is valid)</param>
		/// <param name="Offset">Byte offset within the vertex buffer to the item</param>
		/// <param name="Usage">Semantic usage</param>
		/// <param name="Integer">Indicates if this uses an integer rather than a float</param>
		public readonly record struct VertexLayoutItem(string Name, int Components, int Offset, AttribUsage Usage, bool Integer = false);

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

				if (item.Integer && item.Components != 4)
				{
					throw new InvalidOperationException("A vertex layout integer item must have 4 components");
				}

				VertexLayoutStride += item.Integer ? 4 : item.Components * 4;
			}

			VertexShaderArgs = vertexShaderArgs;
			FragmentShaderArgs = fragmentShaderArgs;
			FragmentOutputName = fragmentOutputName;
		}
	}
}

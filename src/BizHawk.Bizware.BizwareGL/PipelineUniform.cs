using System;
using System.Collections.Generic;
using System.Numerics;

namespace BizHawk.Bizware.BizwareGL
{
	/// <summary>
	/// represents a pipeline uniform...
	/// and one of those can represent multiple shader uniforms!!
	/// </summary>
	public class PipelineUniform
	{
		internal PipelineUniform(Pipeline owner)
		{
			Owner = owner;
		}

		public UniformInfo UniformInfo { get; init; }

		public Pipeline Owner { get; }
		
		public void Set(Matrix4x4 mat, bool transpose = false)
		{
			Owner?.Owner.SetPipelineUniformMatrix(this, mat, transpose);
		}

		public void Set(Vector4 vec)
		{
			Owner?.Owner.SetPipelineUniform(this, vec);
		}

		public void Set(Vector2 vec)
		{
			Owner?.Owner.SetPipelineUniform(this, vec);
		}

		public void Set(float f)
		{
			Owner?.Owner.SetPipelineUniform(this, f);
		}

		public void Set(Vector4[] vecs)
		{
			Owner?.Owner.SetPipelineUniform(this, vecs);
		}

		public void Set(ref Matrix4x4 mat, bool transpose = false)
		{
			Owner?.Owner.SetPipelineUniformMatrix(this, ref mat, transpose);
		}

		public void Set(bool value)
		{
			Owner?.Owner.SetPipelineUniform(this, value);
		}

		public void Set(Texture2d tex)
		{
			Owner?.Owner.SetPipelineUniformSampler(this, tex);
		}
	}
}

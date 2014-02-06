using System;
using System.Collections.Generic;

using OpenTK;

namespace BizHawk.Bizware.BizwareGL
{
	public class PipelineUniform
	{
		internal PipelineUniform(Pipeline owner, UniformInfo info)
		{
			Owner = owner;
			Id = info.Handle;
			SamplerIndex = info.SamplerIndex;
		}

		public Pipeline Owner { get; private set; }
		public IntPtr Id { get; private set; }
		public int SamplerIndex { get; private set; }

		public void Set(Matrix4 mat, bool transpose = false)
		{
			Owner.Owner.SetPipelineUniformMatrix(this, mat, transpose);
		}

		public void Set(Vector4 vec)
		{
			Owner.Owner.SetPipelineUniform(this, vec);
		}

		public void Set(Vector2 vec)
		{
			Owner.Owner.SetPipelineUniform(this, vec);
		}

		public void Set(float f)
		{
			Owner.Owner.SetPipelineUniform(this, f);
		}

		public void Set(Vector4[] vecs)
		{
			Owner.Owner.SetPipelineUniform(this, vecs);
		}

		public void Set(ref Matrix4 mat, bool transpose = false)
		{
			Owner.Owner.SetPipelineUniformMatrix(this, ref mat, transpose);
		}

		public void Set(bool value)
		{
			Owner.Owner.SetPipelineUniform(this, value);
		}

		public void Set(Texture2d tex)
		{
			IntPtr handle;
			if (tex == null)
				handle = Owner.Owner.GetEmptyHandle();
			else handle = tex.Id;
			Owner.Owner.SetPipelineUniformSampler(this, handle);
		}
	}
}
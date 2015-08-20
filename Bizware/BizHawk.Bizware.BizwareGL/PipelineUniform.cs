using System;
using System.Collections.Generic;

using OpenTK;

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
			//Opaque = info.Opaque;
			//SamplerIndex = info.SamplerIndex;
		}

		internal void AddUniformInfo(UniformInfo ui)
		{
			_UniformInfos.Add(ui);
		}

		public IEnumerable<UniformInfo> UniformInfos { get { return _UniformInfos; } }
		List<UniformInfo> _UniformInfos = new List<UniformInfo>();

		/// <summary>
		/// Returns the sole UniformInfo or throws an exception if there's more than one
		/// </summary>
		public UniformInfo Sole
		{
			get
			{
				if (_UniformInfos.Count != 1) throw new InvalidOperationException();
				return _UniformInfos[0];
			}
		}

		public Pipeline Owner { get; private set; }
		
		public void Set(Matrix4 mat, bool transpose = false)
		{
			if (Owner == null) return; //uniform was optimized out
			Owner.Owner.SetPipelineUniformMatrix(this, mat, transpose);
		}

		public void Set(Vector4 vec)
		{
			if (Owner == null) return; //uniform was optimized out
			Owner.Owner.SetPipelineUniform(this, vec);
		}

		public void Set(Vector2 vec)
		{
			if (Owner == null) return; //uniform was optimized out
			Owner.Owner.SetPipelineUniform(this, vec);
		}

		public void Set(float f)
		{
			if (Owner == null) return; //uniform was optimized out
			Owner.Owner.SetPipelineUniform(this, f);
		}

		public void Set(Vector4[] vecs)
		{
			if (Owner == null) return; //uniform was optimized out
			Owner.Owner.SetPipelineUniform(this, vecs);
		}

		public void Set(ref Matrix4 mat, bool transpose = false)
		{
			if (Owner == null) return; //uniform was optimized out
			Owner.Owner.SetPipelineUniformMatrix(this, ref mat, transpose);
		}

		public void Set(bool value)
		{
			if (Owner == null) return; //uniform was optimized out
			Owner.Owner.SetPipelineUniform(this, value);
		}

		public void Set(Texture2d tex)
		{
			if (Owner == null) return; //uniform was optimized out
			Owner.Owner.SetPipelineUniformSampler(this, tex);
		}
	}
}
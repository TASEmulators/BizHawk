using System;
using System.Collections.Generic;

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

		public IEnumerable<UniformInfo> UniformInfos => _UniformInfos;
		private readonly List<UniformInfo> _UniformInfos = new List<UniformInfo>();

		/// <returns>the first and only <see cref="UniformInfo"/></returns>
		/// <exception cref="InvalidOperationException">more than one <see cref="UniformInfo"/> exists</exception>
		public UniformInfo Sole
		{
			get
			{
				if (_UniformInfos.Count != 1) throw new InvalidOperationException();
				return _UniformInfos[0];
			}
		}

		public Pipeline Owner { get; }
		
		public void Set(Matrix4 mat, bool transpose = false)
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

		public void Set(ref Matrix4 mat, bool transpose = false)
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
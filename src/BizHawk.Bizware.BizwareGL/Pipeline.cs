using System;
using System.Collections.Generic;

using BizHawk.Common.CollectionExtensions;

namespace BizHawk.Bizware.BizwareGL
{
	/// <summary>
	/// WARNING! PLEASE SET THIS PIPELINE CURRENT BEFORE SETTING UNIFORMS IN IT! NOT TOO GREAT, I KNOW.
	/// </summary>
	public class Pipeline : IDisposable
	{
		public readonly string Memo;

		public Pipeline(IGL owner, object opaque, bool available, VertexLayout vertexLayout, IReadOnlyList<UniformInfo> uniforms, string memo)
		{
			Memo = memo;
			Owner = owner;
			Opaque = opaque;
			VertexLayout = vertexLayout;
			Available = available;

			// create the uniforms from the info list we got
			if (!Available)
			{
				return;
			}

			UniformsDictionary = new(this);
			foreach (var ui in uniforms)
			{
				UniformsDictionary[ui.Name] = new(this)
				{
					UniformInfo = ui
				};
			}
		}

		/// <summary>
		/// Allows us to create PipelineUniforms on the fly, in case a non-existing one has been requested.
		/// Shader compilers will optimize out unused uniforms, and we wont have a record of it in the uniforms population loop
		/// </summary>
		private class UniformWorkingDictionary : Dictionary<string, PipelineUniform>
		{
			public UniformWorkingDictionary(Pipeline owner)
			{
				Owner = owner;
			}

			private Pipeline Owner;
			public new PipelineUniform this[string key]
			{
#if true
				get => this.GetValueOrPut(key, static _ => new(null));
#else
				get => this.GetValueOrPut(key, static _ => new(new UniformInfo { Opaque = null }));
#endif
				internal set => base[key] = value;
			}
		}

		private readonly UniformWorkingDictionary UniformsDictionary;
		private IDictionary<string, PipelineUniform> Uniforms => UniformsDictionary;

		public IEnumerable<PipelineUniform> GetUniforms() => Uniforms.Values;

		public PipelineUniform TryGetUniform(string name)
		{
			_ = Uniforms.TryGetValue(name, out var ret);
			return ret;
		}

		public PipelineUniform this[string key] => UniformsDictionary[key];

		public IGL Owner { get; }
		public object Opaque { get; }
		public VertexLayout VertexLayout { get; }
		public bool Available { get; }
		public string Errors { get; set; }

		public void Dispose()
		{
			Owner.FreePipeline(this);
		}
	}
}

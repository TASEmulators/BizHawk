using System;
using System.Collections.Generic;

namespace BizHawk.Bizware.BizwareGL
{
	/// <summary>
	/// WARNING! PLEASE SET THIS PIPELINE CURRENT BEFORE SETTING UNIFORMS IN IT! NOT TOO GREAT, I KNOW.
	/// </summary>
	public class Pipeline : IDisposable
	{
		public string Memo;

		public Pipeline(IGL owner, object opaque, bool available, VertexLayout vertexLayout, IEnumerable<UniformInfo> uniforms, string memo)
		{
			Memo = memo;
			Owner = owner;
			Opaque = opaque;
			VertexLayout = vertexLayout;
			Available = available;

			//create the uniforms from the info list we got
			if(!Available)
				return;

			UniformsDictionary = new SpecialWorkingDictionary(this);
			foreach(var ui in uniforms)
				UniformsDictionary[ui.Name] = new PipelineUniform(this);
			foreach (var ui in uniforms)
			{
				UniformsDictionary[ui.Name].AddUniformInfo(ui);
			}
		}

		/// <summary>
		/// Allows us to create PipelineUniforms on the fly, in case a non-existing one has been requested.
		/// Shader compilers will optimize out unused uniforms, and we wont have a record of it in the uniforms population loop
		/// </summary>
		class SpecialWorkingDictionary : Dictionary<string, PipelineUniform>
		{
			public SpecialWorkingDictionary(Pipeline owner)
			{
				Owner = owner;
			}

			Pipeline Owner;
			public new PipelineUniform this[string key]
			{
				get
				{
					PipelineUniform temp;
					if (!TryGetValue(key, out temp))
					{
						var ui = new UniformInfo();
						ui.Opaque = null;
						temp = this[key] = new PipelineUniform(null);
					}

					return temp;
				}

				internal set => base[key] = value;
			}
		}

		readonly SpecialWorkingDictionary UniformsDictionary;
		IDictionary<string, PipelineUniform> Uniforms => UniformsDictionary;

		public IEnumerable<PipelineUniform> GetUniforms() { return Uniforms.Values; }

		public PipelineUniform TryGetUniform(string name)
		{
			PipelineUniform ret = null;
			Uniforms.TryGetValue(name,out ret);
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
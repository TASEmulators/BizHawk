using System;
using System.Collections.Generic;

namespace BizHawk.Bizware.BizwareGL
{
	/// <summary>
	/// WARNING! PLEASE SET THIS PIPELINE CURRENT BEFORE SETTING UNIFORMS IN IT! NOT TOO GREAT, I KNOW.
	/// </summary>
	public class Pipeline : IDisposable
	{
		public Pipeline(IGL owner, IntPtr id, bool available, VertexLayout vertexLayout, IEnumerable<UniformInfo> uniforms)
		{
			Owner = owner;
			Id = id;
			VertexLayout = vertexLayout;
			Available = available;

			//create the uniforms from the info list we got
			UniformsDictionary = new SpecialWorkingDictionary(this);
			foreach(var ui in uniforms)
			{
				UniformsDictionary[ui.Name] = new PipelineUniform(this, ui);
			}
		}

		/// <summary>
		/// Allows us to create PipelineUniforms on the fly, in case a non-existing one has been requested.
		/// GLSL will optimize out unused uniforms, and we wont have a record of it in the uniforms population loop
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
						ui.Handle = Owner.Owner.GetEmptyUniformHandle();
						temp = this[key] = new PipelineUniform(Owner,ui);
					}

					return temp;
				}

				set
				{
					base[key] = value;
				}
			}
		}

		SpecialWorkingDictionary UniformsDictionary;
		IDictionary<string, PipelineUniform> Uniforms { get { return UniformsDictionary; } }

		public PipelineUniform this[string key]
		{
			get { return UniformsDictionary[key]; }
		}

		public IGL Owner { get; private set; }
		public IntPtr Id { get; private set; }
		public VertexLayout VertexLayout { get; private set; }
		public bool Available { get; private set; }

		public void Dispose()
		{
			//todo
		}


	}
}
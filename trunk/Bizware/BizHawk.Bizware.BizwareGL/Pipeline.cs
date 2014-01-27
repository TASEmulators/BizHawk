using System;
using System.Collections.Generic;

namespace BizHawk.Bizware.BizwareGL
{
	public class Pipeline : IDisposable
	{
		public Pipeline(IGL owner, IntPtr id, IEnumerable<UniformInfo> uniforms)
		{
			Owner = owner;
			Id = id;

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

				internal set
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

		///// <summary>
		///// Makes the pipeline current
		///// </summary>
		//public void BindData()
		//{
		//  Owner.BindPipeline(this);
		//}

		public void Dispose()
		{
			//todo
		}


	}
}
using System.Diagnostics;
using System.Collections.Generic;
using System.Drawing;
using BizHawk.Client.Common.Filters;

using BizHawk.Bizware.BizwareGL;

namespace BizHawk.Client.Common.FilterManager
{
	public enum SurfaceDisposition
	{
		Unspecified, Texture, RenderTarget
	}

	public class SurfaceFormat
	{
		public SurfaceFormat(Size size) => Size = size;
		public Size Size { get; }
	}

	public class SurfaceState
	{
		public SurfaceState() { }
		public SurfaceState(SurfaceFormat surfaceFormat, SurfaceDisposition surfaceDisposition = SurfaceDisposition.Unspecified)
		{
			SurfaceFormat = surfaceFormat;
			SurfaceDisposition = surfaceDisposition;
		}

		public SurfaceFormat SurfaceFormat { get; set; }
		public SurfaceDisposition SurfaceDisposition { get; set; }
	}

	public interface IRenderTargetProvider
	{
		RenderTarget Get(Size size);
	}

	public class FilterProgram
	{
		private readonly Dictionary<string, BaseFilter> _filterNameIndex = new Dictionary<string, BaseFilter>();

		public readonly IList<BaseFilter> Filters = new List<BaseFilter>();
		public readonly IList<ProgramStep> Program = new List<ProgramStep>();

		public BaseFilter this[string name]
		{
			get
			{
				_filterNameIndex.TryGetValue(name, out var ret);
				return ret;
			}
		}

		public enum ProgramStepType
		{
			Run,
			NewTarget,
			FinalTarget
		}

		//services to filters:
		public IGuiRenderer GuiRenderer;
		public IGL GL;
		public IRenderTargetProvider RenderTargetProvider;
		public RenderTarget GetRenderTarget(string channel = "default") { return CurrRenderTarget; }
		public RenderTarget CurrRenderTarget;

		public RenderTarget GetTempTarget(int width, int height)
		{
			return RenderTargetProvider.Get(new Size(width, height));
		}

		public void AddFilter(BaseFilter filter, string name = "")
		{
			Filters.Add(filter);
			_filterNameIndex[name] = filter;
		}

		/// <summary>
		/// Receives a point in the coordinate space of the output of the filter program and untransforms it back to input points
		/// </summary>
		public Vector2 UntransformPoint(string channel, Vector2 point)
		{
			for (int i = Filters.Count - 1; i >= 0; i--)
			{
				var filter = Filters[i];
				point = filter.UntransformPoint(channel, point);
			}

			// we COULD handle the case where the output size is 0,0, but it's not mathematically sensible
			// it should be considered a bug to call this under those conditions
			return point;
		}

		/// <summary>
		/// Receives a point in the input space of the filter program and transforms it through to output points
		/// </summary>
		public Vector2 TransformPoint(string channel, Vector2 point)
		{
			foreach (var filter in Filters)
			{
				point = filter.TransformPoint(channel, point);
			}

			// we COULD handle the case where the output size is 0,0, but it's not mathematically sensible
			// it should be considered a bug to call this under those conditions
			// in case the output size is zero, transform all points to zero, since the above maths may have malfunctioned
			////var size = Filters[Filters.Count - 1].FindOutput().SurfaceFormat.Size;
			////if (size.Width == 0) point.X = 0;
			////if (size.Height == 0) point.Y = 0;
			return point;
		}

		public class ProgramStep
		{
			public ProgramStep(ProgramStepType type, object args, string comment = null)
			{
				Type = type;
				Args = args;
				Comment = comment;
			}

			public ProgramStepType Type;
			public object Args;
			public string Comment;
			public override string ToString()
			{
				return Type switch
				{
					ProgramStepType.Run => $"Run {(int) Args} ({Comment})",
					ProgramStepType.NewTarget => $"NewTarget {(Size) Args}",
					ProgramStepType.FinalTarget => "FinalTarget",
					_ => null
				};
			}
		}

		public void Compile(string channel, Size inSize, Size outsize, bool finalTarget)
		{
			Program.Clear();

			//prep filters for initialization
			foreach (var f in Filters)
			{
				f.BeginInitialization(this);
				f.Initialize();
			}

			//propagate input size forwards through filter chain to allow a 'flex' filter to determine what its input will be
			Size presize = inSize;
			foreach (var filter in Filters)
			{
				presize = filter.PresizeInput(channel, presize);
			}

			//propagate output size backwards through filter chain to allow a 'flex' filter to determine its output based on the desired output needs
			presize = outsize;
			for (int i = Filters.Count - 1; i >= 0; i--)
			{
				var filter = Filters[i];
				presize = filter.PresizeOutput(channel, presize);
			}

			SurfaceState currState = null;

			for (int i = 0; i < Filters.Count; i++)
			{
				BaseFilter f = Filters[i];

				// check whether this filter needs input. if so, notify it of the current pipeline state
				var iosi = f.FindInput(channel);
				if (iosi != null)
				{
					iosi.SurfaceFormat = currState.SurfaceFormat;
					f.SetInputFormat(channel, currState);

					if (f.IsNop)
					{
						continue;
					}

					// check if the desired disposition needs to change from texture to render target
					// (if so, insert a render filter)
					if (iosi.SurfaceDisposition == SurfaceDisposition.RenderTarget && currState.SurfaceDisposition == SurfaceDisposition.Texture)
					{
						var renderer = new Render();
						Filters.Insert(i, renderer);
						Compile(channel, inSize, outsize, finalTarget);
						return;
					}
					// check if the desired disposition needs to change from a render target to a texture
					// (if so, the current render target gets resolved, and made no longer current
					else if (iosi.SurfaceDisposition == SurfaceDisposition.Texture && currState.SurfaceDisposition == SurfaceDisposition.RenderTarget)
					{
						var resolver = new Resolve();
						Filters.Insert(i, resolver);
						Compile(channel, inSize, outsize, finalTarget);
						return;
					}
				}

				// now, the filter will have set its output state depending on its input state. check if it outputs:
				iosi = f.FindOutput(channel);
				if (iosi != null)
				{
					if (currState == null)
					{
						currState = new SurfaceState
						{
							SurfaceFormat = iosi.SurfaceFormat,
							SurfaceDisposition = iosi.SurfaceDisposition
						};
					}
					else
					{
						// if output disposition is unspecified, change it to whatever we've got right now
						if (iosi.SurfaceDisposition == SurfaceDisposition.Unspecified)
						{
							iosi.SurfaceDisposition = currState.SurfaceDisposition;
						}

						bool newTarget = false;
						if (iosi.SurfaceFormat.Size != currState.SurfaceFormat.Size)
							newTarget = true;
						else if (currState.SurfaceDisposition == SurfaceDisposition.Texture && iosi.SurfaceDisposition == SurfaceDisposition.RenderTarget)
							newTarget = true;

						if (newTarget)
						{
							currState = new SurfaceState();
							iosi.SurfaceFormat = currState.SurfaceFormat = iosi.SurfaceFormat;
							iosi.SurfaceDisposition = currState.SurfaceDisposition = iosi.SurfaceDisposition;
							Program.Add(new ProgramStep(ProgramStepType.NewTarget, currState.SurfaceFormat.Size));
						}
						else
						{
							currState.SurfaceDisposition = iosi.SurfaceDisposition;
						}
					}
				}

				Program.Add(new ProgramStep(ProgramStepType.Run, i, f.GetType().Name));

			} // filter loop

			// if the current output disposition is a texture, we need to render it
			if (currState.SurfaceDisposition == SurfaceDisposition.Texture)
			{
				var renderer = new Render();
				Filters.Insert(Filters.Count, renderer);
				Compile(channel, inSize, outsize, finalTarget);
				return;
			}

			// patch the program so that the final RenderTarget set operation is the framebuffer instead
			if (finalTarget)
			{
				for (int i = Program.Count - 1; i >= 0; i--)
				{
					var ps = Program[i];
					if (ps.Type == ProgramStepType.NewTarget)
					{
						var size = (Size)ps.Args;
						Debug.Assert(size == outsize);
						ps.Type = ProgramStepType.FinalTarget;
						ps.Args = size;
						break;
					}
				}
			}
		}
	}
}
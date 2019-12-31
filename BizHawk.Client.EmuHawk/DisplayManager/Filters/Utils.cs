using System.Drawing;
using BizHawk.Client.EmuHawk.FilterManager;

using BizHawk.Bizware.BizwareGL;

namespace BizHawk.Client.EmuHawk.Filters
{
	public class SourceImage : BaseFilter
	{
		public SourceImage(Size size)
		{
			Size = size;
		}

		private readonly Size Size;

		public Texture2d Texture;

		public override void Run()
		{
			YieldOutput(Texture);
		}

		public override void Initialize()
		{
			DeclareOutput(new SurfaceState(new SurfaceFormat(Size), SurfaceDisposition.Texture));
		}

		public override void SetInputFormat(string channel, SurfaceState format)
		{
			DeclareOutput(SurfaceDisposition.Texture);
		}
	}

	/// <summary>
	/// transforms an input texture to an output render target (by rendering it)
	/// </summary>
	class Render : BaseFilter
	{
		public override void Initialize()
		{
			DeclareInput(SurfaceDisposition.Texture);
		}

		public override void SetInputFormat(string channel, SurfaceState state)
		{
			DeclareOutput(new SurfaceState(state.SurfaceFormat, SurfaceDisposition.RenderTarget));
		}

		public override void Run()
		{
			var renderer = FilterProgram.GuiRenderer;
			renderer.Begin(FindOutput().SurfaceFormat.Size);
			renderer.SetBlendState(FilterProgram.GL.BlendNoneCopy);
			renderer.Draw(InputTexture);
			renderer.End();
		}
	}

	class Resolve : BaseFilter
	{
		public override void Initialize()
		{
			DeclareInput(SurfaceDisposition.RenderTarget);
		}

		public override void SetInputFormat(string channel, SurfaceState state)
		{
			DeclareOutput(new SurfaceState(state.SurfaceFormat, SurfaceDisposition.Texture));
		}

		public override void Run()
		{
			YieldOutput(FilterProgram.GetRenderTarget().Texture2d);
		}
	}
}
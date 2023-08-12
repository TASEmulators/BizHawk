using System;

using BizHawk.Bizware.BizwareGL;

namespace BizHawk.Bizware.Graphics.Controls
{
	/// <summary>
	/// A factory for creating a GraphicsControl based on an IGL
	/// </summary>
	public static class GraphicsControlFactory
	{
		public static GraphicsControl CreateGraphicsControl(IGL gl)
		{
			GraphicsControl ret = gl switch
			{
				IGL_OpenGL => new OpenGLControl(),
				IGL_D3D9 d3d9 => new D3D9Control(d3d9.CreateSwapChain),
				IGL_GDIPlus gdiPlus => new GDIPlusControl(gdiPlus.CreateControlRenderTarget),
				_ => throw new InvalidOperationException()
			};

			// IGLs need the window handle in order to do things, so best create the control immediately
			ret.CreateControl();
			return ret;
		}
	}
}
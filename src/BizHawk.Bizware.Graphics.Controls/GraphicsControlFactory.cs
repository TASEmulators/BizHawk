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
				IGL_OpenGL openGL => new OpenGLControl(openGL.InitGLState),
				IGL_D3D11 d3d11 => new D3D11Control(d3d11.CreateSwapChain),
				IGL_GDIPlus gdiPlus => new GDIPlusControl(gdiPlus.CreateControlRenderTarget),
				_ => throw new InvalidOperationException()
			};

			// IGLs need the window handle in order to do things, so best create the control immediately
			ret.CreateControl();
			return ret;
		}
	}
}
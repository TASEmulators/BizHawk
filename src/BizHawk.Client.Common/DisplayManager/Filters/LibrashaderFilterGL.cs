using BizHawk.Bizware.Graphics;

namespace BizHawk.Client.Common.Filters
{
	public unsafe class LibrashaderFilterGL : LibrashaderFilterBase
	{
		private LibrashaderProcessorGL _processor;

		public LibrashaderFilterGL(string shaderPresetPath) : base(shaderPresetPath)
		{
		}

		private bool InitProcessor()
		{
			if (!ShouldReinitialize) return true;

			if (FilterProgram.GL is not IGL_OpenGL igl) return false;

			UpdateOutputSize();

			_processor ??= new LibrashaderProcessorGL(igl, _shaderPresetPath);
			return _initialized = _processor.Initialize(_filteredWidth, _filteredHeight);
		}

		public override void Run()
		{
			if (!InitProcessor()) return;

			var inputTexId = (InputTexture as OpenGLTexture2D)?.TexID ?? default;
			if (inputTexId == 0) return;

			var drawFbo = (FilterProgram.CurrRenderTarget as OpenGLRenderTarget)?.FBO ?? default;

			_processor.Render(inputTexId, drawFbo, InputTexture.Width, InputTexture.Height);
		}

		public override void Dispose()
		{
			_processor?.Dispose();
			_processor = null;
			_initialized = false;
		}
	}
}

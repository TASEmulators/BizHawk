using System.Drawing;

namespace BizHawk.Client.Common.Filters
{
	public abstract unsafe class LibrashaderFilterBase : BaseFilter, IDisposable
	{
		protected bool _initialized = false;
		protected Size _outputSize;
		protected Size _lastOutputSize;
		protected string _shaderPresetPath;
		protected int _filteredWidth;
		protected int _filteredHeight;

		public bool IsAvailable => _initialized;

		protected LibrashaderFilterBase(string shaderPresetPath)
		{
			_shaderPresetPath = shaderPresetPath;
		}

		public override void Initialize()
		{
			DeclareInput(SurfaceDisposition.Texture);
		}

		public override void SetInputFormat(string channel, SurfaceState state)
		{
			DeclareOutput(new SurfaceState(new(_outputSize), SurfaceDisposition.RenderTarget));
		}

		public override Size PresizeOutput(string channel, Size size)
		{
			_outputSize = size;
			return size;
		}

		public override Size PresizeInput(string channel, Size inSize)
		{
			return inSize;
		}

		protected void UpdateOutputSize()
		{
			_filteredWidth = _outputSize.Width;
			_filteredHeight = _outputSize.Height;
			_lastOutputSize = _outputSize;
		}

		protected bool ShouldReinitialize => !_initialized || _lastOutputSize != _outputSize;

		public abstract void Dispose();
	}
}

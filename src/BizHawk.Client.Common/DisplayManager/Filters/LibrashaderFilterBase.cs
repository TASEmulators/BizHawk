using System.Drawing;
using System.IO;

namespace BizHawk.Client.Common.Filters
{
	public abstract unsafe class LibrashaderFilterBase : BaseFilter, IDisposable
	{
		protected IntPtr _preset = IntPtr.Zero;
		protected IntPtr _chain = IntPtr.Zero;
		protected uint _frameCount = 0;
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

		protected bool ValidateCommonPrerequisites()
		{
			if (_filteredWidth <= 0 || _filteredHeight <= 0) return false;
			if (!Librashader.Load()) return false;
			if (!File.Exists(_shaderPresetPath)) return false;
			return true;
		}

		protected bool CreatePresetIfNeeded()
		{
			if (_preset != IntPtr.Zero) return true;

			IntPtr error = Librashader.PresetCreate(_shaderPresetPath, out _preset);
			if (error != IntPtr.Zero)
			{
				_ = Librashader.libra_error_print(error);
				return false;
			}
			return true;
		}

		protected void UpdateOutputSize()
		{
			_filteredWidth = _outputSize.Width;
			_filteredHeight = _outputSize.Height;
			_lastOutputSize = _outputSize;
		}

		protected bool ShouldReinitialize => !_initialized || _lastOutputSize != _outputSize;

		protected void FreePreset()
		{
			if (_preset != IntPtr.Zero)
			{
				_ = Librashader.libra_preset_free(ref _preset);
				_preset = IntPtr.Zero;
			}
		}

		public abstract void Dispose();
	}
}

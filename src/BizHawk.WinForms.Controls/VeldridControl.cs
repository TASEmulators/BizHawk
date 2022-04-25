using System;
using System.Diagnostics;
using System.Windows.Forms;

using Veldrid;
using Veldrid.Utilities;

namespace BizHawk.WinForms.Controls
{
	public class VeldridControl : Control
	{
		private GraphicsDevice? _device;

		private bool _isAnimated;

		private DisposeCollectorResourceFactory? _resources;

		private readonly Stopwatch _stopwatch = new();

		public bool IsAnimated
		{
			get => _isAnimated;
			set
			{
				if (value == _isAnimated) return;
				_isAnimated = value;
				if (value) Application.Idle += OnIdle;
				else Application.Idle -= OnIdle;
			}
		}

		public GraphicsBackend VeldridBackend { get; set; } = GraphicsBackend.Vulkan;

		public event Action<GraphicsDevice, ResourceFactory, Swapchain>? GraphicsDeviceCreated;

		public event Action? GraphicsDeviceDestroyed;

		public event Action<float>? Rendering;

		public VeldridControl()
		{
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.DoubleBuffer, false);
			SetStyle(ControlStyles.Opaque, true);
			SetStyle(ControlStyles.ResizeRedraw, true);
			SetStyle(ControlStyles.UserPaint, true);
		}

		protected override void OnHandleCreated(EventArgs e)
		{
			base.OnHandleCreated(e);
			GraphicsDeviceOptions options = new(true, PixelFormat.R32_Float, false);
			if (VeldridBackend == GraphicsBackend.Vulkan) options.PreferStandardClipSpaceYDirection = true;
			_device = VeldridHelpers.CreateGraphicsDevice(this, VeldridBackend, options, Math.Max(1U, (uint) Width), Math.Max(1U, (uint) Height));
			_resources = new DisposeCollectorResourceFactory(_device.ResourceFactory);
			GraphicsDeviceCreated?.Invoke(_device, _resources, _device.MainSwapchain);
		}

		protected override void OnHandleDestroyed(EventArgs e)
		{
			if (_device != null)
			{
				GraphicsDeviceDestroyed?.Invoke();
				_device.WaitForIdle();
				_resources!.DisposeCollector.DisposeAll();
				_device.Dispose();
				_device = null;
			}
			base.OnHandleDestroyed(e);
		}

		private void OnIdle(object sender, EventArgs e) => Invalidate();

		protected override void OnPaint(PaintEventArgs e)
		{
			var elapsedTotalSeconds = (float) _stopwatch.Elapsed.TotalSeconds;
			if (!_stopwatch.IsRunning) _stopwatch.Start();
			else _stopwatch.Restart();
			if (_device != null) Rendering?.Invoke(elapsedTotalSeconds);
		}

		protected override void OnResize(EventArgs e)
		{
			_device?.ResizeMainWindow((uint) Width, (uint) Height);
			Invalidate();
			base.OnResize(e);
		}
	}
}

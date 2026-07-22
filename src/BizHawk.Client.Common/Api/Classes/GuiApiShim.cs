using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Runtime.CompilerServices;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public sealed class GuiApiShim : IGuiApi
	{
		private static InvalidOperationException CantDrawOutsideBatch
			=> new($"Can't make draw calls outside {nameof(WithSurface)}! Check you're calling this method on the instance passed to the lambda.");

		private readonly GuiApi _realImpl;

		[RequiredService]
		private IEmulator Emulator
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _realImpl.Emulator;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set => _realImpl.Emulator = value;
		}

		public GuiApiShim(
			IDialogController dialogController,
			DisplayManagerBase displayManager,
			Action<string> logCallback)
				=> _realImpl = new(dialogController, displayManager, logCallback);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void AddMessage(string message, [LiteralExpected] int? duration = null)
			=> _realImpl.AddMessage(message, duration: duration);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void ClearGraphics(DisplaySurfaceID? surfaceID = null)
			=> _realImpl.ClearGraphics(surfaceID);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void ClearImageCache()
			=> _realImpl.ClearImageCache();

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void ClearText()
			=> _realImpl.ClearText();

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Dispose()
			=> _realImpl.Dispose();

		public void DrawAxis(int x, int y, int size, Color? color = null, DisplaySurfaceID? surfaceID = null)
			=> throw CantDrawOutsideBatch;

		public void DrawBezier(
			Point p1,
			Point p2,
			Point p3,
			Point p4,
			Color? color = null,
			DisplaySurfaceID? surfaceID = null)
				=> throw CantDrawOutsideBatch;

		public void DrawBeziers(Point[] points, Color? color = null, DisplaySurfaceID? surfaceID = null)
			=> throw CantDrawOutsideBatch;

		public void DrawBox(
			int x,
			int y,
			int x2,
			int y2,
			Color? line = null,
			Color? background = null,
			DisplaySurfaceID? surfaceID = null)
				=> throw CantDrawOutsideBatch;

		public void DrawEllipse(
			int x,
			int y,
			int width,
			int height,
			Color? line = null,
			Color? background = null,
			DisplaySurfaceID? surfaceID = null)
				=> throw CantDrawOutsideBatch;

		public void DrawIcon(
			string path,
			int x,
			int y,
			int? width = null,
			int? height = null,
			DisplaySurfaceID? surfaceID = null)
				=> throw CantDrawOutsideBatch;

		public void DrawImage(
			Image img,
			int x,
			int y,
			int? width = null,
			int? height = null,
			bool cache = true,
			DisplaySurfaceID? surfaceID = null)
				=> throw CantDrawOutsideBatch;

		public void DrawImage(
			string path,
			int x,
			int y,
			int? width = null,
			int? height = null,
			bool cache = true,
			DisplaySurfaceID? surfaceID = null)
				=> throw CantDrawOutsideBatch;

		public void DrawImageRegion(
			Image img,
			int source_x,
			int source_y,
			int source_width,
			int source_height,
			int dest_x,
			int dest_y,
			int? dest_width = null,
			int? dest_height = null,
			DisplaySurfaceID? surfaceID = null)
				=> throw CantDrawOutsideBatch;

		public void DrawImageRegion(
			string path,
			int source_x,
			int source_y,
			int source_width,
			int source_height,
			int dest_x,
			int dest_y,
			int? dest_width = null,
			int? dest_height = null,
			DisplaySurfaceID? surfaceID = null)
				=> throw CantDrawOutsideBatch;

		public void DrawLine(int x1, int y1, int x2, int y2, Color? color = null, DisplaySurfaceID? surfaceID = null)
			=> throw CantDrawOutsideBatch;

		public void DrawPie(
			int x,
			int y,
			int width,
			int height,
			int startangle,
			int sweepangle,
			Color? line = null,
			Color? background = null,
			DisplaySurfaceID? surfaceID = null)
				=> throw CantDrawOutsideBatch;

		public void DrawPixel(int x, int y, Color? color = null, DisplaySurfaceID? surfaceID = null)
			=> throw CantDrawOutsideBatch;

		public void DrawPolygon(
			Point[] points,
			Color? line = null,
			Color? background = null,
			DisplaySurfaceID? surfaceID = null)
				=> throw CantDrawOutsideBatch;

		public void DrawRectangle(
			int x,
			int y,
			int width,
			int height,
			Color? line = null,
			Color? background = null,
			DisplaySurfaceID? surfaceID = null)
				=> throw CantDrawOutsideBatch;

		public void DrawString(
			int x,
			int y,
			string message,
			Color? forecolor = null,
			Color? backcolor = null,
			int? fontsize = null,
			string fontfamily = null,
			string fontstyle = null,
			string horizalign = null,
			string vertalign = null,
			DisplaySurfaceID? surfaceID = null)
				=> throw CantDrawOutsideBatch;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Color GetDefaultTextBackground()
			=> _realImpl.GetDefaultTextBackground();

		public void PixelText(
			int x,
			int y,
			string message,
			Color? forecolor = null,
			Color? backcolor = null,
			string fontfamily = null,
			DisplaySurfaceID? surfaceID = null)
				=> throw CantDrawOutsideBatch;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void SetDefaultBackgroundColor(Color color)
			=> _realImpl.SetDefaultBackgroundColor(color);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void SetDefaultForegroundColor(Color color)
			=> _realImpl.SetDefaultForegroundColor(color);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void SetDefaultPixelFont(string fontfamily)
			=> _realImpl.SetDefaultPixelFont(fontfamily);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void SetDefaultTextBackground(Color color)
			=> _realImpl.SetDefaultTextBackground(color);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Text(int x, int y, string message, Color? forecolor = null, string anchor = null)
			=> _realImpl.Text(x: x, y: y, message: message, forecolor, anchor: anchor);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void ToggleCompositingMode()
			=> _realImpl.ToggleCompositingMode();

		public void WithSurface(DisplaySurfaceID surfaceID, Action<IGuiApi> drawingCallsFunc)
		{
			_realImpl.SurfaceID = surfaceID;
			try
			{
				drawingCallsFunc(_realImpl);
			}
			finally
			{
				_realImpl.SurfaceID = null;
			}
		}
	}
}

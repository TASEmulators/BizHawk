using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Numerics;
using System.Runtime.InteropServices;

using ImGuiNET;

using BizHawk.Common;

using SDGraphics = System.Drawing.Graphics;

namespace BizHawk.Bizware.Graphics
{
	/// <summary>
	/// Wraps ImGui to create a simple 2D renderer
	/// </summary>
	internal class ImGui2DRenderer : I2DRenderer
	{
		// ImDrawListSharedData is defined in imgui_internal.h, and therefore it is not exposed in ImGuiNET
		// we want to use it directly however, and cimgui does give exports for it

		[StructLayout(LayoutKind.Sequential)]
		private struct ImDrawListSharedData
		{
			public Vector2 TexUvWhitePixel;
			public IntPtr Font;
			public float FontSize;
			public float CurveTessellationTol;
			// other fields are present, but we don't care about them
		}

		[DllImport("cimgui", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
		private static extern unsafe ImDrawListSharedData* ImDrawListSharedData_ImDrawListSharedData();

		[DllImport("cimgui", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
		private static extern unsafe void ImDrawListSharedData_SetCircleTessellationMaxError(ImDrawListSharedData* self, float max_error);

		private static readonly unsafe ImDrawListSharedData* _drawListSharedData;

		static ImGui2DRenderer()
		{
			unsafe
			{
				_drawListSharedData = ImDrawListSharedData_ImDrawListSharedData();
				// values taken from default ImGuiStyle
				_drawListSharedData->CurveTessellationTol = 1.25f;
				ImDrawListSharedData_SetCircleTessellationMaxError(_drawListSharedData, 0.3f);
			}
		}

		private readonly HashSet<GCHandle> _gcHandles = [ ];
		private ITexture2D _stringTexture;
		private IRenderTarget _pass1RenderTarget;
		private bool _splitNextCmd;
		private bool _needBlending;

		protected virtual float RenderThickness => 1;

		protected readonly IGL _igl;
		protected readonly ImGuiResourceCache _resourceCache;
		protected ImDrawListPtr _imGuiDrawList;
		protected bool _hasDrawStringCommand;
		protected bool _hasClearPending;
		protected Bitmap _stringOutput;
		protected SDGraphics _stringGraphics;
		protected IRenderTarget _pass2RenderTarget;

		public ImGui2DRenderer(IGL igl, ImGuiResourceCache resourceCache)
		{
			_igl = igl;
			_resourceCache = resourceCache;

			unsafe
			{
				_imGuiDrawList = ImGuiNative.ImDrawList_ImDrawList((IntPtr)_drawListSharedData);
			}

			_pendingBlendEnable = true;
			ResetDrawList();
		}

		public void Dispose()
		{
			ClearGCHandles();
			_pass2RenderTarget?.Dispose();
			_pass2RenderTarget = null;
			_pass1RenderTarget?.Dispose();
			_pass1RenderTarget = null;
			_stringTexture?.Dispose();
			_stringTexture = null;
			_stringGraphics?.Dispose();
			_stringGraphics = null;
			_stringOutput?.Dispose();
			_stringOutput = null;
			_imGuiDrawList.Destroy();
			_imGuiDrawList = IntPtr.Zero;
		}

		protected void ClearStringOutput()
		{
			var bmpData = _stringOutput.LockBits(new(0, 0, _stringOutput.Width, _stringOutput.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
			Util.UnsafeSpanFromPointer(ptr: bmpData.Scan0, length: bmpData.Stride * bmpData.Height).Clear();
			_stringOutput.UnlockBits(bmpData);
		}

		private void ClearGCHandles()
		{
			foreach (var gcHandle in _gcHandles)
			{
				switch (gcHandle.Target)
				{
					// we can't actually dispose this, as the bitmap might still be cached within GuiApi
					// it doesn't matter too much, as the finalizer would also dispose this, so no memory leaks will occur
#if false
					case ImGuiUserTexture userTexture:
						// only dispose anything not cached somewhere
						if (userTexture.Bitmap != _stringOutput
							&& !_resourceCache.TextureCache.ContainsKey(userTexture.Bitmap))
						{
							userTexture.Bitmap.Dispose();
						}
						break;
#endif
					case DrawStringArgs args:
						args.Font.Dispose();
						break;
				}

				gcHandle.Free();
			}

			_gcHandles.Clear();
		}

		private void ResetDrawList()
		{
			ClearGCHandles();
			_imGuiDrawList._ResetForNewFrame();
			_imGuiDrawList.Flags |= ImDrawListFlags.AllowVtxOffset;
			_hasDrawStringCommand = false;
			_splitNextCmd = false;
			_needBlending = false;
			EnableBlending = _pendingBlendEnable;
		}

		protected class ImGuiUserTexture
		{
			public Bitmap Bitmap;
			public bool WantCache;
		}

		protected class DrawStringArgs
		{
			public string Str;
			public Font Font;
			public Color Color;
			public float X, Y;
			public StringFormat Format;
			public TextRenderingHint TextRenderingHint;
		}

		protected enum DrawCallbackId
		{
			None,
			DisableBlending,
			EnableBlending,
			DrawString,
		}

		private void PerformPasses(ImDrawCmdPtr cmd, bool forceBlending)
		{
			if ((EnableBlending && _needBlending) || forceBlending)
			{
				_pass1RenderTarget.Bind();
				_resourceCache.SetBlendingParamters(_pass2RenderTarget, false);
				_igl.DrawIndexed((int)cmd.ElemCount, (int)cmd.IdxOffset, (int)cmd.VtxOffset);

				_pass2RenderTarget.Bind();
				_resourceCache.SetBlendingParamters(_pass1RenderTarget, true);
				_igl.DrawIndexed((int)cmd.ElemCount, (int)cmd.IdxOffset, (int)cmd.VtxOffset);
			}
			else
			{
				_resourceCache.SetBlendingParamters(null, true);
				_igl.DrawIndexed((int)cmd.ElemCount, (int)cmd.IdxOffset, (int)cmd.VtxOffset);
			}
		}

		protected virtual void RenderInternal(int width, int height)
		{
			// we handle blending programmatically with 2 passes
			// therefore, we disable normal blending operations
			_igl.DisableBlending();

			_igl.BindPipeline(_resourceCache.Pipeline);
			_resourceCache.SetProjection(width, height);

			if (_imGuiDrawList.VtxBuffer.Size > 0)
			{
				_resourceCache.Pipeline.SetVertexData(_imGuiDrawList.VtxBuffer.Data, _imGuiDrawList.VtxBuffer.Size);
			}

			if (_imGuiDrawList.IdxBuffer.Size > 0)
			{
				_resourceCache.Pipeline.SetIndexData(_imGuiDrawList.IdxBuffer.Data, _imGuiDrawList.IdxBuffer.Size);
			}

			var cmdBuffer = _imGuiDrawList.CmdBuffer;
			for (var i = 0; i < cmdBuffer.Size; i++)
			{
				var cmd = cmdBuffer[i];
				var callbackId = (DrawCallbackId)cmd.UserCallback;
				switch (callbackId)
				{
					case DrawCallbackId.None:
					{
						var texId = cmd.GetTexID();
						ITexture2D tempTex = null;
						if (texId != IntPtr.Zero)
						{
							var userTex = (ImGuiUserTexture)GCHandle.FromIntPtr(texId).Target!;
							if (userTex.WantCache && _resourceCache.TextureCache.TryGetValue(userTex.Bitmap, out var cachedTexture))
							{
								_resourceCache.SetTexture(cachedTexture);
							}
							else
							{
								// skip this draw if it's the string output draw (we execute this at arbitrary points rather)
								if (userTex.Bitmap == _stringOutput)
								{
									continue;
								}

								tempTex = _igl.LoadTexture(userTex.Bitmap);
								_resourceCache.SetTexture(tempTex);
								if (userTex.WantCache)
								{
									_resourceCache.TextureCache.Add(userTex.Bitmap, tempTex);
									tempTex = null;
								}
							}
						}
						else
						{
							_resourceCache.SetTexture(null);
						}

						PerformPasses(cmd, forceBlending: false);
						tempTex?.Dispose();
						break;
					}
					case DrawCallbackId.DisableBlending:
						EnableBlending = false;
						break;
					case DrawCallbackId.EnableBlending:
						EnableBlending = true;
						break;
					case DrawCallbackId.DrawString:
					{
						var stringArgs = (DrawStringArgs)GCHandle.FromIntPtr(cmd.UserCallbackData).Target!;
						var brush = _resourceCache.CachedBrush;
						brush.Color = stringArgs.Color;
						_stringGraphics.TextRenderingHint = stringArgs.TextRenderingHint;
						_stringGraphics.DrawString(stringArgs.Str, stringArgs.Font, brush, stringArgs.X, stringArgs.Y, stringArgs.Format);

						// now draw the string graphics, if the next command is not another draw string command
						if ((DrawCallbackId)cmdBuffer[i + 1].UserCallback != DrawCallbackId.DrawString)
						{
							var lastCmd = cmdBuffer[cmdBuffer.Size - 1];
							var texId = lastCmd.GetTexID();

							// last command must be for drawing the string output bitmap
							var userTex = (ImGuiUserTexture)GCHandle.FromIntPtr(texId).Target!;
							if (userTex.Bitmap != _stringOutput)
							{
								throw new InvalidOperationException("Unexpected bitmap mismatch!");
							}

							_stringTexture.LoadFrom(new BitmapBuffer(_stringOutput, new()));
							_resourceCache.SetTexture(_stringTexture);

							// the string texture is largely transparent, so it implicitly needs blending
							// however, other things might not need blending
							// so as an optimization, don't force blending everywhere, just here if need be
							PerformPasses(lastCmd, forceBlending: true);
							ClearStringOutput();
						}

						break;
					}
					default:
						throw new InvalidOperationException();
				}
			}
		}

		public ITexture2D Render(int width, int height)
		{
			var needsRender = _imGuiDrawList.VtxBuffer.Size > 0 || _imGuiDrawList.IdxBuffer.Size > 0 || _hasDrawStringCommand;
			var needsClear = needsRender || _hasClearPending;
			if (_pass1RenderTarget == null || _pass1RenderTarget.Width != width || _pass1RenderTarget.Height != height)
			{
				_pass1RenderTarget?.Dispose();
				_pass1RenderTarget = _igl.CreateRenderTarget(width, height);
				_pass2RenderTarget?.Dispose();
				_pass2RenderTarget = _igl.CreateRenderTarget(width, height);
				needsClear = true;
			}

			if (_hasDrawStringCommand
				&& (_stringOutput == null
					|| _stringOutput.Width != width
					|| _stringOutput.Height != height))
			{
				_stringTexture?.Dispose();
				_stringGraphics?.Dispose();
				_stringOutput?.Dispose();
				_stringOutput = new(width, height, PixelFormat.Format32bppArgb);
				_stringGraphics = SDGraphics.FromImage(_stringOutput);
				_stringTexture = _igl.CreateTexture(width, height);
			}

			_pass2RenderTarget.Bind();
			_igl.SetViewport(width, height);

			if (needsClear)
			{
				_igl.ClearColor(Color.FromArgb(0));
				_hasClearPending = false;
			}

			if (needsRender)
			{
				if (_hasDrawStringCommand)
				{
					ClearStringOutput();

					// synthesize an add image command for our string bitmap
					// we have to do this now as the string bitmap isn't properly created until here
					// however, the position in the command list (the end) will be a lie
					// we'll rather just reuse the command at the end when we need to draw graphics
					var texture = new ImGuiUserTexture { Bitmap = _stringOutput, WantCache = false };
					var handle = GCHandle.Alloc(texture, GCHandleType.Normal);
					_gcHandles.Add(handle);
					_imGuiDrawList.AddImage(
						user_texture_id: GCHandle.ToIntPtr(handle),
						p_min: new(0, 0),
						p_max: new(_stringOutput.Width, _stringOutput.Height));
				}

				_imGuiDrawList._PopUnusedDrawCmd();
				RenderInternal(width, height);
				ResetDrawList();
			}

			return _pass2RenderTarget;
		}

		public void Clear()
		{
			ResetDrawList();
			_hasClearPending = true;
		}

		public void Discard()
			=> ResetDrawList();

		protected bool EnableBlending { get; private set; }
		private bool _pendingBlendEnable;

		public CompositingMode CompositingMode
		{
			set
			{
				switch (_pendingBlendEnable)
				{
					// CompositingMode.SourceCopy means disable blending
					case true when value == CompositingMode.SourceCopy:
						_imGuiDrawList.AddCallback((IntPtr)DrawCallbackId.DisableBlending, IntPtr.Zero);
						_pendingBlendEnable = false;
						_splitNextCmd = false;
						break;
					// CompositingMode.SourceOver means enable blending
					case false when value == CompositingMode.SourceOver:
						_imGuiDrawList.AddCallback((IntPtr)DrawCallbackId.EnableBlending, IntPtr.Zero);
						_pendingBlendEnable = true;
						_splitNextCmd = false;
						break;
				}
			}
		}

		private void CheckAlpha(Color color)
		{
			if (color.A != 0xFF || _splitNextCmd)
			{
				_imGuiDrawList.AddDrawCmd();
				_splitNextCmd = color.A != 0xFF;
				_needBlending = true;
			}
		}

		public void DrawBezier(Color color, Point pt1, Point pt2, Point pt3, Point pt4)
		{
			CheckAlpha(color);
			_imGuiDrawList.AddBezierCubic(
				p1: pt1.ToVector(),
				p2: pt2.ToVector(),
				p3: pt3.ToVector(),
				p4: pt4.ToVector(),
				col: (uint)color.ToArgb(),
				thickness: RenderThickness);
		}

		public void DrawBeziers(Color color, Point[] points)
		{
			if (points.Length < 4 || (points.Length - 1) % 3 != 0)
			{
				throw new InvalidOperationException("Invalid number of points");
			}

			var startPt = points[0];
			var col = (uint)color.ToArgb();
			for (var i = 1; i < points.Length; i += 3)
			{
				CheckAlpha(color);
				_imGuiDrawList.AddBezierCubic(
					p1: startPt.ToVector(),
					p2: points[i + 0].ToVector(),
					p3: points[i + 1].ToVector(),
					p4: points[i + 2].ToVector(),
					col: col,
					thickness: RenderThickness);
				startPt = points[i + 2];
			}
		}

		public void DrawRectangle(Color color, int x, int y, int width, int height)
		{
			CheckAlpha(color);

			// we don't use AddRect as we want to avoid double drawing at the corners
			// as that produces artifacts with alpha blending

			// keep in mind width/height include the beginning pixel
			// e.g. a 1x1 rect has the same coordinate for all corners, so you don't + 1, you + 1 - 1
			var right = x + width - 1;
			var bottom = y + height - 1;

			if (width == 1 || height == 1)
			{
				// width or height of 1 is just a single line
				DrawLineInternal(color, x, y, right, bottom);
				return;
			}

			// top left to top right
			DrawLineInternal(color, x, y, right, y);
			// top right (and 1 pixel down) to bottom right
			DrawLineInternal(color, right, y + 1, right, bottom);
			// bottom right (and 1 pixel left) to bottom left
			DrawLineInternal(color, right - 1, bottom, x, bottom);
			// bottom left (and 1 pixel up) to top left (and 1 pixel down)
			DrawLineInternal(color, x, bottom - 1, x, y + 1);
		}

		public void FillRectangle(Color color, int x, int y, int width, int height)
		{
			CheckAlpha(color);
			_imGuiDrawList.AddRectFilled(
				p_min: new(x, y),
				p_max: new(x + width, y + height),
				col: (uint)color.ToArgb());
		}

		public void DrawEllipse(Color color, int x, int y, int width, int height)
		{
			CheckAlpha(color);
			var radius = new Vector2(width / 2.0f, height / 2.0f);
			_imGuiDrawList.AddEllipse(
				center: new(x + radius.X, y + radius.Y),
				radius: radius,
				col: (uint)color.ToArgb(),
				rot: 0,
				num_segments: 0,
				RenderThickness);
		}

		public void FillEllipse(Color color, int x, int y, int width, int height)
		{
			CheckAlpha(color);
			var radius = new Vector2(width / 2.0f, height / 2.0f);
			_imGuiDrawList.AddEllipseFilled(
				center: new(x + radius.X, y + radius.Y),
				radius: radius,
				col: (uint)color.ToArgb());
		}

		public void DrawImage(Bitmap image, int x, int y)
		{
			_splitNextCmd = false;
			_needBlending = true;
			var texture = new ImGuiUserTexture { Bitmap = image, WantCache = false };
			var handle = GCHandle.Alloc(texture, GCHandleType.Normal);
			_gcHandles.Add(handle);
			_imGuiDrawList.AddImage(
				user_texture_id: GCHandle.ToIntPtr(handle),
				p_min: new(x, y),
				p_max: new(x + image.Width, y + image.Height));
		}

		public void DrawImage(Bitmap image, Rectangle destRect, int srcX, int srcY, int srcWidth, int srcHeight, bool cache)
		{
			_splitNextCmd = false;
			_needBlending = true;
			var texture = new ImGuiUserTexture { Bitmap = image, WantCache = cache };
			var handle = GCHandle.Alloc(texture, GCHandleType.Normal);
			_gcHandles.Add(handle);
			var imgWidth = (float)image.Width;
			var imgHeight = (float)image.Height;
			_imGuiDrawList.AddImage(
				user_texture_id: GCHandle.ToIntPtr(handle),
				p_min: new(destRect.Left, destRect.Top),
				p_max: new(destRect.Right, destRect.Bottom),
				uv_min: new(srcX / imgWidth, srcY / imgHeight),
				uv_max: new((srcX + srcWidth) / imgWidth, (srcY + srcHeight) / imgHeight));
		}

		private void DrawLineInternal(Color color, int x1, int y1, int x2, int y2)
		{
			var p1 = new Vector2(x1, y1);
			var p2 = new Vector2(x2, y2);

			// imgui seems to have behavior which necessitate the rightmost and bottommost ends be extended to correctly draw the line (some subpixel jitter?)

			if (p1.X > p2.X)
			{
				// right to left drawing, extend the beginning by 1 pixel rightwards
				p1.X++;
			}
			else
			{
				// left to right drawing, extend the end by 1 pixel rightwards
				p2.X++;
			}

			if (p1.Y > p2.Y)
			{
				// down to up drawing, extend the beginning by 1 pixel downwards
				p1.Y++;
			}
			else
			{
				// up to down drawing, extend the end by 1 pixel downwards
				p2.Y++;
			}

			_imGuiDrawList.PathLineTo(p1);
			_imGuiDrawList.PathLineTo(p2);
			_imGuiDrawList.PathStroke((uint)color.ToArgb(), 0, RenderThickness);
		}

		public void DrawLine(Color color, int x1, int y1, int x2, int y2)
		{
			CheckAlpha(color);
			DrawLineInternal(color, x1, y1, x2, y2);
		}

		public void DrawPie(Color color, int x, int y, int width, int height, int startAngle, int sweepAngle)
		{
			CheckAlpha(color);
			var radius = new Vector2(width / 2.0f, height / 2.0f);
			var center = new Vector2(x + radius.X, y + radius.Y);
			var aMin = (float)(Math.PI / 180 * startAngle);
			var aMax = (float)(Math.PI / 180 * (startAngle + sweepAngle));
			_imGuiDrawList.PathEllipticalArcTo(center, radius, 0, aMin, aMax);
			_imGuiDrawList.PathLineTo(center);
			_imGuiDrawList.PathStroke((uint)color.ToArgb(), ImDrawFlags.Closed, RenderThickness);
		}

		public void FillPie(Color color, int x, int y, int width, int height, int startAngle, int sweepAngle)
		{
			CheckAlpha(color);
			var radius = new Vector2(width / 2.0f, height / 2.0f);
			var center = new Vector2(x + radius.X, y + radius.Y);
			var aMin = (float)(Math.PI / 180 * startAngle);
			var aMax = (float)(Math.PI / 180 * (startAngle + sweepAngle));
			_imGuiDrawList.PathEllipticalArcTo(center, radius, 0, aMin, aMax);
			_imGuiDrawList.PathLineTo(center);
			_imGuiDrawList.PathFillConvex((uint)color.ToArgb());
		}

		public unsafe void DrawPolygon(Color color, Point[] points)
		{
			CheckAlpha(color);
			var vectorPoints = Array.ConvertAll(points, static p => new Vector2(p.X + 0.5f, p.Y + 0.5f));
			fixed (Vector2* p = vectorPoints)
			{
				_imGuiDrawList.AddPolyline(
					points: ref *p,
					num_points: vectorPoints.Length,
					col: (uint)color.ToArgb(),
					flags: ImDrawFlags.Closed,
					thickness: RenderThickness);
			}
		}

		public unsafe void FillPolygon(Color color, Point[] points)
		{
			CheckAlpha(color);
			var vectorPoints = Array.ConvertAll(points, static p => new Vector2(p.X + 0.5f, p.Y + 0.5f));
			fixed (Vector2* p = vectorPoints)
			{
				_imGuiDrawList.AddConcavePolyFilled(
					points: ref *p,
					num_points: vectorPoints.Length,
					col: (uint)color.ToArgb());
			}
		}

		public void DrawString(string s, Font font, Color color, float x, float y, StringFormat format, TextRenderingHint textRenderingHint)
		{
			_splitNextCmd = false;
			// drawing a string in the end just overlays a largely transparent texture with the drawn text
			// as such, it implicitly needs blending during the string drawing
			// however, as an optimization, we can avoid blending in general if the string color is opaque
			// if it isn't opaque, we need to blend everywhere regardless
			_needBlending |= color.A != 0xFF;
			var stringArgs = new DrawStringArgs { Str = s, Font = font, Color = color, X = x, Y = y, Format = format, TextRenderingHint = textRenderingHint };
			var handle = GCHandle.Alloc(stringArgs, GCHandleType.Normal);
			_gcHandles.Add(handle);
			_imGuiDrawList.AddCallback((IntPtr)DrawCallbackId.DrawString, GCHandle.ToIntPtr(handle));
			_hasDrawStringCommand = true;
		}
	}
}

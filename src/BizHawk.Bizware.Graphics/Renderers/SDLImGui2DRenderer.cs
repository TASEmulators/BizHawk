using System.Drawing.Imaging;
using System.Runtime.InteropServices;

using ImGuiNET;

using static SDL2.SDL;

#pragma warning disable BHI1007 // target-typed Exception TODO don't

namespace BizHawk.Bizware.Graphics
{
	/// <summary>
	/// Wraps SDL2's software rendering with an ImGui 2D renderer
	/// Used for the GDI+ IGL, which doesn't understand vertexes and such
	/// </summary>
	internal class SDLImGui2DRenderer : ImGui2DRenderer
	{
		// SDL's software renderer sometimes doesn't fill in shapes all the way with a thickness of 1
		// a thickness of 2 seems to suffice however
		protected override float RenderThickness => 2.0f;

		public SDLImGui2DRenderer(IGL_GDIPlus gdiPlus, ImGuiResourceCache resourceCache)
			: base(gdiPlus, resourceCache)
		{
			_ = gdiPlus; // this backend must use the GDI+ display method, as it assumes IRenderTarget is an GDIPlusRenderTarget
		}

		private readonly ref struct SDLSurface
		{
			public readonly IntPtr Surface;

			public SDLSurface(BitmapData bmpData)
			{
				Surface = SDL_CreateRGBSurfaceWithFormatFrom(
					bmpData.Scan0, bmpData.Width, bmpData.Height, 8, bmpData.Stride, SDL_PIXELFORMAT_ABGR8888);
				if (Surface == IntPtr.Zero)
				{
					throw new($"Failed to create SDL surface, SDL error: {SDL_GetError()}");
				}
			}

			public void Dispose()
				=> SDL_FreeSurface(Surface);
		}

		private readonly ref struct SDLSoftwareRenderer
		{
			public readonly IntPtr Renderer;

			public SDLSoftwareRenderer(SDLSurface surface)
			{
				Renderer = SDL_CreateSoftwareRenderer(surface.Surface);
				if (Renderer == IntPtr.Zero)
				{
					throw new($"Failed to create SDL software renderer, SDL error: {SDL_GetError()}");
				}
			}

			public void Dispose()
				=> SDL_DestroyRenderer(Renderer);
		}

		// SDL2-CS import expects float[]/int[]'s instead of raw pointers :(
		[DllImport("SDL2", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
		private static extern int SDL_RenderGeometryRaw(
			IntPtr renderer,
			IntPtr texture,
			IntPtr xy,
			int xy_stride,
			IntPtr color,
			int color_stride,
			IntPtr uv,
			int uv_stride,
			int num_vertices,
			IntPtr indices,
			int num_indices,
			int size_indices
		);

		private static unsafe void RenderCommand(IntPtr sdlRenderer, IntPtr sdlTexture, ImDrawListPtr cmdList, ImDrawCmdPtr cmd)
		{
			var vtxBuffer = (ImDrawVert*)cmdList.VtxBuffer.Data;
			var idxBuffer = (ushort*)cmdList.IdxBuffer.Data;
			var vtx = &vtxBuffer![cmd.VtxOffset];
			_ = SDL_RenderGeometryRaw(
				renderer: sdlRenderer,
				texture: sdlTexture,
				xy: (IntPtr)(&vtx->pos),
				xy_stride: sizeof(ImDrawVert),
				color: (IntPtr)(&vtx->col),
				color_stride: sizeof(ImDrawVert),
				uv: (IntPtr)(&vtx->uv),
				uv_stride: sizeof(ImDrawVert),
				num_vertices: (int)(cmdList.VtxBuffer.Size - cmd.VtxOffset),
				indices: (IntPtr)(&idxBuffer![cmd.IdxOffset]),
				num_indices: (int)cmd.ElemCount,
				size_indices: sizeof(ushort)
			);
		}

		protected override void RenderInternal(int width, int height)
		{
			var rt = (GDIPlusRenderTarget)_pass2RenderTarget;
			var bmpData = rt.SDBitmap.LockBits(rt.GetRectangle(), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
			try
			{
				using var surface = new SDLSurface(bmpData);
				using var renderer = new SDLSoftwareRenderer(surface);
				var sdlRenderer = renderer.Renderer;

				_ = SDL_SetRenderDrawBlendMode(sdlRenderer, EnableBlending
					? SDL_BlendMode.SDL_BLENDMODE_BLEND
					: SDL_BlendMode.SDL_BLENDMODE_NONE);

				var rect = new SDL_Rect { x = 0, y = 0, w = width, h = height };
				_ = SDL_RenderSetViewport(sdlRenderer, ref rect);
				_ = SDL_RenderSetClipRect(sdlRenderer, IntPtr.Zero);

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
							if (texId != IntPtr.Zero)
							{
								var userTex = (ImGuiUserTexture)GCHandle.FromIntPtr(texId).Target!;
								// skip this draw if it's the string output draw (we execute this at arbitrary points rather)
								if (userTex.Bitmap == _stringOutput)
								{
									continue;
								}

								var texBmpData = userTex.Bitmap.LockBits(
									new(0, 0, userTex.Bitmap.Width, userTex.Bitmap.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
								try
								{
									using var texSurf = new SDLSurface(texBmpData);
									var sdlTex = SDL_CreateTextureFromSurface(sdlRenderer, texSurf.Surface);
									if (sdlTex == IntPtr.Zero)
									{
										throw new($"Failed to create SDL texture from surface, SDL error: {SDL_GetError()}");
									}

									try
									{
										RenderCommand(sdlRenderer, sdlTex, _imGuiDrawList, cmd);
									}
									finally
									{
										SDL_DestroyTexture(sdlTex);
									}
								}
								finally
								{
									userTex.Bitmap.UnlockBits(texBmpData);
								}
							}
							else
							{
								RenderCommand(sdlRenderer, IntPtr.Zero, _imGuiDrawList, cmd);
							}
	
							break;
						}
						case DrawCallbackId.DisableBlending:
							_ = SDL_SetRenderDrawBlendMode(sdlRenderer, SDL_BlendMode.SDL_BLENDMODE_NONE);
							break;
						case DrawCallbackId.EnableBlending:
							_ = SDL_SetRenderDrawBlendMode(sdlRenderer, SDL_BlendMode.SDL_BLENDMODE_BLEND);
							break;
						case DrawCallbackId.DrawString:
						{
							var stringArgs = (DrawStringArgs)GCHandle.FromIntPtr(cmd.UserCallbackData).Target!;
							var brush = _resourceCache.CachedBrush;
							brush.Color = stringArgs.Color;
							_stringGraphics.TextRenderingHint = stringArgs.TextRenderingHint;
							_stringGraphics.DrawString(stringArgs.Str, stringArgs.Font, brush, stringArgs.X, stringArgs.Y, stringArgs.Format);

							// now draw the string graphics, if the next command is not another draw string command
							if (i == cmdBuffer.Size
								|| (DrawCallbackId)cmdBuffer[i + 1].UserCallback != DrawCallbackId.DrawString)
							{
								var lastCmd = cmdBuffer[cmdBuffer.Size - 1];
								var texId = lastCmd.GetTexID();

								// last command must be for drawing the string output bitmap
								var userTex = (ImGuiUserTexture)GCHandle.FromIntPtr(texId).Target!;
								if (userTex.Bitmap != _stringOutput)
								{
									throw new InvalidOperationException("Unexpected bitmap mismatch!");
								}

								var texBmpData = _stringOutput.LockBits(
									new(0, 0, _stringOutput.Width, _stringOutput.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
								try
								{
									using var texSurf = new SDLSurface(texBmpData);
									var sdlTex = SDL_CreateTextureFromSurface(sdlRenderer, texSurf.Surface);
									if (sdlTex == IntPtr.Zero)
									{
										throw new($"Failed to create SDL texture from surface, SDL error: {SDL_GetError()}");
									}

									try
									{
										// have to blend here, due to transparent texture usage
										if (!EnableBlending)
										{
											_ = SDL_SetRenderDrawBlendMode(sdlRenderer, SDL_BlendMode.SDL_BLENDMODE_BLEND);
										}

										RenderCommand(sdlRenderer, sdlTex, _imGuiDrawList, lastCmd);
									}
									finally
									{
										SDL_DestroyTexture(sdlTex);

										if (!EnableBlending)
										{
											_ = SDL_SetRenderDrawBlendMode(sdlRenderer, SDL_BlendMode.SDL_BLENDMODE_NONE);
										}
									}
								}
								finally
								{
									_stringOutput.UnlockBits(texBmpData);
								}

								ClearStringOutput();
							}

							break;
						}
						default:
							throw new InvalidOperationException();
					}
				}
			}
			finally
			{
				rt.SDBitmap.UnlockBits(bmpData);
			}
		}
	}
}

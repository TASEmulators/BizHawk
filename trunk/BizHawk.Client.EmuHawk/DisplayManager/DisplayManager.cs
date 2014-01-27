using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Client.Common;

using BizHawk.Bizware.BizwareGL;

namespace BizHawk.Client.EmuHawk
{
	public class DisplayFilterAnalysisReport
	{
		public bool Success;
		public Size OutputSize;
	}


	public class DisplayManager : IDisposable
	{
		public DisplayManager()
		{
			//have at least something here at the start
			luaNativeSurfacePreOSD = new BitmapBuffer(1, 1); 
		}

		readonly SwappableBitmapBufferSet sourceSurfaceSet = new SwappableBitmapBufferSet();

		public bool NeedsToPaint { get; set; }

		DisplaySurface luaEmuSurface = null;
		public void PreFrameUpdateLuaSource()
		{
			luaEmuSurface = luaEmuSurfaceSet.GetCurrent();
		}

		/// <summary>update Global.RenderPanel from the passed IVideoProvider</summary>
		public void UpdateSource(IVideoProvider videoProvider)
		{
			UpdateSourceEx(videoProvider, GlobalWin.PresentationPanel);
		}

		/// <summary>
		/// update the passed IRenderer with the passed IVideoProvider
		/// </summary>
		public void UpdateSourceEx(IVideoProvider videoProvider, PresentationPanel renderPanel)
		{
			var newPendingSurface = sourceSurfaceSet.AllocateSurface(videoProvider.BufferWidth, videoProvider.BufferHeight, false);
			newPendingSurface.AcceptIntArray(videoProvider.GetVideoBuffer());
			sourceSurfaceSet.SetPending(newPendingSurface);
		
			if (renderPanel == null) return;

			currNativeWidth = renderPanel.NativeSize.Width;
			currNativeHeight = renderPanel.NativeSize.Height;

			currentSourceSurface = sourceSurfaceSet.GetCurrent();

			if (currentSourceSurface == null) return;

			//if we're configured to use a scaling filter, apply it now
			//SHOULD THIS BE RUN REPEATEDLY?
			//some filters may need to run repeatedly (temporal interpolation, ntsc scanline field alternating)
			//but its sort of wasted work.
			CheckFilter();

			int w = currNativeWidth;
			int h = currNativeHeight;

			
			DisplaySurface luaSurface = luaNativeSurfaceSet.GetCurrent();

			//do we have anything to do?
			//bool complexComposite = false;
			//if (luaEmuSurface != null) complexComposite = true;
			//if (luaSurface != null) complexComposite = true;

			BitmapBuffer surfaceToRender = filteredSurface;
			if (surfaceToRender == null) surfaceToRender = currentSourceSurface;

			renderPanel.Clear(Color.FromArgb(videoProvider.BackgroundColor));
			renderPanel.Render(surfaceToRender);
			
			//GL TODO - lua unhooked
			if (luaEmuSurface != null)
			{
			  renderPanel.RenderOverlay(luaEmuSurface);
			}

			RenderOSD((IBlitter)renderPanel);

			renderPanel.Present();

			if (filteredSurface != null)
				filteredSurface.Dispose();
			filteredSurface = null;

			NeedsToPaint = false;
		}

		public bool Disposed { get; private set; }

		public void Dispose()
		{
			if (Disposed) return;
			Disposed = true;
		}

		BitmapBuffer currentSourceSurface, filteredSurface;

		//the surface to use to render a lua layer at native resolution (under the OSD)
		BitmapBuffer luaNativeSurfacePreOSD;


		SwappableDisplaySurfaceSet luaNativeSurfaceSet = new SwappableDisplaySurfaceSet();
		public void SetLuaSurfaceNativePreOSD(DisplaySurface surface) { luaNativeSurfaceSet.SetPending(surface); }
		public DisplaySurface GetLuaSurfaceNative()
		{
			return luaNativeSurfaceSet.AllocateSurface(currNativeWidth, currNativeHeight);
		}

		SwappableDisplaySurfaceSet luaEmuSurfaceSet = new SwappableDisplaySurfaceSet();
		public void SetLuaSurfaceEmu(DisplaySurface surface) { luaEmuSurfaceSet.SetPending(surface); }
		public DisplaySurface GetLuaEmuSurfaceEmu()
		{
			int width = 1, height = 1;
			if (currentSourceSurface != null)
				width = currentSourceSurface.Width;
			if (currentSourceSurface != null)
				height = currentSourceSurface.Height;
			return luaEmuSurfaceSet.AllocateSurface(width, height);
		}

		int currNativeWidth, currNativeHeight;
	

		/// <summary>
		/// suspends the display manager so that tricky things can be changed without the display thread going in and getting all confused and hating
		/// </summary>
		public void Suspend()
		{
		}

		/// <summary>
		/// resumes the display manager after a suspend
		/// </summary>
		public void Resume()
		{
		}

		void RenderOSD(IBlitter renderPanel)
		{
			GlobalWin.OSD.Begin(renderPanel);
			renderPanel.Open();
			GlobalWin.OSD.DrawScreenInfo(renderPanel);
			GlobalWin.OSD.DrawMessages(renderPanel);
			renderPanel.Close();
		}

		void CheckFilter()
		{
			IDisplayFilter filter = null;
			switch (Global.Config.TargetDisplayFilter)
			{
				//TODO GL - filters removed
				//case 0:
				//  //no filter
				//  break;
				//case 1:
				//  filter = new Hq2xBase_2xSai();
				//  break;
				//case 2:
				//  filter = new Hq2xBase_Super2xSai();
				//  break;
				//case 3:
				//  filter = new Hq2xBase_SuperEagle();
				//  break;
				//case 4:
				//  filter = new Scanlines2x();
				//  break;
			
			}
			if (filter == null)
				filteredSurface = null;
			else
				filteredSurface = filter.Execute(currentSourceSurface);
		}

		SwappableBitmapBufferSet nativeDisplaySurfaceSet = new SwappableBitmapBufferSet();

		//Thread displayThread;
	}
}
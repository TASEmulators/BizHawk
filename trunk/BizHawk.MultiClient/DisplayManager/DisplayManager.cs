using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace BizHawk.MultiClient
{
	public interface IDisplayFilter
	{
		/// <summary>
		/// describes how this filter will respond to an input format
		/// </summary>
		DisplayFilterAnalysisReport Analyze(Size sourceSize);

		/// <summary>
		/// runs the filter
		/// </summary>
		DisplaySurface Execute(DisplaySurface surface);
	}

	public class DisplayFilterAnalysisReport
	{
		public bool Success;
		public Size OutputSize;
	}

	public class DisplaySurface : IDisposable
	{
		/// <summary>
		/// returns a Graphics object used to render to this surface. be sure to dispose it!
		/// </summary>
		public Graphics GetGraphics()
		{
			Unlock();
			return Graphics.FromImage(bmp);
		}
		Bitmap bmp;
		BitmapData bmpdata;

		//TODO - lock and cache these
		public unsafe int* PixelPtr { get { return (int*)bmpdata.Scan0.ToPointer(); } }
		public IntPtr PixelIntPtr { get { return bmpdata.Scan0; } }
		public int Stride { get { return bmpdata.Stride; } }
		public int OffsetOf(int x, int y) { return y * Stride + x*4; }

		public unsafe void Clear()
		{
			Lock();
			Util.memset32(PixelPtr, 0, Stride * Height);
		}

		/// <summary>
		/// returns a bitmap which you can use but not hold onto.
		/// we may remove this later, as managing a Bitmap just for this may be a drag. (probably not though)
		/// </summary>
		public Bitmap PeekBitmap()
		{
			Unlock();
			return bmp;
		}

		public DisplaySurface(int width, int height)
		{
			//can't create a bitmap with zero dimensions, so for now, just bump it up to one
			if (width == 0) width = 1;
			if (height == 0) height = 1; 
			
			Width = width;
			Height = height;

			bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
			Lock();
		}

		/// <summary>
		/// returns a new surface 
		/// </summary>
		/// <param name="xpad"></param>
		/// <param name="ypad"></param>
		/// <returns></returns>
		public unsafe DisplaySurface ToPaddedSurface(int xpad0, int ypad0, int xpad1, int ypad1)
		{
			Lock();
			int new_width = Width + xpad0 + xpad1;
			int new_height = Height + ypad0 + ypad1;
			DisplaySurface ret = new DisplaySurface(new_width, new_height);
			ret.Lock();
			int* dptr = ret.PixelPtr;
			int* sptr = PixelPtr;
			int dstride = ret.Stride/4;
			int sstride = Stride/4;
			for (int y = 0; y < Height; y++)
				for (int x = 0; x < Width; x++)
				{
					dptr[(y + ypad0) * dstride + x + xpad0] = sptr[y * sstride + x];
				}
			return ret;
		}

		public int Width { get; private set; }
		public int Height { get; private set; }

		public void Dispose()
		{
			if (bmp != null)
				bmp.Dispose();
			bmp = null;
		}

		/// <summary>
		/// copies out the buffer as an int array (hopefully you can do this with a pointer instead and save some time!)
		/// </summary>
		public unsafe int[] ToIntArray()
		{
			Lock();

			int w = bmp.Width;
			int h = bmp.Height;
			var ret = new int[bmp.Width * bmp.Height];
			int* pData = (int*)bmpdata.Scan0.ToPointer();
			int stride = bmpdata.Stride / 4;
			for (int y = 0, i = 0; y < h; y++)
				for (int x = 0; x < w; x++)
					ret[i++] = pData[y * stride + x];

			return ret;
		}

		public unsafe void SetFromIntArray(int[] pixels)
		{
			Lock();

			if (Stride == Width * 4)
			{
				Marshal.Copy(pixels, 0, PixelIntPtr, Width * Height);
				return;
			}

			int w = Width;
			int h = Height;
			int* pData = PixelPtr;
			int stride = Stride / 4;
			for (int y = 0, i = 0; y < h; y++)
				for (int x = 0; x < w; x++)
					pData[y * stride + x] = pixels[i++];
		}

		/// <summary>
		/// locks this surface so that it can be accessed by raw pointer
		/// </summary>
		public void Lock()
		{
			if (bmpdata != null) return;
			var imageLockMode = ImageLockMode.ReadWrite;
			bmpdata = bmp.LockBits(new Rectangle(0, 0, Width, Height), imageLockMode, PixelFormat.Format32bppArgb);
		}

		public bool IsLocked { get { return bmpdata != null; } }

		public void Unlock()
		{
			if (bmpdata != null)
				bmp.UnlockBits(bmpdata);
			bmpdata = null;
		}

		public static unsafe DisplaySurface FromVideoProvider(IVideoProvider provider)
		{
			int w = provider.BufferWidth;
			int h = provider.BufferHeight;
			int[] buffer = provider.GetVideoBuffer();
			var ret = new DisplaySurface(w,h);
			int* pData = ret.PixelPtr;
			int stride = ret.Stride / 4;
			for (int y = 0, i=0; y < h; y++)
				for (int x = 0; x < w; x++)
					pData[y * stride + x] = buffer[i++];
			return ret;
		}
	}


	public class OSDManager
	{
		public string FPS { get; set; }
		public string MT { get; set; }
		private Font MessageFont;
		private Font AlertFont;
		public void Dispose()
		{
			if (MessageFont != null)
			{
				MessageFont.Dispose();
				MessageFont = null;
			}
			if (AlertFont != null)
			{
				AlertFont.Dispose();
				AlertFont = null;
			}
		}
		public OSDManager()
		{
			//MessageFont = new Font(Device, 16, 0, FontWeight.Bold, 1, false, CharacterSet.Default, Precision.Default, FontQuality.Default, PitchAndFamily.Default | PitchAndFamily.DontCare, "Courier");
			//AlertFont = new Font(Device, 16, 0, FontWeight.ExtraBold, 1, true, CharacterSet.Default, Precision.Default, FontQuality.Default, PitchAndFamily.Default | PitchAndFamily.DontCare, "Courier");
			MessageFont = new Font("Courier", 14, FontStyle.Bold, GraphicsUnit.Pixel);
			AlertFont = new Font("Courier", 14, FontStyle.Bold, GraphicsUnit.Pixel);
		}

		private float GetX(Graphics g, int x, int anchor, Font font, string message)
		{
			var size = g.MeasureString(message, font);
			//Rectangle rect = g.MeasureString(Sprite, message, new DrawTextFormat());
			switch (anchor)
			{
				default:
				case 0: //Top Left
				case 2: //Bottom Left
					return x;
				case 1: //Top Right
				case 3: //Bottom Right
					return g.ClipBounds.Width - x - size.Width;
			}
		}

		private float GetY(Graphics g, int y, int anchor, Font font, string message)
		{
			var size = g.MeasureString(message, font);
			switch (anchor)
			{
				default:
				case 0: //Top Left
				case 1: //Top Right
					return y;
				case 2: //Bottom Left
				case 3: //Bottom Right
					return g.ClipBounds.Height - y - size.Height;
			}
		}



		private string MakeFrameCounter()
		{
			if (Global.MovieSession.Movie.Mode == MOVIEMODE.FINISHED)
			{
				return Global.Emulator.Frame.ToString() + "/" + Global.MovieSession.Movie.Length().ToString() + " (Finished)";
			}
			else if (Global.MovieSession.Movie.Mode == MOVIEMODE.PLAY)
			{
				return Global.Emulator.Frame.ToString() + "/" + Global.MovieSession.Movie.Length().ToString();
			}
			else if (Global.MovieSession.Movie.Mode == MOVIEMODE.RECORD)
				return Global.Emulator.Frame.ToString();
			else
			{
				return Global.Emulator.Frame.ToString();
			}
		}

		private string MakeLagCounter()
		{
			return Global.Emulator.LagCount.ToString();
		}

		private List<UIMessage> messages = new List<UIMessage>(5);
		private List<UIDisplay> GUITextList = new List<UIDisplay>();

		public void AddMessage(string message)
		{
			messages.Add(new UIMessage { Message = message, ExpireAt = DateTime.Now + TimeSpan.FromSeconds(2) });
		}

        public void AddGUIText(string message, int x, int y, bool alert, Brush BackGround, Brush ForeColor, int anchor)
		{
            GUITextList.Add(new UIDisplay { Message = message, X = x, Y = y, BackGround = BackGround, ForeColor = ForeColor, Alert = alert, Anchor = anchor });
		}

		public void ClearGUIText()
		{
			GUITextList.Clear();
		}


		public void DrawMessages(Graphics g)
		{
			//todo - not so much brush object churn?

			messages.RemoveAll(m => DateTime.Now > m.ExpireAt);
			DrawScreenInfo(g);
			int line = 1;
			for (int i = messages.Count - 1; i >= 0; i--, line++)
			{
				float x = 3;
				float y = g.ClipBounds.Height - (line * 18);
				g.DrawString(messages[i].Message, MessageFont, Brushes.Black, x + 2, y + 2);
				using(var brush = new SolidBrush(Color.FromArgb(Global.Config.MessagesColor)))
					g.DrawString(messages[i].Message, MessageFont, brush, x, y);
			}
			for (int x = 0; x < GUITextList.Count; x++)
			{
                try
                {
				    float posx = GetX(g, GUITextList[x].X, GUITextList[x].Anchor, MessageFont, GUITextList[x].Message);
				    float posy = GetY(g, GUITextList[x].Y, GUITextList[x].Anchor, MessageFont, GUITextList[x].Message);

				    g.DrawString(GUITextList[x].Message, MessageFont, GUITextList[x].BackGround, posx + 2, posy + 2);
				    g.DrawString(GUITextList[x].Message, MessageFont, Brushes.Gray, posx + 1, posy + 1);

				    if (GUITextList[x].Alert)
					    using(var brush = new SolidBrush(Color.FromArgb(Global.Config.AlertMessageColor)))
						    g.DrawString(GUITextList[x].Message, MessageFont, brush, posx,posy);
				    else
                        g.DrawString(GUITextList[x].Message, MessageFont, GUITextList[x].ForeColor, posx, posy);
			    }
                catch (Exception e)
                {
                    return;
                }
            }
		}


		public string MakeInputDisplay()
		{
			StringBuilder s;
			if (Global.MovieSession.Movie.Mode == MOVIEMODE.INACTIVE || Global.MovieSession.Movie.Mode == MOVIEMODE.FINISHED)
				s = new StringBuilder(Global.GetOutputControllersAsMnemonic());
			else
				s = new StringBuilder(Global.MovieSession.Movie.GetInputFrame(Global.Emulator.Frame - 1));
			s.Replace(".", " ").Replace("|", "").Replace("l", ""); //If l is ever a mnemonic this will squash it.
			
			return s.ToString();
		}

		public string MakeRerecordCount()
		{
			if (Global.MovieSession.Movie.Mode != MOVIEMODE.INACTIVE)
				return "Rerecord Count: " + Global.MovieSession.Movie.Rerecords.ToString();
			else
				return "";
		}

		/// <summary>
		/// Display all screen info objects like fps, frame counter, lag counter, and input display
		/// </summary>
		public void DrawScreenInfo(Graphics g)
		{
			if (Global.Config.DisplayFrameCounter)
			{
				string message = MakeFrameCounter();
				float x = GetX(g, Global.Config.DispFrameCx, Global.Config.DispFrameanchor, MessageFont, message);
				float y = GetY(g, Global.Config.DispFrameCy, Global.Config.DispFrameanchor, MessageFont, message);
				g.DrawString(message, MessageFont, Brushes.Black, x + 1, y + 1);
				using(var brush = new SolidBrush(Color.FromArgb(Global.Config.MessagesColor)))
					g.DrawString(message, MessageFont, brush, x, y );
			}
			if (Global.Config.DisplayInput)
			{
				string input = MakeInputDisplay();
				Color c;
				float x = GetX(g, Global.Config.DispInpx, Global.Config.DispInpanchor, MessageFont, input);
				float y = GetY(g, Global.Config.DispInpy, Global.Config.DispInpanchor, MessageFont, input);
				if (Global.MovieSession.Movie.Mode == MOVIEMODE.PLAY)
				{
					c = Color.FromArgb(Global.Config.MovieInput);
				}
				else
					c = Color.FromArgb(Global.Config.MessagesColor);

				g.DrawString(input, MessageFont, Brushes.Black, x+1,y+1);
				using(var brush = new SolidBrush(c))
					g.DrawString(input, MessageFont, brush, x,y);
			}
			if (Global.MovieSession.MultiTrack.IsActive)
			{
				g.DrawString(MT, MessageFont, Brushes.Black,
				Global.Config.DispFPSx + 1, //TODO: Multitrack position variables
					Global.Config.DispFPSy + 1);
				using(var brush = new SolidBrush(Color.FromArgb(Global.Config.MessagesColor)))
					g.DrawString(MT, MessageFont, Brushes.Black,
						Global.Config.DispFPSx, //TODO: Multitrack position variables
						Global.Config.DispFPSy);
			}
			if (Global.Config.DisplayFPS && FPS != null)
			{
				float x = GetX(g, Global.Config.DispFPSx, Global.Config.DispFPSanchor, MessageFont, FPS);
				float y = GetY(g, Global.Config.DispFPSy, Global.Config.DispFPSanchor, MessageFont, FPS);
				g.DrawString(FPS, MessageFont, Brushes.Black, x + 1, y + 1);
				using(var brush = new SolidBrush(Color.FromArgb(Global.Config.MessagesColor)))
					g.DrawString(FPS, MessageFont, brush, x, y);
			}

			if (Global.Config.DisplayLagCounter)
			{
				string counter = MakeLagCounter();

				if (Global.Emulator.IsLagFrame)
				{
					float x = GetX(g, Global.Config.DispLagx, Global.Config.DispLaganchor, AlertFont, counter);
					float y = GetY(g, Global.Config.DispLagy, Global.Config.DispLaganchor, AlertFont, counter);
					g.DrawString(MakeLagCounter(), AlertFont, Brushes.Black,
					Global.Config.DispLagx + 1,
						Global.Config.DispLagy + 1);
					using(var brush = new SolidBrush(Color.FromArgb(Global.Config.AlertMessageColor)))
						g.DrawString(MakeLagCounter(), AlertFont, brush,
						Global.Config.DispLagx,
							Global.Config.DispLagy);
				}
				else
				{
					float x = GetX(g, Global.Config.DispLagx, Global.Config.DispLaganchor, MessageFont, counter);
					float y = GetY(g, Global.Config.DispLagy, Global.Config.DispLaganchor, MessageFont, counter);
					g.DrawString(counter, MessageFont, Brushes.Black, x + 1, y + 1);
					using(var brush = new SolidBrush(Color.FromArgb(Global.Config.MessagesColor)))
						g.DrawString(counter, MessageFont, brush, x , y );
				}

			}
			if (Global.Config.DisplayRerecordCount)
			{
				string rerec = MakeRerecordCount();
				float x = GetX(g, Global.Config.DispRecx, Global.Config.DispRecanchor, MessageFont, rerec);
				float y = GetY(g, Global.Config.DispRecy, Global.Config.DispRecanchor, MessageFont, rerec);
				g.DrawString(rerec, MessageFont, Brushes.Black, x + 1, y + 1);
				using(var brush = new SolidBrush(Color.FromArgb(Global.Config.MessagesColor)))
				g.DrawString(rerec, MessageFont, brush, x , y);
			}

			if (Global.MovieSession.Movie.Mode == MOVIEMODE.PLAY)
			{
				
				int r = (int)g.ClipBounds.Width;
				Point[] p = { new Point(r - 20, 2), 
								new Point(r - 4, 12), 
								new Point(r - 20, 22) };
				g.FillPolygon(new SolidBrush(Color.Red), p);
				g.DrawPolygon(new Pen(new SolidBrush(Color.Pink)), p);

			}
			else if (Global.MovieSession.Movie.Mode == MOVIEMODE.RECORD)
			{
				g.FillEllipse(new SolidBrush(Color.Red), new Rectangle((int)g.ClipBounds.Width - 22, 2, 20, 20));
				g.DrawEllipse(new Pen(new SolidBrush(Color.Pink)), new Rectangle((int)g.ClipBounds.Width - 22, 2, 20, 20));
			}

			if (Global.MovieSession.Movie.Mode != MOVIEMODE.INACTIVE && Global.Config.DisplaySubtitles)
			{
				
				List<Subtitle> s = Global.MovieSession.Movie.Subtitles.GetSubtitles(Global.Emulator.Frame);
				if (s == null) return;
				for (int i = 0; i < s.Count; i++)
				{
					g.DrawString(s[i].Message, MessageFont, Brushes.Black,
						s[i].X + 1, s[i].Y + 1);
					using (var brush = new SolidBrush(Color.FromArgb((int)s[i].Color)))
						g.DrawString(s[i].Message, MessageFont, brush,
											s[i].X, s[i].Y);
				}
			}
			 
		}
	}

	public class VideoProviderData : IVideoProvider
	{
		public int[] VideoBuffer;

		public int BufferWidth { get; set; }
		public int BufferHeight { get; set; }
		public int BackgroundColor { get; set; }

		public int[] GetVideoBuffer() { return VideoBuffer; }
	}

	public class DisplayManager : IDisposable
	{
		public DisplayManager()
		{
			//have at least something here at the start
			luaNativeSurfacePreOSD = new DisplaySurface(1, 1); 
			
			wakeupEvent = new EventWaitHandle(false, EventResetMode.AutoReset);
			suspendReplyEvent = new EventWaitHandle(false, EventResetMode.AutoReset);
			displayThread = new Thread(ThreadProc);
			displayThread.IsBackground = true;
			displayThread.Start();
		}

		SwappableDisplaySurfaceSet sourceSurfaceSet = new SwappableDisplaySurfaceSet();
		public void UpdateSource(IVideoProvider videoProvider)
		{
			var newPendingSurface = sourceSurfaceSet.AllocateSurface(videoProvider.BufferWidth, videoProvider.BufferHeight, false);
			newPendingSurface.SetFromIntArray(videoProvider.GetVideoBuffer());
			sourceSurfaceSet.SetPending(newPendingSurface);
			wakeupEvent.Set();
		}

		public bool Disposed { get; private set; }

		public void Dispose()
		{
			if (Disposed) return;
			shutdownFlag = true;
			while (shutdownFlag) Thread.Sleep(1);
			wakeupEvent.Dispose();
			Disposed = true;
		}

		DisplaySurface currentSourceSurface;

		//the surface to use to render a lua layer at native resolution (under the OSD)
		DisplaySurface luaNativeSurfacePreOSD;

		/// <summary>
		/// encapsulates thread-safe concept of pending/current display surfaces, reusing buffers where matching 
		/// sizes are available and keeping them cleaned up when they dont seem like theyll need to be used anymore
		/// </summary>
		class SwappableDisplaySurfaceSet
		{
			DisplaySurface Pending, Current;
			Queue<DisplaySurface> ReleasedSurfaces = new Queue<DisplaySurface>();

			/// <summary>
			/// retrieves a surface with the specified size, reusing an old buffer if available and clearing if requested
			/// </summary>
			public DisplaySurface AllocateSurface(int width, int height, bool needsClear=true)
			{
				for(;;) 
				{
					DisplaySurface trial;
					lock (this)
					{
						if (ReleasedSurfaces.Count == 0) break;
						trial = ReleasedSurfaces.Dequeue();
					}
					if (trial.Width == width && trial.Height == height)
					{
						if(needsClear) trial.Clear();
						return trial;
					}
					trial.Dispose();
				}
				return new DisplaySurface(width, height);
			}

			/// <summary>
			/// sets the provided buffer as pending. takes control of the supplied buffer
			/// </summary>
			public void SetPending(DisplaySurface newPending)
			{
				lock(this)
				{
					if (Pending != null) ReleasedSurfaces.Enqueue(Pending);
					Pending = newPending;
				}
			}

			public void ReleaseSurface(DisplaySurface surface)
			{
				lock (this) ReleasedSurfaces.Enqueue(surface);
			}

			/// <summary>
			/// returns the current buffer, making the most recent pending buffer (if there is such) as the new current first.
			/// </summary>
			public DisplaySurface GetCurrent()
			{
				lock(this)
				{
					if(Pending != null)
					{
						if (Current != null) ReleasedSurfaces.Enqueue(Current);
						Current = Pending;
						Pending = null;
					}
				}
				return Current;
			}
		}

		SwappableDisplaySurfaceSet luaNativeSurfaceSet = new SwappableDisplaySurfaceSet();
		public void SetLuaSurfaceNativePreOSD(DisplaySurface surface) { luaNativeSurfaceSet.SetPending(surface); }
		public DisplaySurface GetLuaSurfaceNative()
		{
			return luaNativeSurfaceSet.AllocateSurface(currNativeWidth, currNativeHeight);
		}

		int currNativeWidth, currNativeHeight;
		EventWaitHandle wakeupEvent, suspendReplyEvent;
		bool shutdownFlag, suspendFlag;
		void ThreadProc()
		{
			for (; ; )
			{
				Display();

				//wait until we receive something interesting, or just a little while anyway
				wakeupEvent.WaitOne(10);

				if (suspendFlag)
				{
					suspendFlag = false;
					suspendReplyEvent.Set();
					wakeupEvent.WaitOne();
					suspendReplyEvent.Set();
					wakeupEvent.WaitOne();
					suspendReplyEvent.Set();
				}

				if (shutdownFlag) break;
			}
			shutdownFlag = false;
		}

		/// <summary>
		/// suspends the display manager so that tricky things can be changed without the display thread going in and getting all confused and hating
		/// </summary>
		public void Suspend()
		{
			suspendFlag = true;
			wakeupEvent.Set();
			suspendReplyEvent.WaitOne();
			wakeupEvent.Set();
			suspendReplyEvent.WaitOne();
		}

		/// <summary>
		/// resumes the display manager after a suspend
		/// </summary>
		public void Resume()
		{
			wakeupEvent.Set();
			suspendReplyEvent.WaitOne();
		}


		SwappableDisplaySurfaceSet nativeDisplaySurfaceSet = new SwappableDisplaySurfaceSet();

		/// <summary>
		/// internal display worker proc; runs through the multiply layered display pipeline 
		/// </summary>
		void Display()
		{
			var renderPanel = Global.RenderPanel;
			if (renderPanel == null) return;

			currNativeWidth = Global.RenderPanel.NativeSize.Width;
			currNativeHeight = Global.RenderPanel.NativeSize.Height;

			//if we're configured to use a scaling filter, apply it now
			//SHOULD THIS BE RUN REPEATEDLY?
			//some filters may need to run repeatedly (temporal interpolation, ntsc scanline field alternating)
			//but its sort of wasted work.
			//IDisplayFilter filter = new Hq2xBase_Super2xSai();
			//var tempSurface = filter.Execute(currentSourceSurface);
			//currentSourceSurface.Dispose();
			//currentSourceSurface = tempSurface;

			currentSourceSurface = sourceSurfaceSet.GetCurrent();

			if (currentSourceSurface == null) return;

			int w = currNativeWidth;
			int h = currNativeHeight;
			var nativeBmp = nativeDisplaySurfaceSet.AllocateSurface(w, h, true);
			using (var g = Graphics.FromImage(nativeBmp.PeekBitmap()))
			{
				//scale the source bitmap to the desired size of the render panel
				g.PixelOffsetMode = PixelOffsetMode.HighSpeed;
				g.InterpolationMode = InterpolationMode.NearestNeighbor;
				g.CompositingMode = CompositingMode.SourceCopy;
				g.CompositingQuality = CompositingQuality.HighSpeed;
				g.DrawImage(currentSourceSurface.PeekBitmap(), 0, 0, w, h);
				g.Clip = new Region(new Rectangle(0, 0, nativeBmp.Width, nativeBmp.Height));

				//switch to fancier composition for OSD overlays and such
				g.CompositingMode = CompositingMode.SourceOver;

				//apply a lua layer
				var luaSurface = luaNativeSurfaceSet.GetCurrent();
				if (luaSurface != null) g.DrawImageUnscaled(luaSurface.PeekBitmap(), 0, 0);
				//although we may want to change this if we want to fade out messages or have some other fancy alpha faded gui stuff

				//draw the OSD at native resolution
				Global.OSD.DrawScreenInfo(g);
				Global.OSD.DrawMessages(g);
				g.Clip.Dispose();
			}

			//send the native resolution image to the render panel
			Global.RenderPanel.Render(nativeBmp);

			//release the native resolution image
			nativeDisplaySurfaceSet.ReleaseSurface(nativeBmp);
		}

		Thread displayThread;
	}
}
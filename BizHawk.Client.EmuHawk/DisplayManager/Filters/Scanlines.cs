using System;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

//what license is this?? who knows??
//ref: http://vba-rerecording.googlecode.com/svn/trunk/src/2xsai.cpp

namespace BizHawk.Client.EmuHawk
{
	public class Scanlines2x : IDisplayFilter
	{
		public DisplayFilterAnalysisReport Analyze(Size sourceSize)
		{
			var ret = new DisplayFilterAnalysisReport();
			ret.Success = true;
			ret.OutputSize = new Size(sourceSize.Width * 2, sourceSize.Height * 2);
			return ret;
		}

		public unsafe DisplaySurface Execute(DisplaySurface surface)
		{
			int w = surface.Width;
			int h = surface.Height;
			var ret = new DisplaySurface(w * 2, h * 2);
			RunScanlines((byte*)surface.PixelPtr, surface.Stride, (byte*)ret.PixelPtr, ret.Stride, w, h);
			return ret;
		}

		unsafe static void RunScanlines(byte* srcPtr, int srcPitch, byte* destPtr, int dstPitch, int width, int height)
		{
			byte* srcLine = srcPtr;
			for (int y = 0; y < height; y++)
			{
				byte *s = srcLine;
				//first copied line (2x width)
				for (int x = 0; x < width; x++)
				{
					*destPtr++ = s[0];
					*destPtr++ = s[1];
					*destPtr++ = s[2];
					*destPtr++ = s[3];
					*destPtr++ = s[0];
					*destPtr++ = s[1];
					*destPtr++ = s[2];
					*destPtr++ = s[3];
					s += 4;
				}

				destPtr += dstPitch - width*2 * 4;
				s = srcLine;
				//second copied line (2x width, 25%)
				for (int x = 0; x < width; x++)
				{
					*destPtr++ = (byte)((s[0]*3) >> 2);
					*destPtr++ = (byte)((s[1]*3) >> 2);
					*destPtr++ = (byte)((s[2]*3) >> 2);
					*destPtr++ = s[3];
					*destPtr++ = (byte)((s[0]*3) >> 2);
					*destPtr++ = (byte)((s[1]*3) >> 2);
					*destPtr++ = (byte)((s[2]*3) >> 2);
					*destPtr++ = s[3];
					s += 4;
				}
				srcLine += srcPitch;
				destPtr += dstPitch - width * 2 * 4;
			}
				
		}
		


	}
}
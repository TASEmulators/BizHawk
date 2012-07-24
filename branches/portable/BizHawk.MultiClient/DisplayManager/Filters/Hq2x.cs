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

namespace BizHawk.MultiClient
{

	class Hq2xBase_2xSai : Hq2xBase {}
	class Hq2xBase_Super2xSai : Hq2xBase { }
	class Hq2xBase_SuperEagle : Hq2xBase { }

	public abstract class Hq2xBase : IDisplayFilter
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
			using (var padded = surface.ToPaddedSurface(1, 1, 2, 2))
			{
				if(this is Hq2xBase_2xSai) _2xSaI32((byte*)padded.PixelPtr + padded.OffsetOf(1, 1), (uint)(padded.Stride), null, (byte*)ret.PixelPtr, (uint)(ret.Stride), (uint)w, (uint)h);
				if (this is Hq2xBase_Super2xSai) Super2xSaI32((byte*)padded.PixelPtr + padded.OffsetOf(1, 1), (uint)(padded.Stride), null, (byte*)ret.PixelPtr, (uint)(ret.Stride), (uint)w, (uint)h);
				if (this is Hq2xBase_SuperEagle) SuperEagle32((byte*)padded.PixelPtr + padded.OffsetOf(1, 1), (uint)(padded.Stride), null, (byte*)ret.PixelPtr, (uint)(ret.Stride), (uint)w, (uint)h);
				padded.Dispose();
			}
			return ret;
		}
		
		static int GetResult1 (uint A, uint B, uint C, uint D, uint E )
		{
				int x = 0;
				int y = 0;
				int r = 0;

				if (A == C)
					x += 1;
				else if (B == C)
					y += 1;
				if (A == D)
					x += 1;
				else if (B == D)
					y += 1;
				if (x <= 1)
					r += 1;
				if (y <= 1)
					r -= 1;
				return r;
		}


		static int GetResult2 (uint A, uint B, uint C, uint D, uint E )
		{
			int x = 0;
			int y = 0;
			int r = 0;

			if (A == C)
				x += 1;
			else if (B == C)
				y += 1;
			if (A == D)
				x += 1;
			else if (B == D)
				y += 1;
			if (x <= 1)
				r -= 1;
			if (y <= 1)
				r += 1;
			return r;
		}

		static int GetResult (uint A, uint B, uint C, uint D)
		{
			int x = 0;
			int y = 0;
			int r = 0;

			if (A == C)
				x += 1;
			else if (B == C)
				y += 1;
			if (A == D)
				x += 1;
			else if (B == D)
				y += 1;
			if (x <= 1)
				r += 1;
			if (y <= 1)
				r -= 1;
			return r;
		}

		const uint colorMask = 0xfefefe;
		const uint lowPixelMask = 0x010101;
		const uint qcolorMask = 0xfcfcfc;
		const uint qlowpixelMask = 0x030303;
		const uint redblueMask = 0xF81F;
		const uint greenMask = 0x7E0;

static uint INTERPOLATE (uint A, uint B)
{
  if (A != B) {
    return (((A & colorMask) >> 1) + ((B & colorMask) >> 1) +
            (A & B & lowPixelMask));
  } else
    return A;
}

static uint Q_INTERPOLATE(uint A, uint B, uint C, uint D)
{
  uint x = ((A & qcolorMask) >> 2) +
    ((B & qcolorMask) >> 2) +
    ((C & qcolorMask) >> 2) + ((D & qcolorMask) >> 2);
  uint y = (A & qlowpixelMask) +
    (B & qlowpixelMask) + (C & qlowpixelMask) + (D & qlowpixelMask);

  y = (y >> 2) & qlowpixelMask;
  return x + y;
}


unsafe static void _2xSaI32 (byte *srcPtr, uint srcPitch, byte * deltaPtr ,
               byte *dstPtr, uint dstPitch, uint width, uint height)
{
  uint *dP;
  uint  *bP;
  uint inc_bP = 1;

  uint Nextline = srcPitch >> 2;

  for (; height!=0; height--) {
    bP = (uint *) srcPtr;
    dP = (uint *) dstPtr;

    for (uint finish = width; finish!=0; finish -= inc_bP) {
      uint colorA, colorB;
      uint colorC, colorD,
        colorE, colorF, colorG, colorH,
        colorI, colorJ, colorK, colorL,

        colorM, colorN, colorO, colorP;
      uint product, product1, product2;

      //---------------------------------------
      // Map of the pixels:                    I|E F|J
      //                                       G|A B|K
      //                                       H|C D|L
      //                                       M|N O|P
      colorI = *(bP - Nextline - 1);
      colorE = *(bP - Nextline);
      colorF = *(bP - Nextline + 1);
      colorJ = *(bP - Nextline + 2);

      colorG = *(bP - 1);
      colorA = *(bP);
      colorB = *(bP + 1);
      colorK = *(bP + 2);

      colorH = *(bP + Nextline - 1);
      colorC = *(bP + Nextline);
      colorD = *(bP + Nextline + 1);
      colorL = *(bP + Nextline + 2);

      colorM = *(bP + Nextline + Nextline - 1);
      colorN = *(bP + Nextline + Nextline);
      colorO = *(bP + Nextline + Nextline + 1);
      colorP = *(bP + Nextline + Nextline + 2);

      if ((colorA == colorD) && (colorB != colorC)) {
        if (((colorA == colorE) && (colorB == colorL)) ||
            ((colorA == colorC) && (colorA == colorF)
             && (colorB != colorE) && (colorB == colorJ))) {
          product = colorA;
        } else {
          product = INTERPOLATE (colorA, colorB);
        }

        if (((colorA == colorG) && (colorC == colorO)) ||
            ((colorA == colorB) && (colorA == colorH)
             && (colorG != colorC) && (colorC == colorM))) {
          product1 = colorA;
        } else {
          product1 = INTERPOLATE (colorA, colorC);
        }
        product2 = colorA;
      } else if ((colorB == colorC) && (colorA != colorD)) {
        if (((colorB == colorF) && (colorA == colorH)) ||
            ((colorB == colorE) && (colorB == colorD)
             && (colorA != colorF) && (colorA == colorI))) {
          product = colorB;
        } else {
          product = INTERPOLATE (colorA, colorB);
        }

        if (((colorC == colorH) && (colorA == colorF)) ||
            ((colorC == colorG) && (colorC == colorD)
             && (colorA != colorH) && (colorA == colorI))) {
          product1 = colorC;
        } else {
          product1 = INTERPOLATE (colorA, colorC);
        }
        product2 = colorB;
      } else if ((colorA == colorD) && (colorB == colorC)) {
        if (colorA == colorB) {
          product = colorA;
          product1 = colorA;
          product2 = colorA;
        } else {
          int r = 0;

          product1 = INTERPOLATE (colorA, colorC);
          product = INTERPOLATE (colorA, colorB);

          r += GetResult1 (colorA, colorB, colorG, colorE, colorI);
          r += GetResult2 (colorB, colorA, colorK, colorF, colorJ);
          r += GetResult2 (colorB, colorA, colorH, colorN, colorM);
          r += GetResult1 (colorA, colorB, colorL, colorO, colorP);

          if (r > 0)
            product2 = colorA;
          else if (r < 0)
            product2 = colorB;
          else {
            product2 = Q_INTERPOLATE (colorA, colorB, colorC, colorD);
          }
        }
      } else {
        product2 = Q_INTERPOLATE (colorA, colorB, colorC, colorD);

        if ((colorA == colorC) && (colorA == colorF)
            && (colorB != colorE) && (colorB == colorJ)) {
          product = colorA;
        } else if ((colorB == colorE) && (colorB == colorD)
                   && (colorA != colorF) && (colorA == colorI)) {
          product = colorB;
        } else {
          product = INTERPOLATE (colorA, colorB);
        }

        if ((colorA == colorB) && (colorA == colorH)
            && (colorG != colorC) && (colorC == colorM)) {
          product1 = colorA;
        } else if ((colorC == colorG) && (colorC == colorD)
                   && (colorA != colorH) && (colorA == colorI)) {
          product1 = colorC;
        } else {
          product1 = INTERPOLATE (colorA, colorC);
        }
      }
			*(dP) = colorA | 0xFF000000;
			*(dP + 1) = product | 0xFF000000;
			*(dP + (dstPitch >> 2)) = product1 | 0xFF000000;
			*(dP + (dstPitch >> 2) + 1) = product2 | 0xFF000000;

      bP += inc_bP;
      dP += 2;
    }                 // end of for ( finish= width etc..)

    srcPtr += srcPitch;
    dstPtr += dstPitch * 2;
//    deltaPtr += srcPitch;
  }                   // endof: for (height; height; height--)
}

		
unsafe void SuperEagle32 (byte *srcPtr, uint srcPitch, byte * deltaPtr,
									 byte* dstPtr, uint dstPitch, uint width, uint height)
{
  uint  *dP;
  uint *bP;
//  uint *xP;
  uint inc_bP;

  inc_bP = 1;

  uint Nextline = srcPitch >> 2;

  for (; height!=0; height--) {
    bP = (uint *) srcPtr;
//    xP = (uint *) deltaPtr;
    dP = (uint *)dstPtr;
    for (uint finish = width; finish!=0; finish -= inc_bP) {
      uint color4, color5, color6;
      uint color1, color2, color3;
      uint colorA1, colorA2, colorB1, colorB2, colorS1, colorS2;
      uint product1a, product1b, product2a, product2b;

      colorB1 = *(bP - Nextline);
      colorB2 = *(bP - Nextline + 1);

      color4 = *(bP - 1);
      color5 = *(bP);
      color6 = *(bP + 1);
      colorS2 = *(bP + 2);

      color1 = *(bP + Nextline - 1);
      color2 = *(bP + Nextline);
      color3 = *(bP + Nextline + 1);
      colorS1 = *(bP + Nextline + 2);

      colorA1 = *(bP + Nextline + Nextline);
      colorA2 = *(bP + Nextline + Nextline + 1);

      // --------------------------------------
      if (color2 == color6 && color5 != color3) {
        product1b = product2a = color2;
        if ((color1 == color2) || (color6 == colorB2)) {
          product1a = INTERPOLATE (color2, color5);
          product1a = INTERPOLATE (color2, product1a);
          //                       product1a = color2;
        } else {
          product1a = INTERPOLATE (color5, color6);
        }

        if ((color6 == colorS2) || (color2 == colorA1)) {
          product2b = INTERPOLATE (color2, color3);
          product2b = INTERPOLATE (color2, product2b);
          //                       product2b = color2;
        } else {
          product2b = INTERPOLATE (color2, color3);
        }
      } else if (color5 == color3 && color2 != color6) {
        product2b = product1a = color5;

        if ((colorB1 == color5) || (color3 == colorS1)) {
          product1b = INTERPOLATE (color5, color6);
          product1b = INTERPOLATE (color5, product1b);
          //                       product1b = color5;
        } else {
          product1b = INTERPOLATE (color5, color6);
        }

        if ((color3 == colorA2) || (color4 == color5)) {
          product2a = INTERPOLATE (color5, color2);
          product2a = INTERPOLATE (color5, product2a);
          //                       product2a = color5;
        } else {
          product2a = INTERPOLATE (color2, color3);
        }

      } else if (color5 == color3 && color2 == color6) {
        int r = 0;

        r += GetResult (color6, color5, color1, colorA1);
        r += GetResult (color6, color5, color4, colorB1);
        r += GetResult (color6, color5, colorA2, colorS1);
        r += GetResult (color6, color5, colorB2, colorS2);

        if (r > 0) {
          product1b = product2a = color2;
          product1a = product2b = INTERPOLATE (color5, color6);
        } else if (r < 0) {
          product2b = product1a = color5;
          product1b = product2a = INTERPOLATE (color5, color6);
        } else {
          product2b = product1a = color5;
          product1b = product2a = color2;
        }
      } else {
        product2b = product1a = INTERPOLATE (color2, color6);
        product2b =
          Q_INTERPOLATE (color3, color3, color3, product2b);
        product1a =
          Q_INTERPOLATE (color5, color5, color5, product1a);

        product2a = product1b = INTERPOLATE (color5, color3);
        product2a =
          Q_INTERPOLATE (color2, color2, color2, product2a);
        product1b =
          Q_INTERPOLATE (color6, color6, color6, product1b);

        //                    product1a = color5;
        //                    product1b = color6;
        //                    product2a = color2;
        //                    product2b = color3;
      }
      *(dP) = product1a | 0xFF000000;
      *(dP+1) = product1b | 0xFF000000;
      *(dP + (dstPitch >> 2)) = product2a | 0xFF000000;
      *(dP + (dstPitch >> 2) +1) = product2b | 0xFF000000;
//      *xP = color5;

      bP += inc_bP;
//      xP += inc_bP;
      dP += 2;
    }                 // end of for ( finish= width etc..)

    srcPtr += srcPitch;
    dstPtr += dstPitch * 2;
//    deltaPtr += srcPitch;
  }                   // endof: for (height; height; height--)
}

		
unsafe void Super2xSaI32 (byte *srcPtr, uint srcPitch,
                   byte * deltaPtr , byte *dstPtr, uint dstPitch,
                   uint width, uint height)
{
  uint *bP;
  uint *dP;
  uint inc_bP;
  uint Nextline = srcPitch >> 2;
  inc_bP = 1;

  for (; height!=0; height--) {
    bP = (uint *) srcPtr;
    dP = (uint *) dstPtr;

    for (uint finish = width; finish!=0; finish -= inc_bP) {
      uint color4, color5, color6;
      uint color1, color2, color3;
      uint colorA0, colorA1, colorA2, colorA3,
        colorB0, colorB1, colorB2, colorB3, colorS1, colorS2;
      uint product1a, product1b, product2a, product2b;

      //---------------------------------------    B1 B2
      //                                         4  5  6 S2
      //                                         1  2  3 S1
      //                                           A1 A2

      colorB0 = *(bP - Nextline - 1);
      colorB1 = *(bP - Nextline);
      colorB2 = *(bP - Nextline + 1);
      colorB3 = *(bP - Nextline + 2);

      color4 = *(bP - 1);
      color5 = *(bP);
      color6 = *(bP + 1);
      colorS2 = *(bP + 2);

      color1 = *(bP + Nextline - 1);
      color2 = *(bP + Nextline);
      color3 = *(bP + Nextline + 1);
      colorS1 = *(bP + Nextline + 2);

      colorA0 = *(bP + Nextline + Nextline - 1);
      colorA1 = *(bP + Nextline + Nextline);
      colorA2 = *(bP + Nextline + Nextline + 1);
      colorA3 = *(bP + Nextline + Nextline + 2);

      //--------------------------------------
      if (color2 == color6 && color5 != color3) {
        product2b = product1b = color2;
      } else if (color5 == color3 && color2 != color6) {
        product2b = product1b = color5;
      } else if (color5 == color3 && color2 == color6) {
        int r = 0;

        r += GetResult (color6, color5, color1, colorA1);
        r += GetResult (color6, color5, color4, colorB1);
        r += GetResult (color6, color5, colorA2, colorS1);
        r += GetResult (color6, color5, colorB2, colorS2);

        if (r > 0)
          product2b = product1b = color6;
        else if (r < 0)
          product2b = product1b = color5;
        else {
          product2b = product1b = INTERPOLATE (color5, color6);
        }
      } else {
        if (color6 == color3 && color3 == colorA1
            && color2 != colorA2 && color3 != colorA0)
          product2b =
            Q_INTERPOLATE (color3, color3, color3, color2);
        else if (color5 == color2 && color2 == colorA2
                 && colorA1 != color3 && color2 != colorA3)
          product2b =
            Q_INTERPOLATE (color2, color2, color2, color3);
        else
          product2b = INTERPOLATE (color2, color3);

        if (color6 == color3 && color6 == colorB1
            && color5 != colorB2 && color6 != colorB0)
          product1b =
            Q_INTERPOLATE (color6, color6, color6, color5);
        else if (color5 == color2 && color5 == colorB2
                 && colorB1 != color6 && color5 != colorB3)
          product1b =
            Q_INTERPOLATE (color6, color5, color5, color5);
        else
          product1b = INTERPOLATE (color5, color6);
      }

      if (color5 == color3 && color2 != color6 && color4 == color5
          && color5 != colorA2)
        product2a = INTERPOLATE (color2, color5);
      else
        if (color5 == color1 && color6 == color5
            && color4 != color2 && color5 != colorA0)
          product2a = INTERPOLATE (color2, color5);
        else
          product2a = color2;

      if (color2 == color6 && color5 != color3 && color1 == color2
          && color2 != colorB2)
        product1a = INTERPOLATE (color2, color5);
      else
        if (color4 == color2 && color3 == color2
            && color1 != color5 && color2 != colorB0)
          product1a = INTERPOLATE (color2, color5);
        else
          product1a = color5;
			*(dP) = product1a | 0xFF000000;
			*(dP + 1) = product1b | 0xFF000000;
			*(dP + (dstPitch >> 2)) = product2a | 0xFF000000;
			*(dP + (dstPitch >> 2) + 1) = product2b | 0xFF000000;

      bP += inc_bP;
      dP += 2;
    }                       // end of for ( finish= width etc..)

    srcPtr   += srcPitch;
    dstPtr   += dstPitch * 2;
//        deltaPtr += srcPitch;
  }                 // endof: for (; height; height--)
}

	}
}
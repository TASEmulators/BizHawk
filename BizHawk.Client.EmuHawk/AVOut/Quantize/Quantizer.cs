/////////////////////////////////////////////////////////////////////////////////
// Paint.NET
// Copyright (C) Rick Brewster, Chris Crosetto, Dennis Dietrich, Tom Jackson, 
//               Michael Kelsey, Brandon Ortiz, Craig Taylor, Chris Trevino, 
//               and Luke Walker
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.
// See src/setup/License.rtf for complete licensing and attribution information.
/////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////
// Copied for Paint.NET PCX Plugin
// Copyright (C) Joshua Bell
/////////////////////////////////////////////////////////////////////////////////

// Based on: http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dnaspp/html/colorquant.asp

//Bizhawk says: adapted from https://github.com/inexorabletash/PcxFileType/blob/master/Quantize

using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace BizHawk.Client.EmuHawk
{
    /// <summary>
    /// Summary description for Class1.
    /// </summary>
    internal unsafe abstract class Quantizer
    {
        /// <summary>
        /// Flag used to indicate whether a single pass or two passes are needed for quantization.
        /// </summary>
        private bool _singlePass;
        
        protected bool highquality;
        public bool HighQuality 
        {
            get 
            {
                return highquality;
            }

            set 
            {
                highquality = value;
            }
        }

        protected int ditherLevel;
        public int DitherLevel
        {
            get
            {
                return this.ditherLevel;
            }

            set
            {
                this.ditherLevel = value;
            }
        }

        /// <summary>
        /// Construct the quantizer
        /// </summary>
        /// <param name="singlePass">If true, the quantization only needs to loop through the source pixels once</param>
        /// <remarks>
        /// If you construct this class with a true value for singlePass, then the code will, when quantizing your image,
        /// only call the 'QuantizeImage' function. If two passes are required, the code will call 'InitialQuantizeImage'
        /// and then 'QuantizeImage'.
        /// </remarks>
        public Quantizer(bool singlePass)
        {
            _singlePass = singlePass;
        }

        /// <summary>
        /// Quantize an image and return the resulting output bitmap
        /// </summary>
        /// <param name="source">The image to quantize</param>
        /// <returns>A quantized version of the image</returns>
        public Bitmap Quantize(Image source)
        {
            // Get the size of the source image
            int height = source.Height;
            int width = source.Width;

            // And construct a rectangle from these dimensions
            Rectangle bounds = new Rectangle(0, 0, width, height);

            // First off take a 32bpp copy of the image
            Bitmap copy;
            
            if (source is Bitmap && source.PixelFormat == PixelFormat.Format32bppArgb)
            {
                copy = (Bitmap)source;
            }
            else
            {
                copy = new Bitmap(width, height, PixelFormat.Format32bppArgb);

                // Now lock the bitmap into memory
                using (Graphics g = Graphics.FromImage(copy))
                {
                    g.PageUnit = GraphicsUnit.Pixel;

                    // Draw the source image onto the copy bitmap,
                    // which will effect a widening as appropriate.
                    g.DrawImage(source, 0, 0, bounds.Width, bounds.Height);
                }
            }

            // And construct an 8bpp version
            Bitmap output = new Bitmap(width, height, PixelFormat.Format8bppIndexed);

            // Define a pointer to the bitmap data
            BitmapData sourceData = null;

            try
            {
                // Get the source image bits and lock into memory
                sourceData = copy.LockBits(bounds, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

                // Call the FirstPass function if not a single pass algorithm.
                // For something like an octree quantizer, this will run through
                // all image pixels, build a data structure, and create a palette.
                if (!_singlePass)
                {
                    FirstPass(sourceData, width, height);
                }

                // Then set the color palette on the output bitmap. I'm passing in the current palette 
                // as there's no way to construct a new, empty palette.
                output.Palette = this.GetPalette(output.Palette);

                // Then call the second pass which actually does the conversion
                SecondPass(sourceData, output, width, height, bounds);
            }

            finally
            {
                // Ensure that the bits are unlocked
                copy.UnlockBits(sourceData);
            }

            if (copy != source)
            {
                copy.Dispose();
            }

            // Last but not least, return the output bitmap
            return output;
        }

        /// <summary>
        /// Execute the first pass through the pixels in the image
        /// </summary>
        /// <param name="sourceData">The source data</param>
        /// <param name="width">The width in pixels of the image</param>
        /// <param name="height">The height in pixels of the image</param>
        protected virtual void FirstPass(BitmapData sourceData, int width, int height)
        {
            // Define the source data pointers. The source row is a byte to
            // keep addition of the stride value easier (as this is in bytes)
            byte* pSourceRow = (byte*)sourceData.Scan0.ToPointer();
            int* pSourcePixel;

            // Loop through each row
            for (int row = 0; row < height; row++)
            {
                // Set the source pixel to the first pixel in this row
                pSourcePixel = (Int32*)pSourceRow;

                // And loop through each column
                for (int col = 0; col < width; col++, pSourcePixel++)
                {
                    InitialQuantizePixel(*pSourcePixel);
                }

                // Add the stride to the source row
                pSourceRow += sourceData.Stride;

            }
        }

				int ClampToByte(int val)
				{
					if (val < 0) return 0;
					else if (val > 255) return 255;
					else return val;
				}

        /// <summary>
        /// Execute a second pass through the bitmap
        /// </summary>
        /// <param name="sourceData">The source bitmap, locked into memory</param>
        /// <param name="output">The output bitmap</param>
        /// <param name="width">The width in pixels of the image</param>
        /// <param name="height">The height in pixels of the image</param>
        /// <param name="bounds">The bounding rectangle</param>
        protected virtual void SecondPass(BitmapData sourceData, Bitmap output, int width, int height, Rectangle bounds)
        {
            BitmapData outputData = null;
            Color[] pallete = output.Palette.Entries;
            int weight = ditherLevel;

            try
            {
                // Lock the output bitmap into memory
                outputData = output.LockBits(bounds, ImageLockMode.ReadWrite, PixelFormat.Format8bppIndexed);

                // Define the source data pointers. The source row is a byte to
                // keep addition of the stride value easier (as this is in bytes)
                byte* pSourceRow = (byte *)sourceData.Scan0.ToPointer();
                Int32* pSourcePixel = (Int32 *)pSourceRow;

                // Now define the destination data pointers
                byte* pDestinationRow = (byte *)outputData.Scan0.ToPointer();
                byte* pDestinationPixel = pDestinationRow;

                int[] errorThisRowR = new int[width + 1];
                int[] errorThisRowG = new int[width + 1];
                int[] errorThisRowB = new int[width + 1];

                for (int row = 0; row < height; row++)
                {
                    int[] errorNextRowR = new int[width + 1];
                    int[] errorNextRowG = new int[width + 1];
                    int[] errorNextRowB = new int[width + 1];

                    int ptrInc;

                    if ((row & 1) == 0)
                    {
                        pSourcePixel = (Int32*)pSourceRow;
                        pDestinationPixel = pDestinationRow;
                        ptrInc = +1;
                    }
                    else
                    {
                        pSourcePixel = (Int32*)pSourceRow + width - 1;
                        pDestinationPixel = pDestinationRow + width - 1;
                        ptrInc = -1;
                    }

                    // Loop through each pixel on this scan line
                    for (int col = 0; col < width; ++col)
                    {
                        // Quantize the pixel
                        int srcPixel = *pSourcePixel;

												int srcR = srcPixel & 0xFF; //not
												int srcG = (srcPixel>>8) & 0xFF; //a
												int srcB = (srcPixel>>16) & 0xFF; //mistake
												int srcA = (srcPixel >> 24) & 0xFF;

                        int targetB = ClampToByte(srcB - ((errorThisRowB[col] * weight) / 8));
                        int targetG = ClampToByte(srcG - ((errorThisRowG[col] * weight) / 8));
                        int targetR = ClampToByte(srcR - ((errorThisRowR[col] * weight) / 8));
                        int targetA = srcA;

											int target = (targetA<<24)|(targetB<<16)|(targetG<<8)|targetR;

                        byte pixelValue = QuantizePixel(target);
                        *pDestinationPixel = pixelValue;

												int actual = pallete[pixelValue].ToArgb();

												int actualR = actual & 0xFF;
												int actualG = (actual >> 8) & 0xFF;
												int actualB = (actual >> 16) & 0xFF;
                        int errorR = actualR - targetR;
                        int errorG = actualG - targetG;
                        int errorB = actualB - targetB; 

                        // Floyd-Steinberg Error Diffusion:
                        // a) 7/16 error goes to x+1
                        // b) 5/16 error goes to y+1
                        // c) 3/16 error goes to x-1,y+1
                        // d) 1/16 error goes to x+1,y+1

                        const int a = 7;
                        const int b = 5;
                        const int c = 3;

                        int errorRa = (errorR * a) / 16;
                        int errorRb = (errorR * b) / 16;
                        int errorRc = (errorR * c) / 16;
                        int errorRd = errorR - errorRa - errorRb - errorRc;

                        int errorGa = (errorG * a) / 16;
                        int errorGb = (errorG * b) / 16;
                        int errorGc = (errorG * c) / 16;
                        int errorGd = errorG - errorGa - errorGb - errorGc;

                        int errorBa = (errorB * a) / 16;
                        int errorBb = (errorB * b) / 16;
                        int errorBc = (errorB * c) / 16;
                        int errorBd = errorB - errorBa - errorBb - errorBc;

                        errorThisRowR[col + 1] += errorRa;
                        errorThisRowG[col + 1] += errorGa;
                        errorThisRowB[col + 1] += errorBa;

                        errorNextRowR[width - col] += errorRb;
                        errorNextRowG[width - col] += errorGb;
                        errorNextRowB[width - col] += errorBb;

                        if (col != 0)
                        {
                            errorNextRowR[width - (col - 1)] += errorRc;
                            errorNextRowG[width - (col - 1)] += errorGc;
                            errorNextRowB[width - (col - 1)] += errorBc;
                        }

                        errorNextRowR[width - (col + 1)] += errorRd;
                        errorNextRowG[width - (col + 1)] += errorGd;
                        errorNextRowB[width - (col + 1)] += errorBd;

                        unchecked
                        {
                            pSourcePixel += ptrInc;
                            pDestinationPixel += ptrInc;
                        }
                    }

                    // Add the stride to the source row
                    pSourceRow += sourceData.Stride;

                    // And to the destination row
                    pDestinationRow += outputData.Stride;

                    errorThisRowB = errorNextRowB;
                    errorThisRowG = errorNextRowG;
                    errorThisRowR = errorNextRowR;
                }
            }
            
            finally
            {
                // Ensure that I unlock the output bits
                output.UnlockBits(outputData);
            }
        }

        /// <summary>
        /// Override this to process the pixel in the first pass of the algorithm
        /// </summary>
        /// <param name="pixel">The pixel to quantize</param>
        /// <remarks>
        /// This function need only be overridden if your quantize algorithm needs two passes,
        /// such as an Octree quantizer.
        /// </remarks>
        protected virtual void InitialQuantizePixel(int pixel)
        {
        }

        /// <summary>
        /// Override this to process the pixel in the second pass of the algorithm
        /// </summary>
        /// <param name="pixel">The pixel to quantize</param>
        /// <returns>The quantized value</returns>
        protected abstract byte QuantizePixel(int pixel);

        /// <summary>
        /// Retrieve the palette for the quantized image
        /// </summary>
        /// <param name="original">Any old palette, this is overrwritten</param>
        /// <returns>The new color palette</returns>
        protected abstract ColorPalette GetPalette(ColorPalette original);
    }
}

/* * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * *
 *   Mupen64plus - glide64/wrapper/filter.cpp                              *
 *   Mupen64Plus homepage: http://code.google.com/p/mupen64plus/           *
 *   Copyright (C) 2005-2006 Hacktarux                                     *
 *                                                                         *
 *   This program is free software; you can redistribute it and/or modify  *
 *   it under the terms of the GNU General Public License as published by  *
 *   the Free Software Foundation; either version 2 of the License, or     *
 *   (at your option) any later version.                                   *
 *                                                                         *
 *   This program is distributed in the hope that it will be useful,       *
 *   but WITHOUT ANY WARRANTY; without even the implied warranty of        *
 *   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the         *
 *   GNU General Public License for more details.                          *
 *                                                                         *
 *   You should have received a copy of the GNU General Public License     *
 *   along with this program; if not, write to the                         *
 *   Free Software Foundation, Inc.,                                       *
 *   51 Franklin Street, Fifth Floor, Boston, MA 02110-1301, USA.          *
 * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * */

#include <stdlib.h>
#include <string.h>

#include "../winlnxdefs.h"

#include "main.h"
#include "2xsai.h"

// this filter is crap, it's just some stuffs i tried to see what they're doing
// it's blurring texture edges which is good for some textures, bad for some others.
unsigned char *blur_edges(unsigned char *source, int width, int height, int *width2, int *height2)
{
    unsigned char *result, *temp, *temp2;
    char mx[3*3] = {-1, 0, 1, -2, 0, 2, -1, 0, 1};
    char my[3*3] = {-1, -2, -1, 0, 0, 0, 1, 2 ,1};
    int i,j;

    *width2 = width*2;
    *height2 = height*2;

    result = (unsigned char*)malloc(width*2*height*2*4);
    temp = (unsigned char*)malloc(width*2*height*2*4);
    temp2 = (unsigned char*)malloc(width*2*height*2*4);

    // size * 2
    for (j=0; j<height; j++)
    {
        for (i=0; i<width; i++)
        {
            temp[j*2*width*2*4 + i*2*4 + 0] = source[j*width*4 + i*4 + 0];
            temp[j*2*width*2*4 + i*2*4 + 1] = source[j*width*4 + i*4 + 1];
            temp[j*2*width*2*4 + i*2*4 + 2] = source[j*width*4 + i*4 + 2];
            temp[j*2*width*2*4 + i*2*4 + 3] = source[j*width*4 + i*4 + 3];

            if (i < (width-1))
            {
                temp[j*2*width*2*4 + (i*2+1)*4 + 0] =
                    ((int)source[j*width*4 + i*4 + 0] + (int)source[j*width*4 + (i+1)*4 + 0])>>1;
                temp[j*2*width*2*4 + (i*2+1)*4 + 1] =
                    ((int)source[j*width*4 + i*4 + 1] + (int)source[j*width*4 + (i+1)*4 + 1])>>1;
                temp[j*2*width*2*4 + (i*2+1)*4 + 2] =
                    ((int)source[j*width*4 + i*4 + 2] + (int)source[j*width*4 + (i+1)*4 + 2])>>1;
                temp[j*2*width*2*4 + (i*2+1)*4 + 3] =
                    ((int)source[j*width*4 + i*4 + 3] + (int)source[j*width*4 + (i+1)*4 + 3])>>1;
            }
            else
            {
                temp[j*2*width*2*4 + (i*2+1)*4 + 0] = temp[j*2*width*2*4 + i*2*4 + 0];
                temp[j*2*width*2*4 + (i*2+1)*4 + 1] = temp[j*2*width*2*4 + i*2*4 + 1];
                temp[j*2*width*2*4 + (i*2+1)*4 + 2] = temp[j*2*width*2*4 + i*2*4 + 2];
                temp[j*2*width*2*4 + (i*2+1)*4 + 3] = temp[j*2*width*2*4 + i*2*4 + 3];
            }

            if (j < (height-1))
            {
                temp[(j*2+1)*width*2*4 + i*2*4 + 0] =
                    ((int)source[j*width*4 + i*4 + 0] + (int)source[(j+1)*width*4 + i*4 + 0])>>1;
                temp[(j*2+1)*width*2*4 + i*2*4 + 1] =
                    ((int)source[j*width*4 + i*4 + 1] + (int)source[(j+1)*width*4 + i*4 + 1])>>1;
                temp[(j*2+1)*width*2*4 + i*2*4 + 2] =
                    ((int)source[j*width*4 + i*4 + 2] + (int)source[(j+1)*width*4 + i*4 + 2])>>1;
                temp[(j*2+1)*width*2*4 + i*2*4 + 3] =
                    ((int)source[j*width*4 + i*4 + 3] + (int)source[(j+1)*width*4 + i*4 + 3])>>1;

                if (i < (width-1))
                {
                    temp[(j*2+1)*width*2*4 + (i*2+1)*4 + 0] =
                        ((int)source[j*width*4 + i*4 + 0] + (int)source[(j+1)*width*4 + (i+1)*4 + 0])>>1;
                    temp[(j*2+1)*width*2*4 + (i*2+1)*4 + 1] =
                        ((int)source[j*width*4 + i*4 + 1] + (int)source[(j+1)*width*4 + (i+1)*4 + 1])>>1;
                    temp[(j*2+1)*width*2*4 + (i*2+1)*4 + 2] =
                        ((int)source[j*width*4 + i*4 + 2] + (int)source[(j+1)*width*4 + (i+1)*4 + 2])>>1;
                    temp[(j*2+1)*width*2*4 + (i*2+1)*4 + 3] =
                        ((int)source[j*width*4 + i*4 + 3] + (int)source[(j+1)*width*4 + (i+1)*4 + 3])>>1;
                }
                else
                {
                    temp[(j*2+1)*width*2*4 + (i*2+1)*4 + 0] = temp[j*2*width*2*4 + i*2*4 + 0];
                    temp[(j*2+1)*width*2*4 + (i*2+1)*4 + 1] = temp[j*2*width*2*4 + i*2*4 + 1];
                    temp[(j*2+1)*width*2*4 + (i*2+1)*4 + 2] = temp[j*2*width*2*4 + i*2*4 + 2];
                    temp[(j*2+1)*width*2*4 + (i*2+1)*4 + 3] = temp[j*2*width*2*4 + i*2*4 + 3];
                }
            }
            else
            {
                temp[(j*2+1)*width*2*4 + i*2*4 + 0] = temp[j*2*width*2*4 + i*2*4 + 0];
                temp[(j*2+1)*width*2*4 + i*2*4 + 1] = temp[j*2*width*2*4 + i*2*4 + 1];
                temp[(j*2+1)*width*2*4 + i*2*4 + 2] = temp[j*2*width*2*4 + i*2*4 + 2];
                temp[(j*2+1)*width*2*4 + i*2*4 + 3] = temp[j*2*width*2*4 + i*2*4 + 3];

                temp[(j*2+1)*width*2*4 + (i*2+1)*4 + 0] = temp[j*2*width*2*4 + i*2*4 + 0];
                temp[(j*2+1)*width*2*4 + (i*2+1)*4 + 1] = temp[j*2*width*2*4 + i*2*4 + 1];
                temp[(j*2+1)*width*2*4 + (i*2+1)*4 + 2] = temp[j*2*width*2*4 + i*2*4 + 2];
                temp[(j*2+1)*width*2*4 + (i*2+1)*4 + 3] = temp[j*2*width*2*4 + i*2*4 + 3];
            }
        }
    }

    // gradient
    for (j=0; j<height*2; j++)
    {
        for (i=0; i<width*2; i++)
        {
            int gx_r=0, gy_r=0, gx_g=0, gy_g=0, gx_b=0, gy_b=0, gx_a=0, gy_a=0, k, l;
            if (i==0 || j==0 || j==height*2-1 || i==width*2-1)
            {
                gx_r = temp[j*width*2*4 + i*4 + 0];
                gy_r = temp[j*width*2*4 + i*4 + 0];
                gx_g = temp[j*width*2*4 + i*4 + 1];
                gy_g = temp[j*width*2*4 + i*4 + 1];
                gx_b = temp[j*width*2*4 + i*4 + 2];
                gy_b = temp[j*width*2*4 + i*4 + 2];
                gx_a = temp[j*width*2*4 + i*4 + 3];
                gy_a = temp[j*width*2*4 + i*4 + 3];
            }
            else
            {
                for (k=0; k<3; k++)
                {
                    for (l=0; l<3; l++)
                    {
                        gx_r += (int)temp[(j-1+k)*width*2*4 + (i-1+l)*4 + 0] * mx[k*3+l];
                        gy_r += (int)temp[(j-1+k)*width*2*4 + (i-1+l)*4 + 0] * my[k*3+l];
                        gx_g += (int)temp[(j-1+k)*width*2*4 + (i-1+l)*4 + 1] * mx[k*3+l];
                        gy_g += (int)temp[(j-1+k)*width*2*4 + (i-1+l)*4 + 1] * my[k*3+l];
                        gx_b += (int)temp[(j-1+k)*width*2*4 + (i-1+l)*4 + 2] * mx[k*3+l];
                        gy_b += (int)temp[(j-1+k)*width*2*4 + (i-1+l)*4 + 2] * my[k*3+l];
                        gx_a += (int)temp[(j-1+k)*width*2*4 + (i-1+l)*4 + 3] * mx[k*3+l];
                        gy_a += (int)temp[(j-1+k)*width*2*4 + (i-1+l)*4 + 3] * my[k*3+l];
                    }
                }
            }
            gx_r = gx_r < 0 ? -gx_r : gx_r;
            gy_r = gy_r < 0 ? -gy_r : gy_r;
            gx_g = gx_g < 0 ? -gx_g : gx_g;
            gy_g = gy_g < 0 ? -gy_g : gy_g;
            gx_b = gx_b < 0 ? -gx_b : gx_b;
            gy_b = gy_b < 0 ? -gy_b : gy_b;
            gx_a = gx_a < 0 ? -gx_a : gx_a;
            gy_a = gy_a < 0 ? -gy_a : gy_a;

            temp2[j*width*2*4 + i*4 + 0] = gx_r + gy_r;
            temp2[j*width*2*4 + i*4 + 1] = gx_g + gy_g;
            temp2[j*width*2*4 + i*4 + 2] = gx_b + gy_b;
            temp2[j*width*2*4 + i*4 + 3] = gx_a + gy_a;
        }
    }

    // bluring

    for (j=0; j<height*2; j++)
    {
        for (i=0; i<width*2; i++)
        {
            int mini = i != 0 ? i-1 : 0;
            int maxi = i != width*2-1 ? i+1 : width*2-1;
            int minj = j != 0 ? j-1 : 0;
            int maxj = j != height*2-1 ? j+1 : height*2-1;
            int mini2 = mini != 0 ? mini-1 : 0;
            int maxi2 = maxi != width*2-1 ? maxi+1 : width*2-1;
            int minj2 = minj != 0 ? minj-1 : 0;
            int maxj2 = maxj != height*2-1 ? maxj+1 : height*2-1;
            int total;

            // r
            total = 0;
            total += (int)temp[j*width*2*4 + i*4 + 0];
            total += (int)temp[minj*width*2*4 + mini*4 + 0];
            total += (int)temp[minj*width*2*4 + i*4 + 0];
            total += (int)temp[minj*width*2*4 + maxi*4 + 0];
            total += (int)temp[j*width*2*4 + maxi*4 + 0];
            total += (int)temp[maxj*width*2*4 + maxi*4 + 0];
            total += (int)temp[maxj*width*2*4 + i*4 + 0];
            total += (int)temp[maxj*width*2*4 + mini*4 + 0];
            total += (int)temp[j*width*2*4 + mini*4 + 0];

            total += (int)temp[minj2*width*2*4 + mini2*4 + 0];
            total += (int)temp[minj2*width*2*4 + mini*4 + 0];
            total += (int)temp[minj2*width*2*4 + i*4 + 0];
            total += (int)temp[minj2*width*2*4 + maxi*4 + 0];
            total += (int)temp[minj2*width*2*4 + maxi2*4 + 0];
            total += (int)temp[minj*width*2*4 + maxi2*4 + 0];
            total += (int)temp[j*width*2*4 + maxi2*4 + 0];
            total += (int)temp[maxj*width*2*4 + maxi2*4 + 0];
            total += (int)temp[maxj2*width*2*4 + maxi2*4 + 0];
            total += (int)temp[maxj2*width*2*4 + maxi*4 + 0];
            total += (int)temp[maxj2*width*2*4 + i*4 + 0];
            total += (int)temp[maxj2*width*2*4 + mini*4 + 0];
            total += (int)temp[maxj2*width*2*4 + mini2*4 + 0];
            total += (int)temp[maxj*width*2*4 + mini2*4 + 0];
            total += (int)temp[j*width*2*4 + mini2*4 + 0];
            total += (int)temp[minj*width*2*4 + mini2*4 + 0];
            
            result[j*width*2*4 + i*4 + 0] = 
                ((total / 25) * (int)temp2[j*width*2*4 + i*4 + 0] +
                 ((int)temp[j*width*2*4 + i*4 + 0]) * (255-(int)temp2[j*width*2*4 + i*4 + 0]))/255;

            // g
            total = 0;
            total += (int)temp[j*width*2*4 + i*4 + 1];
            total += (int)temp[minj*width*2*4 + mini*4 + 1];
            total += (int)temp[minj*width*2*4 + i*4 + 1];
            total += (int)temp[minj*width*2*4 + maxi*4 + 1];
            total += (int)temp[j*width*2*4 + maxi*4 + 1];
            total += (int)temp[maxj*width*2*4 + maxi*4 + 1];
            total += (int)temp[maxj*width*2*4 + i*4 + 1];
            total += (int)temp[maxj*width*2*4 + mini*4 + 1];
            total += (int)temp[j*width*2*4 + mini*4 + 1];

            total += (int)temp[minj2*width*2*4 + mini2*4 + 1];
            total += (int)temp[minj2*width*2*4 + mini*4 + 1];
            total += (int)temp[minj2*width*2*4 + i*4 + 1];
            total += (int)temp[minj2*width*2*4 + maxi*4 + 1];
            total += (int)temp[minj2*width*2*4 + maxi2*4 + 1];
            total += (int)temp[minj*width*2*4 + maxi2*4 + 1];
            total += (int)temp[j*width*2*4 + maxi2*4 + 1];
            total += (int)temp[maxj*width*2*4 + maxi2*4 + 1];
            total += (int)temp[maxj2*width*2*4 + maxi2*4 + 1];
            total += (int)temp[maxj2*width*2*4 + maxi*4 + 1];
            total += (int)temp[maxj2*width*2*4 + i*4 + 1];
            total += (int)temp[maxj2*width*2*4 + mini*4 + 1];
            total += (int)temp[maxj2*width*2*4 + mini2*4 + 1];
            total += (int)temp[maxj*width*2*4 + mini2*4 + 1];
            total += (int)temp[j*width*2*4 + mini2*4 + 1];
            total += (int)temp[minj*width*2*4 + mini2*4 + 1];

            result[j*width*2*4 + i*4 + 1] = 
                ((total / 25) * (int)temp2[j*width*2*4 + i*4 + 1] +
                 ((int)temp[j*width*2*4 + i*4 + 1]) * (255-(int)temp2[j*width*2*4 + i*4 + 1]))/255;

            // b
            total = 0;
            total += (int)temp[j*width*2*4 + i*4 + 2];
            total += (int)temp[minj*width*2*4 + mini*4 + 2];
            total += (int)temp[minj*width*2*4 + i*4 + 2];
            total += (int)temp[minj*width*2*4 + maxi*4 + 2];
            total += (int)temp[j*width*2*4 + maxi*4 + 2];
            total += (int)temp[maxj*width*2*4 + maxi*4 + 2];
            total += (int)temp[maxj*width*2*4 + i*4 + 2];
            total += (int)temp[maxj*width*2*4 + mini*4 + 2];
            total += (int)temp[j*width*2*4 + mini*4 + 2];

            total += (int)temp[minj2*width*2*4 + mini2*4 + 2];
            total += (int)temp[minj2*width*2*4 + mini*4 + 2];
            total += (int)temp[minj2*width*2*4 + i*4 + 2];
            total += (int)temp[minj2*width*2*4 + maxi*4 + 2];
            total += (int)temp[minj2*width*2*4 + maxi2*4 + 2];
            total += (int)temp[minj*width*2*4 + maxi2*4 + 2];
            total += (int)temp[j*width*2*4 + maxi2*4 + 2];
            total += (int)temp[maxj*width*2*4 + maxi2*4 + 2];
            total += (int)temp[maxj2*width*2*4 + maxi2*4 + 2];
            total += (int)temp[maxj2*width*2*4 + maxi*4 + 2];
            total += (int)temp[maxj2*width*2*4 + i*4 + 2];
            total += (int)temp[maxj2*width*2*4 + mini*4 + 2];
            total += (int)temp[maxj2*width*2*4 + mini2*4 + 2];
            total += (int)temp[maxj*width*2*4 + mini2*4 + 2];
            total += (int)temp[j*width*2*4 + mini2*4 + 2];
            total += (int)temp[minj*width*2*4 + mini2*4 + 2];

            result[j*width*2*4 + i*4 + 2] = 
                ((total / 25) * (int)temp2[j*width*2*4 + i*4 + 2] +
                 ((int)temp[j*width*2*4 + i*4 + 2]) * (255-(int)temp2[j*width*2*4 + i*4 + 2]))/255;

            // a
            total = 0;
            total += (int)temp[j*width*2*4 + i*4 + 3];
            total += (int)temp[minj*width*2*4 + mini*4 + 3];
            total += (int)temp[minj*width*2*4 + i*4 + 3];
            total += (int)temp[minj*width*2*4 + maxi*4 + 3];
            total += (int)temp[j*width*2*4 + maxi*4 + 3];
            total += (int)temp[maxj*width*2*4 + maxi*4 + 3];
            total += (int)temp[maxj*width*2*4 + i*4 + 3];
            total += (int)temp[maxj*width*2*4 + mini*4 + 3];
            total += (int)temp[j*width*2*4 + mini*4 + 3];

            total += (int)temp[minj2*width*2*4 + mini2*4 + 3];
            total += (int)temp[minj2*width*2*4 + mini*4 + 3];
            total += (int)temp[minj2*width*2*4 + i*4 + 3];
            total += (int)temp[minj2*width*2*4 + maxi*4 + 3];
            total += (int)temp[minj2*width*2*4 + maxi2*4 + 3];
            total += (int)temp[minj*width*2*4 + maxi2*4 + 3];
            total += (int)temp[j*width*2*4 + maxi2*4 + 3];
            total += (int)temp[maxj*width*2*4 + maxi2*4 + 3];
            total += (int)temp[maxj2*width*2*4 + maxi2*4 + 3];
            total += (int)temp[maxj2*width*2*4 + maxi*4 + 3];
            total += (int)temp[maxj2*width*2*4 + i*4 + 3];
            total += (int)temp[maxj2*width*2*4 + mini*4 + 3];
            total += (int)temp[maxj2*width*2*4 + mini2*4 + 3];
            total += (int)temp[maxj*width*2*4 + mini2*4 + 3];
            total += (int)temp[j*width*2*4 + mini2*4 + 3];
            total += (int)temp[minj*width*2*4 + mini2*4 + 3];

            result[j*width*2*4 + i*4 + 3] = 
                ((total / 25) * (int)temp2[j*width*2*4 + i*4 + 3] +
                 ((int)temp[j*width*2*4 + i*4 + 3]) * (255-(int)temp2[j*width*2*4 + i*4 + 3]))/255;
        }
    }

    free(temp2);
    free(temp);
    return result;
}

void hq2x_32( unsigned char * pIn, unsigned char * pOut, int Xres, int Yres, int BpL );
void hq4x_32( unsigned char * pIn, unsigned char * pOut, int Xres, int Yres, int BpL );

unsigned char *filter(unsigned char *source, int width, int height, int *width2, int *height2)
{
    switch(getFilter())
    {
    case 1:
        return blur_edges(source, width, height, width2, height2);
        break;
    case 2:
        {
            unsigned char *result;
            result = (unsigned char*)malloc(width*2*height*2*4);
            *width2 = width*2;
            *height2 = height*2;
            Super2xSaI((DWORD*)source, (DWORD*)result, width, height, width);
            return result;
        }
        break;
    case 3:
        {
            unsigned char *result;
            result = (unsigned char*)malloc(width*2*height*2*4);
            *width2 = width*2;
            *height2 = height*2;
            hq2x_32(source, result, width, height, width*2*4);
            return result;
        }
        break;
    case 4:
        {
            unsigned char *result;
            result = (unsigned char*)malloc(width*4*height*4*4);
            *width2 = width*4;
            *height2 = height*4;
            hq4x_32(source, result, width, height, width*4*4);
            return result;
        }
        break;
    }
    return NULL;
}


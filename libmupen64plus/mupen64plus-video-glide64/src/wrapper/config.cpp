/* * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * *
 *   Mupen64plus - glide64/wrapper/config.cpp                              *
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


#include <SDL_opengl.h>

#include "glide.h"
#include "main.h"

#include "../winlnxdefs.h"

typedef struct _wrapper_config
{
    int res;
    int filter;
    int disable_glsl;
    int disable_dithered_alpha;
  int FBO;
  int disable_auxbuf;
} wrapper_config;

FX_ENTRY void FX_CALL grConfigWrapperExt(HINSTANCE
instance, HWND hwnd)
{
}

#include "../rdp.h"

FX_ENTRY GrScreenResolution_t FX_CALL grWrapperFullScreenResolutionExt(void)
{
   return settings.full_res;
}

int getFilter()
{
   return settings.tex_filter;
}

int getDisableDitheredAlpha()
{
   return settings.noditheredalpha;
}

int getEnableFBO()
{
   return settings.FBO;
}

int getDisableAuxbuf()
{
   return settings.disable_auxbuf;
}

int getDisableGLSL()
{
   return settings.noglsl;
}


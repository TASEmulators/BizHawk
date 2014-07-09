/*
 * z64
 *
 * Copyright (C) 2007  ziggy
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License along
 * with this program; if not, write to the Free Software Foundation, Inc.,
 * 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA.
 *
**/

#include "rdp.h"
#include "rgl.h"

#include <SDL.h>

SDL_Surface *sdl_Screen;
int viewportOffset;

/* definitions of pointers to Core video extension functions */
extern ptr_VidExt_Init                  CoreVideo_Init;
extern ptr_VidExt_Quit                  CoreVideo_Quit;
extern ptr_VidExt_ListFullscreenModes   CoreVideo_ListFullscreenModes;
extern ptr_VidExt_SetVideoMode          CoreVideo_SetVideoMode;
extern ptr_VidExt_SetCaption            CoreVideo_SetCaption;
extern ptr_VidExt_ToggleFullScreen      CoreVideo_ToggleFullScreen;
extern ptr_VidExt_ResizeWindow          CoreVideo_ResizeWindow;
extern ptr_VidExt_GL_GetProcAddress     CoreVideo_GL_GetProcAddress;
extern ptr_VidExt_GL_SetAttribute       CoreVideo_GL_SetAttribute;
extern ptr_VidExt_GL_SwapBuffers        CoreVideo_GL_SwapBuffers;

//int screen_width = 640, screen_height = 480;
int screen_width = 1024, screen_height = 768;
//int screen_width = 320, screen_height = 240;

int viewport_offset;

void rglSwapBuffers()
{
	//TODO: if screenshot capabilities are ever implemented, replace the
	//hard-coded 1 with a value indicating whether the screen has been redrawn
    if (render_callback != NULL)
        render_callback(1);
    CoreVideo_GL_SwapBuffers();
    return;
}

int rglOpenScreen()
{
    if (CoreVideo_Init() != M64ERR_SUCCESS) {
        rdp_log(M64MSG_ERROR, "Could not initialize video.");
        return 0;
    }
    if (rglStatus == RGL_STATUS_WINDOWED) {
        screen_width = rglSettings.resX;
        screen_height = rglSettings.resY;
    } else {
        screen_width = rglSettings.fsResX;
        screen_height = rglSettings.fsResY;
    }

    m64p_video_mode screen_mode = M64VIDEO_WINDOWED;
    if (rglSettings.fullscreen)
        screen_mode = M64VIDEO_FULLSCREEN;

    viewportOffset = 0;

    if (CoreVideo_GL_SetAttribute(M64P_GL_DOUBLEBUFFER, 1) != M64ERR_SUCCESS ||
        CoreVideo_GL_SetAttribute(M64P_GL_BUFFER_SIZE, 32) != M64ERR_SUCCESS ||
        CoreVideo_GL_SetAttribute(M64P_GL_DEPTH_SIZE, 24)  != M64ERR_SUCCESS)
    {
        rdp_log(M64MSG_ERROR, "Could not set video attributes.");
        return 0;
    }

    if (CoreVideo_SetVideoMode(screen_width, screen_height, 32, screen_mode, (m64p_video_flags) 0) != M64ERR_SUCCESS)
    {
        rdp_log(M64MSG_ERROR, "Could not set video mode.");
        return 0;
    }

    CoreVideo_SetCaption("Z64gl");

    rdp_init();
    return 1;
}

void rglCloseScreen()
{
    rglClose();
    CoreVideo_Quit();
}

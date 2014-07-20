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
#include "osal_dynamiclib.h"
#include <SDL.h>

#define THREADED

#define PLUGIN_VERSION           0x020000
#define VIDEO_PLUGIN_API_VERSION 0x020200
#define CONFIG_API_VERSION       0x020000
#define VIDEXT_API_VERSION       0x030000

#define VERSION_PRINTF_SPLIT(x) (((x) >> 16) & 0xffff), (((x) >> 8) & 0xff), ((x) & 0xff)

GFX_INFO gfx;

void (*render_callback)(int) = NULL;
static void (*l_DebugCallback)(void *, int, const char *) = NULL;
static void *l_DebugCallContext = NULL;


/* definitions of pointers to Core video extension functions */
ptr_VidExt_Init                  CoreVideo_Init = NULL;
ptr_VidExt_Quit                  CoreVideo_Quit = NULL;
ptr_VidExt_ListFullscreenModes   CoreVideo_ListFullscreenModes = NULL;
ptr_VidExt_SetVideoMode          CoreVideo_SetVideoMode = NULL;
ptr_VidExt_SetCaption            CoreVideo_SetCaption = NULL;
ptr_VidExt_ToggleFullScreen      CoreVideo_ToggleFullScreen = NULL;
ptr_VidExt_ResizeWindow          CoreVideo_ResizeWindow = NULL;
ptr_VidExt_GL_GetProcAddress     CoreVideo_GL_GetProcAddress = NULL;
ptr_VidExt_GL_SetAttribute       CoreVideo_GL_SetAttribute = NULL;
ptr_VidExt_GL_SwapBuffers        CoreVideo_GL_SwapBuffers = NULL;

/* definitions of pointers to Core config functions */
ptr_ConfigOpenSection      ConfigOpenSection = NULL;
ptr_ConfigSetParameter     ConfigSetParameter = NULL;
ptr_ConfigGetParameter     ConfigGetParameter = NULL;
ptr_ConfigGetParameterHelp ConfigGetParameterHelp = NULL;
ptr_ConfigSetDefaultInt    ConfigSetDefaultInt = NULL;
ptr_ConfigSetDefaultFloat  ConfigSetDefaultFloat = NULL;
ptr_ConfigSetDefaultBool   ConfigSetDefaultBool = NULL;
ptr_ConfigSetDefaultString ConfigSetDefaultString = NULL;
ptr_ConfigGetParamInt      ConfigGetParamInt = NULL;
ptr_ConfigGetParamFloat    ConfigGetParamFloat = NULL;
ptr_ConfigGetParamBool     ConfigGetParamBool = NULL;
ptr_ConfigGetParamString   ConfigGetParamString = NULL;

#ifdef THREADED
volatile static int waiting;
SDL_sem * rdpCommandSema;
SDL_sem * rdpCommandCompleteSema;
SDL_Thread * rdpThread;
int rdpThreadFunc(void * dummy)
{
    while (1) {
        SDL_SemWait(rdpCommandSema);
        waiting = 1;
        if (rglNextStatus == RGL_STATUS_CLOSED)
            rglUpdateStatus();
        else
            rdp_process_list();
        if (!rglSettings.async)
            SDL_SemPost(rdpCommandCompleteSema);

        if (rglStatus == RGL_STATUS_CLOSED) {
            rdpThread = NULL;
            return 0;
        }
    }
    return 0;
}

void rdpSignalFullSync()
{
    SDL_SemPost(rdpCommandCompleteSema);
}
void rdpWaitFullSync()
{
    SDL_SemWait(rdpCommandCompleteSema);
}

void rdpPostCommand()
{
    int sync = rdp_store_list();
    SDL_SemPost(rdpCommandSema);
    if (!rglSettings.async)
        SDL_SemWait(rdpCommandCompleteSema);
    else if (sync) {
        rdpWaitFullSync();
        *gfx.MI_INTR_REG |= 0x20;
        gfx.CheckInterrupts();
    }

    waiting = 0;
}

void rdpCreateThread()
{
    if (!rdpCommandSema) {
        rdpCommandSema = SDL_CreateSemaphore(0);
        rdpCommandCompleteSema = SDL_CreateSemaphore(0);
    }
    if (!rdpThread) {
        LOG("Creating rdp thread\n");
#if SDL_VERSION_ATLEAST(2,0,0)
        rdpThread = SDL_CreateThread(rdpThreadFunc, "z64rdp", 0);
#else
        rdpThread = SDL_CreateThread(rdpThreadFunc, 0);
#endif
    }
}
#endif

void rdp_log(m64p_msg_level level, const char *msg, ...)
{
    char buf[1024];
    va_list args;
    va_start(args, msg);
    vsnprintf(buf, 1023, msg, args);
    buf[1023]='\0';
    va_end(args);
    if (l_DebugCallback)
    {
        l_DebugCallback(l_DebugCallContext, level, buf);
    }
}

#ifdef __cplusplus
extern "C" {
#endif

    EXPORT m64p_error CALL PluginStartup(m64p_dynlib_handle CoreLibHandle, void *Context,
        void (*DebugCallback)(void *, int, const char *))
    {
        ///* first thing is to set the callback function for debug info */
        l_DebugCallback = DebugCallback;
        l_DebugCallContext = Context;

        /* Get the core Video Extension function pointers from the library handle */
        CoreVideo_Init = (ptr_VidExt_Init) osal_dynlib_getproc(CoreLibHandle, "VidExt_Init");
        CoreVideo_Quit = (ptr_VidExt_Quit) osal_dynlib_getproc(CoreLibHandle, "VidExt_Quit");
        CoreVideo_ListFullscreenModes = (ptr_VidExt_ListFullscreenModes) osal_dynlib_getproc(CoreLibHandle, "VidExt_ListFullscreenModes");
        CoreVideo_SetVideoMode = (ptr_VidExt_SetVideoMode) osal_dynlib_getproc(CoreLibHandle, "VidExt_SetVideoMode");
        CoreVideo_SetCaption = (ptr_VidExt_SetCaption) osal_dynlib_getproc(CoreLibHandle, "VidExt_SetCaption");
        CoreVideo_ToggleFullScreen = (ptr_VidExt_ToggleFullScreen) osal_dynlib_getproc(CoreLibHandle, "VidExt_ToggleFullScreen");
	CoreVideo_ResizeWindow = (ptr_VidExt_ResizeWindow) osal_dynlib_getproc(CoreLibHandle, "VidExt_ResizeWindow");
        CoreVideo_GL_GetProcAddress = (ptr_VidExt_GL_GetProcAddress) osal_dynlib_getproc(CoreLibHandle, "VidExt_GL_GetProcAddress");
        CoreVideo_GL_SetAttribute = (ptr_VidExt_GL_SetAttribute) osal_dynlib_getproc(CoreLibHandle, "VidExt_GL_SetAttribute");
        CoreVideo_GL_SwapBuffers = (ptr_VidExt_GL_SwapBuffers) osal_dynlib_getproc(CoreLibHandle, "VidExt_GL_SwapBuffers");

        if (!CoreVideo_Init || !CoreVideo_Quit || !CoreVideo_ListFullscreenModes || !CoreVideo_SetVideoMode ||
            !CoreVideo_SetCaption || !CoreVideo_ToggleFullScreen || !CoreVideo_GL_GetProcAddress ||
            !CoreVideo_GL_SetAttribute || !CoreVideo_GL_SwapBuffers || !CoreVideo_ResizeWindow)
        {
            rdp_log(M64MSG_ERROR, "Couldn't connect to Core video functions");
            return M64ERR_INCOMPATIBLE;
        }

        /* attach and call the CoreGetAPIVersions function, check Config and Video Extension API versions for compatibility */
        ptr_CoreGetAPIVersions CoreAPIVersionFunc;
        CoreAPIVersionFunc = (ptr_CoreGetAPIVersions) osal_dynlib_getproc(CoreLibHandle, "CoreGetAPIVersions");
        if (CoreAPIVersionFunc == NULL)
        {
            rdp_log(M64MSG_ERROR, "Core emulator broken; no CoreAPIVersionFunc() function found.");
            return M64ERR_INCOMPATIBLE;
        }
        int ConfigAPIVersion, DebugAPIVersion, VidextAPIVersion;
        (*CoreAPIVersionFunc)(&ConfigAPIVersion, &DebugAPIVersion, &VidextAPIVersion, NULL);
        if ((ConfigAPIVersion & 0xffff0000) != (CONFIG_API_VERSION & 0xffff0000))
        {
            rdp_log(M64MSG_ERROR, "Emulator core Config API (v%i.%i.%i) incompatible with plugin (v%i.%i.%i)",
                    VERSION_PRINTF_SPLIT(ConfigAPIVersion), VERSION_PRINTF_SPLIT(CONFIG_API_VERSION));
            return M64ERR_INCOMPATIBLE;
        }
        if ((VidextAPIVersion & 0xffff0000) != (VIDEXT_API_VERSION & 0xffff0000))
        {
            rdp_log(M64MSG_ERROR, "Emulator core Video Extension API (v%i.%i.%i) incompatible with plugin (v%i.%i.%i)",
                    VERSION_PRINTF_SPLIT(VidextAPIVersion), VERSION_PRINTF_SPLIT(VIDEXT_API_VERSION));
            return M64ERR_INCOMPATIBLE;
        }

        /* Get the core config function pointers from the library handle */
        ConfigOpenSection = (ptr_ConfigOpenSection) osal_dynlib_getproc(CoreLibHandle, "ConfigOpenSection");
        ConfigSetParameter = (ptr_ConfigSetParameter) osal_dynlib_getproc(CoreLibHandle, "ConfigSetParameter");
        ConfigGetParameter = (ptr_ConfigGetParameter) osal_dynlib_getproc(CoreLibHandle, "ConfigGetParameter");
        ConfigSetDefaultInt = (ptr_ConfigSetDefaultInt) osal_dynlib_getproc(CoreLibHandle, "ConfigSetDefaultInt");
        ConfigSetDefaultFloat = (ptr_ConfigSetDefaultFloat) osal_dynlib_getproc(CoreLibHandle, "ConfigSetDefaultFloat");
        ConfigSetDefaultBool = (ptr_ConfigSetDefaultBool) osal_dynlib_getproc(CoreLibHandle, "ConfigSetDefaultBool");
        ConfigSetDefaultString = (ptr_ConfigSetDefaultString) osal_dynlib_getproc(CoreLibHandle, "ConfigSetDefaultString");
        ConfigGetParamInt = (ptr_ConfigGetParamInt) osal_dynlib_getproc(CoreLibHandle, "ConfigGetParamInt");
        ConfigGetParamFloat = (ptr_ConfigGetParamFloat) osal_dynlib_getproc(CoreLibHandle, "ConfigGetParamFloat");
        ConfigGetParamBool = (ptr_ConfigGetParamBool) osal_dynlib_getproc(CoreLibHandle, "ConfigGetParamBool");
        ConfigGetParamString = (ptr_ConfigGetParamString) osal_dynlib_getproc(CoreLibHandle, "ConfigGetParamString");
        if (!ConfigOpenSection   || !ConfigSetParameter    || !ConfigGetParameter ||
            !ConfigSetDefaultInt || !ConfigSetDefaultFloat || !ConfigSetDefaultBool || !ConfigSetDefaultString ||
            !ConfigGetParamInt   || !ConfigGetParamFloat   || !ConfigGetParamBool   || !ConfigGetParamString)
        {
            rdp_log(M64MSG_ERROR, "Couldn't connect to Core configuration functions");
            return M64ERR_INCOMPATIBLE;
        }

        rglReadSettings();

        return M64ERR_SUCCESS;
    }

    EXPORT m64p_error CALL PluginShutdown(void)
    {
        return M64ERR_SUCCESS;
    }

    EXPORT m64p_error CALL PluginGetVersion(m64p_plugin_type *PluginType, int *PluginVersion, int *APIVersion, const char **PluginNamePtr, int *Capabilities)
    {
        /* set version info */
        if (PluginType != NULL)
            *PluginType = M64PLUGIN_GFX;

        if (PluginVersion != NULL)
            *PluginVersion = PLUGIN_VERSION;

        if (APIVersion != NULL)
            *APIVersion = VIDEO_PLUGIN_API_VERSION;

        if (PluginNamePtr != NULL)
            *PluginNamePtr = "Z64gl";

        if (Capabilities != NULL)
        {
            *Capabilities = 0;
        }

        return M64ERR_SUCCESS;
    }

    EXPORT void CALL SetRenderingCallback(void (*callback)(int))
    {
        render_callback = callback;
    }

	EXPORT void CALL ReadScreen2(void *dest, int *width, int *height, int front)
	{
		LOG("ReadScreen\n");
		*width = rglSettings.resX;
		*height = rglSettings.resY;
		if (dest)
		{
			GLint oldMode;
			glGetIntegerv( GL_READ_BUFFER, &oldMode );
			if (front)
				glReadBuffer( GL_FRONT );
			else
				glReadBuffer( GL_BACK );
			glReadPixels( 0, 0, rglSettings.resX, rglSettings.resY,
						 GL_BGRA, GL_UNSIGNED_BYTE, dest );
			glReadBuffer( oldMode );
		}
    }

    EXPORT int CALL InitiateGFX (GFX_INFO Gfx_Info)
    {
        LOG("InitiateGFX\n");
        gfx = Gfx_Info;
        memset(rdpTiles, 0, sizeof(rdpTiles));
        memset(rdpTmem, 0, 0x1000);
        memset(&rdpState, 0, sizeof(rdpState));
#ifdef THREADED
        if (rglSettings.threaded)
            rdpCreateThread();
#endif
        return true;
    }

    EXPORT void CALL MoveScreen (int xpos, int ypos)
    {
    }

    EXPORT void CALL ChangeWindow()
    {
    }

    EXPORT void CALL ProcessDList(void)
    {
    }


    EXPORT void CALL ProcessRDPList(void)
    {
#ifdef THREADED
        if (rglSettings.threaded) {
            rdpCreateThread();
            rdpPostCommand();
        } else
#endif
        {
            rdp_process_list();
        }

        return;
    }

    EXPORT void CALL ResizeVideoOutput(int Width, int Height)
    {
    }

    EXPORT void CALL RomClosed (void)
    {
#ifdef THREADED
        if (rglSettings.threaded) {
            rglNextStatus = RGL_STATUS_CLOSED;
            do
                rdpPostCommand();
            while (rglStatus != RGL_STATUS_CLOSED);
        } else
#endif
        {
            rglNextStatus = rglStatus = RGL_STATUS_CLOSED;
            rglCloseScreen();
        }
    }

    EXPORT int CALL RomOpen()
    {
        int success = 1;
#ifdef THREADED
        if (rglSettings.threaded) {
            rdpCreateThread();
            //while (rglStatus != RGL_STATUS_CLOSED);
            rglNextStatus = RGL_STATUS_WINDOWED;
        }
        else
#endif
        {
            rglNextStatus = rglStatus = RGL_STATUS_WINDOWED;
            success = rglOpenScreen();
        }
        return success;
    }

    EXPORT void CALL ShowCFB (void)
    {
    }

    EXPORT void CALL UpdateScreen (void)
    {
#ifdef THREADED
        if (rglSettings.threaded) {
            rdpPostCommand();
        } else
#endif
        {
            rglUpdate();
        }
    }

    EXPORT void CALL ViStatusChanged (void)
    {
    }

    EXPORT void CALL ViWidthChanged (void)
    {
    }

#ifdef __cplusplus
}
#endif


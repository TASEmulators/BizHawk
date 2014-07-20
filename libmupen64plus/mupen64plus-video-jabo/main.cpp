/* Mupen64plus-video-jabo */

#include <windows.h>
#include <stdio.h>
#include <stdlib.h>
#include <iostream>
#include <fstream>

#define M64P_PLUGIN_PROTOTYPES 1
#include "m64p_types.h"
#include "m64p_plugin.h"
#include "m64p_common.h"
#include "m64p_config.h"

#include "main.h"
#include "typedefs.h"

#define LOG(x) { std::ofstream myfile; myfile.open ("jabo_wrapper_log.txt", std::ios::app); myfile << x << "\n"; myfile.close(); }

namespace OldAPI
{
	#include "jabo_api.h"
	ptr_InitiateGFX InitiateGFX = NULL;
	ptr_ProcessDList ProcessDList = NULL;
	ptr_ProcessRDPList ProcessRDPList = NULL;
	ptr_ShowCFB ShowCFB = NULL;
	ptr_ViStatusChanged ViStatusChanged = NULL;
	ptr_ViWidthChanged ViWidthChanged = NULL;
	ptr_RomOpen RomOpen = NULL;
	ptr_RomClosed RomClosed = NULL;
	ptr_CloseDLL CloseDLL = NULL;

	ptr_DrawScreen DrawScreen = NULL;
	ptr_MoveScreen MoveScreen = NULL;
	ptr_UpdateScreen UpdateScreen = NULL;
	ptr_DllConfig DllConfig = NULL;
	ptr_GetDllInfo GetDllInfo = NULL;
}

/* local variables */
static void (*l_DebugCallback)(void *, int, const char *) = NULL;
static void *l_DebugCallContext = NULL;
static int l_PluginInit = 0;

HMODULE JaboDLL;
HWND hWnd_jabo;

HMODULE D3D8Dll;

typedef void (*ptr_D3D8_SetRenderingCallback)(void (*callback)(int));
ptr_D3D8_SetRenderingCallback D3D8_SetRenderingCallback = NULL;
typedef void (*ptr_D3D8_ReadScreen)(void *dest, int *width, int *height);
ptr_D3D8_ReadScreen D3D8_ReadScreen = NULL;

void setup_jabo_functions()
{
	JaboDLL = LoadLibrary("Jabotard_Direct3D8.dll");

	if (JaboDLL != NULL)
	{
		OldAPI::InitiateGFX = (OldAPI::ptr_InitiateGFX)GetProcAddress(JaboDLL,"InitiateGFX");
		OldAPI::ProcessDList = (OldAPI::ptr_ProcessDList)GetProcAddress(JaboDLL,"ProcessDList");
		OldAPI::ProcessRDPList = (OldAPI::ptr_ProcessRDPList)GetProcAddress(JaboDLL,"ProcessRDPList");
		OldAPI::ShowCFB = (OldAPI::ptr_ShowCFB)GetProcAddress(JaboDLL,"ShowCFB");
		OldAPI::ViStatusChanged = (OldAPI::ptr_ViStatusChanged)GetProcAddress(JaboDLL,"ViStatusChanged");
		OldAPI::ViWidthChanged = (OldAPI::ptr_ViWidthChanged)GetProcAddress(JaboDLL,"ViWidthChanged");
		OldAPI::RomOpen = (OldAPI::ptr_RomOpen)GetProcAddress(JaboDLL,"RomOpen");
		OldAPI::RomClosed = (OldAPI::ptr_RomClosed)GetProcAddress(JaboDLL,"RomClosed");
		OldAPI::CloseDLL = (OldAPI::ptr_CloseDLL)GetProcAddress(JaboDLL,"CloseDLL");
		
		OldAPI::DrawScreen = (OldAPI::ptr_DrawScreen)GetProcAddress(JaboDLL,"DrawScreen");
		OldAPI::MoveScreen = (OldAPI::ptr_MoveScreen)GetProcAddress(JaboDLL,"MoveScreen");
		OldAPI::UpdateScreen = (OldAPI::ptr_UpdateScreen)GetProcAddress(JaboDLL,"UpdateScreen");
		OldAPI::DllConfig = (OldAPI::ptr_DllConfig)GetProcAddress(JaboDLL,"DllConfig");
		OldAPI::GetDllInfo = (OldAPI::ptr_GetDllInfo)GetProcAddress(JaboDLL,"GetDllInfo");
	}

	D3D8Dll = LoadLibrary("D3D8.dll");
	if (D3D8Dll != NULL)
	{
		D3D8_SetRenderingCallback = (ptr_D3D8_SetRenderingCallback)GetProcAddress(D3D8Dll,"SetRenderingCallback");
		D3D8_ReadScreen = (ptr_D3D8_ReadScreen)GetProcAddress(D3D8Dll,"ReadScreen");
	}
}

/* Global functions */
static void DebugMessage(int level, const char *message, ...)
{
  char msgbuf[1024];
  va_list args;

  if (l_DebugCallback == NULL)
      return;

  va_start(args, message);
  vsprintf(msgbuf, message, args);

  (*l_DebugCallback)(l_DebugCallContext, level, msgbuf);

  va_end(args);
}

#pragma region (De-)Initialization
/* Mupen64Plus plugin functions */
// TODO
EXPORT m64p_error CALL PluginStartup(m64p_dynlib_handle CoreLibHandle, void *Context,
                                   void (*DebugCallback)(void *, int, const char *))
{
	LOG("API WRAPPER:\t PluginStartup")
	setup_jabo_functions();

    ptr_CoreGetAPIVersions CoreAPIVersionFunc;
    
    int ConfigAPIVersion, DebugAPIVersion, VidextAPIVersion;
    
    if (l_PluginInit)
        return M64ERR_ALREADY_INIT;

    /* first thing is to set the callback function for debug info */
    l_DebugCallback = DebugCallback;
    l_DebugCallContext = Context;

    /* attach and call the CoreGetAPIVersions function, check Config API version for compatibility */
    CoreAPIVersionFunc = (ptr_CoreGetAPIVersions) GetProcAddress(CoreLibHandle, "CoreGetAPIVersions");

    if (CoreAPIVersionFunc == NULL)
    {
        DebugMessage(M64MSG_ERROR, "Core emulator broken; no CoreAPIVersionFunc() function found.");
        return M64ERR_INCOMPATIBLE;
    }
    
    (*CoreAPIVersionFunc)(&ConfigAPIVersion, &DebugAPIVersion, &VidextAPIVersion, NULL);
    if ((ConfigAPIVersion & 0xffff0000) != (CONFIG_API_VERSION & 0xffff0000))
    {
        DebugMessage(M64MSG_ERROR, "Emulator core Config API (v%i.%i.%i) incompatible with plugin (v%i.%i.%i)",
                VERSION_PRINTF_SPLIT(ConfigAPIVersion), VERSION_PRINTF_SPLIT(CONFIG_API_VERSION));
        return M64ERR_INCOMPATIBLE;
    }

    l_PluginInit = 1;
    return M64ERR_SUCCESS;
}

// TODO
EXPORT m64p_error CALL PluginShutdown(void)
{
	LOG("API WRAPPER:\t PluginShutdown")
	OldAPI::CloseDLL();

    if (!l_PluginInit)
        return M64ERR_NOT_INIT;

    /* reset some local variables */
    l_DebugCallback = NULL;
    l_DebugCallContext = NULL;

    l_PluginInit = 0;
    return M64ERR_SUCCESS;
}

// TODO
EXPORT int CALL RomOpen(void)
{
	LOG("API WRAPPER:\t RomOpen")
	OldAPI::RomOpen();

    if (!l_PluginInit)
        return 0;

    return 1;
}

// TODO
EXPORT void CALL RomClosed( void )
{
	LOG("API WRAPPER:\t RomClosed")
	OldAPI::RomClosed();

    if (!l_PluginInit)
        return;
}

#pragma endregion

#pragma region Pluginversion
EXPORT m64p_error CALL PluginGetVersion(m64p_plugin_type *PluginType, int *PluginVersion, int *APIVersion, const char **PluginNamePtr, int *Capabilities)
{
	LOG("API WRAPPER:\t PluginGetVersion")
	
	OldAPI::PLUGIN_INFO info;
	OldAPI::GetDllInfo(&info);

    /* set version info */
    if (PluginType != NULL)
        *PluginType = M64PLUGIN_GFX;

    if (PluginVersion != NULL)
        *PluginVersion = PLUGIN_VERSION;

    if (APIVersion != NULL)
        *APIVersion = VIDEO_PLUGIN_API_VERSION;
    
    if (PluginNamePtr != NULL)
        *PluginNamePtr = PLUGIN_NAME;

    if (Capabilities != NULL)
    {
        *Capabilities = 0;
    }
                    
    return M64ERR_SUCCESS;
}
#pragma endregion

// IGNORE
EXPORT void CALL ChangeWindow (void)
{
	LOG("API WRAPPER:\t ChangeWindow")
}

// NOTE: NEW GFX_INFO vs old
EXPORT int CALL InitiateGFX(GFX_INFO Gfx_Info)
{
	LOG("API WRAPPER:\t InitiateGFX")

	OldAPI::GFX_INFO blah;

	blah.hWnd = hWnd_jabo;
	blah.hStatusBar = NULL;
	blah.MemoryBswaped = true;

	blah.HEADER = Gfx_Info.HEADER;

	blah.RDRAM = Gfx_Info.RDRAM;
	blah.DMEM = Gfx_Info.DMEM;
	blah.IMEM = Gfx_Info.IMEM;

	blah.MI_INTR_REG = (DWORD *)Gfx_Info.MI_INTR_REG;

	blah.DPC_START_REG = (DWORD *)Gfx_Info.DPC_START_REG;
	blah.DPC_END_REG = (DWORD *)Gfx_Info.DPC_END_REG;
	blah.DPC_CURRENT_REG = (DWORD *)Gfx_Info.DPC_CURRENT_REG;
	blah.DPC_STATUS_REG = (DWORD *)Gfx_Info.DPC_STATUS_REG;
	blah.DPC_CLOCK_REG = (DWORD *)Gfx_Info.DPC_CLOCK_REG;
	blah.DPC_BUFBUSY_REG = (DWORD *)Gfx_Info.DPC_BUFBUSY_REG;
	blah.DPC_PIPEBUSY_REG = (DWORD *)Gfx_Info.DPC_PIPEBUSY_REG;
	blah.DPC_TMEM_REG = (DWORD *)Gfx_Info.DPC_TMEM_REG;

	blah.VI_STATUS_REG = (DWORD *)Gfx_Info.VI_STATUS_REG;
	blah.VI_ORIGIN_REG = (DWORD *)Gfx_Info.VI_ORIGIN_REG;
	blah.VI_WIDTH_REG = (DWORD *)Gfx_Info.VI_WIDTH_REG;
	blah.VI_INTR_REG = (DWORD *)Gfx_Info.VI_INTR_REG;
	blah.VI_V_CURRENT_LINE_REG = (DWORD *)Gfx_Info.VI_V_CURRENT_LINE_REG;
	blah.VI_TIMING_REG = (DWORD *)Gfx_Info.VI_TIMING_REG;
	blah.VI_V_SYNC_REG = (DWORD *)Gfx_Info.VI_V_SYNC_REG;
	blah.VI_H_SYNC_REG = (DWORD *)Gfx_Info.VI_H_SYNC_REG;
	blah.VI_LEAP_REG = (DWORD *)Gfx_Info.VI_LEAP_REG;
	blah.VI_H_START_REG = (DWORD *)Gfx_Info.VI_H_START_REG;
	blah.VI_V_START_REG = (DWORD *)Gfx_Info.VI_V_START_REG;
	blah.VI_V_BURST_REG = (DWORD *)Gfx_Info.VI_V_BURST_REG;
	blah.VI_X_SCALE_REG = (DWORD *)Gfx_Info.VI_X_SCALE_REG;
	blah.VI_Y_SCALE_REG = (DWORD *)Gfx_Info.VI_Y_SCALE_REG;

	blah.CheckInterrupts = Gfx_Info.CheckInterrupts;

	OldAPI::InitiateGFX(blah);
	OldAPI::DllConfig(hWnd_jabo);

    return(TRUE);
}

EXPORT void CALL MoveScreen (int xpos, int ypos)
{ 
	LOG("API WRAPPER:\t MoveScreen")
	OldAPI::MoveScreen(xpos, ypos);
}

EXPORT void CALL ProcessDList(void)
{
	LOG("API WRAPPER:\t ProcessDList")
	OldAPI::ProcessDList();
}   

EXPORT void CALL ProcessRDPList(void)
{
	LOG("API WRAPPER:\t ProcessRDPList")
	OldAPI::ProcessRDPList();
} 

EXPORT void CALL ShowCFB(void)
{
	LOG("API WRAPPER:\t ShowCFB")
	OldAPI::ShowCFB();
}

EXPORT void CALL UpdateScreen(void)
{
	LOG("API WRAPPER:\t UpdateScreen")
	OldAPI::UpdateScreen();
}

EXPORT void CALL ViStatusChanged(void)
{
	LOG("API WRAPPER:\t ViStatusChanged")
	OldAPI::ViStatusChanged();
}

EXPORT void CALL ViWidthChanged(void)
{
	LOG("API WRAPPER:\t ViWidthChanged")
	OldAPI::ViWidthChanged();
}

// TODO
EXPORT void CALL ReadScreen2(void *dest, int *width, int *height, int bFront)
{
	LOG("API WRAPPER:\t ReadScreen2")
	if (D3D8_ReadScreen != NULL)
	{
		D3D8_ReadScreen(dest, width, height);
	}
	//*width = 800;
	//*height = 600;
}

// TODO
EXPORT void CALL SetRenderingCallback(void (*callback)(int))
{
	LOG("API WRAPPER:\t SetRenderingCallback")
	if (D3D8_SetRenderingCallback != NULL)
	{
		D3D8_SetRenderingCallback(callback);
	}
}

// IMPLEMENT LATER?
EXPORT void CALL FBRead(uint32 addr)
{
	LOG("API WRAPPER:\t FBRead")
}

// IMPLEMENT LATER?
EXPORT void CALL FBWrite(uint32 addr, uint32 size)
{
	LOG("API WRAPPER:\t FBWrite")
}

// ???
EXPORT void CALL FBGetFrameBufferInfo(void *p)
{
	LOG("API WRAPPER:\t FBGetFrameBufferInfo")
    //FrameBufferInfo * pinfo = (FrameBufferInfo *)p;
}



/* Simple Window code */

HINSTANCE  inj_hModule;          //Injected Modules Handle

//WndProc for the new window
LRESULT CALLBACK DLLWindowProc (HWND, UINT, WPARAM, LPARAM);

//Register our windows Class
BOOL RegisterDLLWindowClass(char szClassName[])
{
    WNDCLASSEX wc;

	ZeroMemory(&wc, sizeof(WNDCLASSEX));

    wc.cbSize = sizeof (WNDCLASSEX);
	wc.style = CS_HREDRAW | CS_VREDRAW;
	wc.lpfnWndProc = DLLWindowProc;
	wc.hInstance =  inj_hModule;
	wc.hCursor = LoadCursor (NULL, IDC_ARROW);
	wc.hbrBackground = (HBRUSH) COLOR_WINDOW;
    wc.lpszClassName = szClassName;
    
    if (!RegisterClassEx (&wc))
		return 0;
}

//The new thread
DWORD WINAPI ThreadProc( LPVOID lpParam )
{
    MSG messages;
	char *pString = (char *)(lpParam);
	RegisterDLLWindowClass("InjectedDLLWindowClass");
	hWnd_jabo = CreateWindowEx (0, "InjectedDLLWindowClass", pString, WS_OVERLAPPEDWINDOW, 300, 300, 400, 300, NULL, NULL,inj_hModule, NULL );
	ShowWindow (hWnd_jabo, SW_SHOWNORMAL);
    while (GetMessage (&messages, NULL, 0, 0))
    {
		TranslateMessage(&messages);
        DispatchMessage(&messages);
    }
    return 1;
}

//Our new windows proc
LRESULT CALLBACK DLLWindowProc (HWND hwnd, UINT message, WPARAM wParam, LPARAM lParam)
{
    switch (message)
    {
		case WM_COMMAND:
               break;
		case WM_DESTROY:
			PostQuitMessage (0);
			break;
		default:
			return DefWindowProc (hwnd, message, wParam, lParam);
    }
    return 0;
}

BOOL APIENTRY DllMain( HMODULE hModule, DWORD  ul_reason_for_call,LPVOID lpReserved)
{
	if(ul_reason_for_call==DLL_PROCESS_ATTACH) {
		inj_hModule = hModule;
		CreateThread(0, NULL, ThreadProc, (LPVOID)"Window Title", NULL, NULL);
	}
	return TRUE;
}

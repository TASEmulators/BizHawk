/* Mupen64plus-video-jabo */

#include <windows.h>
#include <stdio.h>
#include <stdlib.h>
#include <iostream>
#include <fstream>
#include <iomanip>

#define M64P_PLUGIN_PROTOTYPES 1
#include "m64p_types.h"
#include "m64p_plugin.h"
#include "m64p_common.h"
#include "m64p_config.h"

#include "main.h"
#include "typedefs.h"
#include "Config.h"	

//#define LOG(x) { std::ofstream myfile; myfile.open ("jabo_wrapper_log.txt", std::ios::app); myfile << x << "\n"; myfile.close(); }
#define LOG(x)

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

/* local variables */
static void (*l_DebugCallback)(void *, int, const char *) = NULL;
static void *l_DebugCallContext = NULL;
static int l_PluginInit = 0;

HMODULE JaboDLL;

HMODULE D3D8Dll;

typedef void (*ptr_D3D8_SetRenderingCallback)(void (*callback)(int));
ptr_D3D8_SetRenderingCallback D3D8_SetRenderingCallback = NULL;
typedef void (*ptr_D3D8_ReadScreen)(void *dest, int *width, int *height);
ptr_D3D8_ReadScreen D3D8_ReadScreen = NULL;
typedef void (*ptr_D3D8_CloseDLL)();
ptr_D3D8_CloseDLL D3D8_CloseDLL = NULL;

DWORD old_options;
DWORD old_initflags;

void setup_jabo_functions()
{
	JaboDLL = LoadLibrary("Jabo_Direct3D8_patched.dll");

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
		D3D8_CloseDLL = (ptr_D3D8_CloseDLL)GetProcAddress(D3D8Dll,"CloseDLL");
	}
}

BOOL readOptionsInitflags (DWORD* options_val, DWORD* initflags_val)
{
	HKEY mainkey;
	if (RegOpenKeyEx(HKEY_CURRENT_USER,"Software\\JaboSoft\\Project64 DLL\\Direct3D8 1.6.1",0,KEY_READ,&mainkey) != ERROR_SUCCESS)
	{
		// key doesn't exist, so we need to create it first
		if (RegCreateKeyEx(HKEY_CURRENT_USER,"Software\\JaboSoft\\Project64 DLL\\Direct3D8 1.6.1",NULL,NULL,NULL,KEY_READ,NULL,&mainkey,NULL) != ERROR_SUCCESS)
		{
			// Couldn't create the key
			printf("readOptionsInitflags:  Couldn't create the key\n");
			return (FALSE);
		}
	}

	// Key exists, try to find the Options Value
	DWORD type;
	DWORD cbData;
	int options_value;
	LSTATUS result = RegQueryValueEx(mainkey, "Options", NULL, &type, (LPBYTE)&options_value, &cbData);
	if (result != ERROR_SUCCESS)
	{
		options_value = 0;
		printf("readOptionsInitflags:  fail A\n");
	}
	*options_val = options_value;

	// Try to find the Direct3D init flags subkey Value
	int initflags_value;
	result = RegQueryValueEx(mainkey, "Direct3D8.InitFlags", NULL, &type, (LPBYTE)&initflags_value, &cbData);
	if (result != ERROR_SUCCESS)
	{
		printf("readOptionsInitflags:  fail B\n");
		initflags_value = 0x00e00000;
	}
	*initflags_val = initflags_value;

	RegCloseKey(mainkey);
	return(TRUE);
}

BOOL writeOptionsInitflags(DWORD options_val, DWORD initflags_val)
{
	// Open the key for writing
	HKEY mainkey;
	if (RegOpenKeyEx(HKEY_CURRENT_USER,"Software\\JaboSoft\\Project64 DLL\\Direct3D8 1.6.1",0,KEY_WRITE,&mainkey) != ERROR_SUCCESS)
	{
		printf("writeOptionsInitflags: Failure to open key for write\n");
		return (FALSE);
	}

	// Store our options value
	DWORD new_val = options_val;
	if (RegSetValueEx(mainkey, "Options", NULL, REG_DWORD, (BYTE *)&new_val, 4) != ERROR_SUCCESS)
	{
		printf("writeOptionsInitflags: Couldn't write options value\n");
	}

	// Store our init flags value
	new_val = initflags_val;
	if (RegSetValueEx(mainkey, "Direct3D8.InitFlags", NULL, REG_DWORD, (BYTE *)&new_val, 4) != ERROR_SUCCESS)
	{
		LOG("writeOptionsInitflags: Couldn't write init flags value");
	}

	RegCloseKey(mainkey);
	return(TRUE);
}

void createRDBFile(unsigned char * header, int resolution_width, int resolution_height, int clear_mode)
{
	std::ofstream rdbFile;
	rdbFile.open("Project64.rdb", std::ios::trunc | std::ios::out);

	// File can't seem to have data on the first line. It has to be a comment or blank
	rdbFile << "\n";

	rdbFile << "[";

	rdbFile << std::hex << std::setfill('0') << std::setw(2) << std::uppercase;
	rdbFile << (int)header[16] << (int)header[17] << (int)header[18] << (int)header[19];
	rdbFile << "-";
	rdbFile << (int)header[20] << (int)header[21] << (int)header[22] << (int)header[23];
	rdbFile << "-C:";
	rdbFile << (int)header[62] << "]\n";

	rdbFile << std::dec << std::nouppercase;
	rdbFile << "Clear Frame=" << clear_mode << "\n";
	rdbFile << "Resolution Width=" << resolution_width << "\n";
	rdbFile << "Resolution Height=" << resolution_height << "\n";
	rdbFile.close();
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

	ConfigOpenSection = (ptr_ConfigOpenSection) GetProcAddress(CoreLibHandle, "ConfigOpenSection");
    ConfigSetParameter = (ptr_ConfigSetParameter) GetProcAddress(CoreLibHandle, "ConfigSetParameter");
    ConfigGetParameter = (ptr_ConfigGetParameter) GetProcAddress(CoreLibHandle, "ConfigGetParameter");
    ConfigSetDefaultInt = (ptr_ConfigSetDefaultInt) GetProcAddress(CoreLibHandle, "ConfigSetDefaultInt");
    ConfigSetDefaultFloat = (ptr_ConfigSetDefaultFloat) GetProcAddress(CoreLibHandle, "ConfigSetDefaultFloat");
    ConfigSetDefaultBool = (ptr_ConfigSetDefaultBool) GetProcAddress(CoreLibHandle, "ConfigSetDefaultBool");
    ConfigSetDefaultString = (ptr_ConfigSetDefaultString) GetProcAddress(CoreLibHandle, "ConfigSetDefaultString");
    ConfigGetParamInt = (ptr_ConfigGetParamInt) GetProcAddress(CoreLibHandle, "ConfigGetParamInt");
    ConfigGetParamFloat = (ptr_ConfigGetParamFloat) GetProcAddress(CoreLibHandle, "ConfigGetParamFloat");
    ConfigGetParamBool = (ptr_ConfigGetParamBool) GetProcAddress(CoreLibHandle, "ConfigGetParamBool");
    ConfigGetParamString = (ptr_ConfigGetParamString) GetProcAddress(CoreLibHandle, "ConfigGetParamString");

    l_PluginInit = 1;
    return M64ERR_SUCCESS;
}

EXPORT m64p_error CALL PluginShutdown(void)
{
	LOG("API WRAPPER:\t PluginShutdown")
	OldAPI::CloseDLL();

	D3D8_CloseDLL();
	FreeLibrary(D3D8Dll);
	FreeLibrary(JaboDLL);

	writeOptionsInitflags(old_options,old_initflags);

    if (!l_PluginInit)
        return M64ERR_NOT_INIT;

    /* reset some local variables */
    l_DebugCallback = NULL;
    l_DebugCallContext = NULL;

    l_PluginInit = 0;
    return M64ERR_SUCCESS;
}

EXPORT int CALL RomOpen(void)
{
	LOG("API WRAPPER:\t RomOpen")
	OldAPI::RomOpen();

	remove("Project64.rdb");

    if (!l_PluginInit)
        return 0;

    return 1;
}

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

	Config_Open();

	SETTINGS settings;
	settings.anisotropic_level = (int)Config_ReadInt("anisotropic_level","ANISOTROPIC_FILTERING_LEVEL",0,TRUE,FALSE);
	settings.brightness = (int)Config_ReadInt("brightness","Brightness level",0,TRUE,FALSE);
	settings.antialiasing_level = (int)Config_ReadInt("antialiasing_level","Antialiasing level",0,TRUE,FALSE);
	settings.super2xsal = (BOOL)Config_ReadInt("super2xsal","Enables Super2xSal textures",FALSE);
	settings.texture_filter = (BOOL)Config_ReadInt("texture_filter","Always use texture filter",FALSE);
	settings.adjust_aspect_ratio = (BOOL)Config_ReadInt("adjust_aspect_ratio","Adjust game aspect ratio to match yours",FALSE);
	settings.legacy_pixel_pipeline = (BOOL)Config_ReadInt("legacy_pixel_pipeline","Use legacy pixel pipeline",FALSE);
	settings.alpha_blending = (BOOL)Config_ReadInt("alpha_blending","Force alpha blending",FALSE);

	// As far as I can tell there is no way to apply this setting without opening the dll config window
	//settings.wireframe = (BOOL)Config_ReadInt("wireframe","Wireframe rendering",FALSE);

	settings.direct3d_transformation_pipeline = (BOOL)Config_ReadInt("direct3d_transformation_pipeline","Use Direct3D transformation pipeline",FALSE);
	settings.z_compare = (BOOL)Config_ReadInt("z_compare","Force Z Compare",FALSE);
	settings.copy_framebuffer = (BOOL)Config_ReadInt("copy_framebuffer","Copy framebuffer to RDRAM",FALSE);
	settings.resolution_width = (int)Config_ReadInt("resolution_width","Emulated Width",-1,TRUE,FALSE);
	settings.resolution_height = (int)Config_ReadInt("resolution_height","Emulated Height",-1,TRUE,FALSE);
	settings.clear_mode = (int)Config_ReadInt("clear_mode","Direct3D Clear Mode Height",0,TRUE,FALSE);

	DWORD new_options_val = 0;
	if (settings.copy_framebuffer == TRUE) { new_options_val |= 0x20000000; }
	if (settings.z_compare == TRUE) { new_options_val |= 0x10000000; }
	if (settings.legacy_pixel_pipeline == TRUE) { new_options_val |= 0x08000000; }
	if (settings.alpha_blending == TRUE) { new_options_val |= 0x04000000; }
	if (settings.adjust_aspect_ratio == TRUE) { new_options_val |= 0x02000000; }
	if (settings.texture_filter == TRUE) { new_options_val |= 0x01000000; }
	if (settings.super2xsal == TRUE) { new_options_val |= 0x00001000; }
	new_options_val |= (((settings.brightness - 100) / 3) & 0x1F) << 19;
	switch (settings.antialiasing_level)
	{
		case 1: new_options_val |= 0x00004000; break;
		case 2: new_options_val |= 0x00008000; break;
		case 3: new_options_val |= 0x00010000; break;
	}
	switch (settings.anisotropic_level)
	{
		case 1: new_options_val |= 0x00000020; break;
		case 2: new_options_val |= 0x00000040; break;
		case 3: new_options_val |= 0x00000080; break;
		case 4: new_options_val |= 0x00000100; break;
	}

	int width, height;
	Config_ReadScreenResolution(&width,&height);
	if (width == 320 && height == 240) { new_options_val |= 0x00000000; }
	else if (width == 400 && height == 300) { new_options_val |= 0x00000001; } 
	else if (width == 512 && height == 384) { new_options_val |= 0x00000002; } 
	else if (width == 640 && height == 480) { new_options_val |= 0x00000003; } 
	else if (width == 800 && height == 600) { new_options_val |= 0x00000004; } 
	else if (width == 1024 && height == 768) { new_options_val |= 0x00000005; } 
	else if (width == 1152 && height == 864) { new_options_val |= 0x00000006; } 
	else if (width == 1280 && height == 960) { new_options_val |= 0x00000007; } 
	else if (width == 1600 && height == 1200) { new_options_val |= 0x00000008; } 
	else if (width == 848 && height == 480) { new_options_val |= 0x00000009; } 
	else if (width == 1024 && height == 576) { new_options_val |= 0x0000000a; } 
	else if (width == 1380 && height == 768) { new_options_val |= 0x0000000b; } 
	else { /* will pick 320x240 */ }

	DWORD new_initflags_val = 0x00e00000;
	if (settings.direct3d_transformation_pipeline == TRUE) { new_initflags_val = 0x00a00000; }

	readOptionsInitflags(&old_options,&old_initflags);

	writeOptionsInitflags(new_options_val,new_initflags_val);

	createRDBFile(Gfx_Info.HEADER, settings.resolution_height, settings.resolution_width, settings.clear_mode);

	OldAPI::GFX_INFO blah;

	blah.hWnd = GetDesktopWindow();
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

EXPORT void CALL ReadScreen2(void *dest, int *width, int *height, int bFront)
{
	LOG("API WRAPPER:\t ReadScreen2")
	if (D3D8_ReadScreen != NULL)
	{
		D3D8_ReadScreen(dest, width, height);
	}
}

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

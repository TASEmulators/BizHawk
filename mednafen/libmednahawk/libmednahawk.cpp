#include "mednafen.h"
#include <stdio.h>
#include <vector>

#include "drivers/input.h"
#include "drivers/main.h"
#include "mempatcher.h"
#include "settings-driver.h"
#include "FileWrapper.h"

MDFNGI *CurGame=NULL;

extern MDFNGI EmulatedPSX;
void BuildPortsInfo(MDFNGI *gi);

void MDFND_PrintError(const char *s)
{
	printf(s);
}

void MDFND_Message(const char *s)
{
	printf(s);
}

static MDFNSetting MednafenSettings[] =
{
  { "filesys.untrusted_fip_check", MDFNSF_NOFLAGS, gettext_noop("Enable untrusted file-inclusion path security check."), gettext_noop("When this setting is set to \"1\", the default, paths to files referenced from files like CUE sheets and PSF rips are checked for certain characters that can be used in directory traversal, and if found, loading is aborted.  Set it to \"0\" if you want to allow constructs like absolute paths in CUE sheets, but only if you understand the security implications of doing so(see \"Security Issues\" section in the documentation)."), MDFNST_BOOL, "0" },
	{ "filesys.path_firmware", MDFNSF_NOFLAGS, gettext_noop("Path to directory for firmware."), NULL, MDFNST_STRING, "firmware" },
	{ "filesys.path_sav", MDFNSF_NOFLAGS, gettext_noop("Path to directory for save games and nonvolatile memory."), gettext_noop("WARNING: Do not set this path to a directory that contains Famicom Disk System disk images, or you will corrupt them when you load an FDS game and exit Mednafen."), MDFNST_STRING, "sav" },
	{ "filesys.fname_sav", MDFNSF_NOFLAGS, gettext_noop("Format string for save games filename."), gettext_noop("WARNING: %x should always be included, otherwise you run the risk of overwriting save data for games that create multiple save data files.\n\nSee fname_format.txt for more information.  Edit at your own risk."), MDFNST_STRING, "%F.%M%x" },
	
	//"dynamic" settings needed probably for each console
	{ "psx.tblur", MDFNSF_NOFLAGS, NULL, NULL, MDFNST_BOOL, "0" },
	{ "psx.forcemono", MDFNSF_NOFLAGS, NULL, NULL, MDFNST_BOOL, "0" },

	//       BuildDynamicSetting(&setting, sysname, "tblur.accum", MDFNSF_COMMON_TEMPLATE | MDFNSF_CAT_VIDEO, CSD_tblur_accum, MDFNST_BOOL, "0");
  //       BuildDynamicSetting(&setting, sysname, "tblur.accum.amount", MDFNSF_COMMON_TEMPLATE | MDFNSF_CAT_VIDEO, CSD_tblur_accum_amount, MDFNST_FLOAT, "50", "0", "100");

	{ NULL }
};


static MDFN_Surface *VTBuffer[2] = { NULL, NULL };
static MDFN_Rect *VTLineWidths[2] = { NULL, NULL };
int16 soundbuf[1024*1024]; //how big? big enough.
int VTBackBuffer = 0;
static MDFN_Rect VTDisplayRects[2];
void FrameAdvance()
{
	EmulateSpecStruct espec;
	memset(&espec, 0, sizeof(EmulateSpecStruct));

	uint32 pitch32 = CurGame->fb_width; 
	MDFN_PixelFormat nf(MDFN_COLORSPACE_RGB, 16, 8, 0, 24);

	if(VTBuffer[0] == NULL || VTBuffer[0]->w != CurGame->fb_width || VTBuffer[0]->h != CurGame->fb_height)
	{
		if(VTBuffer[0]) delete VTBuffer[0];
		if(VTLineWidths[0]) free(VTLineWidths[0]);
		VTBuffer[0] = new MDFN_Surface(NULL, CurGame->fb_width, CurGame->fb_height, CurGame->fb_width, nf);
		VTLineWidths[0] = (MDFN_Rect *)calloc(CurGame->fb_height, sizeof(MDFN_Rect));
	}

	espec.surface = (MDFN_Surface *)VTBuffer[VTBackBuffer];
	espec.LineWidths = (MDFN_Rect *)VTLineWidths[VTBackBuffer];
	espec.skip = false;
	espec.soundmultiplier = 1.0;
	espec.NeedRewind = false;

	//espec.SoundRate = GetSoundRate();
	//espec.SoundBuf = GetEmuModSoundBuffer(&espec.SoundBufMaxSize);
	//espec.SoundVolume = (double)MDFN_GetSettingUI("sound.volume") / 100;

	espec.SoundBufMaxSize = 1024*1024;
	espec.SoundRate = 44100;
	espec.SoundBuf = soundbuf;
	espec.SoundVolume = 1.0;


	MDFNI_Emulate(&espec);

	VTDisplayRects[VTBackBuffer] = espec.DisplayRect;

	//TODO sound
	//sound = espec.SoundBuf + (espec.SoundBufSizeALMS * CurGame->soundchan);
	//ssize = espec.SoundBufSize - espec.SoundBufSizeALMS;
}

extern "C" __declspec(dllexport) void close()
{
	MDFNI_CloseGame();
}

enum eProp
{
	GetPtr_FramebufferPointer,
	GetPtr_FramebufferPitchPixels,
	GetPtr_FramebufferWidth,
	GetPtr_FramebufferHeight,
	SetPtr_FopenCallback,
	SetPtr_FcloseCallback,
	SetPtr_FopCallback
};

typedef void* (*t_FopenCallback)(const char* fname, const char* mode);
static t_FopenCallback FopenCallback;

typedef int (*t_FcloseCallback)(void* fp);
static t_FcloseCallback FcloseCallback;

typedef int64 (*t_FopCallback)(int op, void* ptr, int64 a, int64 b, FILE* fp);
static t_FopCallback FopCallback;

extern "C" __declspec(dllexport) void dll_SetPropPtr(int prop, void* ptr)
{
	switch((eProp)prop)
	{
	case SetPtr_FopenCallback: FopenCallback = (t_FopenCallback)ptr; break;
	case SetPtr_FcloseCallback: FcloseCallback = (t_FcloseCallback)ptr; break;
	case SetPtr_FopCallback: FopCallback = (t_FopCallback)ptr; break;
	}
}

extern "C" __declspec(dllexport) void* dll_GetPropPtr(int prop)
{
	switch((eProp)prop)
	{
	case GetPtr_FramebufferPointer:
		return VTBuffer[0]->pixels;
	case GetPtr_FramebufferPitchPixels:
		return (void*)VTBuffer[0]->pitchinpix;
	case GetPtr_FramebufferWidth:
		return (void*)VTBuffer[0]->w;
	case GetPtr_FramebufferHeight:
		return (void*)VTBuffer[0]->h;
	default:
		return 0;
	}
}

extern "C" __declspec(dllexport) bool psx_LoadCue(const char* path)
{
	MDFNI_CloseGame(); //todo - copy CloseGame into api.. dont like everything i see in there

	//load the psx game
	MDFNGI *gi = CurGame = MDFNI_LoadCD("psx", path);
	
	//does some kind of input hooking-up
	//BuildPortsInfo(gi);

	return true;
}

extern "C" __declspec(dllexport) void psx_FrameAdvance()
{
	FrameAdvance();
}

extern "C" __declspec(dllexport) bool dll_Initialize()
{
	//initialize emulator cores according to a list maintained inside this method
	std::vector<MDFNGI *> ExternalSystems; //wtf are external systems then? custom ones not known to mednafen's internals?
	MDFNI_InitializeModules(ExternalSystems);

	MDFNI_SetBaseDirectory("$psx");

	//add settings specific to each core
	for(unsigned int x = 0; x < MDFNSystems.size(); x++)
	{
	 if(MDFNSystems[x]->Settings)
	  MDFN_MergeSettings(MDFNSystems[x]->Settings);
	}

	//prep settings. for now we reuse a lot of mednafen's internal settings building. i would prefer to have supported settings enumerated in here
	static std::vector <MDFNSetting> NeoDriverSettings; //these better be static or else shit will explode
	//MakeInputSettings(NeoDriverSettings);
	MDFN_MergeSettings(NeoDriverSettings);
	MDFN_MergeSettings(MednafenSettings);

	//settings hacks
	MDFNI_SetSetting("filesys.path_firmware","firmware");
	MDFNI_SetSetting("filesys.path_sav","sav");

	//cheats.. crashes without it, maybe we'll want to use the cheat system later
	MDFN_MergeSettings(MDFNMP_Settings);

	return true;
}
	

//---------

FILE* headless_fopen(const char* path, const char* mode)
{
	return (FILE*)FopenCallback(path,mode);
}

int headless_fclose(FILE* fp)
{
	return FcloseCallback(fp);
}

int64 headless_fop(FOP op, void* ptr, int64 a, int64 b, FILE* fp)
{
	return FopCallback((int)op, ptr, a, b, fp);
}
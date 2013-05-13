#define WIN32_LEAN_AND_MEAN
#include <Windows.h>
#include <gl/GL.h>
#include "../windows/glext.h"

extern "C" {
#include "../cs0.h"
#include "../m68kcore.h"
#include "../peripheral.h"
#include "../vidsoft.h"
#include "../vdp2.h"
#include "../yui.h"
#include "../movie.h"
#include "../vidogl.h"
}

CDInterface FECD =
{
	2,
	"FECD",
	NULL,
	NULL,
	NULL,
	NULL,
	NULL,
	NULL,
};

SoundInterface_struct FESND;


extern "C" SH2Interface_struct *SH2CoreList[] = {
&SH2Interpreter,
&SH2DebugInterpreter,
NULL
};

extern "C" PerInterface_struct *PERCoreList[] = {
&PERDummy,
NULL
};

extern "C" CDInterface *CDCoreList[] = {
&DummyCD,
&ISOCD,
&FECD,
NULL
};

extern "C" SoundInterface_struct *SNDCoreList[] = {
&SNDDummy,
&FESND,
NULL
};

extern "C" VideoInterface_struct *VIDCoreList[] = {
&VIDDummy,
&VIDOGL,
&VIDSoft,
NULL
};

extern "C" M68K_struct *M68KCoreList[] = {
&M68KDummy,
&M68KC68K,
#ifdef HAVE_Q68
&M68KQ68,
#endif
NULL
};

/* If Yabause encounters any fatal errors, it sends the error text to this function */
void YuiErrorMsg(const char *string)
{
	MessageBoxA(NULL, string, "Yabooze dun goofed", 0);
}


int red_size;
int green_size;
int blue_size;
int depth_size;

PFNGLGENFRAMEBUFFERSPROC glGenFramebuffers;
PFNGLDELETEFRAMEBUFFERSPROC glDeleteFramebuffers;
PFNGLGENRENDERBUFFERSPROC glGenRenderbuffers;
PFNGLDELETERENDERBUFFERSPROC glDeleteRenderbuffers;
PFNGLBINDFRAMEBUFFERPROC glBindFramebuffer;
PFNGLBINDRENDERBUFFERPROC glBindRenderbuffer;
PFNGLRENDERBUFFERSTORAGEPROC glRenderbufferStorage;
PFNGLFRAMEBUFFERRENDERBUFFERPROC glFramebufferRenderbuffer;
PFNGLCHECKFRAMEBUFFERSTATUSPROC glCheckFramebufferStatus;

int LoadExtensions()
{
   glBindRenderbuffer = (PFNGLBINDRENDERBUFFERPROC)wglGetProcAddress("glBindRenderbufferEXT");
   if( glBindRenderbuffer == NULL ) return 0;
   glDeleteRenderbuffers = (PFNGLDELETERENDERBUFFERSPROC)wglGetProcAddress("glDeleteRenderbuffersEXT");
   if(glDeleteRenderbuffers==NULL) return 0;
   glGenRenderbuffers = (PFNGLGENRENDERBUFFERSPROC)wglGetProcAddress("glGenRenderbuffersEXT");
   if( glGenRenderbuffers == NULL ) return 0;
   glRenderbufferStorage = (PFNGLRENDERBUFFERSTORAGEPROC)wglGetProcAddress("glRenderbufferStorageEXT");
   if( glRenderbufferStorage == NULL ) return 0;
   glBindFramebuffer = (PFNGLBINDFRAMEBUFFERPROC)wglGetProcAddress("glBindFramebufferEXT");
   if( glBindFramebuffer==NULL) return 0;
   glDeleteFramebuffers = (PFNGLDELETEFRAMEBUFFERSPROC)wglGetProcAddress("glDeleteFramebuffersEXT");
   if( glDeleteFramebuffers==NULL) return 0;
   glGenFramebuffers = (PFNGLGENFRAMEBUFFERSPROC)wglGetProcAddress("glGenFramebuffersEXT");
   if( glGenFramebuffers == NULL ) return 0;
   glCheckFramebufferStatus = (PFNGLCHECKFRAMEBUFFERSTATUSPROC)wglGetProcAddress("glCheckFramebufferStatusEXT");
   if( glCheckFramebufferStatus == NULL ) return 0;
   glFramebufferRenderbuffer = (PFNGLFRAMEBUFFERRENDERBUFFERPROC)wglGetProcAddress("glFramebufferRenderbufferEXT");
   if( glFramebufferRenderbuffer == NULL ) return 0;

   return 1;
}

/* Sets attribute for the Video display. The values passed to this function
   depends on what Video core is being used at the time. This may end up
   being moved to the Video Core. */
void YuiSetVideoAttribute(int type, int val)
{
	// only called in GL mode
	switch (type)
	{
	case RED_SIZE:
		red_size = val;
		break;
	case GREEN_SIZE:
		green_size = val;
		break;
	case BLUE_SIZE:
		blue_size = val;
		break;
	case DEPTH_SIZE:
		depth_size = val;
		break;
	case DOUBLEBUFFER:
		break;
	}
}


GLuint fbuff = 0;
GLuint color = 0;
GLuint depth = 0;

int glwidth, glheight;

u8 *glbuff = NULL;

/* Tells the yui it wants to setup the display to display the specified video
   format. It's up to the yui to setup the actual display. This may end
   up being moved to the Video Core. */
int YuiSetVideoMode(int width, int height, int bpp, int fullscreen)
{
	// only called in GL mode
	if (bpp != 32 || red_size != 8 || green_size != 8 || blue_size != 8 || depth_size != 24)
		return -1; // failure

	if (fbuff)
		glDeleteFramebuffers(1, &fbuff);
	if (color)
		glDeleteRenderbuffers(1, &color);
	if (depth)
		glDeleteRenderbuffers(1, &depth);
	glGenFramebuffers(1, &fbuff);
	glGenRenderbuffers(1, &color);
	glGenRenderbuffers(1, &depth);

	glBindFramebuffer(GL_FRAMEBUFFER, fbuff);
	
	glBindRenderbuffer(GL_RENDERBUFFER, depth);
	glRenderbufferStorage(GL_RENDERBUFFER, GL_DEPTH_COMPONENT24, width, height);
	glFramebufferRenderbuffer(GL_DRAW_FRAMEBUFFER, GL_DEPTH_ATTACHMENT, GL_RENDERBUFFER, depth);
	
	glBindRenderbuffer(GL_RENDERBUFFER, color);
	glRenderbufferStorage(GL_RENDERBUFFER, GL_RGB8, width, height);
	glFramebufferRenderbuffer(GL_DRAW_FRAMEBUFFER, GL_COLOR_ATTACHMENT0, GL_RENDERBUFFER, color);
	
	//glBindRenderbuffer(GL_RENDERBUFFER, 0);

	if (glCheckFramebufferStatus(GL_DRAW_FRAMEBUFFER) != GL_FRAMEBUFFER_COMPLETE)
		return -1;

	glwidth = width;
	glheight = height;

	if (glbuff)
		free(glbuff);
	glbuff = (u8*) malloc(glwidth * glheight * 4);

	return 0; // success
}

int usinggl = 0;
HWND glWND;
HDC glDC;
HGLRC glRC;

void KillGLContext()
{
	wglMakeCurrent(NULL, NULL); 
	wglDeleteContext(glRC);
	ReleaseDC(glWND, glDC);
	DestroyWindow(glWND);
}

int StartGLContext()
{
	PIXELFORMATDESCRIPTOR pfd;
	memset(&pfd,0, sizeof(PIXELFORMATDESCRIPTOR));
	pfd.nSize = sizeof(PIXELFORMATDESCRIPTOR);
	pfd.nVersion = 1;
	pfd.dwFlags = PFD_SUPPORT_OPENGL;
	pfd.iPixelType = PFD_TYPE_RGBA;
	pfd.cColorBits = 24;
	pfd.cAlphaBits = 8;
	pfd.cDepthBits = 24;
	pfd.cStencilBits = 8;
	pfd.iLayerType = PFD_MAIN_PLANE;

	glWND = CreateWindow(L"EDIT", 0, 0, 0, 0, 512, 512, 0, 0, 0, 0);
	if (!glWND)
		return 0;
	glDC = GetDC(glWND);
	if (!glDC)
	{
		DestroyWindow(glWND);
		return 0;
	}
	int PixelFormat = ChoosePixelFormat(glDC, &pfd);
	if (!PixelFormat)
	{
		ReleaseDC(glWND, glDC);
		DestroyWindow(glWND);
		return 0;
	}
	if (!SetPixelFormat(glDC, PixelFormat, &pfd))
	{
		ReleaseDC(glWND, glDC);
		DestroyWindow(glWND);
		return 0;
	}
	glRC = wglCreateContext(glDC);
	if (!glRC)
	{
		ReleaseDC(glWND, glDC);
		DestroyWindow(glWND);
		return 0;
	}
	if (!wglMakeCurrent(glDC, glRC))
	{
		wglDeleteContext(glRC);
		ReleaseDC(glWND, glDC);
		DestroyWindow(glWND);
		return 0;
	}
	return 1;
}



s16 *sndbuff = NULL;
int sndbuffpos = 0;
u32 *vidbuff = NULL;

PerPad_struct *ctrl1;
PerPad_struct *ctrl2;

extern "C" int vdp2height;
extern "C" int vdp2width;

/* Tells the yui to exchange front and back video buffers. This may end
   up being moved to the Video Core. */
void YuiSwapBuffers(void)
{	
	if (vidbuff)
	{
		if (!usinggl)
		{
			u8 *src = (u8*)dispbuffer;
			u8 *dst = (u8*)vidbuff;

			for (int j = 0; j < vdp2height; j++)
				for (int i = 0; i < vdp2width; i++)
				{
					dst[0] = src[2];
					dst[1] = src[1];
					dst[2] = src[0];
					dst[3] = 0xff;
					src += 4;
					dst += 4;
				}	
		}
		else
		{
			glReadPixels(0, 0, glwidth, glheight, GL_BGRA, GL_UNSIGNED_BYTE, glbuff);

			u32 *src = (u32*)glbuff;
			u32 *dst = (u32*)vidbuff;

			dst += glwidth * (glheight - 1);

			for (int j = 0; j < glheight; j++)
			{
				memcpy(dst, src, glwidth * 4);
				src += glwidth;
				dst -= glwidth;
			}
		}
	}
}

static void FESNDUpdateAudio(UNUSED u32 *leftchanbuffer, UNUSED u32 *rightchanbuffer, UNUSED u32 num_samples)
{
	/*
	static s16 stereodata16[44100 / 50];
	ScspConvert32uto16s((s32 *)leftchanbuffer, (s32 *)rightchanbuffer, (s16 *)stereodata16, num_samples);
	*/
}

static u32 FESNDGetAudioSpace(void)
{
	return 44100;
}
// some garbage from the sound system, we'll have to fix this all up
extern "C" void DRV_AviSoundUpdate(void* soundData, int soundLen)
{
	// soundLen should be number of sample pairs (4 byte units)
	if (sndbuff)
	{
		s16 *src = (s16*)soundData;
		s16 *dst = sndbuff;
		dst += sndbuffpos * 2;
		memcpy (dst, src, soundLen * 4);
		sndbuffpos += soundLen;
	}
}

// must hold at least 704x512 pixels
extern "C" __declspec(dllexport) void libyabause_setvidbuff(u32 *buff)
{
	vidbuff = buff;
}

extern "C" __declspec(dllexport) void libyabause_setsndbuff(s16 *buff)
{
	sndbuff = buff;
}

extern "C" __declspec(dllexport) void libyabause_softreset()
{
	YabauseResetButton();
}

extern "C" __declspec(dllexport) void libyabause_hardreset()
{
	YabauseReset();
}

extern "C" __declspec(dllexport) int libyabause_loadstate(const char *fn)
{
	return !YabLoadState(fn);
}

extern "C" __declspec(dllexport) int libyabause_savestate(const char *fn)
{
	return !YabSaveState(fn);
}

extern "C" __declspec(dllexport) int libyabause_savesaveram(const char *fn)
{
	return !T123Save(BupRam, 0x10000, 1, fn);
}

extern "C" __declspec(dllexport) int libyabause_loadsaveram(const char *fn)
{
	return !T123Load(BupRam, 0x10000, 1, fn);
}

extern "C" __declspec(dllexport) int libyabause_saveramodified()
{
	return BupRamWritten;
}

extern "C" __declspec(dllexport) void libyabause_clearsaveram()
{
	FormatBackupRam(BupRam, 0x10000);
}

typedef struct
{
	void *data;
	const char *name;
	int length;
} memoryarea;

memoryarea normmemareas[] =
{
	{NULL, "Boot Rom", 512 * 1024},
	{NULL, "Backup Ram", 64 * 1024},
	{NULL, "Work Ram Low", 1024 * 1024},
	{NULL, "Sound Ram", 512 * 1024},
	{NULL, "VDP1 Ram", 512 * 1024},
	{NULL, "VDP1 Framebuffer", 512 * 1024},
	{NULL, "VDP2 Ram", 512 * 1024},
	{NULL, "VDP2 CRam", 4 * 1024},
	{NULL, "Work Ram High", 1024 * 1024},
	{NULL, NULL, 0}
};

extern "C" __declspec(dllexport) memoryarea *libyabause_getmemoryareas()
{
	normmemareas[0].data = BiosRom;
	normmemareas[1].data = BupRam;
	normmemareas[2].data = LowWram;
	normmemareas[3].data = SoundRam;
	normmemareas[4].data = Vdp1Ram;
	normmemareas[5].data = Vdp1FrameBuffer;
	normmemareas[6].data = Vdp2Ram;
	normmemareas[7].data = Vdp2ColorRam;
	normmemareas[8].data = HighWram;
	return &normmemareas[0];
}

extern "C" __declspec(dllexport) int libyabause_frameadvance(int *w, int *h, int *nsamp)
{
	LagFrameFlag = 1;
	sndbuffpos = 0;
	YabauseEmulate();
	if (usinggl)
	{
		*w = glwidth;
		*h = glheight;
	}
	else
	{
		*w = vdp2width;
		*h = vdp2height;
	}
	*nsamp = sndbuffpos;
	return LagFrameFlag;
}

extern "C" __declspec(dllexport) void libyabause_deinit()
{
	PerPortReset();
	YabauseDeInit();
	if (usinggl)
	{
		KillGLContext();
		usinggl = 0;
	}
	if (glbuff)
	{
		free(glbuff);
		glbuff = NULL;
	}
}

extern "C" __declspec(dllexport) void libyabause_setpads(u8 p11, u8 p12, u8 p21, u8 p22)
{
	ctrl1->padbits[0] = p11;
	ctrl1->padbits[1] = p12;
	ctrl2->padbits[0] = p21;
	ctrl2->padbits[1] = p22;
}

extern "C" __declspec(dllexport) void libyabause_glresize(int w, int h)
{
	if (usinggl)
		VIDCore->Resize(w, h, 0);
}

extern "C" __declspec(dllexport) int libyabause_init(CDInterface *_CD, const char *biosfn, int usegl)
{
	usinggl = usegl;
	if (usegl && (!StartGLContext() || !LoadExtensions()))
		return 0;

	FECD.DeInit = _CD->DeInit;
	FECD.GetStatus = _CD->GetStatus;
	FECD.Init = _CD->Init;
	FECD.ReadAheadFAD = _CD->ReadAheadFAD;
	FECD.ReadSectorFAD = _CD->ReadSectorFAD;
	FECD.ReadTOC = _CD->ReadTOC;

	// only overwrite a few SNDDummy functions
	memcpy(&FESND, &SNDDummy, sizeof(FESND));
	FESND.id = 2;
	FESND.Name = "FESND";
	FESND.GetAudioSpace = FESNDGetAudioSpace;
	FESND.UpdateAudio = FESNDUpdateAudio;

	yabauseinit_struct yinit;
	memset(&yinit, 0, sizeof(yabauseinit_struct));
	yinit.percoretype = PERCORE_DUMMY;
	yinit.sh2coretype = SH2CORE_INTERPRETER;
	if (usegl)
		yinit.vidcoretype = VIDCORE_OGL;
	else
		yinit.vidcoretype = VIDCORE_SOFT;	
	yinit.sndcoretype = 2; //SNDCORE_DUMMY;
	yinit.cdcoretype = 2; // CDCORE_ISO; //CDCORE_DUMMY;
	yinit.m68kcoretype = M68KCORE_C68K;
	yinit.cartpath = CART_NONE;
	yinit.regionid = REGION_AUTODETECT;
	yinit.biospath = biosfn;
	yinit.cdpath = "Saturnus"; //NULL;
	yinit.buppath = NULL;
	yinit.mpegpath = NULL;
	yinit.cartpath = NULL;
	yinit.netlinksetting = NULL;
	yinit.videoformattype = VIDEOFORMATTYPE_NTSC;

	if (YabauseInit(&yinit) != 0)
		return 0;
	
	SpeedThrottleDisable();
	DisableAutoFrameSkip();
	ScspSetFrameAccurate(1);

	OSDChangeCore(OSDCORE_DUMMY);

	ctrl1 = PerPadAdd(&PORTDATA1);
	ctrl2 = PerPadAdd(&PORTDATA2);

	return 1;
}

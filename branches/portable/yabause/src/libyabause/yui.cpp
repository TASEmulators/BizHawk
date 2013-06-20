#define WIN32_LEAN_AND_MEAN
#include <Windows.h>

extern "C" {
#include "../cs0.h"
#include "../m68kcore.h"
#include "../peripheral.h"
#include "../vidsoft.h"
#include "../vdp2.h"
#include "../yui.h"
#include "../movie.h"
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

/* Sets attribute for the Video display. The values passed to this function
   depends on what Video core is being used at the time. This may end up
   being moved to the Video Core. */
void YuiSetVideoAttribute(int type, int val)
{
	// only called in GL mode?
	switch (type)
	{
	case RED_SIZE:
		break;
	case GREEN_SIZE:
		break;
	case BLUE_SIZE:
		break;
	case DEPTH_SIZE:
		break;
	case DOUBLEBUFFER:
		break;
	}
}

/* Tells the yui it wants to setup the display to display the specified video
   format. It's up to the yui to setup the actual display. This may end
   up being moved to the Video Core. */
int YuiSetVideoMode(int width, int height, int bpp, int fullscreen)
{
	// only called in GL mode?
	// return -1; // failure
	return 0; // success
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

extern "C" __declspec(dllexport) int libyabause_frameadvance(int *w, int *h, int *nsamp)
{
	LagFrameFlag = 1;
	sndbuffpos = 0;
	YabauseEmulate();
	*w = vdp2width;
	*h = vdp2height;
	*nsamp = sndbuffpos;
	return LagFrameFlag;
}

extern "C" __declspec(dllexport) void libyabause_deinit()
{
	YabauseDeInit();
}

extern "C" __declspec(dllexport) void libyabause_setpads(u8 p11, u8 p12, u8 p21, u8 p22)
{
	ctrl1->padbits[0] = p11;
	ctrl1->padbits[1] = p12;
	ctrl2->padbits[0] = p21;
	ctrl2->padbits[1] = p22;
}

extern "C" __declspec(dllexport) int libyabause_init(CDInterface *_CD)
{
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
	yinit.vidcoretype = VIDCORE_SOFT;
	yinit.sndcoretype = 2; //SNDCORE_DUMMY;
	yinit.cdcoretype = 2; // CDCORE_ISO; //CDCORE_DUMMY;
	yinit.m68kcoretype = M68KCORE_C68K;
	yinit.cartpath = CART_NONE;
	yinit.regionid = REGION_AUTODETECT;
	yinit.biospath = NULL;
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

	ctrl1 = PerPadAdd(&PORTDATA1);
	ctrl2 = PerPadAdd(&PORTDATA2);

	return 1;
}

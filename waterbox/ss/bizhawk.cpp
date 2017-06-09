#include "ss.h"
#include "stream/MemoryStream.h"
#include <memory>
#include "cdrom/cdromif.h"
#include "cdb.h"
#include "smpc.h"
#include "cart.h"
#include <ctime>

#define EXPORT extern "C" ECL_EXPORT
using namespace MDFN_IEN_SS;

int32 (*FirmwareSizeCallback)(const char *filename);
void (*FirmwareDataCallback)(const char *filename, uint8 *dest);

EXPORT void SetFirmwareCallbacks(int32 (*sizecallback)(const char *filename), void (*datacallback)(const char *filename, uint8 *dest))
{
	FirmwareSizeCallback = sizecallback;
	FirmwareDataCallback = datacallback;
}

struct FrontendTOC
{
	int32 FirstTrack;
	int32 LastTrack;
	int32 DiskType;
	struct
	{
		int32 Adr;
		int32 Control;
		int32 Lba;
		int32 Valid;
	} Tracks[101];
};

static void (*ReadTOCCallback)(int disk, FrontendTOC *dest);
static void (*ReadSector2448Callback)(int disk, int lba, uint8 *dest);

EXPORT void SetCDCallbacks(void (*toccallback)(int disk, FrontendTOC *dest), void (*sectorcallback)(int disk, int lba, uint8 *dest))
{
	ReadTOCCallback = toccallback;
	ReadSector2448Callback = sectorcallback;
}

class MyCDIF : public CDIF
{
  private:
	int disk;

  public:
	MyCDIF(int disk) : disk(disk)
	{
		FrontendTOC t;
		ReadTOCCallback(disk, &t);
		disc_toc.first_track = t.FirstTrack;
		disc_toc.last_track = t.LastTrack;
		disc_toc.disc_type = t.DiskType;
		for (int i = 0; i < 101; i++)
		{
			disc_toc.tracks[i].adr = t.Tracks[i].Adr;
			disc_toc.tracks[i].control = t.Tracks[i].Control;
			disc_toc.tracks[i].lba = t.Tracks[i].Lba;
			disc_toc.tracks[i].valid = t.Tracks[i].Valid;
		}
	}

	virtual void HintReadSector(int32 lba) {}
	virtual bool ReadRawSector(uint8 *buf, int32 lba)
	{
		ReadSector2448Callback(disk, lba, buf);
		return true;
	}
	virtual bool ReadRawSectorPWOnly(uint8 *pwbuf, int32 lba, bool hint_fullread)
	{
		uint8 buff[2448];
		ReadSector2448Callback(disk, lba, buff);
		memcpy(pwbuf, buff + 2352, 96);
		return true;
	}
};

static std::vector<CDIF *> CDInterfaces;
static uint32 *FrameBuffer;
static uint8 IsResetPushed; // 1 or 0

namespace MDFN_IEN_SS
{
extern bool LoadCD(std::vector<CDIF *> *CDInterfaces);
}
EXPORT bool Init(int numDisks, int cartType, int regionDefault, int regionAutodetect)
{
	setting_ss_cart = cartType;
	setting_ss_region_autodetect = regionAutodetect;
	setting_ss_region_default = regionDefault;

	FrameBuffer = (uint32 *)alloc_invisible(1024 * 1024 * sizeof(*FrameBuffer));
	for (int i = 0; i < numDisks; i++)
		CDInterfaces.push_back(new MyCDIF(i));
	auto ret = LoadCD(&CDInterfaces);
	if (ret)
		SMPC_SetInput(12, nullptr, &IsResetPushed);
	return ret;
}

EXPORT void HardReset()
{
	// soft reset is handled as a normal button
	SS_Reset(true);
}

EXPORT void SetDisk(int disk, bool open)
{
	CDB_SetDisc(open, disk < 0 ? nullptr : CDInterfaces[disk]);
}

int setting_ss_slstartp = 0;
int setting_ss_slendp = 255;
int setting_ss_slstart = 0;
int setting_ss_slend = 239;
int setting_ss_region_default = SMPC_AREA_JP;
int setting_ss_cart = CART_NONE;
bool setting_ss_correct_aspect = true;
bool setting_ss_h_blend = false;
bool setting_ss_h_overscan = true;
bool setting_ss_region_autodetect = true;
bool setting_ss_input_sport1_multitap = false;
bool setting_ss_input_sport0_multitap = false;

namespace MDFN_IEN_SS
{
extern void Emulate(EmulateSpecStruct *espec_arg);
}

struct FrameAdvanceInfo
{
	int16 *SoundBuf;

	uint32 *Pixels;

	uint8 *Controllers;

	int64 MasterCycles;

	int32 SoundBufMaxSize;
	int32 SoundBufSize;

	int32 Width;
	int32 Height;

	int16 ResetPushed;
	int16 InputLagged;

	// Set by the system emulation code every frame, to denote the horizontal and vertical offsets of the image, and the size
	// of the image.  If the emulated system sets the elements of LineWidths, then the width(w) of this structure
	// is ignored while drawing the image.
	// int32 x, y, h;

	// Set(optionally) by emulation code.  If InterlaceOn is true, then assume field height is 1/2 DisplayRect.h, and
	// only every other line in surface (with the start line defined by InterlacedField) has valid data
	// (it's up to internal Mednafen code to deinterlace it).
	// bool InterlaceOn;
	// bool InterlaceField;
};

static uint8 ControllerInput[12 * 32];
bool InputLagged;

EXPORT void FrameAdvance(FrameAdvanceInfo &f)
{
	EmulateSpecStruct e;
	int32 LineWidths[1024];
	memset(LineWidths, 0, sizeof(LineWidths));
	e.pixels = FrameBuffer;
	e.pitch32 = 1024;
	e.LineWidths = LineWidths;
	e.SoundBuf = f.SoundBuf;
	e.SoundBufMaxSize = f.SoundBufMaxSize;
	memcpy(ControllerInput, f.Controllers, sizeof(ControllerInput));
	IsResetPushed = f.ResetPushed;
	InputLagged = true;
	Emulate(&e);
	f.SoundBufSize = e.SoundBufSize;
	f.MasterCycles = e.MasterCycles;
	f.InputLagged = InputLagged;

	int w = 256;
	for (int i = 0; i < e.h; i++)
		w = std::max(w, LineWidths[i]);

	const uint32 *src = FrameBuffer;
	uint32 *dst = f.Pixels;
	const int srcp = 1024;
	const int dstp = w;
	src += e.y * srcp + e.x;

	for (int j = 0; j < e.h; j++, src += srcp, dst += dstp)
	{
		memcpy(dst, src, LineWidths[j + e.y] * sizeof(*dst));
	}
	f.Width = w;
	f.Height = e.h;
}

static const char *DeviceNames[] =
	{
		"none",
		"gamepad",
		"3dpad",
		"mouse",
		"wheel",
		"mission",
		"dmission",
		"keyboard"};

EXPORT void SetupInput(const int *portdevices, const int *multitaps)
{
	for (int i = 0; i < 2; i++)
		SMPC_SetMultitap(i, multitaps[i]);
	for (int i = 0; i < 12; i++)
		SMPC_SetInput(i, DeviceNames[portdevices[i]], ControllerInput + i * 32);
}

void (*InputCallback)();
EXPORT void SetInputCallback(void (*callback)())
{
	InputCallback = callback;
}

void (*AddMemoryDomain)(const char *name, const void *ptr, int size, bool writable);

EXPORT void SetAddMemoryDomainCallback(void (*callback)(const char *name, const void *ptr, int size, bool writable))
{
	AddMemoryDomain = callback;
}

EXPORT void SetRtc(int64 ticks, int language)
{
	time_t time = ticks;
	const struct tm *tm = gmtime(&time);
	SMPC_SetRTC(tm, language);
}

namespace MDFN_IEN_SS
{
extern bool CorrectAspect;
extern bool ShowHOverscan;
extern bool DoHBlend;
extern int LineVisFirst;
extern int LineVisLast;
}
EXPORT void SetVideoParameters(bool correctAspect, bool hBlend, bool hOverscan, int sls, int sle)
{
	CorrectAspect = correctAspect;
	ShowHOverscan = hOverscan;
	DoHBlend = hBlend;
	LineVisFirst = sls;
	LineVisLast = sle;
}

// if (BackupRAM_Dirty)SaveBackupRAM();
// if (CART_GetClearNVDirty())SaveCartNV();

/*static MDFN_COLD void CloseGame(void)
{
 try { SaveBackupRAM(); } catch(std::exception& e) { MDFN_PrintError("%s", e.what()); }
 try { SaveCartNV();    } catch(std::exception& e) { MDFN_PrintError("%s", e.what()); }
 try { SaveRTC();	} catch(std::exception& e) { MDFN_PrintError("%s", e.what()); }

 Cleanup();
}*/

/*static MDFN_COLD void SaveBackupRAM(void)
{
 FileStream brs(MDFN_MakeFName(MDFNMKF_SAV, 0, "bkr"), FileStream::MODE_WRITE_INPLACE);

 brs.write(BackupRAM, sizeof(BackupRAM));

 brs.close();
}

static MDFN_COLD void LoadBackupRAM(void)
{
 FileStream brs(MDFN_MakeFName(MDFNMKF_SAV, 0, "bkr"), FileStream::MODE_READ);

 brs.read(BackupRAM, sizeof(BackupRAM));
}

static MDFN_COLD void BackupBackupRAM(void)
{
 MDFN_BackupSavFile(10, "bkr");
}

static MDFN_COLD void BackupCartNV(void)
{
 const char* ext = nullptr;
 void* nv_ptr = nullptr;
 uint64 nv_size = 0;

 CART_GetNVInfo(&ext, &nv_ptr, &nv_size);

 if(ext)
  MDFN_BackupSavFile(10, ext);
}*/

/*static MDFN_COLD void LoadCartNV(void)
{
 const char* ext = nullptr;
 void* nv_ptr = nullptr;
 uint64 nv_size = 0;

 CART_GetNVInfo(&ext, &nv_ptr, &nv_size);

 if(ext)
 {
  //FileStream nvs(MDFN_MakeFName(MDFNMKF_SAV, 0, ext), FileStream::MODE_READ);
  GZFileStream nvs(MDFN_MakeFName(MDFNMKF_SAV, 0, ext), GZFileStream::MODE::READ);

  nvs.read(nv_ptr, nv_size);
 }
}

static MDFN_COLD void SaveCartNV(void)
{
 const char* ext = nullptr;
 void* nv_ptr = nullptr;
 uint64 nv_size = 0;

 CART_GetNVInfo(&ext, &nv_ptr, &nv_size);

 if(ext)
 {
  //FileStream nvs(MDFN_MakeFName(MDFNMKF_SAV, 0, ext), FileStream::MODE_WRITE_INPLACE);
  GZFileStream nvs(MDFN_MakeFName(MDFNMKF_SAV, 0, ext), GZFileStream::MODE::WRITE);

  nvs.write(nv_ptr, nv_size);

  nvs.close();
 }
}*/

/*static MDFN_COLD void SaveRTC(void)
{
 FileStream sds(MDFN_MakeFName(MDFNMKF_SAV, 0, "smpc"), FileStream::MODE_WRITE_INPLACE);

 SMPC_SaveNV(&sds);

 sds.close();
}

static MDFN_COLD void LoadRTC(void)
{
 FileStream sds(MDFN_MakeFName(MDFNMKF_SAV, 0, "smpc"), FileStream::MODE_READ);

 SMPC_LoadNV(&sds);
}*/

int main()
{
	return 0;
}

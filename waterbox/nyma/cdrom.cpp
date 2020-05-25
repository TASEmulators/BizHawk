#include "mednafen/src/types.h"
#include <emulibc.h>
#include <waterboxcore.h>
#include <mednafen/mednafen.h>
#include <stdint.h>
#include <mednafen/cdrom/CDInterface.h>
#include <mednafen/cdrom/CDInterface_MT.h>
#include <mednafen/cdrom/CDInterface_ST.h>
#include <mednafen/cdrom/CDAccess.h>
#include <trio/trio.h>

#include "cdrom.h"

using namespace Mednafen;

struct NymaTOC
{
	int32_t FirstTrack;
	int32_t LastTrack;
	int32_t DiskType;
	struct
	{
		int32_t Adr;
		int32_t Control;
		int32_t Lba;
		int32_t Valid;
	} Tracks[101];
};

static void (*ReadTOCCallback)(int disk, NymaTOC *dest);
static void (*ReadSector2448Callback)(int disk, int lba, uint8 *dest);

ECL_EXPORT void SetCDCallbacks(void (*toccallback)(int disk, NymaTOC *dest), void (*sectorcallback)(int disk, int lba, uint8 *dest))
{
	ReadTOCCallback = toccallback;
	ReadSector2448Callback = sectorcallback;
}

CDInterfaceNyma::CDInterfaceNyma(int d) : disk(d)
{
	NymaTOC t;
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

void CDInterfaceNyma::HintReadSector(int32 lba) {}
bool CDInterfaceNyma::ReadRawSector(uint8 *buf, int32 lba)
{
	ReadSector2448Callback(disk, lba, buf);
	return true;
}
bool CDInterfaceNyma::ReadRawSectorPWOnly(uint8 *pwbuf, int32 lba, bool hint_fullread)
{
	uint8 buff[2448];
	ReadSector2448Callback(disk, lba, buff);
	memcpy(pwbuf, buff + 2352, 96);
	return true;
}

std::vector<CDInterface*>* CDInterfaces;

void StartGameWithCds(int numdisks)
{
	CDInterfaces = new std::vector<CDInterface*>();
	for (int d = 0; d < numdisks; d++)
	{
		CDInterfaces->push_back(new CDInterfaceNyma(d));
	}
	MDFNGameInfo->LoadCD(CDInterfaces);

	// TODO: Figure out wtf all this RMD stuff is
	auto rmd = new RMD_Layout();
	{
		RMD_Drive dr;

		dr.Name = "Virtual CD Drive";
		dr.PossibleStates.push_back(RMD_State({"Tray Open", false, false, true}));
		dr.PossibleStates.push_back(RMD_State({"Tray Closed (Empty)", false, false, false}));
		dr.PossibleStates.push_back(RMD_State({"Tray Closed", true, true, false}));
		dr.CompatibleMedia.push_back(0);
		dr.MediaMtoPDelay = 2000;

		rmd->Drives.push_back(dr);
		rmd->DrivesDefaults.push_back(RMD_DriveDefaults({0, 0, 0}));
		rmd->MediaTypes.push_back(RMD_MediaType({"CD"}));
	}

	const int default_cd = 0;

	for(size_t i = 0; i < CDInterfaces->size(); i++)
	{
		if (i == default_cd)
		{
			rmd->DrivesDefaults[0].State = 2; // Tray Closed
			rmd->DrivesDefaults[0].Media = i;
			rmd->DrivesDefaults[0].Orientation = 0;
		}
		char namebuf[128];
		trio_snprintf(namebuf, sizeof(namebuf), _("Disc %zu of %zu"), i + 1, CDInterfaces->size());
		rmd->Media.push_back(RMD_Media({namebuf, 0}));
	}
	MDFNGameInfo->RMD = rmd;

	// TODO:  Wire up a way for the user to change disks
	Mednafen::MDFNGameInfo->SetMedia(
		0, // drive: 0 unless there's more than one drive
		2, // state: 0 = open, 1 = closed (media absent), 2 = closed (media present)
		0, // media: index into the disk list
		0 // orientation: flip sides on NES FDS.  not used elsewhere?
	);
}

// CDInterface::Load pulls in a bunch of things that we do not want, stub them out here
namespace Mednafen
{
using namespace CDUtility;
CDInterface_MT::CDInterface_Message::~CDInterface_Message(){}
CDInterface_MT::CDInterface_Queue::CDInterface_Queue(){}
CDInterface_MT::CDInterface_Queue::~CDInterface_Queue(){}
CDInterface_MT::CDInterface_MT(std::unique_ptr<CDAccess> cda, const uint64 affinity){}
CDInterface_MT::~CDInterface_MT(){}
bool CDInterface_MT::ReadRawSector(uint8 *buf, int32 lba){return false;}
bool CDInterface_MT::ReadRawSectorPWOnly(uint8* pwbuf, int32 lba, bool hint_fullread){return false;}
void CDInterface_MT::HintReadSector(int32 lba){}
CDInterface_ST::CDInterface_ST(std::unique_ptr<CDAccess> cda) : disc_cdaccess(std::move(cda)){}
CDInterface_ST::~CDInterface_ST(){}
void CDInterface_ST::HintReadSector(int32 lba){}
bool CDInterface_ST::ReadRawSector(uint8 *buf, int32 lba){return false;}
bool CDInterface_ST::ReadRawSectorPWOnly(uint8* pwbuf, int32 lba, bool hint_fullread){return false;}
CDAccess* CDAccess_Open(VirtualFS* vfs, const std::string& path, bool image_memcache){return nullptr;}
}

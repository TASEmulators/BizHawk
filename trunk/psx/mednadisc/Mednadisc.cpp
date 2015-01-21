#include "emuware/emuware.h"

#include "Mednadisc.h"

#include "error.h"

#include "cdrom/CDAccess.h"
#include "cdrom/CDUtility.h"
#include "cdrom/cdromif.h"
#include "cdrom/CDAccess_Image.h"

EW_EXPORT void* mednadisc_LoadCD(const char* fname)
{
	CDAccess* disc = NULL;
	try {
		disc = cdaccess_open_image(fname,false);
	}
	catch(MDFN_Error &e) {
		return NULL;
	}
	return disc;
}

struct JustTOC
{
  uint8 first_track;
  uint8 last_track;
  uint8 disc_type;
};

EW_EXPORT void mednadisc_ReadTOC(CDAccess* disc, JustTOC* justToc, CDUtility::TOC_Track *tracks101)
{
	CDUtility::TOC toc;
	disc->Read_TOC(&toc);
	justToc->first_track = toc.first_track;
	justToc->last_track = toc.last_track;
	justToc->disc_type = toc.disc_type;
	memcpy(tracks101,toc.tracks,sizeof(toc.tracks));
}

EW_EXPORT int32 mednadisc_ReadSector(CDAccess* disc, int lba, void* buf2448)
{
	try {
		disc->Read_Raw_Sector((uint8*)buf2448,lba);
	}	
	catch(MDFN_Error &e) {
		return 0;
	}
	return 1;
}

EW_EXPORT void mednadisc_CloseCD(CDAccess* disc)
{
	delete disc;
} 

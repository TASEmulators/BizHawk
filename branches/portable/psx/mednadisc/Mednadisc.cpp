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

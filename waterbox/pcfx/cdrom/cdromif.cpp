/* Mednafen - Multi-system Emulator
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
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
 */

#include "../defs.h"
#include <string.h>
#include <sys/types.h>
#include "cdromif.h"
//#include "CDAccess.h"
//#include "../general.h"

#include <algorithm>

using namespace CDUtility;

enum
{
 // Status/Error messages
 CDIF_MSG_DONE = 0,		// Read -> emu. args: No args.
 CDIF_MSG_INFO,			// Read -> emu. args: str_message
 CDIF_MSG_FATAL_ERROR,		// Read -> emu. args: *TODO ARGS*

 //
 // Command messages.
 //
 CDIF_MSG_DIEDIEDIE,		// Emu -> read

 CDIF_MSG_READ_SECTOR,		/* Emu -> read
					args[0] = lba
				*/
};


typedef struct
{
 bool valid;
 bool error;
 int32 lba;
 uint8 data[2352 + 96];
} CDIF_Sector_Buffer;


CDIF::CDIF() : UnrecoverableError(false)
{

}

CDIF::~CDIF()
{

}

bool CDIF::ValidateRawSector(uint8 *buf)
{
 int mode = buf[12 + 3];

 if(mode != 0x1 && mode != 0x2)
  return(false);

 if(!edc_lec_check_and_correct(buf, mode == 2))
  return(false);

 return(true);
}

int CDIF::ReadSector(uint8* buf, int32 lba, uint32 sector_count, bool suppress_uncorrectable_message)
{
 int ret = 0;

 if(UnrecoverableError)
  return(false);

 while(sector_count--)
 {
  uint8 tmpbuf[2352 + 96];

  if(!ReadRawSector(tmpbuf, lba))
  {
   puts("CDIF Raw Read error");
   return(FALSE);
  }

  if(!ValidateRawSector(tmpbuf))
  {
   /*if(!suppress_uncorrectable_message)
   {
    MDFN_DispMessage(_("Uncorrectable data at sector %d"), lba);
    MDFN_PrintError(_("Uncorrectable data at sector %d"), lba);
   }*/

   return(false);
  }

  const int mode = tmpbuf[12 + 3];

  if(!ret)
   ret = mode;

  if(mode == 1)
  {
   memcpy(buf, &tmpbuf[12 + 4], 2048);
  }
  else if(mode == 2)
  {
   memcpy(buf, &tmpbuf[12 + 4 + 8], 2048);
  }
  else
  {
   printf("CDIF_ReadSector() invalid sector type at LBA=%u\n", (unsigned int)lba);
   return(false);
  }

  buf += 2048;
  lba++;
 }

 return(ret);
}

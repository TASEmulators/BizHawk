/*  Copyright 2004-2008 Theo Berkau
    Copyright 2005 Joost Peters
    Copyright 2005-2006 Guillaume Duhamel
    
    This file is part of Yabause.

    Yabause is free software; you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation; either version 2 of the License, or
    (at your option) any later version.

    Yabause is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with Yabause; if not, write to the Free Software
    Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301  USA
*/

#include <string.h>
#include <stdlib.h>
#include <assert.h>
#include "cdbase.h"
#include "error.h"
#include "debug.h"

//////////////////////////////////////////////////////////////////////////////

// Contains the Dummy and ISO CD Interfaces

static int DummyCDInit(const char *);
static void DummyCDDeInit(void);
static int DummyCDGetStatus(void);
static s32 DummyCDReadTOC(u32 *);
static int DummyCDReadSectorFAD(u32, void *);
static void DummyCDReadAheadFAD(u32);

CDInterface DummyCD = {
CDCORE_DUMMY,
"Dummy CD Drive",
DummyCDInit,
DummyCDDeInit,
DummyCDGetStatus,
DummyCDReadTOC,
DummyCDReadSectorFAD,
DummyCDReadAheadFAD,
};

static int ISOCDInit(const char *);
static void ISOCDDeInit(void);
static int ISOCDGetStatus(void);
static s32 ISOCDReadTOC(u32 *);
static int ISOCDReadSectorFAD(u32, void *);
static void ISOCDReadAheadFAD(u32);

CDInterface ISOCD = {
CDCORE_ISO,
"ISO-File Virtual Drive",
ISOCDInit,
ISOCDDeInit,
ISOCDGetStatus,
ISOCDReadTOC,
ISOCDReadSectorFAD,
ISOCDReadAheadFAD,
};

//////////////////////////////////////////////////////////////////////////////
// Dummy Interface
//////////////////////////////////////////////////////////////////////////////

static int DummyCDInit(UNUSED const char *cdrom_name)
{
	// Initialization function. cdrom_name can be whatever you want it to
	// be. Obviously with some ports(e.g. the dreamcast port) you probably
	// won't even use it.
	return 0;
}

//////////////////////////////////////////////////////////////////////////////

static void DummyCDDeInit(void)
{
	// Cleanup function. Enough said.
}

//////////////////////////////////////////////////////////////////////////////

static int DummyCDGetStatus(void)
{
	// This function is called periodically to see what the status of the
	// drive is.
	//
	// Should return one of the following values:
	// 0 - CD Present, disc spinning
	// 1 - CD Present, disc not spinning
	// 2 - CD not present
	// 3 - Tray open
	//
	// If you really don't want to bother too much with this function, just
	// return status 0. Though it is kind of nice when the bios's cd
	// player, etc. recognizes when you've ejected the tray and popped in
	// another disc.

	return 0;
}

//////////////////////////////////////////////////////////////////////////////

static s32 DummyCDReadTOC(UNUSED u32 *TOC)
{
	// The format of TOC is as follows:
	// TOC[0] - TOC[98] are meant for tracks 1-99. Each entry has the
	// following format:
	// bits 0 - 23: track FAD address
	// bits 24 - 27: track addr
	// bits 28 - 31: track ctrl
	//
	// Any Unused tracks should be set to 0xFFFFFFFF
	//
	// TOC[99] - Point A0 information 
	// Uses the following format:
	// bits 0 - 7: PFRAME(should always be 0)
	// bits 7 - 15: PSEC(Program area format: 0x00 - CDDA or CDROM,
	//                   0x10 - CDI, 0x20 - CDROM-XA)
	// bits 16 - 23: PMIN(first track's number)
	// bits 24 - 27: first track's addr
	// bits 28 - 31: first track's ctrl
	//
	// TOC[100] - Point A1 information
	// Uses the following format:
	// bits 0 - 7: PFRAME(should always be 0)
	// bits 7 - 15: PSEC(should always be 0)
	// bits 16 - 23: PMIN(last track's number)
	// bits 24 - 27: last track's addr
	// bits 28 - 31: last track's ctrl
	//
	// TOC[101] - Point A2 information
	// Uses the following format:
	// bits 0 - 23: leadout FAD address
	// bits 24 - 27: leadout's addr
	// bits 28 - 31: leadout's ctrl
	//
	// Special Note: To convert from LBA/LSN to FAD, add 150.

	return 0;
}

//////////////////////////////////////////////////////////////////////////////

static int DummyCDReadSectorFAD(UNUSED u32 FAD, void * buffer)
{
	// This function is supposed to read exactly 1 -RAW- 2352-byte sector
	// at the specified FAD address to buffer. Should return true if
	// successful, false if there was an error.
	//
	// Special Note: To convert from FAD to LBA/LSN, minus 150.
	//
	// The whole process needed to be changed since I need more control
	// over sector detection, etc. Not to mention it means less work for
	// the porter since they only have to implement raw sector reading as
	// opposed to implementing mode 1, mode 2 form1/form2, -and- raw
	// sector reading.

	memset(buffer, 0, 2352);

	return 1;
}

//////////////////////////////////////////////////////////////////////////////

static void DummyCDReadAheadFAD(UNUSED u32 FAD)
{
	// This function is called to tell the driver which sector (FAD
	// address) is expected to be read next. If the driver supports
	// read-ahead, it should start reading the given sector in the
	// background while the emulation continues, so that when the
	// sector is actually read with ReadSectorFAD() it'll be available
	// immediately. (Note that there's no guarantee this sector will
	// actually be requested--the emulated CD might be stopped before
	// the sector is read, for example.)
	//
	// This function should NOT block. If the driver can't perform
	// asynchronous reads (or you just don't want to bother handling
	// them), make this function a no-op and just read sectors
	// normally.
}

//////////////////////////////////////////////////////////////////////////////
// ISO Interface
//////////////////////////////////////////////////////////////////////////////

static const s8 syncHdr[12] = { 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x00 };
static FILE *isofile=NULL;
static int isofilesize=0;
static int bytesPerSector = 0;
static int isbincue = 0;
static u32 isoTOC[102];
static struct
{
   u32 fadstart;
   u32 fileoffset;
} isooffsettbl[100];

#define MSF_TO_FAD(m,s,f) ((m * 4500) + (s * 75) + f)

//////////////////////////////////////////////////////////////////////////////

static int InitBinCue(const char *cuefilename)
{
   u32 size;
   char *tempbuffer, *tempbuffer2;
   unsigned int tracknum;
   unsigned int indexnum, min, sec, frame;
   unsigned int pregap=0;
   char *p, *p2;

   fseek(isofile, 0, SEEK_END);
   size = ftell(isofile);
   fseek(isofile, 0, SEEK_SET);

   // Allocate buffer with enough space for reading cue
   if ((tempbuffer = (char *)calloc(size, 1)) == NULL)
      return -1;

   // Skip image filename
   if (fscanf(isofile, "FILE \"%*[^\"]\" %*s\r\n") == EOF)
   {
      free(tempbuffer);
      return -1;
   }

   // Time to generate TOC
   for (;;)
   {
      // Retrieve a line in cue
      if (fscanf(isofile, "%s", tempbuffer) == EOF)
         break;

      // Figure out what it is
      if (strncmp(tempbuffer, "TRACK", 5) == 0)
      {
         // Handle accordingly
         if (fscanf(isofile, "%d %[^\r\n]\r\n", &tracknum, tempbuffer) == EOF)
            break;

         if (strncmp(tempbuffer, "MODE1", 5) == 0 ||
             strncmp(tempbuffer, "MODE2", 5) == 0)
         {
            // Figure out the track sector size
            bytesPerSector = atoi(tempbuffer + 6);

            // Update toc entry
            isoTOC[tracknum-1] = 0x41000000;
         }
         else if (strncmp(tempbuffer, "AUDIO", 5) == 0)
         {
            // fix me
            // Update toc entry
            isoTOC[tracknum-1] = 0x01000000;
         }
      }
      else if (strncmp(tempbuffer, "INDEX", 5) == 0)
      {
         // Handle accordingly

         if (fscanf(isofile, "%d %d:%d:%d\r\n", &indexnum, &min, &sec, &frame) == EOF)
            break;

         if (indexnum == 1)
         {
            // Update toc entry
            isoTOC[tracknum-1] = (isoTOC[tracknum-1] & 0xFF000000) | (MSF_TO_FAD(min, sec, frame) + pregap + 150);

            isooffsettbl[tracknum-1].fadstart = MSF_TO_FAD(min, sec, frame) + pregap + 150;
            isooffsettbl[tracknum-1].fileoffset = pregap + 150;
         }
      }
      else if (strncmp(tempbuffer, "PREGAP", 5) == 0)
      {
         if (fscanf(isofile, "%d:%d:%d\r\n", &min, &sec, &frame) == EOF)
            break;

         pregap += MSF_TO_FAD(min, sec, frame);
      }
      else if (strncmp(tempbuffer, "POSTGAP", 5) == 0)
      {
         if (fscanf(isofile, "%d:%d:%d\r\n", &min, &sec, &frame) == EOF)
            break;
      }
   }

   // Go back, retrieve image filename
   fseek(isofile, 0, SEEK_SET);
   fscanf(isofile, "FILE \"%[^\"]\" %*s\r\n", tempbuffer);
   fclose(isofile);

   // Now go and open up the image file, figure out its size, etc.
   if ((isofile = fopen(tempbuffer, "rb")) == NULL)
   {
      // Ok, exact path didn't work. Let's trim the path and try opening the
      // file from the same directory as the cue.

      // find the start of filename
      p = tempbuffer;

      for (;;)
      {
         if (strcspn(p, "/\\") == strlen(p))
         break;

         p += strcspn(p, "/\\") + 1;
      }

      // append directory of cue file with bin filename
      if ((tempbuffer2 = (char *)calloc(strlen(cuefilename) + strlen(p) + 1, 1)) == NULL)
      {
         free(tempbuffer);
         return -1;
      }

      // find end of path
      p2 = (char *)cuefilename;

      for (;;)
      {
         if (strcspn(p2, "/\\") == strlen(p2))
            break;
         p2 += strcspn(p2, "/\\") + 1;
      }

      // Make sure there was at least some kind of path, otherwise our
      // second check is pretty useless
      if (cuefilename == p2 && tempbuffer == p)
      {
         free(tempbuffer);
         free(tempbuffer2);
         return -1;
      }

      strncpy(tempbuffer2, cuefilename, p2 - cuefilename);
      strcat(tempbuffer2, p);

      // Let's give it another try
      isofile = fopen(tempbuffer2, "rb");
      free(tempbuffer2);

      if (isofile == NULL)
      {
         YabSetError(YAB_ERR_FILENOTFOUND, tempbuffer);
         free(tempbuffer);
         return -1;
      }
   }

   // buffer is no longer needed
   free(tempbuffer);

   fseek(isofile, 0, SEEK_END);
   isofilesize = ftell(isofile);
   fseek(isofile, 0, SEEK_SET);

   // Now then, generate rest of TOC
   isoTOC[99] = (isoTOC[0] & 0xFF000000) | 0x010000;
   isoTOC[100] = (isoTOC[tracknum - 1] & 0xFF000000) | (tracknum << 16);
   isoTOC[101] = (isoTOC[tracknum - 1] & 0xFF000000) | ((isofilesize / bytesPerSector) + pregap + 150);

   isooffsettbl[tracknum].fileoffset = 0;
   isooffsettbl[tracknum].fadstart = 0xFFFFFFFF;

   return 0;
}

//////////////////////////////////////////////////////////////////////////////

static int ISOCDInit(const char * iso) {
   char header[6];

   memset(isoTOC, 0xFF, 0xCC * 2);

   if (!iso)
      return -1;

   if (!(isofile = fopen(iso, "rb")))
   {
      YabSetError(YAB_ERR_FILENOTFOUND, (char *)iso);
      return -1;
   }

   fread((void *)header, 1, 6, isofile);

   // Figure out what kind of image format we're dealing with
   if (strncmp(header, "FILE \"", 6) == 0)
   {
      // It's a BIN/CUE
      isbincue = 1;

      // Generate TOC for bin file
      if (InitBinCue(iso) != 0)
      {
         if (isofile)
            free(isofile);
         return -1;
      }   
   }
   else
   {
      // Assume it's an ISO file
      isbincue = 0;

      fseek(isofile, 0, SEEK_END);
      isofilesize = ftell(isofile);
	
      if (0 == (isofilesize % 2048))
         bytesPerSector = 2048;
      else if (0 == (isofilesize % 2352))
         bytesPerSector = 2352;
      else
      {
         YabSetError(YAB_ERR_OTHER, "Unsupported CD image!\n");

         return -1;
      }

      // Generate TOC
      isoTOC[0] = 0x41000096;
      isoTOC[99] = 0x41010000; 
      isoTOC[100] = 0x41010000;
      isoTOC[101] = (0x41 << 24) | (isofilesize / bytesPerSector);       //this isn't fully correct, but it does the job for now.

      isooffsettbl[0].fileoffset = 150;
      isooffsettbl[0].fadstart = 150;
      isooffsettbl[1].fileoffset = 0;
      isooffsettbl[1].fadstart = 0xFFFFFFFF;
   }

   return 0;
}

//////////////////////////////////////////////////////////////////////////////

static void ISOCDDeInit(void) {
   if (isofile)
   {
      fclose(isofile);
   }
}

//////////////////////////////////////////////////////////////////////////////

static int ISOCDGetStatus(void) {
   return isofile != NULL ? 0 : 2;
}

//////////////////////////////////////////////////////////////////////////////

static s32 ISOCDReadTOC(u32 * TOC) {
   memcpy(TOC, isoTOC, 0xCC * 2);

   return (0xCC * 2);
}

//////////////////////////////////////////////////////////////////////////////

static int ISOCDReadSectorFAD(u32 FAD, void *buffer) {
        int sector;
        int i;
	
        assert(isofile);

        memset(buffer, 0, 2352);

        for (i = 1; i < 100; i++)
        {
           if (FAD < isooffsettbl[i].fadstart)
           {             
               sector = FAD - isooffsettbl[i-1].fileoffset;
               break;
           }
        }
	if (i == 100) {
		CDLOG("Warning: Sector not found in track list");
		return 0;
	}

	if ((sector * bytesPerSector) >= isofilesize) {
		CDLOG("Warning: Trying to read beyond end of CD image! (sector: %d)\n", sector);
		return 0;
	}
	
        fseek(isofile, sector * bytesPerSector, SEEK_SET);
	
	if (2048 == bytesPerSector) {
		memcpy(buffer, syncHdr, 12);
		fread((char *)buffer + 0x10, bytesPerSector, 1, isofile);
	} else { //2352
		fread(buffer, bytesPerSector, 1, isofile);
	}
	
	return 1;
}

//////////////////////////////////////////////////////////////////////////////

static void ISOCDReadAheadFAD(UNUSED u32 FAD)
{
	// No-op
}

//////////////////////////////////////////////////////////////////////////////


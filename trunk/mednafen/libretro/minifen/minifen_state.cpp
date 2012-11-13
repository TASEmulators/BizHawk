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

#include "mednafen.h"
#include "mednafen-endian.h"

#include <string.h>

#include <trio/trio.h>
#include "driver.h"
#include "general.h"
#include "state.h"
#include "video.h"

#define RLSB 		MDFNSTATE_RLSB	//0x80000000

int32 smem_read(StateMem *st, void *buffer, uint32 len)
{
   if((len + st->loc) > st->len)
      return(0);

   memcpy(buffer, st->data + st->loc, len);
   st->loc += len;

   return(len);
}

int32 smem_write(StateMem *st, void *buffer, uint32 len)
{
   if((len + st->loc) > st->malloced)
   {
      uint32 newsize = (st->malloced >= 32768) ? st->malloced : (st->initial_malloc ? st->initial_malloc : 32768);

      while(newsize < (len + st->loc))
         newsize *= 2;
      st->data = (uint8 *)realloc(st->data, newsize);
      st->malloced = newsize;
   }
   memcpy(st->data + st->loc, buffer, len);
   st->loc += len;

   if(st->loc > st->len) st->len = st->loc;

   return(len);
}

int32 smem_putc(StateMem *st, int value)
{
   uint8 tmpval = value;
   if(smem_write(st, &tmpval, 1) != 1)
      return(-1);
   return(1);
}

int32 smem_tell(StateMem *st)
{
   return(st->loc);
}

int32 smem_seek(StateMem *st, uint32 offset, int whence)
{
   switch(whence)
   {
      case SEEK_SET: st->loc = offset; break;
      case SEEK_END: st->loc = st->len - offset; break;
      case SEEK_CUR: st->loc += offset; break;
   }

   if(st->loc > st->len)
   {
      st->loc = st->len;
      return(-1);
   }

   if(st->loc < 0)
   {
      st->loc = 0;
      return(-1);
   }

   return(0);
}

int smem_write32le(StateMem *st, uint32 b)
{
   uint8 s[4];
   s[0]=b;
   s[1]=b>>8;
   s[2]=b>>16;
   s[3]=b>>24;
   return((smem_write(st, s, 4)<4)?0:4);
}

int smem_read32le(StateMem *st, uint32 *b)
{
   uint8 s[4];

   if(smem_read(st, s, 4) < 4)
      return(0);

   *b = s[0] | (s[1] << 8) | (s[2] << 16) | (s[3] << 24);

   return(4);
}


static bool ValidateSFStructure(SFORMAT *sf)
{
   SFORMAT *saved_sf = sf;

   while(sf->size || sf->name)
   {
      SFORMAT *sub_sf = saved_sf;
      while(sub_sf->size || sub_sf->name)
      {
         if(sf != sub_sf)
         {
            if(!strncmp(sf->name, sub_sf->name, 32))
               fprintf(stderr, "Duplicate state variable name: %.32s\n", sf->name);
         }
         sub_sf++;
      }

      sf++;
   }
   return(1);
}


static bool SubWrite(StateMem *st, SFORMAT *sf, const char *name_prefix = NULL)
{
   // FIXME?  It's kind of slow, and we definitely don't want it on with state rewinding...
   ValidateSFStructure(sf);

   while(sf->size || sf->name)	// Size can sometimes be zero, so also check for the text name.  These two should both be zero only at the end of a struct.
   {
      if(!sf->size || !sf->v)
      {
         sf++;
         continue;
      }

      if(sf->size == (uint32)~0)		/* Link to another struct.	*/
      {
         if(!SubWrite(st, (SFORMAT *)sf->v, name_prefix))
            return(0);

         sf++;
         continue;
      }

      int32 bytesize = sf->size;

      char nameo[1 + 256];
      int slen;

      slen = trio_snprintf(nameo + 1, 256, "%s%s", name_prefix ? name_prefix : "", sf->name);
      nameo[0] = slen;

      if(slen >= 255)
      {
         fprintf(stderr, "Warning:  state variable name possibly too long: %s %s %s %d\n", sf->name, name_prefix, nameo, slen);
         slen = 255;
      }

      smem_write(st, nameo, 1 + nameo[0]);
      smem_write32le(st, bytesize);

      /* Flip the byte order... */
      if(sf->flags & MDFNSTATE_BOOL)
      {

      }
      else if(sf->flags & MDFNSTATE_RLSB64)
         Endian_A64_NE_to_LE(sf->v, bytesize / sizeof(uint64));
      else if(sf->flags & MDFNSTATE_RLSB32)
         Endian_A32_NE_to_LE(sf->v, bytesize / sizeof(uint32));
      else if(sf->flags & MDFNSTATE_RLSB16)
         Endian_A16_NE_to_LE(sf->v, bytesize / sizeof(uint16));
      else if(sf->flags & RLSB)
         Endian_V_NE_to_LE(sf->v, bytesize);

      // Special case for the evil bool type, to convert bool to 1-byte elements.
      // Don't do it if we're only saving the raw data.
      if(sf->flags & MDFNSTATE_BOOL)
      {
         for(int32 bool_monster = 0; bool_monster < bytesize; bool_monster++)
         {
            uint8 tmp_bool = ((bool *)sf->v)[bool_monster];
            //printf("Bool write: %.31s\n", sf->name);
            smem_write(st, &tmp_bool, 1);
         }
      }
      else
         smem_write(st, (uint8 *)sf->v, bytesize);

      /* Now restore the original byte order. */
      if(sf->flags & MDFNSTATE_BOOL)
      {

      }
      else if(sf->flags & MDFNSTATE_RLSB64)
         Endian_A64_LE_to_NE(sf->v, bytesize / sizeof(uint64));
      else if(sf->flags & MDFNSTATE_RLSB32)
         Endian_A32_LE_to_NE(sf->v, bytesize / sizeof(uint32));
      else if(sf->flags & MDFNSTATE_RLSB16)
         Endian_A16_LE_to_NE(sf->v, bytesize / sizeof(uint16));
      else if(sf->flags & RLSB)
         Endian_V_LE_to_NE(sf->v, bytesize);
      sf++; 
   }

   return(TRUE);
}

static int WriteStateChunk(StateMem *st, const char *sname, SFORMAT *sf)
{
   int32 data_start_pos;
   int32 end_pos;

   uint8 sname_tmp[32];

   memset(sname_tmp, 0, sizeof(sname_tmp));
   strncpy((char *)sname_tmp, sname, 32);

   if(strlen(sname) > 32)
      fprintf(stderr, "Warning: section name is too long: %s\n", sname);

   smem_write(st, sname_tmp, 32);

   smem_write32le(st, 0);                // We'll come back and write this later.

   data_start_pos = smem_tell(st);

   if(!SubWrite(st, sf))
      return(0);

   end_pos = smem_tell(st);

   smem_seek(st, data_start_pos - 4, SEEK_SET);
   smem_write32le(st, end_pos - data_start_pos);
   smem_seek(st, end_pos, SEEK_SET);

   return(end_pos - data_start_pos);
}

struct compare_cstr
{
   bool operator()(const char *s1, const char *s2) const
   {
      return(strcmp(s1, s2) < 0);
   }
};

typedef std::map<const char *, SFORMAT *, compare_cstr> SFMap_t;

static void MakeSFMap(SFORMAT *sf, SFMap_t &sfmap)
{
   while(sf->size || sf->name) // Size can sometimes be zero, so also check for the text name.  These two should both be zero only at the end of a struct.
   {
      if(!sf->size || !sf->v)
      {
         sf++;
         continue;
      }

      if(sf->size == (uint32)~0)            /* Link to another SFORMAT structure. */
         MakeSFMap((SFORMAT *)sf->v, sfmap);
      else
      {
         assert(sf->name);

         if(sfmap.find(sf->name) != sfmap.end())
            fprintf(stderr, "Duplicate save state variable in internal emulator structures(CLUB THE PROGRAMMERS WITH BREADSTICKS): %s\n", sf->name);

         sfmap[sf->name] = sf;
      }

      sf++;
   }
}

// Fast raw chunk reader
static void DOReadChunk(StateMem *st, SFORMAT *sf)
{
   while(sf->size || sf->name)       // Size can sometimes be zero, so also check for the text name.  
      // These two should both be zero only at the end of a struct.
   {
      if(!sf->size || !sf->v)
      {
         sf++;
         continue;
      }

      if(sf->size == (uint32) ~0) // Link to another SFORMAT struct
      {
         DOReadChunk(st, (SFORMAT *)sf->v);
         sf++;
         continue;
      }

      int32 bytesize = sf->size;

      // Loading raw data, bool types are stored as they appear in memory, not as single bytes in the full state format.
      // In the SFORMAT structure, the size member for bool entries is the number of bool elements, not the total in-memory size,
      // so we adjust it here.
      if(sf->flags & MDFNSTATE_BOOL)
         bytesize *= sizeof(bool);

      smem_read(st, (uint8 *)sf->v, bytesize);
      sf++;
   }
}

static int ReadStateChunk(StateMem *st, SFORMAT *sf, int size)
{
   int temp;

   {
      SFMap_t sfmap;
      SFMap_t sfmap_found;	// Used for identifying variables that are missing in the save state.

      MakeSFMap(sf, sfmap);

      temp = smem_tell(st);
      while(smem_tell(st) < (temp + size))
      {
         uint32 recorded_size;	// In bytes
         uint8 toa[1 + 256];	// Don't change to char unless cast toa[0] to unsigned to smem_read() and other places.

         if(smem_read(st, toa, 1) != 1)
         {
            puts("Unexpected EOF");
            return(0);
         }

         if(smem_read(st, toa + 1, toa[0]) != toa[0])
         {
            puts("Unexpected EOF?");
            return 0;
         }

         toa[1 + toa[0]] = 0;

         smem_read32le(st, &recorded_size);

         SFMap_t::iterator sfmit;

         sfmit = sfmap.find((char *)toa + 1);

         if(sfmit != sfmap.end())
         {
            SFORMAT *tmp = sfmit->second;
            uint32 expected_size = tmp->size;	// In bytes

            if(recorded_size != expected_size)
            {
               fprintf(stderr, "Variable in save state wrong size: %s.  Need: %d, got: %d\n", toa + 1, expected_size, recorded_size);
               if(smem_seek(st, recorded_size, SEEK_CUR) < 0)
               {
                  puts("Seek error");
                  return(0);
               }
            }
            else
            {
               sfmap_found[tmp->name] = tmp;

               smem_read(st, (uint8 *)tmp->v, expected_size);

               if(tmp->flags & MDFNSTATE_BOOL)
               {
                  // Converting downwards is necessary for the case of sizeof(bool) > 1
                  for(int32 bool_monster = expected_size - 1; bool_monster >= 0; bool_monster--)
                  {
                     ((bool *)tmp->v)[bool_monster] = ((uint8 *)tmp->v)[bool_monster];
                  }
               }
               if(tmp->flags & MDFNSTATE_RLSB64)
                  Endian_A64_LE_to_NE(tmp->v, expected_size / sizeof(uint64));
               else if(tmp->flags & MDFNSTATE_RLSB32)
                  Endian_A32_LE_to_NE(tmp->v, expected_size / sizeof(uint32));
               else if(tmp->flags & MDFNSTATE_RLSB16)
                  Endian_A16_LE_to_NE(tmp->v, expected_size / sizeof(uint16));
               else if(tmp->flags & RLSB)
                  Endian_V_LE_to_NE(tmp->v, expected_size);
            }
         }
         else
         {
            fprintf(stderr, "Unknown variable in save state: %s\n", toa + 1);
            if(smem_seek(st, recorded_size, SEEK_CUR) < 0)
            {
               puts("Seek error");
               return(0);
            }
         }
      } // while(...)

      for(SFMap_t::const_iterator it = sfmap.begin(); it != sfmap.end(); it++)
      {
         if(sfmap_found.find(it->second->name) == sfmap_found.end())
         {
            fprintf(stderr, "Variable missing from save state: %s\n", it->second->name);
         }
      }

      assert(smem_tell(st) == (temp + size));
   }
   return 1;
}

static int CurrentState = 0;

/* This function is called by the game driver(NES, GB, GBA) to save a state. */
int MDFNSS_StateAction(StateMem *st, int load, int data_only, std::vector <SSDescriptor> &sections)
{
   std::vector<SSDescriptor>::iterator section;

   if(load)
   {
      {
         char sname[32];

         for(section = sections.begin(); section != sections.end(); section++)
         {
            int found = 0;
            uint32 tmp_size;
            uint32 total = 0;

            while(smem_read(st, (uint8 *)sname, 32) == 32)
            {
               if(smem_read32le(st, &tmp_size) != 4)
                  return(0);

               total += tmp_size + 32 + 4;

               // Yay, we found the section
               if(!strncmp(sname, section->name, 32))
               {
                  if(!ReadStateChunk(st, section->sf, tmp_size))
                  {
                     fprintf(stderr, "Error reading chunk: %s\n", section->name);
                     return(0);
                  }
                  found = 1;
                  break;
               } 
               else
               {
                  if(smem_seek(st, tmp_size, SEEK_CUR) < 0)
                  {
                     puts("Chunk seek failure");
                     return(0);
                  }
               }
            }
            if(smem_seek(st, -total, SEEK_CUR) < 0)
            {
               puts("Reverse seek error");
               return(0);
            }
            if(!found && !section->optional) // Not found.  We are sad!
            {
               fprintf(stderr, "Section missing:  %.32s\n", section->name);
               return(0);
            }
         }
      }
   }
   else
   {
      for(section = sections.begin(); section != sections.end(); section++)
      {
         if(!WriteStateChunk(st, section->name, section->sf))
            return(0);
      }
   }

   return(1);
}

int MDFNSS_StateAction(StateMem *st, int load, int data_only, SFORMAT *sf, const char *name, bool optional)
{
   std::vector <SSDescriptor> love;

   love.push_back(SSDescriptor(sf, name, optional));
   return(MDFNSS_StateAction(st, load, 0, love));
}

int MDFNSS_SaveSM(StateMem *st, int, int, const MDFN_Surface*, const MDFN_Rect*, const MDFN_Rect*)
{
   static const char *header_magic = "MDFNSVST";
   uint8 header[32];
   int neowidth = 0, neoheight = 0;

   memset(header, 0, sizeof(header));
   memcpy(header, header_magic, 8);

   MDFN_en32lsb(header + 16, MEDNAFEN_VERSION_NUMERIC);
   MDFN_en32lsb(header + 24, neowidth);
   MDFN_en32lsb(header + 28, neoheight);
   smem_write(st, header, 32);

   if(!MDFNGameInfo->StateAction(st, 0, 0))
      return(0);

   uint32 sizy = smem_tell(st);
   smem_seek(st, 16 + 4, SEEK_SET);
   smem_write32le(st, sizy);

   return(1);
}

int MDFNSS_LoadSM(StateMem *st, int, int)
{
   uint8 header[32];
   uint32 stateversion;

   smem_read(st, header, 32);

   if(memcmp(header, "MEDNAFENSVESTATE", 16) && memcmp(header, "MDFNSVST", 8))
      return(0);

   stateversion = MDFN_de32lsb(header + 16);

   return(MDFNGameInfo->StateAction(st, stateversion, 0));
}

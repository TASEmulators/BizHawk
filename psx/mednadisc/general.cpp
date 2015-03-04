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

#include "emuware/emuware.h"

#include <string.h>
#include <stdarg.h>

#include <sys/types.h>
#include <sys/stat.h>

#include <string>
#include <map>

#include "general.h"

#include "error.h"

#define _(X) X

#ifdef WIN32
#define PSS "\\"
#else
#define PSS "/"
#endif

static struct {
	bool untrusted_fip_check;
} s_settings;


using namespace std;

static string BaseDirectory;
static string FileBase;
static string FileExt;	/* Includes the . character, as in ".nes" */
static string FileBaseDirectory;

void MDFN_SetBaseDirectory(const std::string& dir)
{
 BaseDirectory = string(dir);
}

std::string MDFN_GetBaseDirectory(void)
{
 return BaseDirectory;
}

// Really dumb, maybe we should use boost?
static bool IsAbsolutePath(const char *path)
{
 #if PSS_STYLE==4
  if(path[0] == ':')
 #elif PSS_STYLE==1
  if(path[0] == '/')
 #else
  if(path[0] == '\\'
  #if PSS_STYLE!=3
   || path[0] == '/'
  #endif
 )
 #endif
 {
  return(true);
 }

 #if defined(WIN32) || defined(DOS)
 if((path[0] >= 'a' && path[0] <= 'z') || (path[0] >= 'A' && path[0] <= 'Z'))
 {
  if(path[1] == ':')
  {
   return(true);
  }
 }
 #endif

 return(false);
}

static bool IsAbsolutePath(const std::string &path)
{
 return(IsAbsolutePath(path.c_str()));
}

bool MDFN_IsFIROPSafe(const std::string &path)
{
 //
 // First, check for any 8-bit characters, and print a warning about portability.
 //
 for(size_t x = 0; x < path.size(); x++)
 {
  if(path[x] & 0x80)
  {
   printf(_("WARNING: Referenced path \"%s\" contains at least one 8-bit non-ASCII character; this may cause portability issues.\n"), path.c_str());
   break;
  }
 }

 // We could make this more OS-specific, but it shouldn't hurt to try to weed out usage of characters that are path
 // separators in one OS but not in another, and we'd also run more of a risk of missing a special path separator case
 // in some OS.
 if(!s_settings.untrusted_fip_check)
  return(true);

 if(path.find('\0') != string::npos)
  return(false);

 if(path.find(':') != string::npos)
  return(false);

 if(path.find('\\') != string::npos)
  return(false);

 if(path.find('/') != string::npos)
  return(false);

#if defined(DOS) || defined(WIN32)
 //
 // http://support.microsoft.com/kb/74496
 //
 {
  static const char* dev_names[] = 
  {
   "CON", "PRN", "AUX", "CLOCK$", "NUL", "COM1", "COM2", "COM3", "COM4", "LPT1", "LPT2", "LPT3", NULL
  };

  for(const char** ls = dev_names; *ls != NULL; ls++)
  {
   if(!strcasecmp(*ls, path.c_str()))
    return(false);
  }
 }
#endif

 return(true);
}

void MDFN_GetFilePathComponents(const std::string &file_path, std::string *dir_path_out, std::string *file_base_out, std::string *file_ext_out)
{
 size_t final_ds;		// in file_path
 string file_name;
 size_t fn_final_dot;		// in local var file_name
 // Temporary output:
 string dir_path, file_base, file_ext;

#if PSS_STYLE==4
 final_ds = file_path.find_last_of(':');
#elif PSS_STYLE==1
 final_ds = file_path.find_last_of('/');
#else
 final_ds = file_path.find_last_of('\\');

 #if PSS_STYLE!=3
  {
   size_t alt_final_ds = file_path.find_last_of('/');

   if(final_ds == string::npos || (alt_final_ds != string::npos && alt_final_ds > final_ds))
    final_ds = alt_final_ds;
  }
 #endif
#endif

 if(final_ds == string::npos)
 {
  dir_path = string(".");
  file_name = file_path;
 }
 else
 {
  dir_path = file_path.substr(0, final_ds);
  file_name = file_path.substr(final_ds + 1);
 }

 fn_final_dot = file_name.find_last_of('.');

 if(fn_final_dot != string::npos)
 {
  file_base = file_name.substr(0, fn_final_dot);
  file_ext = file_name.substr(fn_final_dot);
 }
 else
 {
  file_base = file_name;
  file_ext = string("");
 }

 if(dir_path_out)
  *dir_path_out = dir_path;

 if(file_base_out)
  *file_base_out = file_base;

 if(file_ext_out)
  *file_ext_out = file_ext;
}

std::string MDFN_EvalFIP(const std::string &dir_path, const std::string &rel_path, bool skip_safety_check)
{
 if(!skip_safety_check && !MDFN_IsFIROPSafe(rel_path))
  throw MDFN_Error(0, _("Referenced path \"%s\" is potentially unsafe.  See \"filesys.untrusted_fip_check\" setting.\n"), rel_path.c_str());

 if(IsAbsolutePath(rel_path.c_str()))
  return(rel_path);
 else
 {
  return(dir_path + std::string(PSS) + rel_path);
 }
}


typedef std::map<char, std::string> FSMap;

static std::string EvalPathFS(const std::string &fstring, /*const (won't work because entry created if char doesn't exist) */ FSMap &fmap)
{
 std::string ret = "";
 const char *str = fstring.c_str();
 bool in_spec = false;

 while(*str)
 {
  int c = *str;

  if(!in_spec && c == '%')
   in_spec = true;
  else if(in_spec == true)
  {
   if(c == '%')
    ret = ret + std::string("%");
   else
    ret = ret + fmap[(char)c];
   in_spec = false;
  }
  else
  {
   char ct[2];
   ct[0] = c;
   ct[1] = 0;
   ret += std::string(ct);
  }

  str++;
 }

 return(ret);
}

#if 0
static void CreateMissingDirs(const char *path)
{
 const char *s = path;
 bool first_psep = true;
 char last_char = 0;
 const char char_test1 = '/', char_test2 = '/';


 while(*s)
 {
  if(*s == char_test1 || *s == char_test2)
  {
   if(last_char != *s)	//char_test1 && last_char != char_test2)
   {
    if(!first_psep)
    {
     char tmpbuf[(s - path) + 1];
     tmpbuf[s - path] = 0;
     strncpy(tmpbuf, path, s - path);

     puts(tmpbuf);
     //MDFN_mkdir(tmpbuf, S_IRWXU);
    }
   }

   first_psep = false;
  }
  last_char = *s;
  s++;
 }
}
#endif

const char * GetFNComponent(const char *str)
{
 const char *tp1;

 #if PSS_STYLE==4
     tp1=((char *)strrchr(str,':'));
 #elif PSS_STYLE==1
     tp1=((char *)strrchr(str,'/'));
 #else
     tp1=((char *)strrchr(str,'\\'));
  #if PSS_STYLE!=3
  {
     const char *tp3;
     tp3=((char *)strrchr(str,'/'));
     if(tp1<tp3) tp1=tp3;
  }
  #endif
 #endif

 if(tp1)
  return(tp1+1);
 else
  return(str);
}

void GetFileBase(const char *f)
{
        const char *tp1,*tp3;

 #if PSS_STYLE==4
     tp1=((char *)strrchr(f,':'));
 #elif PSS_STYLE==1
     tp1=((char *)strrchr(f,'/'));
 #else
     tp1=((char *)strrchr(f,'\\'));
  #if PSS_STYLE!=3
     tp3=((char *)strrchr(f,'/'));
     if(tp1<tp3) tp1=tp3;
  #endif
 #endif
     if(!tp1)
     {
      tp1=f;
      FileBaseDirectory = ".";
     }
     else
     {
      char* tmpfn = (char*)alloca(tp1 - f + 1);

      memcpy(tmpfn,f,tp1-f);
      tmpfn[tp1-f]=0;
      FileBaseDirectory = string(tmpfn);

      tp1++;
     }

     if(((tp3=strrchr(f,'.'))!=NULL) && (tp3>tp1))
     {
      char* tmpbase = (char*)alloca(tp3 - tp1 + 1);

      memcpy(tmpbase,tp1,tp3-tp1);
      tmpbase[tp3-tp1]=0;
      FileBase = string(tmpbase);
      FileExt = string(tp3);
     }
     else
     {
      FileBase = string(tp1);
      FileExt = "";
     }
}


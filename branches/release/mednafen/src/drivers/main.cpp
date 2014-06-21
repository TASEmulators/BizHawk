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

#include "main.h"

#ifdef WIN32
#include <windows.h>
#endif

//#include <unistd.h> //zero 07-feb-2012
#include <sys/types.h>
#include <signal.h>
//#include <sys/time.h>  //zero 07-feb-2012
#include <time.h>  //zero 07-feb-2012
#include <direct.h>
//#include <sys/stat.h>  //zero 07-feb-2012
#include <string.h>
//#include <strings.h>  //zero 07-feb-2012
#include <errno.h>
#include <trio/trio.h>
#include <locale.h>

#ifdef HAVE_GETPWUID
#include <pwd.h>
#endif

#ifdef HAVE_LIBCDIO
#include <cdio/version.h>
#endif

#include "input.h"
#include "Joystick.h"
#include "video.h"
#include "opengl.h"
#include "shader.h"
#include "sound.h"
#include "netplay.h"
#include "cheat.h"
#include "fps.h"
#include "debugger.h"
#include "memdebugger.h"
#include "help.h"
#include "video-state.h"
#include "remote.h"
#include "ers.h"
#include "../qtrecord.h"
#include <math.h>

JoystickManager *joy_manager = NULL;
bool MDFNDHaveFocus;
static bool RemoteOn = FALSE;
bool pending_save_state, pending_snapshot, pending_save_movie;
static Uint32 volatile MainThreadID = 0;
static bool ffnosound;

static const char *CSD_xres = gettext_noop("Full-screen horizontal resolution.");
static const char *CSD_yres = gettext_noop("Full-screen vertical resolution.");
static const char *CSDE_xres = gettext_noop("A value of \"0\" will cause the desktop horizontal resolution to be used.");
static const char *CSDE_yres = gettext_noop("A value of \"0\" will cause the desktop vertical resolution to be used.");

static const char *CSD_xscale = gettext_noop("Scaling factor for the X axis.");
static const char *CSD_yscale = gettext_noop("Scaling factor for the Y axis.");

static const char *CSD_xscalefs = gettext_noop("Scaling factor for the X axis in fullscreen mode.");
static const char *CSD_yscalefs = gettext_noop("Scaling factor for the Y axis in fullscreen mode.");
static const char *CSDE_xyscalefs = gettext_noop("For this settings to have any effect, the \"<system>.stretch\" setting must be set to \"0\".");

static const char *CSD_scanlines = gettext_noop("Enable scanlines with specified opacity.");
static const char *CSDE_scanlines = gettext_noop("Opacity is specified in %; IE a value of \"100\" will give entirely black scanlines.");

static const char *CSD_stretch = gettext_noop("Stretch to fill screen.");
static const char *CSD_videoip = gettext_noop("Enable (bi)linear interpolation.");


static const char *CSD_special = gettext_noop("Enable specified special video scaler.");
static const char *CSDE_special = gettext_noop("The destination rectangle is NOT altered by this setting, so if you have xscale and yscale set to \"2\", and try to use a 3x scaling filter like hq3x, the image is not going to look that great. The nearest-neighbor scalers are intended for use with bilinear interpolation enabled, at high resolutions(such as 1280x1024; nn2x(or nny2x) + bilinear interpolation + fullscreen stretching at this resolution looks quite nice).");

static const char *CSD_pixshader = gettext_noop("Enable specified OpenGL pixel shader.");
static const char *CSDE_pixshader = gettext_noop("Obviously, this will only work with the OpenGL \"video.driver\" setting, and only on cards and OpenGL implementations that support pixel shaders, otherwise you will get a black screen, or Mednafen may display an error message when starting up. Bilinear interpolation is disabled with pixel shaders, and any interpolation, if present, will be noted in the description of each pixel shader.");

static MDFNSetting_EnumList VDriver_List[] =
{
 // Legacy:
 { "0", VDRIVER_OPENGL },
 { "1", VDRIVER_SOFTSDL },


 { "opengl", VDRIVER_OPENGL, "OpenGL + SDL", gettext_noop("This output method is preferred, as all features are available with it.") },
 { "sdl", VDRIVER_SOFTSDL, "SDL Surface", gettext_noop("Slower with lower-quality scaling than OpenGL, but if you don't have hardware-accelerated OpenGL rendering, it will be faster than software OpenGL rendering. Bilinear interpolation not available. Pixel shaders do not work with this output method, of course.") },
 { "overlay", VDRIVER_OVERLAY, "SDL Overlay", gettext_noop("As fast as OpenGL, perhaps faster in some situations, *if* it's hardware-accelerated. Scanline effects are not available. hq2x, hq3x, hq4x are not available. The OSD may be missing or glitchy. Bilinear interpolation can't be turned off. Harsh chroma subsampling blurring in some picture types.  If you use this output method, it is strongly recommended to use a special scaler with it, such as nn2x.") },

 { NULL, 0 },
};

static MDFNSetting_EnumList SDriver_List[] =
{
 { "default", -1, "Default", gettext_noop("Default sound driver.") },

 { "alsa", -1, "ALSA", gettext_noop("A recommended driver, and the default for Linux(if available).") },
 { "oss", -1, "Open Sound System", gettext_noop("A recommended driver, and the default for non-Linux UN*X/POSIX/BSD systems, or anywhere ALSA is unavailable. If the ALSA driver gives you problems, you can try using this one instead.\n\nIf you are using OSSv4 or newer, you should edit \"/usr/lib/oss/conf/osscore.conf\", uncomment the max_intrate= line, and change the value from 100(default) to 1000(or higher if you know what you're doing), and restart OSS. Otherwise, performance will be poor, and the sound buffer size in Mednafen will be orders of magnitude larger than specified.\n\nIf the sound buffer size is still excessively larger than what is specified via the \"sound.buffer_time\" setting, you can try setting \"sound.period_time\" to 2666, and as a last resort, 5333, to work around a design flaw/limitation/choice in the OSS API and OSS implementation.") },
 { "dsound", -1, "DirectSound", gettext_noop("A recommended driver, and the default for Microsoft Windows.") },
 { "sdl", -1, "Simple Directmedia Layer", gettext_noop("This driver is not recommended, but it serves as a backup driver if the others aren't available. Its performance is generally sub-par, requiring higher latency or faster CPUs/SMP for glitch-free playback, except where the OS provides a sound callback API itself, such as with Mac OS X and BeOS.") },
 { "jack", -1, "JACK", gettext_noop("Somewhat experimental driver, unusably buggy until Mednafen 0.8.C. The \"sound.buffer_time\" setting controls the size of the local sound buffer, not the server's sound buffer, and the latency reported during startup is for the local sound buffer only. Please note that video card drivers(in the kernel or X), and hardware-accelerated OpenGL, may interfere with jackd's ability to effectively run with realtime response.") },

 { NULL, 0 },
};

static MDFNSetting_EnumList Special_List[] =
{
    { "0", 	-1 },
    { "none", 	-1, "None/Disabled" },
    { "hq2x", 	-1, "hq2x" },
    { "hq3x", 	-1, "hq3x" },
    { "hq4x", 	-1, "hq4x" },
    { "scale2x",-1, "scale2x" },
    { "scale3x",-1, "scale3x" },
    { "scale4x",-1, "scale4x" },

    { "2xsai", 	-1, "2xSaI" },
    { "super2xsai", -1, "Super 2xSaI" },
    { "supereagle", -1, "Super Eagle" },
    { "nn2x",	-1, "Nearest-neighbor 2x" },
    { "nn3x",	-1, "Nearest-neighbor 3x" },
    { "nn4x",	-1, "Nearest-neighbor 4x" },
    { "nny2x",	-1, "Nearest-neighbor 2x, y axis only" },
    { "nny3x",	-1, "Nearest-neighbor 3x, y axis only" }, 
    { "nny4x",	-1, "Nearest-neighbor 4x, y axis only" },
    { NULL, 0 },
};

static MDFNSetting_EnumList Pixshader_List[] =
{
    { "none",		SHADER_NONE,		"None/Disabled" },
    { "autoip", 	SHADER_AUTOIP,	"Auto Interpolation", gettext_noop("Will automatically interpolate on each axis if the corresponding effective scaling factor is not an integer.") },
    { "autoipsharper",	SHADER_AUTOIPSHARPER,	"Sharper Auto Interpolation", gettext_noop("Same as \"autoip\", but when interpolation is done, it is done in a manner that will reduce blurriness if possible.") },
    { "scale2x", 	SHADER_SCALE2X,    "Scale2x" },

    { "ipsharper", 	SHADER_IPSHARPER,  "Sharper bilinear interpolation." },
    { "ipxnoty", 	SHADER_IPXNOTY,    "Linear interpolation on X axis only." },
    { "ipynotx", 	SHADER_IPYNOTX,    "Linear interpolation on Y axis only." },
    { "ipxnotysharper", SHADER_IPXNOTYSHARPER, "Sharper version of \"ipxnoty\"." },
    { "ipynotxsharper", SHADER_IPYNOTXSHARPER, "Sharper version of \"ipynotx\"." },

    { NULL, 0 },
};

static std::vector <MDFNSetting> NeoDriverSettings;
static MDFNSetting DriverSettings[] =
{
  { "input.joystick.global_focus", MDFNSF_NOFLAGS, gettext_noop("Update physical joystick(s) internal state in Mednafen even when Mednafen lacks OS focus."), NULL, MDFNST_BOOL, "1" },
  { "input.joystick.axis_threshold", MDFNSF_NOFLAGS, gettext_noop("Analog axis binary press detection threshold."), gettext_noop("Threshold for detecting a digital-like \"button\" press on analog axis, in percent."), MDFNST_FLOAT, "75", "0", "100" },
  { "input.autofirefreq", MDFNSF_NOFLAGS, gettext_noop("Auto-fire frequency."), gettext_noop("Auto-fire frequency = GameSystemFrameRateHz / (value + 1)"), MDFNST_UINT, "3", "0", "1000" },
  { "input.ckdelay", MDFNSF_NOFLAGS, gettext_noop("Dangerous key action delay."), gettext_noop("The length of time, in milliseconds, that a button/key corresponding to a \"dangerous\" command like power, reset, exit, etc. must be pressed before the command is executed."), MDFNST_UINT, "0", "0", "99999" },

  { "netplay.host", MDFNSF_NOFLAGS, gettext_noop("Server hostname."), NULL, MDFNST_STRING, "netplay.fobby.net" },
  { "netplay.port", MDFNSF_NOFLAGS, gettext_noop("Server port."), NULL, MDFNST_UINT, "4046", "1", "65535" },
  { "netplay.password", MDFNSF_NOFLAGS, gettext_noop("Server password."), gettext_noop("Password to connect to the netplay server."), MDFNST_STRING, "" },
  { "netplay.localplayers", MDFNSF_NOFLAGS, gettext_noop("Local player count."), gettext_noop("Number of local players for network play.  This number is advisory to the server, and the server may assign fewer players if the number of players requested is higher than the number of controllers currently available."), MDFNST_UINT, "1", "0", "16" },
  { "netplay.nick", MDFNSF_NOFLAGS, gettext_noop("Nickname."), gettext_noop("Nickname to use for network play chat."), MDFNST_STRING, "" },
  { "netplay.gamekey", MDFNSF_NOFLAGS, gettext_noop("Key to hash with the MD5 hash of the game."), NULL, MDFNST_STRING, "" },
  { "netplay.smallfont", MDFNSF_NOFLAGS, gettext_noop("Use small(tiny!) font for netplay chat console."), NULL, MDFNST_BOOL, "0" },

  { "video.fs", MDFNSF_NOFLAGS, gettext_noop("Enable fullscreen mode."), NULL, MDFNST_BOOL, "0", },
  { "video.driver", MDFNSF_NOFLAGS, gettext_noop("Select video driver, \"opengl\" or \"sdl\"."), NULL, MDFNST_ENUM, "opengl", NULL, NULL, NULL,NULL, VDriver_List },
  { "video.glvsync", MDFNSF_NOFLAGS, gettext_noop("Attempt to synchronize OpenGL page flips to vertical retrace period."), 
			       gettext_noop("Note: Additionally, if the environment variable \"__GL_SYNC_TO_VBLANK\" does not exist, then it will be created and set to the value specified for this setting.  This has the effect of forcibly enabling or disabling vblank synchronization when running under Linux with NVidia's drivers."),
				MDFNST_BOOL, "1" },

  { "video.frameskip", MDFNSF_NOFLAGS, gettext_noop("Enable frameskip during emulation rendering."), 
					gettext_noop("Disable for rendering code performance testing."), MDFNST_BOOL, "1" },

  { "video.blit_timesync", MDFNSF_NOFLAGS, gettext_noop("Enable time synchronization(waiting) for frame blitting."),
					gettext_noop("Disable to reduce latency, at the cost of potentially increased video \"juddering\", with the maximum reduction in latency being about 1 video frame's time.\nWill work best with emulated systems that are not very computationally expensive to emulate, combined with running on a relatively fast CPU."),
					MDFNST_BOOL, "1" },

  { "ffspeed", MDFNSF_NOFLAGS, gettext_noop("Fast-forwarding speed multiplier."), NULL, MDFNST_FLOAT, "4", "1", "15" },
  { "fftoggle", MDFNSF_NOFLAGS, gettext_noop("Treat the fast-forward button as a toggle."), NULL, MDFNST_BOOL, "0" },
  { "ffnosound", MDFNSF_NOFLAGS, gettext_noop("Silence sound output when fast-forwarding."), NULL, MDFNST_BOOL, "0" },

  { "sfspeed", MDFNSF_NOFLAGS, gettext_noop("SLOW-forwarding speed multiplier."), NULL, MDFNST_FLOAT, "0.75", "0.25", "1" },
  { "sftoggle", MDFNSF_NOFLAGS, gettext_noop("Treat the SLOW-forward button as a toggle."), NULL, MDFNST_BOOL, "0" },

  { "nothrottle", MDFNSF_NOFLAGS, gettext_noop("Disable speed throttling when sound is disabled."), NULL, MDFNST_BOOL, "0"},
  { "autosave", MDFNSF_NOFLAGS, gettext_noop("Automatic load/save state on game load/save."), gettext_noop("Automatically save and load save states when a game is closed or loaded, respectively."), MDFNST_BOOL, "0"},
  { "sound.driver", MDFNSF_NOFLAGS, gettext_noop("Select sound driver."), gettext_noop("The following choices are possible, sorted by preference, high to low, when \"default\" driver is used, but dependent on being compiled in."), MDFNST_ENUM, "default", NULL, NULL, NULL, NULL, SDriver_List },
  { "sound.device", MDFNSF_NOFLAGS, gettext_noop("Select sound output device."), gettext_noop("When using ALSA sound output under Linux, the \"sound.device\" setting \"default\" is Mednafen's default, IE \"hw:0\", not ALSA's \"default\". If you want to use ALSA's \"default\", use \"sexyal-literal-default\"."), MDFNST_STRING, "default", NULL, NULL },
  { "sound.volume", MDFNSF_NOFLAGS, gettext_noop("Sound volume level, in percent."), NULL, MDFNST_UINT, "100", "0", "150" },
  { "sound", MDFNSF_NOFLAGS, gettext_noop("Enable sound output."), NULL, MDFNST_BOOL, "1" },
  { "sound.period_time", MDFNSF_NOFLAGS, gettext_noop("Desired period size in microseconds."), gettext_noop("Currently only affects OSS and ALSA output.  A value of 0 defers to the default in the driver code in SexyAL.\n\nNote: This is not the \"sound buffer size\" setting, that would be \"sound.buffer_time\"."), MDFNST_UINT,  "0", "0", "100000" },
  { "sound.buffer_time", MDFNSF_NOFLAGS, gettext_noop("Desired total buffer size in milliseconds."), NULL, MDFNST_UINT, 
   #ifdef WIN32
   "52"
   #else
   "32"
   #endif
   ,"1", "1000" },
  { "sound.rate", MDFNSF_NOFLAGS, gettext_noop("Specifies the sound playback rate, in sound frames per second(\"Hz\")."), NULL, MDFNST_UINT, "48000", "22050", "1048576"},

  #ifdef WANT_DEBUGGER
  { "debugger.autostepmode", MDFNSF_NOFLAGS, gettext_noop("Automatically go into the debugger's step mode after a game is loaded."), NULL, MDFNST_BOOL, "0" },
  #endif

  { "osd.state_display_time", MDFNSF_NOFLAGS, gettext_noop("The length of time, in milliseconds, to display the save state or the movie selector after selecting a state or movie."),  NULL, MDFNST_UINT, "2000", "0", "15000" },
  { "osd.alpha_blend", MDFNSF_NOFLAGS, gettext_noop("Enable alpha blending for OSD elements."), NULL, MDFNST_BOOL, "1" },
};

static void BuildSystemSetting(MDFNSetting *setting, const char *system_name, const char *name, const char *description, const char *description_extra, MDFNSettingType type, 
	const char *default_value, const char *minimum = NULL, const char *maximum = NULL,
	bool (*validate_func)(const char *name, const char *value) = NULL, void (*ChangeNotification)(const char *name) = NULL, 
        const MDFNSetting_EnumList *enum_list = NULL)
{
 char setting_name[256];

 memset(setting, 0, sizeof(MDFNSetting));

 trio_snprintf(setting_name, 256, "%s.%s", system_name, name);

 setting->name = strdup(setting_name);
 setting->flags = MDFNSF_COMMON_TEMPLATE;
 setting->description = description;
 setting->description_extra = description_extra;
 setting->type = type;
 setting->default_value = default_value;
 setting->minimum = minimum;
 setting->maximum = maximum;
 setting->validate_func = validate_func;
 setting->ChangeNotification = ChangeNotification;
 setting->enum_list = enum_list;
}

// TODO: Actual enum values
static const MDFNSetting_EnumList DisFontSize_List[] =
{
 { "xsmall", 	-1, gettext_noop("4x5") },
 { "small",	-1, gettext_noop("5x7") },
 { "medium",	-1, gettext_noop("6x13") },
 { "large",	-1, gettext_noop("9x18") },
 { NULL, 0 },
};

static const MDFNSetting_EnumList StretchMode_List[] =
{
 { "0", 0, gettext_noop("Disabled") },
 { "off", 0 },

 { "1", 1 },
 { "full", 1, gettext_noop("Full"), gettext_noop("Full-screen stretch, disregarding aspect ratio.") },

 { "2", 2 },
 { "aspect", 2, gettext_noop("Aspect Preserve"), gettext_noop("Full-screen stretch as far as the aspect ratio(in this sense, the equivalent xscalefs == yscalefs) can be maintained.") },

 { "aspect_int", 3, gettext_noop("Aspect Preserve + Integer Scale"), gettext_noop("Full-screen stretch, same as \"aspect\" except that the equivalent xscalefs and yscalefs are rounded down to the nearest integer.") },
 { "aspect_mult2", 4, gettext_noop("Aspect Preserve + Integer Multiple-of-2 Scale"), gettext_noop("Full-screen stretch, same as \"aspect_int\", but rounds down to the nearest multiple of 2.") },

 { NULL, 0 },
};

static const MDFNSetting_EnumList VideoIP_List[] =
{
 { "0", VIDEOIP_OFF, gettext_noop("Disabled") },

 { "1", VIDEOIP_BILINEAR, gettext_noop("Bilinear") },

 // Disabled until a fix can be made for rotation.
 { "x", VIDEOIP_LINEAR_X, gettext_noop("Linear (X)"), gettext_noop("Interpolation only on the X axis.") },
 { "y", VIDEOIP_LINEAR_Y, gettext_noop("Linear (Y)"), gettext_noop("Interpolation only on the Y axis.") },

 { NULL, 0 },
};

void MakeDebugSettings(std::vector <MDFNSetting> &settings)
{
 #ifdef WANT_DEBUGGER
 for(unsigned int i = 0; i < MDFNSystems.size(); i++)
 {
  const DebuggerInfoStruct *dbg = MDFNSystems[i]->Debugger;
  MDFNSetting setting;
  const char *sysname = MDFNSystems[i]->shortname;

  if(!dbg)
   continue;

  BuildSystemSetting(&setting, sysname, "debugger.disfontsize", gettext_noop("Disassembly font size."), gettext_noop("Note: Setting the font size to larger than the default may cause text overlap in the debugger."), MDFNST_ENUM, "small", NULL, NULL, NULL, NULL, DisFontSize_List);
  settings.push_back(setting);

  BuildSystemSetting(&setting, sysname, "debugger.memcharenc", gettext_noop("Character encoding for the debugger's memory editor."), NULL, MDFNST_STRING, dbg->DefaultCharEnc);
  settings.push_back(setting);
 }
 #endif
}

void MakeVideoSettings(std::vector <MDFNSetting> &settings)
{
 for(unsigned int i = 0; i < MDFNSystems.size() + 1; i++)
 {
  int nominal_width;
  int nominal_height;
  bool multires;
  const char *sysname;
  char default_value[256];
  MDFNSetting setting;
  const int default_xres = 0, default_yres = 0;
  const double default_scalefs = 1.0;
  double default_scale;

  if(i == MDFNSystems.size())
  {
   nominal_width = 384;
   nominal_height = 240;
   multires = FALSE;
   sysname = "player";
  }
  else
  {
   nominal_width = MDFNSystems[i]->nominal_width;
   nominal_height = MDFNSystems[i]->nominal_height;
   multires = MDFNSystems[i]->multires;
   sysname = (const char *)MDFNSystems[i]->shortname;
  }

  if(multires)
   default_scale = ceilf(1024 / nominal_width); //zero 07-feb-2012 - changed to ceilf
  else
   default_scale = ceilf(768 / nominal_width); //zero 07-feb-2012 - changed to ceilf

  if(default_scale * nominal_width > 1024)
   default_scale--;

  if(!default_scale)
   default_scale = 1;

  trio_snprintf(default_value, 256, "%d", default_xres);
  BuildSystemSetting(&setting, sysname, "xres", CSD_xres, CSDE_xres, MDFNST_UINT, strdup(default_value), "0", "65536");
  settings.push_back(setting);

  trio_snprintf(default_value, 256, "%d", default_yres);
  BuildSystemSetting(&setting, sysname, "yres", CSD_yres, CSDE_yres, MDFNST_UINT, strdup(default_value), "0", "65536");
  settings.push_back(setting);

  trio_snprintf(default_value, 256, "%f", default_scale);
  BuildSystemSetting(&setting, sysname, "xscale", CSD_xscale, NULL, MDFNST_FLOAT, strdup(default_value), "0.01", "256");
  settings.push_back(setting);
  BuildSystemSetting(&setting, sysname, "yscale", CSD_yscale, NULL, MDFNST_FLOAT, strdup(default_value), "0.01", "256");
  settings.push_back(setting);

  trio_snprintf(default_value, 256, "%f", default_scalefs);
  BuildSystemSetting(&setting, sysname, "xscalefs", CSD_xscalefs, CSDE_xyscalefs, MDFNST_FLOAT, strdup(default_value), "0.01", "256");
  settings.push_back(setting);
  BuildSystemSetting(&setting, sysname, "yscalefs", CSD_yscalefs, CSDE_xyscalefs, MDFNST_FLOAT, strdup(default_value), "0.01", "256");
  settings.push_back(setting);

  BuildSystemSetting(&setting, sysname, "scanlines", CSD_scanlines, CSDE_scanlines, MDFNST_UINT, "0", "0", "100");
  settings.push_back(setting);

  BuildSystemSetting(&setting, sysname, "stretch", CSD_stretch, NULL, MDFNST_ENUM, "aspect_mult2", NULL, NULL, NULL, NULL, StretchMode_List);
  settings.push_back(setting);

  BuildSystemSetting(&setting, sysname, "videoip", CSD_videoip, NULL, MDFNST_ENUM, multires ? "1" : "0", NULL, NULL, NULL, NULL, VideoIP_List);
  settings.push_back(setting);

  BuildSystemSetting(&setting, sysname, "special", CSD_special, CSDE_special, MDFNST_ENUM, "none", NULL, NULL, NULL, NULL, Special_List);
  settings.push_back(setting);

  BuildSystemSetting(&setting, sysname, "pixshader", CSD_pixshader, CSDE_pixshader, MDFNST_ENUM, "none", NULL, NULL, NULL, NULL, Pixshader_List);
  settings.push_back(setting);
 }

}

static SDL_Thread *GameThread;
static MDFN_Surface *VTBuffer[2] = { NULL, NULL };
static MDFN_Rect *VTLineWidths[2] = { NULL, NULL };

static int volatile VTBackBuffer = 0;
static SDL_mutex *VTMutex = NULL, *EVMutex = NULL, *GameMutex = NULL;
static SDL_mutex *StdoutMutex = NULL;

static MDFN_Surface * volatile VTReady;
static MDFN_Rect * volatile VTLWReady;
static MDFN_Rect * volatile VTDRReady;
static MDFN_Rect VTDisplayRects[2];
static bool sc_blit_timesync;

void LockGameMutex(bool lock)
{
 if(lock)
  SDL_mutexP(GameMutex);
 else
  SDL_mutexV(GameMutex);
}

static char *soundrecfn=0;	/* File name of sound recording. */

static char *qtrecfn = NULL;

static char *DrBaseDirectory;

MDFNGI *CurGame=NULL;

void MDFND_PrintError(const char *s)
{
 if(RemoteOn)
  Remote_SendErrorMessage(s);
 else
 {
  if(StdoutMutex)
   SDL_mutexP(StdoutMutex);
 
  puts(s);
  fflush(stdout);

#if 0
  #ifdef WIN32
  MessageBox(0, s, "Mednafen Error", MB_ICONERROR | MB_OK | MB_SETFOREGROUND | MB_TOPMOST);
  #endif
#endif

  if(StdoutMutex)
   SDL_mutexV(StdoutMutex);
 }
}

void MDFND_Message(const char *s)
{
 if(RemoteOn)
  Remote_SendStatusMessage(s);
 else
 {
  if(StdoutMutex)
   SDL_mutexP(StdoutMutex);

  fputs(s,stdout);
  fflush(stdout);

  if(StdoutMutex)
   SDL_mutexV(StdoutMutex);
 }
}

// CreateDirs should make sure errno is intact after calling mkdir() if it fails.
static bool CreateDirs(void)
{
 const char *subs[7] = { "mcs", "mcm", "snaps", "palettes", "sav", "cheats", "firmware" };
 char *tdir;

 if(MDFN_mkdir(DrBaseDirectory, S_IRWXU) == -1 && errno != EEXIST)
 {
  return(FALSE);
 }

 for(unsigned int x = 0; x < sizeof(subs) / sizeof(const char *); x++)
 {
  tdir = trio_aprintf("%s"PSS"%s",DrBaseDirectory,subs[x]);
  if(MDFN_mkdir(tdir, S_IRWXU) == -1 && errno != EEXIST)
  {
   free(tdir);
   return(FALSE);
  }
  free(tdir);
 }

 return(TRUE);
}

#if defined(HAVE_SIGNAL) || defined(HAVE_SIGACTION)

static const char *SiginfoString = NULL;
static bool volatile SignalSafeExitWanted = false;
typedef struct
{
 int number;
 const char *name;
 const char *message;
 const char *translated;	// Needed since gettext() can potentially deadlock when used in a signal handler.
 const bool SafeTryExit;
} SignalInfo;

static SignalInfo SignalDefs[] =
{
 #ifdef SIGINT
 { SIGINT, "SIGINT", gettext_noop("How DARE you interrupt me!\n"), NULL, TRUE },
 #endif

 #ifdef SIGTERM
 { SIGTERM, "SIGTERM", gettext_noop("MUST TERMINATE ALL HUMANS\n"), NULL, TRUE },
 #endif

 #ifdef SIGHUP
 { SIGHUP, "SIGHUP", gettext_noop("Reach out and hang-up on someone.\n"), NULL, FALSE },
 #endif

 #ifdef SIGSEGV
 { SIGSEGV, "SIGSEGV", gettext_noop("Iyeeeeeeeee!!!  A segmentation fault has occurred.  Have a fluffy day.\n"), NULL, FALSE },
 #endif

 #ifdef SIGPIPE
 { SIGPIPE, "SIGPIPE", gettext_noop("The pipe has broken!  Better watch out for floods...\n"), NULL, FALSE },
 #endif

 #if defined(SIGBUS) && SIGBUS != SIGSEGV
 /* SIGBUS can == SIGSEGV on some platforms */
 { SIGBUS, "SIGBUS", gettext_noop("I told you to be nice to the driver.\n"), NULL, FALSE },
 #endif

 #ifdef SIGFPE
 { SIGFPE, "SIGFPE", gettext_noop("Those darn floating points.  Ne'er know when they'll bite!\n"), NULL, FALSE },
 #endif

 #ifdef SIGALRM
 { SIGALRM, "SIGALRM", gettext_noop("Don't throw your clock at the meowing cats!\n"), NULL, TRUE },
 #endif

 #ifdef SIGABRT
 { SIGABRT, "SIGABRT", gettext_noop("Abort, Retry, Ignore, Fail?\n"), NULL, FALSE },
 #endif
 
 #ifdef SIGUSR1
 { SIGUSR1, "SIGUSR1", gettext_noop("Killing your processes is not nice.\n"), NULL, TRUE },
 #endif

 #ifdef SIGUSR2
 { SIGUSR2, "SIGUSR2", gettext_noop("Killing your processes is not nice.\n"), NULL, TRUE },
 #endif
};

static volatile int SignalSTDOUT;

static void SetSignals(void (*t)(int))
{
 SignalSTDOUT = fileno(stdout);

 SiginfoString = _("\nSignal has been caught and dealt with: ");
 for(unsigned int x = 0; x < sizeof(SignalDefs) / sizeof(SignalInfo); x++)
 {
  if(!SignalDefs[x].translated)
   SignalDefs[x].translated = _(SignalDefs[x].message);

  #ifdef HAVE_SIGACTION
  struct sigaction act;

  memset(&act, 0, sizeof(struct sigaction));

  act.sa_handler = t;
  act.sa_flags = SA_RESTART;

  sigaction(SignalDefs[x].number, &act, NULL);
  #else
  signal(SignalDefs[x].number, t);
  #endif
 }
}

static void SignalPutString(const char *string)
{
 size_t count = 0;

 while(string[count]) { count++; }

 write(SignalSTDOUT, string, count);
}

static void CloseStuff(int signum)
{
	const int save_errno = errno;
	const char *name = "unknown";
	const char *translated = NULL;
	bool safetryexit = false;

	for(unsigned int x = 0; x < sizeof(SignalDefs) / sizeof(SignalInfo); x++)
	{
	 if(SignalDefs[x].number == signum)
	 {
	  name = SignalDefs[x].name;
	  translated = SignalDefs[x].translated;
	  safetryexit = SignalDefs[x].SafeTryExit;
	  break;
	 }
	}

	SignalPutString(SiginfoString);
	SignalPutString(name);
        SignalPutString("\n");
	SignalPutString(translated);

	if(safetryexit)
	{
         SignalSafeExitWanted = safetryexit;
	 errno = save_errno;
         return;
	}

	_exit(1);
}
#endif

static ARGPSTRUCT *MDFN_Internal_Args = NULL;

static int HokeyPokeyFallDown(const char *name, const char *value)
{
 if(!MDFNI_SetSetting(name, value))
  return(0);
 return(1);
}

static void DeleteInternalArgs(void)
{
 if(!MDFN_Internal_Args) return;
 ARGPSTRUCT *argptr = MDFN_Internal_Args;

 do
 {
  free((void*)argptr->name);
  argptr++;
 } while(argptr->name || argptr->var || argptr->subs);
 free(MDFN_Internal_Args);
 MDFN_Internal_Args = NULL;
}

static void MakeMednafenArgsStruct(void)
{
 const std::multimap <uint32, MDFNCS> *settings;
 std::multimap <uint32, MDFNCS>::const_iterator sit;

 settings = MDFNI_GetSettings();

 MDFN_Internal_Args = (ARGPSTRUCT *)malloc(sizeof(ARGPSTRUCT) * (1 + settings->size()));

 unsigned int x = 0;

 for(sit = settings->begin(); sit != settings->end(); sit++)
 {
  MDFN_Internal_Args[x].name = strdup(sit->second.name);
  MDFN_Internal_Args[x].description = _(sit->second.desc->description);
  MDFN_Internal_Args[x].var = NULL;
  MDFN_Internal_Args[x].subs = (void *)HokeyPokeyFallDown;
  MDFN_Internal_Args[x].substype = SUBSTYPE_FUNCTION;
  x++;
 }
 MDFN_Internal_Args[x].name = NULL;
 MDFN_Internal_Args[x].var = NULL;
 MDFN_Internal_Args[x].subs = NULL;
}

static int netconnect = 0;
static char * loadcd = NULL;
static char * force_module_arg = NULL;
static int DoArgs(int argc, char *argv[], char **filename)
{
	int ShowCLHelp = 0;
	int DoSetRemote = 0;

	char *dsfn = NULL;
	char *dmfn = NULL;

        ARGPSTRUCT MDFNArgs[] = 
	{
	 { "help", _("Show help!"), &ShowCLHelp, 0, 0 },
	 { "remote", _("Enable remote mode(EXPERIMENTAL AND INCOMPLETE)."), &DoSetRemote, 0, 0 },

	 { "loadcd", _("Load and boot a CD for the specified system."), 0, &loadcd, SUBSTYPE_STRING_ALLOC },

	 { "force_module", _("Force usage of specified emulation module."), 0, &force_module_arg, SUBSTYPE_STRING_ALLOC },

	 { "soundrecord", _("Record sound output to the specified filename in the MS WAV format."), 0,&soundrecfn, SUBSTYPE_STRING_ALLOC },
	 { "qtrecord", _("Record video and audio output to the specified filename in the QuickTime format."), 0, &qtrecfn, SUBSTYPE_STRING_ALLOC }, // TODOC: Video recording done without filtering applied.

	 { "dump_settings_def", _("Dump settings definition data to specified file."), 0, &dsfn, SUBSTYPE_STRING_ALLOC },
	 { "dump_modules_def", _("Dump modules definition data to specified file."), 0, &dmfn, SUBSTYPE_STRING_ALLOC },

         { 0, NULL, (int *)MDFN_Internal_Args, 0, 0},

	 { "connect", _("Connect to the remote server and start network play."), &netconnect, 0, 0 },

	 { 0, 0, 0, 0 }
        };

	const char *usage_string = _("Usage: %s [OPTION]... [FILE]\n");
	if(argc <= 1)
	{
	 printf(_("No command-line arguments specified.\n\n"));
	 printf(usage_string, argv[0]);
	 printf(_("\tPlease refer to the documentation for option parameters and usage.\n\n"));
	 return(0);
	}
	else
	{
	 if(!ParseArguments(argc - 1, &argv[1], MDFNArgs, filename))
	  return(0);

	 if(ShowCLHelp)
	 {
          printf(usage_string, argv[0]);
          ShowArgumentsHelp(MDFNArgs, false);
	  printf("\n");
	  printf(_("Each setting(listed in the documentation) can also be passed as an argument by prefixing the name with a hyphen,\nand specifying the value to change the setting to as the next argument.\n\n"));
	  printf(_("For example:\n\t%s -pce.xres 1680 -pce.yres 1050 -pce.stretch aspect -pce.pixshader autoipsharper \"Hyper Bonk Soldier.pce\"\n\n"), argv[0]);
	  printf(_("Settings specified in this manner are automatically saved to the configuration file, hence they\ndo not need to be passed to future invocations of the Mednafen executable.\n"));
	  printf("\n");
	  return(0);
	 }

	 if(dsfn)
	  MDFNI_DumpSettingsDef(dsfn);

	 if(dmfn)
	  MDFNI_DumpModulesDef(dmfn);

	 if(dsfn || dmfn)
	  return(0);

	 if(*filename == NULL && loadcd == NULL)
	 {
	  puts(_("No game filename specified!"));
	  return(0);
	 }
	}
	return(1);
}

static int volatile NeedVideoChange = 0;
int GameLoop(void *arg);
int volatile GameThreadRun = 0;
void MDFND_Update(MDFN_Surface *surface, int16 *Buffer, int Count);

bool sound_active;	// true if sound is enabled and initialized


static EmuRealSyncher ers;

static int LoadGame(const char *force_module, const char *path)
{
	MDFNGI *tmp;

	CloseGame();

	pending_save_state = 0;
	pending_save_movie = 0;
	pending_snapshot = 0;

	if(loadcd)
	{
	 const char *system = loadcd;

	 if(!system)
	  system = force_module;

	 if(!(tmp = MDFNI_LoadCD(system, path)))
		return(0);
	}
	else
	{
         if(!(tmp=MDFNI_LoadGame(force_module, path)))
	  return 0;
	}
	CurGame = tmp;
	InitGameInput(tmp);

        RefreshThrottleFPS(1);

        SDL_mutexP(VTMutex);
        NeedVideoChange = -1;
        SDL_mutexV(VTMutex);

        if(SDL_ThreadID() != MainThreadID)
          while(NeedVideoChange)
	  {
           SDL_Delay(1);
	  }
	sound_active = 0;

        sc_blit_timesync = MDFN_GetSettingB("video.blit_timesync");

	if(MDFN_GetSettingB("sound"))
	 sound_active = InitSound(tmp);

        if(MDFN_GetSettingB("autosave"))
	 MDFNI_LoadState(NULL, "mcq");

        //zero 07-feb-2012 - no netplay
	//if(netconnect)
	// MDFND_NetworkConnect();

	ers.SetEmuClock(CurGame->MasterClock >> 32);

	GameThreadRun = 1;
	GameThread = SDL_CreateThread(GameLoop, NULL);

	ffnosound = MDFN_GetSettingB("ffnosound");


	if(qtrecfn)
	{
	// MDFNI_StartAVRecord() needs to be called after MDFNI_Load(Game/CD)
         if(!MDFNI_StartAVRecord(qtrecfn, GetSoundRate()))
	 {
	  free(qtrecfn);
	  qtrecfn = NULL;

	  return(0);
	 }
	}

        if(soundrecfn)
        {
 	 if(!MDFNI_StartWAVRecord(soundrecfn, GetSoundRate()))
         {
          free(soundrecfn);
          soundrecfn = NULL;

	  return(0);
         }
        }

	return 1;
}

/* Closes a game and frees memory. */
int CloseGame(void)
{
	if(!CurGame) return(0);

	GameThreadRun = 0;

	SDL_WaitThread(GameThread, NULL);

        if(qtrecfn)	// Needs to be before MDFNI_Closegame() for now
         MDFNI_StopAVRecord();

        if(soundrecfn)
         MDFNI_StopWAVRecord();

	if(MDFN_GetSettingB("autosave"))
	 MDFNI_SaveState(NULL, "mcq", NULL, NULL, NULL);

	MDFND_NetworkClose();

	MDFNI_CloseGame();

        KillGameInput();
	KillSound();

	CurGame = NULL;

	return(1);
}

static void GameThread_HandleEvents(void);
static int volatile NeedExitNow = 0;
double CurGameSpeed = 1;

void MainRequestExit(void)
{
 NeedExitNow = 1;
}

static bool InFrameAdvance = 0;
static bool NeedFrameAdvance = 0;

void DoRunNormal(void)
{
 InFrameAdvance = 0;
}

void DoFrameAdvance(void)
{
 InFrameAdvance = 1;
 NeedFrameAdvance = 1;
}

static int GameLoopPaused = 0;

void DebuggerFudge(void)
{
          LockGameMutex(0);

	  int MeowCowHowFlown = VTBackBuffer;

	  // FIXME.
	  if(!VTDisplayRects[VTBackBuffer].h)
	   VTDisplayRects[VTBackBuffer].h = 10;
          if(!VTDisplayRects[VTBackBuffer].w)
           VTDisplayRects[VTBackBuffer].w = 10;

          MDFND_Update((MDFN_Surface *)VTBuffer[VTBackBuffer], NULL, 0);
	  VTBackBuffer = MeowCowHowFlown;

	  if(sound_active)
	   WriteSoundSilence(10);
	  else
	   SDL_Delay(10);

	  LockGameMutex(1);
}

int64 Time64(void)
{
 static bool cgt_fail_warning = 0;

 #if HAVE_CLOCK_GETTIME && ( _POSIX_MONOTONIC_CLOCK > 0 || defined(CLOCK_MONOTONIC))
 struct timespec tp;

 if(clock_gettime(CLOCK_MONOTONIC, &tp) == -1)
 {
  if(!cgt_fail_warning)
   printf("clock_gettime() failed: %s\n", strerror(errno));
  cgt_fail_warning = 1;
 }
 else
  return((int64)tp.tv_sec * 1000000 + tp.tv_nsec / 1000);

 #else
   #pragma message "clock_gettime() with CLOCK_MONOTONIC not available" //zero 07-feb-2012
 #endif


 #if HAVE_GETTIMEOFDAY
 // Warning: gettimeofday() is not guaranteed to be monotonic!!
 struct timeval tv;

 if(gettimeofday(&tv, NULL) == -1)
 {
  puts("gettimeofday() error");
  return(0);
 }

 return((int64)tv.tv_sec * 1000000 + tv.tv_usec);
 #else
  #pragma message "gettimeofday() not available!!!"  //zero 07-feb-2012
 #endif

 //zero 07-feb-2012 - critical fix for msvc mednafen build to be useful
#ifdef _MSC_VER
 return SDL_GetTicks()*1000;
#endif

 // Yeaaah, this isn't going to work so well.
 return((int64)time(NULL) * 1000000);
}

int GameLoop(void *arg)
{
	while(GameThreadRun)
	{
         int16 *sound;
         int32 ssize;
         int fskip;
        
	 /* If we requested a new video mode, wait until it's set before calling the emulation code again.
	 */
	 while(NeedVideoChange)
	 {
	  if(!GameThreadRun) return(1);	// Might happen if video initialization failed
	  SDL_Delay(1);
	  }
         do
         {
	  if(InFrameAdvance && !NeedFrameAdvance)
	  {
	   SDL_Delay(10);
	  }
	 } while(InFrameAdvance && !NeedFrameAdvance);

          //zero 07-feb-2012 - no netplay
	 //if(MDFNDnetplay && !(NoWaiting & 0x2))	// TODO: Hacky, clean up.
	 // ers.SetETtoRT();

	 fskip = ers.NeedFrameSkip();

	 if(!MDFN_GetSettingB("video.frameskip"))
	  fskip = 0;

	 if(pending_snapshot || pending_save_state || pending_save_movie || NeedFrameAdvance)
	  fskip = 0;

 	 NeedFrameAdvance = 0;

         if(NoWaiting)
	  fskip = 1;

	 VTLineWidths[VTBackBuffer][0].w = ~0;

	 int ThisBackBuffer = VTBackBuffer;

	 LockGameMutex(1);
	 {
	  EmulateSpecStruct espec;
 	  memset(&espec, 0, sizeof(EmulateSpecStruct));

          espec.surface = (MDFN_Surface *)VTBuffer[VTBackBuffer];
          espec.LineWidths = (MDFN_Rect *)VTLineWidths[VTBackBuffer];
	  espec.skip = fskip;
	  espec.soundmultiplier = CurGameSpeed;
	  espec.NeedRewind = DNeedRewind;

 	  espec.SoundRate = GetSoundRate();
	  espec.SoundBuf = GetEmuModSoundBuffer(&espec.SoundBufMaxSize);
 	  espec.SoundVolume = (double)MDFN_GetSettingUI("sound.volume") / 100;

	  int64 before_time = Time64();
	  int64 after_time;

	  static double average_time = 0;

          MDFNI_Emulate(&espec);

	  after_time = Time64();

          average_time += ((after_time - before_time) - average_time) * 0.10;

          assert(espec.MasterCycles);
	  ers.AddEmuTime((espec.MasterCycles - espec.MasterCyclesALMS) / CurGameSpeed);

	  //printf("%lld %f\n", (long long)(after_time - before_time), average_time);

	  VTDisplayRects[VTBackBuffer] = espec.DisplayRect;

	  sound = espec.SoundBuf + (espec.SoundBufSizeALMS * CurGame->soundchan);
	  ssize = espec.SoundBufSize - espec.SoundBufSizeALMS;
	 }
	 LockGameMutex(0);
	 FPS_IncVirtual();
	 if(!fskip)
	  FPS_IncDrawn();

	 do
	 {
	  VTBackBuffer = ThisBackBuffer;
          MDFND_Update(fskip ? NULL : (MDFN_Surface *)VTBuffer[ThisBackBuffer], sound, ssize);
          if((InFrameAdvance && !NeedFrameAdvance) || GameLoopPaused)
	  {
           if(ssize)
	    for(int x = 0; x < CurGame->soundchan * ssize; x++)
	     sound[x] = 0;
	  }
	 } while(((InFrameAdvance && !NeedFrameAdvance) || GameLoopPaused) && GameThreadRun);
	}
	return(1);
}   

char *GetBaseDirectory(void)
{
  //zero 07-feb-2012 - not acceptable behaviour
  //respect PWD for emulation
  return ".";

 //char *ol;
 //char *ret;

 //ol = getenv("MEDNAFEN_HOME");
 //if(ol != NULL && ol[0] != 0)
 //{
 // ret = strdup(ol);
 // return(ret);
 //}

 //ol = getenv("HOME");

 //if(ol)
 //{
 // ret=(char *)malloc(strlen(ol)+1+strlen("/.mednafen"));
 // strcpy(ret,ol);
 // strcat(ret,"/.mednafen");
 // return(ret);
 //}

 //#if defined(HAVE_GETUID) && defined(HAVE_GETPWUID)
 //{
 // struct passwd *psw;

 // psw = getpwuid(getuid());

 // if(psw != NULL && psw->pw_dir[0] != 0 && strcmp(psw->pw_dir, "/dev/null"))
 // {
 //  ret = (char *)malloc(strlen(psw->pw_dir) + 1 + strlen("/.mednafen"));
 //  strcpy(ret, psw->pw_dir);
 //  strcat(ret, "/.mednafen");
 //  return(ret);
 // }
 //}
 //#endif

 //#ifdef WIN32
 //{
 // char *sa;

 // ret=(char *)malloc(MAX_PATH+1);
 // GetModuleFileName(NULL,ret,MAX_PATH+1);

 // sa=strrchr(ret,'\\');
 // if(sa)
 //  *sa = 0;
 // return(ret);
 //}
 //#endif

 //ret = (char *)malloc(1);
 //ret[0] = 0;
 //return(ret);
}

static const int gtevents_size = 2048; // Must be a power of 2.
static volatile SDL_Event gtevents[gtevents_size];
static volatile int gte_read = 0;
static volatile int gte_write = 0;

/* This function may also be called by the main thread if a game is not loaded. */
static void GameThread_HandleEvents(void)
{
 SDL_Event gtevents_temp[gtevents_size];
 unsigned int numevents = 0;

 SDL_mutexP(EVMutex);
 while(gte_read != gte_write)
 {
  memcpy(&gtevents_temp[numevents], (void *)&gtevents[gte_read], sizeof(SDL_Event));

  numevents++;
  gte_read = (gte_read + 1) & (gtevents_size - 1);
 }
 SDL_mutexV(EVMutex);

 for(unsigned int i = 0; i < numevents; i++)
 {
  SDL_Event *event = &gtevents_temp[i];

  switch(event->type)
  {
   case SDL_USEREVENT:
		switch(event->user.code)
   {
		 case CEVT_SET_INPUT_FOCUS:
			MDFNDHaveFocus = (bool)((char*)event->user.data1 - (char*)0);
			//printf("%u\n", MDFNDHaveFocus);
			break;
   }
		break;
  }


  Input_Event(event);
  //NetplayEventHook_GT(event); //zero 07-feb-2012 - no netplay
 }
 SDL_mutexV(EVMutex);
}

void PauseGameLoop(bool p)
{
 GameLoopPaused = p;
}


void SendCEvent(unsigned int code, void *data1, void *data2)
{
 SDL_Event evt;
 evt.user.type = SDL_USEREVENT;
 evt.user.code = code;
 evt.user.data1 = data1;
 evt.user.data2 = data2;
 SDL_PushEvent(&evt);
}

void SendCEvent_to_GT(unsigned int code, void *data1, void *data2)
{
 SDL_Event evt;
 evt.user.type = SDL_USEREVENT;
 evt.user.code = code;
 evt.user.data1 = data1;
 evt.user.data2 = data2;

 SDL_mutexP(EVMutex);
 memcpy((void *)&gtevents[gte_write], &evt, sizeof(SDL_Event));
 gte_write = (gte_write + 1) & (gtevents_size - 1);
 SDL_mutexV(EVMutex);
}

void SDL_MDFN_ShowCursor(int toggle)
{
 int *toog = (int *)malloc(sizeof(int));
 *toog = toggle;

 SDL_Event evt;
 evt.user.type = SDL_USEREVENT;
 evt.user.code = CEVT_SHOWCURSOR;
 evt.user.data1 = toog;
 SDL_PushEvent(&evt);

}

void GT_ToggleFS(void)
{
 SDL_mutexP(VTMutex);
 NeedVideoChange = 1;
 SDL_mutexV(VTMutex);

 if(SDL_ThreadID() != MainThreadID)
  while(NeedVideoChange)
  {
   SDL_Delay(1);
  }
}

void GT_ReinitVideo(void)
{
 SDL_mutexP(VTMutex);
 NeedVideoChange = -1;
 SDL_mutexV(VTMutex);

 if(SDL_ThreadID() != MainThreadID)
 {
  while(NeedVideoChange)
  {
   SDL_Delay(1);
  }
 }
}

static bool krepeat = 0;
void PumpWrap(void)
{
 SDL_Event event;
 SDL_Event gtevents_temp[gtevents_size];
 int numevents = 0;

 bool NITI;

  //zero 07-feb-2012 - no netplay
 //NITI = Netplay_IsTextInput();
 NITI = false;

 if(Debugger_IsActive() || NITI || IsConsoleCheatConfigActive() || Help_IsActive())
 {
  if(!krepeat)
   SDL_EnableKeyRepeat(SDL_DEFAULT_REPEAT_DELAY, SDL_DEFAULT_REPEAT_INTERVAL);
  krepeat = 1;
 }
 else
 {
  if(krepeat)
   SDL_EnableKeyRepeat(0, 0);
  krepeat = 0;
 }

 #if defined(HAVE_SIGNAL) || defined(HAVE_SIGACTION)
 if(SignalSafeExitWanted)
  NeedExitNow = true;
 #endif

 while(SDL_PollEvent(&event))
 {
  if(Debugger_IsActive())
   Debugger_Event(&event);
  else 
   if(IsConsoleCheatConfigActive())
    CheatEventHook(&event);

  //NetplayEventHook(&event); //zero 07-feb-2012 - no netplay

  /* Handle the event, and THEN hand it over to the GUI. Order is important due to global variable mayhem(CEVT_TOGGLEFS. */
  switch(event.type)
  {
   case SDL_ACTIVEEVENT:
   			if(event.active.state & SDL_APPINPUTFOCUS)
   {
			 SendCEvent_to_GT(CEVT_SET_INPUT_FOCUS, (char*)0 + (bool)event.active.gain, NULL);
   }
			break;

   case SDL_SYSWMEVENT: break;
   //case SDL_VIDEORESIZE: //if(VideoResize(event.resize.w, event.resize.h))
			 // NeedVideoChange = -1;
   //			 break;

   case SDL_VIDEOEXPOSE: break;
   case SDL_QUIT: NeedExitNow = 1;break;
   case SDL_USEREVENT:
		switch(event.user.code)
		{
		 case CEVT_SET_STATE_STATUS: MT_SetStateStatus((StateStatusStruct *)event.user.data1); break;
                 case CEVT_SET_MOVIE_STATUS: MT_SetMovieStatus((StateStatusStruct *)event.user.data1); break;
		 case CEVT_WANT_EXIT:
		     //if(!Netplay_TryTextExit())  //zero 07-feb-2012 - no netplay
		     {
		      SDL_Event evt;
		      evt.quit.type = SDL_QUIT;
		      SDL_PushEvent(&evt);
		     }
		     break;
	         case CEVT_SET_GRAB_INPUT:
                         SDL_WM_GrabInput(*(int *)event.user.data1 ? SDL_GRAB_ON : SDL_GRAB_OFF);
                         free(event.user.data1);
                         break;
		 //case CEVT_TOGGLEFS: NeedVideoChange = 1; break;
		 //case CEVT_VIDEOSYNC: NeedVideoChange = -1; break;
		 case CEVT_SHOWCURSOR: SDL_ShowCursor(*(int *)event.user.data1); free(event.user.data1); break;
	  	 case CEVT_DISP_MESSAGE: VideoShowMessage((UTF8*)event.user.data1); break;
		 default: 
			if(numevents < gtevents_size)
			{
			 memcpy(&gtevents_temp[numevents], &event, sizeof(SDL_Event));
			 numevents++;
			}
			break;
		}
		break;
   default: 
           if(numevents < gtevents_size)
           {
            memcpy(&gtevents_temp[numevents], &event, sizeof(SDL_Event));
            numevents++;
           }
	   break;
  }
 }

 SDL_mutexP(EVMutex);
 for(int i = 0; i < numevents; i++)
 {
  memcpy((void *)&gtevents[gte_write], &gtevents_temp[i], sizeof(SDL_Event));
  gte_write = (gte_write + 1) & (gtevents_size - 1);
 }
 SDL_mutexV(EVMutex);

 if(!CurGame)
  GameThread_HandleEvents();
}

bool MT_FromRemote_SoundSync(void)
{
 bool ret = TRUE;

 GameThreadRun = 0;
 SDL_WaitThread(GameThread, NULL);

 KillSound();
 sound_active = 0;

 if(MDFN_GetSettingB("sound"))
 {
  sound_active = InitSound(CurGame);
  if(!sound_active)
   ret = FALSE;
 }
 GameThreadRun = 1;
 GameThread = SDL_CreateThread(GameLoop, NULL);

 return(ret);
}

bool MT_FromRemote_VideoSync(void)
{
          KillVideo();

          //memset(VTBuffer[0], 0, CurGame->pitch * CurGame->fb_height);
          //memset(VTBuffer[1], 0, CurGame->pitch * CurGame->fb_height);

          if(!InitVideo(CurGame))
	   return(0);
	  return(1);
}

void RefreshThrottleFPS(double multiplier)
{
        CurGameSpeed = multiplier;
}

void PrintCompilerVersion(void)
{
 #if defined(__GNUC__)
  MDFN_printf(_("Compiled with gcc %s\n"), __VERSION__);
 #endif
}

void PrintSDLVersion(void)
{
 const SDL_version *sver = SDL_Linked_Version();

 MDFN_printf(_("Compiled against SDL %u.%u.%u, running with SDL %u.%u.%u\n"), SDL_MAJOR_VERSION, SDL_MINOR_VERSION, SDL_PATCHLEVEL, sver->major, sver->minor, sver->patch);
}

#ifdef HAVE_LIBSNDFILE
 #include <sndfile.h>
#endif

void PrintLIBSNDFILEVersion(void)
{
 #ifdef HAVE_LIBSNDFILE
  MDFN_printf(_("Running with %s\n"), sf_version_string());
 #endif
}

void PrintZLIBVersion(void)
{
 #ifdef ZLIB_VERSION
  MDFN_printf(_("Compiled against zlib %s, running with zlib %s\n"), ZLIB_VERSION, zlibVersion());
 #endif
}

void PrintLIBCDIOVersion(void)
{
 #ifdef HAVE_LIBCDIO
  #if LIBCDIO_VERSION_NUM < 83
  const char *cdio_version_string = "(unknown)";
  #endif

  MDFN_printf(_("Compiled against libcdio %s, running with libcdio %s\n"), CDIO_VERSION, cdio_version_string);
 #endif
}

int sdlhaveogl = 0;

//#include <sched.h>

#if 0//#ifdef WIN32
char *GetFileDialog(void)
{
 OPENFILENAME ofn;
 char returned_fn[2048];
 std::string filter;
 bool first_extension = true;

 filter = std::string("Recognized files");
 filter.push_back(0);

 for(unsigned int i = 0; i < MDFNSystems.size(); i++)
 {
  if(MDFNSystems[i]->FileExtensions)
  {
   const FileExtensionSpecStruct *fesc = MDFNSystems[i]->FileExtensions;

   while(fesc->extension && fesc->description)
   {
    if(!first_extension)
     filter.push_back(';');

    filter.push_back('*');
    filter += std::string(fesc->extension);

    first_extension = false;
    fesc++;
   }
  }
 }

 filter.push_back(0);
 filter.push_back(0);

 //fwrite(filter.data(), 1, filter.size(), stdout);

 memset(&ofn, 0, sizeof(ofn));

 ofn.lStructSize = sizeof(ofn);
 ofn.lpstrTitle = "Mednafen Open File";
 ofn.lpstrFilter = filter.data();

 ofn.nMaxFile = sizeof(returned_fn);
 ofn.lpstrFile = returned_fn;

 ofn.Flags = OFN_EXPLORER | OFN_FILEMUSTEXIST | OFN_HIDEREADONLY;

 if(GetOpenFileName(&ofn))
  return(strdup(returned_fn));
 
 return(NULL);
}
#endif


void _killprocess(){ ExitProcess(0); } //zero 07-feb-2012 - dont let sdl crash when closing the console window
int main(int argc, char *argv[])
{
  atexit(&_killprocess); //zero 07-feb-2012 - dont let sdl crash when closing the console window

	//struct sched_param sp;

	//sp.sched_priority = 25;

	//if(sched_setscheduler(getpid(), SCHED_RR, &sp))
	//{
	// printf("%m\n");
	// return(-1);
	//}

	std::vector<MDFNGI *> ExternalSystems;
	int ret;
	char *needie = NULL;

	MDFNDHaveFocus = false;

	DrBaseDirectory=GetBaseDirectory();

	MDFNI_printf(_("Starting Mednafen %s\n"), MEDNAFEN_VERSION);
	MDFN_indent(1);

        MDFN_printf(_("Build information:\n"));
        MDFN_indent(2);
        PrintCompilerVersion();
        PrintZLIBVersion();
        PrintSDLVersion();
        PrintLIBSNDFILEVersion();
	PrintLIBCDIOVersion();
        MDFN_indent(-2);

        MDFN_printf(_("Base directory: %s\n"), DrBaseDirectory);

	#ifdef ENABLE_NLS
	setlocale(LC_ALL, "");

	#ifdef WIN32
        bindtextdomain(PACKAGE, DrBaseDirectory);
	#else
	bindtextdomain(PACKAGE, LOCALEDIR);
	#endif

	bind_textdomain_codeset(PACKAGE, "UTF-8");
	textdomain(PACKAGE);
	#endif

	if(SDL_Init(SDL_INIT_VIDEO)) /* SDL_INIT_VIDEO Needed for (joystick config) event processing? */
	{
	 fprintf(stderr, "Could not initialize SDL: %s\n", SDL_GetError());
	 MDFNI_Kill();
	 return(-1);
	}
	SDL_JoystickEventState(SDL_IGNORE);

	if(!(StdoutMutex = SDL_CreateMutex()))
	{
	 MDFN_PrintError(_("Could not create mutex: %s\n"), SDL_GetError());
	 MDFNI_Kill();
	 return(-1);
	}

        MainThreadID = SDL_ThreadID();

        // Look for external emulation modules here.

	if(!MDFNI_InitializeModules(ExternalSystems))
	 return(-1);

	if(argc >= 2 && (!strcasecmp(argv[1], "-remote") || !strcasecmp(argv[1], "--remote")))
         RemoteOn = TRUE;

	if(RemoteOn)
 	 InitSTDIOInterface();

	for(unsigned int x = 0; x < sizeof(DriverSettings) / sizeof(MDFNSetting); x++)
	 NeoDriverSettings.push_back(DriverSettings[x]);

	MakeDebugSettings(NeoDriverSettings);
	MakeVideoSettings(NeoDriverSettings);
	MakeInputSettings(NeoDriverSettings);

        if(!(ret=MDFNI_Initialize(DrBaseDirectory, NeoDriverSettings)))
         return(-1);

        SDL_EnableUNICODE(1);

        #if defined(HAVE_SIGNAL) || defined(HAVE_SIGACTION)
        SetSignals(CloseStuff);
        #endif

	if(!CreateDirs())
	{
	 ErrnoHolder ene(errno);	// TODO: Maybe we should have CreateDirs() return this instead?

	 MDFN_PrintError(_("Error creating directories: %s\n"), ene.StrError());
	 MDFNI_Kill();
	 return(-1);
	}

	MakeMednafenArgsStruct();

	#if 0 //def WIN32
	if(argc > 1 || !(needie = GetFileDialog()))
	#endif
	if(!DoArgs(argc,argv, &needie))
	{
	 MDFNI_Kill();
	 DeleteInternalArgs();
	 KillInputSettings();
	 return(-1);
	}

        if(!getenv("__GL_SYNC_TO_VBLANK"))
	{
 	 if(MDFN_GetSettingB("video.glvsync"))
	 {
	  #if HAVE_PUTENV
	  static char gl_pe_string[] = "__GL_SYNC_TO_VBLANK=1";
	  putenv(gl_pe_string); 
	  #elif HAVE_SETENV
	  setenv("__GL_SYNC_TO_VBLANK", "1", 0);
	  #endif
	 }
         else
         {
	  #if HAVE_PUTENV
	  static char gl_pe_string[] = "__GL_SYNC_TO_VBLANK=0";
	  putenv(gl_pe_string); 
	  #elif HAVE_SETENV
	  setenv("__GL_SYNC_TO_VBLANK", "0", 0);
	  #endif
         }
	}

	/* Now the fun begins! */
	/* Run the video and event pumping in the main thread, and create a 
	   secondary thread to run the game in(and do sound output, since we use
	   separate sound code which should be thread safe(?)).
	*/

	//InitVideo(NULL);

	VTMutex = SDL_CreateMutex();
        EVMutex = SDL_CreateMutex();
	GameMutex = SDL_CreateMutex();

	VTReady = NULL;
	VTDRReady = NULL;
	VTLWReady = NULL;

	NeedVideoChange = -1;

	joy_manager = new JoystickManager();
	joy_manager->SetAnalogThreshold(MDFN_GetSettingF("analogthreshold") / 100);
	InitCommandInput();

	NeedExitNow = 0;

	#if 0
	{
	 long start_ticks = SDL_GetTicks();

	 for(int i = 0; i < 65536; i++)
	  MDFN_GetSettingB("gg.forcemono");

	 printf("%ld\n", SDL_GetTicks() - start_ticks);
	}
	#endif


        if(LoadGame(force_module_arg, needie))
        {
	 uint32 pitch32 = CurGame->fb_width; 
	 //uint32 pitch32 = round_up_pow2(CurGame->fb_width);
	 MDFN_PixelFormat nf(MDFN_COLORSPACE_RGB, 0, 8, 16, 24);

	 VTBuffer[0] = new MDFN_Surface(NULL, CurGame->fb_width, CurGame->fb_height, pitch32, nf);
         VTBuffer[1] = new MDFN_Surface(NULL, CurGame->fb_width, CurGame->fb_height, pitch32, nf);
         VTLineWidths[0] = (MDFN_Rect *)calloc(CurGame->fb_height, sizeof(MDFN_Rect));
         VTLineWidths[1] = (MDFN_Rect *)calloc(CurGame->fb_height, sizeof(MDFN_Rect));
         NeedVideoChange = -1;
         FPS_Init();

         #ifdef WANT_DEBUGGER
         //MemDebugger_Init(); //zero 07-feb-2012 - no memdebugger now
	 if(MDFN_GetSettingB("debugger.autostepmode"))
	 {
	  Debugger_Toggle();
	  Debugger_ForceSteppingMode();
	 }
         #endif
        }
	else
	 NeedExitNow = 1;

	while(!NeedExitNow)
	{
	 bool DidVideoChange = false;

	 if(RemoteOn)
	  CheckForSTDIOMessages();

	 SDL_mutexP(VTMutex);	/* Lock mutex */

         if(NeedVideoChange)
         {
          KillVideo();

	  for(int i = 0; i < 2; i++)
	   ((MDFN_Surface *)VTBuffer[i])->Fill(0, 0, 0, 0);

          if(NeedVideoChange == -1)
          {
           if(!InitVideo(CurGame))
           {
            NeedExitNow = 1;
            break;
           }
          }
          else
          {
           MDFNI_SetSettingB("video.fs", !MDFN_GetSettingB("video.fs"));

           if(!InitVideo(CurGame))
           {
            MDFNI_SetSettingB("video.fs", !MDFN_GetSettingB("video.fs"));
            InitVideo(CurGame);
           }
          }

	  DidVideoChange = true;
          NeedVideoChange = 0;
         }

         if(VTReady)
         {
	  //static int last_time;
	  //int curtime;

          BlitScreen(VTReady, VTDRReady, VTLWReady);

          //curtime = SDL_GetTicks();
          //printf("%d\n", curtime - last_time);
          //last_time = curtime;

          VTReady = NULL;
         }

	 PumpWrap();
	 if(DidVideoChange)	// Do it after PumpWrap() in case there are stale SDL_ActiveEvent in the SDL event queue.
	  SendCEvent_to_GT(CEVT_SET_INPUT_FOCUS, (char*)0 + (bool)(SDL_GetAppState() & SDL_APPINPUTFOCUS), NULL);

         SDL_mutexV(VTMutex);   /* Unlock mutex */
         SDL_Delay(1);
	}

	CloseGame();

	SDL_DestroyMutex(VTMutex);
        SDL_DestroyMutex(EVMutex);

	for(int x = 0; x < 2; x++)
	{
	 if(VTBuffer[x])
	 {
	  delete VTBuffer[x];
	  VTBuffer[x] = NULL;
	 }

	 if(VTLineWidths[x])
	 {
	  free(VTLineWidths[x]);
	  VTLineWidths[x] = NULL;
	 }
	}

	#if defined(HAVE_SIGNAL) || defined(HAVE_SIGACTION)
	SetSignals(SIG_IGN);
	#endif

	KillCommandInput();

        MDFNI_Kill();

	delete joy_manager;
	joy_manager = NULL;

	KillVideo();

	SDL_Quit();

	DeleteInternalArgs();
	KillInputSettings();

        return(0);
}



static uint32 last_btime = 0;
static void UpdateSoundSync(int16 *Buffer, int Count)
{
 if(Count)
 {
  if(ffnosound && CurGameSpeed != 1)
  {
   for(int x = 0; x < Count * CurGame->soundchan; x++)
    Buffer[x] = 0;
  }
  int32 max = GetWriteSound();
  if(Count > max)
  {
   if(NoWaiting)
    Count = max;
  }
  if(Count >= (max * 0.95))
  {
   ers.SetETtoRT();
  }

  WriteSound(Buffer, Count);

   //zero 07-feb-2012 - no netplay
  if(/*MDFNDnetplay && */GetWriteSound() >= Count * 1.00) // Cheap code to fix sound buffer underruns due to accumulation of timer error during netplay.
  {
   int16 zbuf[128 * 2];
   for(int x = 0; x < 128 * 2; x++) zbuf[x] = 0;
   int t = GetWriteSound();
   t /= CurGame->soundchan;
   while(t > 0) 
   {
    WriteSound(zbuf, (t > 128 ? 128 : t));
    t -= 128;
   }
   ers.SetETtoRT();
  }
 }
 else
 {
  bool nothrottle = MDFN_GetSettingB("nothrottle");

  //zero 07-feb-2012 - no netplay
  if(!NoWaiting && !nothrottle && GameThreadRun/* && !MDFNDnetplay*/)
   ers.Sync();
 }
}

void MDFND_MidSync(const EmulateSpecStruct *espec)
{
 ers.AddEmuTime((espec->MasterCycles - espec->MasterCyclesALMS) / CurGameSpeed, false);

 UpdateSoundSync(espec->SoundBuf + (espec->SoundBufSizeALMS * CurGame->soundchan), espec->SoundBufSize - espec->SoundBufSizeALMS);

 // TODO(once we can ensure it's safe): GameThread_HandleEvents();
 MDFND_UpdateInput(true, false);
}

static void PassBlit(MDFN_Surface *surface)
{
  /* If it's been >= 100ms since the last blit, assume that the blit
     thread is being time-slice starved, and let it run.  This is especially necessary
     for fast-forwarding to respond well(since keyboard updates are
     handled in the main thread) on slower systems or when using a higher fast-forwarding speed ratio.
  */
 if(surface)
 {
  if((last_btime + 100) < SDL_GetTicks())
  {
   //puts("Eep");
   while(VTReady && GameThreadRun) SDL_Delay(1);
  }

  if(!VTReady)
  {
   VTLWReady = VTLineWidths[VTBackBuffer];
   VTDRReady = &VTDisplayRects[VTBackBuffer];
   VTReady = VTBuffer[VTBackBuffer];

   VTBackBuffer ^= 1;
   last_btime = SDL_GetTicks();
   FPS_IncBlitted();
  }
 }
 else if(IsConsoleCheatConfigActive() && !VTReady)
 {
  VTBackBuffer ^= 1;
  VTLWReady = VTLineWidths[VTBackBuffer];
  VTReady = VTBuffer[VTBackBuffer];
  VTBackBuffer ^= 1;
 }
}


void MDFND_Update(MDFN_Surface *surface, int16 *Buffer, int Count)
{
 if(false == sc_blit_timesync)
 {
  //puts("ABBYNORMAL");
  PassBlit(surface);
 }

 UpdateSoundSync(Buffer, Count);

 GameThread_HandleEvents();
 MDFND_UpdateInput();

 if(surface)
 {
  if(pending_snapshot)
   MDFNI_SaveSnapshot(surface, (MDFN_Rect *)&VTDisplayRects[VTBackBuffer], (MDFN_Rect *)VTLineWidths[VTBackBuffer]);

  if(pending_save_state || pending_save_movie)
   LockGameMutex(1);

  if(pending_save_state)
   MDFNI_SaveState(NULL, NULL, surface, (MDFN_Rect *)&VTDisplayRects[VTBackBuffer], (MDFN_Rect *)VTLineWidths[VTBackBuffer]);
  if(pending_save_movie)
   MDFNI_SaveMovie(NULL, surface, (MDFN_Rect *)&VTDisplayRects[VTBackBuffer], (MDFN_Rect *)VTLineWidths[VTBackBuffer]);

  if(pending_save_state || pending_save_movie)
   LockGameMutex(0);

  pending_save_movie = pending_snapshot = pending_save_state = 0;
 }

 if(true == sc_blit_timesync)
 {
  //puts("NORMAL");
  PassBlit(surface);
 }
}

void MDFND_DispMessage(UTF8 *text)
{
 SendCEvent(CEVT_DISP_MESSAGE, text, NULL);
}

void MDFND_SetStateStatus(StateStatusStruct *status)
{
 SendCEvent(CEVT_SET_STATE_STATUS, status, NULL);
}

void MDFND_SetMovieStatus(StateStatusStruct *status)
{
 SendCEvent(CEVT_SET_MOVIE_STATUS, status, NULL);
}

uint32 MDFND_GetTime(void)
{
 return(SDL_GetTicks());
}

void MDFND_Sleep(uint32 ms)
{
 SDL_Delay(ms);
}

struct MDFN_Thread
{
 SDL_Thread *sdl_thread;
};

struct MDFN_Mutex
{
 SDL_mutex *sdl_mutex;
};

MDFN_Thread *MDFND_CreateThread(int (*fn)(void *), void *data)
{
 MDFN_Thread *thread;

 if(!(thread = (MDFN_Thread *)calloc(1, sizeof(MDFN_Thread))))
  return(NULL);

 if(!(thread->sdl_thread = SDL_CreateThread(fn, data)))
 {
  free(thread);
  return(NULL);
 }

 return(thread);
}

void MDFND_WaitThread(MDFN_Thread *thread, int *status)
{
 SDL_WaitThread(thread->sdl_thread, status);
 thread->sdl_thread = NULL;	// To make accidental use-after-free() more apparent.
 free(thread);
}

void MDFND_KillThread(MDFN_Thread *thread)
{
 SDL_KillThread(thread->sdl_thread);
 thread->sdl_thread = NULL;
 free(thread);
}

MDFN_Mutex *MDFND_CreateMutex(void)
{
 MDFN_Mutex *mutex;

 if(!(mutex = (MDFN_Mutex *)calloc(1, sizeof(MDFN_Mutex))))
  return(NULL);

 if(!(mutex->sdl_mutex = SDL_CreateMutex()))
 {
  free(mutex);
  return(NULL);
 }

 return(mutex);
}

void MDFND_DestroyMutex(MDFN_Mutex *mutex)
{
 SDL_DestroyMutex(mutex->sdl_mutex);
 free(mutex);
}

int MDFND_LockMutex(MDFN_Mutex *mutex)
{
 return SDL_mutexP(mutex->sdl_mutex);
}

int MDFND_UnlockMutex(MDFN_Mutex *mutex)
{
 return SDL_mutexV(mutex->sdl_mutex);
}


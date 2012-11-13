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
#include <math.h>
#include <string.h>
#include <trio/trio.h>

#include "video.h"
#include "opengl.h"
#include "shader.h"
#include "nongl.h"
#include "overlay.h"

#include "icon.h"
#include "netplay.h"
#include "cheat.h"

#include "scalebit.h"
#include "hqxx-common.h"
#include "nnx.h"
#include "debugger.h"
#include "fps.h"
#include "help.h"
#include "video-state.h"
#include "../video/selblur.h"

#include "2xSaI.h"

typedef struct
{
        int xres;
        int yres;
        double xscale, xscalefs;
        double yscale, yscalefs;
        int videoip;
        int stretch;
        int special;
        int scanlines;
	ShaderType pixshader;
} CommonVS;

static CommonVS _video;
static int _fullscreen;

static bool osd_alpha_blend;
static unsigned int vdriver = VDRIVER_OPENGL;

typedef struct
{
	const char *name;
	int id;
	int xscale;
	int yscale;
} ScalerDefinition;

static ScalerDefinition Scalers[] = 
{

	{"hq2x", NTVB_HQ2X, 2, 2 },
	{"hq3x", NTVB_HQ3X, 3, 3 },
	{"hq4x", NTVB_HQ4X, 4, 4 },

	{"scale2x", NTVB_SCALE2X, 2, 2 },
	{"scale3x", NTVB_SCALE3X, 3, 3 },
	{"scale4x", NTVB_SCALE4X, 4, 4 },

	{"nn2x", NTVB_NN2X, 2, 2 },
        {"nn3x", NTVB_NN3X, 3, 3 },
        {"nn4x", NTVB_NN4X, 4, 4 },

	{"nny2x", NTVB_NNY2X, 1, 2 },
	{"nny3x", NTVB_NNY3X, 1, 3 },
	{"nny4x", NTVB_NNY4X, 1, 4 },

	{"2xsai", NTVB_2XSAI, 2, 2 },
	{"super2xsai", NTVB_SUPER2XSAI, 2, 2 },
	{"supereagle", NTVB_SUPEREAGLE, 2, 2 },
	{ 0 }
};

static MDFNGI *VideoGI;

static int best_xres = 0, best_yres = 0;

static int cur_xres, cur_yres, cur_flags;

static ScalerDefinition *CurrentScaler = NULL;

static SDL_Surface *screen = NULL;
static SDL_Surface *IconSurface=NULL;

static MDFN_Rect screen_dest_rect;

static MDFN_Surface *DebuggerSurface = NULL;
static MDFN_Rect DebuggerRect;

static MDFN_Surface *NetSurface = NULL;
static MDFN_Rect NetRect;

static MDFN_Surface *CheatSurface = NULL;
static MDFN_Rect CheatRect;

static MDFN_Surface *HelpSurface = NULL;
static MDFN_Rect HelpRect;

static MDFN_Surface *SMSurface = NULL;
static MDFN_Rect SMRect;
static MDFN_Rect SMDRect;

static int curbpp;

static double exs,eys;
static int evideoip;

static int NeedClear = 0;

static MDFN_PixelFormat pf_overlay, pf_normal;

void ClearBackBuffer(void)
{
 if(cur_flags & SDL_OPENGL)
 {
  ClearBackBufferOpenGL();
 }
 else
 {
  SDL_FillRect(screen, NULL, 0);
 }
}

/* Return 1 if video was killed, 0 otherwise(video wasn't initialized). */
void KillVideo(void)
{
 if(IconSurface)
 {
  SDL_FreeSurface(IconSurface);
  IconSurface = NULL;
 }

 if(DebuggerSurface)
 {
  delete DebuggerSurface;
  DebuggerSurface = NULL;
 }

 if(SMSurface)
 {
  delete SMSurface;
  SMSurface = NULL;
 }

 if(CheatSurface)
 {
  delete CheatSurface;
  CheatSurface = NULL;
 }

 if(HelpSurface)
 {
  delete HelpSurface;
  HelpSurface = NULL;
 }

 if(NetSurface)
 {
  delete NetSurface;
  NetSurface = NULL;
 }

 if(cur_flags & SDL_OPENGL)
  KillOpenGL();

 if(vdriver == VDRIVER_OVERLAY)
  OV_Kill();

 VideoGI = NULL;
 cur_flags = 0;
}

static void GenerateDestRect(void)
{
 if(_video.stretch && _fullscreen)
 {
  int nom_width, nom_height;

  if(VideoGI->rotated)
  {
   nom_width = VideoGI->nominal_height;
   nom_height = VideoGI->nominal_width;
  }
  else
  {
   nom_width = VideoGI->nominal_width;
   nom_height = VideoGI->nominal_height;
  }

  if (_video.stretch == 2 || _video.stretch == 3 || _video.stretch == 4)	// Aspect-preserve stretch
  {
   exs = (double)cur_xres / nom_width;
   eys = (double)cur_yres / nom_height;

   if(_video.stretch == 3 || _video.stretch == 4)	// Round down to nearest int.
   {
    exs = floor(exs);
    eys = floor(eys);

    if(_video.stretch == 4)	// Round down to nearest multiple of 2.
    {
     exs = (int)exs & ~1;
     eys = (int)eys & ~1;

     if(!exs) exs = 1;
     if(!eys) eys = 1;
    }
   }

   // Check if we are constrained horizontally or vertically
   if (exs > eys)
   {
    // Too tall for screen, fill vertically
    exs = eys;
   }
   else
   {
    // Too wide for screen, fill horizontally
    eys = exs;
   }

   //printf("%f %f\n", exs, eys);

   screen_dest_rect.w = (int)(exs*nom_width + 0.5); // +0.5 for rounding
   screen_dest_rect.h = (int)(eys*nom_height + 0.5); // +0.5 for rounding

   // Centering:
   int nx = (int)((cur_xres - screen_dest_rect.w) / 2);
   if(nx < 0) nx = 0;
   screen_dest_rect.x = nx;

   int ny = (int)((cur_yres - screen_dest_rect.h) / 2);
   if(ny < 0) ny = 0;
   screen_dest_rect.y = ny;
  }
  else 	// Full-stretch
  {
   screen_dest_rect.x = 0;
   screen_dest_rect.w = cur_xres;

   screen_dest_rect.y = 0;
   screen_dest_rect.h = cur_yres;

   exs = (double)cur_xres / nom_width;
   eys = (double)cur_yres / nom_height;
  }
 }
 else
 {
  if(VideoGI->rotated)
  {
   int32 ny = (int)((cur_yres - VideoGI->nominal_width * exs) / 2);
   int32 nx = (int)((cur_xres - VideoGI->nominal_height * eys) / 2);

   //if(ny < 0) ny = 0;
   //if(nx < 0) nx = 0;

   screen_dest_rect.x = _fullscreen ? nx : 0;
   screen_dest_rect.y = _fullscreen ? ny : 0;
   screen_dest_rect.w = (Uint16)(VideoGI->nominal_height * eys);
   screen_dest_rect.h = (Uint16)(VideoGI->nominal_width * exs);
  }
  else
  {
   int nx = (int)((cur_xres - VideoGI->nominal_width * exs) / 2);
   int ny = (int)((cur_yres - VideoGI->nominal_height * eys) / 2);

   // Don't check to see if the coordinates go off screen here, offscreen coordinates are valid(though weird that the user would want them...)
   // in OpenGL mode, and are clipped to valid coordinates in SDL blit mode code.
   //if(nx < 0)
   // nx = 0;
   //if(ny < 0)
   // ny = 0;

   screen_dest_rect.x = _fullscreen ? nx : 0;
   screen_dest_rect.y = _fullscreen ? ny : 0;

   //printf("%.64f %d, %f, %d\n", exs, VideoGI->nominal_width, exs * VideoGI->nominal_width, (int)(exs * VideoGI->nominal_width));
   // FIXME, stupid floating point
   screen_dest_rect.w = (Uint16)(VideoGI->nominal_width * exs + 0.000000001);
   screen_dest_rect.h = (Uint16)(VideoGI->nominal_height * eys + 0.000000001);
  }
 }

 // Quick and dirty kludge for VB's "hli" and "vli" 3D modes.
 screen_dest_rect.x &= ~1;
 screen_dest_rect.y &= ~1;
 //printf("%d %d\n", screen_dest_rect.x & 1, screen_dest_rect.y & 1);
}

// Argh, lots of thread safety and crashy problems with this, need to re-engineer code elsewhere.
#if 0
int VideoResize(int nw, int nh)
{
 double xs, ys;
 char buf[256];

 if(VideoGI && !_fullscreen)
 {
  std::string sn = std::string(VideoGI->shortname);

  xs = (double)nw / VideoGI->nominal_width;
  ys = (double)nh / VideoGI->nominal_height;

  trio_snprintf(buf, 256, "%.30f", xs);
//  MDFNI_SetSetting(std::string(sn + "." + std::string("xscale")).c_str(), buf);

  trio_snprintf(buf, 256, "%.30f", ys);
//  MDFNI_SetSetting(std::string(sn + "." + std::string("yscale")).c_str(), buf);

  printf("%s, %d %d, %f %f\n", std::string(sn + "." + std::string("xscale")).c_str(), nw, nh, xs, ys);
  return(1);
 }

 return(0);
}
#endif

int GetSpecialScalerID(const std::string &special_string)
{
 int ret = -1;

 if(special_string == "" || !strcasecmp(special_string.c_str(), "none") || special_string == "0")
  ret = 0;
 else
 {
  ScalerDefinition *scaler = Scalers;

  while(scaler->name)
  {
   char tmpstr[16];

   sprintf(tmpstr, "%d", scaler->id);

   if(!strcasecmp(scaler->name, special_string.c_str()) || tmpstr == special_string)
   {
    ret = scaler->id;
    break;
   }
   scaler++;
  }
 }
 return(ret);
}


static uint32 real_rs, real_gs, real_bs, real_as;

int InitVideo(MDFNGI *gi)
{
 const SDL_VideoInfo *vinf;
 int flags = 0; //SDL_RESIZABLE;
 int desbpp;

 VideoGI = gi;

 MDFNI_printf(_("Initializing video...\n"));
 MDFN_indent(1);

 osd_alpha_blend = MDFN_GetSettingB("osd.alpha_blend");

 std::string sn = std::string(gi->shortname);

 if(gi->GameType == GMT_PLAYER)
  sn = "player";

 std::string special_string = MDFN_GetSettingS(std::string(sn + "." + std::string("special")).c_str());

 _fullscreen = MDFN_GetSettingB("video.fs");
 _video.xres = MDFN_GetSettingUI(std::string(sn + "." + std::string("xres")).c_str());
 _video.yres = MDFN_GetSettingUI(std::string(sn + "." + std::string("yres")).c_str());
 _video.xscale = MDFN_GetSettingF(std::string(sn + "." + std::string("xscale")).c_str());
 _video.yscale = MDFN_GetSettingF(std::string(sn + "." + std::string("yscale")).c_str());
 _video.xscalefs = MDFN_GetSettingF(std::string(sn + "." + std::string("xscalefs")).c_str());
 _video.yscalefs = MDFN_GetSettingF(std::string(sn + "." + std::string("yscalefs")).c_str());
 _video.videoip = MDFN_GetSettingI(std::string(sn + "." + std::string("videoip")).c_str());
 _video.stretch = MDFN_GetSettingUI(std::string(sn + "." + std::string("stretch")).c_str());
 _video.scanlines = MDFN_GetSettingUI(std::string(sn + "." + std::string("scanlines")).c_str());

 _video.special = GetSpecialScalerID(special_string);

 #ifdef MDFN_WANT_OPENGL_SHADERS
 _video.pixshader = (ShaderType)MDFN_GetSettingI(std::string(sn + "." + std::string("pixshader")).c_str());
 #else
 _video.pixshader = SHADER_NONE;
 #endif

 CurrentScaler = _video.special ? &Scalers[_video.special - 1] : NULL;

 vinf=SDL_GetVideoInfo();

 if(!best_xres)
 {
  best_xres = vinf->current_w;
  best_yres = vinf->current_h;

  if(!best_xres || !best_yres)
  {
   best_xres = 640;
   best_yres = 480;
  }
 }


 if(vinf->hw_available)
  flags|=SDL_HWSURFACE;

 if(_fullscreen)
  flags|=SDL_FULLSCREEN;

 vdriver = MDFN_GetSettingI("video.driver");

 if(vdriver == VDRIVER_OPENGL)
 {
  if(!sdlhaveogl)
  {
   // SDL_GL_LoadLibrary returns 0 on success, -1 on failure
   if(SDL_GL_LoadLibrary(NULL) == 0)
    sdlhaveogl = 1;
   else
    sdlhaveogl = 0;
  }

  if(sdlhaveogl)
   flags |= SDL_OPENGL;
  else
  {
   MDFN_PrintError(_("Could not load OpenGL library, disabling OpenGL usage!"));
   vdriver = 0;
  }

  SDL_GL_SetAttribute(SDL_GL_DOUBLEBUFFER, 1 );

  #if SDL_VERSION_ATLEAST(1, 2, 10)
  SDL_GL_SetAttribute(SDL_GL_SWAP_CONTROL, MDFN_GetSettingB("video.glvsync"));
  #endif
 }
 else if(vdriver == VDRIVER_SOFTSDL)
  flags |= SDL_DOUBLEBUF;
 else if(vdriver == VDRIVER_OVERLAY)
 {
  //flags |= SDL_
 }

 exs = _fullscreen ? _video.xscalefs : _video.xscale;
 eys = _fullscreen ? _video.yscalefs : _video.yscale;
 evideoip = _video.videoip;

 desbpp = 32;

 if(!_video.stretch || !_fullscreen)
 {
  if(exs > 50)
  {
   MDFND_PrintError(_("Eep!  Effective X scale setting is way too high.  Correcting."));
   exs = 50;
  }
 
  if(eys > 50)
  {
   MDFND_PrintError(_("Eep!  Effective Y scale setting is way too high.  Correcting."));
   eys = 50;
  }
 }

 GenerateDestRect();

 if(_fullscreen)
 {
  if(!screen || cur_xres != _video.xres || cur_yres != _video.yres || cur_flags != flags || curbpp != desbpp)
  {
   if(!(screen = SDL_SetVideoMode(_video.xres ? _video.xres : best_xres, _video.yres ? _video.yres : best_yres, desbpp, flags)))
   {
    MDFND_PrintError(SDL_GetError()); 
    MDFN_indent(-1);
    return(0);
   }
  }
 }
 else
 {
  if(!screen || cur_xres != screen_dest_rect.w || cur_yres != screen_dest_rect.h || cur_flags != flags || curbpp != desbpp)
  {
   if(!(screen = SDL_SetVideoMode(screen_dest_rect.w, screen_dest_rect.h, desbpp, flags)))
   {
    MDFND_PrintError(SDL_GetError());
    MDFN_indent(-1);
    return(0);
   }
  }
 }

 cur_xres = screen->w;
 cur_yres = screen->h;
 cur_flags = flags;
 curbpp = screen->format->BitsPerPixel;

 GenerateDestRect();

 MDFN_printf(_("Video Driver: %s\n"), (cur_flags & SDL_OPENGL) ? _("OpenGL") : (vdriver == VDRIVER_OVERLAY ? _("Overlay") :_("Software SDL") ) );

 MDFN_printf(_("Video Mode: %d x %d x %d bpp\n"),screen->w,screen->h,screen->format->BitsPerPixel);
 if(curbpp!=16 && curbpp!=24 && curbpp!=32)
 {
  MDFN_printf(_("Sorry, %dbpp modes are not supported by Mednafen.  Supported bit depths are 16bpp, 24bpp, and 32bpp.\n"),curbpp);
  KillVideo();
  MDFN_indent(-1);
  return(0);
 }

 //MDFN_printf(_("OpenGL: %s\n"), (cur_flags & SDL_OPENGL) ? _("Yes") : _("No"));

 if(cur_flags & SDL_OPENGL)
 {
  MDFN_indent(1);
  MDFN_printf(_("Pixel shader: %s\n"), MDFN_GetSettingS(std::string(sn + "." + std::string("pixshader")).c_str()).c_str());
  MDFN_indent(-1);
 }

 MDFN_printf(_("Fullscreen: %s\n"), _fullscreen ? _("Yes") : _("No"));
 MDFN_printf(_("Special Scaler: %s\n"), _video.special ? Scalers[_video.special - 1].name : _("None"));

 if(!_video.scanlines)
  MDFN_printf(_("Scanlines: Off\n"));
 else
  MDFN_printf(_("Scanlines: %d%% opacity\n"), _video.scanlines);

 MDFN_printf(_("Destination Rectangle: X=%d, Y=%d, W=%d, H=%d\n"), screen_dest_rect.x, screen_dest_rect.y, screen_dest_rect.w, screen_dest_rect.h);
 if(screen_dest_rect.x < 0 || screen_dest_rect.y < 0 || (screen_dest_rect.x + screen_dest_rect.w) > screen->w || (screen_dest_rect.y + screen_dest_rect.h) > screen->h)
 {
  MDFN_indent(1);
   MDFN_printf(_("Warning:  Destination rectangle exceeds screen dimensions.  This is ok if you really do want the clipping...\n"));
  MDFN_indent(-1);
 }
 if(gi && gi->name)
  SDL_WM_SetCaption((char *)gi->name,(char *)gi->name);
 else
  SDL_WM_SetCaption("Mednafen","Mednafen");

 #ifdef WIN32
  #ifdef LSB_FIRST
  IconSurface=SDL_CreateRGBSurfaceFrom((void *)mednafen_playicon.pixel_data,32,32,32,32*4,0xFF,0xFF00,0xFF0000,0xFF000000);
  #else
  IconSurface=SDL_CreateRGBSurfaceFrom((void *)mednafen_playicon.pixel_data,32,32,32,32*4,0xFF000000,0xFF0000,0xFF00,0xFF);
  #endif
 #else
  #ifdef LSB_FIRST
  IconSurface=SDL_CreateRGBSurfaceFrom((void *)mednafen_playicon128.pixel_data,128,128,32,128*4,0xFF,0xFF00,0xFF0000,0xFF000000);
  #else
  IconSurface=SDL_CreateRGBSurfaceFrom((void *)mednafen_playicon128.pixel_data,128,128,32,128*4,0xFF000000,0xFF0000,0xFF00,0xFF);
  #endif
 #endif
 SDL_WM_SetIcon(IconSurface,0);

 int rs, gs, bs, as;

 if(cur_flags & SDL_OPENGL)
 {
  if(!InitOpenGL(evideoip, _video.scanlines, _video.pixshader, screen, &rs, &gs, &bs, &as))
  {
   KillVideo();
   MDFN_indent(-1);
   return(0);
  }
 }
 else
 {
  rs = screen->format->Rshift;
  gs = screen->format->Gshift;
  bs = screen->format->Bshift;

  as = 0;
  while(as == rs || as == gs || as == bs) // Find unused 8-bits to use as our alpha channel
   as += 8;
 }

 //printf("%d %d %d %d\n", rs, gs, bs, as);

 MDFN_indent(-1);
 SDL_ShowCursor(0);

 real_rs = rs;
 real_gs = gs;
 real_bs = bs;
 real_as = as;

 /* HQXX only supports this pixel format, sadly, and other pixel formats
    can't be easily supported without rewriting the filters.
    We do conversion to the real screen format in the blitting function. 
 */
 if(CurrentScaler) {
  if(CurrentScaler->id == NTVB_HQ2X || CurrentScaler->id == NTVB_HQ3X || CurrentScaler->id == NTVB_HQ4X)
  {
   rs = 16;
   gs = 8;
   bs = 0;
   as = 24;
  }
  else if(CurrentScaler->id == NTVB_2XSAI || CurrentScaler->id == NTVB_SUPER2XSAI || CurrentScaler->id == NTVB_SUPEREAGLE)
  {
   Init_2xSaI(screen->format->BitsPerPixel, 555); // systemColorDepth, BitFormat
  }
 }

 NetSurface = new MDFN_Surface(NULL, screen->w, 18 * 5, screen->w, MDFN_PixelFormat(MDFN_COLORSPACE_RGB, real_rs, real_gs, real_bs, real_as));

 NetRect.w = screen->w;
 NetRect.h = 18 * 5;
 NetRect.x = 0;
 NetRect.y = 0;


 {
  int xmu = 1;
  int ymu = 1;

  if(screen->w >= 768)
   xmu = screen->w / 384;
  if(screen->h >= 576)
   ymu = screen->h / 288;

  SMRect.h = 18 + 2;
  SMRect.x = 0;
  SMRect.y = 0;
  SMRect.w = screen->w;

  SMDRect.w = SMRect.w * xmu;
  SMDRect.h = SMRect.h * ymu;
  SMDRect.x = (screen->w - SMDRect.w) / 2;
  SMDRect.y = screen->h - SMDRect.h;

  if(SMDRect.x < 0)
  {
   SMRect.w += SMDRect.x * 2 / xmu;
   SMDRect.w = SMRect.w * xmu;
   SMDRect.x = 0;
  }
  SMSurface = new MDFN_Surface(NULL, SMRect.w, SMRect.h, SMRect.w, MDFN_PixelFormat(MDFN_COLORSPACE_RGB, real_rs, real_gs, real_bs, real_as));
 }

 //MDFNI_SetPixelFormat(rs, gs, bs, as);
 memset(&pf_normal, 0, sizeof(pf_normal));
 memset(&pf_overlay, 0, sizeof(pf_overlay));

 pf_normal.bpp = 32;
 pf_normal.colorspace = MDFN_COLORSPACE_RGB;
 pf_normal.Rshift = rs;
 pf_normal.Gshift = gs;
 pf_normal.Bshift = bs;
 pf_normal.Ashift = as;

 if(vdriver == VDRIVER_OVERLAY)
 {
  pf_overlay.bpp = 32;
  pf_overlay.colorspace = MDFN_COLORSPACE_YCbCr;
  pf_overlay.Yshift = 0;
  pf_overlay.Ushift = 8;
  pf_overlay.Vshift = 16;
  pf_overlay.Ashift = 24;
 }

 //SetPixelFormatHax((vdriver == VDRIVER_OVERLAY) ? pf_overlay : pf_normal); //rs, gs, bs, as);

 for(int i = 0; i < 2; i++)
 {
  ClearBackBuffer();

  if(cur_flags & SDL_OPENGL)
   FlipOpenGL();
  else
   SDL_Flip(screen);
 }

 return 1;
}

static uint32 howlong = 0;
static UTF8 *CurrentMessage = NULL;

void VideoShowMessage(UTF8 *text)
{
 if(text)
  howlong = MDFND_GetTime() + 2500;
 else
  howlong = 0;

 if(CurrentMessage)
 {
  free(CurrentMessage);
  CurrentMessage = NULL;
 }

 CurrentMessage = text;
}

void BlitRaw(MDFN_Surface *src, const MDFN_Rect *src_rect, const MDFN_Rect *dest_rect, int source_alpha)
{
 if(cur_flags & SDL_OPENGL)
  BlitOpenGLRaw(src, src_rect, dest_rect, (source_alpha != 0) && osd_alpha_blend);
 else 
 {
  SDL_to_MDFN_Surface_Wrapper m_surface(screen);

  //MDFN_SrcAlphaBlitSurface(src, src_rect, &m_surface, dest_rect);
  MDFN_StretchBlitSurface(src, src_rect, &m_surface, dest_rect, (source_alpha > 0) && osd_alpha_blend);
 }

 //if((dest_rect->x < screen_dest_rect.x) || (dest_rect->y < screen_dest_rect.y) ||
 //	((dest_rect->x + dest_rect->w) > (screen_dest_rect.x + screen_dest_rect.w)) || ((dest_rect->y + dest_rect->h) > (screen_dest_rect.y + screen_dest_rect.h)) )
 {
  //puts("Need clear");
  NeedClear = 2;
 }
}

static bool IsInternalMessageActive(void)
{
 return(howlong >= MDFND_GetTime());
}

static bool BlitInternalMessage(void)
{
 if(howlong < MDFND_GetTime())
 {
  if(CurrentMessage)
  {
   free(CurrentMessage);
   CurrentMessage = NULL;
  }
  return(0);
 }

 if(CurrentMessage)
 {
  SMSurface->Fill(0x00, 0x00, 0x00, 0xC0);

  DrawTextTransShadow(SMSurface->pixels + (1 * SMSurface->pitch32), SMSurface->pitch32 << 2, SMRect.w, CurrentMessage,
	SMSurface->MakeColor(0xFF, 0xFF, 0xFF, 0xFF), SMSurface->MakeColor(0x00, 0x00, 0x00, 0xFF), TRUE);
  free(CurrentMessage);
  CurrentMessage = NULL;
 }

 BlitRaw(SMSurface, &SMRect, &SMDRect);
 return(1);
}

static bool OverlayOK;	// Set to TRUE when vdriver == "overlay", and it's safe to use an overlay format
			// "Safe" is equal to OSD being off, and not using a special scaler that
			// requires an RGB pixel format(HQnx)
			// Otherwise, set to FALSE.
			// (Set in the BlitScreen function before any calls to SubBlit())

static void SubBlit(MDFN_Surface *source_surface, const MDFN_Rect &src_rect, const MDFN_Rect &dest_rect)
{
 MDFN_Surface *eff_source_surface = source_surface;
 MDFN_Rect eff_src_rect = src_rect;
 MDFN_Surface *tmp_blur_surface = NULL;
 int overlay_softscale = 0;

 if(!(src_rect.w > 0 && src_rect.w <= 32767) || !(src_rect.h > 0 && src_rect.h <= 32767))
 {
  //fprintf(stderr, "BUG: Source rect out of range; w=%d, h=%d\n", src_rect.w, src_rect.h);
  return;
 }
//#if 0
// assert(src_rect.w > 0 && src_rect.w <= 32767);
// assert(src_rect.h > 0 && src_rect.h <= 32767);
//#endif

 assert(dest_rect.w > 0);
 assert(dest_rect.h > 0);

 // Handle selective blur first
 if(0)
 {
  SelBlurImage sb_spec;

  tmp_blur_surface = new MDFN_Surface(NULL, src_rect.w, src_rect.h, src_rect.w, source_surface->format);

  sb_spec.red_threshold = 8;
  sb_spec.green_threshold = 7;
  sb_spec.blue_threshold = 10;
  sb_spec.radius = 3;
  sb_spec.source = source_surface->pixels + eff_src_rect.x + eff_src_rect.y * source_surface->pitchinpix;
  sb_spec.source_pitch32 = source_surface->pitchinpix;
  sb_spec.dest = tmp_blur_surface->pixels;
  sb_spec.dest_pitch32 = tmp_blur_surface->pitchinpix;
  sb_spec.width = eff_src_rect.w;
  sb_spec.height = eff_src_rect.h;
  sb_spec.red_shift = source_surface->format.Rshift;
  sb_spec.green_shift = source_surface->format.Gshift;
  sb_spec.blue_shift = source_surface->format.Bshift;

  MDFN_SelBlur(&sb_spec);

  eff_source_surface = tmp_blur_surface;
  eff_src_rect.x = 0;
  eff_src_rect.y = 0;
 }

 if(OverlayOK && CurrentScaler && !CurGame->rotated)
 {
  if(CurrentScaler->id == NTVB_NN2X || CurrentScaler->id == NTVB_NN3X || CurrentScaler->id == NTVB_NN4X)
   overlay_softscale = CurrentScaler->id - NTVB_NN2X + 2;
 }

   if(CurrentScaler && !overlay_softscale)
   {
    uint8 *screen_pixies;
    uint32 screen_pitch;
    MDFN_Surface *bah_surface = NULL;
    MDFN_Rect boohoo_rect = eff_src_rect;

    boohoo_rect.x = boohoo_rect.y = 0;
    boohoo_rect.w *= CurrentScaler->xscale;
    boohoo_rect.h *= CurrentScaler->yscale;

    bah_surface = new MDFN_Surface(NULL, boohoo_rect.w, boohoo_rect.h, boohoo_rect.w, eff_source_surface->format);

    screen_pixies = (uint8 *)bah_surface->pixels;
    screen_pitch = bah_surface->pitch32 << 2;

    if(CurrentScaler->id == NTVB_SCALE4X || CurrentScaler->id == NTVB_SCALE3X || CurrentScaler->id == NTVB_SCALE2X)
    {
     // scale2x and scale3x apparently can't handle source heights less than 2.
     // scale4x, it's less than 4
     if(eff_src_rect.h < 2 || (CurrentScaler->id == NTVB_SCALE4X && eff_src_rect.h < 4))
     {
      nnx(CurrentScaler->id - NTVB_SCALE2X + 2, eff_source_surface, &eff_src_rect, bah_surface, &boohoo_rect);
    }
     else
     {
      uint8 *source_pixies = (uint8 *)eff_source_surface->pixels + eff_src_rect.x * sizeof(uint32) + eff_src_rect.y * eff_source_surface->pitchinpix * sizeof(uint32);
      scale((CurrentScaler->id ==  NTVB_SCALE2X)?2:(CurrentScaler->id == NTVB_SCALE4X)?4:3, screen_pixies, screen_pitch, source_pixies, eff_source_surface->pitchinpix * sizeof(uint32), sizeof(uint32), eff_src_rect.w, eff_src_rect.h);
     }
    }
    else if(CurrentScaler->id == NTVB_NN2X || CurrentScaler->id == NTVB_NN3X || CurrentScaler->id == NTVB_NN4X)
    {
     nnx(CurrentScaler->id - NTVB_NN2X + 2, eff_source_surface, &eff_src_rect, bah_surface, &boohoo_rect);
    }
    else if(CurrentScaler->id == NTVB_NNY2X || CurrentScaler->id == NTVB_NNY3X || CurrentScaler->id == NTVB_NNY4X)
    {
     nnyx(CurrentScaler->id - NTVB_NNY2X + 2, eff_source_surface, &eff_src_rect, bah_surface, &boohoo_rect);
    }
    else 
    {
     uint8 *source_pixies = (uint8 *)(eff_source_surface->pixels + eff_src_rect.x + eff_src_rect.y * eff_source_surface->pitchinpix);

     if(CurrentScaler->id == NTVB_HQ2X)
      hq2x_32(source_pixies, screen_pixies, eff_src_rect.w, eff_src_rect.h, eff_source_surface->pitchinpix * sizeof(uint32), screen_pitch);
     else if(CurrentScaler->id == NTVB_HQ3X)
      hq3x_32(source_pixies, screen_pixies, eff_src_rect.w, eff_src_rect.h, eff_source_surface->pitchinpix * sizeof(uint32), screen_pitch);
     else if(CurrentScaler->id == NTVB_HQ4X)
      hq4x_32(source_pixies, screen_pixies, eff_src_rect.w, eff_src_rect.h, eff_source_surface->pitchinpix * sizeof(uint32), screen_pitch);
     else if(CurrentScaler->id == NTVB_2XSAI || CurrentScaler->id == NTVB_SUPER2XSAI || CurrentScaler->id == NTVB_SUPEREAGLE)
     {
      MDFN_Surface *saisrc = NULL;

      saisrc = new MDFN_Surface(NULL, eff_src_rect.w + 4, eff_src_rect.h + 4, eff_src_rect.w + 4, eff_source_surface->format);

      for(int y = 0; y < 2; y++)
      {
       memcpy(saisrc->pixels + (y * saisrc->pitchinpix) + 2, (uint32 *)source_pixies, eff_src_rect.w * sizeof(uint32));
       memcpy(saisrc->pixels + ((2 + y + eff_src_rect.h) * saisrc->pitchinpix) + 2, (uint32 *)source_pixies + (eff_src_rect.h - 1) * eff_source_surface->pitchinpix, eff_src_rect.w * sizeof(uint32));
      }

      for(int y = 0; y < eff_src_rect.h; y++)
      {
       memcpy(saisrc->pixels + ((2 + y) * saisrc->pitchinpix) + 2, (uint32*)source_pixies + y * eff_source_surface->pitchinpix, eff_src_rect.w * sizeof(uint32));
       memcpy(saisrc->pixels + ((2 + y) * saisrc->pitchinpix) + (2 + eff_src_rect.w),
	      saisrc->pixels + ((2 + y) * saisrc->pitchinpix) + (2 + eff_src_rect.w - 1), sizeof(uint32));
      }

      {
       uint8 *saipix = (uint8 *)(saisrc->pixels + 2 * saisrc->pitchinpix + 2);
       uint32 saipitch = saisrc->pitchinpix << 2;

       if(CurrentScaler->id == NTVB_2XSAI)
        _2xSaI32(saipix, saipitch, screen_pixies, screen_pitch, eff_src_rect.w, eff_src_rect.h);
       else if(CurrentScaler->id == NTVB_SUPER2XSAI)
        Super2xSaI32(saipix, saipitch, screen_pixies, screen_pitch, eff_src_rect.w, eff_src_rect.h);
       else if(CurrentScaler->id == NTVB_SUPEREAGLE)
        SuperEagle32(saipix, saipitch, screen_pixies, screen_pitch, eff_src_rect.w, eff_src_rect.h);
      }

      delete saisrc;
     }

     if(bah_surface->format.Rshift != real_rs || bah_surface->format.Gshift != real_gs || bah_surface->format.Bshift != real_bs)
     {
      uint32 *lineptr = bah_surface->pixels;

      unsigned int srs = bah_surface->format.Rshift;
      unsigned int sgs = bah_surface->format.Gshift;
      unsigned int sbs = bah_surface->format.Bshift;
      unsigned int drs = real_rs;
      unsigned int dgs = real_gs;
      unsigned int dbs = real_bs;

      for(int y = 0; y < boohoo_rect.h; y++)
      {
       for(int x = 0; x < boohoo_rect.w; x++)
       {
        uint32 pixel = lineptr[x];
        lineptr[x] = (((pixel >> srs) & 0xFF) << drs) | (((pixel >> sgs) & 0xFF) << dgs) | (((pixel >> sbs) & 0xFF) << dbs);
       }
       lineptr += bah_surface->pitchinpix;
      }
     }
    }

    if(cur_flags & SDL_OPENGL)
     BlitOpenGL(bah_surface, &boohoo_rect, &dest_rect, &eff_src_rect);
    else
    {
     if(OverlayOK)
     {
      SDL_Rect tr;

      tr.x = dest_rect.x;
      tr.y = dest_rect.y;
      tr.w = dest_rect.w;
      tr.h = dest_rect.h;

      OV_Blit(bah_surface, &boohoo_rect, &eff_src_rect, &tr, screen, 0, _video.scanlines, CurGame->rotated);
     }
     else
     {
      SDL_to_MDFN_Surface_Wrapper m_surface(screen);

      MDFN_StretchBlitSurface(bah_surface, &boohoo_rect, &m_surface, &dest_rect, false, _video.scanlines, &eff_src_rect, CurGame->rotated);
    }
   }
    delete bah_surface;
   }
   else // No special scaler:
   {
    if(cur_flags & SDL_OPENGL)
     BlitOpenGL(eff_source_surface, &eff_src_rect, &dest_rect, &eff_src_rect);
    else
    {
     if(OverlayOK)
     {
      SDL_Rect tr;

      tr.x = dest_rect.x;
      tr.y = dest_rect.y;
      tr.w = dest_rect.w;
      tr.h = dest_rect.h;

      OV_Blit(eff_source_surface, &eff_src_rect, &eff_src_rect, &tr, screen, overlay_softscale, _video.scanlines, CurGame->rotated);
     }
     else
     {
      SDL_to_MDFN_Surface_Wrapper m_surface(screen);

      MDFN_StretchBlitSurface(eff_source_surface, &eff_src_rect, &m_surface, &dest_rect, false, _video.scanlines, &eff_src_rect, CurGame->rotated);
    }
   }
   }

 if(tmp_blur_surface)
 {
  delete tmp_blur_surface;
  tmp_blur_surface = NULL;
 } 
}

void BlitScreen(MDFN_Surface *msurface, const MDFN_Rect *DisplayRect, const MDFN_Rect *LineWidths)
{
 MDFN_Rect src_rect;
 const MDFN_PixelFormat *pf_needed = &pf_normal;

 if(!screen) return;

 if(NeedClear)
 {
  NeedClear--;
  ClearBackBuffer();
 }

 if(vdriver == VDRIVER_OVERLAY)
 {
  bool osd_active = Help_IsActive() || SaveStatesActive() || IsConsoleCheatConfigActive() || 
    //Netplay_GetTextView() || //zero 07-feb-2012 - no netplay
		   IsInternalMessageActive() || Debugger_IsActive(NULL, NULL);

  OverlayOK = (vdriver == VDRIVER_OVERLAY) && !osd_active && (!CurrentScaler || (CurrentScaler->id != NTVB_HQ2X && CurrentScaler->id != NTVB_HQ3X &&
		CurrentScaler->id != NTVB_HQ4X));

  if(OverlayOK && LineWidths[0].w != ~0)
  {
   MDFN_Rect first_rect = LineWidths[DisplayRect->y];

   for(int suby = DisplayRect->y; suby < DisplayRect->y + DisplayRect->h; suby++)
   {
    if(LineWidths[suby].w != first_rect.w || LineWidths[suby].x != first_rect.x)
    {
     puts("Skippidy");
     OverlayOK = FALSE;
     break;
    }
   }
  }

  if(OverlayOK)
   pf_needed = &pf_overlay;
 }

 msurface->SetFormat(*pf_needed, TRUE);

 src_rect.x = DisplayRect->x;
 src_rect.w = DisplayRect->w;
 src_rect.y = DisplayRect->y;
 src_rect.h = DisplayRect->h;

 if(OverlayOK)
 {
  int fps_w, fps_h;

  if(FPS_IsActive(&fps_w, &fps_h))
  {
   int fps_xpos = DisplayRect->x;
   int fps_ypos = DisplayRect->y;
   int w_bound = DisplayRect->w;
   int h_bound = DisplayRect->h;

   if(LineWidths[0].w != ~0)
   {
    fps_xpos = LineWidths[DisplayRect->y].x;
    w_bound = LineWidths[DisplayRect->y].w;
   }

   if((fps_xpos + fps_w) > w_bound || (fps_ypos + fps_h) > h_bound)
   {
    puts("FPS draw error");
   }
   else
   {
    FPS_Draw(msurface, fps_xpos, DisplayRect->y);
   }

  }
 }


 if(LineWidths[0].w == ~0) // Skip multi line widths code?
 {
  SubBlit(msurface, src_rect, screen_dest_rect);
 }
 else
 {
  int y;
  int last_y = src_rect.y;
  int last_x = LineWidths[src_rect.y].x;
  int last_width = LineWidths[src_rect.y].w;

  MDFN_Rect sub_src_rect;
  MDFN_Rect sub_dest_rect;

  for(y = src_rect.y; y < (src_rect.y + src_rect.h + 1); y++)
  {
   if(y == (src_rect.y + src_rect.h) || LineWidths[y].x != last_x || LineWidths[y].w != last_width)
   {
    sub_src_rect.x = last_x;
    sub_src_rect.w = last_width;
    sub_src_rect.y = last_y;
    sub_src_rect.h = y - last_y;

    sub_dest_rect.x = screen_dest_rect.x;
    sub_dest_rect.w = screen_dest_rect.w;

    sub_dest_rect.y = screen_dest_rect.y + (last_y - src_rect.y) * screen_dest_rect.h / src_rect.h;
    sub_dest_rect.h = sub_src_rect.h * screen_dest_rect.h / src_rect.h;

    if(!sub_dest_rect.h) // May occur with small yscale values in certain cases, so prevent triggering an assert()
     sub_dest_rect.h = 1;

    // Blit here!
    SubBlit(msurface, sub_src_rect, sub_dest_rect);

    last_y = y;

    if(y != (src_rect.y + src_rect.h))
    {
     last_width = LineWidths[y].w;
     last_x =  LineWidths[y].x;
    }

   }
  }
 }

 unsigned int debw, debh;

 if(Debugger_IsActive(&debw, &debh))
 {
  if(!DebuggerSurface)
  {
   DebuggerSurface = new MDFN_Surface(NULL, 640, 480, 640, MDFN_PixelFormat(MDFN_COLORSPACE_RGB, real_rs, real_gs, real_bs, real_as));
  }
  DebuggerRect.w = debw;
  DebuggerRect.h = debh;
  DebuggerRect.x = 0;
  DebuggerRect.y = 0;

  MDFN_Rect zederect;

  int xm = screen->w / DebuggerRect.w;
  int ym = screen->h / DebuggerRect.h;

  if(xm < 1) xm = 1;
  if(ym < 1) ym = 1;

  //if(xm > ym) xm = ym;
  //if(ym > xm) ym = xm;

  // Allow it to be compacted horizontally, but don't stretch it out, as it's hard(IMHO) to read.
  if(xm > ym) xm = ym;
  if(ym > (2 * xm)) ym = 2 * xm;

  zederect.w = DebuggerRect.w * xm;
  zederect.h = DebuggerRect.h * ym;

  zederect.x = (screen->w - zederect.w) / 2;
  zederect.y = (screen->h - zederect.h) / 2;

  Debugger_Draw(DebuggerSurface, &DebuggerRect, &zederect);

  BlitRaw(DebuggerSurface, &DebuggerRect, &zederect);
 }
#if 0
 if(CKGUI_IsActive())
 {
  if(!CKGUISurface)
  {
   CKGUIRect.w = screen->w;
   CKGUIRect.h = screen->h;

   CKGUISurface = SDL_CreateRGBSurface(SDL_SWSURFACE | SDL_SRCALPHA, CKGUIRect.w, CKGUIRect.h, 32, 0xFF << real_rs, 0xFF << real_gs, 0xFF << real_bs, 0xFF << real_as);
   SDL_SetColorKey(CKGUISurface, SDL_SRCCOLORKEY, 0);
   SDL_SetAlpha(CKGUISurface, SDL_SRCALPHA, 0);
  }
  MDFN_Rect zederect = CKGUIRect;
  CKGUI_Draw(CKGUISurface, &CKGUIRect);
  BlitRaw(CKGUISurface, &CKGUIRect, &zederect);
 }
 else if(CKGUISurface)
 {
  SDL_FreeSurface(CKGUISurface);
  CKGUISurface = NULL;
 }
#endif

 if(Help_IsActive())
 {
  if(!HelpSurface)
  {
   HelpRect.w = std::min<int>(512, screen->w);
   HelpRect.h = std::min<int>(384, screen->h);

   HelpSurface = new MDFN_Surface(NULL, 512, 384, 512, MDFN_PixelFormat(MDFN_COLORSPACE_RGB, real_rs, real_gs, real_bs, real_as));
/*
   HelpSurface = SDL_CreateRGBSurface(SDL_SWSURFACE | SDL_SRCALPHA, 512, 384, 32, 0xFF << real_rs, 0xFF << real_gs, 0xFF << real_bs, 0xFF << real_as);
   SDL_SetColorKey(HelpSurface, SDL_SRCCOLORKEY, 0);
   SDL_SetAlpha(HelpSurface, SDL_SRCALPHA, 0);
*/
   Help_Draw(HelpSurface, &HelpRect);
  }

  MDFN_Rect zederect;

  zederect.w = HelpRect.w * (screen->w / HelpRect.w);
  zederect.h = HelpRect.h * (screen->h / HelpRect.h);

  zederect.x = (screen->w - zederect.w) / 2;
  zederect.y = (screen->h - zederect.h) / 2;

  BlitRaw(HelpSurface, &HelpRect, &zederect, 0);
 }
 else if(HelpSurface)
 {
  delete HelpSurface;
  HelpSurface = NULL;
 }

 DrawSaveStates(screen, exs, eys, real_rs, real_gs, real_bs, real_as);

 if(IsConsoleCheatConfigActive())
 {
  if(!CheatSurface)
  {
   CheatRect.w = screen->w;
   CheatRect.h = screen->h;

   CheatSurface = new MDFN_Surface(NULL, CheatRect.w, CheatRect.h, CheatRect.w, MDFN_PixelFormat(MDFN_COLORSPACE_RGB, real_rs, real_gs, real_bs, real_as));
  }
  MDFN_Rect zederect;

  zederect.x = CheatRect.x;
  zederect.y = CheatRect.y;
  zederect.w = CheatRect.w;
  zederect.h = CheatRect.h;

  DrawCheatConsole(CheatSurface, &CheatRect);
  BlitRaw(CheatSurface, &CheatRect, &zederect);
 }
 else if(CheatSurface)
 {
  delete CheatSurface;
  CheatSurface = NULL;
 }

 //zero 07-feb-2012 - no netplay
 //if(Netplay_GetTextView())
 //{
 //if(Netplay_GetTextView())
 //{
 // DrawNetplayTextBuffer(NetSurface, &NetRect);

  {
   MDFN_Rect zederect;

 //  zederect.x = 0;
 //  zederect.y = screen->h - NetRect.h;
 //  zederect.w = NetRect.w;
 //  zederect.h = NetRect.h;

 //  BlitRaw(NetSurface, &NetRect, &zederect);
 // }
 // if(SDL_MUSTLOCK(NetSurface))
 //  SDL_UnlockSurface(NetSurface);
 //}
 //  BlitRaw(NetSurface, &NetRect, &zederect);
 // }
 }

 BlitInternalMessage();

 if(!OverlayOK)
  FPS_DrawToScreen(screen, real_rs, real_gs, real_bs, real_as);

 if(!(cur_flags & SDL_OPENGL))
 {
  if(!OverlayOK)
   SDL_Flip(screen);
 }
 else
  FlipOpenGL();
}

void PtoV(const int in_x, const int in_y, int32 *out_x, int32 *out_y)
{
 assert(VideoGI);
 if(VideoGI->rotated)
 {
  double tmp_x, tmp_y;

  // Swap X and Y
  tmp_x = ((double)(in_y - screen_dest_rect.y) / eys);
  tmp_y = ((double)(in_x - screen_dest_rect.x) / exs);

  // Correct position(and movement)
  if(VideoGI->rotated == MDFN_ROTATE90)
   tmp_x = VideoGI->nominal_width - 1 - tmp_x;
  else if(VideoGI->rotated == MDFN_ROTATE270)
   tmp_y = VideoGI->nominal_height - 1 - tmp_y;

  *out_x = (int32)round(65536 * tmp_x);
  *out_y = (int32)round(65536 * tmp_y);
 }
 else
 {
  *out_x = (int32)round(65536 * (double)(in_x - screen_dest_rect.x) / exs);
  *out_y = (int32)round(65536 * (double)(in_y - screen_dest_rect.y) / eys);
 }
}

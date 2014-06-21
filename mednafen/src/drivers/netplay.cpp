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

// FIXME: Minor memory leaks may occur on errors(strdup'd, malloc'd, and new'd memory used in inter-thread messages)

#ifdef HAVE_CONFIG_H
#include <config.h>
#endif
#include "main.h"
#include <stdarg.h>

#include <string.h>
#include <math.h>
#include "netplay.h"
#include "console.h"
#include "../md5.h"
#include "../general.h"

#include <trio/trio.h>

#include "NetClient.h"

#ifdef HAVE_POSIX_SOCKETS
#include "NetClient_POSIX.h"
#endif

#ifdef WIN32
#include "NetClient_WS2.h"
#endif

static NetClient *Connection = NULL;


class NetplayConsole : public MDFNConsole
{
        public:
        NetplayConsole(void);

        private:
        virtual bool TextHook(UTF8 *text);
};

static NetplayConsole NetConsole;

// All command functions are called in the main(video blitting) thread.
typedef struct
{
 const char *name;
 bool (*func)(const UTF8 *arg);
 const char *help_args;
 const char *help_desc;
} CommandEntry;

static bool CC_server(const UTF8 *arg);
static bool CC_quit(const UTF8 *arg);
static bool CC_help(const UTF8 *arg);
static bool CC_nick(const UTF8 *arg);
static bool CC_ping(const UTF8 *arg);
static bool CC_integrity(const UTF8 *arg);
static bool CC_gamekey(const UTF8 *arg);
static bool CC_swap(const UTF8 *arg);
static bool CC_dupe(const UTF8 *arg);
static bool CC_drop(const UTF8 *arg);
static bool CC_take(const UTF8 *arg);
static bool CC_list(const UTF8 *arg);

static CommandEntry ConsoleCommands[]   =
{
 { "/server", CC_server,	"[REMOTE_HOST] [PORT]", "Connects to REMOTE_HOST(IP address or FQDN), on PORT." },

 { "/connect", CC_server,	NULL, NULL },

 //{ "/gamekey", CC_gamekey,	"GAMEKEY", "Changes the game key to the specified GAMEKEY." },

 { "/quit", CC_quit,		"[MESSAGE]", "Disconnects from the netplay server."  },

 { "/help", CC_help,		"", "Help, I'm drowning in a sea of cliche metaphors!" },

 { "/nick", CC_nick,		"NICKNAME", "Changes your nickname to the specified NICKNAME." },

 { "/swap", CC_swap,		"A B", "Swap/Exchange all instances of controllers A and B(numbered from 1)." },

 { "/dupe", CC_dupe,            "[A] [...]", "Duplicate and take instances of specified controller(s)." },
 { "/drop", CC_drop,            "[A] [...]", "Drop all instances of specified controller(s)." },
 { "/take", CC_take,            "[A] [...]", "Take all instances of specified controller(s)." },

 //{ "/list", CC_list,		"", "List players in game." },

 { "/ping", CC_ping,		"", "Pings the server." },

 //{ "/integrity", CC_integrity,	"", "Starts netplay integrity check sequence." },

 { NULL, NULL },
};

static const int PopupTime = 3750;
static const int PopupFadeStartTime = 3250;

static int volatile inputable = 0;
static int volatile viewable = 0;
static int64 LastTextTime = -1;

int MDFNDnetplay = 0;  // Only write/read this global variable in the game thread.

static bool CC_server(const UTF8 *arg)
{
 UTF8 server[300];
 unsigned int *port;

 server[0] = 0;

 port = (unsigned int *)malloc(sizeof(unsigned int));

 switch(trio_sscanf((char*)arg, "%.299s %u", (char*)server, port))
 {
  default:
  case 0:
        free(port);
        port = NULL;
	SendCEvent(CEVT_NP_CONNECT, NULL, NULL);
	break;

  case 1:
	free(port);
	port = NULL;
	SendCEvent(CEVT_NP_CONNECT, strdup((char*)server), NULL);
	break;

  case 2:
	SendCEvent(CEVT_NP_CONNECT, strdup((char*)server), port);
	break;
 }

 return(false);
}

static bool CC_gamekey(const UTF8 *arg)
{
// SendCEvent(CEVT_NP_SETGAMEKEY, strdup(arg), NULL);
 return(FALSE);
}

static bool CC_quit(const UTF8 *arg)
{
 SendCEvent(CEVT_NP_DISCONNECT, strdup((const char *)arg), NULL);
 return(FALSE);
}

static bool CC_list(const UTF8 *arg)
{
 SendCEvent(CEVT_NP_LIST, NULL, NULL);
 return(false);
}

static bool CC_swap(const UTF8 *arg)
{
 int a = 0, b = 0;

 sscanf((const char *)arg, "%u %u", &a, &b);

 if(a && b)
 {
  uint32 *sc = new uint32;

  *sc = ((a - 1) & 0xFF) | (((b - 1) & 0xFF) << 8);

  SendCEvent(CEVT_NP_SWAP, sc, NULL);
 }

 return(false);
}

static bool CC_dupe(const UTF8 *arg)
{
 int tmp[32];
 int count;


 memset(tmp, 0, sizeof(tmp));
 count = sscanf((const char *)arg, "%u %u %u %u %u %u %u %u %u %u %u %u %u %u %u %u %u %u %u %u %u %u %u %u %u %u %u %u %u %u %u %u",
			&tmp[0x00], &tmp[0x01], &tmp[0x02], &tmp[0x03], &tmp[0x04], &tmp[0x05], &tmp[0x06], &tmp[0x07],
			&tmp[0x08], &tmp[0x09], &tmp[0x0A], &tmp[0x0B], &tmp[0x0C], &tmp[0x0D], &tmp[0x0E], &tmp[0x0F],
                        &tmp[0x00], &tmp[0x01], &tmp[0x02], &tmp[0x03], &tmp[0x04], &tmp[0x05], &tmp[0x06], &tmp[0x07],
                        &tmp[0x08], &tmp[0x09], &tmp[0x0A], &tmp[0x0B], &tmp[0x0C], &tmp[0x0D], &tmp[0x0E], &tmp[0x0F]);

 if(count > 0)
 {
  uint32 *mask = new uint32;

  *mask = 0;
  for(int i = 0; i < 32; i++)
  {
   if(tmp[i] > 0)
    *mask |= 1U << (unsigned)(tmp[i] - 1);
  }
  SendCEvent(CEVT_NP_DUPE, mask, NULL);
 }

 return(false);
}

static bool CC_drop(const UTF8 *arg)
{
 int tmp[32];
 int count;


 memset(tmp, 0, sizeof(tmp));
 count = sscanf((const char *)arg, "%u %u %u %u %u %u %u %u %u %u %u %u %u %u %u %u %u %u %u %u %u %u %u %u %u %u %u %u %u %u %u %u",
                        &tmp[0x00], &tmp[0x01], &tmp[0x02], &tmp[0x03], &tmp[0x04], &tmp[0x05], &tmp[0x06], &tmp[0x07],
                        &tmp[0x08], &tmp[0x09], &tmp[0x0A], &tmp[0x0B], &tmp[0x0C], &tmp[0x0D], &tmp[0x0E], &tmp[0x0F],
                        &tmp[0x00], &tmp[0x01], &tmp[0x02], &tmp[0x03], &tmp[0x04], &tmp[0x05], &tmp[0x06], &tmp[0x07],
                        &tmp[0x08], &tmp[0x09], &tmp[0x0A], &tmp[0x0B], &tmp[0x0C], &tmp[0x0D], &tmp[0x0E], &tmp[0x0F]);

 if(count > 0)
 {
  uint32 *mask = new uint32;

  *mask = 0;
  for(int i = 0; i < 32; i++)
  {
   if(tmp[i] > 0)
    *mask |= 1U << (unsigned)(tmp[i] - 1);
  }
  SendCEvent(CEVT_NP_DROP, mask, NULL);
 }

 return(false);
}

static bool CC_take(const UTF8 *arg)
{
 int tmp[32];
 int count;


 memset(tmp, 0, sizeof(tmp));
 count = sscanf((const char *)arg, "%u %u %u %u %u %u %u %u %u %u %u %u %u %u %u %u %u %u %u %u %u %u %u %u %u %u %u %u %u %u %u %u",
                        &tmp[0x00], &tmp[0x01], &tmp[0x02], &tmp[0x03], &tmp[0x04], &tmp[0x05], &tmp[0x06], &tmp[0x07],
                        &tmp[0x08], &tmp[0x09], &tmp[0x0A], &tmp[0x0B], &tmp[0x0C], &tmp[0x0D], &tmp[0x0E], &tmp[0x0F],
                        &tmp[0x00], &tmp[0x01], &tmp[0x02], &tmp[0x03], &tmp[0x04], &tmp[0x05], &tmp[0x06], &tmp[0x07],
                        &tmp[0x08], &tmp[0x09], &tmp[0x0A], &tmp[0x0B], &tmp[0x0C], &tmp[0x0D], &tmp[0x0E], &tmp[0x0F]);

 if(count > 0)
 {
  uint32 *mask = new uint32;

  *mask = 0;
  for(int i = 0; i < 32; i++)
  {
   if(tmp[i] > 0)
    *mask |= 1U << (unsigned)(tmp[i] - 1);
  }
  SendCEvent(CEVT_NP_TAKE, mask, NULL);
 }

 return(false);
}

static bool CC_ping(const UTF8 *arg)
{
 SendCEvent(CEVT_NP_PING, NULL, NULL);
 return(false);
}

static bool CC_integrity(const UTF8 *arg)
{
 SendCEvent(CEVT_NP_INTEGRITY, NULL, NULL);
 return(FALSE);
}

static bool CC_help(const UTF8 *arg)
{
 for(unsigned int x = 0; ConsoleCommands[x].name; x++)
 {
  if(ConsoleCommands[x].help_desc)
  {
   char help_buf[256];
   trio_snprintf(help_buf, 256, "%s %s  -  %s", ConsoleCommands[x].name, ConsoleCommands[x].help_args, ConsoleCommands[x].help_desc);
   NetConsole.WriteLine((UTF8*)help_buf);
  }
 }
 return(true);
}

static bool CC_nick(const UTF8 *arg)
{
 SendCEvent(CEVT_NP_SETNICK, strdup((char *)arg), NULL);
 return(true);
}


NetplayConsole::NetplayConsole(void)
{
 //SetSmallFont(1);
}

// Called from main thread
bool NetplayConsole::TextHook(UTF8 *text)
{
	 inputable = viewable = false;
	 LastTextTime = -1;

         for(unsigned int x = 0; ConsoleCommands[x].name; x++)
	 {
          if(!strncasecmp(ConsoleCommands[x].name, (char*)text, strlen(ConsoleCommands[x].name)) && text[strlen(ConsoleCommands[x].name)] <= 0x20)
          {
	   MDFN_trim((char*)&text[strlen(ConsoleCommands[x].name)]);
           inputable = viewable = ConsoleCommands[x].func(&text[strlen(ConsoleCommands[x].name)]);

           free(text);
	   text = NULL;
           break;
          }
	 }

         if(text)
         {
          if(text[0] != 0)	// Is non-empty line?
	  {
	   SendCEvent(CEVT_NP_TEXT_TO_SERVER, text, NULL);
	   text = NULL;
	   viewable = true;
	  }
	  else
	  {
	   free(text);
	   text = NULL;
	  }
         }

	 if(viewable)
          LastTextTime = SDL_GetTicks();

         return(1);
}

// Call from game thread
static void PrintNetStatus(const char *s)
{
 MDFND_NetplayText((uint8 *)s, FALSE);
}

// Call from game thread
static void PrintNetError(const char *format, ...)
{
 char *temp;

 va_list ap;

 va_start(ap, format);

 temp = trio_vaprintf(format, ap);
 MDFND_NetplayText((uint8 *)temp, FALSE);
 free(temp);

 va_end(ap);
}

// Called from game thread
int MDFND_NetworkConnect(void)
{
 std::string nickname = MDFN_GetSettingS("netplay.nick");
 std::string remote_host = MDFN_GetSettingS("netplay.host");
 unsigned int remote_port = MDFN_GetSettingUI("netplay.port");
 std::string game_key = MDFN_GetSettingS("netplay.gamekey");

 if(Connection)
 {
  MDFND_NetworkClose();
 }

 try
 {
  #ifdef HAVE_POSIX_SOCKETS
  Connection = new NetClient_POSIX();
  #elif defined(WIN32)
  Connection = new NetClient_WS2();
  #else
  throw MDFN_Error(0, _("Networking system API support not compiled in."));
  #endif
  Connection->Connect(remote_host.c_str(), remote_port);
 }
 catch(std::exception &e)
 {
  PrintNetError("%s", e.what());
  return(0);
 }

 PrintNetStatus(_("*** Sending initialization data to server."));

 MDFNDnetplay = 1;
 if(!MDFNI_NetplayStart(MDFN_GetSettingUI("netplay.localplayers"), nickname, game_key, MDFN_GetSettingS("netplay.password")))
 {
  MDFNDnetplay = 0;
  return(0);
 }
 PrintNetStatus(_("*** Connection established."));

 return(1);
}

// Called from game thread
void MDFND_SendData(const void *data, uint32 len)
{
 do
 {
  int32 sent = Connection->Send(data, len);
  assert(sent >= 0);

  data = (uint8*)data + sent;
  len -= sent;

  if(len)
  {
   // TODO: Possibility to break out?
   Connection->CanSend(50000);
  }
 } while(len);
}

void MDFND_RecvData(void *data, uint32 len)
{
 NoWaiting &= ~2;

 do
 {
  int32 received = Connection->Receive(data, len);
  assert(received >= 0);

  data = (uint8*)data + received;
  len -= received;

  if(len)
  {
   // TODO: Possibility to break out?
   Connection->CanReceive(50000);
  }
 } while(len);

 if(Connection->CanReceive())
  NoWaiting |= 2;
}

// Called from the game thread
void MDFND_NetworkClose(void)
{
 NoWaiting &= ~2;

 if(Connection)
 {
  delete Connection;
  Connection = NULL;
 }

 if(MDFNDnetplay)
 {
  MDFNI_NetplayStop();
  MDFNDnetplay = 0;
  PrintNetStatus(_("*** Disconnected"));
 }
}

// Called from the game thread
void MDFND_NetplayText(const uint8 *text, bool NetEcho)
{
 uint8 *tot = (uint8 *)strdup((char *)text);
 uint8 *tmp;

 tmp = tot;

 while(*tmp)
 {
  if((uint8)*tmp < 0x20) *tmp = ' ';
  tmp++;
 }

 SendCEvent(CEVT_NP_DISPLAY_TEXT, tot, (void*)NetEcho);
}

// Called from the game thread
void Netplay_ToggleTextView(void)
{
 SendCEvent(CEVT_NP_TOGGLE_TT, NULL, NULL);
}

// Called from main thread
int Netplay_GetTextView(void)
{
 return(viewable);
}

// Called from main thread and game thread
bool Netplay_IsTextInput(void)
{
 return(inputable);
}

// Called from the main thread
bool Netplay_TryTextExit(void)
{
 if(viewable || inputable)
 {
  viewable = FALSE;
  inputable = FALSE;
  LastTextTime = -1;
  return(TRUE);
 }
 else if(LastTextTime > 0 && (int64)SDL_GetTicks() < (LastTextTime + PopupTime + 500)) // Allow some extra time if a user tries to escape away an auto popup box but misses
 {
  return(TRUE);
 }
 else
 {
  return(FALSE);
 }
}

// Called from main thread
void DrawNetplayTextBuffer(MDFN_Surface *surface, const MDFN_Rect *src_rect)
{
 if(!viewable) 
 {
  return;
 }
 if(!inputable)
 {
  if((int64)SDL_GetTicks() >= (LastTextTime + PopupTime))
  {
   viewable = 0;
   return;
  }
 }
 NetConsole.ShowPrompt(inputable);
 NetConsole.Draw(surface, src_rect);
}

// Called from main thread
int NetplayEventHook(const SDL_Event *event)
{
 if(event->type == SDL_USEREVENT)
  switch(event->user.code)
  {
   case CEVT_NP_TOGGLE_TT:
	NetConsole.SetSmallFont(MDFN_GetSettingB("netplay.smallfont"));	// FIXME: Setting manager mutex needed example!
	if(viewable && !inputable)
	{
	 inputable = TRUE;
	}
	else
	{
	 viewable = !viewable;
	 inputable = viewable;
	}
	break;

   case CEVT_NP_DISPLAY_TEXT:
	NetConsole.WriteLine((UTF8*)event->user.data1);
	free(event->user.data1);

	if(!(bool)event->user.data2)
	{
	 viewable = 1;
	 LastTextTime = SDL_GetTicks();
	}
	break;
  }

 if(!inputable)
  return(1);

 return(NetConsole.Event(event));
}

// Called from game thread
int NetplayEventHook_GT(const SDL_Event *event)
{
 if(event->type == SDL_USEREVENT)
  switch(event->user.code)
  {
   case CEVT_NP_TEXT_TO_SERVER:
	if(MDFNDnetplay)
	{
	 MDFNI_NetplayText((const UTF8*)event->user.data1);
         free(event->user.data1);
	}
	else
	 PrintNetError(_("*** Not connected!"));
	break;

   case CEVT_NP_INTEGRITY:
	if(MDFNDnetplay)
	 MDFNI_NetplayIntegrity();
        else
         PrintNetError(_("*** Not connected!"));
	break;

   case CEVT_NP_SWAP:
        if(MDFNDnetplay)
        {
         uint32 sc = *(uint32 *)event->user.data1;

         MDFNI_NetplaySwap((sc >> 0) & 0xFF, (sc >> 8) & 0xFF);
        }
        else
         PrintNetError(_("*** Not connected!"));

        delete (uint32*)event->user.data1;
        break;

   case CEVT_NP_DUPE:
        if(MDFNDnetplay)
        {
         uint32 mask = *(uint32 *)event->user.data1;

         MDFNI_NetplayDupe(mask);
        }
        else
         PrintNetError(_("*** Not connected!"));

        delete (uint32*)event->user.data1;
        break;

   case CEVT_NP_DROP:
        if(MDFNDnetplay)
        {
         uint32 mask = *(uint32 *)event->user.data1;

         MDFNI_NetplayDrop(mask);
        }
        else
         PrintNetError(_("*** Not connected!"));

        delete (uint32*)event->user.data1;
        break;

   case CEVT_NP_TAKE:
        if(MDFNDnetplay)
        {
         uint32 mask = *(uint32 *)event->user.data1;

         MDFNI_NetplayTake(mask);
        }
        else
         PrintNetError(_("*** Not connected!"));

        delete (uint32*)event->user.data1;
        break;

   case CEVT_NP_PING:
	if(MDFNDnetplay)
	 MDFNI_NetplayPing();
	else
         PrintNetError(_("*** Not connected!"));
	break;

   case CEVT_NP_LIST:
        if(MDFNDnetplay)
         MDFNI_NetplayList();
        else
         PrintNetError(_("*** Not connected!"));
        break;


   case CEVT_NP_SETNICK:
	MDFNI_SetSetting("netplay.nick", (char*)event->user.data1);

	if(MDFNDnetplay)
	 MDFNI_NetplayChangeNick((UTF8*)event->user.data1);

	free(event->user.data1);
	break;

   case CEVT_NP_CONNECT:
	if(event->user.data1) // Connect!
	{
	 MDFNI_SetSetting("netplay.host", (char*)event->user.data1);
	 if(event->user.data2)
	 {
	  MDFNI_SetSettingUI("netplay.port", *(unsigned int *)event->user.data2);
	  free(event->user.data2);
	 }
	 free(event->user.data1);
	}
        MDFND_NetworkConnect();
	break;

   case CEVT_NP_DISCONNECT:
	if(MDFNDnetplay)
	{
	 MDFNI_NetplayQuit((const char *)event->user.data1);
	 MDFND_NetworkClose();
         free(event->user.data1);
	}
	else
	 PrintNetError(_("*** Not connected!"));
	break;
  }

 return(1);
}



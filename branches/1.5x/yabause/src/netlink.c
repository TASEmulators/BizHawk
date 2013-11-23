/*  Copyright 2006 Theo Berkau

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

#ifdef USESOCKET
#ifdef __MINGW32__
// I blame mingw for this
#define _WIN32_WINNT 0x501
#endif
#endif

#include <ctype.h>
#ifdef USESOCKET
#ifdef WIN32
#include <winsock2.h>
#include <ws2tcpip.h>
#else
#include <unistd.h>
#include <sys/types.h>
#include <sys/socket.h>
#include <netdb.h>
#include <netinet/in.h>
#include <arpa/inet.h>
#endif
#endif
#include "cs2.h"
#include "error.h"
#include "netlink.h"
#include "debug.h"
#include "scu.h"

Netlink *NetlinkArea = NULL;

#define NL_RESULTCODE_OK                0
#define NL_RESULTCODE_CONNECT           1
#define NL_RESULTCODE_RING              2
#define NL_RESULTCODE_NOCARRIER         3
#define NL_RESULTCODE_ERROR             4
#define NL_RESULTCODE_CONNECT1200       5
#define NL_RESULTCODE_NODIALTONE        6
#define NL_RESULTCODE_BUSY              7
#define NL_RESULTCODE_NOANSWER          8

#define CONNECTTYPE_SERVER      0
#define CONNECTTYPE_CLIENT      1

#define CONNECTSTATUS_IDLE      0
#define CONNECTSTATUS_WAIT      1
#define CONNECTSTATUS_CONNECT   2
#define CONNECTSTATUS_CONNECTED 3

#define MODEMSTATE_COMMAND      0
#define MODEMSTATE_ONLINE       1

#ifdef USESOCKET
static int NetworkInit(void);
static void NetworkDeInit(void);
static int NetworkConnect(const char *ip, const char *port);
static int NetworkWaitForConnect(const char *port);
static int NetworkSend(const void *buffer, int length);
static int NetworkReceive(void *buffer, int maxlength);

#ifndef WIN32
#define closesocket close
#endif
#endif

//////////////////////////////////////////////////////////////////////////////

UNUSED static void NetlinkLSRChange(u8 val)
{
   // If IER bit 2 is set and if any of the error or alarms bits are set(and
   // they weren't previously), trigger an interrupt
   if ((NetlinkArea->reg.IER & 0x4) && ((NetlinkArea->reg.LSR ^ val) & val & 0x1E))
   {
      NetlinkArea->reg.IIR = (NetlinkArea->reg.IIR & 0xF0) | 0x6;
      ScuSendExternalInterrupt12();
   }

   NetlinkArea->reg.LSR = val;
}

//////////////////////////////////////////////////////////////////////////////

#ifndef USESOCKET
UNUSED
#endif
static void NetlinkMSRChange(u8 set, u8 clear)
{
   u8 change;

   change = ((NetlinkArea->reg.MSR >> 4) ^ set) & set;
   change |= (((NetlinkArea->reg.MSR >> 4) ^ 0xFF) ^ clear) & clear;

   // If IER bit 3 is set and CTS/DSR/RI/RLSD changes, trigger interrupt
   if ((NetlinkArea->reg.IER & 0x8) && change)
   {
      NetlinkArea->reg.IIR = NetlinkArea->reg.IIR & 0xF0;
      ScuSendExternalInterrupt12();
   }

   NetlinkArea->reg.MSR &= ~(clear << 4);
   NetlinkArea->reg.MSR |= (set << 4) | change;
}

//////////////////////////////////////////////////////////////////////////////

u8 FASTCALL NetlinkReadByte(u32 addr)
{
   u8 ret;

   switch (addr)
   {
      case 0x95001: // Receiver Buffer/Divisor Latch Low Byte
      {
         if (NetlinkArea->reg.LCR & 0x80) // Divisor Latch Low Byte
            return NetlinkArea->reg.DLL;
         else // Receiver Buffer
         {
            if (NetlinkArea->outbuffersize == 0)
               return 0x00;

            ret = NetlinkArea->outbuffer[NetlinkArea->outbufferstart];
            NetlinkArea->outbufferstart++;
            NetlinkArea->outbuffersize--;

            // If the buffer is empty now, make sure the data available
            // bit in LSR is cleared
            if (NetlinkArea->outbuffersize == 0)
            {
               NetlinkArea->outbufferstart = NetlinkArea->outbufferend = 0;
               NetlinkArea->reg.LSR &= ~0x01;
            }

            // If interrupt has been triggered because of RBR having data, reset it
            if ((NetlinkArea->reg.IER & 0x1) && (NetlinkArea->reg.IIR & 0xF) == 0x4)
               NetlinkArea->reg.IIR = (NetlinkArea->reg.IIR & 0xF0) | 0x1;

            return ret;
         }

         return 0;
      }
      case 0x95009: // Interrupt Identification Register
      {
         // If interrupt has been triggered because THB is empty, reset it
         if ((NetlinkArea->reg.IER & 0x2) && (NetlinkArea->reg.IIR & 0xF) == 0x2)
            NetlinkArea->reg.IIR = (NetlinkArea->reg.IIR & 0xF0) | 0x1;
         return NetlinkArea->reg.IIR;
      }
      case 0x9500D: // Line Control Register
      {
         return NetlinkArea->reg.LCR;
      }
      case 0x95011: // Modem Control Register
      {
         return NetlinkArea->reg.MCR;
      }
      case 0x95015: // Line Status Register
      {
         return NetlinkArea->reg.LSR;
      }
      case 0x95019: // Modem Status Register
      {
         // If interrupt has been triggered because of MSR change, reset it
         if ((NetlinkArea->reg.IER & 0x8) && (NetlinkArea->reg.IIR & 0xF) == 0)
            NetlinkArea->reg.IIR = (NetlinkArea->reg.IIR & 0xF0) | 0x1;
         ret = NetlinkArea->reg.MSR;
         NetlinkArea->reg.MSR &= 0xF0;
         return ret;
      }
      case 0x9501D: // Scratch
      {
         return NetlinkArea->reg.SCR;
      }
      default:
         break;
   }

   LOG("Unimplemented Netlink byte read: %08X\n", addr);
   return 0xFF;
}

//////////////////////////////////////////////////////////////////////////////

static void FASTCALL NetlinkDoATResponse(const char *string)
{
   strcpy((char *)&NetlinkArea->outbuffer[NetlinkArea->outbufferend], string);
   NetlinkArea->outbufferend += (u32)strlen(string);
   NetlinkArea->outbuffersize += (u32)strlen(string);
}

//////////////////////////////////////////////////////////////////////////////

static int FASTCALL NetlinkFetchATParameter(u8 val, u32 *offset)
{
   if (val >= '0' && val <= '9')
   {
      (*offset)++;
      return (val - 0x30);
   }
   else
      return 0;
}

//////////////////////////////////////////////////////////////////////////////

void FASTCALL NetlinkWriteByte(u32 addr, u8 val)
{
   switch (addr)
   {
      case 0x2503D: // ???
      {
         return;
      }
      case 0x95001: // Transmitter Holding Buffer/Divisor Latch Low Byte
      {
         if (NetlinkArea->reg.LCR & 0x80) // Divisor Latch Low Byte
         {
            NetlinkArea->reg.DLL = val;
         }
         else // Transmitter Holding Buffer
         {
            NetlinkArea->inbuffer[NetlinkArea->inbufferend] = val;
            NetlinkArea->inbufferend++;
            NetlinkArea->inbuffersize++;

            // If interrupt has been triggered because THB is empty, reset it
            if ((NetlinkArea->reg.IER & 0x2) && (NetlinkArea->reg.IIR & 0xF) == 0x2)
               NetlinkArea->reg.IIR = (NetlinkArea->reg.IIR & 0xF0) | 0x1;

            if (NetlinkArea->modemstate == MODEMSTATE_COMMAND)
            {

               if (val == 0x0D &&
                   (strncmp((char *)&NetlinkArea->inbuffer[NetlinkArea->inbufferstart], "AT", 2) == 0 ||
                    strncmp((char *)&NetlinkArea->inbuffer[NetlinkArea->inbufferstart], "at", 2) == 0)) // fix me
               {
                  u32 i=NetlinkArea->inbufferstart+2;
                  int resultcode=NL_RESULTCODE_OK;
                  int parameter;

                  LOG("Program issued %s\n", NetlinkArea->inbuffer);

                  // If echo is enabled, do it
                  if (NetlinkArea->isechoenab)
                     NetlinkDoATResponse((char *)NetlinkArea->inbuffer);

                  // Handle AT command
                  while(NetlinkArea->inbuffer[i] != 0xD)
                  {
                     switch (toupper(NetlinkArea->inbuffer[i]))
                     {
                        case '%':
                           break;
                        case '&':
                           // Figure out second part of command
                           i++;
   
                           switch (toupper(NetlinkArea->inbuffer[i]))
                           {
                              case 'C':
                                 // Data Carrier Detect Options
                                 NetlinkFetchATParameter(NetlinkArea->inbuffer[i+1], &i);
                                 break;
                              case 'D':
                                 // Data Terminal Ready Options
                                 NetlinkFetchATParameter(NetlinkArea->inbuffer[i+1], &i);
                                 break;
                              case 'F':
                                 // Factory reset
                                 NetlinkFetchATParameter(NetlinkArea->inbuffer[i+1], &i);
                                 break;
                              case 'K':
                                 // Local Flow Control Options
                                 NetlinkFetchATParameter(NetlinkArea->inbuffer[i+1], &i);
                                 break;
                              case 'Q':
                                 // Communications Mode Options
                                 NetlinkFetchATParameter(NetlinkArea->inbuffer[i+1], &i);
                                 break;
                              case 'S':
                                 // Data Set Ready Options
                                 NetlinkFetchATParameter(NetlinkArea->inbuffer[i+1], &i);
                                 break;
                              default: break;
                           }
                           break;
                        case ')':
                        case '*':
                        case ':':
                        case '?':
                        case '@':
                        case '\\':
                           break;
                        case 'A':
                           // Answer Command(no other commands should follow)
                           break;
                        case 'D':
                           // Dial Command
                           NetlinkArea->connectstatus = CONNECTSTATUS_CONNECT;
   
                           i = NetlinkArea->inbufferend-1; // fix me
                           break;
                        case 'E':
                           // Command State Character Echo Selection

                           parameter = NetlinkFetchATParameter(NetlinkArea->inbuffer[i+1], &i);

                           // Parameter can only be 0 or 1
                           if (parameter < 2)
                              NetlinkArea->isechoenab = parameter;
                           else
                              resultcode = NL_RESULTCODE_ERROR;

                           break;
                        case 'I':
                           // Internal Memory Tests
                           switch(NetlinkFetchATParameter(NetlinkArea->inbuffer[i+1], &i))
                           {
                              case 0:
                                 NetlinkDoATResponse("\r\n28800\r\n");
                                 break;
                              default: break;
                           }
                           break;
                        case 'L':
                           // Speaker Volume Level Selection
                           NetlinkFetchATParameter(NetlinkArea->inbuffer[i+1], &i);
                           break;
                        case 'M':
                           // Speaker On/Off Selection
                           NetlinkFetchATParameter(NetlinkArea->inbuffer[i+1], &i);
                           break;
                        case 'V':
                           // Result Code Format Options
                           NetlinkFetchATParameter(NetlinkArea->inbuffer[i+1], &i);
                           break;
                        case 'W':
                           // Negotiation Progress Message Selection
                           NetlinkFetchATParameter(NetlinkArea->inbuffer[i+1], &i);
                           break;
                        default: break;
                     }

                     i++;
                  }

                  switch (resultcode)
                  {
                     case NL_RESULTCODE_OK: // OK
                        NetlinkDoATResponse("\r\nOK\r\n");
                        break;
                     case NL_RESULTCODE_CONNECT: // CONNECT
                        NetlinkDoATResponse("\r\nCONNECT\r\n");
                        break;
                     case NL_RESULTCODE_RING: // RING
                        NetlinkDoATResponse("\r\nRING\r\n");
                        break;
                     case NL_RESULTCODE_NOCARRIER: // NO CARRIER
                        NetlinkDoATResponse("\r\nNO CARRIER\r\n");
                        break;
                     case NL_RESULTCODE_ERROR: // ERROR
                        NetlinkDoATResponse("\r\nERROR\r\n");
                        break;
                     case NL_RESULTCODE_CONNECT1200: // CONNECT 1200
                        NetlinkDoATResponse("\r\nCONNECT 1200\r\n");
                        break;
                     case NL_RESULTCODE_NODIALTONE: // NO DIALTONE
                        NetlinkDoATResponse("\r\nNO DIALTONE\r\n");
                        break;
                     case NL_RESULTCODE_BUSY: // BUSY
                        NetlinkDoATResponse("\r\nBUSY\r\n");
                        break;
                     case NL_RESULTCODE_NOANSWER: // NO ANSWER
                        NetlinkDoATResponse("\r\nNO ANSWER\r\n");
                        break;
                     default: break;
                  }

                  memset(NetlinkArea->inbuffer, 0, NetlinkArea->inbuffersize);
                  NetlinkArea->inbufferstart = NetlinkArea->inbufferend = NetlinkArea->inbuffersize = 0;

                  if (NetlinkArea->outbuffersize > 0)
                  {
                     // Set Data available bit in LSR
                     NetlinkArea->reg.LSR |= 0x01;
   
                     // Trigger Interrrupt
                     NetlinkArea->reg.IIR = 0x4;
                     ScuSendExternalInterrupt12();
                  }
               }
            }
         }

         return;
      }
      case 0x95005: // Interrupt Enable Register/Divisor Latch High Byte
      {
         if (NetlinkArea->reg.LCR & 0x80) // Divisor Latch High Byte
         {
            NetlinkArea->reg.DLM = val;
         }
         else // Interrupt Enable Register
         {
            NetlinkArea->reg.IER = val;
         }

         return;
      }
      case 0x95009: // FIFO Control Register
      {
         NetlinkArea->reg.FCR = val;

         if (val & 0x1)
            // set FIFO enabled bits
            NetlinkArea->reg.IIR |= 0xC0;
         else
            // clear FIFO enabled bits
            NetlinkArea->reg.IIR &= ~0xC0;

         return;
      }
      case 0x9500D: // Line Control Register
      {
         NetlinkArea->reg.LCR = val;
         return;
      }
      case 0x95011: // Modem Control Register
      {
         NetlinkArea->reg.MCR = val;
         return;
      }
      case 0x95019: // Modem Status Register(read-only)
         return;
      case 0x9501D: // Scratch
      {
         NetlinkArea->reg.SCR = val;
         return;
      }
      default:
         break;
   }

   LOG("Unimplemented Netlink byte write: %08X\n", addr);
}

//////////////////////////////////////////////////////////////////////////////

int NetlinkInit(const char *setting)
{  
   if ((NetlinkArea = malloc(sizeof(Netlink))) == NULL)
   {
      Cs2Area->carttype = CART_NONE;
      YabSetError(YAB_ERR_CANNOTINIT, (void *)"Netlink");
      return 0;
   }

   memset(NetlinkArea->inbuffer, 0, NETLINK_BUFFER_SIZE);
   memset(NetlinkArea->outbuffer, 0, NETLINK_BUFFER_SIZE);

   NetlinkArea->inbufferstart = NetlinkArea->inbufferend = NetlinkArea->inbuffersize = 0;
   NetlinkArea->outbufferstart = NetlinkArea->outbufferend = NetlinkArea->outbuffersize = 0;

   NetlinkArea->isechoenab = 1;
   NetlinkArea->cycles = 0;
   NetlinkArea->modemstate = MODEMSTATE_COMMAND;

   NetlinkArea->reg.RBR = 0x00;
   NetlinkArea->reg.IER = 0x00;
   NetlinkArea->reg.DLL = 0x00;
   NetlinkArea->reg.DLM = 0x00;
   NetlinkArea->reg.IIR = 0x01;
//      NetlinkArea->reg.FCR = 0x??; // have no idea
   NetlinkArea->reg.LCR = 0x00;
   NetlinkArea->reg.MCR = 0x00;
   NetlinkArea->reg.LSR = 0x60;
   NetlinkArea->reg.MSR = 0x30;
   NetlinkArea->reg.SCR = 0x01;

   if (setting == NULL || strcmp(setting, "") == 0)
   {
      // Use Loopback ip and port 1337
      sprintf(NetlinkArea->ipstring, "127.0.0.1");
      sprintf(NetlinkArea->portstring, "1337");
   }
   else
   {
      char *p;
      p = strchr(setting, '\n');
      if (p == NULL)
      {
         strcpy(NetlinkArea->ipstring, setting);
         sprintf(NetlinkArea->portstring, "1337");
      }
      else
      {
         memcpy(NetlinkArea->ipstring, setting, (int)(p - setting));
         NetlinkArea->ipstring[(p - setting)] = '\0';
         if (strlen(p+1) == 0)
            sprintf(NetlinkArea->portstring, "1337");
         else
            strcpy(NetlinkArea->portstring, p+1);
      }
   }

#ifdef USESOCKET
   return NetworkInit();
#else
   return 0;
#endif
}

//////////////////////////////////////////////////////////////////////////////

void NetlinkDeInit(void)
{
#ifdef USESOCKET
   NetworkDeInit();
#endif

   if (NetlinkArea)
      free(NetlinkArea);
}

//////////////////////////////////////////////////////////////////////////////

void NetlinkExec(u32 timing)
{
   NetlinkArea->cycles += timing;

   if (NetlinkArea->cycles >= 20000)
   {
      NetlinkArea->cycles -= 20000;

      switch(NetlinkArea->connectstatus)
      {
         case CONNECTSTATUS_IDLE:
         {
#ifdef USESOCKET
            if (NetworkWaitForConnect(NetlinkArea->portstring) == 0)
            {
               NetlinkArea->connectstatus = CONNECTSTATUS_CONNECTED;
               NetlinkArea->modemstate = MODEMSTATE_ONLINE;

               // This is probably wrong, but let's give it a try anyways
               NetlinkDoATResponse("\r\nRING\r\n\r\nCONNECT\r\n");
               NetlinkMSRChange(0x08, 0x00);

               // Set Data available bit in LSR
               NetlinkArea->reg.LSR |= 0x01;

               // Trigger Interrrupt
               NetlinkArea->reg.IIR = 0x4;
               ScuSendExternalInterrupt12();
               LOG("Connected via idle\n");
            }
#endif
            break;
         }
         case CONNECTSTATUS_CONNECT:
         {
#ifdef USESOCKET
            if (NetworkConnect(NetlinkArea->ipstring, NetlinkArea->portstring) == 0)
            {
               NetlinkArea->connectstatus = CONNECTSTATUS_CONNECTED;
               NetlinkArea->modemstate = MODEMSTATE_ONLINE;

               NetlinkDoATResponse("\r\nCONNECT\r\n");
               NetlinkMSRChange(0x08, 0x00);

               // Set Data available bit in LSR
               NetlinkArea->reg.LSR |= 0x01;

               // Trigger Interrrupt
               NetlinkArea->reg.IIR = 0x4;
               ScuSendExternalInterrupt12();
               LOG("Connected via connect\n");
            }
#endif
            break;
         }
         case CONNECTSTATUS_CONNECTED:
         {
#ifdef USESOCKET
            int bytes;
            fd_set read_fds;
            fd_set write_fds;
            struct timeval tv;

            FD_ZERO(&read_fds);
            FD_ZERO(&write_fds);

            // Let's see if we can even connect at this point
            FD_SET(NetlinkArea->connectsocket, &read_fds);
            FD_SET(NetlinkArea->connectsocket, &write_fds);
            tv.tv_sec = 0;
            tv.tv_usec = 0;

            if (select(NetlinkArea->connectsocket+1, &read_fds, &write_fds, NULL, &tv) < 1)
            {
               LOG("select failed\n");
               return;
            }

            if (NetlinkArea->modemstate == MODEMSTATE_ONLINE && NetlinkArea->inbuffersize > 0 && FD_ISSET(NetlinkArea->connectsocket, &write_fds))
            {
               LOG("Sending to external source...");

               // Send via network connection
               if ((bytes = NetworkSend(&NetlinkArea->inbuffer[NetlinkArea->inbufferstart], NetlinkArea->inbufferend-NetlinkArea->inbufferstart)) >= 0)
               {
                  LOG("Successfully sent %d byte(s)\n", bytes);
                  if (NetlinkArea->inbufferend > bytes)
                  {
                     NetlinkArea->inbufferstart += bytes;
                     NetlinkArea->inbuffersize -= bytes;
                  }
                  else
                     NetlinkArea->inbufferstart = NetlinkArea->inbufferend = NetlinkArea->inbuffersize = 0;
               }
               else
               {
                  LOG("failed.\n");
               }
            }

            if (FD_ISSET(NetlinkArea->connectsocket, &read_fds))
            {
//               if ((bytes = NetworkReceive(&NetlinkArea->outbuffer[NetlinkArea->outbufferend], NETLINK_BUFFER_SIZE-NetlinkArea->outbufferend)) > 0)
               if ((bytes = NetworkReceive(&NetlinkArea->outbuffer[NetlinkArea->outbufferend], 8)) > 0)
               {
                  NetlinkArea->outbufferend += bytes;
                  NetlinkArea->outbuffersize += bytes;

                  NetlinkMSRChange(0x08, 0x00);

                  // Set Data available bit in LSR
                  NetlinkArea->reg.LSR |= 0x01;

                  // Trigger Interrrupt
                  NetlinkArea->reg.IIR = 0x4;
                  ScuSendExternalInterrupt12();
                  LOG("Received %d byte(s) from external source\n", bytes);
               }
            }
#endif
            break;
         }
         default: break;
      }
   }
}

//////////////////////////////////////////////////////////////////////////////
#ifdef USESOCKET

static int NetworkInit(void)
{
#ifdef WIN32
   WSADATA wsaData;

   if (WSAStartup(MAKEWORD(2,2), &wsaData) != 0)
      return -1;
#endif

   NetlinkArea->connectsocket = -1;
   NetlinkArea->connectstatus = CONNECTSTATUS_IDLE;

   return 0;
}

//////////////////////////////////////////////////////////////////////////////

static int NetworkConnect(const char *ip, const char *port)
{
   struct addrinfo *result = NULL,
                   hints;

   memset(&hints, 0, sizeof(hints));

   hints.ai_family = AF_UNSPEC;
   hints.ai_socktype = SOCK_STREAM;
   hints.ai_protocol = IPPROTO_TCP;

   if (getaddrinfo(ip, port, &hints, &result) != 0)
      return -1;

   // Create a Socket
   if ((NetlinkArea->connectsocket = socket(result->ai_family, result->ai_socktype,
                                            result->ai_protocol)) == -1)
   {
      freeaddrinfo(result);
      return -1;
   }

   // Connect to the socket
   if (connect(NetlinkArea->connectsocket, result->ai_addr, (int)result->ai_addrlen) == -1)
   {
      freeaddrinfo(result);
      closesocket(NetlinkArea->connectsocket);
      NetlinkArea->connectsocket = -1;
      return -1;
   }

   freeaddrinfo(result);

   return 0;
}

//////////////////////////////////////////////////////////////////////////////

static int NetworkWaitForConnect(const char *port)
{
   struct addrinfo *result = NULL,
                   hints;
   int ListenSocket = -1;
   fd_set read_fds;
   struct timeval tv;

   memset(&hints, 0, sizeof(hints));

   hints.ai_family = AF_INET;
   hints.ai_socktype = SOCK_STREAM;
   hints.ai_protocol = IPPROTO_TCP;
   hints.ai_flags = AI_PASSIVE;

   if (getaddrinfo(NULL, port, &hints, &result) != 0)
      return -1;

   // Create a socket that the client can connect to
   if ((ListenSocket = socket(result->ai_family, result->ai_socktype,
                              result->ai_protocol)) == -1)
   {
      freeaddrinfo(result);
      return -1;
   }

   // Setup the listening socket
   if (bind(ListenSocket, result->ai_addr, (int)result->ai_addrlen) == -1)
   {
      freeaddrinfo(result);
      closesocket(ListenSocket);
      return -1;
   }

   freeaddrinfo(result);

   // Shhh... Let's listen
   if (listen(ListenSocket, SOMAXCONN) == -1)
   {
      closesocket(ListenSocket);
      return -1;
   }

   FD_ZERO(&read_fds);

   // Let's see if we can even connect at this point
   FD_SET(ListenSocket, &read_fds);
   tv.tv_sec = 0;
   tv.tv_usec = 0;

   if (select(ListenSocket+1, &read_fds, NULL, NULL, &tv) < 1)
   {
      closesocket(ListenSocket);
      return -1;
   }

   if (FD_ISSET(ListenSocket, &read_fds))
   {
      // Good, time to connect
      if ((NetlinkArea->connectsocket = accept(ListenSocket, NULL, NULL)) == -1)
      {
         closesocket(ListenSocket);
         return -1;
      }

      // We don't need the listen socket anymore
      closesocket(ListenSocket);
   }

   return 0;
}

//////////////////////////////////////////////////////////////////////////////

static int NetworkSend(const void *buffer, int length)
{
   int bytessent;

   if ((bytessent = send(NetlinkArea->connectsocket, buffer, length, 0)) == -1)
   {
      // Fix me, better error handling is needed
//      closesocket(NetlinkArea->connectsocket);
//      NetlinkArea->connectsocket = -1;
      return -1;
   }

   return bytessent;
}

//////////////////////////////////////////////////////////////////////////////

static int NetworkReceive(void *buffer, int maxlength)
{
   int bytesreceived;

   bytesreceived = recv(NetlinkArea->connectsocket, buffer, maxlength, 0);

   if (bytesreceived == 0)
   {
      // Fix me, better handling is needed
      LOG("Connection closed\n");
      return -1;
   }
   else if (bytesreceived < 0)
   {
      // Fix me, better error handling is needed
      return -1;
   }

   return bytesreceived;
}

//////////////////////////////////////////////////////////////////////////////

static void NetworkDeInit(void)
{
   if (NetlinkArea->connectsocket != -1)
      closesocket(NetlinkArea->connectsocket);
#ifdef WIN32
   WSACleanup();
#endif
}

//////////////////////////////////////////////////////////////////////////////

#endif

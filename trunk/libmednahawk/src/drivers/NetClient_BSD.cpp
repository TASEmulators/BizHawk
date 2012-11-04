// UNFINISHED

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

#include "NetClient_BSD.h"

#include <sys/types.h>
#include <sys/socket.h>

NetClient_BSD::NetClient_BSD()
{
 fd = -1;
}

~NetClient_BSD::NetClient_BSD()
{

}

void NetClient_BSD::Connect(const char *host, unsigned int port)
{
 struct sockaddr addr;

 fd = socket(AF_INET, SOCK_STREAM, 0);
 if(fd == -1)
 {
  ErrnoHolder ene(errno);

  throw(MDFN_Error(ene.Errno(), ene.StrError()));
 }

 if(connect(fd, &addr, sizeof(struct sockaddr)) == -1)
 {
  ErrnoHolder ene(errno);

  throw(MDFN_Error(ene.Errno(), ene.StrError()));
 }
}

void Disconnect(void)
{


}

bool NetClient_BSD::IsConnected(void)
{
 if(fd == -1)
  return(false);


 return(true);
}

uint32 NetClient_BSD::Send(const void *data, uint32 len, uint32 timeout = 0)
{


}

uint32 NetClient_BSD::Receive(void *data, uint32 len, uint32 timeout = 0)
{


}


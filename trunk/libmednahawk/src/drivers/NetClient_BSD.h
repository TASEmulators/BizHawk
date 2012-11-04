#ifndef __MDFN_DRIVERS_NETCLIENT_BSD_H
#define __MDFN_DRIVERS_NETCLIENT_BSD_H

#include "NetClient.h"

class NetClient_BSD : public NetClient
{
 public:

 NetClient_BSD();   //const char *host);
 virtual ~NetClient_BSD();

 virtual void Connect(const char *host, unsigned int port);

 virtual void Disconnect(void);

 virtual bool IsConnected(void);

 virtual uint32 Send(const void *data, uint32 len, uint32 timeout = 0);

 virtual uint32 Receive(void *data, uint32 len, uint32 timeout = 0);

 private:

 int fd;
}

#endif

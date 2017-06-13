/*

    This file is part of Emu-Pizza

    Emu-Pizza is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    Emu-Pizza is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with Emu-Pizza.  If not, see <http://www.gnu.org/licenses/>.

*/

#include <arpa/inet.h>
#include <errno.h>
#include <pthread.h>
#include <stdlib.h>
#include <string.h>
#include <sys/socket.h>
#include <unistd.h>

#include "cycles.h"
#include "global.h"
#include "network.h"
#include "serial.h"
#include "utils.h"


/* network special binary semaphore */
/* typedef struct network_sem_s {
    pthread_mutex_t mutex;
    pthread_cond_t cvar;
    int v;
} network_sem_t; */

/* network sockets */
int network_sock_broad = -1;
int network_sock_bound = -1;

/* peer addr */
struct sockaddr_in network_peer_addr;

/* uuid to identify myself */
unsigned int network_uuid;

/* uuid to identify peer */
unsigned int network_peer_uuid;

/* progressive number (debug purposes) */
uint8_t network_prog_recv = 0;
uint8_t network_prog_sent = 0;

/* track that network is running */
unsigned char network_running = 0;

/* broadcast address */
char network_broadcast_addr[16];

/* network thread */
pthread_t network_thread;

/* semaphorone */
// network_sem_t network_sem;

/* function to call when connected to another Pizza Boy */
network_cb_t network_connected_cb;
network_cb_t network_disconnected_cb;

/* timeout to declare peer disconnected */
uint8_t network_timeout = 10;

uint8_t prot = 0, pret = 0;

/* prototypes */
void  network_send_data(uint8_t v, uint8_t clock, uint8_t transfer_start);
void *network_start_thread(void *args);

/* is network running? */
char network_is_running()
{
    return network_running;
}

/* start network thread */
void network_start(network_cb_t connected_cb, network_cb_t disconnected_cb,
                   char *broadcast_addr)
{
    /* init semaphore */
    // network_sem_init(&network_sem);

    /* reset bool */
    network_running = 0;

    /* set callback */
    network_connected_cb    = connected_cb;
    network_disconnected_cb = disconnected_cb;

    /* save broadcast addr */
    strncpy(network_broadcast_addr, broadcast_addr, 16);

    /* start thread! */
    pthread_create(&network_thread, NULL, network_start_thread, NULL);    
}

/* stop network thread */
void network_stop()
{
    /* already stopped? */
    if (network_running == 0)
        return;
        
    /* tell thread to stop */
    network_running = 0;    
    
    /* wait for it to exit */
    pthread_join(network_thread, NULL);
}

void *network_start_thread(void *args)
{
    utils_log("Starting network thread\n");

    /* open socket sending broadcast messages */
    network_sock_broad = socket(AF_INET, SOCK_DGRAM, 0);
    
    /* exit on error */
    if (network_sock_broad < 1)
    {
        utils_log("Error opening broadcast socket");
        return NULL;
    }
        
    /* open socket sending/receiving serial cable data */
    network_sock_bound = socket(AF_INET, SOCK_DGRAM, 0);

    /* exit on error */
    if (network_sock_bound < 1)
    {
        utils_log("Error opening serial-link socket");
        close (network_sock_broad);
        return NULL;
    }
    
    /* enable to broadcast */
    int enable=1;
    setsockopt(network_sock_broad, SOL_SOCKET, SO_BROADCAST, 
               &enable, sizeof(enable));

    /* prepare dest stuff */
    struct sockaddr_in broadcast_addr;    
    struct sockaddr_in bound_addr;    
    struct sockaddr_in addr_from;    
    socklen_t addr_from_len = sizeof(addr_from);

    memset(&broadcast_addr, 0, sizeof(broadcast_addr));  
    broadcast_addr.sin_family = AF_INET;                
//  broadcast_addr.sin_addr.s_addr = INADDR_BROADCAST;
//    inet_aton("239.255.0.37",
//              (struct in_addr *) &broadcast_addr.sin_addr.s_addr);
//    inet_aton("192.168.100.255", 
    inet_aton(network_broadcast_addr, 
              (struct in_addr *) &broadcast_addr.sin_addr.s_addr);
    broadcast_addr.sin_port = htons(64333);             

    /* setup listening socket */
    memset(&bound_addr, 0, sizeof(bound_addr));   
    bound_addr.sin_family = AF_INET;                 
    bound_addr.sin_addr.s_addr = INADDR_ANY;   
    bound_addr.sin_port = htons(64333);                
   
    /* bind to selected port */
    if (bind(network_sock_bound, (struct sockaddr *) &bound_addr, 
             sizeof(bound_addr)))
    {
        utils_log("Error binding to port 64333");

        /* close sockets and exit */
        close(network_sock_broad);
        close(network_sock_bound);
        
        return NULL;
    }

    /* assign it to our multicast group */
/*    struct ip_mreq mreq;
    mreq.imr_multiaddr.s_addr=inet_addr("239.255.0.37");
    mreq.imr_interface.s_addr=htonl(INADDR_ANY);

    if (setsockopt(network_sock_bound, IPPROTO_IP, IP_ADD_MEMBERSHIP,
               &mreq, sizeof(mreq)) < 0)
    {
        utils_log("Error joining multicast network");

        close(network_sock_broad);
        close(network_sock_bound);
        
        return NULL;
    }*/

    fd_set rfds;
    char buf[64];
    int ret;
    ssize_t recv_ret;
    struct timeval tv;
    int timeouts = 4;
    // unsigned int v, clock, prog;

    /* message parts */
    char         msg_type;
    unsigned int msg_uuid;
    char         msg_content[64];

    /* generate a random uuid */
    srand(time(NULL));
    network_uuid = rand() & 0xFFFFFFFF;

    /* set callback in case of data to send */
    serial_set_send_cb(&network_send_data);

    /* declare network is running */
    network_running = 1;

    utils_log("Network thread started\n");

    /* loop forever */
    while (network_running)
    {
        FD_ZERO(&rfds);
        FD_SET(network_sock_bound, &rfds);

        /* wait one second */
        tv.tv_sec = 1;
        tv.tv_usec = 0;

        /* one second timeout OR something received */
        ret = select(network_sock_bound + 1, &rfds, NULL, NULL, &tv);

        /* error! */
        if (ret == -1)
            break;

        /* ret 0 = timeout */
        if (ret == 0)
        {
            if (++timeouts == 3)
            {
                /* build output message */
                sprintf(buf, "B%08x%s", network_uuid, global_cart_name);

                /* send broadcast message */
                sendto(network_sock_broad, buf, strlen(buf), 0, 
                             (struct sockaddr *) &broadcast_addr, 
                             sizeof(broadcast_addr));

                utils_log("Sending broadcast message %s\n", buf);

                timeouts = 0;
            }

            if (serial.peer_connected)
            {
                if (--network_timeout == 0)
                {
                    /* notify serial module */
                    serial.peer_connected = 0;

                    /* stop Hard Sync mode */
                    cycles_stop_hs();

                    /* notify by the cb */
                    if (network_disconnected_cb)
                        (*network_disconnected_cb) ();
                }
            }
        }
        else
        {
            /* reset message content */
            bzero(buf, sizeof(buf));
            bzero(msg_content, sizeof(msg_content));

            /* exit if an error occour */
            if ((recv_ret = recvfrom(network_sock_bound, buf, 64, 0,
                         (struct sockaddr *) &addr_from, 
                         (socklen_t *) &addr_from_len)) < 1)
                break;

            /* extract message type (1st byte) */
            msg_type = buf[0];

            /* is it broadcast? */
            //if (sscanf(buf, "%c%08x%s", 
           //            &msg_type, &msg_uuid, msg_content) == 3)
           // {
                /* was it send by myself? */
             //   if (msg_uuid != network_uuid)
             //   {
             
            /* is it a serial data message? */
            if (msg_type == 'M')
            {
                network_prog_recv = (uint8_t) buf[3];

                /* buf[1] contains value - buf[2] contains serial clock */
                /* tell serial module something has arrived             */
                serial_recv_byte((uint8_t) buf[1], (uint8_t) buf[2], buf[4]);
            }
            else if (msg_type == 'B')
            {
                /* extract parts from broadcast message */
                sscanf(buf, "%c%08x%s", &msg_type, &msg_uuid, msg_content);

                /* myself? */
                if (network_uuid == msg_uuid)
                    continue;

                /* not the same game? */
                if (strcmp(msg_content, global_cart_name) != 0)
                    continue;

                /* someone is claiming is playing with the same game? */
                if (serial.peer_connected == 0)
                {
                    /* save peer uuid */
                    network_peer_uuid = msg_uuid;

                    /* refresh timeout */
                    network_timeout = 10;

                    /* save sender */
                    memcpy(&network_peer_addr, &addr_from, 
                           sizeof(struct sockaddr_in));

                    /* just change dst port */
                    network_peer_addr.sin_port = htons(64333);

                    /* notify the other peer by sending a b message */
                    sprintf(buf, "B%08x%s", network_uuid, 
                            global_cart_name);

                    /* send broadcast message */
                    sendto(network_sock_broad, buf, strlen(buf), 0,
                           (struct sockaddr *) &network_peer_addr,
                           sizeof(network_peer_addr));

                    /* log that peer is connected */
                    utils_log("Peer connected: %s\n", 
			                  inet_ntoa(network_peer_addr.sin_addr));

                    /* YEAH */
                    serial.peer_connected = 1;

                    /* notify by the cb */
                    if (network_connected_cb)
                        (*network_connected_cb) ();

                    /* start hard sync */
                    cycles_start_hs();
                }
                else
                {
                    /* refresh timeout */
                    if (network_peer_uuid == msg_uuid)
                        network_timeout = 10;
                }
            } 
        }
    }

    /* free serial */
    serial.peer_connected = 0;

    /* stop hard sync mode */
    cycles_stop_hs();

    /* close sockets */
    close(network_sock_broad);
    close(network_sock_bound);    
    
    return NULL;
}

void network_send_data(uint8_t v, uint8_t clock, uint8_t transfer_start)
{
    char msg[5];

    /* format message */
    network_prog_sent = ((network_prog_sent + 1) & 0xff);

    msg[0] = 'M';
    msg[1] = v;
    msg[2] = clock; 
    msg[3] = network_prog_sent;
    msg[4] = transfer_start;

    if (network_prog_sent != network_prog_recv &&
        network_prog_sent != (uint8_t) (network_prog_recv + 1))
        global_quit = 1;

    /* send */
    sendto(network_sock_bound, msg, 5, 0,
           (struct sockaddr *) &network_peer_addr, sizeof(network_peer_addr));
}

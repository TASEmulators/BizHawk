#pragma once

#include <nall/intrinsics.hpp>
#include <nall/stdint.hpp>
#include <nall/string.hpp>

#if !defined(API_POSIX)
  #error "nall/serial: unsupported system"
#endif

#include <sys/ioctl.h>
#include <fcntl.h>
#include <termios.h>
#include <unistd.h>

namespace nall {

struct serial {
  ~serial() {
    close();
  }

  auto readable() -> bool {
    if(!opened) return false;
    fd_set fdset;
    FD_ZERO(&fdset);
    FD_SET(port, &fdset);
    timeval timeout;
    timeout.tv_sec = 0;
    timeout.tv_usec = 0;
    int result = select(FD_SETSIZE, &fdset, nullptr, nullptr, &timeout);
    if(result < 1) return false;
    return FD_ISSET(port, &fdset);
  }

  //-1 on error, otherwise return bytes read
  auto read(uint8_t* data, uint length) -> int {
    if(!opened) return -1;
    return ::read(port, (void*)data, length);
  }

  auto writable() -> bool {
    if(!opened) return false;
    fd_set fdset;
    FD_ZERO(&fdset);
    FD_SET(port, &fdset);
    timeval timeout;
    timeout.tv_sec = 0;
    timeout.tv_usec = 0;
    int result = select(FD_SETSIZE, nullptr, &fdset, nullptr, &timeout);
    if(result < 1) return false;
    return FD_ISSET(port, &fdset);
  }

  //-1 on error, otherwise return bytes written
  auto write(const uint8_t* data, uint length) -> int {
    if(!opened) return -1;
    return ::write(port, (void*)data, length);
  }

  //rate==0: use flow control (synchronous mode)
  //rate!=0: baud-rate (asynchronous mode)
  auto open(string device, uint rate = 0) -> bool {
    close();

    if(!device) device = "/dev/ttyU0";  //note: default device name is for FreeBSD 10+
    port = ::open(device, O_RDWR | O_NOCTTY | O_NDELAY | O_NONBLOCK);
    if(port == -1) return false;

    if(ioctl(port, TIOCEXCL) == -1) { close(); return false; }
    if(fcntl(port, F_SETFL, 0) == -1) { close(); return false; }
    if(tcgetattr(port, &original_attr) == -1) { close(); return false; }

    termios attr = original_attr;
    cfmakeraw(&attr);
    cfsetspeed(&attr, rate ? rate : 57600);  //rate value has no effect in synchronous mode

    attr.c_lflag &=~ (ECHO | ECHONL | ISIG | ICANON | IEXTEN);
    attr.c_iflag &=~ (BRKINT | PARMRK | INPCK | ISTRIP | INLCR | IGNCR | ICRNL | IXON | IXOFF | IXANY);
    attr.c_iflag |=  (IGNBRK | IGNPAR);
    attr.c_oflag &=~ (OPOST);
    attr.c_cflag &=~ (CSIZE | CSTOPB | PARENB | CLOCAL);
    attr.c_cflag |=  (CS8 | CREAD);
    if(rate) {
      attr.c_cflag &= ~CRTSCTS;
    } else {
      attr.c_cflag |=  CRTSCTS;
    }
    attr.c_cc[VTIME] = attr.c_cc[VMIN] = 0;

    if(tcsetattr(port, TCSANOW, &attr) == -1) { close(); return false; }
    return opened = true;
  }

  auto close() -> void {
    if(port != -1) {
      tcdrain(port);
      if(opened) {
        tcsetattr(port, TCSANOW, &original_attr);
        opened = false;
      }
      ::close(port);
      port = -1;
    }
  }

private:
  int port = -1;
  bool opened = false;
  termios original_attr;
};

}

#ifndef NALL_SNES_USART_HPP
#define NALL_SNES_USART_HPP

#include <nall/platform.hpp>
#include <nall/function.hpp>
#include <nall/serial.hpp>
#include <nall/stdint.hpp>

#define usartproc dllexport

static nall::function<void (unsigned milliseconds)> usart_usleep;
static nall::function<uint8_t ()> usart_read;
static nall::function<void (uint8_t data)> usart_write;

extern "C" usartproc void usart_init(
  nall::function<void (unsigned milliseconds)> usleep,
  nall::function<uint8_t ()> read,
  nall::function<void (uint8_t data)> write
) {
  usart_usleep = usleep;
  usart_read = read;
  usart_write = write;
}

extern "C" usartproc void usart_main();

//

static nall::serial usart;
static bool usart_is_virtual = true;

static bool usart_virtual() {
  return usart_is_virtual;
}

//

static void usarthw_usleep(unsigned milliseconds) {
  usleep(milliseconds);
}

static uint8_t usarthw_read() {
  while(true) {
    uint8_t buffer[1];
    signed length = usart.read((uint8_t*)&buffer, 1);
    if(length > 0) return buffer[0];
  }
}

static void usarthw_write(uint8_t data) {
  uint8_t buffer[1] = { data };
  usart.write((uint8_t*)&buffer, 1);
}

int main(int argc, char **argv) {
  bool result = false;
  if(argc == 1) result = usart.open("/dev/ttyACM0", 57600, true);
  if(argc == 2) result = usart.open(argv[1], 57600, true);
  if(result == false) {
    printf("error: unable to open USART hardware device\n");
    return 0;
  }
  usart_is_virtual = false;
  usart_init(usarthw_usleep, usarthw_read, usarthw_write);
  usart_main();
  usart.close();
  return 0;
}

#endif

#include <nall/tcptext/tcptext-server.hpp>

namespace nall::TCPText {
  NALL_HEADER_INLINE auto Server::sendText(const string &text) -> void {
    sendData((const u8*)text.data(), text.size());
  }

  NALL_HEADER_INLINE auto Server::onData(const vector<u8> &data) -> void {
    string_view dataStr((const char*)data.data(), (u32)data.size());

    if(!hadHandshake) {
      hadHandshake = true;

      // This is a security check for browsers.
      // Any website can request localhost via JS or HTML, while it can't see the result, 
      // GDB will receive the data and commands could be injected (true for all GDB-servers).
      // Since all HTTP requests start with headers, we can simply block anything that doesn't start like a GDB client.
      if(dataStr[0] != '+') {
        printf("Non-GDB client detected (message: %s), disconnect client\n", dataStr.data());
        disconnectClient();
        return;
      }

      onConnect();
    }

    onText(dataStr);
  }
}

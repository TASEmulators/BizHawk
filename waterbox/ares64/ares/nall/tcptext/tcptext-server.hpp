#pragma once

#include <nall/tcptext/tcp-socket.hpp>

/**
 * Provides text-based TCP server on top of the Socket.
 * This handles incoming messages and can send data back to the client.
 */
namespace nall::TCPText {

class Server : public TCP::Socket {
  public: 
    bool hadHandshake{false};

  protected:
    auto onData(const vector<u8> &data) -> void override;

    auto sendText(const string &text) -> void;
    virtual auto onText(string_view text) -> void = 0;
};

}

#if defined(NALL_HEADER_ONLY)
  #include <nall/tcptext/tcptext-server.cpp>
#endif

#pragma once

//Role: base class for Client and Server
//provides shared functionality

#include <nall/http/request.hpp>
#include <nall/http/response.hpp>

namespace nall::HTTP {

struct Role {
  struct Settings {
    int connectionLimit =     1 * 1024;  //server
    int headSizeLimit   =    16 * 1024;  //client, server
    int bodySizeLimit   = 65536 * 1024;  //client, server
    int chunkSize       =    32 * 1024;  //client, server
    int threadStackSize =   128 * 1024;  //server
    int timeoutReceive  =    15 * 1000;  //server
    int timeoutSend     =    15 * 1000;  //server
  } settings;

  inline auto configure(const string& parameters) -> bool;
  inline auto download(int fd, Message& message) -> bool;
  inline auto upload(int fd, const Message& message) -> bool;
};

auto Role::configure(const string& parameters) -> bool {
  auto document = BML::unserialize(parameters);
  for(auto parameter : document) {
    auto name = parameter.name();
    auto value = parameter.integer();

    if(0);
    else if(name == "connectionLimit") settings.connectionLimit = value;
    else if(name == "headSizeLimit") settings.headSizeLimit = value;
    else if(name == "bodySizeLimit") settings.bodySizeLimit = value;
    else if(name == "chunkSize") settings.chunkSize = value;
    else if(name == "threadStackSize") settings.threadStackSize = value;
    else if(name == "timeoutReceive") settings.timeoutReceive = value;
    else if(name == "timeoutSend") settings.timeoutSend = value;
  }
  return true;
}

auto Role::download(int fd, Message& message) -> bool {
  auto& head = message._head;
  auto& body = message._body;
  string chunk;
  uint8_t packet[settings.chunkSize], *p = nullptr;

  head.reset(), head.reserve(4095);
  body.reset(), body.reserve(4095);

  bool headReceived = false;
  bool chunked = false;
  bool chunkReceived = false;
  bool chunkFooterReceived = true;
  int length = 0;
  int chunkLength = 0;
  int contentLength = 0;

  while(true) {
    if(auto limit = settings.headSizeLimit) if(head.size() >= limit) return false;
    if(auto limit = settings.bodySizeLimit) if(body.size() >= limit) return false;

    if(headReceived && !chunked && body.size() >= contentLength) {
      body.resize(contentLength);
      break;
    }

    if(length == 0) {
      length = recv(fd, packet, settings.chunkSize, MSG_NOSIGNAL);
      if(length <= 0) return false;
      p = packet;
    }

    if(!headReceived) {
      head.append((char)*p++);
      --length;

      if(head.endsWith("\r\n\r\n") || head.endsWith("\n\n")) {
        headReceived = true;
        if(!message.setHead()) return false;
        chunked = message.header["Transfer-Encoding"].value().iequals("chunked");
        contentLength = message.header["Content-Length"].value().natural();
      }

      continue;
    }

    if(chunked && !chunkReceived) {
      char n = *p++;
      --length;

      if(!chunkFooterReceived) {
        if(n == '\n') chunkFooterReceived = true;
        continue;
      }

      chunk.append(n);

      if(chunk.endsWith("\r\n") || chunk.endsWith("\n")) {
        chunkReceived = true;
        chunkLength = chunk.hex();
        if(chunkLength == 0) break;
        chunk.reset();
      }

      continue;
    }

    if(!chunked) {
      body.resize(body.size() + length);
      memory::copy(body.get() + body.size() - length, p, length);

      p += length;
      length = 0;
    } else {
      int transferLength = min(length, chunkLength);
      body.resize(body.size() + transferLength);
      memory::copy(body.get() + body.size() - transferLength, p, transferLength);

      p += transferLength;
      length -= transferLength;
      chunkLength -= transferLength;

      if(chunkLength == 0) {
        chunkReceived = false;
        chunkFooterReceived = false;
      }
    }
  }

  if(!message.setBody()) return false;
  return true;
}

auto Role::upload(int fd, const Message& message) -> bool {
  auto transfer = [&](const uint8_t* data, uint size) -> bool {
    while(size) {
      int length = send(fd, data, min(size, settings.chunkSize), MSG_NOSIGNAL);
      if(length < 0) return false;
      data += length;
      size -= length;
    }
    return true;
  };

  if(message.head([&](const uint8_t* data, uint size) -> bool { return transfer(data, size); })) {
    if(message.body([&](const uint8_t* data, uint size) -> bool { return transfer(data, size); })) {
      return true;
    }
  }

  return false;
}

}

#pragma once

#include <nall/base64.hpp>
#include <nall/stdint.hpp>
#include <nall/string.hpp>

#if !defined(PLATFORM_WINDOWS)
  #include <sys/types.h>
  #include <sys/socket.h>
  #include <netinet/in.h>
  #include <netdb.h>
#else
  #include <winsock2.h>
  #include <ws2tcpip.h>
#endif

namespace nall {

struct SMTP {
  enum class Format : uint { Plain, HTML };

  inline auto server(string server, uint16_t port = 25) -> void;
  inline auto from(string mail, string name = "") -> void;
  inline auto to(string mail, string name = "") -> void;
  inline auto cc(string mail, string name = "") -> void;
  inline auto bcc(string mail, string name = "") -> void;
  inline auto attachment(const uint8_t* data, uint size, string name) -> void;
  inline auto attachment(string filename, string name = "") -> bool;
  inline auto subject(string subject) -> void;
  inline auto body(string body, Format format = Format::Plain) -> void;

  inline auto send() -> bool;
  inline auto message() -> string;
  inline auto response() -> string;

  #if defined(API_WINDOWS)
  inline auto close(int) -> int;
  inline SMTP();
  #endif

private:
  struct Information {
    string server;
    uint16_t port;
    struct Contact {
      string mail;
      string name;
    };
    Contact from;
    vector<Contact> to;
    vector<Contact> cc;
    vector<Contact> bcc;
    struct Attachment {
      vector<uint8_t> buffer;
      string name;
    };
    string subject;
    string body;
    Format format = Format::Plain;
    vector<Attachment> attachments;

    string message;
    string response;
  } info;

  inline auto send(int sock, const string& text) -> bool;
  inline auto recv(int sock) -> string;
  inline auto boundary() -> string;
  inline auto filename(const string& filename) -> string;
  inline auto contact(const Information::Contact& contact) -> string;
  inline auto contacts(const vector<Information::Contact>& contacts) -> string;
  inline auto split(const string& text) -> string;
};

auto SMTP::server(string server, uint16_t port) -> void {
  info.server = server;
  info.port = port;
}

auto SMTP::from(string mail, string name) -> void {
  info.from = {mail, name};
}

auto SMTP::to(string mail, string name) -> void {
  info.to.append({mail, name});
}

auto SMTP::cc(string mail, string name) -> void {
  info.cc.append({mail, name});
}

auto SMTP::bcc(string mail, string name) -> void {
  info.bcc.append({mail, name});
}

auto SMTP::attachment(const uint8_t* data, uint size, string name) -> void {
  vector<uint8_t> buffer;
  buffer.resize(size);
  memcpy(buffer.data(), data, size);
  info.attachments.append({std::move(buffer), name});
}

auto SMTP::attachment(string filename, string name) -> bool {
  if(!file::exists(filename)) return false;
  if(name == "") name = notdir(filename);
  auto buffer = file::read(filename);
  info.attachments.append({std::move(buffer), name});
  return true;
}

auto SMTP::subject(string subject) -> void {
  info.subject = subject;
}

auto SMTP::body(string body, Format format) -> void {
  info.body = body;
  info.format = format;
}

auto SMTP::send() -> bool {
  info.message.append("From: =?UTF-8?B?", Base64::encode(contact(info.from)), "?=\r\n");
  info.message.append("To: =?UTF-8?B?", Base64::encode(contacts(info.to)), "?=\r\n");
  info.message.append("Cc: =?UTF-8?B?", Base64::encode(contacts(info.cc)), "?=\r\n");
  info.message.append("Subject: =?UTF-8?B?", Base64::encode(info.subject), "?=\r\n");

  string uniqueID = boundary();

  info.message.append("MIME-Version: 1.0\r\n");
  info.message.append("Content-Type: multipart/mixed; boundary=", uniqueID, "\r\n");
  info.message.append("\r\n");

  string format = (info.format == Format::Plain ? "text/plain" : "text/html");

  info.message.append("--", uniqueID, "\r\n");
  info.message.append("Content-Type: ", format, "; charset=UTF-8\r\n");
  info.message.append("Content-Transfer-Encoding: base64\r\n");
  info.message.append("\r\n");
  info.message.append(split(Base64::encode(info.body)), "\r\n");
  info.message.append("\r\n");

  for(auto& attachment : info.attachments) {
    info.message.append("--", uniqueID, "\r\n");
    info.message.append("Content-Type: application/octet-stream\r\n");
    info.message.append("Content-Transfer-Encoding: base64\r\n");
    info.message.append("Content-Disposition: attachment; size=", attachment.buffer.size(), "; filename*=UTF-8''", filename(attachment.name), "\r\n");
    info.message.append("\r\n");
    info.message.append(split(Base64::encode(attachment.buffer)), "\r\n");
    info.message.append("\r\n");
  }

  info.message.append("--", uniqueID, "--\r\n");

  addrinfo hints;
  memset(&hints, 0, sizeof(addrinfo));
  hints.ai_family = AF_UNSPEC;
  hints.ai_socktype = SOCK_STREAM;
  hints.ai_flags = AI_PASSIVE;

  addrinfo* serverinfo;
  int status = getaddrinfo(info.server, string(info.port), &hints, &serverinfo);
  if(status != 0) return false;

  int sock = socket(serverinfo->ai_family, serverinfo->ai_socktype, serverinfo->ai_protocol);
  if(sock == -1) return false;

  int result = connect(sock, serverinfo->ai_addr, serverinfo->ai_addrlen);
  if(result == -1) return false;

  string response;
  info.response.append(response = recv(sock));
  if(!response.beginswith("220 ")) { close(sock); return false; }

  send(sock, {"HELO ", info.server, "\r\n"});
  info.response.append(response = recv(sock));
  if(!response.beginswith("250 ")) { close(sock); return false; }

  send(sock, {"MAIL FROM: <", info.from.mail, ">\r\n"});
  info.response.append(response = recv(sock));
  if(!response.beginswith("250 ")) { close(sock); return false; }

  for(auto& contact : info.to) {
    send(sock, {"RCPT TO: <", contact.mail, ">\r\n"});
    info.response.append(response = recv(sock));
    if(!response.beginswith("250 ")) { close(sock); return false; }
  }

  for(auto& contact : info.cc) {
    send(sock, {"RCPT TO: <", contact.mail, ">\r\n"});
    info.response.append(response = recv(sock));
    if(!response.beginswith("250 ")) { close(sock); return false; }
  }

  for(auto& contact : info.bcc) {
    send(sock, {"RCPT TO: <", contact.mail, ">\r\n"});
    info.response.append(response = recv(sock));
    if(!response.beginswith("250 ")) { close(sock); return false; }
  }

  send(sock, {"DATA\r\n"});
  info.response.append(response = recv(sock));
  if(!response.beginswith("354 ")) { close(sock); return false; }

  send(sock, {info.message, "\r\n", ".\r\n"});
  info.response.append(response = recv(sock));
  if(!response.beginswith("250 ")) { close(sock); return false; }

  send(sock, {"QUIT\r\n"});
  info.response.append(response = recv(sock));
//if(!response.beginswith("221 ")) { close(sock); return false; }

  close(sock);
  return true;
}

auto SMTP::message() -> string {
  return info.message;
}

auto SMTP::response() -> string {
  return info.response;
}

auto SMTP::send(int sock, const string& text) -> bool {
  const char* data = text.data();
  uint size = text.size();
  while(size) {
    int length = ::send(sock, (const char*)data, size, 0);
    if(length == -1) return false;
    data += length;
    size -= length;
  }
  return true;
}

auto SMTP::recv(int sock) -> string {
  vector<uint8_t> buffer;
  while(true) {
    char c;
    if(::recv(sock, &c, sizeof(char), 0) < 1) break;
    buffer.append(c);
    if(c == '\n') break;
  }
  buffer.append(0);
  return buffer;
}

auto SMTP::boundary() -> string {
  random_lfsr random;
  random.seed(time(0));
  string boundary;
  for(uint n = 0; n < 16; n++) boundary.append(hex<2>(random()));
  return boundary;
}

auto SMTP::filename(const string& filename) -> string {
  string result;
  for(auto& n : filename) {
    if(n <= 32 || n >= 127) result.append("%", hex<2>(n));
    else result.append(n);
  }
  return result;
}

auto SMTP::contact(const Information::Contact& contact) -> string {
  if(!contact.name) return contact.mail;
  return {"\"", contact.name, "\" <", contact.mail, ">"};
}

auto SMTP::contacts(const vector<Information::Contact>& contacts) -> string {
  string result;
  for(auto& contact : contacts) {
    result.append(this->contact(contact), "; ");
  }
  result.trimRight("; ", 1L);
  return result;
}

auto SMTP::split(const string& text) -> string {
  string result;

  uint offset = 0;
  while(offset < text.size()) {
    uint length = min(76, text.size() - offset);
    if(length < 76) {
      result.append(text.slice(offset));
    } else {
      result.append(text.slice(offset, 76), "\r\n");
    }
    offset += length;
  }

  return result;
}

#if defined(API_WINDOWS)
auto SMTP::close(int sock) -> int {
  return closesocket(sock);
}

SMTP::SMTP() {
  int sock = socket(AF_INET, SOCK_STREAM, IPPROTO_TCP);
  if(sock == INVALID_SOCKET && WSAGetLastError() == WSANOTINITIALISED) {
    WSADATA wsaData;
    if(WSAStartup(MAKEWORD(2, 2), &wsaData) != 0) {
      WSACleanup();
      return;
    }
  } else {
    close(sock);
  }
}
#endif

}

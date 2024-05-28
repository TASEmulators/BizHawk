#include <nall/smtp.hpp>

#if defined(PLATFORM_WINDOWS)
  #include <ws2tcpip.h>
#endif

namespace nall {

NALL_HEADER_INLINE auto SMTP::send() -> bool {
  info.message.append("From: =?UTF-8?B?", Encode::Base64(contact(info.from)), "?=\r\n");
  info.message.append("To: =?UTF-8?B?", Encode::Base64(contacts(info.to)), "?=\r\n");
  info.message.append("Cc: =?UTF-8?B?", Encode::Base64(contacts(info.cc)), "?=\r\n");
  info.message.append("Subject: =?UTF-8?B?", Encode::Base64(info.subject), "?=\r\n");

  string uniqueID = boundary();

  info.message.append("MIME-Version: 1.0\r\n");
  info.message.append("Content-Type: multipart/mixed; boundary=", uniqueID, "\r\n");
  info.message.append("\r\n");

  string format = (info.format == Format::Plain ? "text/plain" : "text/html");

  info.message.append("--", uniqueID, "\r\n");
  info.message.append("Content-Type: ", format, "; charset=UTF-8\r\n");
  info.message.append("Content-Transfer-Encoding: base64\r\n");
  info.message.append("\r\n");
  info.message.append(split(Encode::Base64(info.body)), "\r\n");
  info.message.append("\r\n");

  for(auto& attachment : info.attachments) {
    info.message.append("--", uniqueID, "\r\n");
    info.message.append("Content-Type: application/octet-stream\r\n");
    info.message.append("Content-Transfer-Encoding: base64\r\n");
    info.message.append("Content-Disposition: attachment; size=", attachment.buffer.size(), "; filename*=UTF-8''", filename(attachment.name), "\r\n");
    info.message.append("\r\n");
    info.message.append(split(Encode::Base64(attachment.buffer)), "\r\n");
    info.message.append("\r\n");
  }

  info.message.append("--", uniqueID, "--\r\n");

  addrinfo hints;
  memset(&hints, 0, sizeof(addrinfo));
  hints.ai_family = AF_UNSPEC;
  hints.ai_socktype = SOCK_STREAM;
  hints.ai_flags = AI_PASSIVE;

  addrinfo* serverinfo;
  s32 status = getaddrinfo(info.server, string(info.port), &hints, &serverinfo);
  if(status != 0) return false;

  s32 sock = socket(serverinfo->ai_family, serverinfo->ai_socktype, serverinfo->ai_protocol);
  if(sock == -1) return false;

  s32 result = connect(sock, serverinfo->ai_addr, serverinfo->ai_addrlen);
  if(result == -1) return false;

  string response;
  info.response.append(response = recv(sock));
  if(!response.beginsWith("220 ")) { close(sock); return false; }

  send(sock, {"HELO ", info.server, "\r\n"});
  info.response.append(response = recv(sock));
  if(!response.beginsWith("250 ")) { close(sock); return false; }

  send(sock, {"MAIL FROM: <", info.from.mail, ">\r\n"});
  info.response.append(response = recv(sock));
  if(!response.beginsWith("250 ")) { close(sock); return false; }

  for(auto& contact : info.to) {
    send(sock, {"RCPT TO: <", contact.mail, ">\r\n"});
    info.response.append(response = recv(sock));
    if(!response.beginsWith("250 ")) { close(sock); return false; }
  }

  for(auto& contact : info.cc) {
    send(sock, {"RCPT TO: <", contact.mail, ">\r\n"});
    info.response.append(response = recv(sock));
    if(!response.beginsWith("250 ")) { close(sock); return false; }
  }

  for(auto& contact : info.bcc) {
    send(sock, {"RCPT TO: <", contact.mail, ">\r\n"});
    info.response.append(response = recv(sock));
    if(!response.beginsWith("250 ")) { close(sock); return false; }
  }

  send(sock, {"DATA\r\n"});
  info.response.append(response = recv(sock));
  if(!response.beginsWith("354 ")) { close(sock); return false; }

  send(sock, {info.message, "\r\n", ".\r\n"});
  info.response.append(response = recv(sock));
  if(!response.beginsWith("250 ")) { close(sock); return false; }

  send(sock, {"QUIT\r\n"});
  info.response.append(response = recv(sock));
//if(!response.beginsWith("221 ")) { close(sock); return false; }

  close(sock);
  return true;
}

NALL_HEADER_INLINE auto SMTP::send(s32 sock, const string& text) -> bool {
  const char* data = text.data();
  u32 size = text.size();
  while(size) {
    s32 length = ::send(sock, (const char*)data, size, 0);
    if(length == -1) return false;
    data += length;
    size -= length;
  }
  return true;
}

NALL_HEADER_INLINE auto SMTP::recv(s32 sock) -> string {
  vector<u8> buffer;
  while(true) {
    char c;
    if(::recv(sock, &c, sizeof(char), 0) < 1) break;
    buffer.append(c);
    if(c == '\n') break;
  }
  buffer.append(0);
  return buffer;
}

#if defined(API_WINDOWS)

NALL_HEADER_INLINE auto SMTP::close(s32 sock) -> s32 {
  return closesocket(sock);
}

NALL_HEADER_INLINE SMTP::SMTP() {
  s32 sock = socket(AF_INET, SOCK_STREAM, IPPROTO_TCP);
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

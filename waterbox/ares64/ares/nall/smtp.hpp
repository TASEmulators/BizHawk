#pragma once

#include <nall/stdint.hpp>
#include <nall/string.hpp>
#include <nall/file.hpp>
#include <nall/location.hpp>
#include <nall/random.hpp>
#include <nall/encode/base64.hpp>

#if !defined(PLATFORM_WINDOWS)
  #include <sys/types.h>
  #include <sys/socket.h>
  #include <netinet/in.h>
  #include <netdb.h>
#endif

namespace nall {

struct SMTP {
  enum class Format : u32 { Plain, HTML };

  auto server(string server, u16 port = 25) -> void;
  auto from(string mail, string name = "") -> void;
  auto to(string mail, string name = "") -> void;
  auto cc(string mail, string name = "") -> void;
  auto bcc(string mail, string name = "") -> void;
  auto attachment(const u8* data, u32 size, string name) -> void;
  auto attachment(string filename, string name = "") -> bool;
  auto subject(string subject) -> void;
  auto body(string body, Format format = Format::Plain) -> void;

  auto send() -> bool;
  auto message() -> string;
  auto response() -> string;

  #if defined(API_WINDOWS)
  auto close(s32) -> s32;
  SMTP();
  #endif

private:
  struct Information {
    string server;
    u16 port;
    struct Contact {
      string mail;
      string name;
    };
    Contact from;
    vector<Contact> to;
    vector<Contact> cc;
    vector<Contact> bcc;
    struct Attachment {
      vector<u8> buffer;
      string name;
    };
    string subject;
    string body;
    Format format = Format::Plain;
    vector<Attachment> attachments;

    string message;
    string response;
  } info;

  auto send(s32 sock, const string& text) -> bool;
  auto recv(s32 sock) -> string;
  auto boundary() -> string;
  auto filename(const string& filename) -> string;
  auto contact(const Information::Contact& contact) -> string;
  auto contacts(const vector<Information::Contact>& contacts) -> string;
  auto split(const string& text) -> string;
};

inline auto SMTP::server(string server, u16 port) -> void {
  info.server = server;
  info.port = port;
}

inline auto SMTP::from(string mail, string name) -> void {
  info.from = {mail, name};
}

inline auto SMTP::to(string mail, string name) -> void {
  info.to.append({mail, name});
}

inline auto SMTP::cc(string mail, string name) -> void {
  info.cc.append({mail, name});
}

inline auto SMTP::bcc(string mail, string name) -> void {
  info.bcc.append({mail, name});
}

inline auto SMTP::attachment(const u8* data, u32 size, string name) -> void {
  vector<u8> buffer;
  buffer.resize(size);
  memcpy(buffer.data(), data, size);
  info.attachments.append({std::move(buffer), name});
}

inline auto SMTP::attachment(string filename, string name) -> bool {
  if(!file::exists(filename)) return false;
  if(name == "") name = Location::file(filename);
  auto buffer = file::read(filename);
  info.attachments.append({std::move(buffer), name});
  return true;
}

inline auto SMTP::subject(string subject) -> void {
  info.subject = subject;
}

inline auto SMTP::body(string body, Format format) -> void {
  info.body = body;
  info.format = format;
}

inline auto SMTP::message() -> string {
  return info.message;
}

inline auto SMTP::response() -> string {
  return info.response;
}

inline auto SMTP::boundary() -> string {
  PRNG::LFSR random;
  random.seed(time(0));
  string boundary;
  for(u32 n = 0; n < 16; n++) boundary.append(hex(random.random(), 2L));
  return boundary;
}

inline auto SMTP::filename(const string& filename) -> string {
  string result;
  for(auto& n : filename) {
    if(n <= 32 || n >= 127) result.append("%", hex(n, 2L));
    else result.append(n);
  }
  return result;
}

inline auto SMTP::contact(const Information::Contact& contact) -> string {
  if(!contact.name) return contact.mail;
  return {"\"", contact.name, "\" <", contact.mail, ">"};
}

inline auto SMTP::contacts(const vector<Information::Contact>& contacts) -> string {
  string result;
  for(auto& contact : contacts) {
    result.append(this->contact(contact), "; ");
  }
  result.trimRight("; ", 1L);
  return result;
}

inline auto SMTP::split(const string& text) -> string {
  string result;

  u32 offset = 0;
  while(offset < text.size()) {
    u32 length = min(76, text.size() - offset);
    if(length < 76) {
      result.append(text.slice(offset));
    } else {
      result.append(text.slice(offset, 76), "\r\n");
    }
    offset += length;
  }

  return result;
}

}

#if defined(NALL_HEADER_ONLY)
  #include <nall/smtp.cpp>
#endif

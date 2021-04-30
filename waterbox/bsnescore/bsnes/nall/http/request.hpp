#pragma once

#include <nall/decode/url.hpp>
#include <nall/encode/url.hpp>
#include <nall/http/message.hpp>

namespace nall::HTTP {

struct Request : Message {
  using type = Request;

  enum class RequestType : uint { None, Head, Get, Post };

  explicit operator bool() const { return requestType() != RequestType::None; }

  inline auto head(const function<bool (const uint8_t* data, uint size)>& callback) const -> bool override;
  inline auto setHead() -> bool override;

  inline auto body(const function<bool (const uint8_t* data, uint size)>& callback) const -> bool override;
  inline auto setBody() -> bool override;

  auto ipv4() const -> bool { return _ipv6 == false; }
  auto ipv6() const -> bool { return _ipv6 == true; }
  auto ip() const -> string { return _ip; }

  auto requestType() const -> RequestType { return _requestType; }
  auto setRequestType(RequestType value) -> void { _requestType = value; }

  auto path() const -> string { return _path; }
  auto setPath(const string& value) -> void { _path = value; }

  Variables cookie;
  Variables get;
  Variables post;

//private:
  bool _ipv6 = false;
  string _ip;
  RequestType _requestType = RequestType::None;
  string _path;
};

auto Request::head(const function<bool (const uint8_t*, uint)>& callback) const -> bool {
  if(!callback) return false;
  string output;

  string request = path();
  if(get.size()) {
    request.append("?");
    for(auto& variable : get) {
      request.append(Encode::URL(variable.name()), "=", Encode::URL(variable.value()), "&");
    }
    request.trimRight("&", 1L);
  }

  switch(requestType()) {
  case RequestType::Head: output.append("HEAD ", request, " HTTP/1.1\r\n"); break;
  case RequestType::Get : output.append("GET ",  request, " HTTP/1.1\r\n"); break;
  case RequestType::Post: output.append("POST ", request, " HTTP/1.1\r\n"); break;
  default: return false;
  }

  for(auto& variable : header) {
    output.append(variable.name(), ": ", variable.value(), "\r\n");
  }
  output.append("\r\n");

  return callback(output.data<uint8_t>(), output.size());
}

auto Request::setHead() -> bool {
  auto headers = _head.split("\n");
  string request = headers.takeLeft().trimRight("\r", 1L);
  string requestHost;

       if(request.iendsWith(" HTTP/1.0")) request.itrimRight(" HTTP/1.0", 1L);
  else if(request.iendsWith(" HTTP/1.1")) request.itrimRight(" HTTP/1.1", 1L);
  else return false;

       if(request.ibeginsWith("HEAD ")) request.itrimLeft("HEAD ", 1L), setRequestType(RequestType::Head);
  else if(request.ibeginsWith("GET " )) request.itrimLeft("GET ",  1L), setRequestType(RequestType::Get );
  else if(request.ibeginsWith("POST ")) request.itrimLeft("POST ", 1L), setRequestType(RequestType::Post);
  else return false;

  //decode absolute URIs
  request.strip().itrimLeft("http://", 1L);
  if(!request.beginsWith("/")) {
    auto components = request.split("/", 1L);
    requestHost = components(0);
    request = {"/", components(1)};
  }

  auto components = request.split("?", 1L);
  setPath(components(0));

  if(auto queryString = components(1)) {
    for(auto& block : queryString.split("&")) {
      auto p = block.split("=", 1L);
      auto name = Decode::URL(p(0));
      auto value = Decode::URL(p(1));
      if(name) get.append(name, value);
    }
  }

  for(auto& header : headers) {
    if(header.beginsWith(" ") || header.beginsWith("\t")) continue;
    auto part = header.split(":", 1L).strip();
    if(!part[0] || part.size() != 2) continue;
    this->header.append(part[0], part[1]);

    if(part[0].iequals("Cookie")) {
      for(auto& block : part[1].split(";")) {
        auto p = block.split("=", 1L).strip();
        auto name = p(0);
        auto value = p(1).trim("\"", "\"", 1L);
        if(name) cookie.append(name, value);
      }
    }
  }

  if(requestHost) header.assign("Host", requestHost);  //request URI overrides host header
  return true;
}

auto Request::body(const function<bool (const uint8_t*, uint)>& callback) const -> bool {
  if(!callback) return false;

  if(_body) {
    return callback(_body.data<uint8_t>(), _body.size());
  }

  return true;
}

auto Request::setBody() -> bool {
  if(requestType() == RequestType::Post) {
    auto contentType = header["Content-Type"].value();
    if(contentType.iequals("application/x-www-form-urlencoded")) {
      for(auto& block : _body.split("&")) {
        auto p = block.trimRight("\r").split("=", 1L);
        auto name = Decode::URL(p(0));
        auto value = Decode::URL(p(1));
        if(name) post.append(name, value);
      }
    } else if(contentType.imatch("multipart/form-data; boundary=?*")) {
      auto boundary = contentType.itrimLeft("multipart/form-data; boundary=", 1L).trim("\"", "\"", 1L);
      auto blocks = _body.split({"--", boundary}, 1024L);  //limit blocks to prevent memory exhaustion
      for(auto& block : blocks) block.trim("\r\n", "\r\n", 1L);
      if(blocks.size() < 2 || (blocks.takeLeft(), !blocks.takeRight().beginsWith("--"))) return false;
      for(auto& block : blocks) {
        string name;
        string filename;
        string contentType;

        auto segments = block.split("\r\n\r\n", 1L);
        for(auto& segment : segments(0).split("\r\n")) {
          auto statement = segment.split(":", 1L);
          if(statement(0).ibeginsWith("Content-Disposition")) {
            for(auto& component : statement(1).split(";")) {
              auto part = component.split("=", 1L).strip();
              if(part(0).iequals("name")) {
                name = part(1).trim("\"", "\"", 1L);
              } else if(part(0).iequals("filename")) {
                filename = part(1).trim("\"", "\"", 1L);
              }
            }
          } else if(statement(0).ibeginsWith("Content-Type")) {
            contentType = statement(1).strip();
          }
        }

        if(name) {
          post.append(name, segments(1));
          post.append({name, ".filename"}, filename);
          post.append({name, ".content-type"}, contentType);
        }
      }
    }
  }

  return true;
}

}

#pragma once

#include <nall/http/message.hpp>

namespace nall::HTTP {

struct Response : Message {
  using type = Response;

  Response() = default;
  Response(const Request& request) { setRequest(request); }

  explicit operator bool() const { return responseType() != 0; }
  auto operator()(uint responseType) -> type& { return setResponseType(responseType); }

  inline auto head(const function<bool (const uint8_t* data, uint size)>& callback) const -> bool override;
  inline auto setHead() -> bool override;

  inline auto body(const function<bool (const uint8_t* data, uint size)>& callback) const -> bool override;
  inline auto setBody() -> bool override;

  auto request() const -> const Request* { return _request; }
  auto setRequest(const Request& value) -> type& { _request = &value; return *this; }

  auto responseType() const -> uint { return _responseType; }
  auto setResponseType(uint value) -> type& { _responseType = value; return *this; }

  auto hasData() const -> bool { return (bool)_data; }
  auto data() const -> const vector<uint8_t>& { return _data; }
  inline auto setData(const vector<uint8_t>& value) -> type&;

  auto hasFile() const -> bool { return (bool)_file; }
  auto file() const -> const string& { return _file; }
  inline auto setFile(const string& value) -> type&;

  auto hasText() const -> bool { return (bool)_text; }
  auto text() const -> const string& { return _text; }
  inline auto setText(const string& value) -> type&;

  inline auto hasBody() const -> bool;
  inline auto findContentLength() const -> uint;
  inline auto findContentType() const -> string;
  inline auto findContentType(const string& suffix) const -> string;
  inline auto findResponseType() const -> string;
  inline auto setFileETag() -> void;

  const Request* _request = nullptr;
  uint _responseType = 0;
  vector<uint8_t> _data;
  string _file;
  string _text;
};

auto Response::head(const function<bool (const uint8_t*, uint)>& callback) const -> bool {
  if(!callback) return false;
  string output;

  if(auto request = this->request()) {
    if(auto eTag = header["ETag"]) {
      if(eTag.value() == request->header["If-None-Match"].value()) {
        output.append("HTTP/1.1 304 Not Modified\r\n");
        output.append("Connection: close\r\n");
        output.append("\r\n");
        return callback(output.data<uint8_t>(), output.size());
      }
    }
  }

  output.append("HTTP/1.1 ", findResponseType(), "\r\n");
  for(auto& variable : header) {
    output.append(variable.name(), ": ", variable.value(), "\r\n");
  }
  if(hasBody()) {
    if(!header["Content-Length"] && !header["Transfer-Encoding"].value().iequals("chunked")) {
      output.append("Content-Length: ", findContentLength(), "\r\n");
    }
    if(!header["Content-Type"]) {
      output.append("Content-Type: ", findContentType(), "\r\n");
    }
  }
  if(!header["Connection"]) {
    output.append("Connection: close\r\n");
  }
  output.append("\r\n");

  return callback(output.data<uint8_t>(), output.size());
}

auto Response::setHead() -> bool {
  auto headers = _head.split("\n");
  string response = headers.takeLeft().trimRight("\r");

       if(response.ibeginsWith("HTTP/1.0 ")) response.itrimLeft("HTTP/1.0 ", 1L);
  else if(response.ibeginsWith("HTTP/1.1 ")) response.itrimLeft("HTTP/1.1 ", 1L);
  else return false;

  setResponseType(response.natural());

  for(auto& header : headers) {
    if(header.beginsWith(" ") || header.beginsWith("\t")) continue;
    auto variable = header.split(":", 1L).strip();
    if(variable.size() != 2) continue;
    this->header.append(variable[0], variable[1]);
  }

  return true;
}

auto Response::body(const function<bool (const uint8_t*, uint)>& callback) const -> bool {
  if(!callback) return false;
  if(!hasBody()) return true;
  bool chunked = header["Transfer-Encoding"].value() == "chunked";

  if(chunked) {
    string prefix = {hex(findContentLength()), "\r\n"};
    if(!callback(prefix.data<uint8_t>(), prefix.size())) return false;
  }

  if(_body) {
    if(!callback(_body.data<uint8_t>(), _body.size())) return false;
  } else if(hasData()) {
    if(!callback(data().data(), data().size())) return false;
  } else if(hasFile()) {
    file_map map(file(), file_map::mode::read);
    if(!callback(map.data(), map.size())) return false;
  } else if(hasText()) {
    if(!callback(text().data<uint8_t>(), text().size())) return false;
  } else {
    string response = findResponseType();
    if(!callback(response.data<uint8_t>(), response.size())) return false;
  }

  if(chunked) {
    string suffix = {"\r\n0\r\n\r\n"};
    if(!callback(suffix.data<uint8_t>(), suffix.size())) return false;
  }

  return true;
}

auto Response::setBody() -> bool {
  return true;
}

auto Response::hasBody() const -> bool {
  if(auto request = this->request()) {
    if(request->requestType() == Request::RequestType::Head) return false;
  }
  if(responseType() == 301) return false;
  if(responseType() == 302) return false;
  if(responseType() == 303) return false;
  if(responseType() == 304) return false;
  if(responseType() == 307) return false;
  return true;
}

auto Response::findContentLength() const -> uint {
  if(auto contentLength = header["Content-Length"]) return contentLength.value().natural();
  if(_body) return _body.size();
  if(hasData()) return data().size();
  if(hasFile()) return file::size(file());
  if(hasText()) return text().size();
  return findResponseType().size();
}

auto Response::findContentType() const -> string {
  if(auto contentType = header["Content-Type"]) return contentType.value();
  if(hasData()) return "application/octet-stream";
  if(hasFile()) return findContentType(Location::suffix(file()));
  return "text/html; charset=utf-8";
}

auto Response::findContentType(const string& s) const -> string {
  if(s == ".7z"  ) return "application/x-7z-compressed";
  if(s == ".avi" ) return "video/avi";
  if(s == ".bml" ) return "text/plain; charset=utf-8";
  if(s == ".bz2" ) return "application/x-bzip2";
  if(s == ".css" ) return "text/css; charset=utf-8";
  if(s == ".gif" ) return "image/gif";
  if(s == ".gz"  ) return "application/gzip";
  if(s == ".htm" ) return "text/html; charset=utf-8";
  if(s == ".html") return "text/html; charset=utf-8";
  if(s == ".ico" ) return "image/x-icon";
  if(s == ".jpg" ) return "image/jpeg";
  if(s == ".jpeg") return "image/jpeg";
  if(s == ".js"  ) return "application/javascript";
  if(s == ".mka" ) return "audio/x-matroska";
  if(s == ".mkv" ) return "video/x-matroska";
  if(s == ".mp3" ) return "audio/mpeg";
  if(s == ".mp4" ) return "video/mp4";
  if(s == ".mpeg") return "video/mpeg";
  if(s == ".mpg" ) return "video/mpeg";
  if(s == ".ogg" ) return "audio/ogg";
  if(s == ".pdf" ) return "application/pdf";
  if(s == ".png" ) return "image/png";
  if(s == ".rar" ) return "application/x-rar-compressed";
  if(s == ".svg" ) return "image/svg+xml";
  if(s == ".tar" ) return "application/x-tar";
  if(s == ".txt" ) return "text/plain; charset=utf-8";
  if(s == ".wav" ) return "audio/vnd.wave";
  if(s == ".webm") return "video/webm";
  if(s == ".xml" ) return "text/xml; charset=utf-8";
  if(s == ".xz"  ) return "application/x-xz";
  if(s == ".zip" ) return "application/zip";
  return "application/octet-stream";  //binary
}

auto Response::findResponseType() const -> string {
  switch(responseType()) {
  case 200: return "200 OK";
  case 301: return "301 Moved Permanently";
  case 302: return "302 Found";
  case 303: return "303 See Other";
  case 304: return "304 Not Modified";
  case 307: return "307 Temporary Redirect";
  case 400: return "400 Bad Request";
  case 403: return "403 Forbidden";
  case 404: return "404 Not Found";
  case 500: return "500 Internal Server Error";
  case 501: return "501 Not Implemented";
  case 503: return "503 Service Unavailable";
  }
  return "501 Not Implemented";
}

auto Response::setData(const vector<uint8_t>& value) -> type& {
  _data = value;
  header.assign("Content-Length", value.size());
  return *this;
}

auto Response::setFile(const string& value) -> type& {
  //block path escalation exploits ("../" and "..\" in the file location)
  bool valid = true;
  for(uint n : range(value.size())) {
    if(value(n + 0, '\0') != '.') continue;
    if(value(n + 1, '\0') != '.') continue;
    if(value(n + 2, '\0') != '/' && value(n + 2, '\0') != '\\') continue;
    valid = false;
    break;
  }
  if(!valid) return *this;

  //cache images for seven days
  auto suffix = Location::suffix(value);
  uint maxAge = 0;
  if(suffix == ".svg"
  || suffix == ".ico"
  || suffix == ".png"
  || suffix == ".gif"
  || suffix == ".jpg"
  || suffix == ".jpeg") {
    maxAge = 7 * 24 * 60 * 60;
  }

  _file = value;
  header.assign("Content-Length", file::size(value));
  header.assign("ETag", {"\"", chrono::utc::datetime(file::timestamp(value, file::time::modify)), "\""});
  if(maxAge == 0) {
    header.assign("Cache-Control", {"public"});
  } else {
    header.assign("Cache-Control", {"public, max-age=", maxAge});
  }
  return *this;
}

auto Response::setText(const string& value) -> type& {
  _text = value;
  header.assign("Content-Length", value.size());
  return *this;
}

}

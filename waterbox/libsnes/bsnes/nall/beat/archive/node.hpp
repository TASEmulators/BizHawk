#pragma once

#include <nall/arithmetic.hpp>
#include <nall/array-view.hpp>
#include <nall/random.hpp>
#include <nall/cipher/chacha20.hpp>
#include <nall/elliptic-curve/ed25519.hpp>
#include <nall/decode/base.hpp>
#include <nall/encode/base.hpp>
#include <nall/decode/lzsa.hpp>
#include <nall/encode/lzsa.hpp>

namespace nall::Beat::Archive {

struct Node {
  static auto create(string name, string location) -> shared_pointer<Node>;
  static auto createPath(string name) -> shared_pointer<Node>;
  static auto createFile(string name, array_view<uint8_t> memory) -> shared_pointer<Node>;

  explicit operator bool() const { return (bool)name; }
  auto isPath() const -> bool { return  name.endsWith("/"); }
  auto isFile() const -> bool { return !name.endsWith("/"); }
  auto isCompressed() const -> bool { return (bool)compression.type; }

  auto metadata(bool indented = true) const -> string;
  auto compressLZSA() -> bool;

  auto unserialize(array_view<uint8_t> container, Markup::Node metadata) -> bool;
  auto decompress() -> bool;

  auto getTimestamp(string) const -> uint64_t;
  auto getPermissions() const -> uint;
  auto getOwner() const -> string;
  auto getGroup() const -> string;

  //files and paths
  string name;

  bool timestamps = false;
  struct Timestamp {
    string created;
    string modified;
    string accessed;
  } timestamp;

  bool permissions = false;
  struct Permission {
    struct Owner {
      string name;
      bool readable = false;
      bool writable = false;
      bool executable = false;
    } owner;
    struct Group {
      string name;
      bool readable = false;
      bool writable = false;
      bool executable = false;
    } group;
    struct Other {
      bool readable = false;
      bool writable = false;
      bool executable = false;
    } other;
  } permission;

  //files only
  vector<uint8_t> memory;
  uint64_t offset = 0;

  struct Compression {
    string type;
    uint size = 0;  //decompressed size; memory.size() == compressed size
  } compression;
};

auto Node::create(string name, string location) -> shared_pointer<Node> {
  if(!inode::exists(location)) return {};
  shared_pointer<Node> node = new Node;

  node->name = name;

  node->timestamps = true;
  node->timestamp.created  = chrono::utc::datetime(inode::timestamp(location, inode::time::create));
  node->timestamp.modified = chrono::utc::datetime(inode::timestamp(location, inode::time::modify));
  node->timestamp.accessed = chrono::utc::datetime(inode::timestamp(location, inode::time::access));

  uint mode = inode::mode(location);
  node->permissions = true;
  node->permission.owner.name = inode::owner(location);
  node->permission.group.name = inode::group(location);
  node->permission.owner.readable   = mode & 0400;
  node->permission.owner.writable   = mode & 0200;
  node->permission.owner.executable = mode & 0100;
  node->permission.group.readable   = mode & 0040;
  node->permission.group.writable   = mode & 0020;
  node->permission.group.executable = mode & 0010;
  node->permission.other.readable   = mode & 0004;
  node->permission.other.writable   = mode & 0002;
  node->permission.other.executable = mode & 0001;

  if(file::exists(location)) {
    node->memory = file::read(location);
  }

  return node;
}

auto Node::createPath(string name) -> shared_pointer<Node> {
  if(!name) return {};
  shared_pointer<Node> node = new Node;
  node->name = name;
  return node;
}

auto Node::createFile(string name, array_view<uint8_t> memory) -> shared_pointer<Node> {
  if(!name) return {};
  shared_pointer<Node> node = new Node;
  node->name = name;
  node->memory.resize(memory.size());
  memory::copy(node->memory.data(), memory.data(), memory.size());
  return node;
}

auto Node::metadata(bool indented) const -> string {
  string metadata;
  if(!name) return metadata;

  string indent;
  if(indented) {
    indent.append("  ");
    auto bytes = string{name}.trimRight("/");
    for(auto& byte : bytes) {
      if(byte == '/') indent.append("  ");
    }
  }

  if(isPath()) {
    metadata.append(indent, "path: ", name, "\n");
  }

  if(isFile()) {
    metadata.append(indent, "file: ", name, "\n");
  }

  if(timestamps) {
    metadata.append(indent, "  timestamp\n");
  if(timestamp.created  != timestamp.modified)
    metadata.append(indent, "    created: ", timestamp.created,  "\n");
    metadata.append(indent, "    modified: ", timestamp.modified, "\n");
  if(timestamp.accessed != timestamp.modified)
    metadata.append(indent, "    accessed: ", timestamp.accessed, "\n");
  }

  if(permissions) {
    metadata.append(indent, "  permission\n");
    metadata.append(indent, "    owner: ", permission.owner.name, "\n");
  if(permission.owner.readable)
    metadata.append(indent, "      readable\n");
  if(permission.owner.writable)
    metadata.append(indent, "      writable\n");
  if(permission.owner.executable)
    metadata.append(indent, "      executable\n");
    metadata.append(indent, "    group: ", permission.group.name, "\n");
  if(permission.group.readable)
    metadata.append(indent, "      readable\n");
  if(permission.group.writable)
    metadata.append(indent, "      writable\n");
  if(permission.group.executable)
    metadata.append(indent, "      executable\n");
    metadata.append(indent, "    other\n");
  if(permission.other.readable)
    metadata.append(indent, "      readable\n");
  if(permission.other.writable)
    metadata.append(indent, "      writable\n");
  if(permission.other.executable)
    metadata.append(indent, "      executable\n");
  }

  if(isFile()) {
    metadata.append(indent, "  offset: ", offset, "\n");
    if(!isCompressed()) {
      metadata.append(indent, "  size: ", memory.size(), "\n");
    } else {
      metadata.append(indent, "  size: ", compression.size, "\n");
      metadata.append(indent, "  compression: ", compression.type, "\n");
      metadata.append(indent, "    size: ", memory.size(), "\n");
    }
  }

  return metadata;
}

auto Node::unserialize(array_view<uint8_t> container, Markup::Node metadata) -> bool {
  *this = {};
  if(!metadata.text()) return false;

  name = metadata.text();

  if(auto node = metadata["timestamp"]) {
    timestamps = true;
    if(auto created  = node["created" ]) timestamp.created  = created.text();
    if(auto modified = node["modified"]) timestamp.modified = modified.text();
    if(auto accessed = node["accessed"]) timestamp.accessed = accessed.text();
  }

  if(auto node = metadata["permission"]) {
    permissions = true;
    if(auto owner = node["owner"]) {
      permission.owner.name = owner.text();
      permission.owner.readable   = (bool)owner["readable"];
      permission.owner.writable   = (bool)owner["writable"];
      permission.owner.executable = (bool)owner["executable"];
    }
    if(auto group = node["group"]) {
      permission.group.name = group.text();
      permission.group.readable   = (bool)group["readable"];
      permission.group.writable   = (bool)group["writable"];
      permission.group.executable = (bool)group["executable"];
    }
    if(auto other = node["other"]) {
      permission.other.readable   = (bool)other["readable"];
      permission.other.writable   = (bool)other["writable"];
      permission.other.executable = (bool)other["executable"];
    }
  }

  if(isPath()) return true;

  uint offset = metadata["offset"].natural();
  uint size = metadata["size"].natural();

  if(metadata["compression"]) {
    size = metadata["compression/size"].natural();
    compression.type = metadata["compression"].text();
  }

  if(offset + size >= container.size()) return false;

  memory.reallocate(size);
  nall::memory::copy(memory.data(), container.view(offset, size), size);
  return true;
}

auto Node::compressLZSA() -> bool {
  if(!memory) return true;  //don't compress empty files
  if(isCompressed()) return true;  //don't recompress files

  auto compressedMemory = Encode::LZSA(memory);
  if(compressedMemory.size() >= memory.size()) return true;  //can't compress smaller than original size

  compression.type = "lzsa";
  compression.size = memory.size();
  memory = move(compressedMemory);
  return true;
}

auto Node::decompress() -> bool {
  if(!isCompressed()) return true;

  if(compression.type == "lzsa") {
    compression = {};
    memory = Decode::LZSA(memory);
    return (bool)memory;
  }

  return false;
}

auto Node::getTimestamp(string type) const -> uint64_t {
  if(!timestamps) return time(nullptr);

  string value = chrono::utc::datetime();
  if(type == "created" ) value = timestamp.created;
  if(type == "modified") value = timestamp.modified;
  if(type == "accessed") value = timestamp.accessed;

  #if !defined(PLATFORM_WINDOWS)
  struct tm timeInfo{};
  if(strptime(value, "%Y-%m-%d %H:%M:%S", &timeInfo) != nullptr) {
    //todo: not thread safe ...
    auto tz = getenv("TZ");
    setenv("TZ", "", 1);
    timeInfo.tm_isdst = -1;
    auto result = mktime(&timeInfo);
    if(tz) setenv("TZ", tz, 1);
    else unsetenv("TZ");
    if(result != -1) return result;
  }
  #endif

  return time(nullptr);
}

auto Node::getPermissions() const -> uint {
  if(!permissions) return 0755;
  uint mode = 0;
  if(permission.owner.readable  ) mode |= 0400;
  if(permission.owner.writable  ) mode |= 0200;
  if(permission.owner.executable) mode |= 0100;
  if(permission.group.readable  ) mode |= 0040;
  if(permission.group.writable  ) mode |= 0020;
  if(permission.group.executable) mode |= 0010;
  if(permission.other.readable  ) mode |= 0004;
  if(permission.other.writable  ) mode |= 0002;
  if(permission.other.executable) mode |= 0001;
  return mode;
}

auto Node::getOwner() const -> string {
  if(!permissions || !permission.owner.name) {
    #if !defined(PLATFORM_WINDOWS)
    struct passwd* pwd = getpwuid(getuid());
    assert(pwd);
    return pwd->pw_name;
    #endif
  }
  return permission.owner.name;
}

auto Node::getGroup() const -> string {
  if(!permissions || !permission.group.name) {
    #if !defined(PLATFORM_WINDOWS)
    struct group* grp = getgrgid(getgid());
    assert(grp);
    return grp->gr_name;
    #endif
  }
  return permission.group.name;
}

}

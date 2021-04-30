#pragma once

//generic abstraction layer for common storage operations against both files and directories
//these functions are not recursive; use directory::create() and directory::remove() for recursion

#include <nall/platform.hpp>
#include <nall/string.hpp>

namespace nall {

struct inode {
  enum class time : uint { create, modify, access };

  inode() = delete;
  inode(const inode&) = delete;
  auto operator=(const inode&) -> inode& = delete;

  static auto exists(const string& name) -> bool {
    return access(name, F_OK) == 0;
  }

  static auto readable(const string& name) -> bool {
    return access(name, R_OK) == 0;
  }

  static auto writable(const string& name) -> bool {
    return access(name, W_OK) == 0;
  }

  static auto executable(const string& name) -> bool {
    return access(name, X_OK) == 0;
  }

  static auto hidden(const string& name) -> bool {
    #if defined(PLATFORM_WINDOWS)
    auto attributes = GetFileAttributes(utf16_t(name));
    return attributes & FILE_ATTRIBUTE_HIDDEN;
    #else
    //todo: is this really the best way to do this? stat doesn't have S_ISHIDDEN ...
    return name.split("/").last().beginsWith(".");
    #endif
  }

  static auto mode(const string& name) -> uint {
    struct stat data{};
    stat(name, &data);
    return data.st_mode;
  }

  static auto uid(const string& name) -> uint {
    struct stat data{};
    stat(name, &data);
    return data.st_uid;
  }

  static auto gid(const string& name) -> uint {
    struct stat data{};
    stat(name, &data);
    return data.st_gid;
  }

  static auto owner(const string& name) -> string {
    #if !defined(PLATFORM_WINDOWS)
    struct passwd* pw = getpwuid(uid(name));
    if(pw && pw->pw_name) return pw->pw_name;
    #endif
    return {};
  }

  static auto group(const string& name) -> string {
    #if !defined(PLATFORM_WINDOWS)
    struct group* gr = getgrgid(gid(name));
    if(gr && gr->gr_name) return gr->gr_name;
    #endif
    return {};
  }

  static auto timestamp(const string& name, time mode = time::modify) -> uint64_t {
    struct stat data{};
    stat(name, &data);
    switch(mode) {
    #if defined(PLATFORM_WINDOWS)
    //on Windows, the last status change time (ctime) holds the file creation time instead
    case time::create: return data.st_ctime;
    #elif defined(PLATFORM_BSD) || defined(PLATFORM_MACOS)
    //st_birthtime may return -1 or st_atime if it is not supported by the file system
    //the best that can be done in this case is to return st_mtime if it's older
    case time::create: return min((uint)data.st_birthtime, (uint)data.st_mtime);
    #else
    //Linux simply doesn't support file creation time at all
    //this is also our fallback case for unsupported operating systems
    case time::create: return data.st_mtime;
    #endif
    case time::modify: return data.st_mtime;
    //for performance reasons, last access time is usually not enabled on various filesystems
    //ensure that the last access time is not older than the last modify time (eg for NTFS)
    case time::access: return max((uint)data.st_atime, data.st_mtime);
    }
    return 0;
  }

  static auto setMode(const string& name, uint mode) -> bool {
    #if !defined(PLATFORM_WINDOWS)
    return chmod(name, mode) == 0;
    #else
    return _wchmod(utf16_t(name), (mode & 0400 ? _S_IREAD : 0) | (mode & 0200 ? _S_IWRITE : 0)) == 0;
    #endif
  }

  static auto setOwner(const string& name, const string& owner) -> bool {
    #if !defined(PLATFORM_WINDOWS)
    struct passwd* pwd = getpwnam(owner);
    if(!pwd) return false;
    return chown(name, pwd->pw_uid, inode::gid(name)) == 0;
    #else
    return true;
    #endif
  }

  static auto setGroup(const string& name, const string& group) -> bool {
    #if !defined(PLATFORM_WINDOWS)
    struct group* grp = getgrnam(group);
    if(!grp) return false;
    return chown(name, inode::uid(name), grp->gr_gid) == 0;
    #else
    return true;
    #endif
  }

  static auto setTimestamp(const string& name, uint64_t value, time mode = time::modify) -> bool {
    struct utimbuf timeBuffer;
    timeBuffer.modtime = mode == time::modify ? value : inode::timestamp(name, time::modify);
    timeBuffer.actime  = mode == time::access ? value : inode::timestamp(name, time::access);
    return utime(name, &timeBuffer) == 0;
  }

  //returns true if 'name' already exists
  static auto create(const string& name, uint permissions = 0755) -> bool {
    if(exists(name)) return true;
    if(name.endsWith("/")) return mkdir(name, permissions) == 0;
    int fd = open(name, O_CREAT | O_EXCL, permissions);
    if(fd < 0) return false;
    return close(fd), true;
  }

  //returns false if 'name' and 'targetname' are on different file systems (requires copy)
  static auto rename(const string& name, const string& targetname) -> bool {
    return ::rename(name, targetname) == 0;
  }

  //returns false if 'name' is a directory that is not empty
  static auto remove(const string& name) -> bool {
    #if defined(PLATFORM_WINDOWS)
    if(name.endsWith("/")) return _wrmdir(utf16_t(name)) == 0;
    return _wunlink(utf16_t(name)) == 0;
    #else
    if(name.endsWith("/")) return rmdir(name) == 0;
    return unlink(name) == 0;
    #endif
  }
};

}

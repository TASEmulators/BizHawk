#pragma once

#include <nall/function.hpp>
#include <nall/string.hpp>

namespace nall::chrono {

//passage of time functions (from unknown epoch)

inline auto nanosecond() -> uint64_t {
  timespec tv;
  clock_gettime(CLOCK_MONOTONIC, &tv);
  return tv.tv_sec * 1'000'000'000 + tv.tv_nsec;
}

inline auto microsecond() -> uint64_t { return nanosecond() / 1'000; }
inline auto millisecond() -> uint64_t { return nanosecond() / 1'000'000; }
inline auto second() -> uint64_t { return nanosecond() / 1'000'000'000; }

inline auto benchmark(const function<void ()>& f, uint64_t times = 1) -> void {
  auto start = nanosecond();
  while(times--) f();
  auto end = nanosecond();
  print("[chrono::benchmark] ", (double)(end - start) / 1'000'000'000.0, "s\n");
}

//exact date/time functions (from system epoch)

struct timeinfo {
  timeinfo(
    uint year = 0, uint month = 0, uint day = 0,
    uint hour = 0, uint minute = 0, uint second = 0, uint weekday = 0
  ) : year(year), month(month), day(day),
      hour(hour), minute(minute), second(second), weekday(weekday) {
  }

  inline explicit operator bool() const { return month; }

  uint year;     //...
  uint month;    //1 - 12
  uint day;      //1 - 31
  uint hour;     //0 - 23
  uint minute;   //0 - 59
  uint second;   //0 - 60
  uint weekday;  //0 - 6
};

inline auto timestamp() -> uint64_t {
  return ::time(nullptr);
}

//0 = failure condition
inline auto timestamp(const string& datetime) -> uint64_t {
  static const uint monthDays[] = {31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31};
  uint64_t timestamp = 0;
  if(datetime.match("??????????")) {
    return datetime.natural();
  }
  if(datetime.match("????*")) {
    uint year = datetime.slice(0, 4).natural();
    if(year < 1970 || year > 2199) return 0;
    for(uint y = 1970; y < year && y < 2999; y++) {
      uint daysInYear = 365;
      if(y % 4 == 0 && (y % 100 != 0 || y % 400 == 0)) daysInYear++;
      timestamp += daysInYear * 24 * 60 * 60;
    }
  }
  if(datetime.match("????-??*")) {
    uint y = datetime.slice(0, 4).natural();
    uint month = datetime.slice(5, 2).natural();
    if(month < 1 || month > 12) return 0;
    for(uint m = 1; m < month && m < 12; m++) {
      uint daysInMonth = monthDays[m - 1];
      if(m == 2 && y % 4 == 0 && (y % 100 != 0 || y % 400 == 0)) daysInMonth++;
      timestamp += daysInMonth * 24 * 60 * 60;
    }
  }
  if(datetime.match("????-??-??*")) {
    uint day = datetime.slice(8, 2).natural();
    if(day < 1 || day > 31) return 0;
    timestamp += (day - 1) * 24 * 60 * 60;
  }
  if(datetime.match("????-??-?? ??*")) {
    uint hour = datetime.slice(11, 2).natural();
    if(hour > 23) return 0;
    timestamp += hour * 60 * 60;
  }
  if(datetime.match("????-??-?? ??:??*")) {
    uint minute = datetime.slice(14, 2).natural();
    if(minute > 59) return 0;
    timestamp += minute * 60;
  }
  if(datetime.match("????-??-?? ??:??:??*")) {
    uint second = datetime.slice(17, 2).natural();
    if(second > 59) return 0;
    timestamp += second;
  }
  return timestamp;
}

namespace utc {
  inline auto timeinfo(uint64_t time = 0) -> chrono::timeinfo {
    auto stamp = time ? (time_t)time : (time_t)timestamp();
    auto info = gmtime(&stamp);
    return {
      (uint)info->tm_year + 1900,
      (uint)info->tm_mon + 1,
      (uint)info->tm_mday,
      (uint)info->tm_hour,
      (uint)info->tm_min,
      (uint)info->tm_sec,
      (uint)info->tm_wday
    };
  }

  inline auto year(uint64_t timestamp = 0) -> string { return pad(timeinfo(timestamp).year, 4, '0'); }
  inline auto month(uint64_t timestamp = 0) -> string { return pad(timeinfo(timestamp).month, 2, '0'); }
  inline auto day(uint64_t timestamp = 0) -> string { return pad(timeinfo(timestamp).day, 2, '0'); }
  inline auto hour(uint64_t timestamp = 0) -> string { return pad(timeinfo(timestamp).hour, 2, '0'); }
  inline auto minute(uint64_t timestamp = 0) -> string { return pad(timeinfo(timestamp).minute, 2, '0'); }
  inline auto second(uint64_t timestamp = 0) -> string { return pad(timeinfo(timestamp).second, 2, '0'); }

  inline auto date(uint64_t timestamp = 0) -> string {
    auto t = timeinfo(timestamp);
    return {pad(t.year, 4, '0'), "-", pad(t.month, 2, '0'), "-", pad(t.day, 2, '0')};
  }

  inline auto time(uint64_t timestamp = 0) -> string {
    auto t = timeinfo(timestamp);
    return {pad(t.hour, 2, '0'), ":", pad(t.minute, 2, '0'), ":", pad(t.second, 2, '0')};
  }

  inline auto datetime(uint64_t timestamp = 0) -> string {
    auto t = timeinfo(timestamp);
    return {
      pad(t.year, 4, '0'), "-", pad(t.month, 2, '0'), "-", pad(t.day, 2, '0'), " ",
      pad(t.hour, 2, '0'), ":", pad(t.minute, 2, '0'), ":", pad(t.second, 2, '0')
    };
  }
}

namespace local {
  inline auto timeinfo(uint64_t time = 0) -> chrono::timeinfo {
    auto stamp = time ? (time_t)time : (time_t)timestamp();
    auto info = localtime(&stamp);
    return {
      (uint)info->tm_year + 1900,
      (uint)info->tm_mon + 1,
      (uint)info->tm_mday,
      (uint)info->tm_hour,
      (uint)info->tm_min,
      (uint)info->tm_sec,
      (uint)info->tm_wday
    };
  }

  inline auto year(uint64_t timestamp = 0) -> string { return pad(timeinfo(timestamp).year, 4, '0'); }
  inline auto month(uint64_t timestamp = 0) -> string { return pad(timeinfo(timestamp).month, 2, '0'); }
  inline auto day(uint64_t timestamp = 0) -> string { return pad(timeinfo(timestamp).day, 2, '0'); }
  inline auto hour(uint64_t timestamp = 0) -> string { return pad(timeinfo(timestamp).hour, 2, '0'); }
  inline auto minute(uint64_t timestamp = 0) -> string { return pad(timeinfo(timestamp).minute, 2, '0'); }
  inline auto second(uint64_t timestamp = 0) -> string { return pad(timeinfo(timestamp).second, 2, '0'); }

  inline auto date(uint64_t timestamp = 0) -> string {
    auto t = timeinfo(timestamp);
    return {pad(t.year, 4, '0'), "-", pad(t.month, 2, '0'), "-", pad(t.day, 2, '0')};
  }

  inline auto time(uint64_t timestamp = 0) -> string {
    auto t = timeinfo(timestamp);
    return {pad(t.hour, 2, '0'), ":", pad(t.minute, 2, '0'), ":", pad(t.second, 2, '0')};
  }

  inline auto datetime(uint64_t timestamp = 0) -> string {
    auto t = timeinfo(timestamp);
    return {
      pad(t.year, 4, '0'), "-", pad(t.month, 2, '0'), "-", pad(t.day, 2, '0'), " ",
      pad(t.hour, 2, '0'), ":", pad(t.minute, 2, '0'), ":", pad(t.second, 2, '0')
    };
  }
}

}

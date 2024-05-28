#pragma once

#include <nall/function.hpp>
#include <nall/string.hpp>

#include <chrono>

namespace nall::chrono {

//passage of time functions (from unknown epoch)

inline auto nanosecond() -> u64 {
  auto now = std::chrono::steady_clock::now().time_since_epoch();
  return std::chrono::duration_cast<std::chrono::nanoseconds>(now).count();
}

inline auto microsecond() -> u64 { return nanosecond() / 1'000; }
inline auto millisecond() -> u64 { return nanosecond() / 1'000'000; }
inline auto second() -> u64 { return nanosecond() / 1'000'000'000; }

inline auto benchmark(const function<void ()>& f, u64 times = 1) -> void {
  auto start = nanosecond();
  while(times--) f();
  auto end = nanosecond();
  print("[chrono::benchmark] ", (double)(end - start) / 1'000'000'000.0, "s\n");
}

inline auto daysInMonth(u32 month, u32 year) -> u8 {
  u32 days = 30 + ((month + (month >> 3)) & 1);
  if (month == 2) days -= (year % 4 == 0) ? 1 : 2;
  return days;
}

//exact date/time functions (from system epoch)

struct timeinfo {
  timeinfo(u32 year = 0, u32 month = 0, u32 day = 0, u32 hour = 0, u32 minute = 0, u32 second = 0, u32 weekday = 0):
  year(year), month(month), day(day), hour(hour), minute(minute), second(second), weekday(weekday) {
  }

  explicit operator bool() const { return month; }

  u32 year;     //...
  u32 month;    //1 - 12
  u32 day;      //1 - 31
  u32 hour;     //0 - 23
  u32 minute;   //0 - 59
  u32 second;   //0 - 60
  u32 weekday;  //0 - 6
};

inline auto timestamp() -> u64 {
  return ::time(nullptr);
}

//0 = failure condition
inline auto timestamp(const string& datetime) -> u64 {
  static constexpr u32 monthDays[] = {31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31};
  u64 timestamp = 0;
  if(datetime.match("??????????")) {
    return datetime.natural();
  }
  if(datetime.match("????*")) {
    u32 year = datetime.slice(0, 4).natural();
    if(year < 1970 || year > 2199) return 0;
    for(u32 y = 1970; y < year && y < 2999; y++) {
      u32 daysInYear = 365;
      if(y % 4 == 0 && (y % 100 != 0 || y % 400 == 0)) daysInYear++;
      timestamp += daysInYear * 24 * 60 * 60;
    }
  }
  if(datetime.match(R"(????-??*)")) {
    u32 y = datetime.slice(0, 4).natural();
    u32 month = datetime.slice(5, 2).natural();
    if(month < 1 || month > 12) return 0;
    for(u32 m = 1; m < month && m < 12; m++) {
      u32 daysInMonth = monthDays[m - 1];
      if(m == 2 && y % 4 == 0 && (y % 100 != 0 || y % 400 == 0)) daysInMonth++;
      timestamp += daysInMonth * 24 * 60 * 60;
    }
  }
  if(datetime.match(R"(????-??-??*)")) {
    u32 day = datetime.slice(8, 2).natural();
    if(day < 1 || day > 31) return 0;
    timestamp += (day - 1) * 24 * 60 * 60;
  }
  if(datetime.match(R"(????-??-?? ??*)")) {
    u32 hour = datetime.slice(11, 2).natural();
    if(hour > 23) return 0;
    timestamp += hour * 60 * 60;
  }
  if(datetime.match(R"(????-??-?? ??:??*)")) {
    u32 minute = datetime.slice(14, 2).natural();
    if(minute > 59) return 0;
    timestamp += minute * 60;
  }
  if(datetime.match(R"(????-??-?? ??:??:??*)")) {
    u32 second = datetime.slice(17, 2).natural();
    if(second > 59) return 0;
    timestamp += second;
  }
  return timestamp;
}

namespace utc {
  inline auto timeinfo(u64 time = 0) -> chrono::timeinfo {
    auto stamp = time ? (time_t)time : (time_t)timestamp();
    auto info = gmtime(&stamp);
    return {
      (u32)info->tm_year + 1900,
      (u32)info->tm_mon + 1,
      (u32)info->tm_mday,
      (u32)info->tm_hour,
      (u32)info->tm_min,
      (u32)info->tm_sec,
      (u32)info->tm_wday
    };
  }

  inline auto year(u64 timestamp = 0) -> string { return pad(timeinfo(timestamp).year, 4, '0'); }
  inline auto month(u64 timestamp = 0) -> string { return pad(timeinfo(timestamp).month, 2, '0'); }
  inline auto day(u64 timestamp = 0) -> string { return pad(timeinfo(timestamp).day, 2, '0'); }
  inline auto hour(u64 timestamp = 0) -> string { return pad(timeinfo(timestamp).hour, 2, '0'); }
  inline auto minute(u64 timestamp = 0) -> string { return pad(timeinfo(timestamp).minute, 2, '0'); }
  inline auto second(u64 timestamp = 0) -> string { return pad(timeinfo(timestamp).second, 2, '0'); }

  inline auto date(u64 timestamp = 0) -> string {
    auto t = timeinfo(timestamp);
    return {pad(t.year, 4, '0'), "-", pad(t.month, 2, '0'), "-", pad(t.day, 2, '0')};
  }

  inline auto time(u64 timestamp = 0) -> string {
    auto t = timeinfo(timestamp);
    return {pad(t.hour, 2, '0'), ":", pad(t.minute, 2, '0'), ":", pad(t.second, 2, '0')};
  }

  inline auto datetime(u64 timestamp = 0) -> string {
    auto t = timeinfo(timestamp);
    return {
      pad(t.year, 4, '0'), "-", pad(t.month, 2, '0'), "-", pad(t.day, 2, '0'), " ",
      pad(t.hour, 2, '0'), ":", pad(t.minute, 2, '0'), ":", pad(t.second, 2, '0')
    };
  }
}

namespace local {
  inline auto timeinfo(u64 time = 0) -> chrono::timeinfo {
    auto stamp = time ? (time_t)time : (time_t)timestamp();
    auto info = localtime(&stamp);
    return {
      (u32)info->tm_year + 1900,
      (u32)info->tm_mon + 1,
      (u32)info->tm_mday,
      (u32)info->tm_hour,
      (u32)info->tm_min,
      (u32)info->tm_sec,
      (u32)info->tm_wday
    };
  }

  inline auto year(u64 timestamp = 0) -> string { return pad(timeinfo(timestamp).year, 4, '0'); }
  inline auto month(u64 timestamp = 0) -> string { return pad(timeinfo(timestamp).month, 2, '0'); }
  inline auto day(u64 timestamp = 0) -> string { return pad(timeinfo(timestamp).day, 2, '0'); }
  inline auto hour(u64 timestamp = 0) -> string { return pad(timeinfo(timestamp).hour, 2, '0'); }
  inline auto minute(u64 timestamp = 0) -> string { return pad(timeinfo(timestamp).minute, 2, '0'); }
  inline auto second(u64 timestamp = 0) -> string { return pad(timeinfo(timestamp).second, 2, '0'); }

  inline auto date(u64 timestamp = 0) -> string {
    auto t = timeinfo(timestamp);
    return {pad(t.year, 4, '0'), "-", pad(t.month, 2, '0'), "-", pad(t.day, 2, '0')};
  }

  inline auto time(u64 timestamp = 0) -> string {
    auto t = timeinfo(timestamp);
    return {pad(t.hour, 2, '0'), ":", pad(t.minute, 2, '0'), ":", pad(t.second, 2, '0')};
  }

  inline auto datetime(u64 timestamp = 0) -> string {
    auto t = timeinfo(timestamp);
    return {
      pad(t.year, 4, '0'), "-", pad(t.month, 2, '0'), "-", pad(t.day, 2, '0'), " ",
      pad(t.hour, 2, '0'), ":", pad(t.minute, 2, '0'), ":", pad(t.second, 2, '0')
    };
  }
}

}

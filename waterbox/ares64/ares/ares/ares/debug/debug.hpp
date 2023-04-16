namespace ares {

struct Debug {
  auto reset() -> void;

  template<typename... P> auto unhandled(P&&... p) -> void {
    return _unhandled({forward<P>(p)...});
  }

  template<typename... P> auto unimplemented(P&&... p) -> void {
    return _unimplemented({forward<P>(p)...});
  }

  template<typename... P> auto unusual(P&&... p) -> void {
    return _unusual({forward<P>(p)...});
  }

  template<typename... P> auto unverified(P&&... p) -> void {
    return _unverified({forward<P>(p)...});
  }

private:
  auto _unhandled(const string&) -> void;
  auto _unimplemented(const string&) -> void;
  auto _unusual(const string&) -> void;
  auto _unverified(const string&) -> void;

  u64 _totalNotices = 0;
  vector<string> _unhandledNotices;
  vector<string> _unimplementedNotices;
  vector<string> _unusualNotices;
  vector<string> _unverifiedNotices;
};

extern Debug _debug;

}

#define debug(function, ...) if constexpr(1) ares::_debug.function(__VA_ARGS__)

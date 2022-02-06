namespace ares {

Debug _debug;

auto Debug::reset() -> void {
  _totalNotices = 0;
  _unhandledNotices.reset();
  _unimplementedNotices.reset();
  _unusualNotices.reset();
  _unverifiedNotices.reset();
}

auto Debug::_unhandled(const string& text) -> void {
  if(_unhandledNotices.find(text)) return;
  if(_totalNotices++ > 256) return;
  _unhandledNotices.append(text);

  print(terminal::color::yellow("[unhandled] "), text, "\n");
}

auto Debug::_unimplemented(const string& text) -> void {
  if(_unimplementedNotices.find(text)) return;
  if(_totalNotices++ > 256) return;
  _unimplementedNotices.append(text);

  print(terminal::color::magenta("[unimplemented] "), text, "\n");
}

auto Debug::_unusual(const string& text) -> void {
  if(_unusualNotices.find(text)) return;
  if(_totalNotices++ > 256) return;
  _unusualNotices.append(text);

  print(terminal::color::cyan("[unusual] "), text, "\n");
}

auto Debug::_unverified(const string& text) -> void {
  if(_unverifiedNotices.find(text)) return;
  if(_totalNotices++ > 256) return;
  _unverifiedNotices.append(text);

  print(terminal::color::gray("[unverified] "), text, "\n");
}

}

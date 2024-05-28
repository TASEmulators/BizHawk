struct Notification : Tracer {
  DeclareClass(Notification, "debugger.tracer.notification")

  Notification(string name = {}, string component = {}) : Tracer(name, component) {
  }

  auto notify(const string& message = {}) -> void {
    if(!enabled()) return;
    PlatformLog(shared(), message);
  }

protected:
};

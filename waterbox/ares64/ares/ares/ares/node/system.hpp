struct System : Object {
  DeclareClass(System, "system")
  using Object::Object;

  auto game() -> string { if(_game) return _game(); return {}; }
  auto run() -> void { if(_run) return _run(); }
  auto power(bool reset = false) -> void { if(_power) return _power(reset); }
  auto save() -> void { if(_save) return _save(); }
  auto unload() -> void { if(_unload) return _unload(); }
  auto serialize(bool synchronize = true) -> serializer { if(_serialize) return _serialize(synchronize); return {}; }
  auto unserialize(serializer& s) -> bool { if(_unserialize) return _unserialize(s); return false; }

  auto setGame(function<string ()> game) -> void { _game = game; }
  auto setRun(function<void ()> run) -> void { _run = run; }
  auto setPower(function<void (bool)> power) -> void { _power = power; }
  auto setSave(function<void ()> save) -> void { _save = save; }
  auto setUnload(function<void ()> unload) -> void { _unload = unload; }
  auto setSerialize(function<serializer (bool)> serialize) -> void { _serialize = serialize; }
  auto setUnserialize(function<bool (serializer&)> unserialize) -> void { _unserialize = unserialize; }

protected:
  function<string ()> _game;
  function<void ()> _run;
  function<void (bool)> _power;
  function<void ()> _save;
  function<void ()> _unload;
  function<serializer (bool)> _serialize;
  function<bool (serializer&)> _unserialize;
};

#pragma once

#include <nall/maybe.hpp>
#include <nall/range.hpp>
#include <nall/string.hpp>
#include <nall/vector.hpp>

namespace nall::HID {

struct Input {
  Input(const string& name) : _name(name) {}

  auto name() const -> string { return _name; }
  auto value() const -> int16_t { return _value; }
  auto setValue(int16_t value) -> void { _value = value; }

private:
  string _name;
  int16_t _value = 0;
  friend class Group;
};

struct Group : vector<Input> {
  Group(const string& name) : _name(name) {}

  auto name() const -> string { return _name; }
  auto input(uint id) -> Input& { return operator[](id); }
  auto append(const string& name) -> void { vector::append(Input{name}); }

  auto find(const string& name) const -> maybe<uint> {
    for(auto id : range(size())) {
      if(operator[](id)._name == name) return id;
    }
    return nothing;
  }

private:
  string _name;
  friend class Device;
};

struct Device : vector<Group> {
  Device(const string& name) : _name(name) {}

  //id => {pathID}-{vendorID}-{productID}
  auto pathID()    const -> uint32_t { return (uint32_t)(_id >> 32); }  //32-63
  auto vendorID()  const -> uint16_t { return (uint16_t)(_id >> 16); }  //16-31
  auto productID() const -> uint16_t { return (uint16_t)(_id >>  0); }  // 0-15

  auto setPathID   (uint32_t pathID   ) -> void { _id = (uint64_t)pathID   << 32 | vendorID() << 16 | productID() << 0; }
  auto setVendorID (uint16_t vendorID ) -> void { _id = (uint64_t)pathID() << 32 | vendorID   << 16 | productID() << 0; }
  auto setProductID(uint16_t productID) -> void { _id = (uint64_t)pathID() << 32 | vendorID() << 16 | productID   << 0; }

  virtual auto isNull() const -> bool { return false; }
  virtual auto isKeyboard() const -> bool { return false; }
  virtual auto isMouse() const -> bool { return false; }
  virtual auto isJoypad() const -> bool { return false; }

  auto name() const -> string { return _name; }
  auto id() const -> uint64_t { return _id; }
  auto setID(uint64_t id) -> void { _id = id; }
  auto group(uint id) -> Group& { return operator[](id); }
  auto append(const string& name) -> void { vector::append(Group{name}); }

  auto find(const string& name) const -> maybe<uint> {
    for(auto id : range(size())) {
      if(operator[](id)._name == name) return id;
    }
    return nothing;
  }

private:
  string _name;
  uint64_t _id = 0;
};

struct Null : Device {
  enum : uint16_t { GenericVendorID = 0x0000, GenericProductID = 0x0000 };

  Null() : Device("Null") {}
  auto isNull() const -> bool { return true; }
};

struct Keyboard : Device {
  enum : uint16_t { GenericVendorID = 0x0000, GenericProductID = 0x0001 };
  enum GroupID : uint { Button };

  Keyboard() : Device("Keyboard") { append("Button"); }
  auto isKeyboard() const -> bool { return true; }
  auto buttons() -> Group& { return group(GroupID::Button); }
};

struct Mouse : Device {
  enum : uint16_t { GenericVendorID = 0x0000, GenericProductID = 0x0002 };
  enum GroupID : uint { Axis, Button };

  Mouse() : Device("Mouse") { append("Axis"), append("Button"); }
  auto isMouse() const -> bool { return true; }
  auto axes() -> Group& { return group(GroupID::Axis); }
  auto buttons() -> Group& { return group(GroupID::Button); }
};

struct Joypad : Device {
  enum : uint16_t { GenericVendorID = 0x0000, GenericProductID = 0x0003 };
  enum GroupID : uint { Axis, Hat, Trigger, Button };

  Joypad() : Device("Joypad") { append("Axis"), append("Hat"), append("Trigger"), append("Button"); }
  auto isJoypad() const -> bool { return true; }
  auto axes() -> Group& { return group(GroupID::Axis); }
  auto hats() -> Group& { return group(GroupID::Hat); }
  auto triggers() -> Group& { return group(GroupID::Trigger); }
  auto buttons() -> Group& { return group(GroupID::Button); }

  auto rumble() const -> bool { return _rumble; }
  auto setRumble(bool rumble) -> void { _rumble = rumble; }

private:
  bool _rumble = false;
};

}

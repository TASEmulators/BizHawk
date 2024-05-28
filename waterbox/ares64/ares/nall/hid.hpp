#pragma once

#include <nall/maybe.hpp>
#include <nall/range.hpp>
#include <nall/string.hpp>
#include <nall/vector.hpp>

namespace nall::HID {

struct Input {
  Input(const string& name) : _name(name) {}

  auto name() const -> string { return _name; }
  auto value() const -> s16 { return _value; }
  auto setValue(s16 value) -> void { _value = value; }

private:
  string _name;
  s16 _value = 0;
  friend struct Group;
};

struct Group : vector<Input> {
  Group(const string& name) : _name(name) {}

  auto name() const -> string { return _name; }
  auto input(u32 id) -> Input& { return operator[](id); }
  auto append(const string& name) -> void { vector::append(Input{name}); }

  auto find(const string& name) const -> maybe<u32> {
    for(auto id : range(size())) {
      if(operator[](id)._name == name) return id;
    }
    return nothing;
  }

private:
  string _name;
  friend struct Device;
};

struct Device : vector<Group> {
  Device(const string& name) : _name(name) {}
  virtual ~Device() = default;

  //id => {pathID}-{vendorID}-{productID}
  auto pathID()    const -> u32 { return (u32)(_id >> 32); }  //32-63
  auto vendorID()  const -> u16 { return (u16)(_id >> 16); }  //16-31
  auto productID() const -> u16 { return (u16)(_id >>  0); }  // 0-15

  auto setPathID   (u32 pathID   ) -> void { _id = (u64)pathID   << 32 | vendorID() << 16 | productID() << 0; }
  auto setVendorID (u16 vendorID ) -> void { _id = (u64)pathID() << 32 | vendorID   << 16 | productID() << 0; }
  auto setProductID(u16 productID) -> void { _id = (u64)pathID() << 32 | vendorID() << 16 | productID   << 0; }

  virtual auto isNull() const -> bool { return false; }
  virtual auto isKeyboard() const -> bool { return false; }
  virtual auto isMouse() const -> bool { return false; }
  virtual auto isJoypad() const -> bool { return false; }

  auto name() const -> string { return _name; }
  auto id() const -> u64 { return _id; }
  auto setID(u64 id) -> void { _id = id; }
  auto group(u32 id) -> Group& { return operator[](id); }
  auto append(const string& name) -> void { vector::append(Group{name}); }

  auto find(const string& name) const -> maybe<u32> {
    for(auto id : range(size())) {
      if(operator[](id)._name == name) return id;
    }
    return nothing;
  }

private:
  string _name;
  u64 _id = 0;
};

struct Null : Device {
  enum : u16 { GenericVendorID = 0x0000, GenericProductID = 0x0000 };

  Null() : Device("Null") {}
  auto isNull() const -> bool { return true; }
};

struct Keyboard : Device {
  enum : u16 { GenericVendorID = 0x0000, GenericProductID = 0x0001 };
  enum GroupID : u32 { Button };

  Keyboard() : Device("Keyboard") { append("Button"); }
  auto isKeyboard() const -> bool { return true; }
  auto buttons() -> Group& { return group(GroupID::Button); }
};

struct Mouse : Device {
  enum : u16 { GenericVendorID = 0x0000, GenericProductID = 0x0002 };
  enum GroupID : u32 { Axis, Button };

  Mouse() : Device("Mouse") { append("Axis"), append("Button"); }
  auto isMouse() const -> bool { return true; }
  auto axes() -> Group& { return group(GroupID::Axis); }
  auto buttons() -> Group& { return group(GroupID::Button); }
};

struct Joypad : Device {
  enum : u16 { GenericVendorID = 0x0000, GenericProductID = 0x0003 };
  enum GroupID : u32 { Axis, Hat, Trigger, Button };

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

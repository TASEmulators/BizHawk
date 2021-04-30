#pragma once

namespace Emulator {

struct Game {
  struct Memory;
  struct Oscillator;

  inline auto load(string_view) -> void;
  inline auto memory(Markup::Node) -> maybe<Memory>;
  inline auto oscillator(natural = 0) -> maybe<Oscillator>;

  struct Memory {
    Memory() = default;
    inline Memory(Markup::Node);
    explicit operator bool() const { return (bool)type; }
    inline auto name() const -> string;

    string type;
    natural size;
    string content;
    string manufacturer;
    string architecture;
    string identifier;
    boolean nonVolatile;
  };

  struct Oscillator {
    Oscillator() = default;
    inline Oscillator(Markup::Node);
    explicit operator bool() const { return frequency; }

    natural frequency;
  };

  Markup::Node document;
  string sha256;
  string label;
  string name;
  string title;
  string region;
  string revision;
  string board;
  vector<Memory> memoryList;
  vector<Oscillator> oscillatorList;
};

auto Game::load(string_view text) -> void {
  document = BML::unserialize(text);

  sha256 = document["game/sha256"].text();
  label = document["game/label"].text();
  name = document["game/name"].text();
  title = document["game/title"].text();
  region = document["game/region"].text();
  revision = document["game/revision"].text();
  board = document["game/board"].text();

  for(auto node : document.find("game/board/memory")) {
    memoryList.append(Memory{node});
  }

  for(auto node : document.find("game/board/oscillator")) {
    oscillatorList.append(Oscillator{node});
  }
}

auto Game::memory(Markup::Node node) -> maybe<Memory> {
  if(!node) return nothing;
  for(auto& memory : memoryList) {
    auto type = node["type"].text();
    auto size = node["size"].natural();
    auto content = node["content"].text();
    auto manufacturer = node["manufacturer"].text();
    auto architecture = node["architecture"].text();
    auto identifier = node["identifier"].text();
    if(type && type != memory.type) continue;
    if(size && size != memory.size) continue;
    if(content && content != memory.content) continue;
    if(manufacturer && manufacturer != memory.manufacturer) continue;
    if(architecture && architecture != memory.architecture) continue;
    if(identifier && identifier != memory.identifier) continue;
    return memory;
  }
  return nothing;
}

auto Game::oscillator(natural index) -> maybe<Oscillator> {
  if(index < oscillatorList.size()) return oscillatorList[index];
  return nothing;
}

Game::Memory::Memory(Markup::Node node) {
  type = node["type"].text();
  size = node["size"].natural();
  content = node["content"].text();
  manufacturer = node["manufacturer"].text();
  architecture = node["architecture"].text();
  identifier = node["identifier"].text();
  nonVolatile = !(bool)node["volatile"];
}

auto Game::Memory::name() const -> string {
  if(architecture) return string{architecture, ".", content, ".", type}.downcase();
  return string{content, ".", type}.downcase();
}

Game::Oscillator::Oscillator(Markup::Node node) {
  frequency = node["frequency"].natural();
}

}

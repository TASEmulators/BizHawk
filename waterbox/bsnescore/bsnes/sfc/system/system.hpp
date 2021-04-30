struct System {
  enum class Region : uint { NTSC, PAL };

  inline auto loaded() const -> bool { return information.loaded; }
  inline auto region() const -> Region { return information.region; }
  inline auto cpuFrequency() const -> double { return information.cpuFrequency; }
  inline auto apuFrequency() const -> double { return information.apuFrequency; }

  inline auto fastPPU() const -> bool { return hacks.fastPPU; }

  auto run() -> void;
  auto runToSave() -> void;
  auto runToSaveFast() -> void;
  auto runToSaveStrict() -> void;
  auto frameEvent() -> void;

  auto load(Emulator::Interface*) -> bool;
  auto save() -> void;
  auto unload() -> void;
  auto power(bool reset) -> void;

  //serialization.cpp
  auto serialize(bool synchronize) -> serializer;
  auto unserialize(serializer&) -> bool;

  uint frameSkip = 0;
  uint frameCounter = 0;
  bool runAhead = 0;

private:
  Emulator::Interface* interface = nullptr;

  struct Information {
    bool loaded = false;
    Region region = Region::NTSC;
    double cpuFrequency = Emulator::Constants::Colorburst::NTSC * 6.0;
    double apuFrequency = 32040.0 * 768.0;
    uint serializeSize[2] = {0, 0};
  } information;

  struct Hacks {
    bool fastPPU = false;
  } hacks;

  auto serializeAll(serializer&, bool synchronize) -> void;
  auto serializeInit(bool synchronize) -> uint;

  friend class Cartridge;
};

extern System system;

auto Region::NTSC() -> bool { return system.region() == System::Region::NTSC; }
auto Region::PAL() -> bool { return system.region() == System::Region::PAL; }

#pragma once

#include "../nesInstanceBase.hpp"
#include "core/nes_emu/Nes_Emu.h"
#include "core/nes_emu/Nes_State.h"
#include "jaffarCommon/deserializers/base.hpp"
#include "jaffarCommon/serializers/base.hpp"

#define _DUMMY_SIZE 65536

typedef Nes_Emu emulator_t;

extern void register_misc_mappers();
extern void register_extra_mappers();
extern void register_mapper_70();

class NESInstance final : public NESInstanceBase
{
  public:
  NESInstance(const nlohmann::json &config) : NESInstanceBase(config)
  {
    // If running the original QuickNES, register extra mappers now
    register_misc_mappers();
    register_extra_mappers();
    register_mapper_70();
  }

  uint8_t *getLowMem() const override { return _nes.low_mem(); };
  size_t getLowMemSize() const override { return 0x800; };

  void serializeState(jaffarCommon::serializer::Base &serializer) const override
  {
    Mem_Writer w(serializer.getOutputDataBuffer(), _stateSize, 0);
    Auto_File_Writer a(w);
    _nes.save_state(a);
  }

  void deserializeState(jaffarCommon::deserializer::Base &deserializer) override
  {
    Mem_File_Reader r(deserializer.getInputDataBuffer(), _stateSize);
    Auto_File_Reader a(r);
    _nes.load_state(a);
  }

  inline size_t getFullStateSize() const override
  {
    uint8_t *data = (uint8_t *)malloc(_DUMMY_SIZE);
    Mem_Writer w(data, _DUMMY_SIZE);
    Auto_File_Writer a(w);
    _nes.save_state(a);
    free(data);
    return w.size();
  }

  inline size_t getDifferentialStateSize() const override { return getFullStateSize(); }

  std::string getCoreName() const override { return "QuickNES"; }
  void doSoftReset() override { _nes.reset(false); }
  void doHardReset() override { _nes.reset(true); }

  void *getInternalEmulatorPointer() override { return &_nes; }

  void advanceState(const jaffar::input_t &input) override
  {
    if (_doRendering == true) _nes.emulate_frame(input.port1, input.port2);
    if (_doRendering == false) _nes.emulate_skip_frame(input.port1, input.port2);
  }

  protected:
  bool loadROMImpl(const uint8_t *romData, const size_t romSize) override
  {
    // Loading rom data
    Mem_File_Reader romReader(romData, (int)romSize);
    Auto_File_Reader romFile(romReader);
    auto result = _nes.load_ines(romFile);
    return result == 0;
  }

  void enableStateBlockImpl(const std::string &block) override {};
  void disableStateBlockImpl(const std::string &block) override {};

  private:
  // Emulator instance
  emulator_t _nes;
};

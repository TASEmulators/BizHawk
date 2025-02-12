#pragma once

#include "inputParser.hpp"
#include "jaffarCommon/logger.hpp"
#include "jaffarCommon/serializers/contiguous.hpp"
#include "jaffarCommon/serializers/differential.hpp"
#include "jaffarCommon/deserializers/base.hpp"

// Size of image generated in graphics buffer
static const uint16_t image_width = 256;
static const uint16_t image_height = 240;

class NESInstanceBase
{
  public:
  NESInstanceBase(const nlohmann::json &config)
  {
    _inputParser = std::make_unique<jaffar::InputParser>(config);
  }

  virtual ~NESInstanceBase() = default;

  virtual void advanceState(const jaffar::input_t &input) = 0;

  inline void enableRendering() { _doRendering = true; };
  inline void disableRendering() { _doRendering = false; };

  inline bool loadROM(const uint8_t *romData, const size_t romSize)
  {
    // Actually loading rom file
    auto status = loadROMImpl(romData, romSize);

    // Detecting full state size
    _stateSize = getFullStateSize();

    // Returning status
    return status;
  }

  void enableStateBlock(const std::string &block)
  {
    // Calling implementation
    enableStateBlockImpl(block);

    // Recalculating State size
    _stateSize = getFullStateSize();
  }

  void disableStateBlock(const std::string &block)
  {
    // Calling implementation
    disableStateBlockImpl(block);

    // Recalculating State Size
    _stateSize = getFullStateSize();
  }

  virtual size_t getFullStateSize() const = 0;
  virtual size_t getDifferentialStateSize() const = 0;
  inline jaffar::InputParser *getInputParser() const { return _inputParser.get(); }

  // Virtual functions

  virtual uint8_t *getLowMem() const = 0;
  virtual size_t getLowMemSize() const = 0;

  virtual void serializeState(jaffarCommon::serializer::Base &serializer) const = 0;
  virtual void deserializeState(jaffarCommon::deserializer::Base &deserializer) = 0;

  virtual void doSoftReset() = 0;
  virtual void doHardReset() = 0;
  virtual std::string getCoreName() const = 0;
  virtual void *getInternalEmulatorPointer() = 0;
  virtual void setNTABBlockSize(const size_t size) {};

  protected:
  virtual void enableStateBlockImpl(const std::string &block) = 0;
  virtual void disableStateBlockImpl(const std::string &block) = 0;
  virtual bool loadROMImpl(const uint8_t *romData, const size_t romSize) = 0;

  // Storage for the light state size
  size_t _stateSize;

  // Flag to determine whether to enable/disable rendering
  bool _doRendering = true;

  // Input parser instance
  std::unique_ptr<jaffar::InputParser> _inputParser;
};

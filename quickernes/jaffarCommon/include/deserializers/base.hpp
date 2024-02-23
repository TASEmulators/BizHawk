#pragma once

/**
 * @file base.hpp
 * @brief Contains the base class for the data deserializers
 */

namespace jaffarCommon
{

namespace deserializer
{

class Base 
{
  public:

  Base(
    const void* __restrict__ inputDataBuffer, 
    const size_t inputDataBufferSize
  ) :
   _inputDataBuffer((const uint8_t*)inputDataBuffer),
   _inputDataBufferSize(inputDataBufferSize)
  {  }

  virtual ~Base() = default;

  virtual void pop(void* const __restrict__ outputDataBuffer, const size_t outputDataSize) = 0;
  virtual void popContiguous(void* const __restrict__ outputDataBuffer, const size_t outputDataSize) = 0;
  inline size_t getInputSize() const { return _inputDataBufferPos; }
  inline const uint8_t* getInputDataBuffer() const { return _inputDataBuffer; }

  protected:

  const uint8_t* __restrict__ const _inputDataBuffer;
  const size_t _inputDataBufferSize;
  size_t _inputDataBufferPos = 0;
};

} // namespace deserializer

} // namespace jaffarCommon
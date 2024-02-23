#pragma once

/**
 * @file base.hpp
 * @brief Contains the base class for the data serializers
 */

namespace jaffarCommon
{

namespace serializer
{

class Base 
{
  public:

  Base(
    void* __restrict__ outputDataBuffer, 
    const size_t outputDataBufferSize
  ) :
   _outputDataBuffer((uint8_t*)outputDataBuffer),
   _outputDataBufferSize(outputDataBufferSize)
  {  }

  virtual ~Base() = default;

  virtual void push(const void* const __restrict__ inputData, const size_t inputDataSize) = 0;
  virtual void pushContiguous(const void* const __restrict__ inputData, const size_t inputDataSize) = 0;
  inline size_t getOutputSize() const { return _outputDataBufferPos; }
  inline uint8_t* getOutputDataBuffer() const { return _outputDataBuffer; }

  protected:

  uint8_t* __restrict__ const _outputDataBuffer;
  const size_t _outputDataBufferSize;
  size_t _outputDataBufferPos = 0;
};

} // namespace serializer

} // namespace jaffarCommon
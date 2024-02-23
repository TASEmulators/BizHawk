#pragma once

/**
 * @file contiguous.hpp
 * @brief Contains the contiguous data serializer
 */

#include <stdexcept>
#include <cstring>
#include <limits>
#include "base.hpp"

namespace jaffarCommon
{

namespace serializer
{

class Contiguous final : public serializer::Base
{
  public:

  Contiguous(
    void* __restrict outputDataBuffer = nullptr, 
    const size_t outputDataBufferSize = std::numeric_limits<uint32_t>::max()
  ) : serializer::Base(outputDataBuffer, outputDataBufferSize)
  {  }

  ~Contiguous() = default;

  inline void pushContiguous(const void* const __restrict inputData, const size_t inputDataSize) override
  {
    // Only perform memcpy if the output block is not null
    if (_outputDataBuffer != nullptr) memcpy(&_outputDataBuffer[_outputDataBufferPos], inputData, inputDataSize);

    // Moving output data pointer position
    _outputDataBufferPos += inputDataSize;

    // Making sure we do not exceed the maximum size estipulated
    if (_outputDataBufferPos > _outputDataBufferSize) throw std::runtime_error("Maximum output data position reached before contiguous serialization");
  }

  inline void push(const void* const __restrict inputData, const size_t inputDataSize) override
  {
    pushContiguous(inputData, inputDataSize);
  }

};


} // namespace serializer

} // namespace jaffarCommon
#pragma once

/**
 * @file contiguous.hpp
 * @brief Contains the contiguous data deserializer
 */

#include <stdexcept>
#include <cstring>
#include <limits>
#include "base.hpp"

namespace jaffarCommon
{

namespace deserializer
{

class Contiguous final : public deserializer::Base
{
  public:

  Contiguous(
    const void* __restrict__ inputDataBuffer = nullptr, 
    const size_t inputDataBufferSize = std::numeric_limits<uint32_t>::max()
  ) : deserializer::Base(inputDataBuffer, inputDataBufferSize)
  {  }

  ~Contiguous() = default;

  inline void popContiguous(void* const __restrict__ outputDataBuffer, const size_t outputDataSize) override
  { 
    // Making sure we do not exceed the maximum size estipulated
    if (_inputDataBufferPos  + outputDataSize> _inputDataBufferSize) throw std::runtime_error("Maximum input data position reached before contiguous deserialization");

    // Only perform memcpy if the input block is not null
    if (_inputDataBuffer != nullptr) memcpy(outputDataBuffer, &_inputDataBuffer[_inputDataBufferPos], outputDataSize);

    // Moving input data pointer position
    _inputDataBufferPos += outputDataSize;
  }

  inline void pop(void* const __restrict__ outputDataBuffer, const size_t outputDataSize) override
  {
    popContiguous(outputDataBuffer, outputDataSize);
  }

};


} // namespace deserializer

} // namespace jaffarCommon
#pragma once

//matrix multiplication primitives
//used in: ruby/opengl/quark

namespace nall {

template<typename T> inline auto MatrixMultiply(
T* output,
const T* xdata, uint xrows, uint xcols,
const T* ydata, uint yrows, uint ycols
) -> void {
  if(xcols != yrows) return;

  for(uint y : range(xrows)) {
    for(uint x : range(ycols)) {
      T sum = 0;
      for(uint z : range(xcols)) {
        sum += xdata[y * xcols + z] * ydata[z * ycols + x];
      }
      *output++ = sum;
    }
  }
}

template<typename T> inline auto MatrixMultiply(
const T* xdata, uint xrows, uint xcols,
const T* ydata, uint yrows, uint ycols
) -> vector<T> {
  vector<T> output;
  output.resize(xrows * ycols);
  MatrixMultiply(output.data(), xdata, xrows, xcols, ydata, yrows, ycols);
  return output;
}

}

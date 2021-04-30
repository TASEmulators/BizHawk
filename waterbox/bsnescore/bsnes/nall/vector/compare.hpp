#pragma once

namespace nall {

template<typename T> auto vector<T>::operator==(const vector<T>& source) const -> bool {
  if(this == &source) return true;
  if(size() != source.size()) return false;
  for(uint64_t n = 0; n < size(); n++) {
    if(operator[](n) != source[n]) return false;
  }
  return true;
}

template<typename T> auto vector<T>::operator!=(const vector<T>& source) const -> bool {
  return !operator==(source);
}

}

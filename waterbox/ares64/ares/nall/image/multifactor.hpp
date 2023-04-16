#pragma once

namespace nall {

inline multiFactorImage::multiFactorImage(const multiFactorImage& source) {
  (*this) = source;
}

inline multiFactorImage::multiFactorImage(multiFactorImage&& source) {
  operator=(forward<multiFactorImage>(source));
}

inline multiFactorImage::multiFactorImage(const image& lowDPI, const image& highDPI) {
  (*(image*)this) = lowDPI;
  _highDPI = highDPI;
}

inline multiFactorImage::multiFactorImage(const image& source) {
    (*(image*)this) = source;
}

inline multiFactorImage::multiFactorImage(image&& source) {
    operator=(forward<multiFactorImage>(source));
}

inline multiFactorImage::multiFactorImage() {
}

inline multiFactorImage::~multiFactorImage() {
}

inline auto multiFactorImage::operator=(const multiFactorImage& source) -> multiFactorImage& {
  if(this == &source) return *this;
  
  (*(image*)this) = source;
  _highDPI = source._highDPI;

  return *this;
}

inline auto multiFactorImage::operator=(multiFactorImage&& source) -> multiFactorImage& {
  if(this == &source) return *this;

  (*(image*)this) = source;
  _highDPI = source._highDPI;

  return *this;
}

inline auto multiFactorImage::operator==(const multiFactorImage& source) const -> bool {
  if((const image&)*this != (const image&)source) return false;
  return _highDPI != source._highDPI;
}

inline auto multiFactorImage::operator!=(const multiFactorImage& source) const -> bool {
  return !operator==(source);
}

}

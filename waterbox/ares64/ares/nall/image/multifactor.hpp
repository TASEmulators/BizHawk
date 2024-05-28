#pragma once

namespace nall {

inline multiFactorImage::multiFactorImage(const multiFactorImage& source) {
  operator=(source);
}

inline multiFactorImage::multiFactorImage(multiFactorImage&& source) {
  operator=(std::move(source));
}

inline multiFactorImage::multiFactorImage(const image& lowDPI, const image& highDPI) {
  image::operator=(lowDPI);
  _highDPI = highDPI;
}

inline multiFactorImage::multiFactorImage(const image& source) {
  image::operator=(source);
}

inline multiFactorImage::multiFactorImage(image&& source) {
  image::operator=(std::move(source));
}

inline multiFactorImage::multiFactorImage() {
}

inline multiFactorImage::~multiFactorImage() {
}

inline auto multiFactorImage::operator=(const multiFactorImage& source) -> multiFactorImage& {
  if(this == &source) return *this;
  
  image::operator=(source);
  _highDPI = source._highDPI;

  return *this;
}

inline auto multiFactorImage::operator=(multiFactorImage&& source) -> multiFactorImage& {
  if(this == &source) return *this;

  image::operator=(std::move(source));
  _highDPI = std::move(source._highDPI);

  return *this;
}

inline auto multiFactorImage::operator==(const multiFactorImage& source) const -> bool {
  if(image::operator!=(source)) return false;
  return _highDPI == source._highDPI;
}

inline auto multiFactorImage::operator!=(const multiFactorImage& source) const -> bool {
  return !operator==(source);
}

}

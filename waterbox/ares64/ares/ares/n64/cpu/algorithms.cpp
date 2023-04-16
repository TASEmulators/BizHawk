template <typename T>
auto CPU::roundNearest(f32 f) -> T {
#if defined(ARCHITECTURE_ARM64)
  return vrndns_f32(f);
#elif defined(ARCHITECTURE_AMD64) && ARCHITECTURE_SUPPORTS_SSE4_1
  __m128 t = _mm_set_ss(f);
  t = _mm_round_ss(t, t, _MM_FROUND_TO_NEAREST_INT);
  return _mm_cvtss_f32(t);
#else
  return round(f);
#endif
}

template <typename T>
auto CPU::roundNearest(f64 f) -> T {
#if defined(ARCHITECTURE_ARM64)
  float64x1_t vf = vdup_n_f64(f);
  return vget_lane_f64(vrndn_f64(vf), 0);
#elif defined(ARCHITECTURE_AMD64) && ARCHITECTURE_SUPPORTS_SSE4_1
  __m128d t = _mm_set_sd(f);
  t = _mm_round_sd(t, t, _MM_FROUND_TO_NEAREST_INT);
  return _mm_cvtsd_f64(t);
#else
  return round(f);
#endif
}

template <typename T>
auto CPU::roundCeil(f32 f) -> T {
#if defined(ARCHITECTURE_AMD64) && ARCHITECTURE_SUPPORTS_SSE4_1
  __m128 t = _mm_set_ss(f);
  t = _mm_round_ss(t, t, _MM_FROUND_TO_POS_INF);
  return _mm_cvtss_f32(t);
#else
  return ceil(f);
#endif
}

template <typename T>
auto CPU::roundCeil(f64 f) -> T {
#if defined(ARCHITECTURE_AMD64) && ARCHITECTURE_SUPPORTS_SSE4_1
  __m128d t = _mm_set_sd(f);
  t = _mm_round_sd(t, t, _MM_FROUND_TO_POS_INF);
  return _mm_cvtsd_f64(t);
#else
  return ceil(f);
#endif
}

template<typename T>
auto CPU::roundCurrent(f32 f) -> T {
#if defined(ARCHITECTURE_AMD64) && ARCHITECTURE_SUPPORTS_SSE4_1
  auto t = _mm_set_ss(f);
  t = _mm_round_ss(t, t, _MM_FROUND_CUR_DIRECTION);
  return _mm_cvtss_f32(t);
#else
  return rint(f);
#endif
}

template<typename T>
auto CPU::roundCurrent(f64 f) -> T {
#if defined(ARCHITECTURE_AMD64) && ARCHITECTURE_SUPPORTS_SSE4_1
  auto t = _mm_set_sd(f);
  t = _mm_round_sd(t, t, _MM_FROUND_CUR_DIRECTION);
  return _mm_cvtsd_f64(t);
#else
  return rint(f);
#endif
}

template <typename T>
auto CPU::roundFloor(f32 f) -> T {
#if defined(ARCHITECTURE_AMD64) && ARCHITECTURE_SUPPORTS_SSE4_1
  __m128 t = _mm_set_ss(f);
  t = _mm_round_ss(t, t, _MM_FROUND_TO_NEG_INF);
  return _mm_cvtss_f32(t);
#else
  return floor(f);
#endif
}

template <typename T>
auto CPU::roundFloor(f64 f) -> T {
#if defined(ARCHITECTURE_AMD64) && ARCHITECTURE_SUPPORTS_SSE4_1
  __m128d t = _mm_set_sd(f);
  t = _mm_round_sd(t, t, _MM_FROUND_TO_NEG_INF);
  return _mm_cvtsd_f64(t);
#else
  return floor(f);
#endif
}

template <typename T>
auto CPU::roundTrunc(f32 f) -> T {
#if defined(ARCHITECTURE_AMD64) && ARCHITECTURE_SUPPORTS_SSE4_1
  __m128 t = _mm_set_ss(f);
  t = _mm_round_ss(t, t, _MM_FROUND_TO_ZERO);
  return _mm_cvtss_f32(t);
#else
  return trunc(f);
#endif
}

template <typename T>
auto CPU::roundTrunc(f64 f) -> T {
#if defined(ARCHITECTURE_AMD64) && ARCHITECTURE_SUPPORTS_SSE4_1
  __m128d t = _mm_set_sd(f);
  t = _mm_round_sd(t, t, _MM_FROUND_TO_ZERO);
  return _mm_cvtsd_f64(t);
#else
  return trunc(f);
#endif
}

auto CPU::squareRoot(f32 f) -> f32 {
#if defined(ARCHITECTURE_AMD64)
  __m128 t = _mm_set_ss(f);
  t = _mm_sqrt_ss(t);
  return _mm_cvtss_f32(t);
#else
  return sqrt(f);
#endif
}

auto CPU::squareRoot(f64 f) -> f64 {
#if defined(ARCHITECTURE_AMD64)
  __m128d t = _mm_set_sd(f);
  t = _mm_sqrt_sd(t, t);
  return _mm_cvtsd_f64(t);
#else
  return sqrt(f);
#endif
}

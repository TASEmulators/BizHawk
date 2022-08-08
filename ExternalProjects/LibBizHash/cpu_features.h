/* cpu_features.h -- CPU architecture feature check
 * Copyright (C) 2017 Hans Kristian Rosbach
 * For conditions of distribution and use, see copyright notice in README.md
 */

#ifndef CPU_FEATURES_H_
#define CPU_FEATURES_H_

#include <stdint.h>
#include "crc32_fold.h"
#include "x86/x86_features.h"

extern void cpu_check_features(void);

/* CRC32 */
typedef uint32_t (*crc32_func)(uint32_t crc32, const uint8_t *buf, uint32_t len);

extern uint32_t crc32_braid(uint32_t crc, const uint8_t *buf, uint32_t len);
extern uint32_t crc32_pclmulqdq(uint32_t crc32, const uint8_t *buf, uint32_t len);

/* SHA1 */
void BizFastCalcSha1Internal(uint32_t state[5], const uint8_t data[], uint32_t length);

#endif

/* cpu_features.c -- CPU architecture feature check
 * Copyright (C) 2017 Hans Kristian Rosbach
 * For conditions of distribution and use, see copyright notice in README.md
 */

#include <stdint.h>
#include "cpu_features.h"

void cpu_check_features(void) {
    static int features_checked = 0;
    if (features_checked)
        return;
    x86_check_features();
    features_checked = 1;
}

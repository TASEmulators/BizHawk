#ifndef GBButtons_h
#define GBButtons_h

typedef enum : NSUInteger {
    GBRight,
    GBLeft,
    GBUp,
    GBDown,
    GBA,
    GBB,
    GBSelect,
    GBStart,
    GBTurbo,
    GBRewind,
    GBUnderclock,
    GBButtonCount,
    GBGameBoyButtonCount = GBStart + 1,
} GBButton;

extern NSString const *GBButtonNames[GBButtonCount];

static inline NSString *n2s(uint64_t number)
{
    return [NSString stringWithFormat:@"%llx", number];
}

static inline NSString *button_to_preference_name(GBButton button, unsigned player)
{
    if (player) {
        return [NSString stringWithFormat:@"GBPlayer%d%@", player + 1, GBButtonNames[button]];
    }
    return [NSString stringWithFormat:@"GB%@", GBButtonNames[button]];
}

#endif

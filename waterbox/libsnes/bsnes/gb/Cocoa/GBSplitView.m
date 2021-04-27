#import "GBSplitView.h"

@implementation GBSplitView
{
    NSColor *_dividerColor;
}

- (void)setDividerColor:(NSColor *)color 
{
    _dividerColor = color;
    [self setNeedsDisplay:YES];
}

- (NSColor *)dividerColor 
{
    if (_dividerColor) {
        return _dividerColor;
    }
    return [super dividerColor];
}

/* Mavericks comaptibility */
- (NSArray<NSView *> *)arrangedSubviews
{
    if (@available(macOS 10.11, *)) {
        return [super arrangedSubviews];
    }
    else {
        return [self subviews];
    }
}

@end

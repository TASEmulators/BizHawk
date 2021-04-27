#import "GBBorderView.h"

@implementation GBBorderView


- (void)awakeFromNib
{
    self.wantsLayer = YES;
}

- (BOOL)wantsUpdateLayer
{
    return YES;
}

- (void)updateLayer
{
    /* Wonderful, wonderful windowserver(?) bug. Using 0,0,0 here would cause it to render garbage
       on fullscreen windows on some High Sierra machines. Any other value, including the one used
       here (which is rendered exactly the same due to rounding) works around this bug. */
    self.layer.backgroundColor = [NSColor colorWithCalibratedRed:0
                                                           green:0
                                                            blue:1.0 / 1024.0
                                                           alpha:1.0].CGColor;
}
@end

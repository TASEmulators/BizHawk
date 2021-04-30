#import "GBImageCell.h"

@implementation GBImageCell
- (void)drawWithFrame:(NSRect)cellFrame inView:(NSView *)controlView
{
    CGContextRef context = [[NSGraphicsContext currentContext] graphicsPort];
    CGContextSetInterpolationQuality(context, kCGInterpolationNone);
    [super drawWithFrame:cellFrame inView:controlView];
}
@end

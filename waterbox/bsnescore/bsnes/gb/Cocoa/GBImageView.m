#import "GBImageView.h"

@implementation GBImageViewGridConfiguration
- (instancetype)initWithColor:(NSColor *)color size:(NSUInteger)size
{
    self = [super init];
    self.color = color;
    self.size = size;
    return self;
}
@end

@implementation GBImageView
{
    NSTrackingArea *trackingArea;
}
- (void)drawRect:(NSRect)dirtyRect
{
    CGContextRef context = [[NSGraphicsContext currentContext] graphicsPort];
    CGContextSetInterpolationQuality(context, kCGInterpolationNone);
    [super drawRect:dirtyRect];
    CGFloat y_ratio = self.frame.size.height / self.image.size.height;
    CGFloat x_ratio = self.frame.size.width / self.image.size.width;
    for (GBImageViewGridConfiguration *conf in self.verticalGrids) {
        [conf.color set];
        for (CGFloat y = conf.size * y_ratio; y < self.frame.size.height; y += conf.size * y_ratio) {
            NSBezierPath *line = [NSBezierPath bezierPath];
            [line moveToPoint:NSMakePoint(0, y - 0.5)];
            [line lineToPoint:NSMakePoint(self.frame.size.width, y - 0.5)];
            [line setLineWidth:1.0];
            [line stroke];
        }
    }
    
    for (GBImageViewGridConfiguration *conf in self.horizontalGrids) {
        [conf.color set];
        for (CGFloat x = conf.size * x_ratio; x < self.frame.size.width; x += conf.size * x_ratio) {
            NSBezierPath *line = [NSBezierPath bezierPath];
            [line moveToPoint:NSMakePoint(x + 0.5, 0)];
            [line lineToPoint:NSMakePoint(x + 0.5, self.frame.size.height)];
            [line setLineWidth:1.0];
            [line stroke];
        }
    }
    
    if (self.displayScrollRect) {
        NSBezierPath *path = [NSBezierPath bezierPathWithRect:CGRectInfinite];
        for (unsigned x = 0; x < 2; x++) {
            for (unsigned y = 0; y < 2; y++) {
                NSRect rect = self.scrollRect;
                rect.origin.x *= x_ratio;
                rect.origin.y *= y_ratio;
                rect.size.width *= x_ratio;
                rect.size.height *= y_ratio;
                rect.origin.y = self.frame.size.height - rect.origin.y - rect.size.height;
                
                rect.origin.x -= self.frame.size.width * x;
                rect.origin.y += self.frame.size.height * y;

                
                NSBezierPath *subpath = [NSBezierPath bezierPathWithRect:rect];
                [path appendBezierPath:subpath];
            }
        }
        [path setWindingRule:NSEvenOddWindingRule];
        [path setLineWidth:4.0];
        [path setLineJoinStyle:NSRoundLineJoinStyle];
        [[NSColor colorWithWhite:0.2 alpha:0.5] set];
        [path fill];
        [path addClip];
        [[NSColor colorWithWhite:0.0 alpha:0.6] set];
        [path stroke];
    }
}

- (void)setHorizontalGrids:(NSArray *)horizontalGrids
{
    self->_horizontalGrids = horizontalGrids;
    [self setNeedsDisplay];
}

- (void)setVerticalGrids:(NSArray *)verticalGrids
{
    self->_verticalGrids = verticalGrids;
    [self setNeedsDisplay];
}

- (void)setDisplayScrollRect:(bool)displayScrollRect
{
    self->_displayScrollRect = displayScrollRect;
    [self setNeedsDisplay];
}

- (void)updateTrackingAreas
{
    if (trackingArea != nil) {
        [self removeTrackingArea:trackingArea];
    }
    
    trackingArea = [ [NSTrackingArea alloc] initWithRect:[self bounds]
                                                 options:NSTrackingMouseEnteredAndExited | NSTrackingActiveAlways | NSTrackingMouseMoved
                                                   owner:self
                                                userInfo:nil];
    [self addTrackingArea:trackingArea];
}

- (void)mouseExited:(NSEvent *)theEvent
{
    if ([self.delegate respondsToSelector:@selector(mouseDidLeaveImageView:)]) {
        [self.delegate mouseDidLeaveImageView:self];
    }
}

- (void)mouseMoved:(NSEvent *)theEvent
{
    if ([self.delegate respondsToSelector:@selector(imageView:mouseMovedToX:Y:)]) {
        NSPoint location = [self convertPoint:theEvent.locationInWindow fromView:nil];
        location.x /= self.bounds.size.width;
        location.y /= self.bounds.size.height;
        location.y = 1 - location.y;
        location.x *= self.image.size.width;
        location.y *= self.image.size.height;
        [self.delegate imageView:self mouseMovedToX:(NSUInteger)location.x Y:(NSUInteger)location.y];
    }
}

@end

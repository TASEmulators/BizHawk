#import "JOYAxes2D.h"
#import "JOYElement.h"

@implementation JOYAxes2D
{
    JOYElement *_element1, *_element2;
    double _state1, _state2;
    int32_t initialX, initialY;
    int32_t minX, minY;
    int32_t maxX, maxY;

}

+ (NSString *)usageToString: (JOYAxes2DUsage) usage
{
    if (usage < JOYAxes2DUsageNonGenericMax) {
        return (NSString *[]) {
            @"None",
            @"Left Stick",
            @"Right Stick",
            @"Middle Stick",
            @"Pointer",
        }[usage];
    }
    if (usage >= JOYAxes2DUsageGeneric0) {
        return [NSString stringWithFormat:@"Generic 2D Analog Control %d", usage - JOYAxes2DUsageGeneric0];
    }
    
    return [NSString stringWithFormat:@"Unknown Usage 2D Axes %d", usage];
}

- (NSString *)usageString
{
    return [self.class usageToString:_usage];
}

- (uint64_t)uniqueID
{
    return _element1.uniqueID;
}

- (NSString *)description
{
    return [NSString stringWithFormat:@"<%@: %p, %@ (%llu); State: %.2f%%, %.2f degrees>", self.className, self, self.usageString, self.uniqueID, self.distance * 100, self.angle];
}

- (instancetype)initWithFirstElement:(JOYElement *)element1 secondElement:(JOYElement *)element2
{
    self = [super init];
    if (!self) return self;
    
    _element1 = element1;
    _element2 = element2;

    
    if (element1.usagePage == kHIDPage_GenericDesktop) {
        uint16_t usage = element1.usage;
        _usage = JOYAxes2DUsageGeneric0 + usage - kHIDUsage_GD_X + 1;
    }
    initialX = 0;
    initialY = 0;
    minX = element1.max;
    minY = element2.max;
    maxX = element1.min;
    maxY = element2.min;
    
    return self;
}

- (NSPoint)value
{
    return NSMakePoint(_state1, _state2);
}

-(int32_t) effectiveMinX
{
    int32_t rawMin = _element1.min;
    int32_t rawMax = _element1.max;
    if (initialX == 0) return rawMin;
    if (minX <= (rawMin * 2 + initialX) / 3 && maxX >= (rawMax * 2 + initialX) / 3 ) return minX;
    if ((initialX - rawMin) < (rawMax - initialX)) return rawMin;
    return initialX - (rawMax - initialX);
}

-(int32_t) effectiveMinY
{
    int32_t rawMin = _element2.min;
    int32_t rawMax = _element2.max;
    if (initialY == 0) return rawMin;
    if (minX <= (rawMin * 2 + initialY) / 3 && maxY >= (rawMax * 2 + initialY) / 3 ) return minY;
    if ((initialY - rawMin) < (rawMax - initialY)) return rawMin;
    return initialY - (rawMax - initialY);
}

-(int32_t) effectiveMaxX
{
    int32_t rawMin = _element1.min;
    int32_t rawMax = _element1.max;
    if (initialX == 0) return rawMax;
    if (minX <= (rawMin * 2 + initialX) / 3 && maxX >= (rawMax * 2 + initialX) / 3 ) return maxX;
    if ((initialX - rawMin) > (rawMax - initialX)) return rawMax;
    return initialX + (initialX - rawMin);
}

-(int32_t) effectiveMaxY
{
    int32_t rawMin = _element2.min;
    int32_t rawMax = _element2.max;
    if (initialY == 0) return rawMax;
    if (minX <= (rawMin * 2 + initialY) / 3 && maxY >= (rawMax * 2 + initialY) / 3 ) return maxY;
    if ((initialY - rawMin) > (rawMax - initialY)) return rawMax;
    return initialY + (initialY - rawMin);
}

- (bool)updateState
{
    int32_t x = [_element1 value];
    int32_t y = [_element2 value];
    if (x == 0 && y == 0) return false;
    
    if (initialX == 0 && initialY == 0) {
         initialX = x;
         initialY = y;
    }
    
    double old1 = _state1, old2 = _state2;
    {
        int32_t value = x;

        if (initialX != 0) {
            minX = MIN(value, minX);
            maxX = MAX(value, maxX);
        }
        
        double min = [self effectiveMinX];
        double max = [self effectiveMaxX];
        if (min == max) return false;
        
        _state1 = (value - min) / (max - min) * 2 - 1;
    }
    
    {
        int32_t value = y;

        if (initialY != 0) {
            minY = MIN(value, minY);
            maxY = MAX(value, maxY);
        }
        
        double min = [self effectiveMinY];
        double max = [self effectiveMaxY];
        if (min == max) return false;
        
        _state2 = (value - min) / (max - min) * 2 - 1;
    }
    
    if (_state1 < -1 || _state1 > 1 ||
        _state2 < -1 || _state2 > 1) {
        // Makes no sense, recalibrate
        _state1 = _state2 = 0;
        initialX = initialY = 0;
        minX = _element1.max;
        minY = _element2.max;
        maxX = _element1.min;
        maxY = _element2.min;
    }

    return old1 != _state1 || old2 != _state2;
}

- (double)distance
{
    return MIN(sqrt(_state1 * _state1 + _state2 * _state2), 1.0);
}

- (double)angle {
    double temp = atan2(_state2, _state1) * 180 / M_PI;
    if (temp >= 0) return temp;
    return temp + 360;
}
@end

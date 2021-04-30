#import "JOYAxis.h"
#import "JOYElement.h"

@implementation JOYAxis
{
    JOYElement *_element;
    double _state;
    double _min;
}

+ (NSString *)usageToString: (JOYAxisUsage) usage
{
    if (usage < JOYAxisUsageNonGenericMax) {
        return (NSString *[]) {
            @"None",
            @"Analog L1",
            @"Analog L2",
            @"Analog L3",
            @"Analog R1",
            @"Analog R2",
            @"Analog R3",
            @"Wheel",
            @"Rudder",
            @"Throttle",
            @"Accelerator",
            @"Brake",
        }[usage];
    }
    if (usage >= JOYAxisUsageGeneric0) {
        return [NSString stringWithFormat:@"Generic Analog Control %d", usage - JOYAxisUsageGeneric0];
    }
    
    return [NSString stringWithFormat:@"Unknown Usage Axis %d", usage];
}

- (NSString *)usageString
{
    return [self.class usageToString:_usage];
}

- (uint64_t)uniqueID
{
    return _element.uniqueID;
}

- (NSString *)description
{
    return [NSString stringWithFormat:@"<%@: %p, %@ (%llu); State: %f%%>", self.className, self, self.usageString, self.uniqueID, _state * 100];
}

- (instancetype)initWithElement:(JOYElement *)element
{
    self = [super init];
    if (!self) return self;
    
    _element = element;

    
    if (element.usagePage == kHIDPage_GenericDesktop) {
        uint16_t usage = element.usage;
        _usage = JOYAxisUsageGeneric0 + usage - kHIDUsage_GD_X + 1;
    }
    
    _min = 1.0;
    
    return self;
}

- (double) value
{
    return _state;
}

- (bool)updateState
{
    double min = _element.min;
    double max = _element.max;
    if (min == max) return false;
    double old = _state;
    double unnormalized = ([_element value] - min) / (max - min);
    if (unnormalized < _min) {
        _min = unnormalized;
    }
    if (_min != 1) {
        _state = (unnormalized - _min) / (1 - _min);
    }
    return old != _state;
}

@end

#import "JOYEmulatedButton.h"

@interface JOYButton ()
{
    @public bool _state;
}
@end

@implementation JOYEmulatedButton
{
    uint64_t _uniqueID;
}

- (instancetype)initWithUsage:(JOYButtonUsage)usage uniqueID:(uint64_t)uniqueID;
{
    self = [super init];
    self.usage = usage;
    _uniqueID = uniqueID;
    
    return self;
}

- (uint64_t)uniqueID
{
    return _uniqueID;
}

- (bool)updateStateFromAxis:(JOYAxis *)axis
{
    bool old = _state;
    _state = [axis value] > 0.5;
    return _state != old;
}

- (bool)updateStateFromAxes2D:(JOYAxes2D *)axes
{
    bool old = _state;
    if (axes.distance < 0.5) {
        _state = false;
    }
    else {
        unsigned direction = ((unsigned)round(axes.angle / 360 * 8)) & 7;
        switch (self.usage) {
            case JOYButtonUsageDPadLeft:
                _state = direction >= 3 && direction <= 5;
                break;
            case JOYButtonUsageDPadRight:
                _state = direction <= 1 || direction == 7;
                break;
            case JOYButtonUsageDPadUp:
                _state = direction >= 5;
                break;
            case JOYButtonUsageDPadDown:
                _state = direction <= 3 && direction >= 1;
                break;
            default:
                break;
        }
    }
    return _state != old;
}

- (bool)updateStateFromHat:(JOYHat *)hat
{
    bool old = _state;
    if (!hat.pressed) {
        _state = false;
    }
    else {
        unsigned direction = ((unsigned)round(hat.angle / 360 * 8)) & 7;
        switch (self.usage) {
            case JOYButtonUsageDPadLeft:
                _state = direction >= 3 && direction <= 5;
                break;
            case JOYButtonUsageDPadRight:
                _state = direction <= 1 || direction == 7;
                break;
            case JOYButtonUsageDPadUp:
                _state = direction >= 5;
                break;
            case JOYButtonUsageDPadDown:
                _state = direction <= 3 && direction >= 1;
                break;
            default:
                break;
        }
    }
    return _state != old;
}

@end

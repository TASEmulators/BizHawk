#import "JOYHat.h"
#import "JOYElement.h"

@implementation JOYHat
{
    JOYElement *_element;
    double _state;
}

- (uint64_t)uniqueID
{
    return _element.uniqueID;
}

- (NSString *)description
{
    if (self.isPressed) {
        return [NSString stringWithFormat:@"<%@: %p (%llu); State: %f degrees>", self.className, self, self.uniqueID, self.angle];
    }
    return [NSString stringWithFormat:@"<%@: %p (%llu); State: released>", self.className, self, self.uniqueID];

}

- (instancetype)initWithElement:(JOYElement *)element
{
    self = [super init];
    if (!self) return self;
    
    _element = element;
    
    return self;
}

- (bool)isPressed
{
    return _state >= 0 && _state < 360;
}

- (double)angle
{
    if (self.isPressed) return fmod((_state + 270), 360);
    return -1;
}

- (unsigned)resolution
{
    return _element.max - _element.min + 1;
}

- (bool)updateState
{
    unsigned state = ([_element value] - _element.min) * 360.0 / self.resolution;
    if (_state != state) {
        _state = state;
        return true;
    }
    return false;
}

@end

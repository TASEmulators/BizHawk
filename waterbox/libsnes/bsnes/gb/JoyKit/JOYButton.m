#import "JOYButton.h"
#import "JOYElement.h"

@implementation JOYButton
{
    JOYElement *_element;
    bool _state;
}

+ (NSString *)usageToString: (JOYButtonUsage) usage
{
    if (usage < JOYButtonUsageNonGenericMax) {
        return (NSString *[]) {
            @"None",
            @"A",
            @"B",
            @"C",
            @"X",
            @"Y",
            @"Z",
            @"Start",
            @"Select",
            @"Home",
            @"Misc",
            @"Left Stick",
            @"Right Stick",
            @"L1",
            @"L2",
            @"L3",
            @"R1",
            @"R2",
            @"R3",
            @"D-Pad Left",
            @"D-Pad Right",
            @"D-Pad Up",
            @"D-Pad Down",
        }[usage];
    }
    if (usage >= JOYButtonUsageGeneric0) {
        return [NSString stringWithFormat:@"Generic Button %d", usage - JOYButtonUsageGeneric0];
    }
    
    return [NSString stringWithFormat:@"Unknown Usage Button %d", usage];
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
    return [NSString stringWithFormat:@"<%@: %p, %@ (%llu); State: %s>", self.className, self, self.usageString, self.uniqueID, _state? "Presssed" : "Released"];
}

- (instancetype)initWithElement:(JOYElement *)element
{
    self = [super init];
    if (!self) return self;
    
    _element = element;

    if (element.usagePage == kHIDPage_Button) {
        uint16_t usage = element.usage;
        _usage = JOYButtonUsageGeneric0 + usage;
    }
    else if (element.usagePage == kHIDPage_GenericDesktop) {
        switch (element.usage) {
            case kHIDUsage_GD_DPadUp: _usage = JOYButtonUsageDPadUp; break;
            case kHIDUsage_GD_DPadDown: _usage = JOYButtonUsageDPadDown; break;
            case kHIDUsage_GD_DPadRight: _usage = JOYButtonUsageDPadRight; break;
            case kHIDUsage_GD_DPadLeft: _usage = JOYButtonUsageDPadLeft; break;
            case kHIDUsage_GD_Start: _usage = JOYButtonUsageStart; break;
            case kHIDUsage_GD_Select: _usage = JOYButtonUsageSelect; break;
            case kHIDUsage_GD_SystemMainMenu: _usage = JOYButtonUsageHome; break;
        }
    }
    
    return self;
}

- (bool) isPressed
{
    return _state;
}

- (bool)updateState
{
    bool state = [_element value];
    if (_state != state) {
        _state = state;
        return true;
    }
    return false;
}

@end

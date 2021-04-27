#import "JOYController.h"
#import "JOYMultiplayerController.h"
#import "JOYElement.h"
#import "JOYSubElement.h"
#import "JOYFullReportElement.h"

#import "JOYEmulatedButton.h"
#include <IOKit/hid/IOHIDLib.h>

#define PWM_RESOLUTION 16

static NSString const *JOYAxisGroups = @"JOYAxisGroups";
static NSString const *JOYReportIDFilters = @"JOYReportIDFilters";
static NSString const *JOYButtonUsageMapping = @"JOYButtonUsageMapping";
static NSString const *JOYAxisUsageMapping = @"JOYAxisUsageMapping";
static NSString const *JOYAxes2DUsageMapping = @"JOYAxes2DUsageMapping";
static NSString const *JOYCustomReports = @"JOYCustomReports";
static NSString const *JOYIsSwitch = @"JOYIsSwitch";
static NSString const *JOYRumbleUsage = @"JOYRumbleUsage";
static NSString const *JOYRumbleUsagePage = @"JOYRumbleUsagePage";
static NSString const *JOYConnectedUsage = @"JOYConnectedUsage";
static NSString const *JOYConnectedUsagePage = @"JOYConnectedUsagePage";
static NSString const *JOYRumbleMin = @"JOYRumbleMin";
static NSString const *JOYRumbleMax = @"JOYRumbleMax";
static NSString const *JOYSwapZRz = @"JOYSwapZRz";
static NSString const *JOYActivationReport = @"JOYActivationReport";
static NSString const *JOYIgnoredReports = @"JOYIgnoredReports";
static NSString const *JOYIsDualShock3 = @"JOYIsDualShock3";

static NSMutableDictionary<id, JOYController *> *controllers; // Physical controllers
static NSMutableArray<JOYController *> *exposedControllers; // Logical controllers

static NSDictionary *hacksByName = nil;
static NSDictionary *hacksByManufacturer = nil;

static NSMutableSet<id<JOYListener>> *listeners = nil;

static bool axesEmulateButtons = false;
static bool axes2DEmulateButtons = false;
static bool hatsEmulateButtons = false;

@interface JOYController ()
+ (void)controllerAdded:(IOHIDDeviceRef) device;
+ (void)controllerRemoved:(IOHIDDeviceRef) device;
- (void)elementChanged:(IOHIDElementRef) element;
- (void)gotReport:(NSData *)report;

@end

@interface JOYButton ()
- (instancetype)initWithElement:(JOYElement *)element;
- (bool)updateState;
@end

@interface JOYAxis ()
- (instancetype)initWithElement:(JOYElement *)element;
- (bool)updateState;
@end

@interface JOYHat ()
- (instancetype)initWithElement:(JOYElement *)element;
- (bool)updateState;
@end

@interface JOYAxes2D ()
- (instancetype)initWithFirstElement:(JOYElement *)element1 secondElement:(JOYElement *)element2;
- (bool)updateState;
@end

static NSDictionary *CreateHIDDeviceMatchDictionary(const UInt32 page, const UInt32 usage)
{
    return @{
        @kIOHIDDeviceUsagePageKey: @(page),
        @kIOHIDDeviceUsageKey: @(usage),
    };
}

static void HIDDeviceAdded(void *context, IOReturn result, void *sender, IOHIDDeviceRef device)
{
    [JOYController controllerAdded:device];
}

static void HIDDeviceRemoved(void *context, IOReturn result, void *sender, IOHIDDeviceRef device)
{
    [JOYController controllerRemoved:device];
}

static void HIDInput(void *context, IOReturn result, void *sender, IOHIDValueRef value)
{
    [(__bridge JOYController *)context elementChanged:IOHIDValueGetElement(value)];
}

static void HIDReport(void *context, IOReturn result, void *sender, IOHIDReportType type,
                      uint32_t reportID, uint8_t *report, CFIndex reportLength)
{
    if (reportLength) {
        [(__bridge JOYController *)context gotReport:[[NSData alloc] initWithBytesNoCopy:report length:reportLength freeWhenDone:NO]];
    }
}

typedef struct __attribute__((packed)) {
    uint8_t reportID;
    uint8_t sequence;
    uint8_t rumbleData[8];
    uint8_t command;
    uint8_t commandData[26];
} JOYSwitchPacket;

typedef struct __attribute__((packed)) {
    uint8_t reportID;
    uint8_t padding;
    uint8_t rumbleRightDuration;
    uint8_t rumbleRightStrength;
    uint8_t rumbleLeftDuration;
    uint8_t rumbleLeftStrength;
    uint32_t padding2;
    uint8_t ledsEnabled;
    struct {
        uint8_t timeEnabled;
        uint8_t dutyLength;
        uint8_t enabled;
        uint8_t dutyOff;
        uint8_t dutyOn;
    } __attribute__((packed)) led[5];
    uint8_t padding3[13];
} JOYDualShock3Output;

typedef union {
    JOYSwitchPacket switchPacket;
    JOYDualShock3Output ds3Output;
} JOYVendorSpecificOutput;

@implementation JOYController
{
    IOHIDDeviceRef _device;
    NSMutableDictionary<JOYElement *, JOYButton *> *_buttons;
    NSMutableDictionary<JOYElement *, JOYAxis *> *_axes;
    NSMutableDictionary<JOYElement *, JOYAxes2D *> *_axes2D;
    NSMutableDictionary<JOYElement *, JOYHat *> *_hats;
    NSMutableDictionary<NSNumber *, JOYFullReportElement *> *_fullReportElements;
    NSMutableDictionary<JOYFullReportElement *, NSArray<JOYElement *> *> *_multiElements;

    // Button emulation
    NSMutableDictionary<NSNumber *, JOYEmulatedButton *> *_axisEmulatedButtons;
    NSMutableDictionary<NSNumber *, NSArray <JOYEmulatedButton *> *> *_axes2DEmulatedButtons;
    NSMutableDictionary<NSNumber *, NSArray <JOYEmulatedButton *> *> *_hatEmulatedButtons;

    JOYElement *_rumbleElement;
    JOYElement *_connectedElement;
    NSMutableDictionary<NSValue *, JOYElement *> *_iokitToJOY;
    NSString *_serialSuffix;
    bool _isSwitch; // Does this controller use the Switch protocol?
    bool _isDualShock3; // Does this controller use DS3 outputs?
    JOYVendorSpecificOutput _lastVendorSpecificOutput;
    volatile double _rumbleAmplitude;
    bool _physicallyConnected;
    bool _logicallyConnected;
    
    NSDictionary *_hacks;
    NSMutableData *_lastReport;
    
    // Used when creating inputs
    JOYElement *_previousAxisElement;
    
    uint8_t _playerLEDs;
    double _sentRumbleAmp;
    unsigned _rumbleCounter;
    bool _deviceCantSendReports;
}

- (instancetype)initWithDevice:(IOHIDDeviceRef) device hacks:(NSDictionary *)hacks
{
    return [self initWithDevice:device reportIDFilter:nil serialSuffix:nil hacks:hacks];
}

-(void)createOutputForElement:(JOYElement *)element
{
    uint16_t rumbleUsagePage = (uint16_t)[_hacks[JOYRumbleUsagePage] unsignedIntValue];
    uint16_t rumbleUsage = (uint16_t)[_hacks[JOYRumbleUsage] unsignedIntValue];

    if (!_rumbleElement && rumbleUsage && rumbleUsagePage && element.usage == rumbleUsage && element.usagePage == rumbleUsagePage) {
        if (_hacks[JOYRumbleMin]) {
            element.min = [_hacks[JOYRumbleMin] unsignedIntValue];
        }
        if (_hacks[JOYRumbleMax]) {
            element.max = [_hacks[JOYRumbleMax] unsignedIntValue];
        }
        _rumbleElement = element;
    }
}

-(void)createInputForElement:(JOYElement *)element
{
    uint16_t connectedUsagePage = (uint16_t)[_hacks[JOYConnectedUsagePage] unsignedIntValue];
    uint16_t connectedUsage = (uint16_t)[_hacks[JOYConnectedUsage] unsignedIntValue];

    if (!_connectedElement && connectedUsage && connectedUsagePage && element.usage == connectedUsage && element.usagePage == connectedUsagePage) {
        _connectedElement = element;
        _logicallyConnected = element.value != element.min;
        return;
    }
    
    if (element.usagePage == kHIDPage_Button) {
    button: {
        JOYButton *button = [[JOYButton alloc] initWithElement: element];
        [_buttons setObject:button forKey:element];
        NSNumber *replacementUsage = _hacks[JOYButtonUsageMapping][@(button.usage)];
        if (replacementUsage) {
            button.usage = [replacementUsage unsignedIntValue];
        }
        return;
    }
    }
    else if (element.usagePage == kHIDPage_GenericDesktop) {
        NSDictionary *axisGroups = @{
            @(kHIDUsage_GD_X): @(0),
            @(kHIDUsage_GD_Y): @(0),
            @(kHIDUsage_GD_Z): @(1),
            @(kHIDUsage_GD_Rx): @(2),
            @(kHIDUsage_GD_Ry): @(2),
            @(kHIDUsage_GD_Rz): @(1),
        };
        
        axisGroups = _hacks[JOYAxisGroups] ?: axisGroups;

        switch (element.usage) {
            case kHIDUsage_GD_X:
            case kHIDUsage_GD_Y:
            case kHIDUsage_GD_Z:
            case kHIDUsage_GD_Rx:
            case kHIDUsage_GD_Ry:
            case kHIDUsage_GD_Rz: {
                
                JOYElement *other = _previousAxisElement;
                _previousAxisElement = element;
                if (!other) goto single;
                if (other.usage >= element.usage) goto single;
                if (other.reportID != element.reportID) goto single;
                if (![axisGroups[@(other.usage)] isEqualTo: axisGroups[@(element.usage)]]) goto single;
                if (other.parentID != element.parentID) goto single;
                
                JOYAxes2D *axes = nil;
                if (other.usage == kHIDUsage_GD_Z && element.usage == kHIDUsage_GD_Rz && [_hacks[JOYSwapZRz] boolValue]) {
                    axes = [[JOYAxes2D alloc] initWithFirstElement:element secondElement:other];
                }
                else {
                    axes = [[JOYAxes2D alloc] initWithFirstElement:other secondElement:element];
                }
                NSNumber *replacementUsage = _hacks[JOYAxes2DUsageMapping][@(axes.usage)];
                if (replacementUsage) {
                    axes.usage = [replacementUsage unsignedIntValue];
                }
                
                [_axisEmulatedButtons removeObjectForKey:@(_axes[other].uniqueID)];
                [_axes removeObjectForKey:other];
                _previousAxisElement = nil;
                _axes2D[other] = axes;
                _axes2D[element] = axes;
                
                if (axes2DEmulateButtons) {
                    _axes2DEmulatedButtons[@(axes.uniqueID)] = @[
                        [[JOYEmulatedButton alloc] initWithUsage:JOYButtonUsageDPadLeft  uniqueID:axes.uniqueID | 0x100000000L],
                        [[JOYEmulatedButton alloc] initWithUsage:JOYButtonUsageDPadRight uniqueID:axes.uniqueID | 0x200000000L],
                        [[JOYEmulatedButton alloc] initWithUsage:JOYButtonUsageDPadUp  uniqueID:axes.uniqueID | 0x300000000L],
                        [[JOYEmulatedButton alloc] initWithUsage:JOYButtonUsageDPadDown uniqueID:axes.uniqueID | 0x400000000L],
                    ];
                }
                
                /*
                 for (NSArray *group in axes2d) {
                 break;
                 IOHIDElementRef first  = (__bridge IOHIDElementRef)group[0];
                 IOHIDElementRef second = (__bridge IOHIDElementRef)group[1];
                 if (IOHIDElementGetUsage(first)  > element.usage) continue;
                 if (IOHIDElementGetUsage(second) > element.usage) continue;
                 if (IOHIDElementGetReportID(first) != IOHIDElementGetReportID(element)) continue;
                 if ((IOHIDElementGetUsage(first) - kHIDUsage_GD_X) / 3 != (element.usage - kHIDUsage_GD_X) / 3) continue;
                 if (IOHIDElementGetParent(first) != IOHIDElementGetParent(element)) continue;
                 
                 [axes2d removeObject:group];
                 [axes3d addObject:@[(__bridge id)first, (__bridge id)second, _element]];
                 found = true;
                 break;
                 }*/
                break;
            }
            single:
            case kHIDUsage_GD_Slider:
            case kHIDUsage_GD_Dial:
            case kHIDUsage_GD_Wheel: {
                JOYAxis *axis = [[JOYAxis alloc] initWithElement: element];
                [_axes setObject:axis forKey:element];
                
                NSNumber *replacementUsage = _hacks[JOYAxisUsageMapping][@(axis.usage)];
                if (replacementUsage) {
                    axis.usage = [replacementUsage unsignedIntValue];
                }
                
                if (axesEmulateButtons && axis.usage >= JOYAxisUsageL1 && axis.usage <= JOYAxisUsageR3) {
                    _axisEmulatedButtons[@(axis.uniqueID)] =
                    [[JOYEmulatedButton alloc] initWithUsage:axis.usage - JOYAxisUsageL1 + JOYButtonUsageL1 uniqueID:axis.uniqueID];
                }
                
                if (axesEmulateButtons && axis.usage >= JOYAxisUsageGeneric0) {
                    _axisEmulatedButtons[@(axis.uniqueID)] =
                    [[JOYEmulatedButton alloc] initWithUsage:axis.usage - JOYAxisUsageGeneric0 + JOYButtonUsageGeneric0 uniqueID:axis.uniqueID];
                }
                
                break;
            }
            case kHIDUsage_GD_DPadUp:
            case kHIDUsage_GD_DPadDown:
            case kHIDUsage_GD_DPadRight:
            case kHIDUsage_GD_DPadLeft:
            case kHIDUsage_GD_Start:
            case kHIDUsage_GD_Select:
            case kHIDUsage_GD_SystemMainMenu:
                goto button;
                
            case kHIDUsage_GD_Hatswitch: {
                JOYHat *hat = [[JOYHat alloc] initWithElement: element];
                [_hats setObject:hat forKey:element];
                if (hatsEmulateButtons) {
                    _hatEmulatedButtons[@(hat.uniqueID)] = @[
                        [[JOYEmulatedButton alloc] initWithUsage:JOYButtonUsageDPadLeft  uniqueID:hat.uniqueID | 0x100000000L],
                        [[JOYEmulatedButton alloc] initWithUsage:JOYButtonUsageDPadRight uniqueID:hat.uniqueID | 0x200000000L],
                        [[JOYEmulatedButton alloc] initWithUsage:JOYButtonUsageDPadUp  uniqueID:hat.uniqueID | 0x300000000L],
                        [[JOYEmulatedButton alloc] initWithUsage:JOYButtonUsageDPadDown uniqueID:hat.uniqueID | 0x400000000L],
                    ];
                }
                break;
            }
        }
    }
}

- (instancetype)initWithDevice:(IOHIDDeviceRef)device reportIDFilter:(NSArray <NSNumber *> *) filter serialSuffix:(NSString *)suffix hacks:(NSDictionary *)hacks
{
    self = [super init];
    if (!self) return self;
    
    _physicallyConnected = true;
    _logicallyConnected = true;
    _device = (IOHIDDeviceRef)CFRetain(device);
    _serialSuffix = suffix;
    _playerLEDs = -1;

    IOHIDDeviceRegisterInputValueCallback(device, HIDInput, (void *)self);
    IOHIDDeviceScheduleWithRunLoop(device, CFRunLoopGetCurrent(), kCFRunLoopDefaultMode);

    NSArray *array = CFBridgingRelease(IOHIDDeviceCopyMatchingElements(device, NULL, kIOHIDOptionsTypeNone));
    _buttons = [NSMutableDictionary dictionary];
    _axes = [NSMutableDictionary dictionary];
    _axes2D = [NSMutableDictionary dictionary];
    _hats = [NSMutableDictionary dictionary];
    _axisEmulatedButtons = [NSMutableDictionary dictionary];
    _axes2DEmulatedButtons = [NSMutableDictionary dictionary];
    _hatEmulatedButtons = [NSMutableDictionary dictionary];
    _iokitToJOY = [NSMutableDictionary dictionary];
    
    
    //NSMutableArray *axes3d = [NSMutableArray array];
    
    _hacks = hacks;
    _isSwitch = [_hacks[JOYIsSwitch] boolValue];
    _isDualShock3 = [_hacks[JOYIsDualShock3] boolValue];

    NSDictionary *customReports = hacks[JOYCustomReports];
    _lastReport = [NSMutableData dataWithLength:MAX(
                                                    MAX(
                                                        [(__bridge NSNumber *)IOHIDDeviceGetProperty(device, CFSTR(kIOHIDMaxInputReportSizeKey)) unsignedIntValue],
                                                        [(__bridge NSNumber *)IOHIDDeviceGetProperty(device, CFSTR(kIOHIDMaxOutputReportSizeKey)) unsignedIntValue]
                                                        ),
                                                    [(__bridge NSNumber *)IOHIDDeviceGetProperty(device, CFSTR(kIOHIDMaxFeatureReportSizeKey)) unsignedIntValue]
                                                    )];
    IOHIDDeviceRegisterInputReportCallback(device, _lastReport.mutableBytes, _lastReport.length, HIDReport, (void *)self);

    if (hacks[JOYCustomReports]) {
        _multiElements = [NSMutableDictionary dictionary];
        _fullReportElements = [NSMutableDictionary dictionary];

        
        for (NSNumber *_reportID in customReports) {
            signed reportID = [_reportID intValue];
            bool isOutput = false;
            if (reportID < 0) {
                isOutput = true;
                reportID = -reportID;
            }
            
            JOYFullReportElement *element = [[JOYFullReportElement alloc] initWithDevice:device reportID:reportID];
            NSMutableArray *elements = [NSMutableArray array];
            for (NSDictionary <NSString *,NSNumber *> *subElementDef in customReports[_reportID]) {
                if (filter && subElementDef[@"reportID"] && ![filter containsObject:subElementDef[@"reportID"]]) continue;
                JOYSubElement *subElement = [[JOYSubElement alloc] initWithRealElement:element
                                                                                  size:subElementDef[@"size"].unsignedLongValue
                                                                                offset:subElementDef[@"offset"].unsignedLongValue + 8 // Compensate for the reportID
                                                                             usagePage:subElementDef[@"usagePage"].unsignedLongValue
                                                                                 usage:subElementDef[@"usage"].unsignedLongValue
                                                                                   min:subElementDef[@"min"].unsignedIntValue
                                                                                   max:subElementDef[@"max"].unsignedIntValue];
                [elements addObject:subElement];
                if (isOutput) {
                    [self createOutputForElement:subElement];
                }
                else {
                    [self createInputForElement:subElement];
                }
            }
            _multiElements[element] = elements;
            if (!isOutput) {
                _fullReportElements[@(reportID)] = element;
            }
        }
    }
    
    id previous = nil;
    NSSet *ignoredReports = nil;
    if (hacks[ignoredReports]) {
        ignoredReports = [NSSet setWithArray:hacks[ignoredReports]];
    }
    
    for (id _element in array) {
        if (_element == previous) continue; // Some elements are reported twice for some reason
        previous = _element;
        JOYElement *element = [[JOYElement alloc] initWithElement:(__bridge IOHIDElementRef)_element];

        bool isOutput = false;
        if (filter && ![filter containsObject:@(element.reportID)]) continue;

        switch (IOHIDElementGetType((__bridge IOHIDElementRef)_element)) {
            /* Handled */
            case kIOHIDElementTypeInput_Misc:
            case kIOHIDElementTypeInput_Button:
            case kIOHIDElementTypeInput_Axis:
                break;
            case kIOHIDElementTypeOutput:
                isOutput = true;
                break;
            /* Ignored */
            default:
            case kIOHIDElementTypeInput_ScanCodes:
            case kIOHIDElementTypeInput_NULL:
            case kIOHIDElementTypeFeature:
            case kIOHIDElementTypeCollection:
                continue;
        }
        if ((!isOutput && [ignoredReports containsObject:@(element.reportID)]) ||
            (isOutput && [ignoredReports containsObject:@(-element.reportID)])) continue;

        
        if (IOHIDElementIsArray((__bridge IOHIDElementRef)_element)) continue;
        
        if (isOutput) {
            [self createOutputForElement:element];
        }
        else {
            [self createInputForElement:element];
        }
        
        _iokitToJOY[@(IOHIDElementGetCookie((__bridge IOHIDElementRef)_element))] = element;
    }
    
    [exposedControllers addObject:self];
    if (_logicallyConnected) {
        for (id<JOYListener> listener in listeners) {
            if ([listener respondsToSelector:@selector(controllerConnected:)]) {
                [listener controllerConnected:self];
            }
        }
    }
    
    if (_hacks[JOYActivationReport]) {
        [self sendReport:hacks[JOYActivationReport]];
    }
    
    if (_isSwitch) {
        [self sendReport:[NSData dataWithBytes:(uint8_t[]){0x80, 0x04} length:2]];
        [self sendReport:[NSData dataWithBytes:(uint8_t[]){0x80, 0x02} length:2]];
    }
    
    if (_isDualShock3) {
        _lastVendorSpecificOutput.ds3Output = (JOYDualShock3Output){
            .reportID = 1,
            .led = {
                {.timeEnabled =  0xff, .dutyLength = 0x27, .enabled = 0x10, .dutyOff = 0, .dutyOn = 0x32},
                {.timeEnabled =  0xff, .dutyLength = 0x27, .enabled = 0x10, .dutyOff = 0, .dutyOn = 0x32},
                {.timeEnabled =  0xff, .dutyLength = 0x27, .enabled = 0x10, .dutyOff = 0, .dutyOn = 0x32},
                {.timeEnabled =  0xff, .dutyLength = 0x27, .enabled = 0x10, .dutyOff = 0, .dutyOn = 0x32},
                {.timeEnabled =  0,    .dutyLength = 0,    .enabled = 0,    .dutyOff = 0, .dutyOn = 0},
            }
        };

    }
    
    return self;
}

- (NSString *)deviceName
{
    if (!_device) return nil;
    return IOHIDDeviceGetProperty(_device, CFSTR(kIOHIDProductKey));
}

- (NSString *)uniqueID
{
    if (!_device) return nil;
    NSString *serial = (__bridge NSString *)IOHIDDeviceGetProperty(_device, CFSTR(kIOHIDSerialNumberKey));
    if (!serial || [(__bridge NSString *)IOHIDDeviceGetProperty(_device, CFSTR(kIOHIDTransportKey)) isEqualToString:@"USB"]) {
        serial = [NSString stringWithFormat:@"%04x%04x%08x",
                  [(__bridge NSNumber *)IOHIDDeviceGetProperty(_device, CFSTR(kIOHIDVendorIDKey)) unsignedIntValue],
                  [(__bridge NSNumber *)IOHIDDeviceGetProperty(_device, CFSTR(kIOHIDProductIDKey)) unsignedIntValue],
                  [(__bridge NSNumber *)IOHIDDeviceGetProperty(_device, CFSTR(kIOHIDLocationIDKey)) unsignedIntValue]];
    }
    if (_serialSuffix) {
        return [NSString stringWithFormat:@"%@-%@", serial, _serialSuffix];
    }
    return serial;
}

- (NSString *)description
{
    return [NSString stringWithFormat:@"<%@: %p, %@, %@>", self.className, self, self.deviceName, self.uniqueID];
}

- (NSArray<JOYButton *> *)buttons
{
    NSMutableArray *ret = [[_buttons allValues] mutableCopy];
    [ret addObjectsFromArray:_axisEmulatedButtons.allValues];
    for (NSArray *array in _axes2DEmulatedButtons.allValues) {
        [ret addObjectsFromArray:array];
    }
    for (NSArray *array in _hatEmulatedButtons.allValues) {
        [ret addObjectsFromArray:array];
    }
    return ret;
}

- (NSArray<JOYAxis *> *)axes
{
    return [_axes allValues];
}

- (NSArray<JOYAxes2D *> *)axes2D
{
    return [[NSSet setWithArray:[_axes2D allValues]] allObjects];
}

- (NSArray<JOYHat *> *)hats
{
    return [_hats allValues];
}

- (void)gotReport:(NSData *)report
{
    JOYFullReportElement *element = _fullReportElements[@(*(uint8_t *)report.bytes)];
    if (element) {
        [element updateValue:report];
        
        NSArray<JOYElement *> *subElements = _multiElements[element];
        if (subElements) {
            for (JOYElement *subElement in subElements) {
                [self _elementChanged:subElement];
            }
        }
    }
    [self updateRumble];
}

- (void)elementChanged:(IOHIDElementRef)element
{
    JOYElement *_element = _iokitToJOY[@(IOHIDElementGetCookie(element))];
    if (_element) {
        [self _elementChanged:_element];
    }
    else {
        //NSLog(@"Unhandled usage %x (Cookie: %x, Usage: %x)", IOHIDElementGetUsage(element), IOHIDElementGetCookie(element), IOHIDElementGetUsage(element));
    }
}

- (void)_elementChanged:(JOYElement *)element
{
    if (element == _connectedElement) {
        bool old = self.connected;
        _logicallyConnected = _connectedElement.value != _connectedElement.min;
        if (!old && self.connected) {
            for (id<JOYListener> listener in listeners) {
                if ([listener respondsToSelector:@selector(controllerConnected:)]) {
                    [listener controllerConnected:self];
                }
            }
        }
        else if (old && !self.connected) {
            for (id<JOYListener> listener in listeners) {
                if ([listener respondsToSelector:@selector(controllerDisconnected:)]) {
                    [listener controllerDisconnected:self];
                }
            }
        }
    }
    
    if (!self.connected) return;
    {
        JOYButton *button = _buttons[element];
        if (button) {
            if ([button updateState]) {
                for (id<JOYListener> listener in listeners) {
                    if ([listener respondsToSelector:@selector(controller:buttonChangedState:)]) {
                        [listener controller:self buttonChangedState:button];
                    }
                }
            }
            return;
        }
    }
    

    {
        JOYAxis *axis = _axes[element];
        if (axis) {
            if ([axis updateState])  {
                for (id<JOYListener> listener in listeners) {
                    if ([listener respondsToSelector:@selector(controller:movedAxis:)]) {
                        [listener controller:self movedAxis:axis];
                    }
                }
                JOYEmulatedButton *button = _axisEmulatedButtons[@(axis.uniqueID)];
                if ([button updateStateFromAxis:axis]) {
                    for (id<JOYListener> listener in listeners) {
                        if ([listener respondsToSelector:@selector(controller:buttonChangedState:)]) {
                            [listener controller:self buttonChangedState:button];
                        }
                    }
                }
            }
            return;
        }
    }
    
    {
        JOYAxes2D *axes = _axes2D[element];
        if (axes) {
            if ([axes updateState]) {
                for (id<JOYListener> listener in listeners) {
                    if ([listener respondsToSelector:@selector(controller:movedAxes2D:)]) {
                        [listener controller:self movedAxes2D:axes];
                    }
                }
                NSArray <JOYEmulatedButton *> *buttons = _axes2DEmulatedButtons[@(axes.uniqueID)];
                for (JOYEmulatedButton *button in buttons) {
                    if ([button updateStateFromAxes2D:axes]) {
                        for (id<JOYListener> listener in listeners) {
                            if ([listener respondsToSelector:@selector(controller:buttonChangedState:)]) {
                                [listener controller:self buttonChangedState:button];
                            }
                        }
                    }
                }
            }
            return;
        }
    }
    
    {
        JOYHat *hat = _hats[element];
        if (hat) {
            if ([hat updateState]) {
                for (id<JOYListener> listener in listeners) {
                    if ([listener respondsToSelector:@selector(controller:movedHat:)]) {
                        [listener controller:self movedHat:hat];
                    }
                }
                
                NSArray <JOYEmulatedButton *> *buttons = _hatEmulatedButtons[@(hat.uniqueID)];
                for (JOYEmulatedButton *button in buttons) {
                    if ([button updateStateFromHat:hat]) {
                        for (id<JOYListener> listener in listeners) {
                            if ([listener respondsToSelector:@selector(controller:buttonChangedState:)]) {
                                [listener controller:self buttonChangedState:button];
                            }
                        }
                    }
                }
            }
            return;
        }
    }
}

- (void)disconnected
{
    if (_logicallyConnected && [exposedControllers containsObject:self]) {
        for (id<JOYListener> listener in listeners) {
            if ([listener respondsToSelector:@selector(controllerDisconnected:)]) {
                [listener controllerDisconnected:self];
            }
        }
    }
    _physicallyConnected = false;
    [exposedControllers removeObject:self];
    [self setRumbleAmplitude:0];
    [self updateRumble];
    _device = nil;
}

- (void)sendReport:(NSData *)report
{
    if (!report.length) return;
    if (!_device) return;
    if (_deviceCantSendReports) return;
    /* Some Macs fail to send reports to some devices, specifically the DS3, returning the bogus(?) error code 1 after
       freezing for 5 seconds. Stop sending reports if that's the case. */
    if (IOHIDDeviceSetReport(_device, kIOHIDReportTypeOutput, *(uint8_t *)report.bytes, report.bytes, report.length) == 1) {
        _deviceCantSendReports = true;
        NSLog(@"This Mac appears to be incapable of sending output reports to %@", self);
    }
}

- (void)setPlayerLEDs:(uint8_t)mask
{
    mask &= 0xF;
    if (mask == _playerLEDs) {
        return;
    }
    _playerLEDs = mask;
    if (_isSwitch) {
        _lastVendorSpecificOutput.switchPacket.reportID = 0x1; // Rumble and LEDs
        _lastVendorSpecificOutput.switchPacket.sequence++;
        _lastVendorSpecificOutput.switchPacket.sequence &= 0xF;
        _lastVendorSpecificOutput.switchPacket.command = 0x30; // LED
        _lastVendorSpecificOutput.switchPacket.commandData[0] = mask;
        [self sendReport:[NSData dataWithBytes:&_lastVendorSpecificOutput.switchPacket length:sizeof(_lastVendorSpecificOutput.switchPacket)]];
    }
    else if (_isDualShock3) {
        _lastVendorSpecificOutput.ds3Output.reportID = 1;
        _lastVendorSpecificOutput.ds3Output.ledsEnabled = mask << 1;
        [self sendReport:[NSData dataWithBytes:&_lastVendorSpecificOutput.ds3Output length:sizeof(_lastVendorSpecificOutput.ds3Output)]];
    }
}

- (void)updateRumble
{
    if (!self.connected) {
        return;
    }
    if (!_rumbleElement && !_isSwitch && !_isDualShock3) {
        return;
    }
    if (_rumbleElement.max == 1 && _rumbleElement.min == 0) {
        double ampToSend = _rumbleCounter < round(_rumbleAmplitude * PWM_RESOLUTION);
        if (ampToSend != _sentRumbleAmp) {
            [_rumbleElement setValue:ampToSend];
            _sentRumbleAmp = ampToSend;
        }
        _rumbleCounter += round(_rumbleAmplitude * PWM_RESOLUTION);
        if (_rumbleCounter >= PWM_RESOLUTION) {
            _rumbleCounter -= PWM_RESOLUTION;
        }
    }
    else {
        if (_rumbleAmplitude == _sentRumbleAmp) {
            return;
        }
        _sentRumbleAmp = _rumbleAmplitude;
        if (_isSwitch) {
            double frequency = 144;
            double amp = _rumbleAmplitude;
            
            uint8_t highAmp = amp * 0x64;
            uint8_t lowAmp = amp * 0x32 + 0x40;
            if (frequency < 0) frequency = 0;
            if (frequency > 1252) frequency = 1252;
            uint8_t encodedFrequency = (uint8_t)round(log2(frequency / 10.0) * 32.0);
            
            uint16_t highFreq = (encodedFrequency - 0x60) * 4;
            uint8_t lowFreq = encodedFrequency - 0x40;
            
            //if (frequency < 82 || frequency > 312) {
            if (amp) {
                highAmp = 0;
            }
            
            if (frequency < 40 || frequency > 626) {
                lowAmp = 0;
            }
            
            _lastVendorSpecificOutput.switchPacket.rumbleData[0] = _lastVendorSpecificOutput.switchPacket.rumbleData[4] = highFreq & 0xFF;
            _lastVendorSpecificOutput.switchPacket.rumbleData[1] = _lastVendorSpecificOutput.switchPacket.rumbleData[5] = (highAmp << 1) + ((highFreq >> 8) & 0x1);
            _lastVendorSpecificOutput.switchPacket.rumbleData[2] = _lastVendorSpecificOutput.switchPacket.rumbleData[6] = lowFreq;
            _lastVendorSpecificOutput.switchPacket.rumbleData[3] = _lastVendorSpecificOutput.switchPacket.rumbleData[7] = lowAmp;
            
            
            _lastVendorSpecificOutput.switchPacket.reportID = 0x10; // Rumble only
            _lastVendorSpecificOutput.switchPacket.sequence++;
            _lastVendorSpecificOutput.switchPacket.sequence &= 0xF;
            _lastVendorSpecificOutput.switchPacket.command = 0; // LED
            [self sendReport:[NSData dataWithBytes:&_lastVendorSpecificOutput.switchPacket length:sizeof(_lastVendorSpecificOutput.switchPacket)]];
        }
        else if (_isDualShock3) {
            _lastVendorSpecificOutput.ds3Output.reportID = 1;
            _lastVendorSpecificOutput.ds3Output.rumbleLeftDuration = _lastVendorSpecificOutput.ds3Output.rumbleRightDuration = _rumbleAmplitude? 0xff : 0;
            _lastVendorSpecificOutput.ds3Output.rumbleLeftStrength = _lastVendorSpecificOutput.ds3Output.rumbleRightStrength = round(_rumbleAmplitude * 0xff);
            [self sendReport:[NSData dataWithBytes:&_lastVendorSpecificOutput.ds3Output length:sizeof(_lastVendorSpecificOutput.ds3Output)]];
        }
        else {
            [_rumbleElement setValue:_rumbleAmplitude * (_rumbleElement.max - _rumbleElement.min) + _rumbleElement.min];
        }
    }
}

- (void)setRumbleAmplitude:(double)amp /* andFrequency: (double)frequency */
{
    if (amp < 0) amp = 0;
    if (amp > 1) amp = 1;
    _rumbleAmplitude = amp;
}

- (bool)isConnected
{
    return _logicallyConnected && _physicallyConnected;
}

+ (void)controllerAdded:(IOHIDDeviceRef) device
{
    NSString *name = (__bridge NSString *)IOHIDDeviceGetProperty(device, CFSTR(kIOHIDProductKey));
    NSDictionary *hacks = hacksByName[name];
    if (!hacks) {
        hacks = hacksByManufacturer[(__bridge NSNumber *)IOHIDDeviceGetProperty(device, CFSTR(kIOHIDVendorIDKey))];
    }
    NSArray *filters = hacks[JOYReportIDFilters];
    JOYController *controller = nil;
    if (filters) {
        controller = [[JOYMultiplayerController alloc] initWithDevice:device
                                                      reportIDFilters:filters
                                                                hacks:hacks];
    }
    else {
        controller = [[JOYController alloc] initWithDevice:device hacks:hacks];
    }
        
    [controllers setObject:controller forKey:[NSValue valueWithPointer:device]];


}

+ (void)controllerRemoved:(IOHIDDeviceRef) device
{
    [[controllers objectForKey:[NSValue valueWithPointer:device]] disconnected];
    [controllers removeObjectForKey:[NSValue valueWithPointer:device]];
}

+ (NSArray<JOYController *> *)allControllers
{
    return exposedControllers;
}

+ (void)load
{
#include "ControllerConfiguration.inc"
}

+(void)registerListener:(id<JOYListener>)listener
{
    [listeners addObject:listener];
}

+(void)unregisterListener:(id<JOYListener>)listener
{
    [listeners removeObject:listener];
}

+ (void)startOnRunLoop:(NSRunLoop *)runloop withOptions: (NSDictionary *)options
{
    axesEmulateButtons = [options[JOYAxesEmulateButtonsKey] boolValue];
    axes2DEmulateButtons = [options[JOYAxes2DEmulateButtonsKey] boolValue];
    hatsEmulateButtons = [options[JOYHatsEmulateButtonsKey] boolValue];
    
    controllers = [NSMutableDictionary dictionary];
    exposedControllers = [NSMutableArray array];
    NSArray *array = @[
        CreateHIDDeviceMatchDictionary(kHIDPage_GenericDesktop, kHIDUsage_GD_Joystick),
        CreateHIDDeviceMatchDictionary(kHIDPage_GenericDesktop, kHIDUsage_GD_GamePad),
        CreateHIDDeviceMatchDictionary(kHIDPage_GenericDesktop, kHIDUsage_GD_MultiAxisController),
        @{@kIOHIDDeviceUsagePageKey: @(kHIDPage_Game)},
    ];

    listeners = [NSMutableSet set];
    static IOHIDManagerRef manager = nil;
    if (manager) {
        CFRelease(manager); // Stop the previous session
    }
    manager = IOHIDManagerCreate(kCFAllocatorDefault, kIOHIDOptionsTypeNone);
    
    if (!manager) return;
    if (IOHIDManagerOpen(manager, kIOHIDOptionsTypeNone)) {
        CFRelease(manager);
        return;
    }
    
    IOHIDManagerSetDeviceMatchingMultiple(manager, (__bridge CFArrayRef)array);
    IOHIDManagerRegisterDeviceMatchingCallback(manager, HIDDeviceAdded, NULL);
    IOHIDManagerRegisterDeviceRemovalCallback(manager, HIDDeviceRemoved, NULL);
    IOHIDManagerScheduleWithRunLoop(manager, [runloop getCFRunLoop], kCFRunLoopDefaultMode);
}

- (void)dealloc
{
    if (_device) {
        CFRelease(_device);
        _device = NULL;
    }
}
@end

#import <Foundation/Foundation.h>

typedef enum {
    JOYAxes2DUsageNone,
    JOYAxes2DUsageLeftStick,
    JOYAxes2DUsageRightStick,
    JOYAxes2DUsageMiddleStick,
    JOYAxes2DUsagePointer,
    JOYAxes2DUsageNonGenericMax,
    
    JOYAxes2DUsageGeneric0 = 0x10000,
} JOYAxes2DUsage;

@interface JOYAxes2D : NSObject
- (NSString *)usageString;
+ (NSString *)usageToString: (JOYAxes2DUsage) usage;
- (uint64_t)uniqueID;
- (double)distance;
- (double)angle;
- (NSPoint)value;
@property JOYAxes2DUsage usage;
@end



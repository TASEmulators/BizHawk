#import <Foundation/Foundation.h>

@interface JOYHat : NSObject
- (uint64_t)uniqueID;
- (double)angle;
- (unsigned)resolution;
@property (readonly, getter=isPressed) bool pressed;

@end



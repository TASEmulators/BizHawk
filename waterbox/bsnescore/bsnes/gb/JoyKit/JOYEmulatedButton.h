#import "JOYButton.h"
#import "JOYAxis.h"
#import "JOYAxes2D.h"
#import "JOYHat.h"

@interface JOYEmulatedButton : JOYButton
- (instancetype)initWithUsage:(JOYButtonUsage)usage uniqueID:(uint64_t)uniqueID;
- (bool)updateStateFromAxis:(JOYAxis *)axis;
- (bool)updateStateFromAxes2D:(JOYAxes2D *)axes;
- (bool)updateStateFromHat:(JOYHat *)hat;
@end

#import <Foundation/Foundation.h>
#import "JOYButton.h"
#import "JOYAxis.h"
#import "JOYAxes2D.h"
#import "JOYHat.h"

static NSString const *JOYAxesEmulateButtonsKey = @"JOYAxesEmulateButtons";
static NSString const *JOYAxes2DEmulateButtonsKey = @"JOYAxes2DEmulateButtons";
static NSString const *JOYHatsEmulateButtonsKey = @"JOYHatsEmulateButtons";

@class JOYController;

@protocol JOYListener <NSObject>

@optional
-(void) controllerConnected:(JOYController *)controller;
-(void) controllerDisconnected:(JOYController *)controller;
-(void) controller:(JOYController *)controller buttonChangedState:(JOYButton *)button;
-(void) controller:(JOYController *)controller movedAxis:(JOYAxis *)axis;
-(void) controller:(JOYController *)controller movedAxes2D:(JOYAxes2D *)axes;
-(void) controller:(JOYController *)controller movedHat:(JOYHat *)hat;

@end

@interface JOYController : NSObject
+ (void)startOnRunLoop:(NSRunLoop *)runloop withOptions: (NSDictionary *)options;
+ (NSArray<JOYController *> *) allControllers;
+ (void) registerListener:(id<JOYListener>)listener;
+ (void) unregisterListener:(id<JOYListener>)listener;
- (NSString *)deviceName;
- (NSString *)uniqueID;
- (NSArray<JOYButton *> *) buttons;
- (NSArray<JOYAxis *> *) axes;
- (NSArray<JOYAxes2D *> *) axes2D;
- (NSArray<JOYHat *> *) hats;
- (void)setRumbleAmplitude:(double)amp;
- (void)setPlayerLEDs:(uint8_t)mask;
@property (readonly, getter=isConnected) bool connected;
@end



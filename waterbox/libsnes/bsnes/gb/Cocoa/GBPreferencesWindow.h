#import <Cocoa/Cocoa.h>
#import <JoyKit/JoyKit.h>

@interface GBPreferencesWindow : NSWindow <NSTableViewDelegate, NSTableViewDataSource, JOYListener>
@property IBOutlet NSTableView *controlsTableView;
@property IBOutlet NSPopUpButton *graphicsFilterPopupButton;
@property (strong) IBOutlet NSButton *analogControlsCheckbox;
@property (strong) IBOutlet NSButton *aspectRatioCheckbox;
@property (strong) IBOutlet NSPopUpButton *highpassFilterPopupButton;
@property (strong) IBOutlet NSPopUpButton *colorCorrectionPopupButton;
@property (strong) IBOutlet NSPopUpButton *frameBlendingModePopupButton;
@property (strong) IBOutlet NSPopUpButton *colorPalettePopupButton;
@property (strong) IBOutlet NSPopUpButton *displayBorderPopupButton;
@property (strong) IBOutlet NSPopUpButton *rewindPopupButton;
@property (strong) IBOutlet NSButton *configureJoypadButton;
@property (strong) IBOutlet NSButton *skipButton;
@property (strong) IBOutlet NSMenuItem *bootROMsFolderItem;
@property (strong) IBOutlet NSPopUpButtonCell *bootROMsButton;
@property (strong) IBOutlet NSPopUpButton *rumbleModePopupButton;

@property (weak) IBOutlet NSPopUpButton *dmgPopupButton;
@property (weak) IBOutlet NSPopUpButton *sgbPopupButton;
@property (weak) IBOutlet NSPopUpButton *cgbPopupButton;
@property (weak) IBOutlet NSPopUpButton *preferredJoypadButton;
@property (weak) IBOutlet NSPopUpButton *playerListButton;

@end

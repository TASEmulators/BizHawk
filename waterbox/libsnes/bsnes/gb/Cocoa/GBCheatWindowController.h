#import <Foundation/Foundation.h>
#import <AppKit/AppKit.h>
#import "Document.h"

@interface GBCheatWindowController : NSObject <NSTableViewDelegate, NSTableViewDataSource, NSTextFieldDelegate>
@property (weak) IBOutlet NSTableView *cheatsTable;
@property (weak) IBOutlet NSTextField *addressField;
@property (weak) IBOutlet NSTextField *valueField;
@property (weak) IBOutlet NSTextField *oldValueField;
@property (weak) IBOutlet NSButton *oldValueCheckbox;
@property (weak) IBOutlet NSTextField *descriptionField;
@property (weak) IBOutlet NSTextField *importCodeField;
@property (weak) IBOutlet NSTextField *importDescriptionField;
@property (weak) IBOutlet Document *document;
- (void)cheatsUpdated;
@end


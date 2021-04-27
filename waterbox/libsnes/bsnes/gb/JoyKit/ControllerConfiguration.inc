#define BUTTON(x) @(JOYButtonUsageGeneric0 + (x))
#define AXIS(x) @(JOYAxisUsageGeneric0 + (x))
#define AXES2D(x) @(JOYAxes2DUsageGeneric0 + (x))

hacksByManufacturer = @{
    @(0x045E): @{ // Microsoft
        /* Generally untested, but Microsoft goes by the book when it comes to HID report descriptors, so
           it should work out of the box. The hack is only here for automatic mapping */
            
        JOYAxisGroups: @{
              @(kHIDUsage_GD_X): @(0),
              @(kHIDUsage_GD_Y): @(0),
              @(kHIDUsage_GD_Z): @(2),
              @(kHIDUsage_GD_Rx): @(1),
              @(kHIDUsage_GD_Ry): @(1),
              @(kHIDUsage_GD_Rz): @(3),
        },
        
        JOYButtonUsageMapping: @{
              BUTTON(1): @(JOYButtonUsageA),
              BUTTON(2): @(JOYButtonUsageB),
              BUTTON(3): @(JOYButtonUsageX),
              BUTTON(4): @(JOYButtonUsageY),
              BUTTON(5): @(JOYButtonUsageL1),
              BUTTON(6): @(JOYButtonUsageR1),
              BUTTON(7): @(JOYButtonUsageLStick),
              BUTTON(8): @(JOYButtonUsageRStick),
              BUTTON(9): @(JOYButtonUsageStart),
              BUTTON(10): @(JOYButtonUsageSelect),
              BUTTON(11): @(JOYButtonUsageHome),
         },
        
         JOYAxisUsageMapping: @{
             AXIS(3): @(JOYAxisUsageL1),
             AXIS(6): @(JOYAxisUsageR1),
         },
         
         JOYAxes2DUsageMapping: @{
             AXES2D(1): @(JOYAxes2DUsageLeftStick),
             AXES2D(4): @(JOYAxes2DUsageRightStick),
         },
    },
    
    @(0x054C): @{ // Sony
        /* Generally untested, but should work */
            
        JOYAxisGroups: @{
              @(kHIDUsage_GD_X): @(0),
              @(kHIDUsage_GD_Y): @(0),
              @(kHIDUsage_GD_Z): @(1),
              @(kHIDUsage_GD_Rx): @(2),
              @(kHIDUsage_GD_Ry): @(3),
              @(kHIDUsage_GD_Rz): @(1),
        },
        
        JOYButtonUsageMapping: @{
              BUTTON(1): @(JOYButtonUsageY),
              BUTTON(2): @(JOYButtonUsageB),
              BUTTON(3): @(JOYButtonUsageA),
              BUTTON(4): @(JOYButtonUsageX),
              BUTTON(5): @(JOYButtonUsageL1),
              BUTTON(6): @(JOYButtonUsageR1),
              BUTTON(7): @(JOYButtonUsageL2),
              BUTTON(8): @(JOYButtonUsageR2),
              BUTTON(9): @(JOYButtonUsageSelect),
              BUTTON(10): @(JOYButtonUsageStart),
              BUTTON(11): @(JOYButtonUsageLStick),
              BUTTON(12): @(JOYButtonUsageRStick),
              BUTTON(13): @(JOYButtonUsageHome),
              BUTTON(14): @(JOYButtonUsageMisc),
         },
        
         JOYAxisUsageMapping: @{
             AXIS(4): @(JOYAxisUsageL1),
             AXIS(5): @(JOYAxisUsageR1),
         },
         
         JOYAxes2DUsageMapping: @{
             AXES2D(1): @(JOYAxes2DUsageLeftStick),
             AXES2D(4): @(JOYAxes2DUsageRightStick),
         },
    }
};

hacksByName = @{
    @"WUP-028": @{ // Nintendo GameCube Controller Adapter
        JOYReportIDFilters: @[@[@1], @[@2], @[@3], @[@4]],
        JOYButtonUsageMapping: @{
            BUTTON(1): @(JOYButtonUsageA),
            BUTTON(2): @(JOYButtonUsageB),
            BUTTON(3): @(JOYButtonUsageX),
            BUTTON(4): @(JOYButtonUsageY),
            BUTTON(5): @(JOYButtonUsageStart),
            BUTTON(6): @(JOYButtonUsageZ),
            BUTTON(7): @(JOYButtonUsageR1),
            BUTTON(8): @(JOYButtonUsageL1),
        },
        
        JOYAxisUsageMapping: @{
            AXIS(3): @(JOYAxisUsageL1),
            AXIS(6): @(JOYAxisUsageR1),
        },
        
        JOYAxes2DUsageMapping: @{
            AXES2D(1): @(JOYAxes2DUsageLeftStick),
            AXES2D(4): @(JOYAxes2DUsageRightStick),
        },
        
        JOYAxisGroups: @{
            @(kHIDUsage_GD_X): @(0),
            @(kHIDUsage_GD_Y): @(0),
            @(kHIDUsage_GD_Z): @(2),
            @(kHIDUsage_GD_Rx): @(1),
            @(kHIDUsage_GD_Ry): @(1),
            @(kHIDUsage_GD_Rz): @(3),
        },
        
        JOYRumbleUsage: @1,
        JOYRumbleUsagePage: @0xFF00,
        
        JOYConnectedUsage: @2,
        JOYConnectedUsagePage: @0xFF00,
        
        JOYActivationReport: [NSData dataWithBytes:(uint8_t[]){0x13} length:1],

        JOYCustomReports: @{
            
            // Rumble
            @(-17): @[
                @{@"reportID": @(1), @"size":@1, @"offset":@0,  @"usagePage":@(0xFF00), @"usage":@1, @"min": @0, @"max": @1},
                @{@"reportID": @(2), @"size":@1, @"offset":@8,  @"usagePage":@(0xFF00), @"usage":@1, @"min": @0, @"max": @1},
                @{@"reportID": @(3), @"size":@1, @"offset":@16, @"usagePage":@(0xFF00), @"usage":@1, @"min": @0, @"max": @1},
                @{@"reportID": @(4), @"size":@1, @"offset":@24, @"usagePage":@(0xFF00), @"usage":@1, @"min": @0, @"max": @1},
            ],
            
            @(33): @[
            
            // Player 1
            
            @{@"reportID": @(1), @"size":@1, @"offset":@4, @"usagePage":@(0xFF00), @"usage":@2, @"min": @0, @"max": @1},
                                        
            @{@"reportID": @(1), @"size":@1, @"offset":@8, @"usagePage":@(kHIDPage_Button), @"usage":@1},
            @{@"reportID": @(1), @"size":@1, @"offset":@9, @"usagePage":@(kHIDPage_Button), @"usage":@2},
            @{@"reportID": @(1), @"size":@1, @"offset":@10,@"usagePage":@(kHIDPage_Button), @"usage":@3},
            @{@"reportID": @(1), @"size":@1, @"offset":@11,@"usagePage":@(kHIDPage_Button), @"usage":@4},
                
            @{@"reportID": @(1), @"size":@1, @"offset":@12, @"usagePage":@(kHIDPage_GenericDesktop), @"usage":@(kHIDUsage_GD_DPadLeft)},
            @{@"reportID": @(1), @"size":@1, @"offset":@13, @"usagePage":@(kHIDPage_GenericDesktop), @"usage":@(kHIDUsage_GD_DPadRight)},
            @{@"reportID": @(1), @"size":@1, @"offset":@14, @"usagePage":@(kHIDPage_GenericDesktop), @"usage":@(kHIDUsage_GD_DPadDown)},
            @{@"reportID": @(1), @"size":@1, @"offset":@15, @"usagePage":@(kHIDPage_GenericDesktop), @"usage":@(kHIDUsage_GD_DPadUp)},
            
            
            @{@"reportID": @(1), @"size":@1, @"offset":@16, @"usagePage":@(kHIDPage_Button), @"usage":@5},
            @{@"reportID": @(1), @"size":@1, @"offset":@17, @"usagePage":@(kHIDPage_Button), @"usage":@6},
            @{@"reportID": @(1), @"size":@1, @"offset":@18, @"usagePage":@(kHIDPage_Button), @"usage":@7},
            @{@"reportID": @(1), @"size":@1, @"offset":@19, @"usagePage":@(kHIDPage_Button), @"usage":@8},
            
            @{@"reportID": @(1), @"size":@8, @"offset":@24, @"usagePage":@(kHIDPage_GenericDesktop), @"usage":@(kHIDUsage_GD_X), @"min": @0, @"max": @255},
            @{@"reportID": @(1), @"size":@8, @"offset":@32, @"usagePage":@(kHIDPage_GenericDesktop), @"usage":@(kHIDUsage_GD_Y), @"min": @255, @"max": @0},

            @{@"reportID": @(1), @"size":@8, @"offset":@40, @"usagePage":@(kHIDPage_GenericDesktop), @"usage":@(kHIDUsage_GD_Rx), @"min": @0, @"max": @255},
            @{@"reportID": @(1), @"size":@8, @"offset":@48, @"usagePage":@(kHIDPage_GenericDesktop), @"usage":@(kHIDUsage_GD_Ry), @"min": @255, @"max": @0},

            @{@"reportID": @(1), @"size":@8, @"offset":@56, @"usagePage":@(kHIDPage_GenericDesktop), @"usage":@(kHIDUsage_GD_Z), @"min": @0, @"max": @255},
            @{@"reportID": @(1), @"size":@8, @"offset":@64, @"usagePage":@(kHIDPage_GenericDesktop), @"usage":@(kHIDUsage_GD_Rz), @"min": @0, @"max": @255},
            
            // Player 2
            
            @{@"reportID": @(2), @"size":@1, @"offset":@(4 + 72), @"usagePage":@(0xFF00), @"usage":@2, @"min": @0, @"max": @1},

            
            @{@"reportID": @(2), @"size":@1, @"offset":@(8 + 72), @"usagePage":@(kHIDPage_Button), @"usage":@1},
            @{@"reportID": @(2), @"size":@1, @"offset":@(9 + 72), @"usagePage":@(kHIDPage_Button), @"usage":@2},
            @{@"reportID": @(2), @"size":@1, @"offset":@(10 + 72), @"usagePage":@(kHIDPage_Button), @"usage":@3},
            @{@"reportID": @(2), @"size":@1, @"offset":@(11 + 72), @"usagePage":@(kHIDPage_Button), @"usage":@4},
                
            @{@"reportID": @(2), @"size":@1, @"offset":@(12 + 72),  @"usagePage":@(kHIDPage_GenericDesktop), @"usage":@(kHIDUsage_GD_DPadLeft)},
            @{@"reportID": @(2), @"size":@1, @"offset":@(13 + 72),  @"usagePage":@(kHIDPage_GenericDesktop), @"usage":@(kHIDUsage_GD_DPadRight)},
            @{@"reportID": @(2), @"size":@1, @"offset":@(14 + 72), @"usagePage":@(kHIDPage_GenericDesktop), @"usage":@(kHIDUsage_GD_DPadDown)},
            @{@"reportID": @(2), @"size":@1, @"offset":@(15 + 72), @"usagePage":@(kHIDPage_GenericDesktop), @"usage":@(kHIDUsage_GD_DPadUp)},
            
            
            @{@"reportID": @(2), @"size":@1, @"offset":@(16 + 72), @"usagePage":@(kHIDPage_Button), @"usage":@5},
            @{@"reportID": @(2), @"size":@1, @"offset":@(17 + 72), @"usagePage":@(kHIDPage_Button), @"usage":@6},
            @{@"reportID": @(2), @"size":@1, @"offset":@(18 + 72), @"usagePage":@(kHIDPage_Button), @"usage":@7},
            @{@"reportID": @(2), @"size":@1, @"offset":@(19 + 72), @"usagePage":@(kHIDPage_Button), @"usage":@8},
            
            @{@"reportID": @(2), @"size":@8, @"offset":@(24 + 72), @"usagePage":@(kHIDPage_GenericDesktop), @"usage":@(kHIDUsage_GD_X), @"min": @0, @"max": @255},
            @{@"reportID": @(2), @"size":@8, @"offset":@(32 + 72), @"usagePage":@(kHIDPage_GenericDesktop), @"usage":@(kHIDUsage_GD_Y), @"min": @255, @"max": @0},

            @{@"reportID": @(2), @"size":@8, @"offset":@(40 + 72), @"usagePage":@(kHIDPage_GenericDesktop), @"usage":@(kHIDUsage_GD_Rx), @"min": @0, @"max": @255},
            @{@"reportID": @(2), @"size":@8, @"offset":@(48 + 72), @"usagePage":@(kHIDPage_GenericDesktop), @"usage":@(kHIDUsage_GD_Ry), @"min": @255, @"max": @0},
            
            @{@"reportID": @(2), @"size":@8, @"offset":@(56 + 72), @"usagePage":@(kHIDPage_GenericDesktop), @"usage":@(kHIDUsage_GD_Z), @"min": @0, @"max": @255},
            @{@"reportID": @(2), @"size":@8, @"offset":@(64 + 72), @"usagePage":@(kHIDPage_GenericDesktop), @"usage":@(kHIDUsage_GD_Rz), @"min": @0, @"max": @255},
            
            // Player 3
            
            @{@"reportID": @(3), @"size":@1, @"offset":@(4 + 144), @"usagePage":@(0xFF00), @"usage":@2, @"min": @0, @"max": @1},
            
            @{@"reportID": @(3), @"size":@1, @"offset":@(8 + 144), @"usagePage":@(kHIDPage_Button), @"usage":@1},
            @{@"reportID": @(3), @"size":@1, @"offset":@(9 + 144), @"usagePage":@(kHIDPage_Button), @"usage":@2},
            @{@"reportID": @(3), @"size":@1, @"offset":@(10 + 144), @"usagePage":@(kHIDPage_Button), @"usage":@3},
            @{@"reportID": @(3), @"size":@1, @"offset":@(11 + 144), @"usagePage":@(kHIDPage_Button), @"usage":@4},
                
            @{@"reportID": @(3), @"size":@1, @"offset":@(12 + 144),  @"usagePage":@(kHIDPage_GenericDesktop), @"usage":@(kHIDUsage_GD_DPadLeft)},
            @{@"reportID": @(3), @"size":@1, @"offset":@(13 + 144),  @"usagePage":@(kHIDPage_GenericDesktop), @"usage":@(kHIDUsage_GD_DPadRight)},
            @{@"reportID": @(3), @"size":@1, @"offset":@(14 + 144), @"usagePage":@(kHIDPage_GenericDesktop), @"usage":@(kHIDUsage_GD_DPadDown)},
            @{@"reportID": @(3), @"size":@1, @"offset":@(15 + 144), @"usagePage":@(kHIDPage_GenericDesktop), @"usage":@(kHIDUsage_GD_DPadUp)},
            
            
            @{@"reportID": @(3), @"size":@1, @"offset":@(16 + 144), @"usagePage":@(kHIDPage_Button), @"usage":@5},
            @{@"reportID": @(3), @"size":@1, @"offset":@(17 + 144), @"usagePage":@(kHIDPage_Button), @"usage":@6},
            @{@"reportID": @(3), @"size":@1, @"offset":@(18 + 144), @"usagePage":@(kHIDPage_Button), @"usage":@7},
            @{@"reportID": @(3), @"size":@1, @"offset":@(19 + 144), @"usagePage":@(kHIDPage_Button), @"usage":@8},
            
            @{@"reportID": @(3), @"size":@8, @"offset":@(24 + 144), @"usagePage":@(kHIDPage_GenericDesktop), @"usage":@(kHIDUsage_GD_X), @"min": @0, @"max": @255},
            @{@"reportID": @(3), @"size":@8, @"offset":@(32 + 144), @"usagePage":@(kHIDPage_GenericDesktop), @"usage":@(kHIDUsage_GD_Y), @"min": @255, @"max": @0},

            @{@"reportID": @(3), @"size":@8, @"offset":@(40 + 144), @"usagePage":@(kHIDPage_GenericDesktop), @"usage":@(kHIDUsage_GD_Rx), @"min": @0, @"max": @255},
            @{@"reportID": @(3), @"size":@8, @"offset":@(48 + 144), @"usagePage":@(kHIDPage_GenericDesktop), @"usage":@(kHIDUsage_GD_Ry), @"min": @255, @"max": @0},
            
            @{@"reportID": @(3), @"size":@8, @"offset":@(56 + 144), @"usagePage":@(kHIDPage_GenericDesktop), @"usage":@(kHIDUsage_GD_Z), @"min": @0, @"max": @255},
            @{@"reportID": @(3), @"size":@8, @"offset":@(64 + 144), @"usagePage":@(kHIDPage_GenericDesktop), @"usage":@(kHIDUsage_GD_Rz), @"min": @0, @"max": @255},

            // Player 4
            
            @{@"reportID": @(4), @"size":@1, @"offset":@(4 + 216), @"usagePage":@(0xFF00), @"usage":@2, @"min": @0, @"max": @1},
            
            @{@"reportID": @(4), @"size":@1, @"offset":@(8 + 216), @"usagePage":@(kHIDPage_Button), @"usage":@1},
            @{@"reportID": @(4), @"size":@1, @"offset":@(9 + 216), @"usagePage":@(kHIDPage_Button), @"usage":@2},
            @{@"reportID": @(4), @"size":@1, @"offset":@(10 + 216), @"usagePage":@(kHIDPage_Button), @"usage":@3},
            @{@"reportID": @(4), @"size":@1, @"offset":@(11 + 216), @"usagePage":@(kHIDPage_Button), @"usage":@4},
                
            @{@"reportID": @(4), @"size":@1, @"offset":@(12 + 216),  @"usagePage":@(kHIDPage_GenericDesktop), @"usage":@(kHIDUsage_GD_DPadLeft)},
            @{@"reportID": @(4), @"size":@1, @"offset":@(13 + 216),  @"usagePage":@(kHIDPage_GenericDesktop), @"usage":@(kHIDUsage_GD_DPadRight)},
            @{@"reportID": @(4), @"size":@1, @"offset":@(14 + 216), @"usagePage":@(kHIDPage_GenericDesktop), @"usage":@(kHIDUsage_GD_DPadDown)},
            @{@"reportID": @(4), @"size":@1, @"offset":@(15 + 216), @"usagePage":@(kHIDPage_GenericDesktop), @"usage":@(kHIDUsage_GD_DPadUp)},
            
            
            @{@"reportID": @(4), @"size":@1, @"offset":@(16 + 216), @"usagePage":@(kHIDPage_Button), @"usage":@5},
            @{@"reportID": @(4), @"size":@1, @"offset":@(17 + 216), @"usagePage":@(kHIDPage_Button), @"usage":@6},
            @{@"reportID": @(4), @"size":@1, @"offset":@(18 + 216), @"usagePage":@(kHIDPage_Button), @"usage":@7},
            @{@"reportID": @(4), @"size":@1, @"offset":@(19 + 216), @"usagePage":@(kHIDPage_Button), @"usage":@8},
            
            @{@"reportID": @(4), @"size":@8, @"offset":@(24 + 216), @"usagePage":@(kHIDPage_GenericDesktop), @"usage":@(kHIDUsage_GD_X), @"min": @0, @"max": @255},
            @{@"reportID": @(4), @"size":@8, @"offset":@(32 + 216), @"usagePage":@(kHIDPage_GenericDesktop), @"usage":@(kHIDUsage_GD_Y), @"min": @255, @"max": @0},

            @{@"reportID": @(4), @"size":@8, @"offset":@(40 + 216), @"usagePage":@(kHIDPage_GenericDesktop), @"usage":@(kHIDUsage_GD_Rx), @"min": @0, @"max": @255},
            @{@"reportID": @(4), @"size":@8, @"offset":@(48 + 216), @"usagePage":@(kHIDPage_GenericDesktop), @"usage":@(kHIDUsage_GD_Ry), @"min": @255, @"max": @0},
            
            @{@"reportID": @(4), @"size":@8, @"offset":@(56 + 216), @"usagePage":@(kHIDPage_GenericDesktop), @"usage":@(kHIDUsage_GD_Z), @"min": @0, @"max": @255},
            @{@"reportID": @(4), @"size":@8, @"offset":@(64 + 216), @"usagePage":@(kHIDPage_GenericDesktop), @"usage":@(kHIDUsage_GD_Rz), @"min": @0, @"max": @255},

            
        ]},
    },
    
    @"GameCube Controller Adapter": @{ // GameCube Controller PC Adapter
        JOYAxisGroups: @{
            @(kHIDUsage_GD_X): @(0),
            @(kHIDUsage_GD_Y): @(0),
            @(kHIDUsage_GD_Z): @(1),
            @(kHIDUsage_GD_Rx): @(2),
            @(kHIDUsage_GD_Ry): @(3),
            @(kHIDUsage_GD_Rz): @(1),
        },
        JOYReportIDFilters: @[@[@1], @[@2], @[@3], @[@4]],
        JOYButtonUsageMapping: @{
            BUTTON(1): @(JOYButtonUsageX),
            BUTTON(2): @(JOYButtonUsageA),
            BUTTON(3): @(JOYButtonUsageB),
            BUTTON(4): @(JOYButtonUsageY),
            BUTTON(5): @(JOYButtonUsageL1),
            BUTTON(6): @(JOYButtonUsageR1),
            BUTTON(8): @(JOYButtonUsageZ),
            BUTTON(10): @(JOYButtonUsageStart),
            BUTTON(13): @(JOYButtonUsageDPadUp),
            BUTTON(14): @(JOYButtonUsageDPadRight),
            BUTTON(15): @(JOYButtonUsageDPadDown),
            BUTTON(16): @(JOYButtonUsageDPadLeft),
        },
        
        JOYAxisUsageMapping: @{
            AXIS(4): @(JOYAxisUsageL1),
            AXIS(5): @(JOYAxisUsageR1),
        },
        
        JOYAxes2DUsageMapping: @{
            AXES2D(1): @(JOYAxes2DUsageLeftStick),
            AXES2D(3): @(JOYAxes2DUsageRightStick),
        },
        
        JOYRumbleUsage: @1,
        JOYRumbleUsagePage: @0xFF00,
        JOYRumbleMin: @0,
        JOYRumbleMax: @255,
        JOYSwapZRz: @YES,
    },
    
    @"Twin USB Joystick": @{ // DualShock PC Adapter
        JOYAxisGroups: @{
            @(kHIDUsage_GD_X): @(0),
            @(kHIDUsage_GD_Y): @(0),
            @(kHIDUsage_GD_Z): @(1),
            @(kHIDUsage_GD_Rx): @(2),
            @(kHIDUsage_GD_Ry): @(2),
            @(kHIDUsage_GD_Rz): @(1),
        },
        JOYReportIDFilters: @[@[@1], @[@2]],
        JOYButtonUsageMapping: @{
            BUTTON(1): @(JOYButtonUsageX),
            BUTTON(2): @(JOYButtonUsageA),
            BUTTON(3): @(JOYButtonUsageB),
            BUTTON(4): @(JOYButtonUsageY),
            BUTTON(5): @(JOYButtonUsageL2),
            BUTTON(6): @(JOYButtonUsageR2),
            BUTTON(7): @(JOYButtonUsageL1),
            BUTTON(8): @(JOYButtonUsageR1),
            BUTTON(9): @(JOYButtonUsageSelect),
            BUTTON(10): @(JOYButtonUsageStart),
            BUTTON(11): @(JOYButtonUsageLStick),
            BUTTON(12): @(JOYButtonUsageRStick),
            BUTTON(13): @(JOYButtonUsageDPadUp),
            BUTTON(14): @(JOYButtonUsageDPadRight),
            BUTTON(15): @(JOYButtonUsageDPadDown),
            BUTTON(16): @(JOYButtonUsageDPadLeft),
        },
        
        JOYAxes2DUsageMapping: @{
            AXES2D(1): @(JOYAxes2DUsageLeftStick),
            AXES2D(6): @(JOYAxes2DUsageRightStick),
        },
        
        JOYSwapZRz: @YES,
    },
    
    @"Pro Controller": @{ // Switch Pro Controller
        JOYIsSwitch: @YES,
        JOYAxisGroups: @{
              @(kHIDUsage_GD_X): @(0),
              @(kHIDUsage_GD_Y): @(0),
              @(kHIDUsage_GD_Z): @(0),
              @(kHIDUsage_GD_Rx): @(1),
              @(kHIDUsage_GD_Ry): @(1),
              @(kHIDUsage_GD_Rz): @(1),
        },
            
        JOYButtonUsageMapping: @{
            BUTTON(1): @(JOYButtonUsageB),
            BUTTON(2): @(JOYButtonUsageA),
            BUTTON(3): @(JOYButtonUsageY),
            BUTTON(4): @(JOYButtonUsageX),
            BUTTON(5): @(JOYButtonUsageL1),
            BUTTON(6): @(JOYButtonUsageR1),
            BUTTON(7): @(JOYButtonUsageL2),
            BUTTON(8): @(JOYButtonUsageR2),
            BUTTON(9): @(JOYButtonUsageSelect),
            BUTTON(10): @(JOYButtonUsageStart),
            BUTTON(11): @(JOYButtonUsageLStick),
            BUTTON(12): @(JOYButtonUsageRStick),
            BUTTON(13): @(JOYButtonUsageHome),
            BUTTON(14): @(JOYButtonUsageMisc),
        },
        
        JOYAxes2DUsageMapping: @{
            AXES2D(1): @(JOYAxes2DUsageLeftStick),
            AXES2D(4): @(JOYAxes2DUsageRightStick),
        },
        
        JOYCustomReports: @{
            @(0x30): @[
            
            // For USB mode, which uses the wrong report descriptor
            
            @{@"reportID": @(1), @"size":@1, @"offset":@16, @"usagePage":@(kHIDPage_Button), @"usage":@3},
            @{@"reportID": @(1), @"size":@1, @"offset":@17, @"usagePage":@(kHIDPage_Button), @"usage":@4},
            @{@"reportID": @(1), @"size":@1, @"offset":@18, @"usagePage":@(kHIDPage_Button), @"usage":@1},
            @{@"reportID": @(1), @"size":@1, @"offset":@19, @"usagePage":@(kHIDPage_Button), @"usage":@2},
            
            // SR and SL not used on the Pro Controller
            @{@"reportID": @(1), @"size":@1, @"offset":@22, @"usagePage":@(kHIDPage_Button), @"usage":@6},
            @{@"reportID": @(1), @"size":@1, @"offset":@23, @"usagePage":@(kHIDPage_Button), @"usage":@8},
            
            @{@"reportID": @(1), @"size":@1, @"offset":@24, @"usagePage":@(kHIDPage_Button), @"usage":@9},
            @{@"reportID": @(1), @"size":@1, @"offset":@25, @"usagePage":@(kHIDPage_Button), @"usage":@10},
            @{@"reportID": @(1), @"size":@1, @"offset":@26, @"usagePage":@(kHIDPage_Button), @"usage":@12},
            @{@"reportID": @(1), @"size":@1, @"offset":@27, @"usagePage":@(kHIDPage_Button), @"usage":@11},
            
            @{@"reportID": @(1), @"size":@1, @"offset":@28, @"usagePage":@(kHIDPage_Button), @"usage":@13},
            @{@"reportID": @(1), @"size":@1, @"offset":@29, @"usagePage":@(kHIDPage_Button), @"usage":@14},

            @{@"reportID": @(1), @"size":@1, @"offset":@32, @"usagePage":@(kHIDPage_GenericDesktop), @"usage":@(kHIDUsage_GD_DPadDown)},
            @{@"reportID": @(1), @"size":@1, @"offset":@33, @"usagePage":@(kHIDPage_GenericDesktop), @"usage":@(kHIDUsage_GD_DPadUp)},
            @{@"reportID": @(1), @"size":@1, @"offset":@34, @"usagePage":@(kHIDPage_GenericDesktop), @"usage":@(kHIDUsage_GD_DPadRight)},
            @{@"reportID": @(1), @"size":@1, @"offset":@35, @"usagePage":@(kHIDPage_GenericDesktop), @"usage":@(kHIDUsage_GD_DPadLeft)},
            
            // SR and SL not used on the Pro Controller
            @{@"reportID": @(1), @"size":@1, @"offset":@38, @"usagePage":@(kHIDPage_Button), @"usage":@5},
            @{@"reportID": @(1), @"size":@1, @"offset":@39, @"usagePage":@(kHIDPage_Button), @"usage":@7},

            /* Sticks */
            @{@"reportID": @(1), @"size":@12, @"offset":@40, @"usagePage":@(kHIDPage_GenericDesktop), @"usage":@(kHIDUsage_GD_X), @"min": @0, @"max": @0xFFF},
            @{@"reportID": @(1), @"size":@12, @"offset":@52, @"usagePage":@(kHIDPage_GenericDesktop), @"usage":@(kHIDUsage_GD_Y), @"min": @0xFFF, @"max": @0},
            
            @{@"reportID": @(1), @"size":@12, @"offset":@64, @"usagePage":@(kHIDPage_GenericDesktop), @"usage":@(kHIDUsage_GD_Rx), @"min": @0, @"max": @0xFFF},
            @{@"reportID": @(1), @"size":@12, @"offset":@76, @"usagePage":@(kHIDPage_GenericDesktop), @"usage":@(kHIDUsage_GD_Ry), @"min": @0xFFF, @"max": @0},
            ],
        },
    },
    
    JOYIgnoredReports: @(0x30), // Ignore the real 0x30 report as it's broken
    
    @"PLAYSTATION(R)3 Controller": @{ // DualShock 3
        JOYAxisGroups: @{
            @(kHIDUsage_GD_X): @(0),
            @(kHIDUsage_GD_Y): @(0),
            @(kHIDUsage_GD_Z): @(1),
            @(kHIDUsage_GD_Rx): @(2),
            @(kHIDUsage_GD_Ry): @(3),
            @(kHIDUsage_GD_Rz): @(1),
        },
        
        JOYButtonUsageMapping: @{
            BUTTON(1): @(JOYButtonUsageSelect),
            BUTTON(2): @(JOYButtonUsageL3),
            BUTTON(3): @(JOYButtonUsageR3),
            BUTTON(4): @(JOYButtonUsageStart),
            BUTTON(5): @(JOYButtonUsageDPadUp),
            BUTTON(6): @(JOYButtonUsageDPadRight),
            BUTTON(7): @(JOYButtonUsageDPadDown),
            BUTTON(8): @(JOYButtonUsageDPadLeft),
            BUTTON(9): @(JOYButtonUsageL2),
            BUTTON(10): @(JOYButtonUsageR2),
            BUTTON(11): @(JOYButtonUsageL1),
            BUTTON(12): @(JOYButtonUsageR1),
            BUTTON(13): @(JOYButtonUsageX),
            BUTTON(14): @(JOYButtonUsageA),
            BUTTON(15): @(JOYButtonUsageB),
            BUTTON(16): @(JOYButtonUsageY),
            BUTTON(17): @(JOYButtonUsageHome),
        },
        
        JOYAxisUsageMapping: @{
            AXIS(4): @(JOYAxisUsageL1),
            AXIS(5): @(JOYAxisUsageR1),
            AXIS(8): @(JOYAxisUsageL2),
            AXIS(9): @(JOYAxisUsageR2),
        },
        
        JOYAxes2DUsageMapping: @{
            AXES2D(1): @(JOYAxes2DUsageLeftStick),
            AXES2D(3): @(JOYAxes2DUsageRightStick),
        },
                
        JOYCustomReports: @{
            @(0x01): @[
            /* Pressure sensitive inputs */
            @{@"reportID": @(1), @"size":@8, @"offset":@(13 * 8), @"usagePage":@(kHIDPage_GenericDesktop), @"usage":@(kHIDUsage_GD_Slider), @"min": @0, @"max": @255},
            @{@"reportID": @(1), @"size":@8, @"offset":@(14 * 8), @"usagePage":@(kHIDPage_GenericDesktop), @"usage":@(kHIDUsage_GD_Slider), @"min": @0, @"max": @255},
            @{@"reportID": @(1), @"size":@8, @"offset":@(15 * 8), @"usagePage":@(kHIDPage_GenericDesktop), @"usage":@(kHIDUsage_GD_Slider), @"min": @0, @"max": @255},
            @{@"reportID": @(1), @"size":@8, @"offset":@(16 * 8), @"usagePage":@(kHIDPage_GenericDesktop), @"usage":@(kHIDUsage_GD_Slider), @"min": @0, @"max": @255},
            @{@"reportID": @(1), @"size":@8, @"offset":@(17 * 8), @"usagePage":@(kHIDPage_GenericDesktop), @"usage":@(kHIDUsage_GD_Dial), @"min": @0, @"max": @255},
            @{@"reportID": @(1), @"size":@8, @"offset":@(18 * 8), @"usagePage":@(kHIDPage_GenericDesktop), @"usage":@(kHIDUsage_GD_Wheel), @"min": @0, @"max": @255},
            @{@"reportID": @(1), @"size":@8, @"offset":@(19 * 8), @"usagePage":@(kHIDPage_GenericDesktop), @"usage":@(kHIDUsage_GD_Rx), @"min": @0, @"max": @255},
            @{@"reportID": @(1), @"size":@8, @"offset":@(20 * 8), @"usagePage":@(kHIDPage_GenericDesktop), @"usage":@(kHIDUsage_GD_Ry), @"min": @0, @"max": @255},
            @{@"reportID": @(1), @"size":@8, @"offset":@(21 * 8), @"usagePage":@(kHIDPage_GenericDesktop), @"usage":@(kHIDUsage_GD_Slider), @"min": @0, @"max": @255},
            @{@"reportID": @(1), @"size":@8, @"offset":@(22 * 8), @"usagePage":@(kHIDPage_GenericDesktop), @"usage":@(kHIDUsage_GD_Slider), @"min": @0, @"max": @255},
            @{@"reportID": @(1), @"size":@8, @"offset":@(23 * 8), @"usagePage":@(kHIDPage_GenericDesktop), @"usage":@(kHIDUsage_GD_Slider), @"min": @0, @"max": @255},
            @{@"reportID": @(1), @"size":@8, @"offset":@(24 * 8), @"usagePage":@(kHIDPage_GenericDesktop), @"usage":@(kHIDUsage_GD_Slider), @"min": @0, @"max": @255},
            ]
        },
        
        JOYIsDualShock3: @YES,
    },

};

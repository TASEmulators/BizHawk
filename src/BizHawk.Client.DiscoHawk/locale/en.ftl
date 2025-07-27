### DiscoHawk strings



## CLI COMPARE readout dialog

discohawkcomparereadout-6110-tab-dest-textstr = DST Context
discohawkcomparereadout-6110-tab-log-textstr = Log
discohawkcomparereadout-6110-tab-src-textstr = SRC Context

discohawkcomparereadout-2005-windowtitlestatic = ComparisonResults



## main window

maindiscoform-1872-area-hawkdisc-label = Drag here to HAWK your disc - dump it out as a clean CCD/CHD

maindiscoform-6011-area-mp3extract-label = Drag a disc here to extract the audio tracks to MP3

maindiscoform-5766-btn-about-textstr = &About

maindiscoform-4723-btn-exit-textstr = E&xit

maindiscoform-5267-compare-hawk-label = BizHawk

maindiscoform-5267-compare-mednafen-label = Mednafen

maindiscoform-4639-group-compare-list-label = Compare Reading To:

maindiscoform-7187-group-engine-label = Disc Reading Engine

maindiscoform-5561-group-hawkoutput-label = Output Format

maindiscoform-4559-radio-engine-hawk-textstr = BizHawk
maindiscoform-4559-radio-engine-hawk-longdesc-label =
    - Uses FFMPEG for audio decoding
    - Loads ISO, CUE, CCD, CDI, CHD, MDS, and NRG

maindiscoform-7205-radio-engine-mednafen-textstr = Mednafen
maindiscoform-7205-radio-engine-mednafen-longdesc-label =
    - Doesn't support audio decoding yet
    (even though Mednafen proper can do it)
    - Loads ISO, CUE, and CCD

maindiscoform-7576-radio-hawkoutput-ccd-textstr = CCD

maindiscoform-2884-radio-hawkoutput-chd-textstr = CHD

maindiscoform-7426-pane-operations-label = - Operations -

maindiscoform-3997-windowtitlestatic = DiscoHawk



## "Hawk disc" dialogs

discodischawking-6945-errbox-hawk-windowtitlestatic = Error loading disc

discodischawking-3654-errbox-misc-windowtitlestatic = Error loading disc



## mp3 extract dialogs

discomp3extract-7691-errbox-misc-windowtitlestatic = Error loading disc

discomp3extract-5715-errbox-noffmpeg-label =
    This function requires FFmpeg, but it doesn't appear to have been downloaded.
    EmuHawk can automatically download it: you just need to set up A/V recording with the FFmpeg writer.
discomp3extract-5715-errbox-noffmpeg-windowtitlestatic = FFmpeg missing

discomp3extract-3418-prompt-overwrite-fmtstr =
    Do you want to overwrite existing files? Choosing "No" will simply skip those. You could also "Cancel" the extraction entirely.

    caused by file: {"{0}"}
discomp3extract-3418-prompt-overwrite-windowtitlestatic = File to extract already exists



## About window

discohawkabout-9804-btn-dismiss-textstr = OK

discohawkabout-4584-lbl-explainer-label =
    DiscoHawk converts bolloxed-up crusty disc images to totally tidy CCD.

    DiscoHawk is part of the BizHawk project ( https://github.com/TASEmulators/BizHawk ).

    BizHawk is a .net-based multi-system emulator brought to you by some of the rerecording emulator principals. We wrote our own cue parsing/generating code to be able to handle any kind of junk we threw at it. Instead of trapping it in the emulator, we liberated it in the form of this tool, to be useful in other environments.

    To use, drag a disc (.cue, .iso, .ccd, .cdi, .mds, .nrg) into the top area. DiscoHawk will dump a newly cleaned up CCD file set to the same directory as the original disc image, and call it _hawked.

    This is beta software. You are invited to report problems to our bug tracker or IRC. Problems consist of: crusty disc images that crash DiscoHawk or that cause DiscoHawk to produce a _hawked.ccd which fails to serve your particular purposes (which we will need to be informed of, in case we are outputting wrongly.)

discohawkabout-2822-windowtitlestatic = About DiscoHawk



### strings for the Fluent sample



## Closing tabs

tabs-close-button = Close
tabs-close-tooltip = {$tabCount ->
    [one] Close {$tabCount} tab
   *[other] Close {$tabCount} tabs
}
tabs-close-warning =
    You are about to close {$tabCount} tabs.
    Are you sure you want to continue?

## Syncing

-sync-brand-name = Firefox Account

sync-dialog-title = {-sync-brand-name}
sync-headline-title =
    {-sync-brand-name}: The best way to bring
    your data always with you
sync-signedout-title =
    Connect with your {-sync-brand-name}

## Datetime
date-is = The date is 'DATETIME($dt, weekday: "short", month: "short", year: "numeric", day: "numeric", hour: "numeric", minute: "numeric", second: "numeric")'.

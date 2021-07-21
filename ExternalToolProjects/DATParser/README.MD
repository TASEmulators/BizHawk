# BizHawk DAT Tools
## External DAT Parser

This is a quick external tool that will parse either NoIntro or TOSEC DAT files and output them to text files in a format compatible with the BizHawk text gamedb standard.

Useful for quickly getting new cores up and running with valid ROM hash information.

### Usage

Like other BizHawk external tools, open the .csproj file in Visual Studio (in this case "DATParser.csproj") and rebuild it. This will place a DLL in your BizHawk > output > ExternalTools folder enabling you to access it through EmuHawk > Tools > External Tool > DATTool

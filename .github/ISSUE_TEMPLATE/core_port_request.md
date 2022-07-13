---
name: Core port request
about: Request another emulator be ported into BizHawk
title: "[Core Port Req.] (name of emulator, and systems emulated if it's not obvious)"
labels: 'Core: Future core, Enhancement'
assignees: ''

---

[//]: # "This description supports Markdown syntax. There's a cheatsheet here: https://guides.github.com/features/mastering-markdown/"
[//]: # "These lines are comments, for letting you know what you should be writing. You can delete them or leave them in."
[//]: # "Also, please don't waste your time writing until you've checked for duplicate core requests, both on the issue tracker and on this Wiki page: https://github.com/TASEmulators/BizHawk/wiki/Core-Requests"

### Upstream info
- [Website](https://example.com)
- Target platforms: (win/mac/tux)
- [Source repo](https://github.com/group/repo)
- Language(s): (programming language)
- License: (license name/identifier)

### Merits
[//]: # "Briefly explain why this emulator is worth including in BizHawk. If it emulates the same system as an existing core, compare them."
(explanation)

### Technical details
[//]: # "Non-exhaustive list of things to consider:"
- (able to build .dll/.so for P/Invoke?)
- (frontend/backend separation--for example, can backend be built without SDL?)
- (can force single-threaded?)
- (has I/O abstraction accepting byte array, or only accepts file paths? for disc-based consoles, can swap out implementation for BizHawk's?)
- (savestate quality)

[//]: # "Code speaks louder than words: If you're able to make a proof-of-concept, pushing it to GitHub and putting a link here will speed up the process."

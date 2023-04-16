---
name: Bug report
about: Crashes, inaccurate emulation, nitpicks, or regressions
title: '(issue title -- summarise the summary)'
labels: ''
assignees: ''

---

[//]: # "This issue description supports Markdown syntax (this is what comments look like). There's a cheatsheet here: https://guides.github.com/features/mastering-markdown/"
[//]: # "You can leave these comments here or delete them. Also, please remember to search for similar issues before writing anything, including in closed issues!"
[//]: # "One more thing: if you're on Linux, please comment on #1430 instead of opening an issue so we don't annoy the other devs."

### Summary
[//]: # "Briefly describe what's broken. Include relevant details: loaded core, loaded rom's hash, open tools, running scripts... You can embed a screenshot if it's easier to show the bug, but if you need more than one please put them at the end."
Whenever my cat sits on the left side of my keyboard, games run too fast for me to react to.

### Repro
[//]: # "If you can't figure out the list of steps, delete this section and put 'heisenbug' in the summary somewhere. If a Lua script can cause the bug, you can embed that instead (as simple as possible please)."
1. first step
2. second step
3. et cetera

### Output
[//]: # "Paste the contents of the error dialog if there is one (try Ctrl+C, it usually works), or paste the output from the Lua Console, or delete this section."
```
System.InvalidOperationException: o noes
  at BizHawk.Client.EmuHawk.HawkBiz.Crash()
  at BizHawk.Client.EmuHawk.HawkBiz.RunWithoutCrashing()
```

### Host env.
[//]: # "List the computers you've found the bug with. If there's a version that doesn't have the bug, please put that in too. Here are some examples:"
- BizHawk 2.5.2; Win10 Pro 1903; AMD/AMD
- BizHawk 2.4.2; Win8.1; Intel/NVIDIA
- BizHawk dev build at 370996875; Win10 Home 1809; Intel/AMD
- BizHawk 2.5; Fedora 31; AMD/NVIDIA

[//]: # "(screenshots, if applicable)"

[//]: # "That's it! If you'd like to help more, you could try a dev build (see Testing in the readme) or an older release. Click submit now and you can edit it later."


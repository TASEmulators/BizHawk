this code was used to aid in the creation of r4203.  it's not really production quality, and two generate steps are involved with manual
editing inbetween each.

rough outline of functionality:

1. in Program.cs, uncomment "PHASE 1" and comment "PHASE 2".
   run the program and redirect stdout to "out.cpp"
2. edit "out.cpp" to produce "fixed.cpp".  as fixed.cpp is included
   in the svn commit, i won't provide any other details.
3. in Program.cs, uncomment "PHASE 2" and comment "PHASE 1".
    run the program and redirect stdout. to "uop.cpp"
4. fix up "uop.cpp" by hand, and integrate it into the bsnes source tree.


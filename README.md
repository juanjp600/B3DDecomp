# B3DDecomp

Blitz3D and BlitzPlus game disassembler and decompiler

This isn't 100% functional yet so the UX sucks, but here are some usage instructions if you're curious:
- Build Blitz3DDisasm and Blitz3DDecomp
- Navigate to the bin directory at the root of the repository
- Drag and drop a Blitz3D or BlitzPlus executable onto Blitz3DDecomp

### Note For SCP - Containment Breach modders

If you're seeing an exception and the decompiler fails to generate a `Main.bb` and `Functions` folder, you probably need to give the game's [`fmod.decls`](https://raw.githubusercontent.com/Regalis11/scpcb/refs/heads/master/fmod.decls) as input, like so:
```
./Blitz3DDecomp [GAME_EXE] fmod.decls
```

This is a bug in SCP - Containment Breach. The decls, for whatever reason, incorrectly remove a parameter from `FSOUND_Stream_Open`. This breaks the decompiler because it makes the assumption that the compiler had a correct understanding of the number of parameters in the functions it's using, but in this case it did not. I'm not entirely sure how this doesn't completely break the game because it does screw up the stack, but so far nobody's really noticed so idk lol

# 0.0.5
- Fixed the game crashing when trying to move an npc in the fbn editor
  - The fix for this works but I don't properly understand the cause. There's a chance there are problems caused by the fix (hopefully not). I'll need to look into it more later.
- Made the fbn editor save option work. The file is saved to `data\field\myfolder` where the game is installed. I know this is inconsistent with envs, that's a later problem :)
- Fixed a crash when entering some fields such as the shrine at night
  - This was due to the game sometimes loading envs as `field/env/f%03d_%03d.ENV` instead of just `f%03d_%03d.ENV` which I wasn't taking into account.

# 0.0.4
Added very WiP support for the FBN Editor menu. This is mainly useful for freecam and removing NPCs currently. Trying to move existing or new ones causes a crash :(

The keybinds for the ENV and FBN menus can be changed in the mod's configuration. They default to F4 and F5 respectively.

# 0.0.3
- Fixed the text in the debug menu being really big.

Previously I did not know how to change text size so I expanded the size of all options to fit big text, now I've put them back to what they originally were which is much smaller. I believe a different font would've been used in the real debug menu but this is much closer than before.

# 0.0.2
- Added update information :naosmiley:

# 0.0.1
- The very WiP first version of the mod, just includes Environment Editor activated by pressing F4. 

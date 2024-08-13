# Clone Dash

## Notes about "Engine Restructure"
The core of the game was redone from the ground up. Now uses a lot more of a coherent system. There's still work to do, but this will be merged into main soon.
If you're looking to contribute, please contribute to this branch,

This is a WIP open-source clone of 'Muse Dash', a parkour-rhythm game combination. It can currently load the base game levels and provides the core gameplay functionality.

It currently implements:
 - All basic implementations of the games enemies, single hits, double hits ("geminis"), sustain notes, mashers, boss, etc...
 - Console-based searching for songs (you'll need to know the song name as it is stored in the files, and this is a temporary solution)
 - Pausing/resuming the song using the ESC key. Note that it does not tell you visually when it's paused right now, and unpausing gives you 3 seconds before it resumes game playback. Haven't figured out a good UI solution yet

![CloneDash_iK1pd9AlJL](https://github.com/user-attachments/assets/02ec7e9e-8e9a-4a20-93a9-0d4c851ceac7)
![CloneDash_BF52fggWJg](https://github.com/user-attachments/assets/2fed8c0f-9e92-42ea-9747-ab82cc72d2f9)

This is a hastily-put-together alpha of game functionality. It kind of matches the base gameplay, but there's still work that needs to be done. The current core of the project is a bit of a mess and a lot of this will change as I further work on the project. You will need to own the game to play this currently, as it relies on finding a valid Steam installation of the game, and does not have any custom level support (yet...)

There are still a lot of things I'd like to implement that are currently missing, for starters, custom levels. It would be nice to provide a lot of customizability and accessibility options the base game does not allow for as well.

## Building
You should be able to just clone the repository, open it in Visual Studio 2022, and build the game. If there's any issues, let me know and I'll try to resolve them. I don't have other machines to get Mac/Linux support working at the moment, and there is a  bit of a reliance on some Windows functionality, but it is a goal to be compatible with other operating systems.

## Credits/Attribution

OdinSerializer is licensed under the Apache 2.0 license: https://github.com/TeamSirenix/odin-serializer.

I used this custom version of OdinSerializer built to be independent of Unity, which you can find here: https://github.com/wqaetly/OdinSerializerForNetCore

Raylib and Raylib-cs are licensed under the ZLib license: 

https://github.com/raysan5/raylib

https://github.com/ChrisDill/Raylib-cs

punch.wav: Cartoon_Punch_05.wav by RSilveira_88 -- https://freesound.org/s/216199/ -- License: Attribution 4.0

AssetStudio is licensed under the MIT license: https://github.com/Perfare/AssetStudio

All image assets were made by me and are placeholder assets.

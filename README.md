# HARD FOREST

A 2D pixel-art platformer for mobile (landscape), built in Unity 6 (6000.6.7f1).
You play as a samurai fighting through a haunted forest and a dark castle, collecting crystals and defeating a stone golem.

## Game Overview

- **Genre:** 2D action platformer
- **Platform:** Mobile (Android / iOS), landscape orientation
- **Engine:** Unity 6 (6000.6.7f1)
- **Reference resolution:** 1920 x 1080
- **Author:** [your name], solo project

## Core Loop

Run and jump across tile-based levels, fight enemies with the katana, collect crystals (0 / 4 per level), defeat the golem mini-boss, and reach the portal to the next location.

## Controls

| Action | Mobile (on-screen) | PC (testing) |
|---|---|---|
| Move left / right | Left and right arrow buttons (bottom-left) | A / D or arrow keys |
| Jump | Up arrow button (bottom-right) | Space |
| Attack | Sword button (bottom-right) | F / J |
| Pause | Pause button (top-center) | Pause button |

## UI Screens

- **Main Menu:** title, Start, Settings, Quit
- **HUD:** hero portrait and health bar, crystal counter, boss health bar, pause button, touch controls
- **Pause Menu:** Resume, Restart, Home

UI is built with Canvas Scaler (Scale With Screen Size, 1920 x 1080, Match = 1), anchored to screen corners, and wrapped in a SafeArea panel for notches and cutouts. Tested in the Unity Device Simulator on a notch phone, a 16:9 phone and a 4:3 tablet.

## Current Features

- Player movement, jump, melee attack, hurt and idle animations
- Stone golem mini-boss with projectile attack and boss health bar
- Crystal collection counter
- Touch controls, pause menu, main menu
- Tile-based levels

## Planned (weeks 9-13)

- [more levels and a second location (dark castle)]
- [additional enemy types]
- [second attack unlocked after defeating the boss]
- [sound and music polish]

## Credits and Licenses

All third-party assets are free and used under their licenses:

| Asset | Author | License | Link |
|---|---|---|---|
| [hero sprites] | [author] | [license] | [link] |
| [golem sprites] | [author] | [license] | [link] |
| [forest tiles and background] | [author] | [license] | [link] |
| [castle tiles] | [author] | [license] | [link] |
| [UI: touch buttons] | [author] | [license] | [link] |
| [UI: pause menu and portrait frames] | [author] | [license] | [link] |
| [audio] | [author] | [license] | [link] |

## AI Usage

AI chat was used for learning and guidance only. See `AI_LOG.txt`.

## How to Run

1. Open the project in Unity 6 (6000.6.7f1).
2. Open `Assets/Main Menu/MainMenu.unity`.
3. Press Play. For mobile layout testing use the Device Simulator tab in the Game view.

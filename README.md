# 🐾 TamaPoke (Desktop Version)

🌐 **[한국어로 읽기 (Korean) 🇰🇷](./README.ko.md)**

> A retro-style Pokemon Tamagotchi desktop widget game reimagined using C# WPF and the MVVM pattern. Raise your own Pokemon on your desktop, evolve them, manage your party, and enjoy battle and capture systems[cite: 6].

<!-- 🌟 TamaPoke Screenshot Gallery (10 Images) -->
| Main Play Screen | Various Idle Animations |
|:---:|:---:|
| <img src="./ScreenShot/Idle.jpg" width="400"/> | <img src="./ScreenShot/Idle2.jpg" width="400"/> |
| **Feeding & Interaction** | **Desktop Walking Mode** |
| <img src="./ScreenShot/eat.jpg" width="400"/> | <img src="./ScreenShot/walking.jpg" width="400"/> |
| **Wild Pokemon Battle** | **Gym Challenge** |
| <img src="./ScreenShot/battle.jpg" width="400"/> | <img src="./ScreenShot/gym.jpg" width="400"/> |
| **Pokedex Collection** | **Party Management** |
| <img src="./ScreenShot/dex.jpg" width="400"/> | <img src="./ScreenShot/party.jpg" width="400"/> |
| **Detailed Settings** | **Integrated Update System** |
| <img src="./ScreenShot/setting.jpg" width="400"/> | <img src="./ScreenShot/update.jpg" width="400"/> |

---

## 🎮 User Manual

TamaPoke operates as a floating, semi-transparent widget on your desktop[cite: 6]. Left-click your Pokemon to freely move it anywhere on the screen[cite: 6].

### 🔘 Bottom Action Buttons
Manage your Pokemon's core conditions using the four buttons at the bottom of the main screen[cite: 6].
* 🍎 **Feed**: Give delicious berries to increase 'Fullness'[cite: 6].
* 🎮 **Play**: Play with your Pokemon to increase 'Joy', or engage in mini-games and wild battles[cite: 6].
* 🌙 **Sleep**: Put them to sleep to recover 'Energy'[cite: 6].
* 🫧 **Clean**: Bathe them to maintain 'Hygiene'[cite: 6].

### 🖱️ Context Menu (Right-Click)
Right-click your Pokemon to access various systems[cite: 6]:
* 📖 **Open Pokedex**: Check the collection records of Pokemon you've met or caught[cite: 6].
* 📋 **Open Profile**: View your Pokemon's level, combat stats, gym badges, and medals[cite: 6].
* 📁 **View Party**: Manage up to 6 party members and swap the Pokemon on your main screen[cite: 6].
* 🎒 **Open Bag**: Check your item inventory, including Poke Balls and Potions[cite: 6].
* 🍃 **Release to Wild**: Say goodbye and release your Pokemon into nature[cite: 6].

### 💻 System Tray Menu
Right-click the system tray icon in the Windows taskbar to quickly access core features and settings[cite: 6].
* **Bring to Front**: Bring the hidden TamaPoke widget back to the top of the screen[cite: 6].
* **Walking Mode**: Toggle the mode where your Pokemon roams freely around the desktop[cite: 6].
* **Settings**: Finely adjust the game environment to your preference[cite: 6].
  * **Use Tray Notifications**: Receive Windows notifications for status changes[cite: 6].
  * **Farewell Age (Days)**: Adjust the base lifespan (in days) before parting with your Pokemon[cite: 6].
  * **Roaming Pokemon Count**: Set the number of party Pokemon that appear during Walking Mode[cite: 6].
  * **Pokemon Size (Scale)**: Adjust the dot pixel size of the Pokemon in Walking Mode[cite: 6].
  * **Taskbar Restriction**: Restrict movement so Pokemon don't hide behind the taskbar[cite: 6].
  * **Hatch Generation Selection**: Select generations (Gen 1 to Gen 9) to encounter Pokemon from specific regions[cite: 6].
* **Check for Updates**: Check for the latest TamaPoke release and update the client[cite: 6].
* **Fully Exit**: Safely save all progress (Pokedex, party, status) and completely close the program[cite: 6].

---

## 🌟 Key Features

* **Desktop Floating Widget**: Real-time status management with a transparent overlay and hover opacity control[cite: 6].
* **Life Cycle & Raising System**: 
  * Manage 4 core stats (Fullness, Joy, Energy, Hygiene) by feeding, playing, sleeping, and cleaning[cite: 6].
  * Features time-based growth and a multi-branch evolution system (including condition requirements and evolution postponement)[cite: 6].
  * Safe runaway, release, and emotional farewell mechanisms trigger when the lifespan is reached[cite: 6].
* **Party & Battle/Capture System**: 
  * Keep up to 6 Pokemon in your party and freely swap with your main Pokemon[cite: 6].
  * Intuitive in-game overlay pop-ups for swapping or releasing members when catching a new Pokemon with a full party[cite: 6].
  * Built-in safe synchronization logic to prevent data corruption (cloning bugs) during releases[cite: 6].
* **Mini-Game System**: Manage stats through interactive mini-games like ball bouncing, berry catching, rhythm memory, and cleaning[cite: 6].
* **JSON-Based Data Persistence**: Progress, Pokedex, and party info are safely saved and loaded via JSON save files[cite: 6].

---

## 🛠️ Tech Stack

* **Language**: C#, C++[cite: 6]
* **Framework**: WPF (Windows Presentation Foundation), .NET[cite: 6]
* **Architecture**: MVVM Pattern[cite: 6]
* **Data Storage**: JSON[cite: 6]

---

## 🔗 References (Open Source Projects)

This desktop project was reimagined for the desktop environment by referencing the game loops, planning, and ideas from the following embedded/web-based original `TamaPoke` projects[cite: 6]:

* [socquique/TamaPoke](https://github.com/socquique/TamaPoke) — Original firmware and basic Tamagotchi mechanics[cite: 6].
* [ShadowEnemyx/TamaPoke (Expanded Update)](https://github.com/ShadowEnemyx/TamaPoke/tree/tamapoke-expanded-update) — Expanded features and content references[cite: 6].
* [DylanPDao/TamaPoke](https://github.com/DylanPDao/TamaPoke) — Battle and additional system expansion ideas[cite: 6].

---

## 📝 Credits & Acknowledgements

* **Sprite Assets**: All Pokemon dot sprites used in this game utilize the original resources from the Pokemon Mystery Dungeon community's open-source project, [PMDCollab/SpriteCollab](https://github.com/PMDCollab/SpriteCollab)[cite: 6]. Deep thanks to the community contributors for providing high-quality sprites[cite: 6].
  * These resources are used strictly for non-commercial purposes under the **Creative Commons Attribution-NonCommercial 4.0 International (CC BY-NC 4.0)** license[cite: 6].
  * Read the full license: [https://creativecommons.org/licenses/by-nc/4.0/](https://creativecommons.org/licenses/by-nc/4.0/)[cite: 6].
* **Badge Images**: Gym badge images reference and cite materials from [Bulbapedia](https://bulbapedia.bulbagarden.net/wiki/Main_Page)[cite: 6].
* **Disclaimer**: This is a non-commercial fan project[cite: 6]. All copyrights and trademarks related to Pokémon belong to Nintendo, Creatures Inc., and GAME FREAK inc[cite: 6].
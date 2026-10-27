<div align="center">

# 🐾 TamaPoke PC

**A retro Pokémon Tamagotchi that lives on your Windows desktop**

Hatch an egg, raise your Pokémon, take it for walks, battle gyms, and Mega Evolve — all in a tiny Poké Ball widget.

![Platform](https://img.shields.io/badge/platform-Windows-0078D6?style=flat-square)
![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?style=flat-square)
![WPF](https://img.shields.io/badge/UI-WPF-5C2D91?style=flat-square)
[![License](https://img.shields.io/badge/license-MIT-green?style=flat-square)](./LICENSE)

🌐 **English** · [한국어](./README.ko.md) · 📘 [Manual](./MANUAL.md)

<img src="./ScreenShot/Idle.jpg" width="300"/>&nbsp;&nbsp;<img src="./ScreenShot/battle.jpg" width="300"/>

</div>

---

## ✨ Highlights

| | | |
|:---:|:---:|:---:|
| 🥚 **Raise & Evolve**<br/><sub>Hatch, feed, play, sleep and bathe.<br/>Level up over real time and evolve.</sub> | 🚶 **Desktop Walks**<br/><sub>Your party roams the taskbar<br/>and digs items out of the grass.</sub> | ⚔️ **Battles & Gyms**<br/><sub>Wild battles, status effects, catching,<br/>and 64 gym leaders across 9 regions.</sub> |
| 💎 **Mega Evolution**<br/><sub>Earn the Mega Ring, collect stone shards,<br/>and Mega Evolve once per battle.</sub> | 📖 **Pokédex**<br/><sub>All 1,025 Pokémon, sorted by region.<br/>Fill it by hatching and catching.</sub> | 🌐 **Korean / English**<br/><sub>Switch languages instantly,<br/>including Pokémon and move names.</sub> |

---

## 📸 Screenshots

| Main screen | Sleeping at night | Feeding |
|:---:|:---:|:---:|
| <img src="./ScreenShot/Idle.jpg" width="260"/> | <img src="./ScreenShot/Idle2.jpg" width="260"/> | <img src="./ScreenShot/eat.jpg" width="260"/> |
| **Wild battle** | **Mega Evolution** | **Gym badges** |
| <img src="./ScreenShot/battle.jpg" width="260"/> | <img src="./ScreenShot/mega.jpg" width="260"/> | <img src="./ScreenShot/gym.jpg" width="260"/> |
| **Pokédex** | **Party** | **Bag & Mega Stones** |
| <img src="./ScreenShot/dex.jpg" width="260"/> | <img src="./ScreenShot/party.jpg" width="260"/> | <img src="./ScreenShot/bag.jpg" width="260"/> |

<div align="center">

**Walk mode — your party wanders across the taskbar**

<img src="./ScreenShot/walking.jpg" width="600"/>

| Settings | Updater |
|:---:|:---:|
| <img src="./ScreenShot/setting.jpg" width="260"/> | <img src="./ScreenShot/update.jpg" width="260"/> |

</div>

---

## 🚀 Getting Started

1. Install the [.NET 9 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/9.0) (Windows 10 / 11).
2. Download the latest `TamaPoke_vX.X.X.X.zip` from [Releases](https://github.com/gomdolit-jeong/TamaPoke_PC/releases/latest) and extract it anywhere.
3. Run `TamaPoke.exe`.
4. On the first run you'll be asked to download the Pokémon images — click **Yes** and the updater fetches them for you (takes a few minutes).

> Your progress is saved automatically to `save.json` next to the program, with a backup in `save.bak.json`.

---

## 🎮 How to Play

> 📘 For details on every feature, see the **[Manual](./MANUAL.md)**.

Drag the Poké Ball with the left mouse button to place it anywhere on your desktop.

### Care buttons
| Button | Action |
|:---:|---|
| 🍎 | **Feed** — choose a berry to fill *Fullness* |
| 🎮 | **Play** — pick a mini-game to raise *Joy* |
| 🌙 | **Sleep** — rest to recover *Energy* |
| 🫧 | **Bath** — keep *Hygiene* up |

### Right-click menu (on your Pokémon)
📖 Pokédex · 📋 Profile (stats, gyms, medals, wild battles) · 📂 Party · 🎒 Bag · 🍃 Release to the wild

### Tray menu & shortcuts
| Menu | Shortcut | Description |
|---|:---:|---|
| Show TamaPoke | | Bring the widget back to the front |
| Walk mode | `Ctrl` + `W` | Let your party roam the desktop |
| Settings | `Ctrl` + `O` | Language, notifications, farewell age, walk options, hatch generations |
| Update TamaPoke | `Ctrl` + `U` | Download data / images and check for a new version |
| Quit | | Save everything and exit |

<sub>Shortcuts work while the main screen is focused.</sub>

---

## 🌟 Features in Detail

<details>
<summary><b>🥚 Life cycle & evolution</b></summary>

- Manage four stats — Fullness, Joy, Energy and Hygiene. Neglect causes care mistakes, and a Pokémon that is left alone too long may run away.
- Your Pokémon gains one level per hour of running time and evolves at its evolution level (evolution can be postponed).
- Shiny Pokémon, gender differences and regional forms can hatch.
- When the farewell age is reached, you say goodbye and a new egg begins.
</details>

<details>
<summary><b>🚶 Walk mode</b></summary>

- Party Pokémon walk around the desktop (or on top of the taskbar) at an adjustable size.
- Time keeps flowing: stats never drop below a safe floor, so nobody runs away while you're busy. The whole party grows, at ⅓ of the usual speed.
- Pokémon sometimes dive into the grass and find items such as Potions, Poké Balls — and Mega Stone shards.
- Pokémon that can Mega Evolve sometimes do so for a few minutes while walking. Count and size settings apply instantly.
</details>

<details>
<summary><b>⚔️ Battles, catching & gyms</b></summary>

- Turn-based battles with type matchups, STAB, dodging and status conditions (burn, poison, bad poison, paralysis, sleep, freeze).
- Speed decides who moves first (the slower side still gets a chance), and stat moves like Growl or Swords Dance change stats up to ±6 stages.
- Weaken wild Pokémon and throw Poké Balls to catch them — if your party is full, choose who to send away.
- Challenge 64 gym leaders from Kanto to Paldea in order, and rematch the ones you've beaten.
- Learn new moves after winning.
</details>

<details>
<summary><b>💎 Mega Evolution</b></summary>

- Earn the **Mega Ring** by collecting 8 gym badges.
- Collect **10 shards** to complete a Mega Stone. Shards come from wild battles, gym wins and walk mode, mostly for your lead and party Pokémon.
- Press **💎 Mega Evolve** in battle — once per battle, without using your turn. Stats and types change to the Mega form until the battle ends.
- Every Mega form with a sprite is supported, including the new Mega Evolutions from *Legends: Z-A*, and Primal Reversion.
</details>

<details>
<summary><b>🛡️ Safe saves & data</b></summary>

- Saves are written safely and backed up, and a damaged save is restored automatically from the backup.
- On startup, missing or damaged data files and sprites are detected, and the updater opens with only the needed items checked.
</details>

---

## 🛠️ Tech Stack

- **Language**: C#
- **Framework**: WPF on .NET 9
- **Architecture**: MVVM
- **Data**: JSON (game data from [PokéAPI](https://pokeapi.co/), save files, translations)

---

## 🔗 References (Open Source Projects)

This desktop project was reimagined for the desktop environment by referencing the game loops, planning, and ideas from the following embedded/web-based original `TamaPoke` projects:

* [socquique/TamaPoke](https://github.com/socquique/TamaPoke) — Original firmware and basic Tamagotchi mechanics.
* [ShadowEnemyx/TamaPoke (Expanded Update)](https://github.com/ShadowEnemyx/TamaPoke/tree/tamapoke-expanded-update) — Expanded features and content references.
* [DylanPDao/TamaPoke](https://github.com/DylanPDao/TamaPoke) — Battle and additional system expansion ideas.

---

## 📝 Credits & Acknowledgements

* **Sprite Assets**: All Pokemon dot sprites used in this game utilize the original resources from the Pokemon Mystery Dungeon community's open-source project, [PMDCollab/SpriteCollab](https://github.com/PMDCollab/SpriteCollab). Deep thanks to the community contributors for providing high-quality sprites.
  * These resources are used strictly for non-commercial purposes under the **Creative Commons Attribution-NonCommercial 4.0 International (CC BY-NC 4.0)** license.
  * Read the full license: [https://creativecommons.org/licenses/by-nc/4.0/](https://creativecommons.org/licenses/by-nc/4.0/).
* **Badge Images**: Gym badge images reference and cite materials from [Bulbapedia](https://bulbapedia.bulbagarden.net/wiki/Main_Page).
* **Game Data**: Pokémon names, base stats, evolutions, moves and Mega Evolution data were generated from [PokéAPI](https://pokeapi.co/) ([PokeAPI/api-data](https://github.com/PokeAPI/api-data), BSD-3-Clause).
* **Libraries**: [Emoji.Wpf](https://github.com/samhocevar/emoji.wpf) (WTFPL).
* **Disclaimer**: This is a non-commercial fan project. All copyrights and trademarks related to Pokémon belong to Nintendo, Creatures Inc., and GAME FREAK inc.

---

## ⚖️ License

- **Source code**: [MIT](./LICENSE). The MIT license covers the source code only.
- **Images and data**: Sprites, gym badges and Pokémon names/data belong to their respective owners (see Credits above) and are **not** covered by the MIT license. They may be used for **non-commercial purposes only**.
- Pokémon © Nintendo / Creatures Inc. / GAME FREAK inc. This project is unofficial and not affiliated with or endorsed by them.

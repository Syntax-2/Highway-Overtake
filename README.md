<div align="center">
  <h1 align="center">Highway Overtake 🚗💨</h1>
  <p align="center">
    <b>An adrenaline-fueled endless driver for Android, built with Unity.</b>
  </p>
  <img src="GamePlayGif.gif" alt="Highway Overtake Gameplay GIF" width="80%">
  <br>
  <em>Short gameplay</em>
</div>

---

## 🚦 Overview

**Highway Overtake** is a fast-paced endless driving game where your goal is to survive as long as possible on a bustling highway. Weave through traffic, collect coins, unlock new cars and maps, and upgrade your ride for even more excitement!

---

## 🚀 Key Features

- **Endless Driving Action:**  
  Navigate through *procedurally generated* traffic and obstacles on a never-ending road.

- **Dynamic Day/Night Cycle:**  
  Experience immersive lighting transitions from day to sunset to night, with changing ambient light and fog.

- **Car & Map Shops:**  
  - Earn in-game currency to purchase new cars and unlock visually distinct maps.
  - All unlockables managed via robust **ScriptableObjects**.
  - Purchases and selections are saved and persist between sessions using **PlayerPrefs**.

- **Vehicle Upgrade System:**  
  Use your earnings to improve your car's *Engine*, *Turbo*, and *Brakes*.

- **Juicy Gameplay Feel:**
  - **Camera Effects:** FOV widens with speed; Cinemachine Impulse-driven shake on collisions.
  - **Post-Processing:** Bloom, Chromatic Aberration, and Lens Distortion effects during speed boosts.
  - **Atmospheric Effects:** Dynamic ambient particles (snow, dust) and speed lines.

---

## 🛠️ Technical Highlights

| Feature                        | Implementation & Purpose                                                                                      |
|---------------------------------|--------------------------------------------------------------------------------------------------------------|
| **Singleton Manager Architecture** | Persistent managers (`GameDataManager`, `PlayerCarManager`) handle global game state, ensuring data persists across scene loads. |
| **ScriptableObject-Driven Design** | Decouples game data (cars, maps, upgrades) from logic for easy scalability and content management.           |
| **Event-Driven Systems**            | Uses C# events (`Action<T>`) for clean communication between managers and UI, avoiding tight dependencies.  |
| **Procedural Content**              | Spawner scripts dynamically generate the endless level for infinite replayability.                          |
| **Dynamic UI Management**           | Persistent `CameraAndMenuManager` handles complex UI/camera transitions and remembers views between scenes. |
| **URP & Post-Processing**           | Universal Render Pipeline for optimized graphics and dynamic Volume system for effects.                     |

---

## 💰 Monetization Strategy

- **Rewarded Video Ads:**  
  Players can choose to watch ads to double their earnings after a run.

- **In-App Purchases (IAP):**  
  System for buying in-game currency. <i>(Planned/Implemented)</i>

---

## 🎮 How to Play

1. **Steer** using the on-screen joystick.
2. **Control speed:** Press and hold Gas or Brake pedals.
3. **Overtake** AI cars and avoid obstacles to increase your score.
4. **Collect coins** to spend in the Shop and Garage.
5. **Grab power-ups** for a temporary advantage!

---

<div align="center">
  <sub>
    This project was created as a comprehensive solo development exercise, demonstrating skills in C# programming, Unity engine development, UI/UX design, and game architecture.
  </sub>
</div>

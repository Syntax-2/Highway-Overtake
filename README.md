Highway Overtake
Highway Overtake is an adrenaline-fueled endless driver built in Unity, where your goal is to survive as long as possible on a busy highway. Weave through traffic, collect coins, purchase new cars and maps, and upgrade your ride to dominate the road. This project showcases a complete game loop, from dynamic UI and persistent data management to juicy, satisfying gameplay effects.

► View on Google Play Store (Replace with your link once published)

🚀 Key Features
Endless Driving Action: Navigate through procedurally generated traffic and obstacles on a never-ending road.

Dynamic Day/Night Cycle: Experience maps like never before with a fully dynamic lighting system that transitions from day to sunset to night, complete with changing ambient light and fog.

Car & Map Shops:

Earn in-game currency to purchase new cars and unlock new, visually distinct maps.

Data for all unlockables is managed via robust ScriptableObjects.

Player purchases and selections are saved and persist between sessions using PlayerPrefs.

Vehicle Upgrade System: Use your earnings to improve your currently equipped car!

Engine: Increase top speed and torque.

Turbo: Boost your acceleration rate.

Brakes: Enhance your stopping power.

Juicy Gameplay Feel: The driving experience is enhanced with multiple layers of feedback:

Camera Effects: Camera FOV widens with speed, and a Cinemachine Impulse-driven shake provides visceral feedback on collisions.

Post-Processing: Bloom, Chromatic Aberration, and Lens Distortion effects activate during speed boosts for a thrilling sense of velocity.

Particle Effects: Dynamic ambient particles (snow, dust) and speed lines add atmosphere and depth to the world.

UI Feedback: The score text "pops" on collection, and UI buttons have satisfying animations and sounds.

💰 Monetization Strategy
This game is designed to be free-to-play, with monetization implemented through:

Rewarded Video Ads: Players can choose to watch ads to double their earnings after a run or to gain other in-game advantages.

In-App Purchases (IAP): A system for players to buy in-game currency or exclusive content directly. (Mention this if you plan to implement it)

Banner/Interstitial Ads: Non-intrusive ad placements to generate revenue. (Mention this if you plan to implement it)

🛠️ Technical Highlights
This project was built with a focus on creating a scalable and maintainable codebase, demonstrating several key development patterns:

Singleton Manager Architecture: Persistent Singleton managers (GameDataManager, PlayerCarManager, CameraAndMenuManager) handle global game state, ensuring data like currency, car/map selections, and UI state persist correctly across scene loads.

ScriptableObject-Driven Design: Car, map, and upgrade data are stored in ScriptableObjects, decoupling the data from the game logic. This makes it incredibly easy to add new cars or maps without changing code.

Event-Driven Systems:

The PlayerCarManager uses C# events (Action<GameObject>) to notify dependent systems (like spawners and cameras) when the active player car changes, eliminating hard-coded dependencies.

UI buttons use the modern Event System (IPointerDownHandler, IPointerUpHandler) for better performance and flexibility over legacy OnMouse_ methods.

Dynamic UI Management: The CameraAndMenuManager handles complex UI panel and camera state transitions, including remembering the active view between scene reloads.

Procedural Content: Spawner scripts (RoadSpawner, AICarSpawner, CoinSpawner, ObstacleSpawner) dynamically generate the endless level around the player, ensuring a unique run every time.

URP & Post-Processing: The game leverages Unity's Universal Render Pipeline (URP) for optimized graphics and uses a dynamic Volume system for atmospheric post-processing effects.

🎮 How to Play
Use the on-screen joystick to steer your car left and right.

Press and hold the Gas and Brake pedals to control your speed.

Overtake AI cars and avoid obstacles to increase your score.

Collect coins to spend in the Shop and Garage.

Collect power-ups for a temporary advantage!

This project was created as a comprehensive solo development exercise, demonstrating skills in C# programming, Unity engine development, UI/UX design, and game architecture.
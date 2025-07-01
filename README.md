<div align="center">
<h1 align="center">Highway Overtake 🚗💨</h1>
<p align="center">
An adrenaline-fueled endless driver for Android, built with Unity.
</p>
</div>

<p align="center">
<img src="GamePlayGif.gif" alt="Highway Overtake Gameplay GIF" width="80%">
</p>
<p align="center">
<em>To add your own GIF: Create a short gameplay video, convert it to a GIF named <code>gameplay.gif</code>, and upload it to the root of this repository.</em>
</p>

<div align="center">
<a href="https://www.google.com/search?q=https://play.google.com/store/apps/details%3Fid%3Dcom.yourcompany.highwayovertake">
<img src="https://www.google.com/search?q=https://img.shields.io/badge/Google_Play-Download-414141%3Fstyle%3Dfor-the-badge%26logo%3Dgoogle-play" alt="Download on Google Play"/>
</a>
</div>

Highway Overtake is a complete endless driver where the goal is to survive as long as possible on a busy highway. Weave through traffic, collect coins, purchase new cars and maps, and upgrade your ride to dominate the road. This project was built from the ground up to demonstrate a full game loop, from dynamic UI and persistent data management to juicy, satisfying gameplay effects.

🚀 Key Features
Endless Driving Action: Navigate through procedurally generated traffic and obstacles on a never-ending road.

Dynamic Day/Night Cycle: Experience maps with a fully dynamic lighting system that transitions from day to sunset to night, complete with changing ambient light and fog.

Car & Map Shops:

Earn in-game currency to purchase new cars and unlock visually distinct maps.

Data for all unlockables is managed via robust ScriptableObjects.

Player purchases and selections are saved and persist between sessions using PlayerPrefs.

Vehicle Upgrade System: Use your earnings to improve your currently equipped car's Engine, Turbo, and Brakes.

Juicy Gameplay Feel: The driving experience is enhanced with multiple layers of feedback:

Camera Effects: Camera FOV widens with speed, and a Cinemachine Impulse-driven shake provides visceral feedback on collisions.

Post-Processing: Bloom, Chromatic Aberration, and Lens Distortion effects activate during speed boosts for a thrilling sense of velocity.

Atmospheric Effects: Dynamic ambient particles (snow, dust) and speed lines add depth to the world.

🛠️ Technical Highlights
This project demonstrates a scalable and maintainable codebase, showcasing several key development patterns:

<table>
<thead>
<tr>
<th align="left">Feature</th>
<th align="left">Implementation & Purpose</th>
</tr>
</thead>
<tbody>
<tr>
<td><strong>Singleton Manager Architecture</strong></td>
<td>Persistent managers (<code>GameDataManager</code>, <code>PlayerCarManager</code>) handle global game state, ensuring data persists across scene loads.</td>
</tr>
<tr>
<td><strong>ScriptableObject-Driven Design</strong></td>
<td>Decouples game data (cars, maps, upgrades) from logic for easy scalability and content management without changing code.</td>
</tr>
<tr>
<td><strong>Event-Driven Systems</strong></td>
<td>Uses C# events (<code>Action&lt;T&gt;</code>) for clean communication between managers and UI, avoiding hard-coded dependencies.</td>
</tr>
<tr>
<td><strong>Procedural Content</strong></td>
<td>Spawner scripts dynamically generate the endless level around the player for infinite replayability.</td>
</tr>
<tr>
<td><strong>Dynamic UI Management</strong></td>
<td>A persistent <code>CameraAndMenuManager</code> handles complex UI panel and camera state transitions, remembering the view between scenes.</td>
</tr>
<tr>
<td><strong>URP & Post-Processing</strong></td>
<td>Leverages the Universal Render Pipeline for optimized graphics and a dynamic Volume system for atmospheric effects.</td>
</tr>
</tbody>
</table>

💰 Monetization Strategy
This game is designed to be free-to-play, with monetization implemented through:

<ul>
<li><b>Rewarded Video Ads:</b> Players can choose to watch ads to double their earnings after a run.</li>
<li><b>In-App Purchases (IAP):</b> A system for players to buy in-game currency. <i>(Planned/Implemented)</i></li>
</ul>

🎮 How to Play
<ol>
<li>Use the on-screen joystick to steer your car.</li>
<li>Press and hold the Gas and Brake pedals to control your speed.</li>
<li>Overtake AI cars and avoid obstacles to increase your score.</li>
<li>Collect coins to spend in the Shop and Garage.</li>
<li>Grab power-ups for a temporary advantage!</li>
</ol>

This project was created as a comprehensive solo development exercise, demonstrating skills in C# programming, Unity engine development, UI/UX design, and game architecture.
Tank Battleground is a simple top-down arena-style tank shooter developed in Unity. Players control a green tank and must navigate a grid-based battlefield, avoid obstacles, and destroy enemy vehicles (represented by blue cars) using projectile-based weapons. The environment contains destructible and static obstacles (white cube blocks), dynamic AI targets, and visual effects (explosions, health indicators).   

This repository contains all the project files, scripts, scene prefabs, and asset configurations necessary to build and run the game as seen in the screenshot above.   

Features   

    Player Tank Controls   

        Smooth movement & rotation using Rigidbody physics.   

        Turret aiming toward target position.   

        Projectile firing with collision detection.   

    Health System   

        Health bar slider (UI Image Fill) above the player tank.   

        Damage feedback: flashing or particle effect on hit.   

        Destroy player tank on health depletion.   

        The enemy prefab includes a World Space health bar with its fill image assigned. FaceCamera automatically finds and shares the MainCamera; no camera assignment is needed per enemy.

    Visual & Audio Effects

        Explosion particle system when enemies or obstacles are destroyed.

        Muzzle flash and shell tracers for tank cannon.

        (Optional) Background music and impact sounds.

    Modular Code Architecture

        Clean separation of scripts for movement, shooting, health management, and AI behavior.

        Easy to extend: add more enemy types, power-ups, or level variants.

Controls and QA

    Tank movement: WASD or gamepad left stick.

    Turret aim: Mouse or gamepad right stick.

    Fire: Left mouse button, gamepad right trigger, or right shoulder button.

    Debug panel: Press F3, Backquote (`), or the gamepad View/Select button in the MainMenu or during gameplay. It persists across scene loads.

    The debug panel includes FPS/frame-time and managed-memory metrics, active enemy/projectile counts, God Mode, enemy spawning, level skip, and 0.5x/1.0x/1.5x time-scale buttons.

    Scene gizmos: Camera bounds are outlined in cyan in the Scene view. Select an enemy tank to see its red attack radius (minDistance) and yellow detection radius (followRange).

    Enemy navigation: Active enemy tanks patrol to area-weighted random destinations across the whole baked NavMesh, choosing routes only when a complete path is available. Bake a NavMesh for each gameplay scene before testing; the enemy agent uses a 3-unit radius and 3.2-unit height, so use matching bake settings.

    Audio: GameAudioManager is initialized from the MainMenu and persists across scene loads. Its MainMenu Inspector assigns WinLevel.wav, LosingLevel.wav, and AttackSound.mp3 (used for player and enemy tank shots). The button-click slot is left empty until a dedicated click clip is added. The win clip plays when the last active enemy is destroyed; the lose clip plays when the player tank is destroyed.

The project is currently configured for Unity 6000.3.10f1. Package updates include Cinemachine 3.1.7, Input System 1.17.0, and AI Navigation 2.0.10. The enabled scene order is MainMenu, Level1, Level2, and Level3. The debug panel is bootstrapped by MainMenu, can be toggled in any scene, and persists across level loads; each gameplay scene also contains a fallback for launching it directly in the Editor. The debug level-skip button advances to the next enabled gameplay scene, skipping MainMenu and wrapping between gameplay scenes. Defeating all enemies opens the authored win panel in each level.

See OPTIMIZATION_TIPS.md for practical profiling and optimization recommendations.
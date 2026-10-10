Top-Down Tank Performance Tips

Use the Unity Profiler on a representative Windows build and target machine before changing quality settings. Optimize the measured bottleneck, then compare the same scene and workload again.

Rendering

- Use the Profiler's CPU/GPU Usage modules and Frame Debugger to find excessive draw calls, overdraw, and expensive passes. Share materials and meshes where it makes sense; combine small static meshes or use static batching for level geometry that never moves.
- Mark only genuinely stationary environment objects as Batching Static. Keep tanks, projectiles, animated objects, and anything that moves out of static batching.
- Bake static lighting and reflection probes for fixed level geometry. Use mixed or realtime lights only where gameplay needs them, and keep shadow-casting lights and shadow distance modest.
- Use appropriate texture sizes, platform-specific compression (for example, ASTC on supported mobile targets and BC formats on Windows), and mipmaps for 3D surfaces. Check visual quality after compression.

Physics and Gameplay

- Review Project Settings > Physics > Layer Collision Matrix. Disable layer pairs that do not need collisions; keep projectile, player, enemy, ground, and obstacle interactions explicit and tested.
- Use simple colliders for tanks and obstacles, and avoid unnecessary MeshColliders on moving objects. Keep physics work in fixed-step callbacks and avoid per-frame logging or allocations in hot paths.
- Pool frequently reused projectiles and impact effects. Size prewarm counts from observed concurrent use; verify that pool expansion and cleanup behave correctly during play.
- Use a ground-only layer mask for aim raycasts. If a level uses a different ground object, put its collider on the Ground layer or update that level's mask.

Frame Rate and Validation

- Profile a standalone Development Build with the Profiler attached; the Editor adds overhead and is not a reliable final FPS measurement.
- Pick a frame-rate target that the slowest supported target can sustain. Set vSync or Application.targetFrameRate deliberately for the target platform rather than assuming an uncapped frame rate is faster or smoother.
- Re-test the same combat scenario after each change. Compare frame time, CPU/GPU usage, draw calls, garbage collection, and memory—not just peak FPS.

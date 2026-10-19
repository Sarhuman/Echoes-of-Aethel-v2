# Echoes of Aethel v2 — art direction and acceptance

## Benchmark
Primary presentation reference: **Hades (2020), Supergiant Games**, Steam app 1145360.
Benchmark: contrast, staging, action readability, painterly environments, strong silhouettes and dramatic light.
**No copying** characters, assets, UI, emblems, environments, or specific compositions.

## Pipeline
**Unity 6 Universal 3D / URP, real-time 3D, locked orthographic isometric camera**.
Meshy Pro manual export > Blender art cleanup, rig and animation > Unity prefabs.
Hades used extensively pre-rendered 2D imagery from 3D; v2 is a distinct real-time 3D production, not identical rendering technology.

## Vertical slice art specification
- **Bastião:** distinctive armored silhouette, readable axe/shield at gameplay zoom, helmet/cloth contrast; skeletal rig, idle/walk/attack/guard/dash/hit/death. No generic capsule as approved art.
- **Lobo Etéreo:** wolf anatomy, ethereal markings, visible windup and bite, skeletal quadruped rig. No primitive capsule as approved art.
- **Arena / Bosque:** handmade modular ruins, twisted trees, layered ground, moss rock, terrain landmarks; establish clear traversable lanes and combat space.
- **Rendering:** cool ambient + warm local accents; painted materials with intentional hue/value grouping. Validate in Game view at 1080p, not just Blender renders.
- **Camera:** fixed orthographic, gameplay character/enemy scale evaluated together; zero permanent occlusion of player.
- **Attack:** anticipation > active > recovery; damage at authored contact time (not animation-only); telegraph distinguishable by motion and contrast.

## Acceptance gate
1. Capture actual runtime: stationary Bastion, moving Bastion, enemy windup, hit, dash, full arena.
2. Record gameplay and frame-time in Windows build on target RX 580 hardware; editor FPS is not benchmark.
3. Review before/after screenshots next to benchmark for *principles*, not copied compositions.
4. No visual change may delete scripts, colliders, scene references or undo previous gameplay.
5. Create scenes **only once**; builds must not regenerate levels.
6. No image mockup may be labeled as a Unity runtime screenshot.

## Scope boundary
The included arena generator uses capsules **solely as engineering placeholders**. Artistic approval is blocked until original authored models, animations, materials, lighting and scene dressing are integrated and tested. Do not claim Hades-quality from this scaffold.

## Near-term production order
1. Establish project and stable playable arena.
2. Produce a *single* approved hero model, then wolf, with rig and animations.
3. Produce environment kit of 1 tree, 1 rock, 1 ruined arch and 1 floor module.
4. Replace placeholders in scene by editing visual children/prefabs only.
5. Add hitstop/camera impulse/VFX as presentation, without changing damage rules.
6. Performance/visual playtest, then approve first biome before expanding.

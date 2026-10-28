# FastNoiseLite reuse

- Upstream: https://github.com/Auburn/FastNoiseLite
- Fixed ref: 785f37a9ad76e283586a379675085f2063ae03f7
- File: CSharp/FastNoiseLite.cs
- Git blob: 9b52a2e1633678b031587ffe1474a093f2c6aff1
- License: MIT; upstream header and LICENSE.txt retained.
- Reused directly for seeded OpenSimplex2S FBm and domain-warp sampling during seabed generation.
- The noise algorithm does not run in the water fragment shader. Sand-bank layout, reef aprons, highlight and refraction adjustments are project-authored.

Reviewed references, not imported wholesale:

- aniruddhahar/URP-WaterShaders: depth-controlled clarity/refraction and underwater caustics, Unity 2022 / URP14. Existing orthographic-safe depth reconstruction and Boat Attack caustics retained.
- MatrixRex/Uber-Stylized-Water: adjustable broad specular and broken surface highlights, Unity6. No Unity6 assets or shaders imported.
- mozankatip/InteractiveStylizedWater: render-target ripple interaction. Existing game wakes and ripple pools retained; no second interaction camera added.

This release modifies seabed shape, underwater visibility and restrained surface lighting. It does not claim to import all features of the reviewed projects.

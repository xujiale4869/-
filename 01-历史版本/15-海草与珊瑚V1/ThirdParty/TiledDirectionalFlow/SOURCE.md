# Tiled directional flow provenance

- Upstream: https://github.com/Scrawk/Tiled-Directional-Flow
- Pinned commit: `386892cc3f6e21b4694b2777a9e7e66d47a3dd67`
- Original algorithm: Frans van Hoesel, University of Groningen, 2010.
- Unity port: Justin / Scrawk, 2017.
- `Upstream.shader.txt` is the unmodified upstream `Assets/TiledDirectionalFlow/Shaders/TiledDirectinalFlow.shader`, stored as text to avoid importing its Built-in Surface Shader into URP.
- Original shader git blob: `4b99b1ed11c6fbfa3d9de6184479c284a8d3ed0e`; SHA256 `72612d7597ef0e7a151bb0530c7f1bada51eb3084db42366fa1ab0ce1ae9debc`.
- `LICENSE.txt` is the unmodified upstream LICENSE, including MIT and the original Creditware notice. SHA256 `ed471ce09ebcba676424c710e333080cb130c160049f2ec5f8935a2f54d883aa`.

Actual runtime adaptation: `Assets/Deepsea/Shaders/Includes/TiledCurrent.hlsl`. It keeps the four offset tiles, cubic blending, speed variation and variance compensation. It uses URP texture sampling, world metre coordinates, a locally baked direction field, and rotates sampled normal slopes back to world space.

The generated shore/reef flow field and smooth periodic normal texture in `ShallowWaterFlow.cs` are original project code. No upstream textures or sample scene were imported. The original refraction, depth reconstruction, seabed model, Gerstner height, and boat physics remain project-owned integrations.

Other repositories reviewed but not imported in this release: cbunt/unity-voronoi-water, TLabAltoh/TLabWaterSystem and softcat477/Water-Surface-Shader.

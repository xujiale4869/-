# Boat Attack water components

Source: https://github.com/Unity-Technologies/boat-attack-water
Pinned commit: 45d6e8c52a9f615fd6b83a91cdd0e9fda9ea86d3
Retrieved: 2026-10-16
License: Unity Companion License (see LICENSE.md and https://unity.com/legal/licenses/unity-companion-license).

Imported verbatim: Runtime/Shaders/GerstnerWaves.hlsl, Runtime/Textures/WaterNormals.tif, Runtime/Textures/Samples/basicFoam.png.
Textures are in ../../Resources/Water. Unity imports the packed RG/BA normal fields as linear RGBA data with mipmaps and a 1024 pixel maximum; foam is also linear data.
Ocean.shader directly includes the original Gerstner source. OceanSurface supplies four directional waves and matches their height on the CPU for the player's visual hull and wake.
OceanSurface reflection is a project-specific URP14 adapter derived from the horizontal-plane reflection approach in upstream Runtime/Rendering/PlanarReflections.cs. It removes dependencies on upstream quality settings and Unity.Mathematics and restores render state with try/finally.

This is a selective integration, not installation of the full preview water package. Existing propulsion, collision, rescue and save rules remain project code. Ocean-URP and Crest are not installed.

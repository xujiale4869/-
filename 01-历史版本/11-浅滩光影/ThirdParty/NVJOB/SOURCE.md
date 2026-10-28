# Coastal ecology assets

Source: https://github.com/nvjob/nvjob-boids
Pinned commit: cfeb0fc63f9863f473c2c30ccad6731aa1ef05e8
Upstream file: Assets/#NVJOB Boids/Simple Boids/Mesh/Bird Mesh.fbx
Imported as: Resources/Wildlife/Seabird.fbx, 40316 bytes
SHA256: EB157AE09528E52F37376BC3CC89BF0DEF9E05EC588CB07BC0E8A546E1F0A571
Derived mesh: Resources/Wildlife/CoastalBird.asset, normalized at import by EcologyBuild.
The bird geometry is reused under MIT; copyright and license are retained.

Bird-original.shader.txt preserves the upstream built-in-pipeline shader as a reference only. It is not executed or imported as a Unity shader.
Seabird.shader is original URP code with original wing deformation and coastal colour masks.
The fish mesh and steering helper retain their existing MarineLife attribution.
CoastalEcology.cs, the grass interaction, bird targeting, safe flight paths, and water shadows are original work.
No NVJOB example scene, water system, sound assets, or other bundled assets are imported.
Descent was reviewed as a plant-motion reference; no Descent code or assets are included.

This is a small authored gameplay abstraction, not a scientific ecosystem simulation.

# Reef life expansion

Original authored meshes: dome coral, sea fan, tube sponges, anemone ribbons, starfish, urchins and crab. Placement and resident behaviour are original. They represent stylized habitats rather than a biological simulation.

Branching coral continues to call mattatz/unity-procedural-tree at commit 05374e1d9f8a2b24d58f78b319248922d52bc6db. Its full MIT license and sources remain in ThirdParty/ProceduralTree; no new dependency or download was added this round.

New reef fish use the already imported Resources/Wildlife/SwimmingFish mesh; its Vivian Menard provenance and MIT notice are preserved in ThirdParty/MarineLife. ReefFish.shader extends the project's existing original MarineFish URP adapter with a striped tint. It retains the opaque-scene depth rejection for hull and island occlusion.

Reef foliage and benthos render as submerged opaque geometry before Ocean, which applies fog to the final water composite. The previous double fog blend is removed for the expanded habitat only. Water palettes, bathymetry and original object materials are not modified.

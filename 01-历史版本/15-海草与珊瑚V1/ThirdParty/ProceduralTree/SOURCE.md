# Procedural coral branch mesh source

Repository: https://github.com/mattatz/unity-procedural-tree

Pinned commit: 05374e1d9f8a2b24d58f78b319248922d52bc6db

License: MIT, Copyright (c) 2019 mattatz. Full license retained in LICENSE.txt.

Imported runtime source: ProceduralTree.cs, ProceduralModelingBase.cs and Curve/{CurveBase,CatmullRomCurve,FrenetFrame}.cs. The three curve sources were flattened into this folder; namespaces and implementation are unchanged. GitHub UTF-8 content is retained; the patch writer may normalize the final newline. No demo scene, editor script, bark texture or original tree material is used.

Actual use: ReefGarden.cs calls ProceduralModeling.ProceduralTree.Build once per coral colony. Seeded 3-generation branching is rescaled, flattened into broad coral forms, vertex-tinted and combined into a single static mesh. This is an adaptation of a tree mesh generator into coral silhouettes, not a downloaded biologically accurate coral model.

Placement, coral plate mesh, seagrass mesh, habitat composition, URP material and verification are original. Soft grass moves; hard coral branches do not bend.

Source blob SHAs:
- ProceduralTree.cs: f8da2c02a804f1c751722ee6fbbb62978bcb6e2b
- CurveBase.cs: 0d4d220dfe39050e1e2012dc287a036818fccadd
- CatmullRomCurve.cs: cad5936bb0d83980d657554536f14ceac2883710
- FrenetFrame.cs: 93386da1100f967e8fff31ba8f3e8cd66d979d63
- ProceduralModelingBase.cs: 2eadd74f1c0d4f0c8dee9898c6bb9383cd09b95d
- LICENSE: ca891aa49e4204d35e17d5faf22323cc3c4671cd

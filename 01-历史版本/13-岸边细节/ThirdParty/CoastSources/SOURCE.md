# East-shore wash / source record

## Imported sound, CC0

- Original: Rmutt, `oceanwaves-10.wav`, Freesound 156598.
- Original page and license: https://freesound.org/people/Rmutt/sounds/156598/ (Creative Commons 0).
- GitHub redistribution: https://github.com/twtrubiks/moonseal/tree/8e7ef422753e12083a152671ae33aef73c40f4cd/public/audio
- Imported file: public/audio/ocean.mp3, 1,441,167 bytes.
- Git blob SHA1: 6598bfdf270a89515cdd84c521e9912910624ad2.
- Downloaded file SHA256: 454ccb2efd30d28b0a0a594573d93d4c4faaffbe318a5b789052e5c7dbf1a7d4.
- Derived asset: Resources/Coast/OceanSurf.ogg. Mono 22,050 Hz, 24-second loop, two-second wrap crossfade. Peak normalization before Vorbis encoding. No speech or music was added.
- The upstream audio README is retained. The original field recording remains the credited source; the GitHub app's code license is separate from the recording's CC0 dedication.

CC0 deed: https://creativecommons.org/publicdomain/zero/1.0/
CC0 legal text: https://creativecommons.org/publicdomain/zero/1.0/legalcode.en

## Reviewed, not imported

- MatrixRex/Uber-Stylized-Water, revision 1fdd16c6312611fef7fb248dee86cdd3fd456a1e. MIT; requires Unity 6. The current game is Unity 2022.3 / URP 14, so none of its generated shader code, assets, or dependencies are included.
- JiahuanZhang/ToonWaterShader-URP. Depth-buffer foam reference; no code/assets included.
- przemyslawzaworski/Unity-Procedural-Rock-Generation, revision 4260cdaaf120049a2fd846c655a5bc62064b971c. README states MIT; GPU scalar-field / triangulation generator reviewed. No code/assets included.

## Original work

ShoreCycle.cs, SurfWash.hlsl, ShoreSwash.shader, ShorelineAtmosphere.cs and the two bounded CPU-generated rock meshes are original. Foam, wet trail and local ocean sound gain share one time/phase definition. Rocks replace only rendering meshes/materials; existing colliders remain assigned to their original geometry.

The existing Ocean shader continues to use the project's already-attributed Boat Attack foam/normal maps and Gerstner code. No new water system is substituted.

## Verification status

This package has offline mathematical/audio checks only. The remote Windows device did not respond during preparation. Unity compilation, Windows rendering, navigation regression and listening in game are pending; the included checks must run before treating this as a release.

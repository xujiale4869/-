# Marine life integration
## Imported and used
- VivianMenard/boids: https://github.com/VivianMenard/boids
  Revision: 3908b0710211f7157e927a11ba8127f9a3d877db
  Original: Assets/3DObjects/fish.fbx
  Imported as Resources/Wildlife/Fish.fbx; normalized mesh generated as SwimmingFish.asset.
  License: VivianBoids-LICENSE.txt (MIT, retain copyright and permission notice).
- SebLague/Boids: https://github.com/SebLague/Boids
  Revision: fdda4db1368df92fbacd8a644bbe6dc61d705441
  Original: Assets/Scripts/Boid.cs, preserved as SebBoid.cs.txt.
  SchoolSteering.cs adapts target, alignment, cohesion, separation and steering-force calculations.
  License: SebBoids-LICENSE.txt (MIT, retain copyright and permission notice).
## Authored for this project
Wildlife.cs handles the bay habitats, vessel avoidance, staggered flight, water sampling and pooling.
Long pectoral fin mesh, MarineFish/MarineSplash shaders, impact droplets and Ocean.shader ripple integration are project-authored.
No flying-fish package or interactive-water package is claimed to be imported.
Boat Attack water source and its separate license remain in ../BoatAttack.

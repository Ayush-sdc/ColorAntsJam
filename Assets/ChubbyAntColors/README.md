# Chubby Ant Colors

Nine colors: Pink, Red, Orange, Yellow, Green, Cyan, Blue, Purple, Brown. Each has plain and WithRope prefabs, for 18 total. Import the package and drag a prefab from Assets/ChubbyAntColors/Prefabs into your scene.

All colors share the same chubby ant meshes. Bodies, limbs and irises have matching color materials; eyes, pupils, catchlights, cheeks and golden rope share materials. Toggle the rope child to release an ant visually.

Local +Z is forward, Y is up. Approximate dimensions: 1.6 wide by 2.85 long. Start at uniform scale 0.28–0.30 for one-unit queue spacing. Includes a box collider and color showcase scene.

Static meshes: no walking animation, queue movement or rope physics. Fixed art-light shader, independent of scene lights and shadows. Built-in renderer verified in Unity 2022.3; URP has not been tested and HDRP is not supported. Mesh detail is unchanged: 56,752 triangles plain / 72,112 with rope. Profile large crowds on your target device.

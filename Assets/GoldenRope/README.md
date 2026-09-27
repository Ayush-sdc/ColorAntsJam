# Golden rope connectors

Import GoldenRope.unitypackage into Unity. Prefabs are in Assets/GoldenRope/Prefabs. Four lengths are included: 2, 3, 4 and 5 units, assuming a grid cell is 1 unit. Pick a rope long enough to span your linked ant queues.

Each prefab is a real 3D mesh with three twisted golden strands, closed cut ends, subtle fiber lines, one renderer, and one shared material. The reference's compact tan/gold rope strip informed the color and twist. This is a visual connector, not a physics rope or a connected-queue gameplay implementation.

Placement: local +Z is lengthwise, +Y faces the top-down camera. Pivot is at the center; thickness is about 0.262 units. Place it about 0.2–0.3 units above the board to avoid intersecting ants. Rotate around Y by 90 degrees for a horizontal connector. Scale X and Y equally to change thickness. Scaling Z changes length and stretches the twist pitch; select the closest baked length for best results.

Optional endpoint following: assign Start and End on RopeBetweenPoints. It centers and points the rope between the transforms and updates its length. World Offset defaults to +0.25 Y. Leave the fields empty for normal static placement. Root-level placement or a uniformly scaled parent is recommended. Do not use endpoints that are children of the rope. There is no sag simulation or collider.

The supplied AntsJam/Golden Rope shader uses fixed art lighting to preserve the reference's appearance regardless of scene lighting. Its single SRPDefaultUnlit pass is designed for Built-in and URP; it does not receive or cast scene shadows and is not an HDRP shader. Color, groove tint, fiber highlight and light direction are editable. Built-in rendering is verified in the included Unity preview. URP must be checked in your target project.

Open Scenes/RopeShowcase for all lengths. The editor menu Tools > Ant Queue > Build Golden Rope Prefabs regenerates the named assets and opens a new scene. Save your scene first. It exports a package and PNG to the project parent folder.

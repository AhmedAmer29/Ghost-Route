Modular Red Brick Wall

Files:
- ModularRedBrickWall.fbx: Unity-ready model
- ModularRedBrickWall.blend: Blender source file
- create_modular_brick_wall.py: Blender generator script

Size:
- Width: 4.0 Unity units
- Height: 3.2 Unity units
- Depth: 0.36 Unity units

Use in Main Street:
- Drag ModularRedBrickWall.fbx into the scene.
- Add a Box Collider sized about X 4.0, Y 3.2, Z 0.36 if Unity does not generate one.
- Keep the wall at full height so the player cannot jump over it.
- Duplicate segments side by side using the Snap_Left and Snap_Right child transforms.
- Stack only if needed using Snap_Top and Snap_Bottom.

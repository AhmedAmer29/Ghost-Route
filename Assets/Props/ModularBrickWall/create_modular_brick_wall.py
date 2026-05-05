import math
from pathlib import Path

import bpy


OUT_DIR = Path(__file__).resolve().parent
BLEND_PATH = OUT_DIR / "ModularRedBrickWall.blend"
FBX_PATH = OUT_DIR / "ModularRedBrickWall.fbx"

WALL_WIDTH = 4.0
WALL_HEIGHT = 3.2
WALL_DEPTH = 0.36
BRICK_HEIGHT = 0.18
MORTAR = 0.02
ROWS = 16


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()


def make_material(name, color, roughness=0.85):
    material = bpy.data.materials.new(name)
    material.use_nodes = True
    bsdf = material.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = color
    bsdf.inputs["Roughness"].default_value = roughness
    return material


def add_cube(name, location, scale, material):
    bpy.ops.mesh.primitive_cube_add(size=1, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(material)
    return obj


def bevel_object(obj, amount, segments=1):
    bevel = obj.modifiers.new("Small chipped edge bevel", "BEVEL")
    bevel.width = amount
    bevel.segments = segments
    bevel.affect = "EDGES"

    weighted_normals = obj.modifiers.new("Weighted normals", "WEIGHTED_NORMAL")
    weighted_normals.keep_sharp = True


def build_wall():
    brick_materials = [
        make_material("Brick_Red_A", (0.56, 0.12, 0.07, 1.0)),
        make_material("Brick_Red_B", (0.42, 0.08, 0.045, 1.0)),
        make_material("Brick_Red_C", (0.66, 0.18, 0.10, 1.0)),
        make_material("Brick_Dark_Soot", (0.24, 0.045, 0.035, 1.0)),
    ]
    mortar_material = make_material("Dark_Mortar", (0.19, 0.18, 0.16, 1.0), 0.95)
    # One solid backing piece keeps the wall fully opaque and gives Unity a simple collider shape.
    backing = add_cube(
        "BrickWall_OpaqueBacking_Collider",
        (0, 0, WALL_HEIGHT / 2),
        (WALL_WIDTH, WALL_DEPTH, WALL_HEIGHT),
        mortar_material,
    )
    backing.display_type = "TEXTURED"

    row_pitch = BRICK_HEIGHT + MORTAR
    brick_depth = WALL_DEPTH + 0.025
    full_brick_width = 0.38
    half_brick_width = (full_brick_width - MORTAR) / 2

    bricks = [backing]
    for row in range(ROWS):
        z = MORTAR + BRICK_HEIGHT / 2 + row * row_pitch
        is_offset = row % 2 == 1

        if is_offset:
            x = -WALL_WIDTH / 2 + half_brick_width / 2
            first_width = half_brick_width
        else:
            x = -WALL_WIDTH / 2 + full_brick_width / 2
            first_width = full_brick_width

        brick_index = 0
        while x < WALL_WIDTH / 2 - MORTAR:
            width = first_width if brick_index == 0 and is_offset else full_brick_width
            right_edge = x + width / 2
            if right_edge > WALL_WIDTH / 2 - MORTAR:
                width -= right_edge - (WALL_WIDTH / 2 - MORTAR)

            if width > 0.06:
                material = brick_materials[(row * 3 + brick_index) % len(brick_materials)]
                proud = 0.005 * math.sin(row * 1.7 + brick_index * 0.9)
                brick = add_cube(
                    f"Brick_R{row:02d}_{brick_index:02d}",
                    (x, -0.015 + proud, z),
                    (width, brick_depth, BRICK_HEIGHT),
                    material,
                )
                bevel_object(brick, 0.01, 1)
                bricks.append(brick)

            x += width + MORTAR
            brick_index += 1
            first_width = full_brick_width

    # Thin caps make the module easy to stack vertically and align from the sides.
    caps = [
        add_cube("Flat_Top_Snap_Edge", (0, 0, WALL_HEIGHT + 0.025), (WALL_WIDTH, WALL_DEPTH, 0.05), mortar_material),
        add_cube("Flat_Bottom_Snap_Edge", (0, 0, 0.025), (WALL_WIDTH, WALL_DEPTH, 0.05), mortar_material),
        add_cube("Left_Side_Snap_Edge", (-WALL_WIDTH / 2 + 0.025, 0, WALL_HEIGHT / 2), (0.05, WALL_DEPTH, WALL_HEIGHT), mortar_material),
        add_cube("Right_Side_Snap_Edge", (WALL_WIDTH / 2 - 0.025, 0, WALL_HEIGHT / 2), (0.05, WALL_DEPTH, WALL_HEIGHT), mortar_material),
    ]
    for cap in caps:
        bevel_object(cap, 0.006, 1)
        bricks.append(cap)

    # Connector markers: Unity imports these as empty transforms for easy side/top/bottom alignment.
    connector_specs = [
        ("Snap_Left", (-WALL_WIDTH / 2, 0, WALL_HEIGHT / 2)),
        ("Snap_Right", (WALL_WIDTH / 2, 0, WALL_HEIGHT / 2)),
        ("Snap_Top", (0, 0, WALL_HEIGHT)),
        ("Snap_Bottom", (0, 0, 0)),
    ]
    for name, location in connector_specs:
        marker = bpy.data.objects.new(name, None)
        bpy.context.collection.objects.link(marker)
        marker.location = location
        marker.empty_display_type = "PLAIN_AXES"
        marker.empty_display_size = 0.2
        bricks.append(marker)

    bpy.ops.object.select_all(action="DESELECT")
    for obj in bricks:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = backing

    empty = bpy.data.objects.new("ModularRedBrickWall_Root", None)
    bpy.context.collection.objects.link(empty)
    empty.empty_display_type = "CUBE"
    empty.empty_display_size = 0.35

    for obj in bricks:
        obj.parent = empty

    return empty


def add_lights_and_camera():
    bpy.ops.object.light_add(type="AREA", location=(0, -4, 4))
    light = bpy.context.object
    light.name = "Preview_Area_Light"
    light.data.energy = 450
    light.data.size = 5

    bpy.ops.object.camera_add(location=(0, -6, 2.0), rotation=(math.radians(74), 0, 0))
    bpy.context.scene.camera = bpy.context.object


def export_asset():
    bpy.ops.wm.save_as_mainfile(filepath=str(BLEND_PATH))
    bpy.ops.export_scene.fbx(
        filepath=str(FBX_PATH),
        use_selection=False,
        object_types={"EMPTY", "MESH"},
        apply_unit_scale=True,
        bake_space_transform=False,
        axis_forward="-Z",
        axis_up="Y",
        add_leaf_bones=False,
    )


def main():
    clear_scene()
    bpy.context.scene.unit_settings.system = "METRIC"
    bpy.context.scene.unit_settings.scale_length = 1.0
    build_wall()
    add_lights_and_camera()
    export_asset()


if __name__ == "__main__":
    main()

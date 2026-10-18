"""将冻结的芙莉莲 3.8 待机数据烘焙为原生 Polygon2D/AnimationPlayer。

这是针对本素材的构建期格式转换，不是通用 Spine 运行时。保留图集原始像素，
在 24 fps 采样骨骼和顶点形变；遇到未支持的数据立即失败，避免静默丢失动画。
"""
import json
import math
import os
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = Path(os.environ.get("FRIEREN_STS1_SOURCE", ROOT.parents[1] / "references/FrierenMod_STS1")) / "src/main/resources/FrierenModResources/img/char/Frieren"
OUT = ROOT / "FrierenMod/scenes"
PREFIX = "res://FrierenMod/images/character/"


def numbers(values):
    return ", ".join(f"{v:.5f}" for v in values)


def vector_array(values):
    return "PackedVector2Array(" + numbers(values) + ")"


def atlas_regions():
    regions = {}
    for line in (SOURCE / "character_ArmatureFrieren.atlas").read_text().splitlines()[6:]:
        if not line.strip():
            continue
        if not line.startswith(" "):
            current = regions.setdefault(line, {})
        else:
            key, value = line.strip().split(": ")
            current[key] = value if key == "rotate" else [int(x) for x in value.split(",")]
    return regions


def blend_keys(keys, time, values):
    if not keys:
        return []
    left = keys[0]
    for right in keys[1:]:
        if time <= right.get("time", 0):
            t = (time - left.get("time", 0)) / (right["time"] - left.get("time", 0))
            return [a + (b - a) * max(0, t) for a, b in zip(values(left), values(right))]
        left = right
    return values(left)


def transform(matrix, x, y):
    a, b, c, d, tx, ty = matrix
    return a * x + b * y + tx, c * x + d * y + ty


def bake():
    data = json.loads((SOURCE / "character_ArmatureFrieren.json").read_text(encoding="utf-8-sig"))
    assert data["skeleton"]["spine"] == "3.8.75"
    assert list(data["animations"]) == ["Idle"]
    assert not any(data.get(k) for k in ["ik", "transform", "path"])
    idle = data["animations"]["Idle"]
    assert set(idle) <= {"bones", "deform"}
    for timelines in idle["bones"].values():
        assert set(timelines) == {"rotate"}
    def check_curves(value):
        if isinstance(value, dict):
            assert "curve" not in value
            for child in value.values():
                check_curves(child)
        elif isinstance(value, list):
            for child in value:
                check_curves(child)
    check_curves(idle)
    regions = atlas_regions()
    bones = data["bones"]
    indices = {bone["name"]: i for i, bone in enumerate(bones)}

    def matrices(time):
        result = []
        for bone in bones:
            assert not any(k in bone for k in ["transform", "shearX", "shearY", "scaleX", "scaleY"])
            keys = idle["bones"].get(bone["name"], {}).get("rotate", [])
            angle = bone.get("rotation", 0) + (blend_keys(keys, time, lambda k: [k.get("angle", 0)])[0] if keys else 0)
            co, si = math.cos(math.radians(angle)), math.sin(math.radians(angle))
            x, y = bone.get("x", 0), bone.get("y", 0)
            if "parent" in bone:
                a, b, c, d, tx, ty = result[indices[bone["parent"]]]
                result.append((a*co+b*si, -a*si+b*co, c*co+d*si, -c*si+d*co, a*x+b*y+tx, c*x+d*y+ty))
            else:
                result.append((co, -si, si, co, x, y))
        return result

    times = [i / 24 for i in range(121)]
    poses = [matrices(t) for t in times]
    meshes = []
    for slot in data["slots"]:
        name = slot["attachment"]
        attachment = data["skins"]["default"][slot["name"]][name]
        region = regions[name]
        assert attachment.get("type", "region") in ["region", "mesh"]
        assert slot.get("blend", "normal") == "normal"
        # UV 为原始未裁切图片坐标；还原图集旋转、裁边的偏移。
        x, y = region["xy"]
        w, h = region["size"]
        ow, oh = region["orig"]
        ox, oy = region["offset"]
        rotated = region["rotate"] == "true"
        if attachment.get("type") == "mesh":
            vertices = attachment["vertices"]
            local_uv = attachment["uvs"]
            triangles = attachment["triangles"]
        else:
            assert (w, h) == (ow, oh) and (ox, oy) == (0, 0)
            aw, ah = attachment["width"] / 2, attachment["height"] / 2
            co, si = math.cos(math.radians(attachment.get("rotation", 0))), math.sin(math.radians(attachment.get("rotation", 0)))
            vertices = []
            for vx, vy in [(-aw, -ah), (-aw, ah), (aw, ah), (aw, -ah)]:
                vertices.extend([co*vx-si*vy+attachment.get("x", 0), si*vx+co*vy+attachment.get("y", 0)])
            local_uv = [0, 1, 0, 0, 1, 0, 1, 1]
            triangles = [0, 1, 2, 2, 3, 0]
        uv = []
        for u, v in zip(local_uv[::2], local_uv[1::2]):
            uv.extend([x-(oh-oy-h)+v*oh, y-(ow-ox-w)+(1-u)*ow] if rotated else [x-ox+u*ow, y-(oh-oy-h)+v*oh])
        deform = idle.get("deform", {}).get("default", {}).get(slot["name"], {}).get(name, [])
        weighted = len(vertices) != len(local_uv)
        deform_size = len(vertices)
        if weighted:
            cursor = deform_size = 0
            while cursor < len(vertices):
                count = int(vertices[cursor])
                deform_size += count * 2
                cursor += 1 + count * 4
        def offsets(key):
            result = [0] * deform_size
            start = key.get("offset", 0)
            result[start:start+len(key.get("vertices", []))] = key.get("vertices", [])
            return result
        frames = []
        for t, pose in zip(times, poses):
            points = []
            delta = blend_keys(deform, t, offsets) if deform else [0] * deform_size
            if weighted:
                cursor = deformation_cursor = 0
                while cursor < len(vertices):
                    count = int(vertices[cursor]); cursor += 1
                    px = py = total = 0
                    for _ in range(count):
                        bone, vx, vy, weight = vertices[cursor:cursor+4]; cursor += 4
                        vx += delta[deformation_cursor]; vy += delta[deformation_cursor+1]
                        deformation_cursor += 2
                        wx, wy = transform(pose[int(bone)], vx, vy)
                        px += wx * weight; py += wy * weight; total += weight
                    assert abs(total - 1) < 0.002
                    points.extend([px, py])
            else:
                for i in range(0, len(vertices), 2):
                    points.extend(transform(pose[indices[slot["bone"]]], vertices[i]+delta[i], vertices[i+1]+delta[i+1]))
            assert len(points) == len(local_uv)
            # 旧模组除以 4；Godot Y 轴向下。地面原点沿用原骨骼原点。
            frames.append([value / (4 if i % 2 == 0 else -4) for i, value in enumerate(points)])
        meshes.append(dict(name=slot["name"], uv=uv, triangles=triangles, frames=frames,
                           alpha=int(slot.get("color", "ffffffff")[6:8], 16)/255))

    def track(index, path, keys, values, discrete=False):
        return f'''tracks/{index}/type = "value"
tracks/{index}/path = NodePath("{path}")
tracks/{index}/interp = 1
tracks/{index}/enabled = true
tracks/{index}/keys = {{"times": PackedFloat32Array({numbers(keys)}), "transitions": PackedFloat32Array({numbers([1]*len(keys))}), "update": {1 if discrete else 0}, "values": [{', '.join(values)}]}}
'''
    content = ['[gd_scene load_steps=11 format=3]\n',
               f'[ext_resource type="Texture2D" path="{PREFIX}atlas.png" id="1"]\n',
               f'[ext_resource type="Texture2D" path="{PREFIX}corpse.png" id="2"]\n',
               '[ext_resource type="Script" path="res://FrierenMod/presentation/return_to_idle.gd" id="3"]\n']
    for state in ["Idle", "Attack", "Cast", "Hit", "Dead", "Relaxed"]:
        duration = 5/1.2 if state in ["Idle", "Relaxed"] else 0.5
        content.append(f'[sub_resource type="Animation" id="Anim_{state}"]\nresource_name = "{state}"\nlength = {duration}\nloop_mode = {1 if state in ["Idle", "Relaxed"] else 0}\n')
        for i, mesh in enumerate(meshes):
            keys = [t/1.2 for t in times] if state in ["Idle", "Relaxed"] else [0]
            frames = mesh["frames"] if len(keys) > 1 else mesh["frames"][:1]
            content.append(track(i, f'Visuals/Body/{mesh["name"]}:polygon', keys, [vector_array(f) for f in frames]))
        n = len(meshes)
        content.append(track(n, "Visuals/Body:visible", [0], ["false" if state == "Dead" else "true"], True))
        content.append(track(n+1, "Visuals/Corpse:visible", [0], ["true" if state == "Dead" else "false"], True))
        motion = {"Attack": [0, 20, 0], "Cast": [0, -5, 0], "Hit": [0, -12, 0]}.get(state, [0, 0, 0])
        content.append(track(n+2, "Visuals:position", [0, duration/2, duration], [f"Vector2({x}, 0)" for x in motion]))
        color = {"Cast": "Color(1.3, 1.5, 1.7, 1)", "Hit": "Color(1.5, 0.6, 0.6, 1)"}.get(state, "Color(1, 1, 1, 1)")
        content.append(track(n+3, "Visuals:modulate", [0, duration/2, duration], ["Color(1, 1, 1, 1)", color, "Color(1, 1, 1, 1)"]))
    content.append('[sub_resource type="AnimationLibrary" id="Library"]\n_data = {' + ', '.join(f'"{s}": SubResource("Anim_{s}")' for s in ["Idle", "Attack", "Cast", "Hit", "Dead", "Relaxed"]) + '}\n')
    content.append('''[node name="Frieren" type="Node2D"]
[node name="Visuals" type="Node2D" parent="."]
unique_name_in_owner = true
[node name="Body" type="Node2D" parent="Visuals"]
''')
    for mesh in meshes:
        triangles = ', '.join('PackedInt32Array(' + ', '.join(map(str, mesh["triangles"][i:i+3])) + ')' for i in range(0, len(mesh["triangles"]), 3))
        content.append(f'''[node name="{mesh['name']}" type="Polygon2D" parent="Visuals/Body"]
texture = ExtResource("1")
color = Color(1, 1, 1, {mesh['alpha']})
polygon = {vector_array(mesh['frames'][0])}
uv = {vector_array(mesh['uv'])}
polygons = [{triangles}]
''')
    content.append('''[node name="Corpse" type="Sprite2D" parent="Visuals"]
visible = false
position = Vector2(-36, -113)
scale = Vector2(0.42, 0.42)
texture = ExtResource("2")
[node name="Bounds" type="Control" parent="."]
unique_name_in_owner = true
offset_left = -100.0
offset_top = -330.0
offset_right = 100.0
mouse_filter = 2
[node name="AnimationPlayer" type="AnimationPlayer" parent="."]
libraries = {&"": SubResource("Library")}
autoplay = "Idle"
script = ExtResource("3")
''')
    target = OUT / "frieren_combat.tscn"
    target.write_text('\n'.join(content), encoding="utf-8")
    return target


if __name__ == "__main__":
    print(bake())

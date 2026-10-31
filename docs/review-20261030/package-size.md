# 模组体积核对

2026-10-31，0.10.5。MiB = 1,048,576 字节。仅统计发行内容，不把工作区、原始资料、Git历史和测试输出当成安装大小。

| 发行文件 | MiB |
|---|---:|
| FrierenMod.dll | 0.62 |
| FrierenMod.json | 0.00 |
| FrierenMod.pck | 235.81 |
| LICENSE.STS1 | 0.00 |
| LICENSE.WATCHER | 0.00 |
| README.md | 0.03 |

| 打包资源分类 | MiB |
|---|---:|
| 其余原图片和图层 | 76.83 |
| 纹理及其他Godot导入副本 | 51.56 |
| 音频导入副本 | 43.75 |
| 20首BGM原音频 | 42.35 |
| 卡槽背景原图 | 19.03 |
| 本地化、场景和其他资源 | 1.74 |
| 其他原音频 | 0.34 |

0.10.4起成就悬停直接复用二代字体。四份一代字体仅保留为仓库资料，原字体约39.29MiB及导入副本约34.95MiB均不再打入发行包。

其余 `FrierenMod/` 资源原文件和 `.import` 指向的 Godot 导入副本仍一起打包。图片原 PNG 与导入纹理、音频原文件与导入流会同时占体积。PCK 是打包容器，不会自动把所有文件再做一次通用压缩。

可以进一步按引用排除未使用资源、停止重复装入可省略的原文件，但要同时验证 Godot 导入路径、直接 FileAccess 读取和纹理/音频运行时加载；本批先统计，不擅自删原图、音频或作者资料。

| 最大单项（含导入副本） | MiB |
|---|---:|
| .godot/imported/south_hero.ogg-0d464658e5279b0e0067ffc710e791b1.oggvorbisstr | 3.49 |
| FrierenMod/audio/bgm/south_hero.ogg | 3.42 |
| .godot/imported/divine_revolte.ogg-42be1bba43ec22a9711a471cd59d4bae.oggvorbisstr | 3.29 |
| FrierenMod/audio/bgm/divine_revolte.ogg | 3.21 |
| .godot/imported/golden_land.ogg-c57b7663962f9a74c0bdb0fa1d5a1b82.oggvorbisstr | 3.03 |
| FrierenMod/audio/bgm/golden_land.ogg | 2.97 |
| .godot/imported/first_class_mage.ogg-6b603319218bb596e202b6feece5eb42.oggvorbisstr | 2.90 |
| FrierenMod/audio/bgm/first_class_mage.ogg | 2.82 |
| .godot/imported/ready_weapon.ogg-24269d626e0cea013ceba15606963640.oggvorbisstr | 2.74 |
| .godot/imported/zoltraak.ogg-bae6b611012a519aeee0075520a9a5bf.oggvorbisstr | 2.69 |
| .godot/imported/frieren_slayer.ogg-a34f13876adb7e69ef0504c135a28fc4.oggvorbisstr | 2.68 |
| FrierenMod/audio/bgm/ready_weapon.ogg | 2.65 |
| FrierenMod/audio/bgm/frieren_slayer.ogg | 2.62 |
| FrierenMod/audio/bgm/zoltraak.ogg | 2.62 |
| .godot/imported/magic_within.ogg-3b818e35eb270f9da4026b068f679c01.oggvorbisstr | 2.49 |
| FrierenMod/audio/bgm/magic_within.ogg | 2.43 |
| .godot/imported/knife_throat.ogg-c81a8d2dd2ab317d5ae589165deb0c8e.oggvorbisstr | 2.33 |
| FrierenMod/audio/bgm/knife_throat.ogg | 2.26 |
| .godot/imported/dangerous_medley.ogg-57f9512c6254c61f7092a702b44888e6.oggvorbisstr | 2.19 |
| .godot/imported/dragon_smasher.ogg-6ea259850c8570f7d11c1cfb3192141e.oggvorbisstr | 2.15 |
| .godot/imported/odd_clouds.ogg-0c3c1a2e75cf9f17da8e10675e4e8f13.oggvorbisstr | 2.14 |
| FrierenMod/audio/bgm/dangerous_medley.ogg | 2.14 |
| FrierenMod/audio/bgm/dragon_smasher.ogg | 2.09 |
| FrierenMod/audio/bgm/odd_clouds.ogg | 2.07 |
| .godot/imported/sleepy_frieren.ogg-8c19d40ec8f47a4efba8a724197d1e7a.oggvorbisstr | 2.00 |
| .godot/imported/waltz.ogg-ef8320ecd3c05da345d61d3e0c10e93c.oggvorbisstr | 1.99 |
| FrierenMod/audio/bgm/sleepy_frieren.ogg | 1.96 |
| FrierenMod/audio/bgm/waltz.ogg | 1.95 |
| .godot/imported/grassy_turtles.ogg-af22bc113d354d47d2d9926e7b002c3d.oggvorbisstr | 1.93 |
| .godot/imported/deep_dungeon.ogg-ce33dca69c706efbfb04827b9653baaa.oggvorbisstr | 1.89 |

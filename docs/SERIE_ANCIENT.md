# 第三幕先古之民：赛莉耶

0.12.18加入，0.12.24扩展。使用BaseLib `CustomAncientModel`，仅全队均为芙莉莲时加入第三幕（`ActModel.Index == 2`）随机候选。首次按原生芙莉莲访问记录必遇，之后与其他候选等权随机；联机由房主统一判定，在原生地图生成结束、显示之前同步，各玩家存档保留整局决策。入口、治疗、对话、选项、领取和结束页沿用原生流程。控制台可输入 `ancient FRIERENMOD-SERIE` 预览。

奖励池为冰柱樱、服从的天平、舒适的床、辛美尔之墓、圣雪结晶、腐坏贤者之颅，稀有度为Ancient，百科归入赛莉耶。事件从未持有的遗物中随机展示最多三件，选一件；六件共用一个池，墓碑权重0.5，其他权重1，不放回抽取。剩余不足三件时展示全部剩余，全部持有时直接结束。第三幕三星委托保留原有四件一代Boss遗物入口。

0.12.20：服从的天平按作者新版重做为回合结束时检查抽／手／弃三堆魔力，有魔力则通过原生EnergyNextTurnPower在下一回合获得1能量，不再有旧版的普通战斗限制与额外抽牌。仅在持有者参与的玩家回合触发，已消耗区魔力不计入。

作者提供的928×524原图保存在 `assets/author_adjusted/20261111/Serie.png`，游戏图保持原字节；场景等比铺满，底部渐暗以承托原生对白和选项。`tools/prepare-serie-assets.mjs` 从原图裁切地图/历史头像，不重画人物。完整场景为 `FrierenMod/scenes/ancients/serie.tscn`。

对话为本模组原创，包括初遇、花的开放时间、舒服的床三组。性格参考动画第27～28话中两人对魔法的不同看法及协会禁入的背景，保留赛莉耶的刻薄和芙莉莲的平淡回嘴，不使用逐句摘抄。参考：[动画官方第27话](https://frieren-anime.jp/story/ep27/)、[木棉花官方第27话](https://www.youtube.com/watch?v=V_HG3UJPN78)、[动画官方第28话](https://frieren-anime.jp/story/ep28/)。中英文本在各语言 `ancients.json` 的 `FRIERENMOD-SERIE` 条目维护。

同批修复升级圣徽：`HolyEmblem.GetUpgradeReplacement()` 需返回规范模型，原生百科的解锁/发现集合使用模型实例比较，返回 `ToMutable()` 会显示成锁。作者写入 `HolyEmblemOutline.png` 的金色光芒版保存为 `assets/author_adjusted/20261111/UpgradedHolyEmblem.png` 并转接到独立升级图；原普通圣徽轮廓恢复。升级版Atlas使用完整128×128画布，避免原68×68裁框截断光芒。

专项验证入口：`tools/run-engine-check.ps1 -Mode serie-only` 与 `-Mode upgraded-emblem-only`；前者检查候选范围、随机一致性、持有排除、四件领取/存档、双语对话、真实百科节点及原图资源，后者保留原有升级替换与魔法书存档测试。未替代游戏实测。

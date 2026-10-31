# 能力正负分类核对

0.10.10，共57个具体模组能力模型，含旧档兼容模型。按能力持有者承受的效果分类；敌人的增益对玩家不利，也仍属于敌人的增益。

本轮修正：封印、赖床、好黑啊、自大的魔族、灵魂解放改为减益；服从天平、三种谜题线索及隐藏顺序状态改为中性；已弃用小伎俩无效果，归中性。封印被除锈等清除时归还暂存卡，避免卡牌丢失。

混合或资源转换效果保留其既有主要用途分类：睡衣形态以消耗魔力造成伤害，雷电魔法以伤害替代抽牌，魔力代付以魔力替代能量，均保留增益。力量敏捷交换是临时交换后的恢复标记，保留增益。

| 能力 | 模型 | 分类 |
|---|---|---|
| 下回合加速同调 | `AccelerateFlowNextTurnPower` | 增益 |
| 加速同调 | `AccelerateFlowPower` | 增益 |
| 自大的魔族 | `AuraArrogancePower` | 减益 |
| 优雅 | `AuraElegancePower` | 增益 |
| 服从天平 | `AuraScalesPower` | 中性 |
| 灵魂解放 | `AuraSoulPower` | 减益 |
| 服从 | `AuraSubmissionPower` | 减益 |
| 无法生成魔力 | `BanManaGainPower` | 减益 |
| 捕鸟魔法 | `BirdCapturedPower` | 减益 |
| 无耗咏唱 | `ChantWithoutManaPower` | 增益 |
| 从容咏唱 | `ChantWithoutManaTimesPower` | 增益 |
| 专注 | `ConcentrationPower` | 增益 |
| 复制体 | `CopyPower` | 增益 |
| 好黑啊 | `DarkPower` | 减益 |
| 神格 | `DivinityPower` | 增益 |
| 领域展开 | `DomainExpansionPower` | 增益 |
| 魔力 | `EnemyManaPower` | 增益 |
| ？？！ | `Factor001Power` | 中性 |
| ？！？ | `Factor010Power` | 中性 |
| ！？？ | `Factor100Power` | 中性 |
| 飞行魔法 | `FlyingMagicPower` | 减益 |
| 芙莉莲的手提箱 | `FrierenSuitcasePower` | 增益 |
| 葬送的芙莉莲 | `FrierenTheSlayerPower` | 增益 |
| 全速前进 | `FullAheadPower` | 增益 |
| 饰品魔法 | `GainRelicPower` | 增益 |
| 我要更多奖励！ | `GoodLuckRewardPower` | 增益 |
| 魔力亲和 | `ManaAffinityPower` | 增益 |
| 魔力代付 | `ManaInsteadOfEnergyPower` | 增益 |
| 魔力回流 | `ManaReturnPower` | 减益 |
| 真言 | `MantraPower` | 增益 |
| 金属化 | `MetallicizePower` | 增益 |
| 宝箱怪 | `MimicPower` | 中性 |
| 打开时空之门 | `OpenTheWaygatePower` | 增益 |
| 涌流 | `OutpouringPower` | 增益 |
| 睡衣形态 | `PajamasFormPower` | 增益 |
| 纸飞机 | `PaperPlanePower` | 增益 |
| 永续 | `PerpetualPower` | 增益 |
| 幻影 | `PhantasmalPower` | 增益 |
| ？？？ | `PuzzlePower` | 中性 |
| 再循环 | `RecyclePower` | 增益 |
| 力量恢复 | `RestoreStrengthPower` | 增益 |
| 纵卷发形态 | `RingletFormPower` | 增益 |
| 仪式准备 | `RitesPreparationPower` | 增益 |
| 封印 | `SealPower` | 减益 |
| 蓄势待发 | `SimmeringPower` | 增益 |
| 赖床 | `SleepInPower` | 减益 |
| 魔法精通 | `SpellCasterPower` | 增益 |
| 柔韧 | `SpiegelMalleablePower` | 增益 |
| 近战魔法使 | `StrikeUpPower` | 增益 |
| 被吞噬 | `SwallowedPower` | 减益 |
| 沼泽脱困 | `SwampManaNextTurnPower` | 增益 |
| 力量敏捷交换 | `SwitchStrengthAndDexterityPower` | 增益 |
| 节俭 | `SynchroWithoutManaPower` | 增益 |
| 雷电魔法 | `ThunderPower` | 增益 |
| 时间回溯 | `TimeTravelPower` | 增益 |
| 小伎俩 | `TrickPower` | 中性 |
| 咏唱削弱 | `WeakenedChantPower` | 减益 |

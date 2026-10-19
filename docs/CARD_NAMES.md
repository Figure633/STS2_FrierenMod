# 卡名登记

更新日期：2026-10-19。本次只登记和核对名称，不修改模组运行文本，功能版本仍为 0.5.2。

根目录 [卡名对照表.xlsx](../卡名对照表.xlsx) 是给作者查阅的表格，包含两个工作表：

- **二代卡名对照**：当前 130 个具体卡模型，包括基础牌、普通卡牌、临时与兼容牌、28 种因子、8 种魔具和选择预览。前三列为代码 ID、中英文名；之后保留二代当前名称、差异状态及一二代本地化 ID。
- **一代全部卡名**：一代中文和英文 `cards.json` 中全部 242 个名称键，包括菲伦、停用内容、临时牌、因子、魔具与未移植内容。名称键数量不代表有效卡牌池数量；有同名 Java 文件时附源码相对路径，未找到时明确标记。

中文、英文均按原文件读取，未重新翻译。一代同 ID 的名称优先作为查阅名称；二代现名另列，以免把移植时的改名误认为一代原名。四个无一代同 ID 的选择预览暂保留二代现名，并标记待确认；这不意味着要求它们恢复成一代的某张牌。

`BabySleeping` 的中文是 **婴儿般的睡眠**，英文是 **Sleeping Like a Baby**。一代与当前二代均使用这组名字。交流中未核实的名字直接使用代码 ID。

## 数据来源

- 一代中文：`../reference/FrierenMod_STS1/src/main/resources/FrierenModResources/localization/ZHS/cards.json`
- 一代英文：同目录 `ENG/cards.json`
- 一代类文件：`../reference/FrierenMod_STS1/src/main/java/FrierenMod/cards/`
- 二代中英文：`FrierenMod/localization/{zhs,eng}/cards.json`
- 二代具体模型：`Scripts/Frieren/Cards/` 下的 `public sealed class`

通过代码类名与一代本地化 ID 的末段匹配，仅忽略大小写与下划线；不使用名称相似度猜测对应关系。二代本地化 ID 必须确实存在于中英文本文件。原始空格、标点和英文拼写保留，差异按原文精确比较。

## 本次发现的中文名差异

以下七个模型已标记“需对齐”，尚未更改游戏文本。`Kiss` 原文含前导空格，表格和索引保留该空格。

| 代码 ID | 一代原名 | 二代当前名 |
|---|---|---|
| GrapeSpell | 把甜葡萄变成酸葡萄的魔法 | 让葡萄变酸的魔法 |
| Kiss | 飞吻(杀伤力极强!) | 飞吻 |
| PancakeSpell | 翻薄饼时会很顺利的魔法 | 顺利翻薄饼的魔法 |
| RedAppleSpell | 将红苹果变成青苹果的魔法 | 将红苹果变青的魔法 |
| SayThinkingSpell | 把思考的事情说出来的魔法 | 说出心声的魔法 |
| SecondDefensiveMagic | 防御魔法 | 防御魔法·追加 |
| TongueTwisterSpell | 说绕口令时绝对不会大舌头的魔法 | 流利说绕口令的魔法 |

英文有 19 个模型名称与一代不同，仅登记，按用户要求留待后期处理。此处仅核对名称，不代表卡牌描述或玩法已经完成对齐。

## 快速查询与更新

[card_names.jsonl](inventory/card_names.jsonl) 每行一个二代模型记录，可按代码 ID、中英文或本地化 ID 搜索，例如 `rg '"model":"BabySleeping"' docs/inventory/card_names.jsonl`。它与 Excel 来自同一份数据，不要求为了称呼一张牌而启动 Excel。

生成入口为 `tools/export_card_names.mjs`，仅导出表格和索引，不改本地化。使用 Codex 配套 Node 与 artifact-tool；将配套 `node_modules` 连接到忽略目录 `artifacts/card-names/node_modules` 后运行。可通过 `FRIEREN_STS1_SOURCE` 指定其他一代源码位置。生成后检查表格、索引和名称差异，再提交；作者若手改表格，应先核对修改并回写名称来源，不能直接重跑脚本覆盖作者修改。

验证包括二代具体模型与所有 `.title` 数量一致、ID 无重复、每个中英标题存在、`BabySleeping` 原名确认，以及导出 Excel 与提取记录逐格核对。两个工作表均提供筛选和冻结表头。

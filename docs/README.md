# 文档入口

当前需求和验收进度维护在根目录 [TODOs.docx](../TODOs.docx)，卡名与作者意见维护在 [卡名对照表.xlsx](../卡名对照表.xlsx)。运行版本、构建安装和历史改动见 [README](../README.md)，协作规则见 [AGENTS](../AGENTS.md)。

本次统计见 [卡池、卡图和卡背核对](CONTENT_AUDIT.md)。统计口径、各稀有度完整名单、卡图问题及尚未接入的新卡背集中在这一份文档，不另维护重复统计报告。

| 用途 | 文档 |
|---|---|
| 代码导航与角色边界 | [CODE_STRUCTURE.md](CODE_STRUCTURE.md)、[ARCHITECTURE_REFERENCES.md](ARCHITECTURE_REFERENCES.md) |
| 本地原版、BaseLib与一代源码 | [LOCAL_REFERENCES.md](LOCAL_REFERENCES.md) |
| 正式版／测试版兼容入口 | [VERSION_COMPATIBILITY.md](VERSION_COMPATIBILITY.md) |
| 委托规则与报酬 | [COMMISSIONS.md](COMMISSIONS.md) |
| 第三幕先古之民赛莉耶与升级圣徽 | [SERIE_ANCIENT.md](SERIE_ANCIENT.md) |
| 阿乌拉战斗 | [AURA_BOSS.md](AURA_BOSS.md) |
| 联机设计和验收范围 | [MULTIPLAYER.md](MULTIPLAYER.md) |
| 作者待决定的代理新增内容 | [AGENT_ADDITIONS_REVIEW.md](AGENT_ADDITIONS_REVIEW.md) |
| 代理补写及尚无逐句确认的文本 | [AGENT_TEXT_REVIEW.md](AGENT_TEXT_REVIEW.md)，附全部中英文本来源索引 |
| 卡图、事件图与卡背模板 | [CARD_ART_GUIDE.md](CARD_ART_GUIDE.md)、[EVENT_ART_GUIDE.md](EVENT_ART_GUIDE.md)、[SLOT_BACK_TEMPLATES.md](SLOT_BACK_TEMPLATES.md) |
| 二代新增卡槽背景的解锁条件 | [作者填写的Excel表](../outputs/slot-backs-final-20261109/二代新增卡槽背景-解锁条件填写表.xlsx)，23组文案已登记，22组已接入，圣雪结晶待作者新增事件 |
| 能力分类原则 | [POWER_TYPES.md](POWER_TYPES.md)，具体分类以当前模型的 Type 为准 |
| 资源来源与文件指纹 | [assets.json](assets.json)、[generated_assets.json](generated_assets.json) |
| 卡名检索 | [inventory/card_names.jsonl](inventory/card_names.jsonl)，作者工作簿优先 |
| 音乐清单 | [inventory/bgm_audio.json](inventory/bgm_audio.json) |
| 一代源码基线与原始目录 | [baseline.json](baseline.json)、[inventory/content.json](inventory/content.json)、[content_inventory.csv](content_inventory.csv) |
| 一代77张有效芙莉莲卡的对应关系 | [inventory/frieren_card_parity.json](inventory/frieren_card_parity.json)，由程序集测试读取 |

`inventory/` 中的一代原始清单是来源证据，不代表当前二代的完成进度。当前卡牌统计需先用实际游戏模型导出，再运行 `tools/audit_current_content.py`；卡图原生预览入口是 `--card-art-audit-only`。默认临时目录为 `artifacts/content-audit-current/`；自定义目录时，Python传入`--output`，引擎预览使用同值的`FRIEREN_CONTENT_AUDIT_DIR`。生成新截图后用`--native`核对透明区域是否露黑。

已移除的初期计划、旧交接、旧测试报告和2026年10月的重复统计可通过 Git 历史查阅。作者 Word／Excel、`assets/` 原图、外层参考源码和 `outputs/` 待采用卡背成品继续保留。源码快照的清单与校验值只在打包时重新生成，不在工作树保留过期副本。

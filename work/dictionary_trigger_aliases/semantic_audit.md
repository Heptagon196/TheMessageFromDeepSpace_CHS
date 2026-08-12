# 词典触发词—对白语义全量审计

审计对象：`patch/Translations/dictionary_trigger_aliases.json` 的全部 154 条规则。

审计方法：逐条连接游戏原始 AdvancedListener 条件、中文附加触发词、对白标题和对白全文；同时按运行时 matcher 复算同一编辑事件的交叉命中。术语判断遵循锁定词义、上下文优先和同一概念唯一译法原则。

## 结论摘要

- 阻断：33 条（稳定触发无关对白，或属于 16 组无门控双触发冲突）
- 错误：21 条（至少一个中文别名与对白正文/元语言笑点不一致）
- 风险：10 条（范围过宽、词性偏移或容易误触发）
- 通过：90 条
- 需要调整：64 / 154 条

说明：同一冲突组的两端都会标为“阻断”，所以阻断条数是受影响规则数，不是冲突组数。16 组冲突均未发现其它条件门控，实际运行时会同时满足。最长且唯一匹配机制可消除长短包含冲突；同长同词冲突仍需调整别名。

## 16 组规则交叉命中

| 规则 A | 规则 B | 可复现中文输入 |
|---|---|---|
| #36 `HUSBAND` | #37 `WIFE` | 配偶 |
| #50 `BIGGEST` | #51 `MOST` | 最大 |
| #55 `MOST` | #56 `SUPERLATIVE` | 最 / 最高级 |
| #77 `HELISEC` | #78 `HELIUM` | 氦秒 |
| #93 `VISUAL` | #94 `VOBJ` | 可视对象 |
| #99 `HUMANITY` | #100 `HUMANS` | 人类 |
| #106 `MESSAGE` | #107 `MSG` | 信息 / 消息 / 讯息 |
| #116 `THEN` | #117 `THEREFORE` | 所以 |
| #122 `FLIP` | #123 `NOT` | 取反 |
| #130 `SEVEN` | #151 `ANS` | 回答 |
| #134 `DECIMAL` | #135 `FLOAT` | 小数 |
| #137 `MULTIPLY` | #138 `X` | 乘 |
| #139 `ADD` | #140 `PLUS` | 加 / 加法 |
| #142 `AND` | #143 `WITH` | 与 / 和 |
| #147 `SKIP` | #148 `SPACE` | 间隔 |
| #153 `IDFK` | #154 `IDK` | md不知道 / tm不知道 / 妈不知道 |

## 全量逐条结果

| # | 结论 | 词条 ID | 通道 | 英文条件 | 中文规则 | 对白 | 发现与建议 |
|---:|---|---:|---|---|---|---|---|
| 1 | 通过 | -192 | EditEntryIDContains | `ROST` | contains:喙/鸟嘴 | 979《Rost--→Rost——》 | 中文别名与实际对白的核心语义一致。 |
| 2 | 阻断 | -188 | EditEntryIDToName | `POSITIVE` | exact:正/正数/正值/阳性 | 974《Landlubber→旱鸭子》 | `POSITIVE` 实际连到《Landlubber/旱鸭子》，中文输入“正”等会稳定触发完全无关对白。 建议：删除本条中文 rules；若要保留，先修正游戏数据中的对白引用。 |
| 3 | 通过 | -187 | EditEntryIDToName | `NEGATIVE` | exact:负/负数/负值/阴性 | 977《Negative?→负？》 | 中文别名与实际对白的核心语义一致。 |
| 4 | 通过 | -186 | EditEntryIDToName | `MUTUAL` | exact:相互/互相/双向 | 976《Mutual→相互》 | 中文别名与实际对白的核心语义一致。 |
| 5 | 通过 | -184 | EditEntryIDToName | `LANDLUBBER` | exact:旱鸭子 | 974《Landlubber→旱鸭子》 | 中文别名与实际对白的核心语义一致。 |
| 6 | 风险 | -184 | EditEntryIDContains | `TERR` | contains:陆/地 | 975《Terra--→Terra——》 | “陆/地”是过宽的 contains，容易让大量无关名称触发《Terra》。 建议：改成精确且独占的“泰拉/大地”等；不要使用单字 contains。 |
| 7 | 通过 | -177 | EditEntryIDToName | `COLDSIDE` | exact:冷面/寒冷面 | 972《Coldside→冷面》 | 中文别名与实际对白的核心语义一致。 |
| 8 | 通过 | -177 | EditEntryIDToName | `NIGHTSIDE` | exact:夜面/背阳面 | 971《Nightside→夜面》 | 中文别名与实际对白的核心语义一致。 |
| 9 | 通过 | -176 | EditEntryIDToName | `DAYSIDE` | exact:昼面/向阳面 | 970《Dayside→昼面》 | 中文别名与实际对白的核心语义一致。 |
| 10 | 通过 | -176 | EditEntryIDToName | `HOTSIDE` | exact:热面/炎热面 | 969《Hotside→热面》 | 中文别名与实际对白的核心语义一致。 |
| 11 | 通过 | -173 | EditEntryIDToName | `OCEAN` | exact:海洋/大海 | 968《Ocean→海洋》 | 中文别名与实际对白的核心语义一致。 |
| 12 | 通过 | -171 | EditEntryIDToName | `SIDE` | exact:侧/面/一边 | 967《Side→侧面》 | 中文别名与实际对白的核心语义一致。 |
| 13 | 通过 | -168 | EditEntryIDToName | `PREHISTORIC` | exact:史前/远古 | 964《Prehistoric→史前》 | 中文别名与实际对白的核心语义一致。 |
| 14 | 通过 | -164 | EditEntryIDToName | `TEMPERATURE` | exact:温度 | 963《Temperature→温度》 | 中文别名与实际对白的核心语义一致。 |
| 15 | 通过 | -163 | EditEntryIDToName | `FEEL` | exact:感觉/感受 | 961《Feel→感觉》 | 中文别名与实际对白的核心语义一致。 |
| 16 | 通过 | -163 | EditEntryIDToName | `OBSERVE` | exact:观察/观测 | 962《Observe→观察》 | 中文别名与实际对白的核心语义一致。 |
| 17 | 通过 | -158 | EditEntryIDToName | `PART` | exact:部分/部件/一部分 | 959《Part→部分》 | 中文别名与实际对白的核心语义一致。 |
| 18 | 通过 | -152 | EditEntryIDToName | `EMOTION` | exact:情绪/情感 | 952《Emotion→情感》 | 中文别名与实际对白的核心语义一致。 |
| 19 | 风险 | -151 | EditEntryIDContains | `OLD` | contains:老/年长/老年 | 950《Old→老》 | 单字 contains“老”会把“老师/老板”等无关名称也判为 OLD。 建议：改为 exact，或只保留“年老/老年/年长”。 |
| 20 | 错误 | -150 | EditEntryIDContains | `MIDDLEAGE` | contains:中年/成年 | 949《Middleage→中年》 | “成年”只表示已成年，不等于“中年”，会触发《Middleage》中年对白。 建议：删除“成年”，保留“中年/中年期”。 |
| 21 | 通过 | -149 | EditEntryIDContains | `YOUNG` | contains:年轻/幼年/幼体 | 951《Young→年轻》 | 中文别名与实际对白的核心语义一致。 |
| 22 | 风险 | -147 | EditEntryIDContains | `BRAIN` | contains:脑/大脑 | 947《Brain--→大脑——》 | 单字 contains“脑”会命中“电脑”等并触发《Brain》对白。 建议：优先 exact“大脑/脑”；若必须 contains，增加边界或排除逻辑。 |
| 23 | 通过 | -147 | EditEntryIDContains | `CORE` | contains:核心/中枢 | 948《Core--→核心——》 | 中文别名与实际对白的核心语义一致。 |
| 24 | 通过 | -146 | EditEntryIDContains | `ARM` | contains:手臂/胳膊 | 945《Arm--→手臂——》 | 中文别名与实际对白的核心语义一致。 |
| 25 | 通过 | -146 | EditEntryIDContains | `LEG` | contains:腿 | 946《Leg--→腿——》 | 中文别名与实际对白的核心语义一致。 |
| 26 | 通过 | -146 | EditEntryIDContains | `LIMB` | contains:肢体/四肢 | 944《Limb--→肢体——》 | 中文别名与实际对白的核心语义一致。 |
| 27 | 通过 | -144 | EditEntryIDToName | `SEX` | exact:性/性行为/交配 | 943《-___-→》 | 中文别名与实际对白的核心语义一致。 |
| 28 | 通过 | -141 | EditEntryIDContains | `KEPLER` | contains:开普勒 | 942《Kepler→开普勒》 | 中文别名与实际对白的核心语义一致。 |
| 29 | 通过 | -141 | EditEntryIDContains | `SUN` | contains:太阳 | 941《Sun--→太阳——》 | 中文别名与实际对白的核心语义一致。 |
| 30 | 通过 | -140 | EditEntryIDToName | `ERID` | exact:埃里德 | 940《Erid→埃里德》 | 中文别名与实际对白的核心语义一致。 |
| 31 | 错误 | -140 | EditEntryIDToName | `SHEEN` | exact:希恩 | 939《Sheen→光泽》 | `SHEEN` 在该对白中是普通名词“光泽”，不是人名音译“希恩”。 建议：改为“光泽/亮泽/光彩”。 |
| 32 | 通过 | -138 | EditEntryIDToName | `ME` | exact:我/自己 | 936《Me→我》 | 中文别名与实际对白的核心语义一致。 |
| 33 | 通过 | -137 | EditEntryIDToName | `ALAN` | exact:艾伦/埃克斯/艾伦·埃克斯 | 933《Alan→艾伦》 | 中文别名与实际对白的核心语义一致。 |
| 34 | 通过 | -137 | EditEntryIDToName | `BAUTISTA` | exact:巴蒂斯塔/布莱恩/布莱恩·巴蒂斯塔 | 934《Bautista→巴蒂斯塔》 | 中文别名与实际对白的核心语义一致。 |
| 35 | 通过 | -137 | EditEntryIDToName | `COLLINS` | exact:柯林斯/凯莉/凯莉·柯林斯 | 935《Collins→柯林斯》 | 中文别名与实际对白的核心语义一致。 |
| 36 | 阻断 | -137 | EditEntryIDContains | `HUSBAND` | contains:丈夫/老公/先生/配偶 | 932《Husband--→丈夫——》 | “配偶”与 `WIFE` 共用，必然同时触发丈夫和妻子对白；“先生”还可能表示称谓。 建议：删除“配偶/先生”，只留“丈夫/老公”。 |
| 37 | 阻断 | -137 | EditEntryIDContains | `WIFE` | contains:妻子/老婆/太太/配偶 | 986《Wife--→妻子——》 | “配偶”与 `HUSBAND` 共用，必然同时触发丈夫和妻子对白。 建议：删除“配偶”，只留“妻子/老婆/太太”。 |
| 38 | 通过 | -127 | EditEntryIDToName | `DESIRE` | exact:愿望/渴望 | 927《Desire→愿望》 | 中文别名与实际对白的核心语义一致。 |
| 39 | 通过 | -127 | EditEntryIDToName | `GOAL` | exact:目标/目的 | 928《Goal→目标》 | 中文别名与实际对白的核心语义一致。 |
| 40 | 通过 | -127 | EditEntryIDToName | `HOPE` | exact:希望 | 925《Hope→希望》 | 中文别名与实际对白的核心语义一致。 |
| 41 | 通过 | -127 | EditEntryIDToName | `WANT` | exact:想要/想 | 926《Want→想要》 | 中文别名与实际对白的核心语义一致。 |
| 42 | 通过 | -122 | EditEntryIDToName | `THEN` | exact:然后/接着/下一步 | 923《Then→然后》 | 中文别名与实际对白的核心语义一致。 |
| 43 | 风险 | -121 | EditEntryIDContains | `TIME` | contains:时间/时刻/在……时 | 922《Time--→时间——》 | “在……时”包含字面省略号，几乎不是玩家会输入的实际名称。 建议：删除该值，保留“时间/时刻”；需要句式匹配时另做规范化。 |
| 44 | 通过 | -117 | EditEntryIDToName | `ALMOST` | exact:几乎/差不多 | 918《Almost→几乎》 | 中文别名与实际对白的核心语义一致。 |
| 45 | 通过 | -117 | EditEntryIDToName | `CLOSE` | exact:接近/相近 | 919《Close→接近》 | 中文别名与实际对白的核心语义一致。 |
| 46 | 通过 | -116 | EditEntryIDToName | `PORTION` | exact:少部分/一小部分 | 917《Portion→部分》 | 中文别名与实际对白的核心语义一致。 |
| 47 | 通过 | -115 | EditEntryIDToName | `LIONSHARE` | exact:大多数/大头/绝大部分 | 916《Lionshare→大部分》 | 中文别名与实际对白的核心语义一致。 |
| 48 | 通过 | -114 | EditEntryIDToName | `CENTER` | exact:中心/中央/中间 | 915《Center→中心》 | 中文别名与实际对白的核心语义一致。 |
| 49 | 通过 | -114 | EditEntryIDToName | `MEAN` | exact:平均/均值/平均数 | 913《Mean→平均数》 | 中文别名与实际对白的核心语义一致。 |
| 50 | 阻断 | -113 | EditEntryIDToName | `BIGGEST` | exact:最大/最大的 | 911《Biggest→最大》 | “最大”与同词条 `MOST` 共用，会同时触发《Biggest》和《Most》。 建议：本条保留“最大/最大的”，从 `MOST` 删除“最大”。 |
| 51 | 阻断 | -113 | EditEntryIDToName | `MOST` | exact:最多/最/最大 | 912《Most→最多》 | “最大”与 `BIGGEST` 共用，造成双触发。 建议：删除“最大”，保留“最多”；“最”是否保留取决于该词条的预期词性。 |
| 52 | 风险 | -112 | EditEntryIDToName | `LEAST` | exact:最少/最低 | 910《Least→最少》 | “最低”偏向数值下界，不总是 `least` 的“最少”。 建议：若该词条只表达数量，删除“最低”；否则保留并在说明中限定。 |
| 53 | 通过 | -112 | EditEntryIDToName | `SMALLEST` | exact:最小/最小的 | 909《Smallest→最小》 | 中文别名与实际对白的核心语义一致。 |
| 54 | 错误 | -111 | EditEntryIDToName | `ZERO` | exact:零/没有/空 | 908《Zero→零》 | 对白明确讨论数值 0；“没有/空”并不等于零，会触发不相称对白。 建议：只保留“零”，可补“〇”；删除“没有/空”。 |
| 55 | 阻断 | -106 | EditEntryIDToName | `MOST` | exact:最/最高级/最最 | 243《Most→最》 | “最”和“最高级”均与 `SUPERLATIVE` 共用，一次输入触发两段语义不同的对白。 建议：本条只保留副词“最”；删除“最高级/最最”。 |
| 56 | 阻断 | -106 | EditEntryIDToName | `SUPERLATIVE` | exact:最高级/最 | 244《Superlative→最高级》 | 与 `MOST` 共用“最/最高级”；而对白谈的是美式毕业评选 superlatives，不是单纯语法最高级。 建议：使用独占的“毕业评选/班级之最/之最”，并与对白译文统一。 |
| 57 | 通过 | -103 | EditEntryIDToName | `HYCEAN` | exact:海氢/海氢行星 | 904《Hycean→海氢行星》 | 中文别名与实际对白的核心语义一致。 |
| 58 | 风险 | -100 | EditEntryIDToName | `BE` | exact:是/为 | 931《Be→存在》 | “是/为”是系词义，现译对白采用存在义“存在”；两种 `be` 语义未完全对齐。 建议：锁定为“存在”，或把对白统一改回系词语义后再接受“是/为”。 |
| 59 | 风险 | -99 | EditEntryIDContains | `INGROUP` | contains:组内/集合内 | 254《Ingroup→同群》 | “组内/集合内”是位置或成员关系，现译对白使用二元关系“同群”，词性不一致。 建议：改为“同群/同组/同一组”；若保留集合义，应同步调整对白。 |
| 60 | 通过 | -99 | EditEntryIDContains | `SUBSET` | contains:子集 | 255《Subset→子集》 | 中文别名与实际对白的核心语义一致。 |
| 61 | 通过 | -96 | EditEntryIDToName | `NUETRONSTAR` | exact:种子星 | 1005《Nuetronstar→中子星》 / 204《Nuetronstar→Nuetronstar（中子星拼错）》 | 通过（“种子星”是为拼错 neutron star 设计的本地化错字笑点）。 |
| 62 | 通过 | -95 | EditEntryIDToName | `WHITEDWARF` | exact:白矮星 | 1008《Whitedwarf→白矮星》 / 207《Whitedwarf→白矮星》 | 中文别名与实际对白的核心语义一致。 |
| 63 | 通过 | -92 | EditEntryIDToName | `ROTATE` | exact:旋转/转动 | 902《Rotate→旋转》 | 中文别名与实际对白的核心语义一致。 |
| 64 | 通过 | -88 | EditEntryIDToName | `DECOMPOSE` | exact:分解/拆解 | 197《Decompose→分解》 | 中文别名与实际对白的核心语义一致。 |
| 65 | 通过 | -88 | EditEntryIDToName | `DESTROY` | exact:摧毁/破坏/毁掉 | 198《Destroy→摧毁》 | 中文别名与实际对白的核心语义一致。 |
| 66 | 错误 | -86 | EditEntryIDContains | `PREP` | contains:介词 | 900《Prep--→介——》 | 对白把 `prep` 解释为 preposition 的缩写；输入完整“介词”后仍谈“简称”，元语言笑点不成立。 建议：使用独占的中文缩写并同步对白，或不提供中文别名。 |
| 67 | 通过 | -86 | EditEntryIDToName | `TO` | exact:到/向/朝 | 901《To→向》 | 中文别名与实际对白的核心语义一致。 |
| 68 | 通过 | -85 | EditEntryIDToName | `ACTION` | exact:动作/行动 | 899《Action→动作》 | 中文别名与实际对白的核心语义一致。 |
| 69 | 通过 | -82 | EditEntryIDToName | `CONJUNCTION` | exact:连词 | 212《Conjunction→连词》 | 中文别名与实际对白的核心语义一致。 |
| 70 | 通过 | -82 | EditEntryIDToName | `PREPOSITION` | exact:介词 | 213《Preposition→介词》 | 中文别名与实际对白的核心语义一致。 |
| 71 | 风险 | -77 | EditEntryIDToName | `ALTERNATIVE` | exact:或者/另一种/备选 | 209《Alternative→可选项》 | “或者”符合连接符语义；“另一种/备选”偏名词，而对白展示的是 A/B/C 之间的操作符。 建议：只保留“或者/或”，把名词义留给 `OPTION`。 |
| 72 | 通过 | -77 | EditEntryIDToName | `OPTION` | exact:选项/选择 | 210《Option→选项》 | 中文别名与实际对白的核心语义一致。 |
| 73 | 通过 | -74 | EditEntryIDToName | `WEIGHT` | exact:重量 | 227《Weight→重量》 | 中文别名与实际对白的核心语义一致。 |
| 74 | 通过 | -73 | EditEntryIDToName | `DISTANCEUNIT` | exact:距离单位/长度单位 | 229《Another Distance Unit→另一个距离单位》 | 中文别名与实际对白的核心语义一致。 |
| 75 | 通过 | -72 | EditEntryIDToName | `DISTANCEUNIT` | exact:距离单位/长度单位 | 222《Distance Unit→距离单位》 | 中文别名与实际对白的核心语义一致。 |
| 76 | 通过 | -72 | EditEntryIDToName | `METER` | exact:米/公尺 | 223《Meter→米》 | 中文别名与实际对白的核心语义一致。 |
| 77 | 阻断 | -69 | EditEntryIDToName | `HELISEC` | exact:氦秒/埃克斯秒/外星秒 | 219《Helisec→氦秒》 | “氦秒”会被 `HELIUM` 的 contains“氦”再次命中；“埃克斯秒/外星秒”也不是 `helisec` 的同义词。 建议：只保留“氦秒”，并把 `HELIUM` 中文规则改为 exact。 |
| 78 | 阻断 | -69 | EditEntryIDContains | `HELIUM` | contains:氦 | 218《Helium→氦》 | contains“氦”会吞掉“氦秒”，稳定同时触发《Helium》和《Helisec》。 建议：中文别名使用 exact“氦/氦气”，不要沿用英文条件的 contains 模式。 |
| 79 | 通过 | -69 | EditEntryIDToName | `SECOND` | exact:秒 | 220《Second→秒》 | 中文别名与实际对白的核心语义一致。 |
| 80 | 通过 | -69 | EditEntryIDToName | `TIMEUNIT` | exact:时间单位 | 221《Timeunit→时间单位》 | 中文别名与实际对白的核心语义一致。 |
| 81 | 通过 | -68 | EditEntryIDToName | `UNIT` | exact:单位/计量单位 | 234《Unit→单位》 | 中文别名与实际对白的核心语义一致。 |
| 82 | 通过 | -67 | EditEntryIDToName | `LIGHTSPEED` | exact:光速 | 224《Light Speed→光速》 | 中文别名与实际对白的核心语义一致。 |
| 83 | 错误 | -65 | EditEntryIDToName | `TIME` | exact:时间/变化参数 | 232《Time→时间》 | 对白命名并讨论“时间”；“变化参数”只是对时间作用的解释，不是同义名称。 建议：删除“变化参数”，保留“时间/时刻”。 |
| 84 | 通过 | -61 | EditEntryIDToName | `NUETRON` | exact:种子 | 192《Nuetron→Nuetron（中子拼错）》 | 通过（“种子”承接 nuetron/neutron 拼错笑点）。 |
| 85 | 错误 | -58 | EditEntryIDToName | `CHEMREACT` | exact:化学反应 | 183《Chemreact→化学反应缩写》 | 对白笑点在 `Chemreact` 是压缩造词；输入完整“化学反应”后仍称其为混成词，语义断裂。 建议：设计中文缩写并同步对白，或不提供中文别名。 |
| 86 | 通过 | -58 | EditEntryIDToName | `REACT` | exact:反应 | 184《React→反应》 | 中文别名与实际对白的核心语义一致。 |
| 87 | 通过 | -56 | EditEntryIDToName | `ELEMENT` | exact:元素 | 182《Element?→元素？》 | 中文别名与实际对白的核心语义一致。 |
| 88 | 通过 | -55 | EditEntryIDToName | `ATOM` | exact:原子 | 189《Atom→原子》 | 中文别名与实际对白的核心语义一致。 |
| 89 | 错误 | -53 | EditEntryIDToName | `VIZ` | exact:图/图像/可视化 | 894《Viz→Viz》 | 对白反复讨论 `viz` 这个缩写；输入“图/图像/可视化”后，缩写笑点和指代都不成立。 建议：不给本条中文别名，或设计中文缩写并重译对白。 |
| 90 | 通过 | -52 | EditEntryIDToName | `BALL` | exact:球/球体 | 889《Ball→球》 | 中文别名与实际对白的核心语义一致。 |
| 91 | 通过 | -52 | EditEntryIDToName | `DOT` | exact:点/圆点 | 892《Dot→点》 | 中文别名与实际对白的核心语义一致。 |
| 92 | 通过 | -52 | EditEntryIDToName | `PIXEL` | exact:像素 | 893《Pixel→像素》 | 中文别名与实际对白的核心语义一致。 |
| 93 | 阻断 | -52 | EditEntryIDToName | `VISUAL` | exact:图像/视觉对象/可视对象 | 891《Visual→视觉》 | “可视对象”与 `VOBJ` 共用，会同时触发《Visual》和《Vobj》。 建议：本条保留“视觉/视觉单位”；删除“可视对象”。 |
| 94 | 阻断 | -52 | EditEntryIDToName | `VOBJ` | exact:可视对象 | 890《Vobj→VOBJ》 | “可视对象”与 `VISUAL` 共用；且对白明确解释缩写 VOBJ，完整中文名称无法承接缩写笑点。 建议：不提供中文别名，或设计中文缩写并同步对白。 |
| 95 | 错误 | -51 | EditEntryIDToName | `Z` | exact:高度/垂直坐标/Z 坐标 | 888《Z→》 | 对白强调玩家输入的是单字符 `Z`；“高度/垂直坐标/Z 坐标”都会让“才一个字符”的对白失真。 建议：只依赖原版大小写兼容，不添加概念型中文别名。 |
| 96 | 错误 | -50 | EditEntryIDToName | `Y` | exact:深度/纵坐标/Y 坐标 | 887《Y→》 | 对白明确复述玩家输入单字符 `Y`；中文概念名会与对白事实冲突。 建议：只保留原版 Y/y 触发。 |
| 97 | 错误 | -49 | EditEntryIDToName | `X` | exact:宽度/横坐标/X 坐标 | 886《X→》 | 对白围绕单字符 `X` 展开；“宽度/横坐标/X 坐标”不是同一次输入。 建议：只保留原版 X/x 触发。 |
| 98 | 通过 | -46 | EditEntryIDToName | `EARTH` | exact:地球 | 167《Earth→地球》 | 中文别名与实际对白的核心语义一致。 |
| 99 | 阻断 | -46 | EditEntryIDToName | `HUMANITY` | exact:人类 | 168《Humanity→人类》 | “人类”与 `HUMANS` 共用，没有其它门控，会同时触发两段对白。 建议：用“人类文明/全人类”区分 HUMANITY，或只给其中一条中文别名。 |
| 100 | 阻断 | -46 | EditEntryIDToName | `HUMANS` | exact:人类/人 | 169《Humans→人类》 | “人类”与 `HUMANITY` 共用，必然双触发。 建议：本条保留“人类/人”，让 HUMANITY 使用独占译法。 |
| 101 | 通过 | -45 | EditEntryIDToName | `ALIENS` | exact:外星人/异星人 | 166《Aliens→外星人》 | 中文别名与实际对白的核心语义一致。 |
| 102 | 通过 | -44 | EditEntryIDToName | `COMPUTER` | exact:电脑/计算机 | 175《Computer?→电脑？》 | 中文别名与实际对白的核心语义一致。 |
| 103 | 错误 | -44 | EditEntryIDFromName | `METEOR` | exact:陨石/流星 | 174《Meteorite→陨石》 | 条件表示从错误名称 METEOR 改走；“陨石”本来就是对白认可的 meteorite，纳入后会错误庆祝纠正。 建议：仅保留“流星”。 |
| 104 | 错误 | -44 | EditEntryIDToName | `METEOR` | exact:陨石/流星 | 176《Meteor→流星》 | 对白会纠正“这是陨石，不是流星”；若玩家输入别名“陨石”，纠正内容与输入相反。 建议：仅保留“流星”。 |
| 105 | 通过 | -43 | EditEntryIDToName | `MASSAGE` | exact:按摩 | 170《The medium is the massage→媒介即按摩》 | 通过（“按摩”承接 message/massage 与 McLuhan 双关）。 |
| 106 | 阻断 | -43 | EditEntryIDToName | `MESSAGE` | exact:讯息/消息/信息 | 171《Message→讯息》 | “讯息/消息/信息”与 `MSG` 完全共用，会同时触发完整词和缩写两段对白。 建议：这些中文全称只归 `MESSAGE`。 |
| 107 | 阻断 | -43 | EditEntryIDToName | `MSG` | exact:讯息/消息/信息 | 172《Msg→Msg（讯息）》 | 与 `MESSAGE` 完全共用别名；对白又依赖 MSG 的字符数笑点，中文全称无法承接。 建议：不提供中文别名，或另造独占缩写并同步对白。 |
| 108 | 错误 | -42 | EditEntryIDToName | `F` | exact:频率 | 177《F for freq?→F 是频率的缩写？》 | 对白强调单字符 F 且角色认为 F 没意义；输入“频率”后对白事实相反。 建议：只保留原版 F/f 触发。 |
| 109 | 通过 | -42 | EditEntryIDToName | `SIGNAL` | exact:信号 | 180《Signal→信号》 | 中文别名与实际对白的核心语义一致。 |
| 110 | 通过 | -41 | EditEntryIDToName | `FROM` | exact:从/来自 | 164《From!→从！》 | 中文别名与实际对白的核心语义一致。 |
| 111 | 错误 | -41 | EditEntryIDToName | `TO` | exact:到/至/终点 | 158《To!→到！》 / 165《To→到》 | “终点”是名词，对白讨论介词/后置词“到”，语义和词性不符。 建议：删除“终点”，保留“到/至”。 |
| 112 | 错误 | -40 | EditEntryIDToName | `FROM` | exact:从/来自/起点 | 157《From→从》 / 156《Source -> From→“来源”改为“从”》 | “起点”不是介词 FROM；对白还明确争论“从”和“来源”。 建议：删除“起点”；保留“从”，谨慎保留“来自”。 |
| 113 | 错误 | -40 | EditEntryIDFromName | `FROM` | exact:从/来自/起点 | 155《From -> Source→“从”改为“来源”》 | 该条件检测旧名称 FROM；把旧名“起点”也视为“从”，会触发并不成立的“从改来源”式反应。 建议：只保留“从”，或限定与新名称的组合条件。 |
| 114 | 错误 | -39 | EditEntryIDToName | `ADDCOORDS` | exact:平移/移动/偏移 | 161《Addcoords→坐标相加》 | 对白明确把 `Addcoords` 展开为“坐标相加”；“平移/移动/偏移”是可能结果，不是名称同义词。 建议：改为“坐标相加/加坐标”；若保留平移，应重写对白。 |
| 115 | 错误 | -38 | EditEntryIDToName | `MAKE` | exact:制造/制作/生成/构造 | 159《Make→使》 | 对白是 `Make(s) sense` 双关，现译为“说得通”；“制造/制作/生成/构造”均接不上。 建议：使用“使/让”并重新评估双关，或不提供中文别名。 |
| 116 | 阻断 | -36 | EditEntryIDToName | `THEN` | exact:所以/那么/于是 | 153《Then→那么》 | “所以”与 `THEREFORE` 共用；且本对白采用“那么”，不是因果连接词的唯一用法。 建议：本条使用“那么/然后/接着”，删除“所以”。 |
| 117 | 阻断 | -36 | EditEntryIDToName | `THEREFORE` | exact:所以/因此 | 154《Therefore→因此》 | “所以”与 `THEN` 共用，会同时触发《Then》和《Therefore》。 建议：保留“因此/所以”，让 THEN 使用独占译法。 |
| 118 | 通过 | -33 | EditEntryIDToName | `LESSER` | exact:小于/较小 | 149《Lesser→小于》 | 中文别名与实际对白的核心语义一致。 |
| 119 | 通过 | -33 | EditEntryIDToName | `NOTEQUAL` | exact:不等于/不相等 | 150《Not equal??→不等于？？》 | 中文别名与实际对白的核心语义一致。 |
| 120 | 通过 | -32 | EditEntryIDToName | `GREATER` | exact:大于/较大 | 146《Greater→大于》 | 中文别名与实际对白的核心语义一致。 |
| 121 | 风险 | -31 | EditEntryIDToName | `\|` | exact:或/或者/｜ | 142《\|→》 | 原对白赞叹符号 `\|` 很简洁；输入“或者”后这句评价弱化。 建议：保留一字符“或/｜”，删除“或者”或接受轻微笑点损失。 |
| 122 | 阻断 | -29 | EditEntryIDToName | `FLIP` | exact:反转/翻转/取反 | 139《Flip→取反》 | “取反”与 `NOT` 共用，会同时触发位翻转和逻辑非对白。 建议：本条使用“翻转/位翻转”，删除“取反”。 |
| 123 | 阻断 | -29 | EditEntryIDToName | `NOT` | exact:非/不是/取反 | 140《Not→非》 | “取反”与 `FLIP` 共用，造成双触发。 建议：本条使用“非/逻辑非/不是”，删除“取反”。 |
| 124 | 通过 | -28 | EditEntryIDToName | `INCORRECT` | exact:错误/不正确 | 131《Incorrect!→错误！》 | 中文别名与实际对白的核心语义一致。 |
| 125 | 通过 | -28 | EditEntryIDToName | `TRUE` | exact:真/正确 | 132《True?→真？》 | 中文别名与实际对白的核心语义一致。 |
| 126 | 通过 | -27 | EditEntryIDToName | `CORRECT` | exact:正确/对 | 133《Correct!→正确！》 | 中文别名与实际对白的核心语义一致。 |
| 127 | 通过 | -27 | EditEntryIDToName | `FALSE` | exact:假/错误/错 | 134《False?→假？》 | 中文别名与实际对白的核心语义一致。 |
| 128 | 通过 | -26 | EditEntryIDToName | `PROPOSITION` | exact:命题 | 129《Proposition→命题》 | 中文别名与实际对白的核心语义一致。 |
| 129 | 通过 | -15 | EditEntryIDToName | `(` | exact:（ | 120《Oops! Open, not closed!→弄反了！是左括号！》 | 中文别名与实际对白的核心语义一致。 |
| 130 | 阻断 | -12 | EditEntryIDToName | `SEVEN` | exact:七/填空/回答 | 93《Signal -12 is just 7??→信号 -12 就是 7？？》 | `SEVEN` 对白明确确认数字 7；“填空/回答”不是七，而且“回答”还会同时命中全局 `ANS`。 建议：只保留“七”，可补“7/柒”。 |
| 131 | 通过 | -11 | EditEntryIDToName | `SEVEN` | exact:七 | 93《Signal -12 is just 7??→信号 -12 就是 7？？》 | 中文别名与实际对白的核心语义一致。 |
| 132 | 通过 | -11 | EditEntryIDToName | `VALUE` | exact:值/数值 | 115《Value→值》 | 中文别名与实际对白的核心语义一致。 |
| 133 | 错误 | -11 | EditEntryIDToName | `VAR` | exact:变量 | 116《Var→变量》 | 对白解释 `Var` 是 variable 的英文缩写；输入完整“变量”后仍出现 Var，指代不一致。 建议：不提供中文别名，或设计独占缩写并同步对白。 |
| 134 | 阻断 | -10 | EditEntryIDToName | `DECIMAL` | exact:小数/十进制 | 96《Decimal→小数》 | “小数”与 `FLOAT` 共用，会同时触发 Decimal 与 Float 对白。 建议：本条只保留“十进制/小数点”（需结合目标词义），不要与 FLOAT 共用“小数”。 |
| 135 | 阻断 | -10 | EditEntryIDToName | `FLOAT` | exact:浮点/浮点数/小数 | 100《Float→浮点数》 | “小数”与 `DECIMAL` 共用；技术上“小数”也不等于浮点数。 建议：只保留“浮点/浮点数”。 |
| 136 | 通过 | -10 | EditEntryIDToName | `OCTAL` | exact:八进制 | 102《Octal→八进制》 | 中文别名与实际对白的核心语义一致。 |
| 137 | 阻断 | -6 | EditEntryIDToName | `MULTIPLY` | exact:乘/乘法 | 101《Multiply→乘》 | “乘”与符号条件 `X` 共用，会同时触发《Multiply》和《X to multiply》。 建议：本条保留“乘/乘法”，让 X 只接受符号。 |
| 138 | 阻断 | -6 | EditEntryIDToName | `X` | exact:乘/乘号/× | 117《X to multiply→用 X 表示乘法》 | “乘”与 `MULTIPLY` 共用；对白又明确评价单字符 X。 建议：只保留独占符号“×”（若游戏输入允许），否则不加中文别名。 |
| 139 | 阻断 | -5 | EditEntryIDToName | `ADD` | exact:加/加法 | 90《Add→加》 | “加/加法”与 `PLUS` 共用，会同时触发两段对白。 建议：本条保留“加/相加”，PLUS 使用“加上/加号”。 |
| 140 | 阻断 | -5 | EditEntryIDToName | `PLUS` | exact:加/加号/加法 | 105《Plus→加上》 | “加/加法”与 `ADD` 共用，造成双触发。 建议：删除“加/加法”，保留独占的“加上/加号/正号”。 |
| 141 | 通过 | -4 | EditEntryIDToName | `EQUALS` | exact:等于/相等 | 99《Equals→等于》 | 中文别名与实际对白的核心语义一致。 |
| 142 | 阻断 | -3 | EditEntryIDToName | `AND` | exact:和/与/并且 | 881《And→与》 | “和/与”与 `WITH` 共用，会同时触发并列连词和伴随关系对白。 建议：本条使用“且/并且”，避免“和/与”。 |
| 143 | 阻断 | -3 | EditEntryIDToName | `WITH` | exact:和/与/一起 | 882《With→与……一起》 | “和/与”与 `AND` 共用，造成双触发。 建议：本条使用“一起/与……一起/伴随”。 |
| 144 | 通过 | -2 | EditEntryIDToName | `APPLE` | exact:苹果 | 113《APPLE→苹果》 | 通过（玩家把信号误命名为“苹果”，对白正是在否定这个猜测）。 |
| 145 | 错误 | -2 | EditEntryIDToName | `ENDNUM` | exact:末数/末尾数字 | 97《Endnum→数字结束》 | “末数/末尾数字”表示最后一个数字；`Endnum` 对白表示数字序列结束标记。 建议：改为“数字结束/数终/结束数字”。 |
| 146 | 风险 | -2 | EditEntryIDToName | `PLUSONE` | exact:加一/加上一个 | 91《Signal -2 as Add One→把信号 -2 记作“加一”》 | “加上一个”可理解为增加一个对象，不一定是数值 +1。 建议：保留“加一”，把“加上一个”改为“加上 1/递增一”。 |
| 147 | 阻断 | -2 | EditEntryIDToName | `SKIP` | exact:跳过/间隔 | 111《Skip→跳过》 | “间隔”与 `SPACE` 共用，会同时触发跳过和空间/空格对白。 建议：只保留“跳过/略过”。 |
| 148 | 阻断 | -2 | EditEntryIDToName | `SPACE` | exact:空格/间隔 | 114《Space→空间》 | “间隔”与 `SKIP` 共用；对白同时覆盖外太空与排版空格，并不讨论间隔动作。 建议：删除“间隔”，使用“空间/空格”。 |
| 149 | 通过 | -2 | EditEntryIDToName | `_` | exact:＿/下划线 | 112《Underscore→下划线》 | 中文别名与实际对白的核心语义一致。 |
| 150 | 通过 | None | EditEntryToName | `?` | exact:？ | 110《?→》 | 中文别名与实际对白的核心语义一致。 |
| 151 | 阻断 | None | EditEntryToName | `ANS` | exact:答案/回答 | 94《Ans→答案》 | 对白在解释缩写 `Ans`；输入完整“答案/回答”后缩写问答失去指代，“回答”还会同时命中词条 -12 的 `SEVEN`。 建议：不提供中文别名，或使用独占中文缩写并同步对白。 |
| 152 | 错误 | None | EditEntryToName | `ASDF` | exact:乱打的/随便打的/键盘乱敲 | 80《ASDF→ASDF》 / 81《ASDF 2→ASDF 2》 | 对白要求玩家实际按下 ASDF 键位；“乱打的/随便打的/键盘乱敲”是解释，不是那串输入。 建议：保留原版 ASDF，或设计中文键盘乱按串并重写对白。 |
| 153 | 阻断 | None | EditEntryToName | `IDFK` | contains:md不知道/tm不知道；contains_all:妈/不知道 | 86《IDFK→IDFK》 / 85《Alan FK→艾伦 FK》 | 所有中文 IDFK 变体都包含“不知道”，会同时命中全局 `IDK` contains 规则。 建议：把 IDK 改为 exact；IDFK 也优先使用 exact 的完整变体。 |
| 154 | 阻断 | None | EditEntryToName | `IDK` | contains:不知道 | 87《IDK→IDK》 / 88《IDK is temp?→“IDK”是暂定名？》 | contains“不知道”会吞掉所有 IDFK 中文变体，一次输入触发 IDK 与 IDFK 两组对白。 建议：改为 exact“不知道”，不要使用全局 contains。 |

## 判定边界

- 原英文触发仍有效；本报告只判断新增中文别名是否安全，不把原版已有的大小写兼容视为问题。
- 缩写、字符数、键盘位置和拼写错误属于对白语义的一部分，不能只翻译缩写展开后的概念。
- `contains` 按无边界子串匹配，因此“脑”会命中“电脑”，“氦”会命中“氦秒”；这类不是理论风险，而是当前 matcher 的实际行为。
- 不同词条 ID 的同译词不会在一次 ID 定向编辑中互相冲突；报告只把同一编辑候选可同时满足的规则列为双触发。

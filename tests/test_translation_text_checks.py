from __future__ import annotations

import json
import sys
from pathlib import Path


PROJECT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(PROJECT / "tools"))

from translation_text_checks import (
    validate_chinese_quotes,
    validate_dialogue_ellipsis,
    validate_duplicate_punctuation,
    validate_dictionary_slogan,
    validate_garbled_subtitle,
    validate_locked_glossary_terms,
    validate_natural_chinese_punctuation,
)


assert validate_chinese_quotes("如果全都‘超出范围’，那什么才算专业？"), (
    "中文正文的一级引用不得使用单引号"
)
assert validate_chinese_quotes("他说：'不行'。"), "半角引号不得混入中文正文"
assert not validate_chinese_quotes("如果全都“超出范围”，那什么才算专业？")
assert not validate_chinese_quotes("“它就‘bon’地响，所以叫‘bon-go’！”"), (
    "双引号内的二级引用应允许使用单引号"
)
assert not validate_chinese_quotes("O'Brien 的名字保留英文撇号。"), (
    "英文词内部的撇号不是中文引号"
)
assert validate_chinese_quotes("“没有闭合。"), "未闭合的中文引号必须报错"
assert validate_chinese_quotes("错误的”开闭顺序“。"), "顺序错误的中文引号必须报错"

assert validate_dialogue_ellipsis("等等..."), "ASCII 三点省略号必须报错"
assert validate_dialogue_ellipsis("等等....."), "任意三个及以上连续半角句点必须报错"
assert validate_dialogue_ellipsis("等等…"), "单个 U+2026 不是规范的中文省略号"
assert not validate_dialogue_ellipsis("等等……"), "成对中文省略号应通过校验"
assert not validate_dialogue_ellipsis("版本 1.2.3。"), "普通版本号不应被误判为省略号"

assert validate_natural_chinese_punctuation("外星克."), (
    "紧邻中文的半角句号必须报错"
)
assert validate_natural_chinese_punctuation("你好,世界"), (
    "中文自然语言中的半角逗号必须报错"
)
assert validate_natural_chinese_punctuation("{SIG_N069}."), (
    "动态信号词语后的半角句号必须在构建期报错"
)
assert not validate_natural_chinese_punctuation("版本 1.2.3。"), (
    "版本号和小数点不得误报"
)
assert not validate_natural_chinese_punctuation("RUN CORE.MOS"), (
    "程序文件名中的半角句点不得误报"
)
assert not validate_natural_chinese_punctuation("正在启动..."), (
    "旧系统字体刻意使用的 ASCII 三点由上下文校验负责，不在此处误报"
)
assert not validate_natural_chinese_punctuation("我很高兴 :)"), (
    "ASCII 颜文字不得被误判为英文标点泄漏"
)
assert not validate_natural_chinese_punctuation("例如：        :->"), (
    "由冒号开头的箭头颜文字不得误报"
)
assert not validate_natural_chinese_punctuation("一个“.”符号。"), (
    "作为被引用字符的半角标点必须保留"
)
assert not validate_natural_chinese_punctuation("1. 第一项\n2. 第二项"), (
    "数字列表标记中的半角句点必须保留"
)

assert validate_duplicate_punctuation("没错。。"), "相邻的重复句号必须报错"
assert validate_duplicate_punctuation("没错。$animD19。"), (
    "动画控制标记不可见，标记两侧的重复句号也必须报错"
)
assert validate_duplicate_punctuation("第一句。{PART_001}。第二句"), (
    "对白分段标记不可见，标记两侧的重复句号也必须报错"
)
assert validate_duplicate_punctuation("等等，，继续"), "重复逗号必须报错"
assert validate_duplicate_punctuation("甲、、乙"), "重复顿号必须报错"
assert validate_duplicate_punctuation("标题：：正文"), "重复冒号必须报错"
assert validate_duplicate_punctuation("甲；；乙"), "重复分号必须报错"
assert validate_duplicate_punctuation("其实，。"), (
    "不同的停顿标点连续出现也必须报错，不能只检查相同字符重复"
)
assert validate_duplicate_punctuation("等等，！"), "逗号和感叹号连续也必须报错"
assert validate_duplicate_punctuation("结束。？"), "句号和问号连续也必须报错"
assert not validate_duplicate_punctuation("等等……"), "规范中文省略号不得误报"
assert not validate_duplicate_punctuation("等等…………"), "多组规范中文省略号不得误报"
assert not validate_duplicate_punctuation("这——不可能"), "规范中文破折号不得误报"
assert not validate_duplicate_punctuation("太好了！！"), "对白的强调叹号不得误报"
assert not validate_duplicate_punctuation("什么？？"), "对白的强调问号不得误报"
assert not validate_duplicate_punctuation("什么！？"), "问号和叹号连用属于有效语气标点"
assert not validate_duplicate_punctuation("真的？……"), "问号后接省略号属于有效语气标点"
assert not validate_duplicate_punctuation("……真的？"), "省略号后接问号属于有效语气标点"
assert not validate_duplicate_punctuation("住手——！"), "破折号后接叹号属于有效语气标点"
assert not validate_duplicate_punctuation("说完了。……"), "句号后的独立停顿不得误报"

assert validate_locked_glossary_terms(
    "The employee-of-the-week celebration is over.",
    "本周最佳员工庆祝活动结束了。",
), "锁定术语必须拒绝‘本周最佳员工’和普通‘庆祝活动’"
assert not validate_locked_glossary_terms(
    "The employee-of-the-week celebration is over.",
    "每周最佳员工表彰活动结束了。",
), "employee-of-the-week celebration 应统一为‘每周最佳员工表彰活动’"

assert validate_garbled_subtitle("你个[听不清]！"), "旧的含混语音标注必须报错"
assert not validate_garbled_subtitle("你个[含混的嘟囔]！"), (
    "定稿的含混语音标注应通过校验"
)

assert not validate_dictionary_slogan(
    "{SPEAKER_BAUTISTA}{PART_000}Happy dictionary,{PART_001}happy life.",
    "{SPEAKER_BAUTISTA}{PART_000}幸福词典，{PART_001}幸福人生。",
), "完整词典口号必须允许由 PART 标记分段"
assert validate_dictionary_slogan(
    "{SPEAKER_BAUTISTA}{PART_000}Happy dictionary,{PART_001}happy life.",
    "{SPEAKER_BAUTISTA}{PART_000}词典幸福，{PART_001}人生幸福。",
), "词典口号不得颠倒成“词典幸福／人生幸福”"
assert not validate_dictionary_slogan(
    "{SPEAKER_AKERS}{PART_000}Happy Dictionary--",
    "{SPEAKER_AKERS}{PART_000}幸福词典——",
) and not validate_dictionary_slogan(
    "{SPEAKER_COLLINS}{PART_000}--Happy life!",
    "{SPEAKER_COLLINS}{PART_000}——幸福人生！",
), "由不同角色拆开说的幸福口号必须逐句通过校验"
assert validate_dictionary_slogan(
    "{SPEAKER_AKERS}{PART_000}Happy Dictionary--",
    "{SPEAKER_AKERS}{PART_000}幸福人生——",
) and validate_dictionary_slogan(
    "{SPEAKER_COLLINS}{PART_000}--Happy life!",
    "{SPEAKER_COLLINS}{PART_000}——幸福词典！",
), "拆给不同角色的前后半句即使都用了规范词，也不能互换"
assert not validate_dictionary_slogan(
    "{SPEAKER_BAUTISTA}{PART_000}Unhappy dictionary.",
    "{SPEAKER_BAUTISTA}{PART_000}不幸词典。",
) and not validate_dictionary_slogan(
    "{SPEAKER_AKERS}{PART_000}Unhappy life.",
    "{SPEAKER_AKERS}{PART_000}不幸人生。",
), "由不同角色拆开说的不幸口号必须逐句通过校验"
assert validate_dictionary_slogan(
    "{SPEAKER_BAUTISTA}{PART_000}Unhappy dictionary.",
    "{SPEAKER_BAUTISTA}{PART_000}词典不开心。",
), "Unhappy dictionary 不得再混用“不开心／不幸福”"
assert validate_dictionary_slogan(
    "{SPEAKER_BAUTISTA}{PART_000}Happy dictionary,{PART_001}happy life.",
    "{SPEAKER_BAUTISTA}{PART_000}幸福人生，{PART_001}幸福词典。",
), "仅检查两个规范短语是否存在会漏掉顺序颠倒"
assert validate_dictionary_slogan(
    "{SPEAKER_BAUTISTA}{PART_000}And happy dictionaries.",
    "{SPEAKER_BAUTISTA}{PART_000}还有快乐词典。",
), "复数 happy dictionaries 也属于同一固定口号术语"
assert not validate_dictionary_slogan(
    "{SPEAKER_BAUTISTA}{PART_000}And happy dictionaries.",
    "{SPEAKER_BAUTISTA}{PART_000}还有幸福词典。",
), "复数 happy dictionaries 必须允许规范译名"
assert validate_dictionary_slogan(
    "{SPEAKER_BAUTISTA}{PART_000}Unhappy dictionaries.",
    "{SPEAKER_BAUTISTA}{PART_000}不开心的词典。",
), "复数 unhappy dictionaries 也必须命中固定译名"
assert not validate_dictionary_slogan(
    "{SPEAKER_BAUTISTA}{PART_000}Unhappy dictionaries.",
    "{SPEAKER_BAUTISTA}{PART_000}不幸词典。",
), "复数 unhappy dictionaries 必须允许规范译名"

dialogue_payload = json.loads(
    (PROJECT / "patch" / "Translations" / "dialogue.json").read_text(encoding="utf-8")
)
titles_payload = json.loads(
    (PROJECT / "patch" / "Translations" / "titles.json").read_text(encoding="utf-8")
)
glossary_payload = json.loads(
    (PROJECT / "work" / "glossary.locked.json").read_text(encoding="utf-8")
)
nozomi_glossary = next(
    item for item in glossary_payload["characters"] if item["canonical"] == "Nozomi"
)
assert nozomi_glossary["render"] == "希美", "日语人名 Nozomi 必须统一译为‘希美’"
all_nozomi_runtime_text = [
    entry["translated_text"]
    for payload in (dialogue_payload, titles_payload)
    for entry in payload["entries"]
]
assert not any("诺佐米" in text for text in all_nozomi_runtime_text), (
    "运行时译文不得残留 Nozomi 的生硬音译‘诺佐米’"
)
nozomi_dialogue = {
    entry["stable_key"]: entry["translated_text"]
    for entry in dialogue_payload["entries"]
    if entry["stable_key"] in {
        "dialogue:1130/frame:0", "dialogue:1130/frame:1",
        "dialogue:646/frame:0", "dialogue:646/frame:3",
        "dialogue:646/frame:4", "dialogue:646/frame:5",
        "dialogue:646/frame:8",
    }
}
assert nozomi_dialogue["dialogue:1130/frame:0"].endswith("你和谜美……？")
assert nozomi_dialogue["dialogue:1130/frame:1"].startswith(
    "{SPEAKER_COLLINS}{PART_000}是希美，"
), "Akers 叫错名字后，Collins 必须明确纠正为‘希美’"
assert nozomi_dialogue["dialogue:646/frame:0"].endswith("那你和谜美聊得怎么样？")
assert nozomi_dialogue["dialogue:646/frame:3"].endswith(
    "你把“希美”念成“谜美”了。"
)
assert nozomi_dialogue["dialogue:646/frame:4"].endswith("怎么会？")
assert "“希美”读 Nozomi，不是 Nazomi。" in nozomi_dialogue[
    "dialogue:646/frame:5"
]
assert nozomi_dialogue["dialogue:646/frame:8"].endswith("Nozomi。"), (
    "讨论日语发音时必须保留罗马字 Nozomi，不能用汉字‘希美’替代读音"
)
is_wordplay_entry = next(
    entry
    for entry in dialogue_payload["entries"]
    if entry["stable_key"] == "dialogue:242/frame:1"
)
assert is_wordplay_entry["translated_text"] == (
    "{SPEAKER_COLLINS}{PART_000}“是”，"
    "{PART_001}也就是用来表示赋值关系的动词。"
    "{PART_002}柯林斯博士“是”女性，"
    "{PART_003}巴蒂斯塔博士“是”程序员——"
), "IS 接龙必须统一落到自然的中文‘是’字句，不能把第二个 IS 单独译成‘在’"

small_wordplay_entries = {
    entry["stable_key"]: entry["translated_text"]
    for entry in dialogue_payload["entries"]
    if entry["stable_key"] in {"dialogue:907/frame:0", "dialogue:907/frame:1"}
}
assert small_wordplay_entries == {
    "dialogue:907/frame:0": (
        "{SPEAKER_AKERS}{PART_000}$animA1词典里又添了一个"
        "{PART_001}很不错的“小”词条，"
        "{PART_002}{PLAYER_NAME}！"
    ),
    "dialogue:907/frame:1": (
        "{SPEAKER_BAUTISTA}{PART_000}够了，艾伦。"
        "{PART_001}上次那个关于“大”的双关，"
        "{PART_002}我忍了。"
        "{PART_003}这次的双关已经尬过头了。"
    ),
}, "大小双关必须使用自然中文，cheesy 不得误译为煽情意义的‘肉麻’"

meteor_sleep_entry = next(
    entry
    for entry in dialogue_payload["entries"]
    if entry["stable_key"] == "dialogue:793/frame:19"
)
assert "{PART_003}让我们睡觉 {SIG_N085}。" in meteor_sleep_entry["translated_text"], (
    "示范陨石语语法时必须保留‘动作 + SIG_N085’的词序"
)

can_only_imagine_entries = {
    entry["stable_key"]: entry["translated_text"]
    for entry in dialogue_payload["entries"]
    if entry["stable_key"] in {
        "dialogue:46/frame:2",
        "dialogue:794/frame:36",
        "dialogue:806/frame:23",
        "dialogue:806/frame:24",
    }
}
assert "也不知道多普勒博士琢磨这些问题时" in can_only_imagine_entries[
    "dialogue:46/frame:2"
]
assert "我不禁想象，接下来的翻译工作中" in can_only_imagine_entries[
    "dialogue:794/frame:36"
]
assert "{PART_016}我会热切期待，{PART_017}一如既往。" in can_only_imagine_entries[
    "dialogue:794/frame:36"
]
assert "我只能想到一种定义。" in can_only_imagine_entries[
    "dialogue:806/frame:23"
], "实义 Can only imagine 应保留‘只能想到’，不能按习语误改"
assert "那真不知道下周还会有什么新发现。" in can_only_imagine_entries[
    "dialogue:806/frame:24"
]

hottest_wordplay_entries = {
    entry["stable_key"]: entry["translated_text"]
    for entry in dialogue_payload["entries"]
    if entry["stable_key"] in {
        "dialogue:857/frame:5",
        "dialogue:857/frame:7",
        "dialogue:857/frame:8",
        "dialogue:857/frame:10",
    }
}
assert "谁的温度最高？" in hottest_wordplay_entries["dialogue:857/frame:5"]
assert "最火辣的我" in hottest_wordplay_entries["dialogue:857/frame:5"]
assert hottest_wordplay_entries["dialogue:857/frame:7"].endswith("想得美。")
assert hottest_wordplay_entries["dialogue:857/frame:8"].endswith("问问又不亏……")
assert hottest_wordplay_entries["dialogue:857/frame:10"].endswith("想得美。"), (
    "hottest 双关必须区分客观温度与自夸的火辣，并保持两个分支措辞一致"
)

week_37_entries = {
    entry["stable_key"]: entry["translated_text"]
    for entry in dialogue_payload["entries"]
    if entry["stable_key"].startswith("dialogue:795/frame:")
}
assert week_37_entries["dialogue:795/frame:2"].endswith("你还好吧？")
assert "这两个概念都相当{SIG_N104}" in week_37_entries["dialogue:795/frame:7"]
assert "负责这种感知的{SIG_N160}" in week_37_entries["dialogue:795/frame:12"]
assert week_37_entries["dialogue:795/frame:12"].endswith("{PART_008}挺酷的。")
assert "谢谢你这次肯发表意见" in week_37_entries["dialogue:795/frame:13"]
assert week_37_entries["dialogue:795/frame:18"] == (
    "{SPEAKER_AKERS}{PART_000}我的枕头在呼唤我了！"
    "{PART_001}隔着这么远我都听见了！"
)
assert week_37_entries["dialogue:795/frame:20"].endswith(
    "{PART_000}祝大家{PART_001}做个好梦。"
), "第 37 周结束对白应保持自然表达，并保留所有分段和外星词语标记"

print("Translation-text checks passed: quotes, ellipses, punctuation, cues, slogans and glossary.")

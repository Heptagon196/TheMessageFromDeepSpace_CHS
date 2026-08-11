import json
from pathlib import Path


ROOT = Path(__file__).resolve().parent
SOURCE = ROOT / "src_19_system_other.json"
OUTPUT = ROOT / "trans_19.json"


ROMAN = {
    "0": "零",
    "I": "一",
    "II": "二",
    "III": "三",
    "IV": "四",
    "V": "五",
    "VI": "六",
    "VII": "七",
    "VIII": "八",
    "IX": "九",
    "X": "十",
    "XI": "十一",
}


def act_title(source: str) -> str | None:
    upper = source.upper()
    if not upper.startswith("ACT "):
        return None
    rest = upper[4:]
    if " - PART " in rest:
        act, part = rest.split(" - PART ", 1)
        return f"第{ROMAN[act]}幕·第{ROMAN[part]}部分"
    return f"第{ROMAN[rest]}幕"


NUMBER_WORDS = [
    "Zero", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine",
    "Ten", "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen",
    "Seventeen", "Eighteen", "Nineteen", "Twenty", "Twenty-One", "Twenty-Two",
    "Twenty-Three", "Twenty-Four", "Twenty-Five", "Twenty-Six", "Twenty-Seven",
    "Twenty-Eight", "Twenty-Nine", "Thirty", "Thirty-One", "Thirty-Two",
    "Thirty-Three", "Thirty-Four", "Thirty-Five", "Thirty-Six", "Thirty-Seven",
    "Thirty-Eight", "Thirty-Nine", "Forty", "Forty-One", "Forty-Two", "Forty-Three",
    "Forty-Four", "Forty-Five", "Forty-Six", "Forty-Seven", "Forty-Eight",
    "Forty-Nine", "Fifty", "Fifty-One", "Fifty-Two", "Fifty-Three", "Fifty-Four",
    "Fifty-Five", "Fifty-Six", "Fifty-Seven", "Fifty-Eight", "Fifty-Nine", "Sixty",
]


def chinese_number(number: int) -> str:
    digits = "零一二三四五六七八九"
    if number < 10:
        return digits[number]
    tens, ones = divmod(number, 10)
    prefix = "十" if tens == 1 else digits[tens] + "十"
    return prefix if ones == 0 else prefix + digits[ones]


TRANSLATIONS = {
    "Grand Finale": "最终章",
    "ACKNOWLEDGE": "回应",
    "Return Our Message": "传回我们的讯息",
    "To See What We Say": "看看我们传达了什么",
    "First Contact": "初次接触",
    "Our Universal Bonds": "宇宙间的共同纽带",
    "Where We Are": "我们身在何方",
    "Life": "生命",
    "We Are Not Alone": "我们并不孤单",
    "We Hope We Are Not Alone": "但愿我们并不孤单",
    "Sent with Love": "寄以爱意",
    "Our Species": "我们的物种",
    "Our Beliefs": "我们的信念",
    "Our Gift": "我们的礼物",
    "Parting Words": "临别寄语",
    "Math: Our Common Bond": "数学：我们共同的纽带",
    "Cannot Compute": "无法计算",
    "Well played.": "真有你的。",
    "Not this computer.": "这台电脑可算不了。",
    "Digit {DYN_0} is outside base ": "数字 {DYN_0} 不适用于以下进制：",
    "nope.": "不行。",
    "very large.": "太大了。",
    "very small.": "太小了。",
    "Cmon man.": "别闹了。",
    "LARGE TRANSMISSION COMPILING": "正在编译大型传输",
    " demo": " 演示版",
    "Dr. ": "Dr. ",
    "* * * New Log Ready * * *": "* * * 新日志已就绪 * * *",
    "{SPEAKER_AUTO_LOG}{PART_000}AUTO-LOG ENDED{PART_001}SAVED TO LOG":
        "{SPEAKER_AUTO_LOG}{PART_000}自动日志已结束{PART_001}已保存至日志",
    "{SPEAKER_AUTO_LOG}{PART_000}AUTO-LOG TRANSCRIPT{PART_001}BOOTING ON...{PART_002}[@ @] to skip{PART_003}...":
        "{SPEAKER_AUTO_LOG}{PART_000}自动日志记录{PART_001}正在启动...{PART_002}按 [@ @] 跳过{PART_003}...",
    "{SPEAKER_AUTO_LOG}{PART_000}ENDING AUTO-LOG DUE TO INACTIVITY{PART_001}[Right-Click] OR [Tab] TO ADVANCE DIALOGUES":
        "{SPEAKER_AUTO_LOG}{PART_000}因长时间无操作，正在结束自动日志{PART_001}按 [右键] 或 [Tab] 继续对话",
    "Pilot": "驾驶员",
    "Copilot": "副驾驶员",
    "Pasted {DYN_0} from clipboard": "已从剪贴板粘贴 {DYN_0}",
    "Copied {DYN_0} to Clipboard": "已将 {DYN_0} 复制到剪贴板",
    "Your colleagues have some ideas for what these signals mean...": "同事们对这些信号的含义有了一些想法...",
    "View the hypotheses inside each entry in the dictionary. (Dictionary -> Entry Notes -> Hypotheses)":
        "可在词典各条目中查看假说。\n（词典 → 条目注释 → 假说）",
    "Freq #": "频率 #",
    " Hz": " Hz",
    "DATA MISSING": "数据缺失",
    "TRANSMISSION: ": "传输：",
    "MISSION TIME: ": "任务时间：",
    "METEOR_OS v{DYN_0} BOOT SEQUENCE ... OK\nCPU @ 4MHz\nMEMORY TEST : 262144\n\nCMOS ... 99%\nRTC CPU TIME SYNC ... OK\nIRQ0 - IRQ8 ... OK\n\n-RUN BAU_CORE_{DYN_0}.MOS \n-RUN DUAL_GUI.MOS ... OK\n-RUN AUTO_LOG.MOS ... OK\n-RUN {PLAYER_NAME}.MOS ... OK\n\n\n\n{DYN_1} {DYN_2} (AKDT) \nTOTAL OPERATION TIME: {DYN_3}":
        "METEOR_OS v{DYN_0} 启动序列 ... 完成\nCPU @ 4MHz\n内存测试：262144\n\nCMOS ... 99%\nRTC CPU 时间同步 ... 完成\nIRQ0 - IRQ8 ... 完成\n\n-RUN BAU_CORE_{DYN_0}.MOS \n-RUN DUAL_GUI.MOS ... 完成\n-RUN AUTO_LOG.MOS ... 完成\n-RUN {PLAYER_NAME}.MOS ... 完成\n\n\n\n{DYN_1} {DYN_2} (AKDT) \n总运行时间：{DYN_3}",
    "METEOR_OS v{DYN_0} SHUT DOWN : INIT\nCPU @ 4MHz\n\nCMOS ... 99%\n\n-FREE BAU_CORE_{DYN_0}.MOS \n-FREE DUAL_GUI.MOS ... OK\n-FREE AUTO_LOG.MOS ... OK\n-FREE {PLAYER_NAME}.MOS ... OK\n\n{DYN_1} {DYN_2} (AKDT)\nTOTAL OPERATION TIME: {DYN_3}\n\nDE-ALLOCATING 221033 BYTES ... OK\nMEMORY FREE : 262144\n\n\nSYSTEM OFFLINE":
        "METEOR_OS v{DYN_0} 关机初始化\nCPU @ 4MHz\n\nCMOS ... 99%\n\n-FREE BAU_CORE_{DYN_0}.MOS \n-FREE DUAL_GUI.MOS ... 完成\n-FREE AUTO_LOG.MOS ... 完成\n-FREE {PLAYER_NAME}.MOS ... 完成\n\n{DYN_1} {DYN_2} (AKDT)\n总运行时间：{DYN_3}\n\n正在释放 221033 字节 ... 完成\n可用内存：262144\n\n\n系统离线",
    "[Left-click] to Re-enter Name\n[Right-click] or [Tab] to Continue":
        "按 [左键] 重新输入姓名\n按 [右键] 或 [Tab] 继续",
    "{SPEAKER_AKERS}{PART_000}Hey!{PART_001}That's my name!":
        "{SPEAKER_AKERS}{PART_000}嘿！{PART_001}这可是我的名字！",
    "{SPEAKER_DOPPLER}{PART_000}Doug's mine.":
        "{SPEAKER_DOPPLER}{PART_000}道格归我。",
    "{SPEAKER_DOPPLER}{PART_000}Another Doppler?":
        "{SPEAKER_DOPPLER}{PART_000}又一个多普勒？",
    "{SPEAKER_DOPPLER}{PART_000}A certain astronomer calls me that.":
        "{SPEAKER_DOPPLER}{PART_000}有位天文学家就是这么叫我的。",
    "{SPEAKER_DOPPLER}{PART_000}D is reserved for me.":
        "{SPEAKER_DOPPLER}{PART_000}D 是我的。",
    "{SPEAKER_AUTO_LOG}{PART_000}Why.{PART_001}Why do you want that.":
        "{SPEAKER_AUTO_LOG}{PART_000}为什么。{PART_001}为什么非得叫这个名字。",
    "{SPEAKER_AUTO_LOG}{PART_000}Be more creative.":
        "{SPEAKER_AUTO_LOG}{PART_000}换个有创意的。",
    "{SPEAKER_AUTO_LOG}{PART_000}No.{PART_001}Pick a new name.":
        "{SPEAKER_AUTO_LOG}{PART_000}不行。{PART_001}换个名字。",
    "{SPEAKER_AKERS}{PART_000}Sorry!{PART_001}Name's taken !!":
        "{SPEAKER_AKERS}{PART_000}抱歉！{PART_001}这个名字有人用了！",
    "{SPEAKER_AKERS}{PART_000}A for effort.":
        "{SPEAKER_AKERS}{PART_000}努力可嘉，给你个 A。",
    "{SPEAKER_BAUTISTA}{PART_000}Nope.":
        "{SPEAKER_BAUTISTA}{PART_000}不行。",
    "{SPEAKER_BAUTISTA}{PART_000}Mmm.{PART_001}Pick another name.":
        "{SPEAKER_BAUTISTA}{PART_000}嗯……{PART_001}换个名字。",
    "{SPEAKER_COLLINS}{PART_000}Another Carrie?{PART_001}Nice to meet you!":
        "{SPEAKER_COLLINS}{PART_000}又一个凯莉？{PART_001}很高兴认识你！",
    "{SPEAKER_COLLINS}{PART_000}This could get confusing...":
        "{SPEAKER_COLLINS}{PART_000}这样很容易混淆……",
    "{SPEAKER_COLLINS}{PART_000}C is for Carrie!":
        "{SPEAKER_COLLINS}{PART_000}C 代表凯莉！",
    "{SPEAKER_DOPPLER}{PART_000}There can only be one.":
        "{SPEAKER_DOPPLER}{PART_000}有我一个就够了。",
    "--- Mission Translator ---\nWhat is your name?":
        "--- 任务翻译员 ---\n你叫什么名字？",
    "Alan's Journal: ": "艾伦的手记：",
    " - Complete!!": " - 完成！！",
    "Bautista's Log: ": "巴蒂斯塔的日志：",
    "Carrie's Diary: ": "凯莉的日记：",
    "Doppler's Report: ": "多普勒的报告：",
    " Begins...": " 开始...",
    "***NO NEW WORDS***": "***没有新词***",
    "WORDS:": "词汇：",
    "Thoughts?": "有什么想法？",
    "'s Thoughts: ": "的想法：",
    "TOTAL TRANSMISSIONS:": "传输总数：",
    "TOTAL WORDS:": "词汇总数：",
    "Groups Completed:": "已完成的谜题组：",
    "TRANSMISSIONS:": "传输：",
    "Week ": "周数：",
    "WORDS NAMED: ": "已命名词汇：",
    " = 14.37218841684362 kilograms": " = 14.37218841684362 千克",
    " = 0.8066 seconds": " = 0.8066 秒",
    "Distance light travels in 1  ": "光在以下时长内传播的距离：1  ",
    " = 241,908,530.21 meters": " = 241,908,530.21 米",
    " = ~8.3 Earth masses": " = 约为地球质量的 8.3 倍",
    "Distance light travels in 1/8^9 ": "光在以下时长内传播的距离：1/8^9 ",
    " = (approx) 1.80236 meters": " = 约 1.80236 米",
    "Song Playing: ": "正在播放：",
    "SPECTRUM POINT: ": "光谱点：",
    "Advance dialogue to Continue": "继续对话",
    "Press 'Esc' to Submit": "按 [Esc] 提交",
    "Translator said...": "翻译员说...",
    "262 years ago...": "262 年前...",
    "NAME SIGNAL": "命名信号",
}


def translate(source: str) -> str:
    translated_act = act_title(source)
    if translated_act is not None:
        return translated_act
    if source in NUMBER_WORDS:
        return chinese_number(NUMBER_WORDS.index(source))
    return TRANSLATIONS[source]


def main() -> None:
    items = json.loads(SOURCE.read_text(encoding="utf-8"))
    results = []
    missing = []
    for item in items:
        source = item["source_text"]
        try:
            translated = translate(source)
        except KeyError:
            missing.append(source)
            continue
        results.append({"text_index": item["text_index"], "translated_text": translated})

    if missing:
        raise SystemExit("Missing translations:\n" + "\n".join(repr(x) for x in sorted(set(missing))))
    if len(results) != len(items):
        raise SystemExit(f"Count mismatch: {len(results)} != {len(items)}")
    OUTPUT.write_text(json.dumps(results, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(f"wrote {len(results)} translations to {OUTPUT}")


if __name__ == "__main__":
    main()

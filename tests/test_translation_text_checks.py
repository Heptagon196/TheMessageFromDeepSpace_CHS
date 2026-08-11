from __future__ import annotations

import sys
from pathlib import Path


PROJECT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(PROJECT / "tools"))

from translation_text_checks import validate_chinese_quotes


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

print("Translation-text checks passed: Chinese quote hierarchy and balance.")

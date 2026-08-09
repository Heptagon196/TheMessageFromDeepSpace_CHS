import json
from pathlib import Path


BASE = Path(__file__).resolve().parent
SOURCE = BASE / "src_18_system_hypotheses.json"
OUTPUT = BASE / "trans_18.json"
NEWTERMS = BASE / "newterms_18.txt"

# These are the three researchers' visible guesses for dictionary entries.
# Concatenated/all-caps guesses are translated; locked alien/puzzle metavariables
# would stay unchanged, but none of those locked metavariables occurs in this batch.
PAIRS = r"""
NEGATIVE	负
FLIP	取反
ADDONE	加一
SEQ	序列
GAP	间隔
TOGETHER	相加
ASSOCIATE	关联
COMPUTE	计算
RESULT_IS	结果为
ADDPREV	加上前一项
SUM	和
MULTIPLYPREV	乘以前一项
PRODUCT	积
SUBTRACTPREV	减去前一项
DIFFERENCE	差
DIVIDEPREV	除以前一项
QUOTIENT	商
EXPONENT	指数
POWER	幂
DECIMALOCTIMAL	十进制/八进制
FRACTION_NUMB	分数
THING	东西
VARIABLE	变量
FILLIN	填空
RESPONSE	回答
MAKECOORDINATE	生成坐标
FUNC	函数
PLOTS_VARIABLES	绘制变量图
START	开始
HAS	包含
STOP	结束
ENDHAS	包含结束
POINT	点
POSITION	位置
LINE	直线
LS	线段
LINE_SEGMENT	线段
POLYGON	多边形
SHAPE	形状
CONTAINER	容器
STRUCT	结构
GEOMETRY	几何图形
LENGTH	长度
MAGNITUDE	大小
DISTANCE	距离
PERIMETER	周长
BORDER_LENGTH	边界长度
AREA	面积
SIZE	大小
QUANTITY	数量
AMOUNT	总量
CLOSEENOUGH	差不多
ROUND	舍入
APPROXIMATELY	近似
KEEPGOING	继续
CONTINUED	延续
TRUEORFALSE	真假
STATEMENT	命题
TRUE	真
YES	是
FALSE	假
NO	否
REVERSE	反向
NEGATE	取反
BOTHTRUE	两者都为真
AND	且
ATLEASTONETRUE	至少一个为真
OR	或
NOTEQUAL	不等于
IS_GREATER_THAN	大于
NOTEQUALAGAINIGUESS	又是不等于吧
IS_LESS_THAN	小于
BIGGER	更大
AREA>	面积>
IS_LARGER_THAN	大于
SMALLER	更小
AREA<	面积<
IS_SMALLER_THAN	小于
SO	所以
THUS	因此
IFTRUE	如果为真
IF	如果
WITHIN	在...内
CONSTRUCT	构造
PRODUCE	生成
SHIFT	偏移
TRANSLATE	平移
MOVE	移动
STARTPLACE	起点
SOURCE	源
FROM_PREV	从上一项
ENDPLACE	终点
DEST	目标
TO_PREV	到上一项
FREQUENCY	频率
FREQ	频率
WORD	词
TRANSMISSION	传输
INFOPACKET	信息包
METEORITE	陨石
TRANSMITTER	发射器
BEACON	信标
METEORITESOURCE	陨石来源
COMMUNICATOR_A	通信端 A
METEORITERECEIVER	陨石接收方
COMMUNICATOR_B	通信端 B
CIRCLE	圆
SPHERE	球体
BALL	球
XCOORD	X 坐标
HORIZONTAL	水平
WIDTH	宽度
YCOORD	Y 坐标
DEPTH	深度
ZCOORD	Z 坐标
VERTICAL	垂直
HEIGHT	高度
ORB	球体
VISUALOBJ	可视对象
DRAWN_SPHERE	绘制的球体
IMAGE	图像
VISUALFRAME	图像帧
3D_DRAWING	三维图
COLOR	颜色
HUE	色相
VISIBLE_LIGHT	可见光
ELEMENT	元素
ATOM	原子
INSTANCE	个体
COMPOUND	化合物
MOLECULE	分子
CHEMICAL	化学物质
CHEMICALREACTION	化学反应
REACTS_TO_FORM	反应生成
CHEMICALREACTIONAGAIN	又是化学反应
TRANSFORMATION	转化
PROTON	质子
NEUTRON	中子
ELECTRON	电子
NUCLIDE	核素
ISOTOPE	同位素
ATOM_VARIANT	原子变体
RADIOACTIVEDECAY	放射性衰变
HALVING	减半
ATOM_DECOMPOSE	原子分解
DECAYFACTOR?	衰变因子？
LAMBDA	λ
PROGRESSOR	变化参数
SPEED	速度
RATE	速率
SPEEDOFLIGHT	光速
UNITOFMEASURE	计量单位
MEASUREMENT	测量
BENCHMARK	基准
AKERSECOND	埃克斯秒
ALIENSEC	外星秒
MINOR_SECOND	小时间单位
AKERYEAR	埃克斯年
ALIENYEAR	外星年
33_DAY_YEAR	33 天的一年
AKERMONTH	埃克斯月
ALIENMONTH	外星月
9_DAY_LUNAR	9 天的月周期
LIGHTAKERSECOND	光埃克斯秒
ALIENMETER	外星米
MAJOR_DISTANCE	大距离单位
MINILIGHTAKERSECOND	微型光埃克斯秒
ALIENNANOMETER	外星纳米
MINOR_DISTANCE	小距离单位
MASS	质量
MATTER	物质
AKERPOUND	埃克斯磅
ALIENGRAM	外星克
MINOR_MASS	小质量单位
MEGAAKERPOUND	兆埃克斯磅
ALIENRONNAGRAM	外星容克
MAJOR_MASS	大质量单位
OR(NOTMATH)	或者（不是数学那个）
OR_OPTION	二选一
TYPE	类型
PART_OF_SPEECH	词性
THINGAMABOB	那个东西
NOUN	名词
DOTHING	做事
VERB	动词
ADJECTIVE	形容词
PREPO_CONJUNCTION	介词/连词
ACTOR	施事者
SUBJECT	主语
ACTEDON	受事者
OBJECT	宾语
VERBCOMMIT	做动作
GO	去
DO	做
TARGET	目标
AT	朝向
OPP	相反
OPPOSITE	相反
DESTROY!!	摧毁！！
BREAK	破坏
DECONSTRUCT	拆解
NUCLEARFUSION	核聚变
ELEMFUSE	元素融合
MAKE_ATOMS	制造原子
STAR	恒星
ATOM_MAKER	原子制造者
MOTION	运动
TRANSLATION	平移
MOVEMENT	运动
ORBIT	公转
SPIN	自转
ROTATION	旋转
PLANET	行星
STAR_ORBITER	绕恒星天体
MOON	卫星
SATELLITE	卫星
WHITEDWARFSTAR	白矮星
NEUTRONSTAR	中子星
[I_TRUST_AKERS]	[我相信埃克斯]
BLACKHOLE	黑洞
GALAXY	星系
STARCLUSTER	星团
MEMBER	成员
HYPONYM	下义词
EQUALSAGAIN?	又是等于？
RELATE	关联
IS	是
LIFE	生命
ORGANIC	有机
CARBON-BASED	碳基
HYDRO	含氢
HYDROGEN-BASED	氢基
KINDASORTA	有点像
INTANGIBLE	无形
ABSTRACT	抽象
REALANDSOLID	真实有形
TANGIBLE	有形
CONCRETE	具体
MOSTEST	最最
+TOP+	+顶级+
ABSOLUTE	绝对
VERY	非常
EXTREME	极端
RELATIVELYLARGE	相对较大
BIG	大
RELATIVELYSMALL	相对较小
MINI	小型
SMALL	小
ALL	全部
TOTAL	总计
NOTHING	什么都没有
NULL	空
NONE	无
MINIMUM	最小值
MIN	最小
MAXIMUM	最大值
MAX	最大
MEDIAN	中位数
AVG	平均数
MIDDLE	中间值
MODE	众数
MAJORITY	多数
LESS	较少
UNCOMMON	少见
MINORITY	少数
SIMILAR	相似
NEARLY	接近
BEFORE	之前
PAST	过去
PREVIOUS	之前
NOW	现在
PRESENT	当前
CURRENT	当前
AFTER	之后
FUTURE	未来
COMING	即将到来
ATTIME	在...时
WHEN	何时
NEXT	下一个
LINKS_TO	连接到
KNOWED	知道的
AWARE	感知
KNOWN	已知
NOTKNOWED	不知道的
UNAWARE	未察觉
UNKNOWN	未知
UNIVERSE	宇宙
EVERYTHING	万物
EXISTENCE	存在
INFINITY	无穷
UNCOUNTABLE	不可数
LEARN	学习
COEXIST	共存
ASPIRE	追求
INDIVIDUAL	个体
SPECIES_MEMBER	物种成员
AKERIAN	埃克斯人
AMAN	一个人
ALIENFRIEND	外星朋友
HUMAN	人类
MAN	人
HUMANFRIEND	人类朋友
SPECIES	物种
BORN	出生
BIRTH	出生
DIE	死亡
LIVELIFE	度过一生
LIFETIME	一生
LIFESPAN	寿命
LIFE_DURATION	存活时间
METEORAUTHOR	陨石作者
AUTHOR	作者
METEOR_MAKER	陨石制造者
AUTHORWIFE	作者的妻子
COPARENT	共同亲本
MAKER_MATE	制造者的伴侣
ALAN	艾伦
TRANSLATOR	翻译员
US_5	我们五个
EARTH	地球
DEST_PLANET	目标行星
HOME	家园
AKERTH	埃克斯星
SRC_PLANET	来源行星
ALIEN_HOME	外星家园
AKERIA	埃克斯恒星
ALIEN_STAR	外星恒星
CHILD	孩子
OFFSPRING	后代
PARENT	亲本
BABYMAKING	生孩子
[TERM_CENSORED]	[词语已屏蔽]
REPRODUCE	繁殖
CANDO	做得到
CAN	能
ABLETODO	能够做到
CONSUME	消耗
EAT	吃
BABYALIEN	外星宝宝
LIFEPHASE1	生命阶段 1
BABY	幼体
ADULTALIEN	成年外星人
LIFEPHASE2	生命阶段 2
FULLYGROWN	完全成熟
OLDALIEN	老年外星人
LIFEPHASE3	生命阶段 3
ELDERLY	老年
FEELING	感受
PERSPECTIVE	视角
RESPECT	尊重
LOVE	爱
SHOULDDO	应该做
GOOD	好
SHOULDAVOID	应该避免
BAD	坏
HAPPY	快乐
SAD	悲伤
PIECE	一部分
COMPONENT	组成部分
PORTION	部分
FULLTHING	完整事物
WHOLE	整体
ENTIRETY	全体
BODYPART	身体部位
BIOLOGICAL	生物结构
ORGAN	器官
LIGHT	光
LIGHTSEER	感光器
OBSERVER	观察器
EYE	眼睛
GATHERINPUT	获取信息
SEE	看见
ATOMICJITTER	原子振动
THERMAL	热
HEAT	热量
COLDMATTER	冷物质
ROCK	岩石
SOLID	固体
MIDMATTER	中温物质
MOLTEN	熔融体
LIQUID	液体
HOTMATTER	热物质
FORMLESS	无固定形态
GAS	气体
REALLYLONGTIMEAGO	很久很久以前
600MYEARAGO	6 亿年前
ANCIENT	远古
CONTAINED	已包含
IN	在内
INSIDE	内部
UNCONTAINED	未包含
OUT	在外
OUTSIDE	外部
FACE	面
DIRECTION	方向
ATMOSPHERE	大气
SKY	天空
AIR	空气
BODYOFWATER	水体
BIGWATER	大片水域
BODY_OF_WATER	水体
CHANGE	变化
TRANSFORM	转化
ALTER	改变
NEAR	附近
ADJACENT	相邻
CLOSE	接近
STARSIDE	向阳面
PLANET_SIDE_1	行星一侧
DARKSIDE	背阳面
PLANET_SIDE_2	行星另一侧
EVOLUTION	演化
GENETIC	遗传
SPECIATION	物种形成
ANIMAL	动物
MOBILELIFE	可移动生物
NONANIMAL	非动物
IMMOBILELIFE	不可移动生物
PLANT	植物
PREDATOR	捕食者
EATER	摄食者
HETEROTROPH	异养生物
PREY	猎物
EATEN	被食者
AUTOTROPH	自养生物
FISH	鱼
OCEANDWELLER	海洋生物
AQUATIC	水生
LANDLUBBER!	陆地家伙！
LANDDWELLER	陆生生物
TERRESTRIAL	陆生
FISHPLANT	鱼草
IMMOBILEWATERLIFE	固着水生生物
SEAWEED?	海藻？
BOTHWAYS	双向
COMMUTATIVE	可交换
RECIPROCATED	相互
POSITIVE	正
GRAVITATE	吸引
ATTRACT	吸引
PROPEL	推开
REPEL	排斥
ELECTROMAGNETISM	电磁现象
ELECTRIC	电性
ELECTRICITY	电
ELECTRICEYE	电眼
ELECTRICORGAN	发电器官
ELECTRIC_ORGAN	发电器官
BRAIN	大脑
CONSCIOUSNESS	意识
INTELLIGENT	有智慧
SMART	聪明
COMMUNICATION	交流
COLLECTIVEINFORMATION	共享信息
LANGUAGE	语言
SPEAK	说话
INFORMATIONEXCHANGE	信息交换
COMMUNICATE	交流
CURIOSITY	好奇心
SCIENCE	科学
QUEST_FOR_KNOWLEDGE	求知
BOTHDO	一起做
COLLABORATE	协作
COOPERATE	合作
WHOLESPECIES	整个物种
SOVEREIGNTY	主权
CIVILIZATION	文明
EASYKNOWLEDGE	简单知识
FACT	事实
TRICKYKNOWLEDGE	难懂的知识
OPINION	观点
BELIEF	信念
THINK	思考
OPINE	认为
BELIEVE	相信
COLLECTIVETHOUGHTS	集体思想
PHILOSOPHY	哲学
CIVILIZATION_BELIEFS	文明信仰
TENET	信条
DOCTRINE	教义
VIRTUE	美德
STRIVETOLIVE	努力活下去
LIFEGOOD	珍爱生命
LIFE_IS_PRECIOUS	生命珍贵
STRIVETOREPRODUCE	努力繁殖
PERSISTSPECIESGOOD	重视物种延续
MAKE_OFFSPRING	繁育后代
STRIVETODO	努力行动
PRODUCTIONGOOD	重视创造
EXERCISE_WILL	践行意志
STRIVETOLEARN	努力学习
LEARNGOOD	重视学习
DO_SCIENCE	科学研究
STRIVETOSHARE	努力分享
TEACHGOOD	重视传授
SHARE_KNOWLEDGE	分享知识
BELIEFSTRUCTURE	信仰体系
VIEWPOINT	观点
IDEOLOGY	意识形态
APPRECIATED	受重视
PLEASUREABLE	令人愉悦
ENJOYED	喜欢过
HANGOUT	相处
ACQUAINT	结识
SOCIALIZE	社交
BUDDY	伙伴
FRIEND	朋友
ABILITY	能力
SKILL	技能
CAPABILITY	本领
ELECTROTALK	电流对话
ELECCOMM	电通信
ELECTRIC_SPEAK	电信号交流
ANIMATE	肢体动作
VISUALCOMM	视觉交流
BODY_LANGUAGE	肢体语言
LANDCRAWL	陆地爬行
WALK	行走
RUN	奔跑
WATERCRAWL	水中爬行
SWIM	游泳
AIRCRAWL??	空中爬行？？
FLY	飞行
ATHLETICABILITY	运动能力
BODYCONTROL	身体控制力
DEXTERITY	灵巧性
UNIQUE	独特
DIFFERING	不同
DIVERSE	多样
PRODIGY	天赋
PASSION	热爱
CELEBRATED_ABILITY	招牌本领
SKILLFIND	发掘技能
JUDGE	评判
COMPETE	竞争
BEAUTY	美
PREFER	偏好
EXPRESSION	表达
SELFSPECIALIZE	发展专长
COMPARTMENTALIZE?	分工？
ADAPT	适应
TRAIN	训练
ADAPTABILITY	适应能力
ROTATIONALSYMMETRY	旋转对称
REPULSE	厌恶
PAIN	痛苦
SUFFER	受苦
IMPENDINGDOOM	大难临头
LIMBDEATH	肢体死亡
LIMB_DEATH	肢体死亡
EARLYDEATH	早亡
BODYDEATH	身体死亡
CORE_DEATH	核心死亡
REJECT	拒绝
IGNORE	忽视
HATE	憎恨
SOUL	灵魂
CONSCIOUS	有意识
IMPACT	影响
PINNACLE	巅峰
GIFT	天赋
LIMITING	限制
INSUFFICIENT	不足
BARRIER	障碍
STUFF??	东西？？
INFORMATION	信息
WORDS	文字
TECHNOLOGIZE	技术化
UNDERSTOOD	理解
MASTERY	掌握
SPACETIME	时空
DIMENSIONS	维度
ENERGY	能量
UNIVERSAL_POTENTIAL	宇宙势能
ROBOT	机器人
COMPUTER :-)	计算机 :-)
THINKING_MACHINE	思考机器
NUCLEARFUSIONDEVICE	核聚变装置
FUSER	聚变器
ATOM_MACHINE	原子机器
UNTIL_AGAIN	下次再见
HELLO	你好
THANK_YOU	谢谢
"""

# Identical English guesses must remain identical in the localized dictionary.
INDEX_OVERRIDES: dict[int, str] = {}


def build_mapping() -> dict[str, str]:
    mapping: dict[str, str] = {}
    for line in PAIRS.strip().splitlines():
        source, translated = line.split("\t", 1)
        if source in mapping and mapping[source] != translated:
            raise ValueError(f"conflicting translation for {source!r}")
        mapping[source] = translated
    return mapping


def main() -> None:
    source_items = json.loads(SOURCE.read_text(encoding="utf-8"))
    mapping = build_mapping()
    unique_sources = {item["source_text"] for item in source_items}
    missing = sorted(unique_sources - mapping.keys())
    unused = sorted(mapping.keys() - unique_sources)
    assert not missing, f"missing source translations: {missing}"
    assert not unused, f"unused source translations: {unused}"

    output = [
        {
            "text_index": item["text_index"],
            "translated_text": INDEX_OVERRIDES.get(
                item["text_index"], mapping[item["source_text"]]
            ),
        }
        for item in source_items
    ]
    assert len(output) == len(source_items) == 639
    assert [item["text_index"] for item in output] == [
        item["text_index"] for item in source_items
    ]
    assert all(item["translated_text"].strip() for item in output)
    OUTPUT.write_text(
        json.dumps(output, ensure_ascii=False, indent=2) + "\n", encoding="utf-8"
    )
    NEWTERMS.write_text("", encoding="utf-8")
    print(
        json.dumps(
            {
                "items": len(output),
                "unique_sources": len(unique_sources),
                "missing": len(missing),
                "newterms": 0,
            },
            ensure_ascii=False,
        )
    )


if __name__ == "__main__":
    main()

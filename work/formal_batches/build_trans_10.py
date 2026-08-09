import json
from pathlib import Path


BASE = Path(__file__).resolve().parent
SOURCE = BASE / "src_10_dialogue_chunks_771_805.json"
OUTPUT = BASE / "trans_10.json"

translations = [
    "8 个氢",  # 0
    "{SPEAKER_COLLINS}{PART_000}巴蒂斯塔，{PART_001}帮我们配平这个 {SIG_N059}。",  # 1
    "{SPEAKER_BAUTISTA}{PART_000}有 8 个 {SIG_N056} 1 没出现在 {SIG_N058} 左边。",  # 2
    "{SPEAKER_COLLINS}{PART_000}所以答案就是这个？",  # 3
    "{SPEAKER_BAUTISTA}{PART_000}嗯。{PART_001}不确定。",  # 4
    "{SPEAKER_COLLINS}{PART_000}给我讲讲。",  # 5
    "{SPEAKER_BAUTISTA}{PART_000}还不知道这 8 个 {SIG_N056} 1 是怎么排列的。{PART_001}可能是 2 个 {SIG_N057}，每个含 4 个 {SIG_N056} 1，{PART_002}也可能是 4 个 {SIG_N057}，每个含 2 个 {SIG_N056} 1，{PART_003}或者是 8 个 {SIG_N056} 1。{PART_004}还得再看。",  # 6
    "氢气",  # 7
    "{SPEAKER_BAUTISTA}{PART_000}$animB4氢原子只有 1 个电子能与其他原子共用，{PART_001}也只容得下 1 个额外电子。{PART_002}所以自然界中的氢气以 {SIG_N057} 存在，其中有 2 个 {SIG_N056} 1。",  # 8
    "{SPEAKER_COLLINS}{PART_000}嗯，{PART_001}那么 8 个 {SIG_N056} 1 除以 2 就是……",  # 9
    "{SPEAKER_COLLINS}{PART_000}{SIG_N056} 1、{SIG_N056} 6、{SIG_N056} 8、{SIG_N056} 18——{PART_001}我把它们译成氢、碳、氧、硫，{PART_002}对吧？",  # 10
    "{SPEAKER_BAUTISTA}{PART_000}对。",  # 11
    "{SPEAKER_COLLINS}{PART_000}我在参考页签里找不到和这个 {SIG_N059} 对得上的内容。",  # 12
    "{SPEAKER_BAUTISTA}{PART_000}那就找出少了哪些 {SIG_N056}。",  # 13
    "{SPEAKER_COLLINS}{PART_000}这还是你的强项。{PART_001}所以说说吧，{PART_002}你发现了什么？",  # 14
    "{SPEAKER_BAUTISTA}{PART_000}左边有 36H、18S、6O，还少一个 {SIG_N057}，位于 {SIG_N012} 中。{PART_001}右边有 36H、18S、18O 和 6C。",  # 15
    "{SPEAKER_COLLINS}{PART_000}如果我没弄错，{PART_001}这就是说，{PART_002}少了 6 个 {SIG_N056} 6 和 12 个 {SIG_N056} 8，都在 {SIG_N058} 前面？",  # 16
    "{SPEAKER_BAUTISTA}{PART_000}嗯哼。",  # 17
    "{SPEAKER_COLLINS}{PART_000}看看怎么把它配平吧。",  # 18
    "CO2",  # 19
    "{SPEAKER_COLLINS}{PART_000}我们少了 6 个 {SIG_N056} 6 和 12 个 {SIG_N056} 8。{PART_001}会不会是少了 6 个 {SIG_N057}，每个含一个 {SIG_N056} 6 和 2 个 {SIG_N056} 8？",  # 20
    "A 组、B 组",  # 21
    "{SPEAKER_COLLINS}{PART_000}{SIG_N104} {SIG_N077} {SIG_N105}——{PART_001}也就是说，每个 {SIG_N011} 要么属于 {SIG_N104}，要么属于 {SIG_N105}。{PART_002}我们得判断这 4 个词分别更适合哪一组。",  # 22
    "有形？",  # 23
    "{SPEAKER_AKERS}{PART_000}要定义 {SIG_N104} 和 {SIG_N105}，我们只有上次传输里的例子可参考。{PART_001}偏偏这几个又没列在里面……",  # 24
    "{SPEAKER_BAUTISTA}{PART_000}这 2 个像是柯林斯负责的词。",  # 25
    "{SPEAKER_COLLINS}{PART_000}嗯……",  # 26
    "{SPEAKER_AKERS}{PART_000}看，她开始动脑筋了！{PART_001}这些肯定是柯林斯负责的词。{PART_002}又是语法？",  # 27
    "{SPEAKER_COLLINS}{PART_000}不太可能。{PART_001}这 2 个类别……{PART_002}难道是？{PART_003}也许不是，{PART_004}也许不是。",  # 28
    "{SPEAKER_AKERS}{PART_000}快说吧。",  # 29
    "{SPEAKER_COLLINS}{PART_000}上次的传输——{PART_001}所有 {SIG_N105} 词似乎都更实在，{PART_002}更具体，{PART_003}也更固定。{PART_004}{SIG_N023} 永远是 {SIG_N023}。{PART_005}{SIG_N093} 永远是 {SIG_N093}。{PART_006}而第一组呢，{PART_007}各位，{PART_008}看看。",  # 30
    "{SPEAKER_AKERS}{PART_000}{SIG_N002}、{SIG_N003}、{SIG_N019}、{SIG_N011}——{PART_001}呃，我大概懂你的意思了。{PART_002}这些词没那么……",  # 31
    "{SPEAKER_COLLINS}{PART_000}有形。{PART_001}没那么有形。",  # 32
    "{SPEAKER_AKERS}{PART_000}那这次的传输呢？",  # 33
    "{SPEAKER_COLLINS}{PART_000}我觉得这些全都有形。{PART_001}这是我最有把握的假说。",  # 34
    "A 组、B 组——2",  # 35
    "{SPEAKER_AKERS}{PART_000}你看，{SIG_N104} 和 {SIG_N105} 这边有进展了。{PART_001}最后两个我们已经知道，{PART_002}之前的示例传输里给过。",  # 36
    "{SPEAKER_COLLINS}{PART_000}我觉得 {SIG_N083} 属于 {SIG_N104}，{PART_001}和 {SIG_N078} 一样。{PART_002}而 {SIG_N063} 感觉很实在，{PART_003}定义明确，{PART_004}肯定是有形的。",  # 37
    "{SPEAKER_AKERS}{PART_000}好吧，{PART_001}等着看吧。",  # 38
    "列出 2 种用法",  # 39
    "{SPEAKER_COLLINS}{PART_000}他们列出了 {SIG_N025} 的 2 种不同用法。{PART_001}{SIG_N105} {SIG_N025} 用在严格的数值意义上。{PART_002}而 {SIG_N104} {SIG_N025} 用来表示没有写出的细节。",  # 40
    "{SPEAKER_AKERS}{PART_000}{SIG_N104} {SIG_N025} 就像在说：“嘿！后面还有，但我们懒得写了。”",  # 41
    "{SPEAKER_COLLINS}{PART_000}正是。{PART_001}那最后一句该用哪个？",  # 42
    "2 种用法——2",  # 43
    "{SPEAKER_AKERS}{PART_000}这和上次的传输一样，{PART_001}对吧？",  # 44
    "{SPEAKER_COLLINS}{PART_000}这次比较的是 {SIG_N104} {SIG_N047} 和 {SIG_N105} {SIG_N047}。",  # 45
    "{SPEAKER_AKERS}{PART_000}{SIG_N105} {SIG_N047} 是画圆的方程。{PART_001}之前见过。",  # 46
    "{SPEAKER_COLLINS}{PART_000}而 {SIG_N104} {SIG_N047} 指的是有些近似球形或圆形的物体。",  # 47
    "{SPEAKER_AKERS}{PART_000}他们真是这个意思？",  # 48
    "{SPEAKER_COLLINS}{PART_000}我想应该是，{PART_001}那就只剩最后一个问题：{PART_002}{SIG_N090} 是 {SIG_N104} {SIG_N047} 吗？",  # 49
    "1000 个信号",  # 50
    "{SPEAKER_BAUTISTA}{PART_000}这次传输用了 1000 多个信号。",  # 51
    "{SPEAKER_AKERS}{PART_000}所以才加载了这么久？！",  # 52
    "{SPEAKER_COLLINS}{PART_000}1000 个信号乘以 0.8066 秒……{PART_001}一共要多久？",  # 53
    "{SPEAKER_BAUTISTA}{PART_000}大约 13.5 分钟。",  # 54
    "{SPEAKER_AKERS}{PART_000}他们为什么需要这么多信号？",  # 55
    "{SPEAKER_COLLINS}{PART_000}说明这个 {SIG_N053} 一定在描绘非常精细的东西。",  # 56
    "一样吗？",  # 57
    "{SPEAKER_COLLINS}{PART_000}这次回应有 3 个选项：{PART_001}{SIG_N034}、{SIG_N035}，或者 {SIG_N022} {SIG_N004}。",  # 58
    "{SPEAKER_AKERS}{PART_000}什么时候会用 {SIG_N022} {SIG_N004}？",  # 59
    "{SPEAKER_BAUTISTA}{PART_000}这还用问。",  # 60
    "{SPEAKER_AKERS}{PART_000}算了，我好像懂了。{PART_001}如果它们彼此不是 {SIG_N034} 或 {SIG_N035} 的话。",  # 61
    "{SPEAKER_BAUTISTA}{PART_000}很好。",  # 62
    "多少颗雀斑",  # 63
    "{SPEAKER_AKERS}{PART_000}凯莉，{PART_001}有件事我早就想问你了。",  # 64
    "{SPEAKER_COLLINS}{PART_000}哦，是语言学问题吗？",  # 65
    "{SPEAKER_AKERS}{PART_000}你有多少颗雀斑？",  # 66
    "{SPEAKER_COLLINS}{PART_000}你……{PART_001}你觉得我数过吗？",  # 67
    "{SPEAKER_BAUTISTA}{PART_000}你没数过？{PART_001}为什么？",  # 68
    "{SPEAKER_COLLINS}{PART_000}你也这样，{PART_001}巴蒂斯塔？{PART_002}别说得好像数别人脸上的雀斑很正常一样！",  # 69
    "{SPEAKER_AKERS}{PART_000}这是你自己的脸，{PART_001}为什么不数！",  # 70
    "{SPEAKER_COLLINS}{PART_000}我从没觉得这很重要！",  # 71
    "{SPEAKER_BAUTISTA}{PART_000}很简单。{PART_001}找一块雀斑密度看起来处于平均水平的皮肤，数数有多少颗，{PART_002}再乘以全身有雀斑的皮肤大约相当于多少个这样的区域。",  # 72
    "{SPEAKER_AKERS}{PART_000}怎么选出雀斑密度处于平均水平的那块？",  # 73
    "{SPEAKER_BAUTISTA}{PART_000}凭直觉。{PART_001}看东西时，直觉比刻意分析更准。",  # 74
    "{SPEAKER_AKERS}{PART_000}我觉得她鼻梁上那块就挺平均。",  # 75
    "{SPEAKER_COLLINS}{PART_000}我就在这里，{PART_001}两位……",  # 76
    "{SPEAKER_BAUTISTA}{PART_000}不，那里太密了。{PART_001}她颧骨附近的密度比较平均。",  # 77
    "{SPEAKER_AKERS}{PART_000}哦，说得对！{PART_001}就选那里！",  # 78
    "{SPEAKER_COLLINS}{PART_000}这也太让人不自在了……",  # 79
    "{SPEAKER_AKERS}{PART_000}好，我在心里算，{PART_001}你也自己算。{PART_002}这次传输结束后，{PART_003}我们再说各自估了多少。",  # 80
    "{SPEAKER_BAUTISTA}{PART_000}成交。",  # 81
    "{SPEAKER_COLLINS}{PART_000}要不你们别再盯着我的脸，搞得像在检查牲口一样？",  # 82
    "{SPEAKER_AKERS}{PART_000}（小声）一、二、三……{PART_001}喂！{PART_002}别动！",  # 83
    "雀斑统计",  # 84
    "{SPEAKER_AKERS}{PART_000}好了，巴蒂斯塔，{PART_001}我把答案写下来了。",  # 85
    "{SPEAKER_COLLINS}{PART_000}我还以为你们两个已经忘了这事。",  # 86
    "{SPEAKER_BAUTISTA}{PART_000}我算完了。",  # 87
    "{SPEAKER_AKERS}{PART_000}你先说，我再给你看我写的答案。",  # 88
    "{SPEAKER_BAUTISTA}{PART_000}309 颗雀斑。{PART_001}你纸上写的多少？",  # 89
    "{SPEAKER_COLLINS}{PART_000}不可能！",  # 90
    "{SPEAKER_BAUTISTA}{PART_000}给我看看。",  # 91
    "{SPEAKER_COLLINS}{PART_000}天啊，他说的是真的！{PART_001}你们两个串通好了？",  # 92
    "{SPEAKER_AKERS}{PART_000}我拿命发誓，{PART_001}这就是我算出来的！{PART_002}我就是怕你不信才写下来；{PART_003}绝对没串通！",  # 93
    "{SPEAKER_BAUTISTA}{PART_000}呵呵。{PART_001}凯莉，{PART_002}经我们专业判断，你脸上有 309 颗雀斑。",  # 94
    "{SPEAKER_COLLINS}{PART_000}太好了。{PART_001}我一直都特别想知道……",  # 95
    "是半径，不是直径",  # 96
    "{SPEAKER_BAUTISTA}{PART_000}5575 {SIG_N073} 3——{PART_001}直觉告诉我，这太大了。",  # 97
    "{SPEAKER_AKERS}{PART_000}可我们每一步都做对了，{PART_001}不是吗？{PART_002}把 11 {SIG_N073} 当作球的半径代入，结果就是 5575。",  # 98
    "{SPEAKER_BAUTISTA}{PART_000}我明白了。{PART_001}11 {SIG_N073} 是直径，{PART_002}不是半径。",  # 99
    "{SPEAKER_AKERS}{PART_000}哎呀，{PART_001}那就除以二，再试一次。",  # 100
    "意义由听者构建",  # 101
    "{SPEAKER_AKERS}{PART_000}快结束了。{PART_001}我想再过一周，{PART_002}全世界都会看到这则讯息。",  # 102
    "{SPEAKER_COLLINS}{PART_000}当然，{PART_001}他们也会看到我们的译文。",  # 103
    "{SPEAKER_AKERS}{PART_000}其实，{PART_001}我担心的就是这个。",  # 104
    "{SPEAKER_BAUTISTA}{PART_000}担心别人发现你漏掉了什么？",  # 105
    "{SPEAKER_AKERS}{PART_000}什么？{PART_001}我为什么要怕这个？{PART_002}不是，{PART_003}我是担心我们所有人都漏掉了什么。{PART_004}我怕这则讯息里有些东西，我们所有人，{PART_005}乃至全人类，{PART_006}都可能会漏掉。",  # 106
    "{SPEAKER_COLLINS}{PART_000}有些思想，{PART_001}可能只有他们自己才能理解。",  # 107
    "{SPEAKER_AKERS}{PART_000}那更吓人。{PART_001}有些东西根本无法翻译。",  # 108
    "{SPEAKER_COLLINS}{PART_000}恐怕比这还棘手，{PART_001}艾伦。{PART_002}我们的大脑，{PART_003}以及他们身上相当于大脑的器官，{PART_004}在某些方面可能完全不兼容。{PART_005}双方的认知结构也许都决定了各自只能理解某些信息。",  # 109
    "{SPEAKER_AKERS}{PART_000}一道无法跨越的语言障碍——{PART_001}谢谢你，凯莉，{PART_002}我正需要听到这个……",  # 110
    "{SPEAKER_COLLINS}{PART_000}那我再说件能让你振作的事：{PART_001}所有交流都是这样。{PART_002}有人把大脑想成水桶，{PART_003}语言则是把信息从一个桶运到另一个桶的工具。{PART_004}但事实并非如此——{PART_005}信息是由听者构建的。",  # 111
    "{SPEAKER_AKERS}{PART_000}怎么构建？{PART_001}这到底是什么意思？{PART_002}你想想这有多说不通，{PART_003}凯莉。{PART_004}你刚解释的理论，{PART_005}不就是你的话传进了我的大脑吗？",  # 112
    "{SPEAKER_COLLINS}{PART_000}不，{PART_001}是我的大脑把这个思想在我心里的模型，{PART_002}转化成了我们早已约定好的符号。{PART_003}说出口的话，只是我脑中那个思想的摹本。",  # 113
    "{SPEAKER_AKERS}{PART_000}可你还是把这个“摹本”倒进了我脑子里的水桶。",  # 114
    "{SPEAKER_COLLINS}{PART_000}不，{PART_001}我只是把符号交给你，{PART_002}再由你的大脑重新解读。{PART_003}是你的大脑在构建意义。{PART_004}作为说话的人，{PART_005}我会尽量用符号引导你重建出同样的心理模型，{PART_006}但说到底，{PART_007}结果不由我控制。",  # 115
    "{SPEAKER_AKERS}{PART_000}外星人也是一样。{PART_001}不管多努力，{PART_002}他们能给我们的也只有外在的符号。{PART_003}那我就更害怕我们会误解什么，{PART_004}会不可避免地把他们传来的知识还原错。{PART_005}这也让我想知道，{PART_006}怎样才能跨越这种生物障碍？{PART_007}我们究竟要怎样，{PART_008}真正地、{PART_009}完美地、{PART_010}准确地传达任何东西？",  # 116
    "{SPEAKER_COLLINS}{PART_000}我不知道这是否可能，{PART_001}除非能用某种方法复制大脑。",  # 117
    "{SPEAKER_BAUTISTA}{PART_000}有一个办法。",  # 118
    "{SPEAKER_COLLINS}{PART_000}巴蒂斯塔？",  # 119
    "{SPEAKER_BAUTISTA}{PART_000}意识就是障碍。{PART_001}如果意识可以共享，{PART_002}意义也能共享，{PART_003}不需要符号，也不需要重构。{PART_004}意义会被直接感知。",  # 120
    "做得太多",  # 121
    "{SPEAKER_AKERS}{PART_000}{PLAYER_NAME}，{PART_001}你到底在干什么？！",  # 122
    "{SPEAKER_COLLINS}{PART_000}埃克斯，{PART_001}别这么没礼貌。{PART_002}有话直说。",  # 123
    "{SPEAKER_AKERS}{PART_000}{PLAYER_NAME}，{PART_001}你做过头了！{PART_002}他们已经定义过 {SIG_N016} 0 和 {SIG_N016} 3。{PART_003}不用把里面的东西也写出来。",  # 124
    "{SPEAKER_COLLINS}{PART_000}怎么了？{PART_001}艾伦？{PART_002}你还好吗？",  # 125
    "{SPEAKER_AKERS}{PART_000}我们知道了人类并不孤单。{PART_001}我们知道外面还有别的存在，{PART_002}有意识，{PART_003}有意义，{PART_004}也有信念。{PART_005}我们知道了他们的模样，{PART_006}知道了怎样找到他们的家园，{PART_007}知道他们是什么，{PART_008}知道他们如何演化，{PART_009}如何生活，{PART_010}相信什么，{PART_011}感受什么，{PART_012}做些什么，{PART_013}也知道了他们科学的巅峰。{PART_014}可是……{PART_015}我不知道。",  # 126
    "{SPEAKER_COLLINS}{PART_000}怎么了？",  # 127
    "{SPEAKER_AKERS}{PART_000}我现在该怎么办？{PART_001}知道了这一切，{PART_002}往后余生又该怎么过？{PART_003}我该怎么继续生活？{PART_004}窥见了外面的世界以后，{PART_005}我怎么还能继续过这种平凡的人类生活？{PART_006}……",  # 128
    "{SPEAKER_COLLINS}{PART_000}艾伦，{PART_001}我们了解的关于他们的一切，{PART_002}也许有一天他们也能从我们身上了解到。{PART_003}而且很明显——{PART_004}他们想知道。{PART_005}迫切地想。{PART_006}{SIG_N136} 和另外几十个协助制造陨石的 {SIG_N129}，{PART_007}那些让这次任务成为可能的存在，{PART_008}以及支撑这一切的文化与文明，{PART_009}全都是为了和我们交流。{PART_010}他们迫切地想知道我们是否存在，{PART_011}想知道宇宙中是否还有和他们一样的存在。{PART_012}而我们的确存在，{PART_013}就在这颗名为地球的淡蓝色小点上。{PART_014}你说人类生活平淡无奇，{PART_015}可那恰恰是他们想了解的一切，{PART_016}艾伦。{PART_017}正如外星生命令我们着迷，我们也会令他们着迷。{PART_018}{SIG_N045} 和 {SIG_N046}。{PART_019}我们都属于宇宙中一种罕见的存在：{PART_020}{SIG_N101}。",  # 129
    "{SPEAKER_AKERS}{PART_000}道理我都懂，心里却接受不了。{PART_001}我还是忍不住觉得迷茫。{PART_002}我依然不知道该做什么。{PART_003}不知道怎样才能走出这一切。{PART_004}这简直是世间万物收到过最美的礼物，{PART_005}也许是整个银河系中最珍贵的一份爱。{PART_006}我究竟该怎么继续生活？{PART_007}该怎么走下去？",  # 130
    "{SPEAKER_COLLINS}{PART_000}我想你知道答案，{PART_001}艾伦。{PART_002}整则讯息里都在反复出现：{PART_003}{SIG_N044} 1。{PART_004}……{PART_005}如果平凡的人类生活让你苦恼，{PART_006}也许你可以把这份平凡分享给懂得珍惜它的存在。{PART_007}分享给那些迫切想知道自己并不孤单的存在。{PART_008}艾伦，{PART_009}他们想要 {SIG_N044} 1。{PART_010}他们想要回应。{PART_011}他们想知道我们的一切。{PART_012}我们是什么，{PART_013}长什么样，{PART_014}我们是谁，{PART_015}爱什么，{PART_016}相信什么，{PART_017}认定什么是真实，{PART_018}身为人类意味着什么。",  # 131
    "{SPEAKER_AKERS}{PART_000}{SIG_N044} 1……{PART_001}……{PART_002}谢谢你，凯莉。{PART_003}我知道接下来该怎么做了。",  # 132
    "第 31 周结束",  # 133
    "{SPEAKER_COLLINS}{PART_000}这周，{PART_001}{PLAYER_NAME} 定义了 {SIG_N128}、{SIG_N129} 和 {SIG_N130}。",  # 134
    "{SPEAKER_DOPPLER}{PART_000}只有三个词？",  # 135
    "{SPEAKER_AKERS}{PART_000}话说，{PART_001}你们材料团队最近又做了什么？{PART_002}有重大进展吗？",  # 136
    "{SPEAKER_DOPPLER}{PART_000}$animD01没有，{PART_001}但复杂的材料分析，{PART_002}本来就需要时间。{PART_003}$animD22这可是交流。{PART_004}我以为会快一些，{PART_005}$animD04像说话一样。",  # 137
    "{SPEAKER_COLLINS}{PART_000}这是单向交流，{PART_001}借助的是无线电波。{PART_002}$animD21要是他们真有一个在这里，{PART_003}当然会快得多。{PART_004}那样我们可以指向彼此都看得到的东西，也能观察肢体语言。",  # 138
    "{SPEAKER_DOPPLER}{PART_000}$animD07可我们没这个条件。",  # 139
    "{SPEAKER_COLLINS}{PART_000}没错，长官。{PART_001}这更像是从文献和泥板中分析古代语言。",  # 140
    "{SPEAKER_BAUTISTA}{PART_000}反正他们可能早就全死了。",  # 141
    "{SPEAKER_COLLINS}{PART_000}$animD05别这么说！！",  # 142
    "{SPEAKER_AKERS}{PART_000}$animD19我倒真想亲眼见一个，{PART_001}活生生的！{PART_002}或者他们身上相当于肉身的东西。{PART_003}光看那些 {SIG_N053} 传输很难判断。{PART_004}但太空实在太大，{PART_005}所以他们没亲自来，{PART_006}而是造出陨石探测器，{PART_007}发往可能孕育生命的星球，{PART_008}比如地球。",  # 143
    "{SPEAKER_BAUTISTA}{PART_000}262,122 颗陨石。$animD20",  # 144
    "{SPEAKER_AKERS}{PART_000}$animD19想象一下，外星人开着车跑遍 262,000 颗行星？{PART_001}这一趟的油钱可不得了……",  # 145
    "{SPEAKER_BAUTISTA}{PART_000}$animD20火箭不用汽油。",  # 146
    "{SPEAKER_AKERS}{PART_000}$animD19你以为我不知道？{PART_001}别忘了，{PART_002}我参加过阿波罗计划！",  # 147
    "{SPEAKER_BAUTISTA}{PART_000}$animD02你又不是火箭科学家。",  # 148
    "{SPEAKER_AKERS}{PART_000}可我身边全是火箭科学家！",  # 149
    "{SPEAKER_DOPPLER}{PART_000}$animD01好了，{PART_001}好了，{PART_002}我们收尾吧。{PART_003}我相信这周的翻译工作干得不错。{PART_004}我会……{PART_005}调整一下预期。{PART_006}现在回去，{PART_007}好好睡觉。{PART_008}明早我要看到你们全都精神十足。",  # 150
    "{SPEAKER_AKERS}{PART_000}晚安，多普！",  # 151
    "{SPEAKER_COLLINS}{PART_000}各位，{PART_001}做个好梦！",  # 152
    "{SPEAKER_BAUTISTA}{PART_000}晚安。",  # 153
    "第 32 周结束",  # 154
    "{SPEAKER_BAUTISTA}{PART_000}够惨的。",  # 155
    "{SPEAKER_DOPPLER}{PART_000}$animD21这是预兆，{PART_001}警告，{PART_002}还是威胁？",  # 156
    "{SPEAKER_COLLINS}{PART_000}这只是对 {SIG_N101} 的肯定。{PART_001}也是对我们彼此相通之处的肯定。{PART_002}陨石的传输用语相当简短。{PART_003}这门语言有时很精确，{PART_004}有时又相对或抽象。{PART_005}但其中表达的思想很准确，{PART_006}只是常常很干巴，也很讲逻辑。",  # 157
    "{SPEAKER_DOPPLER}{PART_000}$animD01难道是因为他们本身就这么冷冰冰、只讲逻辑？",  # 158
    "{SPEAKER_COLLINS}{PART_000}很难说。{PART_001}这可能源于他们的文化，也可能只是因为……{PART_002}怎么说呢，{PART_003}要写出一则能跨越漫长宇宙岁月的讯息。",  # 159
    "{SPEAKER_DOPPLER}{PART_000}$animD21你是说，为了更容易翻译，他们去掉了一些文化色彩。{PART_001}你们以前谈过这件事。",  # 160
    "{SPEAKER_COLLINS}{PART_000}没错，{PART_001}不过我不太相信他们真的冷冰冰、缺乏感情。{PART_002}很难想象一个漠不关心的物种会制作星际讯息。{PART_003}他们最初对 {SIG_N127} 的定义给了我希望。{PART_004}他们的确会在乎，{PART_005}也在努力表达出来。",  # 161
    "{SPEAKER_DOPPLER}{PART_000}$animD22那为什么不一上来就说“我不希望人类死掉”？",  # 162
    "{SPEAKER_COLLINS}{PART_000}“希望”——{PART_001}我们有对应的词吗？{PART_002}要表达“希望”，{PART_003}得先建立视角、{PART_004}选择、{PART_005}欲望。{PART_006}$animD21还有善，{PART_007}恶。{PART_008}我们也许快讲到这些了。",  # 163
    "{SPEAKER_DOPPLER}{PART_000}只是听起来很阴沉——{PART_001}{SIG_N110} {SIG_N130} {SIG_N133} {SIG_N085}。",  # 164
    "{SPEAKER_COLLINS}{PART_000}他们就是这样表达的。{PART_001}先提出一组前提，{PART_002}再推出结论。{PART_003}或者很多时候，{PART_004}让我们替他们补上结论。",  # 165
    "{SPEAKER_DOPPLER}{PART_000}用来证明我们理解了。",  # 166
    "{SPEAKER_COLLINS}{PART_000}就拿 {SIG_N110} {SIG_N130} {SIG_N133} {SIG_N085} 来说。{PART_001}他们先提出前提：{SIG_N110} {SIG_N101} {SIG_N128} {SIG_N133}。{PART_002}再提出另一个前提：{SIG_N130} {SIG_N099} {SIG_N101} {SIG_N128}。{PART_003}{SIG_N036}，{SIG_N110} {SIG_N130} {SIG_N133} {SIG_N085}。{PART_004}读起来就像数学证明。{PART_005}所有长方形都有 4 条边，{PART_006}所有正方形都是长方形，{PART_007}所以——",  # 167
    "{SPEAKER_DOPPLER}{PART_000}$animD00所有正方形都有 4 条边。{PART_001}$animD21你想说明什么？",  # 168
    "{SPEAKER_COLLINS}{PART_000}多普勒，{PART_001}对那次传输，正确的回应是：{PART_002}{SIG_N110} {SIG_N129} {SIG_N133} {SIG_N085}。{PART_003}你觉得这是为什么？{PART_004}他们为什么要引导我们这样描述他们自己？{PART_005}那不是预兆，{PART_006}不是警告，{PART_007}也不是威胁。{PART_008}而是在赞颂我们之间的共同之处。{PART_009}他们想和我们产生共鸣。",  # 169
    "{SPEAKER_DOPPLER}{PART_000}$animD16他们连我们是否存在都不知道，怎么可能和我们产生共鸣？",  # 170
    "{SPEAKER_COLLINS}{PART_000}只要我们存在，{PART_001}$animD21他们就知道我们是有生命的——{PART_002}是宇宙中最罕见的存在之一。",  # 171
    "第 33 周结束",  # 172
    "{SPEAKER_DOPPLER}{PART_000}翻译小组，{PART_001}我们继续——{PART_002}$animD15你们看起来不一样了。",  # 173
    "{SPEAKER_BAUTISTA}{PART_000}嗯？",  # 174
    "{SPEAKER_DOPPLER}{PART_000}连你也是，{PART_001}巴蒂斯塔。{PART_002}你们好像有点……{PART_003}$animD16惆怅？{PART_004}不，{PART_005}像惆怅，{PART_006}但又更快乐。{PART_007}$animD06这周发生了什么？{PART_008}怎么全是一副豁然开朗的样子？",  # 175
    "{SPEAKER_COLLINS}{PART_000}我们认识了一个人。{PART_001}她是 {SIG_N150}，{PART_002}是那些 {SIG_N044} {SIG_N043} 的制造者，{PART_003}也是一个费尽周折也要说出这句话的人：{PART_004}{SIG_N046} {SIG_N131} {SIG_N134} {SIG_N085} {SIG_N120} {SIG_N121} {SIG_N127} {SIG_N085}。",  # 176
    "{SPEAKER_DOPPLER}{PART_000}$animD22能给我翻译一下吗？",  # 177
    "{SPEAKER_COLLINS}{PART_000}那是 {PLAYER_NAME} 的工作，{PART_001}长官。",  # 178
    "{SPEAKER_DOPPLER}{PART_000}……{PART_001}这个人有名字吗？",  # 179
    "{SPEAKER_COLLINS}{PART_000}还是得靠 {PLAYER_NAME}。{PART_001}我们的银河笔友被命名为 {SIG_N136}。",  # 180
    "{SPEAKER_DOPPLER}{PART_000}明白了。{PART_001}$animD00我没什么好说的。{PART_002}我承认，上周提到 {SIG_N133}，{PART_003}$animD02让我心里很沉重。",  # 181
    "{SPEAKER_COLLINS}{PART_000}那不是威胁，{PART_001}多普勒。{PART_002}而是对 {SIG_N101} 的肯定。",  # 182
    "{SPEAKER_DOPPLER}{PART_000}$animD04我现在明白了。{PART_001}$animD19也看到了你们的神情。{PART_002}$animD20那间屋里正在发生一件了不起的事。{PART_003}$animD21继续把你们的发现告诉我。{PART_004}$animD22等到时机成熟，{PART_005}全地球都会想听到这些。{PART_006}而就我个人而言，{PART_007}我也要依靠你们四个，{PART_008}去理解一件对我来说无比珍贵的东西。{PART_009}$animD23我们收到了一份礼物。{PART_010}我们有责任把它传给全世界。{PART_011}我想，其他人也会从中感到安宁。{PART_012}$animD17也许那一天到来时，我们每个人都会多一分豁然开朗。",  # 183
    "{SPEAKER_COLLINS}{PART_000}一定会的，{PART_001}长官。",  # 184
    "{SPEAKER_DOPPLER}{PART_000}$animD18好了。{PART_001}还有工作没做完。",  # 185
    "{SPEAKER_COLLINS}{PART_000}我们会做到的。",  # 186
    "{SPEAKER_DOPPLER}{PART_000}$animD22我知道你们会。{PART_001}$animD24祝顺利。",  # 187
    "第 34 周结束",  # 188
    "{SPEAKER_DOPPLER}{PART_000}翻译小组，{PART_001}$animD23见到各位很高兴。",  # 189
    "{SPEAKER_AKERS}{PART_000}我们才更高兴，{PART_001}长官。",  # 190
    "{SPEAKER_DOPPLER}{PART_000}开始吧。{PART_001}$animD06有什么发现？",  # 191
    "{SPEAKER_COLLINS}{PART_000}这周的重点应该是 {SIG_N144}。",  # 192
    "{SPEAKER_AKERS}{PART_000}{SIG_N144}？{PART_001}$animD19我可不是这么理解的！",  # 193
    "{SPEAKER_COLLINS}{PART_000}那我换个说法：{PART_001}熟悉的关系——{PART_002}{SIG_N142} 和 {SIG_N143}。",  # 194
    "{SPEAKER_DOPPLER}{PART_000}$animD18这倒让我挑起了眉。{PART_001}$animD04当然，只是打个比方。{PART_002}$animD05有人说我说话时手势相当丰富。{PART_003}$animD00总之，{PART_004}{SIG_N142} 和 {SIG_N143}——{PART_005}$animD22这是你的解释，{PART_006}{PLAYER_NAME}？{PART_007}那我相信。",  # 195
    "{SPEAKER_COLLINS}{PART_000}如果你接受这个解释，{PART_001}那接下来的发现应该会让你高兴。",  # 196
    "{SPEAKER_DOPPLER}{PART_000}$animD21什么发现？",  # 197
    "{SPEAKER_COLLINS}{PART_000}{SIG_N045} {SIG_N114} {SIG_N142} {SIG_N023} 是 9。",  # 198
    "{SPEAKER_DOPPLER}{PART_000}9，{PART_001}是吗。{PART_002}$animD02嗯……{PART_003}$animD07不是 8？",  # 199
    "{SPEAKER_AKERS}{PART_000}我也是这么说的！！",  # 200
    "{SPEAKER_BAUTISTA}{PART_000}你还指望他们把集体的 {SIG_N142} {SIG_N023} 统一起来。",  # 201
    "{SPEAKER_DOPPLER}{PART_000}也是。{PART_001}$animD20可就差一点。",  # 202
    "{SPEAKER_COLLINS}{PART_000}我们还知道了 {SIG_N136} 的 {SIG_N142} {SIG_N023}。",  # 203
    "{SPEAKER_AKERS}{PART_000}他只有 7 个。",  # 204
    "{SPEAKER_DOPPLER}{PART_000}$animD08比 {SIG_N114} 少——{PART_001}等等，{PART_002}$animD15“他”？",  # 205
    "{SPEAKER_AKERS}{PART_000}一不小心说顺口了。",  # 206
    "{SPEAKER_DOPPLER}{PART_000}你把 {SIG_N136} 想象成男性？",  # 207
    "{SPEAKER_AKERS}{PART_000}也不完全是。{PART_001}要是他们的性别在生物学上和我们差不多，我才会吃惊。{PART_002}不过我好像把 {SIG_N136} 当成了朋友，{PART_003}大概吧。{PART_004}感觉自己越来越了解对方了，{PART_005}或者他，{PART_006}随便怎么说。",  # 208
    "{SPEAKER_COLLINS}{PART_000}$animD21虽然目前我们几乎不了解 {SIG_N136} 的个人情况，{PART_001}我却觉得自己很熟悉她，{PART_002}只是通过这些传输，{PART_003}通过她选择告诉我们的内容。{PART_004}她和其他人很可能都不知道我们的存在，{PART_005}可她还是告诉我们如何找到他们的行星，{PART_006}还在努力说明他们是什么——{PART_007}以及他们是谁。",  # 209
    "{SPEAKER_DOPPLER}{PART_000}$animD09那你们四个算是交到朋友了。{PART_001}我们很快再谈。{PART_002}继续翻译。{PART_003}也谢谢你们总能从一切事物中构建意义。",  # 210
    "第 35 周结束",  # 211
    "{SPEAKER_DOPPLER}{PART_000}$animD04欢迎，{PART_001}欢——",  # 212
    "{SPEAKER_COLLINS}{PART_000}多普勒，{PART_001}{SIG_N129} 是 2 个 {SIG_N131}。",  # 213
    "{SPEAKER_DOPPLER}{PART_000}等等，{PART_001}$animD15这是什么意思？",  # 214
    "{SPEAKER_COLLINS}{PART_000}我一直很难理解，{PART_001}从生物学上说，{PART_002}现在也还是一头雾水。{PART_003}但根据 {PLAYER_NAME} 的定义、{PART_004}他们的回应传输，{PART_005}以及一些 {SIG_N053} 传输，{PART_006}目前最合理的理解是：他们是 2 个 {SIG_N131}，{PART_007}却像 1 个一样运作。",  # 215
    "{SPEAKER_DOPPLER}{PART_000}$animD21怎么做到的？",  # 216
    "{SPEAKER_COLLINS}{PART_000}他们共同经历一个完整的生命周期。{PART_001}同时，{PART_002}{SIG_N144} {SIG_N085}，{PART_003}然后 {SIG_N132} {SIG_N085}，{PART_004}{SIG_N134} {SIG_N085}，{PART_005}{SIG_N133} {SIG_N085}。",  # 217
    "{SPEAKER_DOPPLER}{PART_000}{SIG_N144} {SIG_N085}——？{PART_001}$animD02你在说什么？{PART_002}这是什么语法？",  # 218
    "{SPEAKER_AKERS}{PART_000}是外星语的语法。{PART_001}$animD19可以把 {SIG_N085} 理解成启动它前面的 {SIG_N080}。",  # 219
    "{SPEAKER_COLLINS}{PART_000}抱歉，{PART_001}这实在——",  # 220
    "{SPEAKER_DOPPLER}{PART_000}$animD21不用道歉。{PART_001}继续说，{PART_002}柯林斯。",  # 221
    "{SPEAKER_COLLINS}{PART_000}他们共同度过一个完整的生命周期。{PART_001}还有一件事，{PART_002}他们分享的一次传输。{PART_003}我越想，{PART_004}越觉得其中意味深长。{PART_005}读一下。",  # 222
    "{SPEAKER_DOPPLER}{PART_000}$animD06咳。{PART_001}“{SIG_N147} {SIG_N135} {SIG_N114} {SIG_N004} 6653 {SIG_N070} {SIG_N002} {SIG_N002}。”",  # 223
    "{SPEAKER_AKERS}{PART_000}{SIG_N002} {SIG_N002} 那些就跳过吧。{PART_001}相当于句号。",  # 224
    "{SPEAKER_DOPPLER}{PART_000}$animD19好。{PART_001}$animD06“{SIG_N146} {SIG_N135} {SIG_N114} {SIG_N004} 288 {SIG_N070}——{PART_002}{SIG_N146} {SIG_N135} {SIG_N033} {SIG_N147} {SIG_N135} {SIG_N036}”，嗯。{PART_003}嗯……？",  # 225
    "{SPEAKER_COLLINS}{PART_000}继续读。",  # 226
    "{SPEAKER_DOPPLER}{PART_000}288……{PART_001}$animD02所以当 {SIG_N146} {SIG_N133}……{PART_002}{SIG_N129} {SIG_N133} {SIG_N085}。{PART_003}$animD16这真是……{PART_004}不可思议。{PART_005}$animD01也让人摸不着头脑。",  # 227
    "{SPEAKER_COLLINS}{PART_000}看来你的感受和我一样。",  # 228
    "{SPEAKER_DOPPLER}{PART_000}$animD17这事我得睡一觉再想。{PART_001}大家也该睡了。{PART_002}$animD07那就去睡吧。{PART_003}让我们 {SIG_N085} 地睡觉。{PART_004}$animD19我说对了吗？",  # 229
    "{SPEAKER_AKERS}{PART_000}说对了，{PART_001}老大。",  # 230
    "{SPEAKER_COLLINS}{PART_000}我现在恐怕睡不着。",  # 231
    "{SPEAKER_DOPPLER}{PART_000}我就知道，{PART_001}$animD21柯林斯。{PART_002}那就去做你需要做的事，{PART_003}但也要准备好继续翻译。{PART_004}$animD24下次见。",  # 232
    "{SPEAKER_AKERS}{PART_000}晚安，多普。",  # 233
    "{SPEAKER_BAUTISTA}{PART_000}各位晚安。",  # 234
    "第 36 周结束",  # 235
    "{SPEAKER_DOPPLER}{PART_000}翻译小组，{PART_001}一如既往，很高兴见到各位。{PART_002}上周的发现现在更清楚了吗？{PART_003}{SIG_N146}、{SIG_N147}？",  # 236
    "{SPEAKER_COLLINS}{PART_000}多普勒博士，{PART_001}我认为这周的发现更加重要。{PART_002}从翻译工作的角度看，{PART_003}我觉得最近这些传输，{PART_004}尤其有价值。{PART_005}而从个人角度看，{PART_006}我觉得它们很美。",  # 237
    "{SPEAKER_DOPPLER}{PART_000}请{PART_001}详细说说。",  # 238
    "{SPEAKER_COLLINS}{PART_000}我很乐意。{PART_001}越是思考他们的传输，{PART_002}以及 {PLAYER_NAME} 的定义，{PART_003}我受到的触动就越深。{PART_004}大约 100 次传输以前，{PART_005}我们定义了 {SIG_N128}。{PART_006}单独来看，{PART_007}它没多少意义。{PART_008}但它让我们能把 {SIG_N129} 定义为 {SIG_N045} {SIG_N128}。",  # 239
    "{SPEAKER_DOPPLER}{PART_000}这有什么用？",  # 240
    "{SPEAKER_COLLINS}{PART_000}他们在强调一件事：{PART_001}个体的视角和特质。{PART_002}他们不再只谈 {SIG_N045}，{PART_003}也就是他们的集体。{PART_004}是时候谈谈组成这个族群的每一个个体了。",  # 241
    "{SPEAKER_DOPPLER}{PART_000}组成族群的每个个体，{PART_001}是吗……",  # 242
    "{SPEAKER_COLLINS}{PART_000}接着，他们又深入探讨了 {SIG_N101}，{PART_001}探讨成为 {SIG_N101} 意味着什么。",  # 243
    "{SPEAKER_DOPPLER}{PART_000}{SIG_N132}、{PART_001}{SIG_N134}，{PART_002}还有 {SIG_N133}。{PART_003}我知道自己一开始很担心。",  # 244
    "{SPEAKER_AKERS}{PART_000}要是你还需要一点安慰，{PART_001}我就喜欢回想陨石开头说了什么。{PART_002}他们一有能力，{PART_003}就传达了 {SIG_N140}。{PART_004}他们主动现身，也让自己处于不设防的境地。{PART_005}他们不是来 {SIG_N046} {SIG_N133} {SIG_N085} 的。{PART_006}我这么说对吗？",  # 245
    "{SPEAKER_DOPPLER}{PART_000}我明白你的意思，{PART_001}埃克斯。{PART_002}这确实让我安心。{PART_003}柯林斯，{PART_004}继续说 {SIG_N101}。",  # 246
    "{SPEAKER_COLLINS}{PART_000}他们定义了 {SIG_N135}，{PART_001}还定义了几个在生物学，{PART_002}甚至文化上可能更重要的词：{PART_003}{SIG_N149}、{SIG_N150} 和 {SIG_N151}。",  # 247
    "{SPEAKER_DOPPLER}{PART_000}于是就讲到了……{PART_001}{SIG_N136}。",  # 248
    "{SPEAKER_COLLINS}{PART_000}在制造 {SIG_N044} 0 的时候——",  # 249
    "{SPEAKER_AKERS}{PART_000}——也就是 18 万年前。",  # 250
    "{SPEAKER_COLLINS}{PART_000}18 万年前——{PART_001}她还是 {SIG_N150}。",  # 251
    "{SPEAKER_BAUTISTA}{PART_000}当时已有 177 个 {SIG_N070}——{PART_001}相当于地球上的 16 岁。",  # 252
    "{SPEAKER_DOPPLER}{PART_000}16……？{PART_001}天啊。{PART_002}我还没完全消化，{PART_003}真的。{PART_004}{SIG_N045} 一定是才华横溢的 {SIG_N131}，{PART_005}是远胜于我们的天才。",  # 253
    "{SPEAKER_COLLINS}{PART_000}有这种可能，{PART_001}不过我有所怀疑。{PART_002}我觉得必须记住，{PART_003}{SIG_N045} {SIG_N135} {SIG_N004} 288 {SIG_N070}。",  # 254
    "{SPEAKER_BAUTISTA}{PART_000}一生已经走过了 60%。{PART_001}相当于人类的四十到{PART_002}五十岁。",  # 255
    "{SPEAKER_AKERS}{PART_000}这么看倒也不错。",  # 256
    "{SPEAKER_DOPPLER}{PART_000}50，{PART_001}是吗？{PART_002}哇，{PART_003}我——{PART_004}算了。",  # 257
    "{SPEAKER_AKERS}{PART_000}一切都好吧，{PART_001}多普？",  # 258
    "{SPEAKER_DOPPLER}{PART_000}没事，{PART_001}真的，{PART_002}我很好。{PART_003}想到 {SIG_N136} 算是我的同龄人，感觉有点奇怪。{PART_004}仅此而已。{PART_005}继续吧，{PART_006}柯林斯。",  # 259
    "{SPEAKER_COLLINS}{PART_000}他们的衰老方式和我们不同。{PART_001}但从某些方面说，{PART_002}我们其实，{PART_003}嗯……{PART_004}要说清楚又不能不——{PART_005}啊，{PART_006}抱歉。",  # 260
    "{SPEAKER_AKERS}{PART_000}凯莉？",  # 261
    "{SPEAKER_BAUTISTA}{PART_000}嗯？",  # 262
    "{SPEAKER_COLLINS}{PART_000}我，{PART_001}对——{PART_002}他们先提出了判断，{PART_003}关于 {SIG_N154} 和 {SIG_N155}。{PART_004}这些词和 {SIG_N132}、{SIG_N134}、{SIG_N133}、{SIG_N088}、{SIG_N044} 0、{SIG_N127} 有关。{PART_005}都与 {SIG_N101} 有关，{PART_006}也就是与我们珍视的一切有关。{PART_007}呼……{PART_008}请让我缓一缓，{PART_009}拜托。",  # 263
    "{SPEAKER_AKERS}{PART_000}慢慢来，{PART_001}凯莉！{PART_002}我们没事，{PART_003}都在这里陪着你。",  # 264
    "{SPEAKER_COLLINS}{PART_000}他们又一次引入了视角，{PART_001}也就是——{PART_002}{SIG_N128} 的视角：{PART_003}{PLAYER_NAME} 认为它与 {SIG_N152} 有关。{PART_004}他们用它定义了 {SIG_N156} 和 {SIG_N157}。",  # 265
    "{SPEAKER_DOPPLER}{PART_000}什么会让一个 {SIG_N129} 感到 {SIG_N157}？",  # 266
    "{SPEAKER_COLLINS}{PART_000}{SIG_N133}。{PART_001}简单来说，{PART_002}那是 {SIG_N157} 的深渊。{PART_003}是 {SIG_N106} {SIG_N157}。{PART_004}尤其是——{PART_005}虽然说来很沉重——{PART_006}这样吧，{PART_007}我读一句他们的话：{PART_008}{SIG_N143} {SIG_N129} {SIG_N086} {SIG_N014} {SIG_N142} {SIG_N129} {SIG_N133} {SIG_N015} {SIG_N100} {SIG_N106} {SIG_N157}。",  # 267
    "{SPEAKER_DOPPLER}{PART_000}……{PART_001}明白了。",  # 268
    "{SPEAKER_COLLINS}{PART_000}还有，多普勒，{PART_001}接下来的传输也许更能触动你。{PART_002}{SIG_N129} 0 {SIG_N086} {SIG_N014} {SIG_N130} 0 {SIG_N133} {SIG_N085} {SIG_N015}，{PART_003}{SIG_N100} {SIG_N107} {SIG_N157}。",  # 269
    "{SPEAKER_DOPPLER}{PART_000}即使他们从未见过我们。",  # 270
    "{SPEAKER_COLLINS}{PART_000}还有——{PART_001}唉，{PART_002}我讨厌自己说着说着就哽咽——{PART_003}抱歉。{PART_004}真正触动我的是：{PART_005}{SIG_N136}、{PART_006}那 264 个 {SIG_N129}，他们参与建造了 {SIG_N044} 0，{PART_007}以及她的整个 {SIG_N131}，{PART_008}他们跨越银河伸出手，向我们介绍自己。{PART_009}尽管在他们看来，{PART_010}{SIG_N046} {SIG_N100} {SIG_N124}，{PART_011}开口时却满怀温暖。{PART_012}开口便是 {SIG_N153}。{PART_013}……",  # 271
    "{SPEAKER_DOPPLER}{PART_000}……{PART_001}我很感激。{PART_002}当然，我感激他们，也感激 {SIG_N136}，{PART_003}可这不必多说。{PART_004}我也感激你，{PART_005}凯莉，{PART_006}还有 {PLAYER_NAME}、{PART_007}埃克斯博士、{PART_008}巴蒂斯塔博士——{PART_009}我很感激，恰好是你们四个在为这一切构建意义。{PART_010}{SIG_N153}，{PART_011}是吗？{PART_012}……{PART_013}我只能想象，接下来的翻译工作还会带来什么。{PART_014}下周见，{PART_015}翻译小组。{PART_016}我会一如既往地，{PART_017}热切期待。",  # 272
    "第 37 周结束",  # 273
    "{SPEAKER_DOPPLER}{PART_000}$animD19翻译小组，{PART_001}$animD20翻译小组，{PART_002}$animD21翻译小组。{PART_003}$animD22很高兴见到各位。",  # 274
    "{SPEAKER_AKERS}{PART_000}高兴的是我才对！",  # 275
    "{SPEAKER_DOPPLER}{PART_000}$animD02好吧？",  # 276
    "{SPEAKER_AKERS}{PART_000}怎么了！",  # 277
    "{SPEAKER_DOPPLER}{PART_000}总之，{PART_001}$animD22由你开场吧，{PLAYER_NAME}。",  # 278
    "{SPEAKER_DOPPLER}{PART_000}好。{PART_001}$animD21柯林斯？",  # 279
    "{SPEAKER_COLLINS}{PART_000}{SIG_N158} 和 {SIG_N159}，{PART_001}长官。{PART_002}都相当 {SIG_N104}，{PART_003}但适用于不同语境。",  # 280
    "{SPEAKER_BAUTISTA}{PART_000}比如 {SIG_N101} {SIG_N158}。{PART_001}也就是 {SIG_N160}。",  # 281
    "{SPEAKER_DOPPLER}{PART_000}$animD20巴蒂斯塔，{PART_001}你……？{PART_002}继续。",  # 282
    "{SPEAKER_BAUTISTA}{PART_000}{SIG_N162} 很有意思。{PART_001}{SIG_N162} {SIG_N086} {SIG_N161} {SIG_N163} {SIG_N085}。{PART_002}这解释了 {SIG_N054}，{PART_003}也就是第 5 个 {SIG_N052} 参数。",  # 283
    "{SPEAKER_DOPPLER}{PART_000}$animD06那些 {SIG_N044} 球体亮起来时，不就已经弄明白了吗？{PART_001}当时他们就展示了自己能够感知电磁辐射。",  # 284
    "{SPEAKER_BAUTISTA}{PART_000}$animD08嗯。{PART_001}对。{PART_002}但他们还定义了与此协同运作的 {SIG_N160}。{PART_003}很有意思。{PART_004}他们不能假定人类拥有 {SIG_N162}，{PART_005}也不能假定人类的 {SIG_N162} 足够精确。{PART_006}所以他们认为，假定接收者掌握无线电传输技术更稳妥。{PART_007}因此，确认外星人拥有 {SIG_N162}……{PART_008}真不错。",  # 285
    "{SPEAKER_DOPPLER}{PART_000}这就说得通了。{PART_001}$animD03谢谢你这次也发表了看法——{PART_002}$animD05算了。",  # 286
    "{SPEAKER_BAUTISTA}{PART_000}别说。",  # 287
    "{SPEAKER_DOPPLER}{PART_000}$animD04还学到了别的吗？",  # 288
    "{SPEAKER_COLLINS}{PART_000}差不多就这些，{PART_001}长官。",  # 289
    "{SPEAKER_DOPPLER}{PART_000}那么，{PART_001}$animD24大家回去睡一觉吧。",  # 290
    "{SPEAKER_AKERS}{PART_000}枕头在喊我的名字！{PART_001}我在这里都听到了！",  # 291
    "{SPEAKER_BAUTISTA}{PART_000}呵呵。{PART_001}晚安。",  # 292
    "{SPEAKER_COLLINS}{PART_000}各位，{PART_001}做个好梦。",  # 293
    "第 38 周结束",  # 294
    "{SPEAKER_DOPPLER}{PART_000}$animD23翻译小组，{PART_001}各位怎么样？",  # 295
    "{SPEAKER_AKERS}{PART_000}好得很，{PART_001}多普！{PART_002}这周陨石先生又带来了些有意思的东西！",  # 296
    "{SPEAKER_DOPPLER}{PART_000}哦，{PART_001}$animD19那好吧。{PART_002}请讲。",  # 297
    "{SPEAKER_AKERS}{PART_000}一开始，我们定义了几个辅助词，{PART_001}我通常不喜欢这类词。{PART_002}不过 {SIG_N165}、{SIG_N166} 和 {SIG_N167} 其实很简单，也挺有趣。{PART_003}他们用 {SIG_N053} 和我们玩了个小游戏，画出一些 {SIG_N056}，{PART_004}让我们说出它们处于哪个阶段！",  # 298
    "{SPEAKER_BAUTISTA}{PART_000}不是游戏。{PART_001}$animD20只是在确认我们理解了。{PART_002}就这样。",  # 299
    "{SPEAKER_AKERS}{PART_000}谁说陨石先生不能两件事一起做？",  # 300
    "{SPEAKER_DOPPLER}{PART_000}$animD19这些词有什么用？",  # 301
    "{SPEAKER_AKERS}{PART_000}诶诶诶！{PART_001}还没讲到呢！",  # 302
    "{SPEAKER_BAUTISTA}{PART_000}你刚才冲多普勒摇手指了？",  # 303
    "{SPEAKER_DOPPLER}{PART_000}$animD02对，真的很奇怪。",  # 304
    "{SPEAKER_AKERS}{PART_000}刚才确实不太妥，{PART_001}$animD05不过就当没发生，继续吧。{PART_002}接下来还要定义 {SIG_N169} 和 {SIG_N170}，{PART_003}现在说起来，{PART_004}真不敢相信我们之前居然没定义过？{PART_005}$animD19感觉这应该是第 150 次传输左右就会讲的东西？{PART_006}我们都有 3 种不同的 {SIG_N065} {SIG_N068} 了，{PART_007}却还没有内外之分。",  # 305
    "{SPEAKER_BAUTISTA}{PART_000}$animD20还有 {SIG_N103}，{PART_001}似乎没什么用。",  # 306
    "{SPEAKER_COLLINS}{PART_000}$animD21还有 {SIG_N095}。{PART_001}我倒有兴趣，{PART_002}不过很怀疑以后还能不能再见到。",  # 307
    "{SPEAKER_BAUTISTA}{PART_000}那可能是最没用的信号。",  # 308
    "{SPEAKER_AKERS}{PART_000}喂！{PART_001}$animD10别动我的天文学术语！",  # 309
    "{SPEAKER_DOPPLER}{PART_000}$anim01说回正题，{PART_001}埃克斯。",  # 310
    "{SPEAKER_AKERS}{PART_000}对，{PART_001}我们定义了 {SIG_N168}，{PART_002}$animD19这个词很奇怪。{PART_003}不过目前我们只用它说过：{PART_004}{SIG_N168} {SIG_N140} {SIG_N164} {SIG_N033} {SIG_N119} {SIG_N140} {SIG_N164}。",  # 311
    "{SPEAKER_DOPPLER}{PART_000}所以它发生了变化。{PART_001}嗯。",  # 312
    "{SPEAKER_AKERS}{PART_000}不知道为什么。{PART_001}然后，我们用 {SIG_N170} 定义了 {SIG_N172}，{PART_002}这就有意思了。{PART_003}从一颗 {SIG_N093} 的 {SIG_N172} 可以看出很多东西。",  # 313
    "{SPEAKER_DOPPLER}{PART_000}$animD06那他们说了些什么？",  # 314
    "{SPEAKER_AKERS}{PART_000}到这周结束时，{PART_001}我们已经大致弄清了其中的成分：{PART_002}八分之四是氮气，{PART_003}比我们的八分之六少一些。{PART_004}然后八分之一是甲烷，{PART_005}这就很有意思——",  # 315
    "{SPEAKER_DOPPLER}{PART_000}等等，{PART_001}$animD17你刚才说八分之一？！",  # 316
    "{SPEAKER_AKERS}{PART_000}没错！{PART_001}你想到我在想什么了。",  # 317
    "{SPEAKER_DOPPLER}{PART_000}甲烷吸收长波辐射的能力极强。",  # 318
    "{SPEAKER_AKERS}{PART_000}也就是说，它特别能留住热量。",  # 319
    "{SPEAKER_DOPPLER}{PART_000}也就是说——{PART_001}$animD11哦，我明白了！",  # 320
    "{SPEAKER_COLLINS}{PART_000}明白什么？",  # 321
    "{SPEAKER_BAUTISTA}{PART_000}他是怎么推出来的。",  # 322
    "{SPEAKER_AKERS}{PART_000}$animD19{SIG_N168} {SIG_N140} {SIG_N164} {SIG_N033} {SIG_N119} {SIG_N140} {SIG_N164}。{PART_001}他们的 {SIG_N172} 中甲烷比例极高，可能对塑造其 {SIG_N119} {SIG_N140} 的环境条件，{PART_002}起到了重要作用。",  # 323
    "{SPEAKER_DOPPLER}{PART_000}我们知道为什么会有这么多甲烷吗？",  # 324
    "{SPEAKER_AKERS}{PART_000}还不知道。{PART_001}还有，多普，要是你想知道，{PART_002}我知道你肯定想，{PART_003}他们还列出了其他几种主要 {SIG_N057}，它们组成了 {SIG_N172}：{PART_004}二氧化碳、{PART_005}氢气、{PART_006}水蒸气、{PART_007}氨、{PART_008}氩，{PART_009}还有臭氧。",  # 325
    "{SPEAKER_DOPPLER}{PART_000}$animD06氢气很显眼。{PART_001}它并不稳定，{PART_002}说明有某种机制在不断补充。{PART_003}$animD07你刚才还说了臭氧？",  # 326
    "{SPEAKER_AKERS}{PART_000}嗯哼！",  # 327
    "{SPEAKER_DOPPLER}{PART_000}$animD16这就很有启发了。",  # 328
    "{SPEAKER_COLLINS}{PART_000}抱歉，{PART_001}为什么这么让人兴奋？",  # 329
    "{SPEAKER_AKERS}{PART_000}臭氧是 {SIG_N057} {SIG_N014} 3 {SIG_N056} 8{SIG_N015}。{PART_001}氧气是 {SIG_N057} {SIG_N014} 2 {SIG_N056} 8 {SIG_N015}。{PART_002}从化学上说，{PART_003}很相似。{PART_004}但 {SIG_N056} 8 只有 2 个价电子，{PART_005}所以一次只能与另外 2 个 {SIG_N056} 成键，{PART_006}或者与 1 个 {SIG_N056} 形成双键，比如另一个 {SIG_N056} 8。",  # 330
    "{SPEAKER_COLLINS}{PART_000}$animD21可臭氧违反了这条规则？",  # 331
    "{SPEAKER_AKERS}{PART_000}恒星发出的紫外线不断把氧气的 {SIG_N057} 拆开，{PART_001}留下游离的 {SIG_N056} 8 四处游荡，闯进原本好端端的氧气 {SIG_N057} 里。",  # 332
    "{SPEAKER_BAUTISTA}{PART_000}$animD20拆家贼。",  # 333
    "{SPEAKER_AKERS}{PART_000}但更多紫外线接着照过来，{PART_001}又会把臭氧拆开！",  # 334
    "{SPEAKER_COLLINS}{PART_000}所以这到底说明什么？",  # 335
    "{SPEAKER_AKERS}{PART_000}多普？",  # 336
    "{SPEAKER_DOPPLER}{PART_000}$animD22说明臭氧能吸收紫外辐射，{PART_001}而紫外辐射会损害地球生命的 DNA，并引发癌症。{PART_002}紫外辐射这种东西……{PART_003}$animD06呃，{PART_004}我最近一直在练陨石的句法：{PART_005}$animD15是会 {SIG_N086} {SIG_N101} {SIG_N085} {SIG_N133} 的东西。",  # 337
    "{SPEAKER_BAUTISTA}{PART_000}嗯……",  # 338
    "{SPEAKER_COLLINS}{PART_000}不太对，{PART_001}多普勒。{PART_002}应该是 {SIG_N133} {SIG_N085}。",  # 339
    "{SPEAKER_DOPPLER}{PART_000}$animD00啊，{PART_001}$animD02既然已经这么丢脸，{PART_002}$animD24今晚就到这里吧。",  # 340
    "{SPEAKER_BAUTISTA}{PART_000}晚安。",  # 341
    "{SPEAKER_COLLINS}{PART_000}做个好梦！",  # 342
    "{SPEAKER_AKERS}{PART_000}大家晚安！",  # 343
    "第 39 周结束",  # 344
    "{SPEAKER_DOPPLER}{PART_000}欢迎，{PART_001}各位。{PART_002}$animD01你们好像都在沉思。",  # 345
    "{SPEAKER_COLLINS}{PART_000}他们给我们讲了一个故事——{PART_001}连续 10 次传输，一次都没考过我们。{PART_002}我们只需要确认自己理解了 {SIG_N168} {SIG_N140} {SIG_N174} {SIG_N085}。",  # 346
    "{SPEAKER_DOPPLER}{PART_000}$animD21有意思。{PART_001}我想，你们已经有翻译所需的一切了，{PART_002}对吧？{PART_003}$animD20不会又像 {SIG_N054} 那样，信息藏在陨石本身？",  # 347
    "{SPEAKER_COLLINS}{PART_000}不是。{PART_001}$animD21工具都齐了，{PART_002}只差一样。",  # 348
    "{SPEAKER_AKERS}{PART_000}这周解开的最后一次传输，在定义 {SIG_N178} 之前就先用上了这个词。",  # 349
    "{SPEAKER_DOPPLER}{PART_000}$animD06在定义之前？",  # 350
    "{SPEAKER_COLLINS}{PART_000}一般来说，{PART_001}介绍 {SIG_N042} 时会采用固定格式的定义传输。{PART_002}先出现 {SIG_N042}，{PART_003}再出现 2 个 {SIG_N002}，{PART_004}接着是 {SIG_N078}。{PART_005}之后是其他分组信息，{PART_006}例如：{PART_007}{SIG_N136} {SIG_N099} {SIG_N129}。{PART_008}$animD21然后有时会给出一个简单定义，{PART_009}要么用例子，{PART_010}要么联系到之前定义过的 {SIG_N042}。",  # 351
    "{SPEAKER_DOPPLER}{PART_000}对，对。{PART_001}$animD06我在你们的笔记里见过。{PART_002}所以 {SIG_N178}，{PART_003}$animD08一直没有定义？",  # 352
    "{SPEAKER_COLLINS}{PART_000}没有。{PART_001}这一连串传输的第 10 次，{PART_002}也就是故事中，{PART_003}第一次出现 {SIG_N042} -178。",  # 353
    "{SPEAKER_DOPPLER}{PART_000}那是什么意思？",  # 354
    "{SPEAKER_COLLINS}{PART_000}他们想说明一个道理。",  # 355
    "{SPEAKER_DOPPLER}{PART_000}嗯……{PART_001}我明白了。",  # 356
    "{SPEAKER_AKERS}{PART_000}还有，多普，{PART_001}走之前，{PART_002}我们有了个新发现。{PART_003}你知道地球有 {SIG_N176} 和 {SIG_N177} 吧？",  # 357
    "{SPEAKER_DOPPLER}{PART_000}$animD19没有。",  # 358
    "{SPEAKER_AKERS}{PART_000}没错！{PART_001}但月球有！{PART_002}月球自转一周和公转一周的时间相同，{PART_003}所以永远是同一面对着我们。{PART_004}{SIG_N110} {SIG_N065} {SIG_N121}——{PART_005}永远如此。{PART_006}这叫潮汐锁定。",  # 359
    "{SPEAKER_DOPPLER}{PART_000}他们还定义了 {SIG_N176} 和 {SIG_N177}？{PART_001}所以他们的 {SIG_N108} {SIG_N094} 处于潮汐锁定？{PART_002}$animD18这条信息很有用。",  # 360
    "{SPEAKER_AKERS}{PART_000}嗯，也有可能，{PART_001}不过{PART_002}我们还不知道。",  # 361
    "{SPEAKER_DOPPLER}{PART_000}$animD19嗯？",  # 362
    "{SPEAKER_AKERS}{PART_000}但我们确定 {SIG_N140} 被潮汐锁定——{PART_001}和 {SIG_N141} 锁定在一起。",  # 363
    "{SPEAKER_DOPPLER}{PART_000}……{PART_001}等等。{PART_002}$animD02那就意味着……",  # 364
    "{SPEAKER_AKERS}{PART_000}没错！{PART_001}{SIG_N176} 和 {SIG_N177}。{PART_002}永昼与永夜。",  # 365
    "{SPEAKER_DOPPLER}{PART_000}天啊……{PART_001}那就意味着……{PART_002}$animD05我得琢磨琢磨。",  # 366
    "{SPEAKER_COLLINS}{PART_000}加上过去 2 周定义的那些 {SIG_N104} 辅助 {SIG_N042}：{PART_001}$animD21{SIG_N169}、{SIG_N170}、{SIG_N171}，{PART_002}我们对 {SIG_N140} {SIG_N174} {SIG_N085} 有了不少重大发现。{PART_003}这周收获很大。",  # 367
    "{SPEAKER_DOPPLER}{PART_000}一如既往，干得漂亮。{PART_001}$animD22{PLAYER_NAME}、{PART_002}$animD21柯林斯博士、{PART_003}$animD20巴蒂斯塔博士，{PART_004}$animD19还有埃克斯博士，{PART_005}下周见——{PART_006}$animD24祝顺利。",  # 368
    "{SPEAKER_AKERS}{PART_000}晚安，多普！",  # 369
    "{SPEAKER_BAUTISTA}{PART_000}各位晚安。",  # 370
    "{SPEAKER_COLLINS}{PART_000}祝各位，{PART_001}做个好梦！",  # 371
    "第 40 周结束",  # 372
    "{SPEAKER_DOPPLER}{PART_000}翻译小组——{PART_001}欢迎各位。{PART_002}$animD07我记得上周说到 {SIG_N178}。{PART_003}想必已经有答案了。",  # 373
    "{SPEAKER_COLLINS}{PART_000}有，{PART_001}长官。{PART_002}我们终于收到了那次顺序颠倒的定义传输。{PART_003}{SIG_N178} 是 {SIG_N131} {SIG_N038} {SIG_N085} 的过程。",  # 374
    "{SPEAKER_DOPPLER}{PART_000}啊，{PART_001}$animD16达尔文会很欣慰。{PART_002}几百次传输前，他们就粗略描绘过 DNA，{PART_003}我猜他们早已弄明白了。",  # 375
    "{SPEAKER_BAUTISTA}{PART_000}他们很可能在科学的所有方面都胜过我们。",  # 376
    "{SPEAKER_DOPPLER}{PART_000}$animD20你为什么这么说？",  # 377
    "{SPEAKER_BAUTISTA}{PART_000}知识沿着平行轨道前进，{PART_001}又彼此影响。{PART_002}信息相互关联。{PART_003}物理学和化学会共同进步。{PART_004}对化学理解得更深，我们才能制造电脑和火箭。{PART_005}各种能力不断叠加、倍增。{PART_006}光是陨石这项工程奇迹，就足以证明他们更先进。",  # 378
    "{SPEAKER_AKERS}{PART_000}所以我才希望能从他们那里学点东西！",  # 379
    "{SPEAKER_DOPPLER}{PART_000}我也希望如此。{PART_001}$animD22还学到了什么？",  # 380
    "{SPEAKER_COLLINS}{PART_000}我们定义了一组生态学术语。{PART_001}$animD21{SIG_N179}、{SIG_N180}、{SIG_N181}、{SIG_N182}、{SIG_N183}、{SIG_N184}、{SIG_N185}。",  # 381
    "{SPEAKER_DOPPLER}{PART_000}{SIG_N185}？{PART_001}特意把它单列出来，似乎有些奇怪。",  # 382
    "{SPEAKER_COLLINS}{PART_000}按我们的标准，{PART_001}是的。{PART_002}但得知 {SIG_N147} {SIG_N100} {SIG_N185} 后，{PART_003}我的看法变了。",  # 383
    "{SPEAKER_DOPPLER}{PART_000}{SIG_N147} {SIG_N100} {SIG_N185}……{PART_001}就是“一对八”关系中的“一”，{PART_002}对吗？",  # 384
    "{SPEAKER_COLLINS}{PART_000}对，{PART_001}长官。",  # 385
    "{SPEAKER_DOPPLER}{PART_000}那是……{PART_001}$animD02{SIG_N185}？",  # 386
    "{SPEAKER_COLLINS}{PART_000}他们和我们非常不同。",  # 387
    "{SPEAKER_AKERS}{PART_000}不过至少我们都顶着 {SIG_N179} 这个不起眼的称号。{PART_001}那他们也没先进到哪去，{PART_002}对吧？",  # 388
    "{SPEAKER_COLLINS}{PART_000}他不喜欢这个词带有的意味。",  # 389
    "{SPEAKER_DOPPLER}{PART_000}$animD04他们非常珍视我们。{PART_001}我们知道这一点。{PART_002}总之，{PART_003}一如既往，很高兴见到各位。{PART_004}$animD24都去睡一觉吧。",  # 390
    "{SPEAKER_AKERS}{PART_000}各位晚安！",  # 391
    "{SPEAKER_BAUTISTA}{PART_000}晚安。",  # 392
    "{SPEAKER_COLLINS}{PART_000}祝大家做个好梦！",  # 393
    "第 41 周结束",  # 394
    "{SPEAKER_DOPPLER}{PART_000}欢迎，{PART_001}欢迎。{PART_002}$animD19我们上次说到哪了？{PART_003}$animD21是在了解他们的生态，{PART_004}对吧？",  # 395
    "{SPEAKER_BAUTISTA}{PART_000}我欠 {SIG_N136} 一个道歉。",  # 396
    "{SPEAKER_DOPPLER}{PART_000}$animD20什么？",  # 397
    "{SPEAKER_BAUTISTA}{PART_000}是的。",  # 398
    "{SPEAKER_COLLINS}{PART_000}巴蒂斯塔，{PART_001}你还好吗？",  # 399
    "{SPEAKER_BAUTISTA}{PART_000}是的。",  # 400
    "{SPEAKER_AKERS}{PART_000}他连“嗯哼”都不说了——{PART_001}$animD02他到底怎么了？！",  # 401
    "{SPEAKER_BAUTISTA}{PART_000}我没事。{PART_001}我反感 {SIG_N136} 的 {SIG_N104} 语言。{PART_002}$animD20我反感依赖主观体验的信息。{PART_003}数学是宇宙的语言；{PART_004}它是客观的。{PART_005}所有模型，{PART_006}所有理解现实内在本质的方式，都以数学为基础——{PART_007}现实由数学函数定义，{PART_008}由映射符号的算法定义。{PART_009}现实是由 {SIG_N105} 关系组成的网络，{PART_010}不是视角的状态。{PART_011}{SIG_N136} 说 1 {SIG_N002} 8 {SIG_N005} {SIG_N004} 1，我不接受。{PART_012}{SIG_N104} 的胡说八道。",  # 402
    "{SPEAKER_COLLINS}{PART_000}他们不是这个意思，{PART_001}巴蒂斯塔。{PART_002}$animD21{SIG_N136} 和 {SIG_N045} 是想和我们交流——{PART_003}不是重新定义现实。",  # 403
    "{SPEAKER_BAUTISTA}{PART_000}现在我明白了。{PART_001}{SIG_N193} 改变了我的看法。",  # 404
    "{SPEAKER_DOPPLER}{PART_000}{SIG_N193}？{PART_001}$animD07这个词我不熟悉。{PART_002}是最近定义的吗？",  # 405
    "{SPEAKER_BAUTISTA}{PART_000}{SIG_N193} 就像水。{PART_001}再多也只是同一种东西变多了。{PART_002}1 {SIG_N193} {SIG_N002} 8 {SIG_N193} {SIG_N005} {SIG_N004} 1 {SIG_N193}。{PART_003}这说得通。",  # 406
    "{SPEAKER_COLLINS}{PART_000}你以前强烈反对这种物质名词的同一关系。{PART_001}为什么现在又说它合理？",  # 407
    "{SPEAKER_BAUTISTA}{PART_000}$animD20现在我明白了，交流是 {SIG_N193} 的延伸。{PART_001}神经元之间传递的数学映射，{PART_002}也会通过编码在词语中的信息传递。{PART_003}正如 9 个 {SIG_N193} 是 1 个整体，{PART_004}我的 {SIG_N193} 也是 {SIG_N136} 的 {SIG_N193}。{PART_005}我们是一个系统。{PART_006}如果可以，{PART_007}我想给妻子打个电话。{PART_008}我还欠她一个道歉。",  # 408
    "{SPEAKER_DOPPLER}{PART_000}$animD00呃，{PART_001}去吧。{PART_002}$animD04你可以离开了。{PART_003}……",  # 409
    "{SPEAKER_AKERS}{PART_000}这到底是怎么回事？",  # 410
    "{SPEAKER_DOPPLER}{PART_000}$animD05我还以为你们三个比我清楚。",  # 411
    "{SPEAKER_COLLINS}{PART_000}我想……{PART_001}不，{PART_002}我不确定。{PART_003}他心里有些事情突然想通了。",  # 412
    "{SPEAKER_DOPPLER}{PART_000}$animD21确实。",  # 413
    "{SPEAKER_AKERS}{PART_000}对……{PART_001}看他说“是的”真的很奇怪。{PART_002}我好像从没见他把嘴张那么大。{PART_003}有时他说话就像怕声音不小心溜出来。",  # 414
    "{SPEAKER_DOPPLER}{PART_000}$animD19好了，{PART_001}好了，{PART_002}我想我们聊得够多了。{PART_003}我，{PART_004}呃，{PART_005}不知道该怎么说。{PART_006}$animD18就把巴蒂斯塔的话当成一道思考题吧？{PART_007}唉，{PART_008}$animD00谁知道他刚才有没有说清楚。{PART_009}$animD24各位保重。",  # 415
    "{SPEAKER_AKERS}{PART_000}好，{PART_001}你也保重，{PART_002}多普。{PART_003}晚安，{PLAYER_NAME}，{PART_004}还有柯林斯博士。",  # 416
    "{SPEAKER_COLLINS}{PART_000}祝各位，{PART_001}做个好梦。",  # 417
    "第 42 周结束",  # 418
    "{SPEAKER_DOPPLER}{PART_000}翻译小组，各位好。{PART_001}$animD08有什么新进展吗？",  # 419
    "{SPEAKER_COLLINS}{PART_000}他们又给我们讲了一个故事。{PART_001}这一次，{PART_002}讲的是他们 {SIG_N178} 的历史。{PART_003}$animD21我们现在知道现代 {SIG_N129} 是怎样形成的，{PART_004}它们源于 {SIG_N168} {SIG_N146} 和 {SIG_N168} {SIG_N147}。{PART_005}最好以 {PLAYER_NAME} 的解释为准，{PART_006}所以我就不详说了。{PART_007}简而言之，{PART_008}这是一个极端的共生案例。",  # 420
    "{SPEAKER_DOPPLER}{PART_000}所以很久以前，{PART_001}$animD02他们彼此独立？",  # 421
    "{SPEAKER_COLLINS}{PART_000}对，{PART_001}长官。{PART_002}$animD21他们的食物链和栖息地相互关联。{PART_003}如今他们自己也融为一体。",  # 422
    "{SPEAKER_DOPPLER}{PART_000}明白了。",  # 423
    "{SPEAKER_COLLINS}{PART_000}我们还学到了几个特别有意思的新词：{PART_001}$animD06{SIG_N194}、{PART_002}{SIG_N197}，{PART_003}还有我最喜欢的：{PART_004}{SIG_N195}。",  # 424
    "{SPEAKER_DOPPLER}{PART_000}从 {PLAYER_NAME} 的定义来看，{PART_001}$animD05我明白你为什么喜欢。",  # 425
    "{SPEAKER_COLLINS}{PART_000}根据之前的一次传输，{PART_001}他们制造了这 262,000 颗陨石，{PART_002}希望找到 {SIG_N101}，哪怕是在别的 {SIG_N093} 上。{PART_003}但他们也在寻找 {SIG_N194} {SIG_N101}。",  # 426
    "{SPEAKER_DOPPLER}{PART_000}$animD21为什么？",  # 427
    "{SPEAKER_COLLINS}{PART_000}根据目前有限的传输，{PART_001}我猜他们想与我们分享 {SIG_N195} 和 {SIG_N197}。{PART_002}他们迫切寻找其他 {SIG_N101}，好和对方 {SIG_N196}。",  # 428
    "{SPEAKER_DOPPLER}{PART_000}先进的科学、{PART_001}短暂的寿命，{PART_002}还有共生的本质——{PART_003}他们是社会性的 {SIG_N131}，这很合理。{PART_004}$animD06幸好 {SIG_N044}{PART_005}63901 找到了我们。",  # 429
    "{SPEAKER_AKERS}{PART_000}$animD19确实是件好事，{PART_001}多普。",  # 430
    "{SPEAKER_DOPPLER}{PART_000}那么，{PART_001}$animD24应该就这些了。",  # 431
    "{SPEAKER_COLLINS}{PART_000}其实，多普勒，{PART_001}$animD21我觉得他们有一件特别的东西想和我们分享。",  # 432
    "{SPEAKER_DOPPLER}{PART_000}$animD15是 {SIG_N195}，还是 {SIG_N197}？",  # 433
    "{SPEAKER_COLLINS}{PART_000}还不清楚，{PART_001}但有了 {PLAYER_NAME} 那本收录了 197 个词的漂亮词典，{PART_002}我们已经有能力讨论各种各样的话题。{PART_003}可以讨论 {SIG_N197}、{SIG_N191}、{SIG_N164} 和 {SIG_N093}。{PART_004}可以讨论他们的 {SIG_N176} 和 {SIG_N177}。{PART_005}可以讨论 {SIG_N181}、{SIG_N182} 和 {SIG_N178}。{PART_006}可以讨论 {SIG_N195}、{SIG_N156}、{SIG_N157}、{SIG_N153}，{PART_007}以及所有让我们产生感受的事物。{PART_008}而你用一句话概括得最好：{PART_009}“他们是社会性的 {SIG_N131}。”",  # 434
    "{SPEAKER_DOPPLER}{PART_000}$animD04这一切都让我满心欢喜。{PART_001}翻译小组，{PART_002}我无法想象你们四个是怎样思考的。{PART_003}你们一路攀过层层抽象，{PART_004}$animD16如今即将登顶。{PART_005}$animD24祝顺利，{PART_006}各位。",  # 435
    "第 43 周结束",  # 436
    "{SPEAKER_DOPPLER}{PART_000}欢迎各位。{PART_001}$animD06我大致看了看这周的进度。{PART_002}22 次传输，还有 1、2、3、……{PART_003}11 个新 {SIG_N042}$animD16？！",  # 437
    "{SPEAKER_COLLINS}{PART_000}这是我们遇到过 {SIG_N042} 密度最高的一次。{PART_001}最接近的一次还是很久以前的第 10 周。",  # 438
    "{SPEAKER_DOPPLER}{PART_000}$animD22提醒我一下。",  # 439
    "{SPEAKER_BAUTISTA}{PART_000}逻辑术语。",  # 440
    "{SPEAKER_COLLINS}{PART_000}那周在 28 次传输里引入了 12 个 {SIG_N042}。{PART_001}$animD21要在这样的密度下依然让人理解，{PART_002}就得成组引入相关术语：{PART_003}{SIG_N027} 和 {SIG_N028}，{SIG_N030} 和 {SIG_N031}。",  # 441
    "{SPEAKER_AKERS}{PART_000}你看，{SIG_N031} 后来就再没出现过。{PART_001}$animD19自从被 {SIG_N077} 取代，{PART_002}它就开始四处逃亡了。",  # 442
    "{SPEAKER_COLLINS}{PART_000}我猜 {SIG_N031} 太 {SIG_N105} 了，至少 {SIG_N136} 是这么看的。",  # 443
    "{SPEAKER_AKERS}{PART_000}可 {SIG_N030} 就能留下？",  # 444
    "{SPEAKER_COLLINS}{PART_000}$animD21我想，她觉得 {SIG_N031} 的逻辑功能和它的 {SIG_N104} 解释相差太远。{PART_001}相比之下，{SIG_N030} 的歧义更少。",  # 445
    "{SPEAKER_DOPPLER}{PART_000}$animD02虽然 {SIG_N030} 和 {SIG_N031} 的确让我兴奋不已……",  # 446
    "{SPEAKER_COLLINS}{PART_000}好。{PART_001}许多传输都是很短的 {SIG_N012} {SIG_N004} 0，{PART_002}但确定含义还是花了些时间。{PART_003}现在的内容不像以前那么 {SIG_N105} 了。",  # 447
    "{SPEAKER_DOPPLER}{PART_000}$animD00明白。{PART_001}$animD06这周学到了哪些新词？",  # 448
    "{SPEAKER_COLLINS}{PART_000}我们定义了 {SIG_N198}、{SIG_N199} 和 {SIG_N201}，{PART_001}很重要的是，{PART_002}它们又引出了 5 个 {SIG_N204}。",  # 449
    "{SPEAKER_DOPPLER}{PART_000}$animD07分别是什么？",  # 450
    "{SPEAKER_COLLINS}{PART_000}还是让 {PLAYER_NAME} 来说吧。{PART_001}……{PART_002}不过我们都知道会怎样……",  # 451
    "{SPEAKER_AKERS}{PART_000}那我就尽量优呀地说出来。{PART_001}我知道是“优雅”，好了，别管我。{PART_002}咳，{PART_003}这 5 个 {SIG_N045} {SIG_N199} {SIG_N204} 是：{PART_004}{SIG_N205}、{PART_005}{SIG_N206}、{PART_006}{SIG_N207}、{PART_007}{SIG_N208}，{PART_008}还有 {SIG_N209}。",  # 452
    "{SPEAKER_DOPPLER}{PART_000}有意思。{PART_001}$animD02这是在宣告……",  # 453
    "{SPEAKER_COLLINS}{PART_000}宣告道德准则。{PART_001}为了说明这些道德目标，{PART_002}{SIG_N136} 认为 {SIG_N044} 0 符合其中 3 类。",  # 454
    "{SPEAKER_DOPPLER}{PART_000}4？{PART_001}$animD21不是有 5 类吗？",  # 455
    "{SPEAKER_COLLINS}{PART_000}{SIG_N205} 和 {SIG_N206}——{PART_001}{SIG_N044} 0 与生死无关。{PART_002}而最有意思的是，{PART_003}多普勒，{PART_004}这些是 {SIG_N106} {SIG_N108} {SIG_N045} {SIG_N201}。{PART_005}这些是起引导作用的 {SIG_N204}，属于 {SIG_N045} {SIG_N199}。",  # 456
    "{SPEAKER_DOPPLER}{PART_000}$animD22那么 {PLAYER_NAME}，{PART_001}就靠你找出他们为什么要和我们分享这些了。{PART_002}$animD02天啊，{PART_003}10 个月前我还像个傻瓜一样问这问那。{PART_004}问他们有没有告诉我们怎么解决电力问题，{PART_005}问他们是不是为和平而来。{PART_006}$animD12而现在，答案似乎就在眼前。{PART_007}继续努力，{PART_008}翻译小组$animD24。",  # 457
    "{SPEAKER_COLLINS}{PART_000}我们会的，多普勒。",  # 458
    "{SPEAKER_AKERS}{PART_000}你知道我们会！",  # 459
    "{SPEAKER_BAUTISTA}{PART_000}嗯哼。",  # 460
    "第 44 周结束",  # 461
    "{SPEAKER_DOPPLER}{PART_000}欢迎各位，{PART_001}我们的八条腿朋友又分享了什么？",  # 462
    "{SPEAKER_COLLINS}{PART_000}我们暂时不谈上周引入的 {SIG_N201}，转向了另一个话题。{PART_001}现在已经大致了解他们的 {SIG_N216}。{PART_002}用 {PLAYER_NAME} 的定义说起来，语法可能有点别扭，{PART_003}不过我相信你明白这个概念。",  # 463
    "{SPEAKER_DOPPLER}{PART_000}大致明白。",  # 464
    "{SPEAKER_AKERS}{PART_000}他们能 {SIG_N217}。",  # 465
    "{SPEAKER_DOPPLER}{PART_000}什么意思？",  # 466
    "{SPEAKER_COLLINS}{PART_000}{SIG_N196}——靠的是 {SIG_N191}！",  # 467
    "{SPEAKER_DOPPLER}{PART_000}{SIG_N196}，{PART_001}是吗？{PART_002}也就是有输入和输出。{PART_003}等等，{PART_004}他们能产生 {SIG_N191}？{PART_005}地球上有生命能做到吗？",  # 468
    "{SPEAKER_AKERS}{PART_000}我一时想不到。",  # 469
    "{SPEAKER_BAUTISTA}{PART_000}制作参考页签时没见过。",  # 470
    "{SPEAKER_COLLINS}{PART_000}这超出我的专业范围了。",  # 471
    "{SPEAKER_DOPPLER}{PART_000}那么 {PLAYER_NAME}，{PART_001}希望你有一些相关的生物学知识。{PART_002}或者懂 {SIG_N191}。",  # 472
    "{SPEAKER_AKERS}{PART_000}我们还了解了他们的 {SIG_N222} {SIG_N216}。",  # 473
    "{SPEAKER_DOPPLER}{PART_000}他们是横扫大地的强大猎手吗？",  # 474
    "{SPEAKER_COLLINS}{PART_000}多普勒，{PART_001}你似乎忘了他们的构造。{PART_002}他们是由 {SIG_N185} 和 {SIG_N184} {SIG_N183} 组成的共生体。",  # 475
    "{SPEAKER_DOPPLER}{PART_000}没想到自己有一天会听到这种话。",  # 476
    "{SPEAKER_COLLINS}{PART_000}我也没想到自己会说。",  # 477
    "{SPEAKER_DOPPLER}{PART_000}好，{PART_001}谢谢你们分享进展。",  # 478
    "{SPEAKER_AKERS}{PART_000}其实，多普，{PART_001}还有一件事引起了我的注意。{PART_002}是关于 {SIG_N044} 1 的。",  # 479
    "{SPEAKER_DOPPLER}{PART_000}我们发回的讯息？",  # 480
    "{SPEAKER_AKERS}{PART_000}他们建议，{PART_001}或者更准确地说，{PART_002}解释了我们应该用 {SIG_N161} 来构建它。{PART_003}这种说法有点奇怪，不过我明白意思。",  # 481
    "{SPEAKER_DOPPLER}{PART_000}用 {SIG_N161} 构建……{PART_001}我懂了。{PART_002}这样速度更快。",  # 482
    "{SPEAKER_AKERS}{PART_000}应该说越快越好，{PART_001}还得补充一句。{PART_002}不过，{PART_003}他们离得很远。{PART_004}而且 {SIG_N161} 在某种意义上会随时间衰减。",  # 483
    "{SPEAKER_DOPPLER}{PART_000}因为红移，{PART_001}源自……{PART_002}啊，那叫什么来着——",  # 484
    "{SPEAKER_AKERS}{PART_000}我的意思是，{SIG_N161} 在某种意义上会越来越稀薄，{PART_001}就像远方的恒星会变暗。{PART_002}所以，{PART_003}很遗憾，{PART_004}我们才不知道 {SIG_N141}。{PART_005}至少现在不知道……{PART_006}但抵消这种衰减的方法，{PART_007}就是造一台强大的发射器。{PART_008}你知道今年建在波多黎各的阿雷西博天文台吧？",  # 485
    "{SPEAKER_DOPPLER}{PART_000}建在群山之间的那个？",  # 486
    "{SPEAKER_AKERS}{PART_000}想让传输抵达时仍然清晰可辨，就得有那么强的设备。",  # 487
    "{SPEAKER_DOPPLER}{PART_000}嗯。{PART_001}这点值得记住。{PART_002}谢谢你们分享。{PART_003}翻译小组，{PART_004}时间差不多了。{PART_005}下周见。",  # 488
    "{SPEAKER_AKERS}{PART_000}下周见。",  # 489
    "{SPEAKER_BAUTISTA}{PART_000}晚安。",  # 490
    "{SPEAKER_COLLINS}{PART_000}做个好梦。",  # 491
    "第 45 周结束",  # 492
    "{SPEAKER_DOPPLER}{PART_000}欢迎回来，翻译小组。{PART_001}$animD04这周短一些，{PART_002}是吧？{PART_003}说说你们发现了什么。",  # 493
    "{SPEAKER_COLLINS}{PART_000}这周定义的 {SIG_N042} 里，最有意思的是 {SIG_N224}。{PART_001}$animD20我猜这是一种社会学效应。",  # 494
    "{SPEAKER_AKERS}{PART_000}而且全都和 {SIG_N108} {SIG_N216} 有关。{PART_001}$animD02就是像我这样的特殊个体。",  # 495
    "{SPEAKER_BAUTISTA}{PART_000}请无视他后半句话。",  # 496
    "{SPEAKER_DOPPLER}{PART_000}$animD20已经无视了，{PART_001}巴蒂斯塔。",  # 497
    "{SPEAKER_AKERS}{PART_000}可恶。",  # 498
    "{SPEAKER_COLLINS}{PART_000}有意思的是，他们特意讨论了 {SIG_N223} {SIG_N224}。{PART_001}$animD21这让我们得以一窥他们如何看待 {SIG_N128} 在 {SIG_N199} 中的作用。{PART_002}不过眼下可供分析的材料不多。",  # 499
    "{SPEAKER_DOPPLER}{PART_000}{SIG_N223} {SIG_N224}，{PART_001}是吗？{PART_002}都有哪些 {SIG_N224}？",  # 500
    "{SPEAKER_COLLINS}{PART_000}{SIG_N222}、{SIG_N074}、{SIG_N051} {SIG_N020}，还有一个很特别的……{PART_001}{SIG_N044} 0。",  # 501
    "{SPEAKER_DOPPLER}{PART_000}{SIG_N044} 0$animD02？{PART_001}真出人意料。",  # 502
    "{SPEAKER_COLLINS}{PART_000}是啊。",  # 503
    "{SPEAKER_DOPPLER}{PART_000}$animD06沿着这些线索查下去。{PART_001}继续翻译。",  # 504
    "{SPEAKER_AKERS}{PART_000}这就是我们的工作！",  # 505
    "{SPEAKER_DOPPLER}{PART_000}$animD00我看得出来，你们四个越来越了解他们的族群，{PART_001}或者说，{PART_002}他们的 {SIG_N199}。{PART_003}$animD22那就继续做你们$animD24最擅长的事。{PART_004}继续翻译。",  # 506
    "{SPEAKER_AKERS}{PART_000}知道了，{PART_001}多普！",  # 507
    "{SPEAKER_COLLINS}{PART_000}明白。",  # 508
    "{SPEAKER_BAUTISTA}{PART_000}嗯哼！",  # 509
    "第 46 周结束",  # 510
    "{SPEAKER_DOPPLER}{PART_000}$animD20欢迎，{PART_001}$animD19欢——",  # 511
    "{SPEAKER_COLLINS}{PART_000}他们的信仰体系。",  # 512
    "{SPEAKER_DOPPLER}{PART_000}$animD21柯林斯博士？{PART_001}哦，{PART_002}请继续。{PART_003}$animD23有什么发现？",  # 513
    "{SPEAKER_COLLINS}{PART_000}{SIG_N136} 和 {SIG_N137}，{PART_001}在 {SIG_N229} 这个问题上，{PART_002}都 {SIG_N211} {SIG_N202} {SIG_N085}。",  # 514
    "{SPEAKER_DOPPLER}{PART_000}$animD06这完全听不懂。",  # 515
    "{SPEAKER_AKERS}{PART_000}对你这样的人类来说，当然。",  # 516
    "{SPEAKER_COLLINS}{PART_000}不，{PART_001}我不接受这种说法。{PART_002}这确实能理解，{PART_003}即使对人类也是。{PART_004}$animD21他们费了很大力气，让我们能够理解。{PART_005}但你得设法体会作为 {SIG_N129} 是什么感觉。{PART_006}得站在他们的立场思考。",  # 517
    "{SPEAKER_BAUTISTA}{PART_000}他们需要 8 只鞋吗？",  # 518
    "{SPEAKER_COLLINS}{PART_000}每个 {SIG_N129} 都由 9 个 {SIG_N128} 组成。{PART_001}每个都有自己的 {SIG_N193}。{PART_002}但他们将其视作 1 个 {SIG_N193}。{PART_003}至于身份认同，{PART_004}这点还不清楚。{PART_005}不过重要的概念是 {SIG_N227}，{PART_006}{SIG_N169} {SIG_N223}，{PART_007}用 {PLAYER_NAME} 的话来说。",  # 519
    "{SPEAKER_DOPPLER}{PART_000}$animD02我还是不明白。{PART_001}$animD15给我讲讲。",  # 520
    "{SPEAKER_COLLINS}{PART_000}在这种情况下，类比是很有用的工具。{PART_001}我觉得最好这样理解：{PART_002}人类可以通过锻炼增长肌肉。{PART_003}我们可以单独增强某个部位的力量。{PART_004}而且能增强的不只有力量。{PART_005}就连我们的神经系统，{PART_006}也会改变。",  # 521
    "{SPEAKER_AKERS}{PART_000}神经系统？{PART_001}真的？",  # 522
    "{SPEAKER_COLLINS}{PART_000}我用左手写字比右手好得多。{PART_001}这是身体一个部位发生的局部变化。{PART_002}如果你能理解这一点，{PART_003}再记住那 8 个 {SIG_N146}，{PART_004}{SIG_N227} 和 {SIG_N229} 的概念就逐渐清楚了。",  # 523
    "{SPEAKER_DOPPLER}{PART_000}$animD00这真是……{PART_001}$animD02天啊。",  # 524
    "{SPEAKER_COLLINS}{PART_000}还有，多普勒，{PART_001}他们终于再次谈到了 {SIG_N212} 和 {SIG_N211}，{PART_002}并说明在这个问题上，{PART_003}双方持有不同的道德立场。{PART_004}这 2 个 {SIG_N210} 的观点从根本上相左。",  # 525
    "{SPEAKER_DOPPLER}{PART_000}$animD16这正是他们的区别所在……",  # 526
    "{SPEAKER_COLLINS}{PART_000}这是我目前最好的理解。{PART_001}我相信这些推断，{PART_002}{PLAYER_NAME} 的定义也让我安心。{PART_003}……",  # 527
    "{SPEAKER_AKERS}{PART_000}还有我的贡献。",  # 528
    "{SPEAKER_BAUTISTA}{PART_000}别理这个小丑。",  # 529
    "{SPEAKER_DOPPLER}{PART_000}谢谢你花时间给我解释。{PART_001}$animD22这确实是出于我个人的好奇，{PART_002}不过等到全人类都能看到这一切时，我也希望这些会议录像能帮他们了解事情的经过。{PART_003}$animD04无论那一天何时到来。{PART_004}$animD24谢谢各位。",  # 530
    "{SPEAKER_COLLINS}{PART_000}不用客气，{PART_001}多普勒博士。",  # 531
    "{SPEAKER_AKERS}{PART_000}你可是我们的主管！",  # 532
    "{SPEAKER_BAUTISTA}{PART_000}我们尽力而为。",  # 533
    "第 47 周结束",  # 534
    "{SPEAKER_DOPPLER}{PART_000}翻译小组，{PART_001}$animD04请开始吧。",  # 535
    "{SPEAKER_COLLINS}{PART_000}这周继续讨论了道德，{PART_001}也因此{PART_002}引出了两个新的难题：{PART_003}{SIG_N231} 和 {SIG_N214}。",  # 536
    "{SPEAKER_AKERS}{PART_000}$animD19“难题”这个词好像不太对。",  # 537
    "{SPEAKER_COLLINS}{PART_000}$animD21也许，{PART_001}该说争议点？",  # 538
    "{SPEAKER_DOPPLER}{PART_000}我猜，这是……{PART_001}$animD06让我想想……{PART_002}$animD22{SIG_N212} 和 {SIG_N211} 之间的争议？",  # 539
    "{SPEAKER_COLLINS}{PART_000}正是。{PART_001}上周，{PART_002}他们探讨了这两个 {SIG_N210} 在什么地方发生冲突，{PART_003}也就是如何看待 {SIG_N227} 和 {SIG_N229}，这两个概念都存在于单个 {SIG_N129} 内部。",  # 540
    "{SPEAKER_DOPPLER}{PART_000}我光是试着想象，{PART_001}$animD02都觉得非常……奇特。",  # 541
    "{SPEAKER_COLLINS}{PART_000}对于他们的 {SIG_N231} {SIG_N201}，我们的感受也差不多。{PART_001}$animD21其深层含义源于他们的生物构造。{PART_002}不过核心思想围绕的是个体临终时的看法。",  # 542
    "{SPEAKER_DOPPLER}{PART_000}这有什么好争论的？{PART_001}$animD16{SIG_N133} 很可怕。{PART_002}我想，任何由演化产生的生物都会有相同的本能。",  # 543
    "{SPEAKER_AKERS}{PART_000}$animD19这点他们和你一样，{PART_001}多普。",  # 544
    "{SPEAKER_COLLINS}{PART_000}但一个 {SIG_N129} 经历 {SIG_N133} 有两种方式：{PART_001}{SIG_N231} 和 {SIG_N232}。",  # 545
    "{SPEAKER_DOPPLER}{PART_000}$animD21怎么说？",  # 546
    "{SPEAKER_BAUTISTA}{PART_000}2 个 {SIG_N131}，1 个 {SIG_N129}。{PART_001}{SIG_N146} 先发生：{PART_002}{SIG_N231}。{PART_003}{SIG_N147} 先发生——",  # 547
    "{SPEAKER_DOPPLER}{PART_000}$animD10——{SIG_N232}。{PART_001}明白。{PART_002}$animD22哪一种更常见？{PART_003}我想他们的 {SIG_N135} 并不完全相同。",  # 548
    "{SPEAKER_AKERS}{PART_000}{SIG_N231} 更常见，{PART_001}但会带来某种……{PART_002}{SIG_N230}。",  # 549
    "{SPEAKER_DOPPLER}{PART_000}按照 {PLAYER_NAME} 的定义，{PART_001}$animD02我明白了。",  # 550
    "{SPEAKER_COLLINS}{PART_000}你最好亲自读这些传输，{PART_001}多普勒。{PART_002}不过综合来看，{PART_003}我们觉得最好用这句话理解：{PART_004}{SIG_N169} {SIG_N233} {SIG_N085}。",  # 551
    "{SPEAKER_DOPPLER}{PART_000}$animD21他们会 {SIG_N169} {SIG_N233}，这似乎并不合理。",  # 552
    "{SPEAKER_COLLINS}{PART_000}他们也这么认为。{PART_001}他们承认 {SIG_N169} {SIG_N233} {SIG_N085} {SIG_N100} {SIG_N155}。{PART_002}但这种认知仍无法完全阻止那种感受涌上心头。",  # 553
    "{SPEAKER_BAUTISTA}{PART_000}这是他们的缺陷。{PART_001}不理性。",  # 554
    "{SPEAKER_AKERS}{PART_000}是不理性，{PART_001}$animD19但我敢说他们自己也知道。{PART_002}只是他们知道的和感受到的之间存在一道鸿沟。{PART_003}……",  # 555
    "{SPEAKER_COLLINS}{PART_000}还有另一道鸿沟：{PART_001}$animD21{SIG_N136} 和 {SIG_N137} 得出了不同结论。{PART_002}{SIG_N136} {SIG_N086} {SIG_N212} {SIG_N202} {SIG_N085}。{PART_003}她认为生命是 1 个 {SIG_N234} {SIG_N129}，{PART_004}而不是 9 个 {SIG_N234} {SIG_N128}。",  # 556
    "{SPEAKER_BAUTISTA}{PART_000}那就是 {SIG_N196}：{PART_001}{SIG_N234} 的交换。",  # 557
    "{SPEAKER_DOPPLER}{PART_000}那 {SIG_N137} 怎么想？",  # 558
    "{SPEAKER_COLLINS}{PART_000}{SIG_N211}：{PART_001}这和他们涌现出的能力有关。{PART_002}{SIG_N101} 对现代 {SIG_N147} 而言毫无意义，{PART_003}没有那 8 个 {SIG_N146}，它什么都不是；{PART_004}反过来也一样。{PART_005}{SIG_N169} {SIG_N233} {SIG_N085} 这件事，{PART_006}对 {SIG_N137} 而言，{PART_007}其实忽视了他们存在中至关重要的一面。",  # 559
    "{SPEAKER_DOPPLER}{PART_000}我明白了。{PART_001}$animD07不过和往常一样，{PART_002}我还得花些时间消化这些想法。",  # 560
    "{SPEAKER_AKERS}{PART_000}就像凯莉说的，{PART_001}这些传输应该会比我们讲得更好。{PART_002}亲自体会完全是另一回事。",  # 561
    "{SPEAKER_DOPPLER}{PART_000}$animD06{SIG_N214}：{PART_001}这是 {SIG_N212} 和 {SIG_N211} 之间的第 3 个核心分歧。",  # 562
    "{SPEAKER_COLLINS}{PART_000}对，{PART_001}它围绕的是这样一个问题：{PART_002}{SIG_N101} 有何意义。",  # 563
    "{SPEAKER_BAUTISTA}{PART_000}我不这么理解。",  # 564
    "{SPEAKER_COLLINS}{PART_000}$animD21对 {SIG_N212} 来说，{PART_001}{SIG_N110} {SIG_N156} {SIG_N100} {SIG_N214} {SIG_N085}。{PART_002}他们透过这个视角看待是什么赋予了 {SIG_N101} 意义。{PART_003}而 {SIG_N211} 认为这只是大局的一部分，{PART_004}并不存在唯一答案。",  # 565
    "{SPEAKER_BAUTISTA}{PART_000}不。{PART_001}$animD20{SIG_N212} {SIG_N214} 讲的是回忆、{PART_002}分类、{PART_003}交流。{PART_004}{SIG_N101} 是这些的融合。{PART_005}{SIG_N211} 讲的是与 {SIG_N101} 直接交互，{PART_006}而不是回忆它。",  # 566
    "{SPEAKER_COLLINS}{PART_000}我的理解完全不是这样。",  # 567
    "{SPEAKER_BAUTISTA}{PART_000}那就是你错了。",  # 568
    "{SPEAKER_COLLINS}{PART_000}那是你的看法。",  # 569
    "{SPEAKER_DOPPLER}{PART_000}$animD23好了，{PART_001}好了。{PART_002}我很欣赏这场辩论。{PART_003}那么 {SIG_N136} 和 {SIG_N137}，{PART_004}$animD15各自站在哪一边？",  # 570
    "{SPEAKER_AKERS}{PART_000}这里 {SIG_N136} 支持 {SIG_N211}。{PART_001}在他看来，{PART_002}一切都指向 {SIG_N156}。{PART_003}{SIG_N214}、{SIG_N197}、那 5 个 {SIG_N204}——{PART_004}全都是好的。{PART_005}但 {SIG_N137} 的看法不同。{PART_006}{SIG_N137} 把全部 5 个 {SIG_N204} 都归到同一个概念下：{PART_007}{SIG_N214}。",  # 571
    "{SPEAKER_BAUTISTA}{PART_000}{SIG_N137} 脱离现实。",  # 572
    "{SPEAKER_COLLINS}{PART_000}$animD21她对现实的分析与她从何处获得意义并不是一回事。{PART_001}这是两码事，{PART_002}巴蒂斯塔博士。",  # 573
    "{SPEAKER_DOPPLER}{PART_000}$animD22两种观点我都很欣赏，{PART_001}$animD04我相信以后看这段录像的人也一样。{PART_002}不过该收尾了。",  # 574
    "{SPEAKER_AKERS}{PART_000}其实，多普，{PART_001}我们还知道了一件事。{PART_002}$animD19{SIG_N129} 只要短短 32 个 {SIG_N070} 就会成熟，快得吓人。{PART_003}我说的成熟，{PART_004}就是……{PART_005}{SIG_N144} 那方面，{PART_006}你懂的，{PART_007}繁殖那回事——",  # 575
    "{SPEAKER_DOPPLER}{PART_000}$animD02好，{PART_001}好，{PART_002}我明白。{PART_003}$animD14接着说。",  # 576
    "{SPEAKER_AKERS}{PART_000}好凶！{PART_001}$animD19总之，{SIG_N136} 给我们算了一笔账。{PART_002}288 {SIG_N070} {SIG_N135} 除以 32 个 {SIG_N070}，{PART_003}等于 9。",  # 577
    "{SPEAKER_DOPPLER}{PART_000}这有什么意义？",  # 578
    "{SPEAKER_AKERS}{PART_000}想想看。",  # 579
    "{SPEAKER_DOPPLER}{PART_000}288 / 32 等于 9……$animD06{PART_001}那就意味着——{PART_002}你是说有 9 代，{PART_003}$animD17同时活着。",  # 580
    "{SPEAKER_AKERS}{PART_000}对。{PART_001}也就是说，就在此刻，{PART_002}假设我们那群八条腿的朋友还活在那里，{PART_003}{SIG_N136} 估计自己的第 62,178 代后代还活着。",  # 581
    "{SPEAKER_DOPPLER}{PART_000}18 万年$animD00，{PART_001}6.2 万代。{PART_002}多么漫长的一段时间。",  # 582
    "{SPEAKER_AKERS}{PART_000}我跟你说，{PART_001}多普。{PART_002}一谈到太空，尺度就大得无法理解。",  # 583
    "{SPEAKER_DOPPLER}{PART_000}……{PART_001}$animD21这一周又干得很出色。{PART_002}$animD19我们心里的所有问题，{PART_003}$animD20甚至那些还没来得及问的，终于都有了答案。{PART_004}$animD22回家吧，{PART_005}至少是回到你们在阿拉斯加的临时住处，{PART_006}好好休息，{PART_007}恢复精神，{PART_008}$animD24准备像过去 47 周一样继续构建意义。",  # 584
    "{SPEAKER_AKERS}{PART_000}你知道我们会，{PART_001}多普。",  # 585
    "{SPEAKER_COLLINS}{PART_000}多普勒博士，{PART_001}你也要{PART_002}好好休息。",  # 586
    "{SPEAKER_BAUTISTA}{PART_000}祝顺利。",  # 587
    "{SPEAKER_DOPPLER}{PART_000}哈哈，{PART_001}$animD20说得对，巴蒂斯塔。{PART_002}$animD24祝顺利。",  # 588
]


def main() -> None:
    source = json.loads(SOURCE.read_text(encoding="utf-8"))
    if len(source) != len(translations):
        raise SystemExit(f"translation count mismatch: {len(translations)} != {len(source)}")
    result = [
        {"text_index": item["text_index"], "translated_text": translated}
        for item, translated in zip(source, translations, strict=True)
    ]
    OUTPUT.write_text(json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


if __name__ == "__main__":
    main()

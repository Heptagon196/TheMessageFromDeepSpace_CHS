import json
from pathlib import Path


BASE = Path(__file__).resolve().parent
SOURCE = BASE / "src_14_dialogue_chunks_1137_1236.json"
OUTPUT = BASE / "trans_14.json"
NEWTERMS = BASE / "newterms_14.txt"


CHUNKS = [
r'''000|||克洛伊和卡特
001|||{SPEAKER_AKERS}{PART_000}他们多大了？
002|||{SPEAKER_COLLINS}{PART_000}嗯？{PART_001}哦，{PART_002}克洛伊 16 岁。{PART_003}卡特 13 岁。
003|||{SPEAKER_AKERS}{PART_000}你有孩子都 16 年了，居然从没告诉我？！
004|||{SPEAKER_COLLINS}{PART_000}艾伦，{PART_001}我们才认识 8 个月！
005|||{SPEAKER_AKERS}{PART_000}哦，对。{PART_001}那他们是什么样的人？{PART_002}你带他们看过星星，告诉他们太空有多美、多像画一样吗？{PART_003}跟他们说过翻译工作的事吗？{PART_004}他们玩什么运动？{PART_005}打架能打赢多普勒家的孩子吗——
006|||{SPEAKER_COLLINS}{PART_000}等等等等，{PART_001}一个一个来！{PART_002}好吧，{PART_003}克洛伊喜欢科学，{PART_004}像她妈妈。{PART_005}他们大概知道我在做什么，但不知道事情的全貌。{PART_006}这么久没回家，当然是最难熬的。{PART_007}不过他们应该也因此明白了，这件事非常重要。{PART_008}听约翰在电话里说，{PART_009}卡特最近迷上了冰球。
007|||{SPEAKER_AKERS}{PART_000}那他们打得过多普勒家的孩子吗？
008|||{SPEAKER_COLLINS}{PART_000}多普勒的孩子大多都二十多岁了，{PART_001}所以恐怕不太可能。
009|||{SPEAKER_AKERS}{PART_000}那你可得赶紧训练他们！
010|||{SPEAKER_COLLINS}{PART_000}等等，{PART_001}你到底想干什么，{PART_002}都在说些什么？{PART_003}真拿你没办法。
011|||{SPEAKER_BAUTISTA}{PART_000}你现在才发现。
012|||图像游戏
013|||{SPEAKER_AKERS}{PART_000}这个小游戏还挺有意思！
014|||{SPEAKER_BAUTISTA}{PART_000}嗯？
015|||{SPEAKER_AKERS}{PART_000}就是这个小小的 {SIG_N165} {SIG_N077} {SIG_N166} {SIG_N077} {SIG_N167} 游戏！{PART_001}它画出一个 {SIG_N053}，我们只要把它猜出来！
016|||{SPEAKER_BAUTISTA}{PART_000}嗯。{PART_001}确实。
017|||再来一轮
018|||{SPEAKER_AKERS}{PART_000}$animA1游戏又来一轮！
019|||很久很久……
020|||{SPEAKER_BAUTISTA}{PART_000}8 {SIG_N002} 10 {SIG_N009} 6 {SIG_N006}。{PART_001}$animB5嗯。{PART_002}很久很久。
021|||掌握自己的未来
022|||{SPEAKER_AKERS}{PART_000}想他们吗？
023|||{SPEAKER_COLLINS}{PART_000}我的家人？{PART_001}当然想。
024|||{SPEAKER_AKERS}{PART_000}那他们为什么不来这里？{PART_001}我们也不知道翻译工作要持续多久。{PART_002}都已经 8 个月了，还可能更久。{PART_003}我知道你从前被带着满世界搬家，{PART_004}永远不知道未来会是什么样，{PART_005}可现在你能掌握自己的未来了。
025|||{SPEAKER_COLLINS}{PART_000}我太想念约翰、{PART_001}克洛伊{PART_002}和卡特了。{PART_003}我想念回到家，听他们讲这一天过得怎么样。{PART_004}想念给他们做晚饭，辅导功课。{PART_005}想念给克洛伊梳头，听她说起新交的朋友。{PART_006}也想听卡特说，他和朋友打算造一座堡垒。{PART_007}我怀疑他们最后根本不会动手，{PART_008}但还是爱听。{PART_009}可我{PART_010}也没有那么残忍。{PART_011}我还记得每两年搬一次家是什么滋味，{PART_012}刚把脚下的土地当成自己的家，就又被连根拔起。{PART_013}一辈子都在当新来的孩子，{PART_014}从没熟悉过一座城的大街小巷，{PART_015}从没和谁真正亲近，{PART_016}说了太多次再见。{PART_017}我不会为了他们赶进度，{PART_018}也真的很想他们。{PART_019}但这里不是他们能过得开心的地方。{PART_020}就算我能掌握自己的未来，{PART_021}也不想替他们决定未来。
026|||定义一大堆
027|||{SPEAKER_AKERS}{PART_000}$animA3你知道吗，每次我们费半天劲，{PART_001}定义一大堆词，{PART_002}$animA2结果只在两段传输里用过，我都觉得特别好笑。
028|||{SPEAKER_BAUTISTA}{PART_000}嗯？
029|||{SPEAKER_AKERS}{PART_000}$animA4刚才不就是嘛！{PART_001}我们花时间定义了 {SIG_N164}、{SIG_N165}、{SIG_N166} 和 {SIG_N167}，{PART_002}接着又定义了 {SIG_N168}，{PART_003}就为了说：{PART_004}$animA1{SIG_N168} {SIG_N140} {SIG_N164} {SIG_N033} {SIG_N119} {SIG_N140} {SIG_N164}。
030|||{SPEAKER_BAUTISTA}{PART_000}$animB1谁知道呢。{PART_001}那些词以后说不定还会出现。
031|||{SPEAKER_AKERS}{PART_000}$animA5现在又轮到什么 {SIG_N169} 和 {SIG_N170} 了，{PART_001}也不知道这些 {SIG_N087} 到底是什么意思……
032|||它们回来了
033|||{SPEAKER_BAUTISTA}{PART_000}{SIG_N165} 和它的朋友们又出现了。
034|||{SPEAKER_AKERS}{PART_000}看样子是。
035|||{SPEAKER_BAUTISTA}{PART_000}所以你错了。
036|||{SPEAKER_AKERS}{PART_000}我哪里错了？！
037|||{SPEAKER_BAUTISTA}{PART_000}那些词定义完又用上了。{PART_001}不只两段传输。
038|||{SPEAKER_AKERS}{PART_000}你真讨厌。
039|||{SPEAKER_BAUTISTA}{PART_000}不。{PART_001}日志结束。
040|||氢气
041|||{SPEAKER_AKERS}{PART_000}等等，{PART_001}刚才那段传输是不是说，{SIG_N168} {SIG_N172} 是……{PART_002}{SIG_N115} 氢气？
042|||{SPEAKER_COLLINS}{PART_000}{SIG_N057} {SIG_N014} 2 {SIG_N056} 1 {SIG_N015}：{PART_001}我想你说得对。
043|||{SPEAKER_AKERS}{PART_000}氢气可是高度易燃——{PART_001}我是说，能把飞艇炸掉的那种易燃。
044|||{SPEAKER_COLLINS}{PART_000}艾伦……
045|||{SPEAKER_AKERS}{PART_000}怎么了！{PART_001}兴登堡号出事都已经是 35 年前了！{PART_002}我只是陈述事实：{PART_003}氢气——{PART_004}不怎么稳定。{PART_005}所以他们的 {SIG_N172} {SIG_N174} {SIG_N085} 也说得通。
046|||恒星的影响
047|||{SPEAKER_COLLINS}{PART_000}$animC1艾伦，{PART_001}你看懂那段传输了吗？
048|||{SPEAKER_AKERS}{PART_000}$animA4当然看懂了！
049|||{SPEAKER_COLLINS}{PART_000}$animC3那怎么一直没开口？
050|||{SPEAKER_AKERS}{PART_000}问题就在这：{PART_001}$animA3我正想把这些信息拼起来。{PART_002}根据 {SIG_N141} 的 {SIG_N074}，{PART_003}$animC5它比 {SIG_N046} {SIG_N090} 小得多——
051|||{SPEAKER_BAUTISTA}{PART_000}$animB4{SIG_N046} 加油。
052|||{SPEAKER_AKERS}{PART_000}$animA1对！{PART_001}{SIG_N046} 加油！{PART_002}$animB0既然 {SIG_N141} 更小，{PART_003}那它很可能是一颗红矮星——{PART_004}温度更低、{PART_005}体积更小的 {SIG_N090}。
053|||{SPEAKER_COLLINS}{PART_000}那么，为什么 {SIG_N141} {SIG_N174} {SIG_N085} 呢？
054|||{SPEAKER_AKERS}{PART_000}$animA5这我{PART_001}就不确定了。{PART_002}红矮星进行越来越多的 {SIG_N089} 时，会逐渐升温、变亮。{PART_003}但在 {SIG_N168} 这样的时间尺度上，{PART_004}$animA2我不认为会有多大变化，{PART_005}除非它是颗年轻的 {SIG_N090}，{PART_006}才刚进入主序阶段。
055|||他们确实如此
056|||{SPEAKER_COLLINS}{PART_000}这个更让我着迷。{PART_001}{SIG_N101} {SIG_N086} {SIG_N140} {SIG_N172} {SIG_N174} {SIG_N085}。
057|||{SPEAKER_AKERS}{PART_000}是吗？
058|||{SPEAKER_COLLINS}{PART_000}这就像动物产生二氧化碳，{PART_001}植物产生氧气。{PART_002}不知道他们的 {SIG_N101} 是否也让 {SIG_N172} 达成了自身的平衡。
059|||没有“太阳”这个词
060|||{SPEAKER_AKERS}{PART_000}$animA1我对 {SIG_N136} 可有意见！{PART_001}怎么连 {SIG_N046} {SIG_N090} 的专用词都没有。
061|||{SPEAKER_BAUTISTA}{PART_000}$animB4太阳。
062|||{SPEAKER_AKERS}{PART_000}不是！{PART_001}$animA2我是说——{PART_002}$animA5哎呀，你真气人！{PART_003}$animB0我是说，{SIG_N136} 怎么没定义一个 {SIG_N042} 来指代 {SIG_N046} {SIG_N090}？{PART_004}$animA4我们有 {SIG_N141}，也有 {SIG_N046} {SIG_N090}。{PART_005}这也太偏心了！
063|||{SPEAKER_COLLINS}{PART_000}{SIG_N046} {SIG_N090} 并没有那么重要。{PART_001}$animC3它主要是用来对比，证明我们理解了 {SIG_N090} 和 {SIG_N141}。
064|||{SPEAKER_AKERS}{PART_000}可怎么也该{PART_001}$animA0给个面子吧。
065|||{SPEAKER_COLLINS}{PART_000}这让我想起“简洁定律”。{PART_001}$animC4常用词往往更短，因为这样能提高沟通效率。{PART_002}{SIG_N141} 的使用频率比 {SIG_N046} {SIG_N090} 高得多，{PART_003}所以它有自己的 {SIG_N042}，不必依靠 {SIG_N045} {SIG_N090}。
066|||{SPEAKER_AKERS}{PART_000}简洁个什么劲——{PART_001}$animA4我只觉得，{PART_002}这也太不尊重人了，{PART_003}{SIG_N136}！
067|||巴蒂斯塔的牢骚
068|||{SPEAKER_BAUTISTA}{PART_000}$animB1{SIG_N048} {SIG_N171} {SIG_N023} {SIG_N004} {SIG_N126}。{PART_001}不是 6。
069|||{SPEAKER_COLLINS}{PART_000}$animC1关键是 {SIG_N104} 这个 {SIG_N042}。
070|||{SPEAKER_BAUTISTA}{PART_000}{SIG_N104} 令人厌恶。{PART_001}$animB5最烂的 {SIG_N042}。
071|||{SPEAKER_COLLINS}{PART_000}{SIG_N104} 和 {SIG_N105} 明明非常有用！{PART_001}$animC4它们能突出特定语境或理解框架，{PART_002}帮助我们解开先前定义的 {SIG_N042} 中涉及的概念。{PART_003}作用非常强大。
072|||{SPEAKER_BAUTISTA}{PART_000}不。{PART_001}$animB3它会制造歧义，{PART_002}容许多种解释。{PART_003}$animB4模糊逻辑太可恶了。
073|||{SPEAKER_COLLINS}{PART_000}$animC5看来我们会一直各持己见。
074|||“永远”这个说法
075|||{SPEAKER_AKERS}{PART_000}凯莉，{PART_001}$animA5你帮我看看，是不是我想岔了。
076|||{SPEAKER_BAUTISTA}{PART_000}检查结果出来了。{PART_001}你不会高兴的。
077|||{SPEAKER_AKERS}{PART_000}去你的。{PART_001}我可比你正常多了。
078|||{SPEAKER_BAUTISTA}{PART_000}这下我们总算意见一致了。
079|||{SPEAKER_AKERS}{PART_000}$animA4喂——！{PART_001}你能不能——{PART_002}$animA5算了。{PART_003}凯莉，{PART_004}$animA3你用大白话给我解释一下他们的这句话：{PART_005}{SIG_N110} {SIG_N065} {SIG_N121}——{PART_006}$animA5这是什么意思？
080|||{SPEAKER_COLLINS}{PART_000}{SIG_N121} 接在时间之后，{PART_001}$animC3构成一个时间框架。{PART_002}{SIG_N065} 就只是，{PART_003}$animC5嗯，{PART_004}{SIG_N065}。{PART_005}而 {SIG_N110} 表示全部，{PART_006}$animC4一种集合，{PART_007}把一切都包含在内。{PART_008}所以我认为这句话的意思是：{PART_009}$animC5“永远”。
081|||{SPEAKER_AKERS}{PART_000}我的直觉也是这么说。{PART_001}$animA0谢谢你帮我确认。
082|||{SPEAKER_COLLINS}{PART_000}你似乎在专心想事情。{PART_001}我就不打扰了。
083|||它们一样？
084|||{SPEAKER_AKERS}{PART_000}1 {SIG_N070} 和 1 {SIG_N070}$animA1……{PART_001}$animA3它们一样……？
085|||{SPEAKER_BAUTISTA}{PART_000}{SIG_N012} {SIG_N004} 1。{PART_001}$animB1密码变了。
086|||{SPEAKER_COLLINS}{PART_000}再看看这个——{PART_001}$animC1{SIG_N012} {SIG_N004} 2。
087|||密码不断变化
088|||{SPEAKER_BAUTISTA}{PART_000}{SIG_N012} {SIG_N004} 3。{PART_001}嗯。
089|||{SPEAKER_COLLINS}{PART_000}密码一直在变……
090|||讲述一个故事
091|||{SPEAKER_BAUTISTA}{PART_000}序列还在继续。
092|||{SPEAKER_COLLINS}{PART_000}我知道为什么。{PART_001}他们在讲一个故事：{PART_002}{SIG_N140} 的故事。
093|||尚未定义
094|||{SPEAKER_COLLINS}{PART_000}等等。{PART_001}等一下。{PART_002}那个 {SIG_N042} 还没有定义。
095|||{SPEAKER_AKERS}{PART_000}这就有点反常了。
096|||{SPEAKER_COLLINS}{PART_000}之前没有哪段传输定义过 {SIG_N178}。{PART_001}所以这个故事就是为了引出它。
097|||原来有过
098|||{SPEAKER_COLLINS}{PART_000}$animC1原来真有过。
099|||{SPEAKER_AKERS}{PART_000}什么“在那里”？
100|||{SPEAKER_COLLINS}{PART_000}定义传输。{PART_001}……{PART_002}$animC5就是上周的那个？{PART_003}你不记得我们讨论过了？{PART_004}$animC3我们还没定义 {SIG_N178}，就已经见过它了。
101|||{SPEAKER_AKERS}{PART_000}$animA3我想起来了。
102|||{SPEAKER_COLLINS}{PART_000}什么？！
103|||{SPEAKER_AKERS}{PART_000}$animA5不好意思。{PART_001}我在想别的事。
104|||{SPEAKER_BAUTISTA}{PART_000}他的心早就飞到九霄云外了。
105|||{SPEAKER_AKERS}{PART_000}$animA1我的心飞到 {SIG_N140} 星球去了。
106|||数值类比
107|||{SPEAKER_COLLINS}{PART_000}巴蒂斯塔，{PART_001}$animC3看到那段传输了吗？{PART_002}用数值类比 {SIG_N178}。{PART_003}我想你会喜欢。
108|||{SPEAKER_BAUTISTA}{PART_000}不喜欢。
109|||{SPEAKER_COLLINS}{PART_000}真的？{PART_001}$animC1我还以为你会喜欢 {SIG_N105} 数字。
110|||{SPEAKER_BAUTISTA}{PART_000}坏就坏在 {SIG_N117}。{PART_001}它需要语境。{PART_002}$animB1还要理解 {SIG_N112}、{SIG_N113} 和偏差。
111|||{SPEAKER_COLLINS}{PART_000}$animC4我们可以用统计描述来处理，{PART_001}不是吗？
112|||{SPEAKER_BAUTISTA}{PART_000}可以，{PART_001}但那段传输没有这么做。{PART_002}$animC4它依赖对 {SIG_N117} 的先入之见。{PART_003}模糊，{PART_004}含混，{PART_005}$animC5不严谨。
113|||{SPEAKER_COLLINS}{PART_000}$animC5你这个看法倒很有意思。
114|||一个词
115|||{SPEAKER_COLLINS}{PART_000}这似乎和上一段传输相呼应。{PART_001}要怎么把它浓缩成一个 {SIG_N042} 呢？
116|||看来我们确实是
117|||{SPEAKER_BAUTISTA}{PART_000}$animB4你对此有意见？
118|||{SPEAKER_AKERS}{PART_000}$animA3我不觉得我们是 {SIG_N179}。{PART_001}我们会创造语言、火箭和计算机。
119|||{SPEAKER_BAUTISTA}{PART_000}{SIG_N045} {SIG_N099} {SIG_N179}，{PART_001}也会。
120|||{SPEAKER_AKERS}{PART_000}$animA5我知道，{PART_001}我知道。
121|||{SPEAKER_BAUTISTA}{PART_000}$animB3你完全不讲理。
122|||{SPEAKER_AKERS}{PART_000}我也没说自己讲理。{PART_001}可要是有个专门指我们的词就好了，{PART_002}$animA4指我们这种超级聪明的 {SIG_N179}。
123|||{SPEAKER_BAUTISTA}{PART_000}就算有，{PART_001}$animB2也不会包括你。
124|||{SPEAKER_AKERS}{PART_000}喂！
125|||这是辅助词吗？
126|||{SPEAKER_COLLINS}{PART_000}我说的不是正式语法意义上的分类，{PART_001}但把他们的词分为“辅助”和“目标”两类，会很有帮助。{PART_002}我必须问自己：{PART_003}{SIG_N186} 是辅助词吗，{PART_004}也就是专门为了定义另一个概念才引入的词？{PART_005}还是说，它本身就是最终要定义的概念？
127|||{SPEAKER_AKERS}{PART_000}那你怎么判断是哪一种？
128|||{SPEAKER_COLLINS}{PART_000}如果他们用这个词教我们新的东西，{PART_001}那它就是目标词。{PART_002}如果定义完马上用它来定义别的词，{PART_003}那我就会称它为辅助词。
129|||果然是辅助词
130|||{SPEAKER_AKERS}{PART_000}好吧，答案揭晓：{PART_001}“辅助词”。{PART_002}失望吗？
131|||{SPEAKER_COLLINS}{PART_000}词语永远不会让我失望。{PART_001}而眼前还有两个问题，{PART_002}{SIG_N189} 和 {SIG_N190}。
132|||又来一个
133|||{SPEAKER_COLLINS}{PART_000}又一个 {SIG_N160}？
134|||{SPEAKER_AKERS}{PART_000}关于 {SIG_N191} 的？
135|||{SPEAKER_COLLINS}{PART_000}这次我也想不出一个简单的、{PART_001}在人类世界中能与它对应的事物，{PART_002}好提出什么假说。{PART_003}{PLAYER_NAME} 可能得发挥想象力了。
136|||{SPEAKER_AKERS}{PART_000}至少这个是“目标”词！
137|||拆下来了……
138|||{SPEAKER_COLLINS}{PART_000}$animC1看到拆下来的 {SIG_N146}……{PART_001}$animC5我不喜欢。
139|||{SPEAKER_AKERS}{PART_000}$animA1看得我反胃！
140|||{SPEAKER_BAUTISTA}{PART_000}这只是模型。{PART_001}$animB1一幅解剖图。
141|||{SPEAKER_AKERS}{PART_000}$animA3就不能画得像人类的解剖图吗？{PART_001}$animA5画出剥去皮肤的人体剖面，让人能看到里面的器官？
142|||{SPEAKER_COLLINS}{PART_000}听你这么一说，{PART_001}$animC3我们也没好到哪里去。
143|||{SPEAKER_BAUTISTA}{PART_000}$animB3反正也没定义透明的 {SIG_N054}。
144|||{SPEAKER_COLLINS}{PART_000}$animC5那看来拆下来已经是最好的办法了……
145|||内与外
146|||{SPEAKER_COLLINS}{PART_000}{SIG_N169} 和 {SIG_N170} 的 {SIG_N192}。{PART_001}$animC1天哪！{PART_002}难道那是……？
147|||{SPEAKER_AKERS}{PART_000}哦——！{PART_001}这次又在琢磨什么？{PART_002}$animA3肯定有意思。
148|||{SPEAKER_COLLINS}{PART_000}$animC2不，不是，{PART_001}我只是想到……{PART_002}1 {SIG_N129} 等于 2 {SIG_N131}，{PART_003}$animC5对吧？{PART_004}$animA0虽说它们似乎会在 {SIG_N132} 前后结合，{PART_005}但它们一定会通过某种机制交换物质、互相交流。{PART_006}$animC3有了 {SIG_N169} {SIG_N192} 和 {SIG_N170} {SIG_N192}，{PART_007}我们或许就知道后一件事是怎么做到的了。
149|||9 等于 1 的胡话
150|||{SPEAKER_BAUTISTA}{PART_000}又是这种胡话。{PART_001}8 {SIG_N002} 1 {SIG_N005} {SIG_N004} 1。
151|||{SPEAKER_COLLINS}{PART_000}是 {SIG_N104} 概念，{PART_001}不是 {SIG_N105} 数学。
152|||{SPEAKER_BAUTISTA}{PART_000}处理 {SIG_N192} 时，{PART_001}他们是对的。{PART_002}把单位相加，得到正确数字。{PART_003}这里，{PART_004}我不赞同。
153|||{SPEAKER_COLLINS}{PART_000}这话有道理。
154|||{SPEAKER_BAUTISTA}{PART_000}嗯？
155|||{SPEAKER_COLLINS}{PART_000}为什么 24 个 {SIG_N192} 就是 24，{PART_001}9 个 {SIG_N193} 却是 1？
156|||确信我们有——
157|||{SPEAKER_COLLINS}{PART_000}{SIG_N044} 0 确信我们有 {SIG_N193}，{PART_001}却不确信我们有 {SIG_N162}？{PART_002}他们怎么能推断出来？
158|||没有无线电天线
159|||{SPEAKER_AKERS}{PART_000}他们知道我们没有无线电天线，{PART_001}对吧？{PART_002}我们不能直接 {SIG_N043} {SIG_N163} {SIG_N085}。{PART_003}得造机器来做。
160|||{SPEAKER_COLLINS}{PART_000}即使我们真的长着生物无线电天线，{PART_001}他们没把握判断到这种程度也很合理。
161|||巴蒂斯塔恍然大悟
162|||{SPEAKER_BAUTISTA}{PART_000}哦……
163|||{SPEAKER_COLLINS}{PART_000}怎么了，巴蒂斯塔？
164|||{SPEAKER_BAUTISTA}{PART_000}我明白了。{PART_001}……
165|||特蕾莎和艾伦很像？
166|||{SPEAKER_AKERS}{PART_000}话说巴蒂斯塔，{PART_001}你妻子是什么样的人？{PART_002}认识这么久，{PART_003}我对她一无所知。
167|||{SPEAKER_BAUTISTA}{PART_000}特蕾莎很善良。{PART_001}也很有主见。{PART_002}……{PART_003}嗓门很大。{PART_004}这一点让我想起你，{PART_005}艾伦。{PART_006}不过她比你好看多了。
168|||{SPEAKER_AKERS}{PART_000}哎呀，你真这么想？
169|||{SPEAKER_BAUTISTA}{PART_000}你受宠若惊什么。
170|||{SPEAKER_AKERS}{PART_000}你说我让你想起她！{PART_001}而且你显然很爱她！
171|||{SPEAKER_BAUTISTA}{PART_000}我说你丑。{PART_001}注意这个。
172|||{SPEAKER_AKERS}{PART_000}不不，{PART_001}这话我可不能当没听见。{PART_002}你真的挺喜欢我！
173|||{SPEAKER_BAUTISTA}{PART_000}结论错误。{PART_001}日志结束。
174|||{SPEAKER_AKERS}{PART_000}巴蒂斯塔，{PART_001}我把你当成好朋友。{PART_002}但我想你也知道，{PART_003}所以我还是叫你丑吧。
175|||{SPEAKER_BAUTISTA}{PART_000}好。
176|||不谈这件事
177|||{SPEAKER_AKERS}{PART_000}$animA5话说，巴蒂斯塔……
178|||{SPEAKER_BAUTISTA}{PART_000}嗯？
179|||{SPEAKER_AKERS}{PART_000}上周结束时那件事，{PART_001}嗯？{PART_002}$animA3你居然真的——
180|||{SPEAKER_BAUTISTA}{PART_000}不谈。
181|||{SPEAKER_AKERS}{PART_000}$animA4什么？！{PART_001}连后续都不让问——
182|||{SPEAKER_BAUTISTA}{PART_000}不。{PART_001}已经说得够多了。{PART_002}$animB4现在是新的一周。
183|||{SPEAKER_AKERS}{PART_000}哦，好吧！{PART_001}$animA5我不烦你了。
184|||{SPEAKER_BAUTISTA}{PART_000}$animB3很好。
185|||另一个故事
186|||{SPEAKER_COLLINS}{PART_000}{SIG_N012} {SIG_N004} 1！{PART_001}$animC1又是一个序列；{PART_002}又是一个故事！
187|||他们故事的开端
188|||{SPEAKER_COLLINS}{PART_000}这不只是另一个序列，{PART_001}也不只是另一个故事。{PART_002}这是他们的故事：{PART_003}{SIG_N146} 和 {SIG_N147} 的故事。
189|||五万年前
190|||{SPEAKER_COLLINS}{PART_000}500,000 个 {SIG_N070}……{PART_001}换算成地球年有多久？
191|||{SPEAKER_BAUTISTA}{PART_000}1 个 {SIG_N070} 等于 33 个地球日。{PART_001}1 个地球年等于 365 个地球日。{PART_002}大致除以 10。
192|||{SPEAKER_AKERS}{PART_000}5,000 个地球年以前……
193|||{SPEAKER_AKERS}{PART_000}等等，{PART_001}我漏了一个 0。
194|||{SPEAKER_COLLINS}{PART_000}那就是 18 万年前再往前 5 万年。
195|||避开了贬义
196|||{SPEAKER_AKERS}{PART_000}{SIG_N046} {SIG_N100} {SIG_N194}。{PART_001}我喜欢这个说法。
197|||{SPEAKER_COLLINS}{PART_000}看吧？{PART_001}你认为 {SIG_N179} 带有的负面含义，并非最终定论。
198|||谁知道什么
199|||{SPEAKER_COLLINS}{PART_000}这个角度很有意思。{PART_001}对 {SIG_N044} 0 来说，{PART_002}我们的存在显而易见，也显然是 {SIG_N194}。{PART_003}但 {SIG_N045} 仍然一无所知。''',
r'''200|||{SPEAKER_AKERS}{PART_000}我们仍然是 {SIG_N124}，{PART_001}这意味着在他们看来，{PART_002}其他 {SIG_N194} {SIG_N101} 的存在仍然是 {SIG_N124}。
201|||这难道是……
202|||{SPEAKER_COLLINS}{PART_000}{SIG_N043}，{SIG_N042}……{PART_001}{SIG_N045} {SIG_N195}……？{PART_002}会是这个吗？
203|||为什么是 512？
204|||{SPEAKER_AKERS}{PART_000}为什么要 {SIG_N043} 512？{PART_001}$animA1这个数字有什么特别之处吗？
205|||{SPEAKER_BAUTISTA}{PART_000}{PLAYER_NAME}，{PART_001}回答他。{PART_002}……{PART_003}8 的立方。
206|||{SPEAKER_AKERS}{PART_000}$animA3哦，对。{PART_001}对他们来说就是 1-0-0-0。
207|||{SPEAKER_BAUTISTA}{PART_000}$animB4离翻译开头任意远的位置。
208|||两大支柱
209|||{SPEAKER_COLLINS}{PART_000}{SIG_N195} 与 {SIG_N197}，{PART_001}是 {SIG_N194} {SIG_N101} 的两大支柱。
210|||第四维度
211|||{SPEAKER_BAUTISTA}{PART_000}{SIG_N239} 加上另一个？{PART_001}{SIG_N136} 预期存在第四个维度。
212|||264 人真多
213|||{SPEAKER_AKERS}{PART_000}264 个人可真不少。
214|||{SPEAKER_BAUTISTA}{PART_000}“人”。
215|||{SPEAKER_AKERS}{PART_000}你也太咬文嚼字了。
216|||{SPEAKER_COLLINS}{PART_000}264 个 {SIG_N194} {SIG_N101} {SIG_N128} 的确非常多。
217|||{SPEAKER_BAUTISTA}{PART_000}他们建造的是 {SIG_N044} 0。{PART_001}这需要材料科学家、{PART_002}计算机程序员、{PART_003}语言学家、{PART_004}建筑工程师。
218|||{SPEAKER_AKERS}{PART_000}别忘了还要有天文学家，寻找很可能存在生命的行星。{PART_001}还要计算抵达那里的轨道。{PART_002}不过说到这个，{PART_003}不知道他们是否了解相对论。
219|||错误
220|||{SPEAKER_AKERS}{PART_000}{SIG_N123}？{PART_001}真的？{PART_002}这居然行得通，{PART_003}{PLAYER_NAME}？{PART_004}{SIG_N200} 0 {SIG_N004} {SIG_N027}。
221|||{SPEAKER_COLLINS}{PART_000}这一点仍然是 {SIG_N123}。{PART_001}它非常简单，不可能弄错。{PART_002}他们是有意在说明什么。
222|||{SPEAKER_AKERS}{PART_000}真是个奇怪的观点。
223|||两种性质
224|||{SPEAKER_COLLINS}{PART_000}或许这正是你一直在找的明确定义，{PART_001}艾伦。
225|||{SPEAKER_AKERS}{PART_000}嗯？
226|||{SPEAKER_COLLINS}{PART_000}{SIG_N200} 有两种性质：{PART_001}1. 处于 {SIG_N027} {SIG_N077} {SIG_N028} 的状态。{PART_002}2. 处于 {SIG_N123} {SIG_N077} {SIG_N124} 的状态。
227|||{SPEAKER_AKERS}{PART_000}看来 {SIG_N136} 说什么，{PART_001}就是什么。
228|||{SPEAKER_COLLINS}{PART_000}不过我更好奇的是这个新词，以及它有何不同。
229|||定义加速
230|||{SPEAKER_COLLINS}{PART_000}真有意思。{PART_001}$animC3{SIG_N136} 加快了定义的节奏。
231|||{SPEAKER_AKERS}{PART_000}是吗？
232|||{SPEAKER_COLLINS}{PART_000}以前，我们会用更多传输来证明自己理解了每个 {SIG_N042}。{PART_001}$animC4先正式定义，{PART_002}再举例说明，{PART_003}展示它的极端情况，{PART_004}最后还要证明我们理解了更广泛的概念。{PART_005}可现在，{PART_006}$animC5感觉进展快得像在飞，{PART_007}接连定义出一个个 {SIG_N042}。
233|||{SPEAKER_AKERS}{PART_000}大概是 {SIG_N136} 信任我们了。{PART_001}$animA1我们都走到这一步了，{PART_002}对吧？
234|||{SPEAKER_COLLINS}{PART_000}$animA0也可能是我们的词汇基础更扎实了。
235|||{SPEAKER_BAUTISTA}{PART_000}$animB1也可能是 {SIG_N045} 懒了。
236|||{SPEAKER_COLLINS}{PART_000}巴蒂斯塔！{PART_001}$animC4不许说我们星际朋友的坏话！
237|||我的天哪！
238|||{SPEAKER_AKERS}{PART_000}说到定义加速……{PART_001}我的天哪！
239|||{SPEAKER_COLLINS}{PART_000}我知道。
240|||这五个词
241|||{SPEAKER_AKERS}{PART_000}天哪，{PART_001}他们还没说完。
242|||{SPEAKER_COLLINS}{PART_000}安静点，艾伦，{PART_001}我在专心看。{PART_002}这五个 {SIG_N042} 意义重大。
243|||先前的描述
244|||{SPEAKER_AKERS}{PART_000}之前他们说 {SIG_N125} {SIG_N074} 是 {SIG_N239} 和 {SIG_N065}。
245|||层级
246|||{SPEAKER_COLLINS}{PART_000}他们把 {SIG_N201} 划分成不同层级。{PART_001}每个 {SIG_N128} 都属于 {SIG_N199}、{SIG_N203}，同时也有自己的视角。
247|||尚未再次出现
248|||{SPEAKER_COLLINS}{PART_000}{SIG_N212} 和 {SIG_N211} 已经定义了，{PART_001}但还没再次出现。
249|||{SPEAKER_AKERS}{PART_000}担心吗？
250|||{SPEAKER_COLLINS}{PART_000}嗯，{PART_001}不——{PART_002}只是好奇。
251|||凯莉也一样
252|||{SPEAKER_COLLINS}{PART_000}{SIG_N136}，{PART_001}我也有同感。{PART_002}凯莉 {SIG_N086} {SIG_N195} {SIG_N100} {SIG_N213}。
253|||温暖的心意
254|||{SPEAKER_COLLINS}{PART_000}{SIG_N106} {SIG_N108} {SIG_N130} {SIG_N215}……{PART_001}真是温暖的心意。
255|||更省能量
256|||{SPEAKER_AKERS}{PART_000}{SIG_N037} {SIG_N044} 1 {SIG_N100} {SIG_N161}。{PART_001}一个 {SIG_N044} 显然不可能是 {SIG_N161}，{PART_002}这还用说，{PART_003}但它包含的信息……{PART_004}从能耗上看，肯定更节省能量。
257|||能够产生……？
258|||{SPEAKER_COLLINS}{PART_000}要 {SIG_N196}，{PART_001}就需要输入和输出。{PART_002}也就是说，在这里，{PART_003}他们能产生 {SIG_N191}。{PART_004}这可能吗？
259|||{SPEAKER_AKERS}{PART_000}这我可一点头绪都没有。
260|||{SPEAKER_BAUTISTA}{PART_000}我不是生物学家。
261|||如果埃克斯能
262|||{SPEAKER_BAUTISTA}{PART_000}$animB5埃克斯。
263|||{SPEAKER_AKERS}{PART_000}哦！{PART_001}有何贵干！
264|||{SPEAKER_BAUTISTA}{PART_000}如果你能 {SIG_N217}，{PART_001}你肯定会往 {SIG_N166} 里钻，{PART_002}$animB4还会非常吵。{PART_003}烦死所有人。
265|||{SPEAKER_AKERS}{PART_000}你对妻子也这么刻薄吗？
266|||{SPEAKER_BAUTISTA}{PART_000}不。{PART_001}$animB5她很可爱。
267|||{SPEAKER_AKERS}{PART_000}那我不可爱？{PART_001}$animB0……{PART_002}别回答。
268|||{SPEAKER_BAUTISTA}{PART_000}嗯哼。
269|||计算一下
270|||{SPEAKER_AKERS}{PART_000}嘿，书呆子，{PART_001}能算出那些 {SIG_N039} {SIG_N066} 吗？
271|||{SPEAKER_BAUTISTA}{PART_000}不能。{PART_001}去看那张漂亮的 {SIG_N068} 参考页，{PART_002}书呆子。
272|||{SPEAKER_AKERS}{PART_000}书呆子？！{PART_001}不许这么叫我！
273|||{SPEAKER_BAUTISTA}{PART_000}你活该倒霉。
274|||没有翅膀
275|||{SPEAKER_AKERS}{PART_000}看来他们做不到。
276|||{SPEAKER_BAUTISTA}{PART_000}没有翅膀会很难。
277|||{SPEAKER_AKERS}{PART_000}他们只有那些 {SIG_N146}！
278|||跨度巨大
279|||{SPEAKER_BAUTISTA}{PART_000}{SIG_N151} {SIG_N129} {SIG_N074}$animB1 很反常。{PART_001}跨度达 21.2328 个 {SIG_N075}。
280|||{SPEAKER_AKERS}{PART_000}仔细一想，{PART_001}$animA4那是多少？{PART_002}300 磅？
281|||{SPEAKER_BAUTISTA}{PART_000}还差一点。
282|||{SPEAKER_AKERS}{PART_000}$animA5真奇怪。
283|||{SPEAKER_BAUTISTA}{PART_000}$animB4尤其是 {SIG_N114} 只有 2.2 个 {SIG_N075}。
284|||足够热爱
285|||{SPEAKER_AKERS}{PART_000}{SIG_N136} {SIG_N100} {SIG_N109} {SIG_N216} {SIG_N222}。{PART_001}抱歉啊，伙计，{PART_002}但如果我理解的 {SIG_N222} 没错，{PART_003}那我可没这种感受。
286|||{SPEAKER_BAUTISTA}{PART_000}我不信。
287|||{SPEAKER_AKERS}{PART_000}为什么不信？{PART_001}说不定我也会 {SIG_N108} {SIG_N220} {SIG_N216} 呢！
288|||{SPEAKER_BAUTISTA}{PART_000}你没一样拿手。{PART_001}也没有真正热爱过什么。
289|||{SPEAKER_AKERS}{PART_000}是吗？！{PART_001}你在这颗星球上总共就爱两样东西，还好意思这么说我。
290|||{SPEAKER_BAUTISTA}{PART_000}哪两样……？
291|||{SPEAKER_AKERS}{PART_000}计算机，{PART_001}还有你妻子。{PART_002}恐怕优先顺序{PART_003}也是这样。
292|||{SPEAKER_BAUTISTA}{PART_000}嗯。{PART_001}我的兴趣一向不多。
293|||{SPEAKER_AKERS}{PART_000}我知道！{PART_001}而我正好相反！{PART_002}我喜欢的东西很多，老实说，{PART_003}可哪一样都称不上高手。
294|||{SPEAKER_BAUTISTA}{PART_000}因为你对任何兴趣都不够热爱。
295|||{SPEAKER_AKERS}{PART_000}真是气死我了！{PART_001}你就会歪曲我的话！
296|||{SPEAKER_BAUTISTA}{PART_000}呵呵。
297|||艾伦明白了
298|||{SPEAKER_COLLINS}{PART_000}{SIG_N227} 这个概念很特别。{PART_001}到现在我还是很难准确界定它。
299|||{SPEAKER_AKERS}{PART_000}真的？{PART_001}我觉得一点也不难！
300|||{SPEAKER_COLLINS}{PART_000}哦，是吗？
301|||{SPEAKER_BAUTISTA}{PART_000}那就说来听听。
302|||{SPEAKER_AKERS}{PART_000}{SIG_N227} {SIG_N100} {SIG_N169} {SIG_N129} {SIG_N223} {SIG_N216}。{PART_001}而且，{PART_002}{SIG_N227} {SIG_N099} {SIG_N079}。
303|||{SPEAKER_COLLINS}{PART_000}你只是在复述陨石给出的定义。
304|||{SPEAKER_AKERS}{PART_000}因为意思就是这个。
305|||{SPEAKER_COLLINS}{PART_000}你确实用了{PLAYER_NAME}的词典，{PART_001}没错。{PART_002}可你一点都没说清这一定义该如何应用。{PART_003}我为什么要跟你解释这些？{PART_004}你明明知道自己在胡闹，真是个小丑。
306|||{SPEAKER_AKERS}{PART_000}小丑？！{PART_001}你怎么能这么叫同事！
307|||{SPEAKER_COLLINS}{PART_000}平常当然不会。{PART_001}可只有你能把每个人都逼到极限。
308|||{SPEAKER_BAUTISTA}{PART_000}小丑男。
309|||{SPEAKER_AKERS}{PART_000}别跟着起哄！{PART_001}你还像平时一样闭嘴吧！
310|||{SPEAKER_BAUTISTA}{PART_000}呵呵呵。
311|||别告诉我们……
312|||{SPEAKER_AKERS}{PART_000}{SIG_N229}，{PART_001}是吗？
313|||{SPEAKER_COLLINS}{PART_000}别又说你已经“理解”它了。{PART_001}$animC4你肯定又要复述{PLAYER_NAME}的话。
314|||{SPEAKER_AKERS}{PART_000}话说，{PART_001}凭什么是“{PLAYER_NAME}的”词？{PART_002}$animA1我们才是翻译小组！{PART_003}这些词是大家的！
315|||{SPEAKER_COLLINS}{PART_000}每一个词都是{PLAYER_NAME}命名的。{PART_001}$animC2这是人家的解读。
316|||{SPEAKER_AKERS}{PART_000}不对，{PART_001}$animA4才不是！{PART_002}他们偶尔也用过我们的假说，{PART_003}不是吗？
317|||{SPEAKER_BAUTISTA}{PART_000}嗯？{PART_001}也许用过我或柯林斯的。
318|||{SPEAKER_AKERS}{PART_000}你觉得他们从没用过我的假说？{PART_001}$animA2那 {SIG_N095} 呢？
319|||{SPEAKER_BAUTISTA}{PART_000}参考页签。
320|||{SPEAKER_AKERS}{PART_000}真气人，{PART_001}$animA1那一页是我写的！
321|||{SPEAKER_COLLINS}{PART_000}可以肯定的是，{SIG_N221} 没有采用你的假说，{PART_001}$animC5绝对没有。
322|||{SPEAKER_AKERS}{PART_000}听着，{PART_001}那段定义传输一次说了三个词！{PART_002}我看到 {SIG_N170} {SIG_N165} 出现在附近，自然就想到了爬行。{PART_003}$animA5我以为这就是那三个词的共同主题，{PART_004}行了吧？
323|||{SPEAKER_COLLINS}{PART_000}$animC3假说后面要加上问号，从来都不是好兆头。
324|||{SPEAKER_BAUTISTA}{PART_000}$animB4除了 {SIG_N012}。
325|||{SPEAKER_COLLINS}{PART_000}这点我同意。
326|||这些是什么？
327|||{SPEAKER_COLLINS}{PART_000}艾伦，{PART_001}我知道你开玩笑说自己理解 {SIG_N227} 和 {SIG_N229}，{PART_002}可这两个词也让我一头雾水。
328|||{SPEAKER_AKERS}{PART_000}我可没说是在开玩笑……
329|||{SPEAKER_COLLINS}{PART_000}我能理清他们借助 {SIG_N105} 对几何等概念所作的指涉。{PART_001}$animB4可要把这个概念归结成一个词，实在很棘手。
330|||{SPEAKER_AKERS}{PART_000}$animA3也许它就是那种无法翻译的词。
331|||{SPEAKER_COLLINS}{PART_000}没有什么词“无法翻译”。{PART_001}至少在人类语言的范畴内没有。
332|||{SPEAKER_AKERS}{PART_000}$animA2一个都没有？
333|||{SPEAKER_COLLINS}{PART_000}也许找不到一一对应的词，{PART_001}翻译时也可能丢失某些附带含义。{PART_002}但只要使用足够多的词，{PART_003}人类语言可以自由表达任何事物。{PART_004}这种特性叫开放性，{PART_005}也就是沟通任何想法的能力。
334|||{SPEAKER_AKERS}{PART_000}这话是不是太偏向人类了？{PART_001}人类当然能和人类交谈。{PART_002}大家的脑子都大同小异。
335|||{SPEAKER_BAUTISTA}{PART_000}$animB4你的似乎缺了一部分。
336|||{SPEAKER_AKERS}{PART_000}闭嘴！{PART_001}$animB0我只是觉得，“人类能谈论任何事”这个推论也太大胆了。
337|||{SPEAKER_COLLINS}{PART_000}你能想到什么无法交流的事物吗？
338|||{SPEAKER_AKERS}{PART_000}呃……{PART_001}想不到，{PART_002}$animA4可我们都是人类！{PART_003}我们有相同的视角，{PART_004}$animA5也有相同的共同经历。{PART_005}所以我想到的一切都能传达给你，并不奇怪。
339|||{SPEAKER_COLLINS}{PART_000}我不同意。{PART_001}$animC3关键在于抽象。{PART_002}人脑会独立挑选并归纳不同物体的特征。{PART_003}苹果是红的，{PART_004}有三维形态，{PART_005}还有甜味。{PART_006}我们可以任意抽取这些特征，再运用到别处。{PART_007}我能想象一个既不红、也不是苹果的物体。{PART_008}也能想象一个只有红色、没有其他特征的物体。{PART_009}$animC5同样也能想象一个有三维形态、却不是苹果的物体。
340|||{SPEAKER_AKERS}{PART_000}或者不是苹果，却很甜的东西。{PART_001}$animA2我懂了。{PART_002}而且谈到语言，我最信任的就是你，{PART_003}胜过所有人。{PART_004}$animA5不过这事还得在我肚肚里待一阵。
341|||{SPEAKER_BAUTISTA}{PART_000}哪个成年人会说“肚肚”……
342|||两个概念再次出现
343|||{SPEAKER_COLLINS}{PART_000}$animC1它们出现了！{PART_001}{SIG_N212} 和 {SIG_N211}！
344|||{SPEAKER_AKERS}{PART_000}$animA1除了{PLAYER_NAME}给的定义，{PART_001}我完全不知道这两个词是什么意思。
345|||{SPEAKER_COLLINS}{PART_000}$animA0我也不知道。{PART_001}所以才会这么兴奋。{PART_002}$animC3就好像之前只是让它们登场，做好铺垫。{PART_003}$animC5现在终于要揭晓答案了。
346|||为何钟情计算机
347|||{SPEAKER_COLLINS}{PART_000}巴蒂斯塔，{PART_001}如果你不介意我问，{PART_002}是什么让你对计算机如此着迷——{PART_003}又如此投入？
348|||{SPEAKER_BAUTISTA}{PART_000}计算机很好。{PART_001}……
349|||{SPEAKER_AKERS}{PART_000}想过给人写演讲稿吗？
350|||{SPEAKER_BAUTISTA}{PART_000}没有，{PART_001}我妻子就是干这个的。
351|||{SPEAKER_AKERS}{PART_000}什么？
352|||{SPEAKER_BAUTISTA}{PART_000}嗯哼。
353|||{SPEAKER_AKERS}{PART_000}你是认真的。{PART_001}你娶了个话匣子。
354|||{SPEAKER_BAUTISTA}{PART_000}不是话匣子。{PART_001}是演讲稿撰稿人。
355|||{SPEAKER_AKERS}{PART_000}你是说，沉默寡言的巴蒂斯塔，娶了个靠文字谋生的人？
356|||{SPEAKER_BAUTISTA}{PART_000}我不沉默。{PART_001}{PLAYER_NAME}才沉默。{PART_002}有话可说时，我自然会说。
357|||{SPEAKER_AKERS}{PART_000}而你的“话”经常就是拿我开涮。
358|||{SPEAKER_BAUTISTA}{PART_000}我认为很有必要。
359|||为何钟情计算机（二）
360|||{SPEAKER_COLLINS}{PART_000}巴蒂斯塔，{PART_001}世上有许多美好的事物。{PART_002}语言妙不可言，{PART_003}夜空令人陶醉，{PART_004}材料科学更有无穷妙用。{PART_005}说实话，{PART_006}无论看向哪里，{PART_007}人总能发现一些很有意义的东西。{PART_008}但关键在于，这些学科中总有某种特别的东西，能与我们每个人产生共鸣。{PART_009}巴蒂斯塔博士，{PART_010}到底是什么吸引你投入计算机？
361|||{SPEAKER_BAUTISTA}{PART_000}嗯。{PART_001}我可以说，是因为它们精确。{PART_002}也可以说，计算和算法支撑着一切知识。{PART_003}还可以说，数学是宇宙的语言，{PART_004}而计算机是我们与它交互的方式。{PART_005}但我的理由比这些简单得多。{PART_006}我喜欢和计算机打交道。
362|||{SPEAKER_AKERS}{PART_000}这谁都看得出来，{PART_001}可她问的是——
363|||{SPEAKER_COLLINS}{PART_000}巴蒂斯塔，{PART_001}请继续说。
364|||{SPEAKER_BAUTISTA}{PART_000}对我来说……{PART_001}嗯。{PART_002}对我来说，{PART_003}{PART_004}我做任何事都有明确的意图。{PART_005}这似乎并不常见。{PART_006}遇见特蕾莎以前，我并不知道。
365|||{SPEAKER_AKERS}{PART_000}你妻子……
366|||{SPEAKER_COLLINS}{PART_000}她只是生活，{PART_001}活在当下，{PART_002}去感受，{PART_003}去观察，{PART_004}去体验。{PART_005}音乐让她想跳舞，她就跳舞。{PART_006}遇到有趣的事，她就大笑。{PART_007}心里被触动，她就落泪。
367|||{SPEAKER_COLLINS}{PART_000}这些事你都不会做吗？
368|||{SPEAKER_BAUTISTA}{PART_000}会。{PART_001}但大多数时候，这些事还要经过另一层——{PART_002}意图。{PART_003}向内审视，{PART_004}分析一切，{PART_005}不断追寻第一性原理。{PART_006}这让我对自己更有把握，{PART_007}确信行动符合处境，{PART_008}却也很乏味。{PART_009}我并未真正投入生活。{PART_010}我只是站在电话另一端，{PART_011}等着听指示，告诉我该有什么感受。{PART_012}我担心自己缺少了什么。
369|||{SPEAKER_COLLINS}{PART_000}你担心自己错过了什么？
370|||{SPEAKER_BAUTISTA}{PART_000}嗯。{PART_001}我怕自己错过的，是关掉计算机后才发现 8 个小时已经过去。{PART_002}仿佛只是一眨眼。{PART_003}是特蕾莎和我结束一场谈话，{PART_004}再望向窗外，才发现太阳早已落山。
371|||最快乐的词典
372|||{SPEAKER_DOPPLER}{PART_000}信号 -245——{PART_001}就是这个，{PART_002}对吧？
373|||{SPEAKER_AKERS}{PART_000}$animA1对！
374|||{SPEAKER_COLLINS}{PART_000}我们成功确认了定义传输，{PART_001}这下$animC5明白了吧？
375|||{SPEAKER_DOPPLER}{PART_000}也就是说……{PART_001}$animA0呼，{PART_002}说错了别见怪。{PART_003}我们有一本快乐的词典，{PART_004}还有快乐的生活？
376|||{SPEAKER_DOPPLER}{PART_000}怎么样？
377|||{SPEAKER_BAUTISTA}{PART_000}$animB5精彩。
378|||{SPEAKER_AKERS}{PART_000}$animA4说得一点没错，{PART_001}多普！
379|||{SPEAKER_COLLINS}{PART_000}$animC5而且我们的词典现在就是最快乐的词典了。
380|||{SPEAKER_DOPPLER}{PART_000}已经快乐到顶了。
381|||{SPEAKER_AKERS}{PART_000}$animA5我也开心到顶了 :)
382|||一晃而过
383|||{SPEAKER_AKERS}{PART_000}50 周了，{PART_001}$animA3是吧？{PART_002}感觉像过了漫长的一生。
384|||{SPEAKER_BAUTISTA}{PART_000}真的？{PART_001}$animB2对我来说只是一瞬间。
385|||{SPEAKER_DOPPLER}{PART_000}我一直在翻译工作的外围，感受不太一样。{PART_001}几乎像看着一个孩子长大。{PART_002}$animA5你未必总知道那颗小脑袋里在想什么，{PART_003}但看着他们长成自己的模样，实在很美。
386|||{SPEAKER_COLLINS}{PART_000}你这么说很有意思。{PART_001}$animC5感觉我已经认识你们四个很久了。{PART_002}……
387|||一个物种
388|||{SPEAKER_DOPPLER}{PART_000}以我们如今的了解，{PART_001}太空深处的某个地方，存在一个有智慧、{PART_002}有感情、{PART_003}社会性极强的物种。{PART_004}他们充满好奇，热爱自身之外的生命。{PART_005}他们迫切地想找到其他生命。{PART_006}……{PART_007}我想，我们早就知道这样的物种存在了。
389|||还是不想说再见
390|||{SPEAKER_AKERS}{PART_000}我还是不想说再见。
391|||{SPEAKER_COLLINS}{PART_000}艾伦，{PART_001}我说接下来这些话时，能不能假装没看到我流泪？
392|||{SPEAKER_AKERS}{PART_000}哈哈，我会尽力的，{PART_001}凯莉。
393|||{SPEAKER_COLLINS}{PART_000}我不想和你们任何一个说再见。{PART_001}{SIG_N136}、{PART_002}{SIG_N137}、{PART_003}多普勒、{PART_004}巴蒂斯塔、{PART_005}艾伦，{PART_006}还有{PLAYER_NAME}。{PART_007}我不想和你们任何一个说再见。{PART_008}但如果我学到了什么，{PART_009}那就是我们随时可以再次说声你好。
394|||来自深空的讯息
395|||{SPEAKER_DOPPLER}{PART_000}那么，到此为止了。{PART_001}977。
396|||{SPEAKER_AKERS}{PART_000}对。{PART_001}$animA5就到这里。{PART_002}……
397|||{SPEAKER_BAUTISTA}{PART_000}$animB1没有 {SIG_N012}。
398|||{SPEAKER_COLLINS}{PART_000}$animC4这说明我们已经无话可说。{PART_001}$animC3没有任何话能再引出新的传输了。
399|||{SPEAKER_DOPPLER}{PART_000}$animB5那么，翻译工作已经完成。{PART_001}50 周，{PART_002}977 段传输。{PART_003}$animC5你们四个不仅解开了每一段传输，{PART_004}还找到了意义深远的解读。{PART_005}这本词典包含 245 个频率和 8 个数字。{PART_006}正是通过这些词，我们学会像他们一样思考，{PART_007}学会看待他们，{PART_008}学会如何谈论这个共同生活的宇宙，{PART_009}知道了他们把哪里称作家园，{PART_010}学会接纳他们的语言，{PART_011}知道他们尚不了解我们，{PART_012}却希望有朝一日能够相识。{PART_013}正是通过这些词，我们了解了他们是什么，{PART_014}是谁，{PART_015}以及如何走到今天。{PART_016}我们了解了他们如何看待生命、{PART_017}死亡，{PART_018}以及两者之间度过的时光。{PART_019}即使别无其他，{PART_020}他们也托付给我们一份礼物，{PART_021}一份足以彻底改变人类文明的礼物。{PART_022}……{PART_023}毫无疑问，这是我们一生中最不可思议的发现。{PART_024}我很高兴能参与其中。{PART_025}但坦白说，{PART_026}此刻我只感到感激。{PART_027}感谢命运让我找到你们四个，{PART_028}也感谢各位始终坚持，终于理解了这一切。{PART_029}我知道这绝不容易。{PART_030}谢谢你们。{PART_031}……''',
r'''400|||{SPEAKER_AKERS}{PART_000}多普，{PART_001}$animA3其实该道谢的人是我。{PART_002}$animA4谢谢你让我加入。{PART_003}这件事，呃，{PART_004}$animA5哈哈……{PART_005}对我的意义比你想象中更大。{PART_006}不过，{PART_007}$animA2我想你也明白。
401|||{SPEAKER_DOPPLER}{PART_000}你是个好人，{PART_001}艾伦。
402|||{SPEAKER_AKERS}{PART_000}能参与这一切……{PART_001}$animA5我真的很幸运。{PART_002}谢谢你们一直陪着我，{PART_003}各位。{PART_004}我想自己再也遇不到比你们更好的一群人了。{PART_005}我很幸运。
403|||{SPEAKER_BAUTISTA}{PART_000}我也一样。{PART_001}$animB1致所有正在观看这段录像的人：{PART_002}希望你们喜欢。{PART_003}不过，我们也要为埃克斯博士的胡闹向各位道歉。
404|||{SPEAKER_AKERS}{PART_000}喂！
405|||{SPEAKER_BAUTISTA}{PART_000}$animB3我们尽了全力，试着理解这则讯息。{PART_001}宇宙是一个完整的系统；{PART_002}我们都在其中扮演自己的角色。{PART_003}所以……{PART_004}$animB4去 {SIG_N154} 吧。{PART_005}去沟通，{PART_006}去分享，{PART_007}活在当下。{PART_008}或者找到能让你做到这些的人。{PART_009}就像这个团队。{PART_010}你们四位……{PART_011}$animB5与你们共事是我的荣幸。{PART_012}埃克斯博士、{PART_013}柯林斯博士、{PART_014}{PLAYER_NAME}、{PART_015}多普勒博士——{PART_016}希望有一天还能再次合作。{PART_017}……
406|||{SPEAKER_COLLINS}{PART_000}我，{PART_001}呃……{PART_002}$animC2能得到这个机会，我欣喜若狂。{PART_003}从道理上说，{PART_004}很容易明白这件事为什么意义如此重大。{PART_005}$animC4这是人类第一次接触外星生命！{PART_006}$animC3当然令人惊叹！{PART_007}$animC5可我还从中学到了不同的一课。{PART_008}$animC1正是我们一次又一次、{PART_009}一周又一周的{PART_010}点滴行动，{PART_011}让这段旅程真正被温暖包裹，{PART_012}$animC5被爱包裹。{PART_013}我很庆幸有各位这样的同事和朋友。{PART_014}要是不承认自己也有些舍不得结束，就不够坦诚了。{PART_015}不过我想，大家都有同感。
407|||{SPEAKER_AKERS}{PART_000}啊……{PART_001}对。
408|||{SPEAKER_BAUTISTA}{PART_000}嗯哼。
409|||{SPEAKER_COLLINS}{PART_000}每个人总有一天都得说再见，{PART_001}$animC3对吧？{PART_002}我们唯一能做的，就是趁还在这里时，尽力与彼此建立联系，{PART_003}$animC1相伴同行。{PART_004}而且再见也不必是永别。
410|||{SPEAKER_AKERS}{PART_000}当然，{PART_001}凯莉。
411|||{SPEAKER_COLLINS}{PART_000}还有你，多普勒。
412|||{SPEAKER_DOPPLER}{PART_000}怎么了？
413|||{SPEAKER_COLLINS}{PART_000}如果你又碰上一颗百年难遇、还会发射无线电传输的陨石，{PART_001}$animC5希望你能记得叫上我，{PART_002}好吗？
414|||{SPEAKER_DOPPLER}{PART_000}哈哈哈，{PART_001}到时我第一个就给你们四个打电话！
415|||{SPEAKER_COLLINS}{PART_000}与你们共事是我的荣幸。
416|||{SPEAKER_AKERS}{PART_000}确实如此。
417|||{SPEAKER_BAUTISTA}{PART_000}嗯哼。
418|||{SPEAKER_DOPPLER}{PART_000}那么。{PART_001}……{PART_002}该说的都说了，{PART_003}我想这段录——
419|||一开始就是新定义
420|||{SPEAKER_AKERS}{PART_000}$animA1这么快就有新定义了，{PART_001}是吧？
421|||{SPEAKER_COLLINS}{PART_000}每周通常都是从定义传输开始的。{PART_001}$animC3往往也是在这时，我们决定最好结束本周的工作。
422|||{SPEAKER_AKERS}{PART_000}一个概念接着一个。{PART_001}$animA3不知道这次和上周的 {SIG_N227} 有什么关系。
423|||我不喜欢这个新词
424|||{SPEAKER_AKERS}{PART_000}我不喜欢这个新词。
425|||{SPEAKER_COLLINS}{PART_000}$animC5显然他们也有同感：{PART_001}{SIG_N213} {SIG_N087}。
426|||不同的度量
427|||{SPEAKER_AKERS}{PART_000}他们不是刚告诉我们 {SIG_N065} 了吗：{PART_001}6365 个 {SIG_N070}？
428|||{SPEAKER_BAUTISTA}{PART_000}嗯。{PART_001}不。{PART_002}$animB1那是 {SIG_N135} {SIG_N007}。{PART_003}这个是……{PART_004}$animB5嗯。{PART_005}另一种度量。
429|||{SPEAKER_AKERS}{PART_000}说得可真具体……
430|||用数学衡量情感
431|||{SPEAKER_AKERS}{PART_000}把情感拿来做数学比较，感觉真奇怪。
432|||{SPEAKER_COLLINS}{PART_000}$animC3谁知道他们脑中是否真是这种思维模型。{PART_001}$animC1也可能是受限于我们掌握的 {SIG_N043} 和 {SIG_N042}。{PART_002}$animC5过去这一年，我们四个对此可太有体会了。
433|||他们意见不合
434|||{SPEAKER_AKERS}{PART_000}你在琢磨什么呢，{PART_001}凯莉？
435|||{SPEAKER_COLLINS}{PART_000}我们能得出两个结论：{PART_001}第一，{PART_002}一个 {SIG_N128} 可以有多个 {SIG_N210}，{PART_003}具体取决于语境。
436|||{SPEAKER_AKERS}{PART_000}第二呢？
437|||{SPEAKER_COLLINS}{PART_000}{SIG_N136} 和 {SIG_N137} 意见不合。
438|||话匣子
439|||{SPEAKER_BAUTISTA}{PART_000}埃克斯。
440|||{SPEAKER_AKERS}{PART_000}哦，{PART_001}有什么能为你效劳的，{PART_002}巴蒂斯塔？{PART_003}等等。{PART_004}这是巴蒂斯塔。{PART_005}准没好事……
441|||{SPEAKER_BAUTISTA}{PART_000}你会和这些话匣子相处得很好。
442|||{SPEAKER_AKERS}{PART_000}喂！{PART_001}其实……{PART_002}这不算骂人。{PART_003}我知道你是想骂我，但我觉得这是好事！
443|||{SPEAKER_BAUTISTA}{PART_000}嗯。{PART_001}好。
444|||{SPEAKER_AKERS}{PART_000}好？？{PART_001}你打什么鬼主意……{PART_002}不，{PART_003}我不会上当。{PART_004}这次是我打败你了，{PART_005}巴蒂斯塔！
445|||{SPEAKER_BAUTISTA}{PART_000}嗯？
446|||{SPEAKER_AKERS}{PART_000}不不，{PART_001}别装得一脸无所谓！{PART_002}你说什么都伤不到我！
447|||{SPEAKER_COLLINS}{PART_000}买咖啡的跑腿。
448|||{SPEAKER_AKERS}{PART_000}什——{PART_001}连你也这样，{PART_002}凯莉……？{PART_003}:(
449|||外星信念中的人类
450|||{SPEAKER_AKERS}{PART_000}各位，{PART_001}$animA4你们有没有想过，自己对这些问题持什么立场？{PART_002}$animA3{SIG_N211}，{PART_003}{SIG_N212}？
451|||{SPEAKER_BAUTISTA}{PART_000}关于 {SIG_N231}，{PART_001}$animB4我认为不可能。
452|||{SPEAKER_COLLINS}{PART_000}$animA0也许不是不可能，{PART_001}$animC2但……{PART_002}确实，{PART_003}$animC5恐怕很难。
453|||{SPEAKER_AKERS}{PART_000}那么，{PART_001}$animA5另外两根支柱呢……？
454|||管中窥豹
455|||{SPEAKER_COLLINS}{PART_000}虽然这只是对他们道德观的惊鸿一瞥，{PART_001}却足以让我们了解他们会讨论什么话题。{PART_002}把 {SIG_N136} 和 {SIG_N137} 的 {SIG_N201}，{PART_003}仅仅归结为三个根本问题上的两类立场——{PART_004}这终究只能算管中窥豹。
456|||九代同堂
457|||{SPEAKER_AKERS}{PART_000}九代人同时生活。{PART_001}我以前还觉得，贝丝曾祖母能见到第四代后人就够厉害了。
458|||第一个道德难题
459|||{SPEAKER_COLLINS}{PART_000}我们的第一个道德难题：{PART_001}$animC5该不该建造 {SIG_N019} 1？
460|||{SPEAKER_BAUTISTA}{PART_000}该。{PART_001}$animB4{SIG_N207} {SIG_N154}，而且它不是 {SIG_N205} {SIG_N155}。
461|||{SPEAKER_AKERS}{PART_000}绝对不行！{PART_001}$animA1你完全没考虑 {SIG_N208} {SIG_N155} 和 {SIG_N209} {SIG_N155}！
462|||{SPEAKER_BAUTISTA}{PART_000}$animB5不是所有事都必须 {SIG_N208} 和 {SIG_N209}。
463|||{SPEAKER_AKERS}{PART_000}$animA4可它不该违背那些 {SIG_N204}！
464|||{SPEAKER_COLLINS}{PART_000}$animC0天哪……{PART_001}$animC3我们掌握的信息远远不够，根本无法做出合理判断。{PART_002}这完全取决于 {SIG_N019} 1 到底是什么；{PART_003}现在纯属假设。
465|||{SPEAKER_BAUTISTA}{PART_000}$animB3他喜欢和我唱反调。
466|||{SPEAKER_AKERS}{PART_000}$animA5绝对不是！{PART_001}是你根本不在乎 {SIG_N208} 和 {SIG_N209}！
467|||{SPEAKER_BAUTISTA}{PART_000}$animB1这不就是个例子。
468|||{SPEAKER_AKERS}{PART_000}$animA4这不公平！
469|||好久不见
470|||{SPEAKER_AKERS}{PART_000}{SIG_N197}，{PART_001}好久不见。
471|||{SPEAKER_COLLINS}{PART_000}这倒提醒我了，{PART_001}艾伦。
472|||{SPEAKER_AKERS}{PART_000}嗯？
473|||{SPEAKER_COLLINS}{PART_000}为什么是现在？
474|||数学与科学的作用
475|||{SPEAKER_COLLINS}{PART_000}一年前，{PART_001}数学与科学只是建立共同词汇的垫脚石。{PART_002}数学让我们理解了组合、{PART_003}变换，{PART_004}还建立了句法。{PART_005}接着，原子和宇宙学开启了通往通用 {SIG_N068} 的大门。{PART_006}随后又有化学、{PART_007}生物学{PART_008}和生态学的零碎知识打下基础，{PART_009}让我们得以讨论他们的文化。
476|||{SPEAKER_AKERS}{PART_000}到这里我都明白。
477|||{SPEAKER_COLLINS}{PART_000}为什么 {SIG_N197} 现在又出现了？{PART_001}它在发挥什么作用？
478|||真讽刺
479|||{SPEAKER_AKERS}{PART_000}仔细想想，{PART_001}定义之后，我们还是第一次讨论 {SIG_N197}。{PART_002}真讽刺，{PART_003}对吧？
480|||{SPEAKER_BAUTISTA}{PART_000}哪里讽刺了。
481|||{SPEAKER_AKERS}{PART_000}讨论 {SIG_N197} 是为了 {SIG_N195}，讨论 {SIG_N195} 又是为了 {SIG_N197}。
482|||这么晚才定义
483|||{SPEAKER_AKERS}{PART_000}这么晚才定义 {SIG_N240}，真奇怪。{PART_001}按{PLAYER_NAME}目前给的定义，{PART_002}还以为它会在第 250 段左右的传输里出现。
484|||{SPEAKER_COLLINS}{PART_000}我倒没想过。{PART_001}也许 {SIG_N240} 非常模糊，{PART_002}用当时掌握的工具很难定义。
485|||{SPEAKER_AKERS}{PART_000}用英语{PART_001}也一样很难定义。
486|||{SPEAKER_BAUTISTA}{PART_000}一点也不难。
487|||{SPEAKER_AKERS}{PART_000}我现在就能一口气说出自己认为最好的定义。{PART_001}可你能挑出定义里的任意一个词，问我“那又是什么？”{PART_002}于是我们就越挖越深，直到碰上……{PART_003}谁知道是什么。
488|||嗯……
489|||{SPEAKER_BAUTISTA}{PART_000}嗯……{PART_001}$animB5我的程序员直觉在报警了。
490|||另一个序列
491|||{SPEAKER_COLLINS}{PART_000}$animC1{SIG_N199} {SIG_N238} 1——{PART_001}看来又是某种序列。
492|||{SPEAKER_AKERS}{PART_000}不过这次可不是白送答案的序列。{PART_001}$animA5我们得自己争取，{PART_002}对吧。
493|||他们的下一片未知领域
494|||{SPEAKER_COLLINS}{PART_000}{SIG_N028}？{PART_001}它仍然是 {SIG_N238} {SIG_N029}。
495|||{SPEAKER_AKERS}{PART_000}所以那就是他们接下来要探索的未知领域。
496|||{SPEAKER_BAUTISTA}{PART_000}或者曾经是。
497|||{SPEAKER_COLLINS}{PART_000}曾经？
498|||{SPEAKER_BAUTISTA}{PART_000}这则讯息创作于 18 万年前。
499|||同样尚未探索
500|||{SPEAKER_COLLINS}{PART_000}既然 {SIG_N239} {SIG_N197} 仍未被探索，{PART_001}{SIG_N065} {SIG_N197} 也同样如此。
501|||{SPEAKER_BAUTISTA}{PART_000}那是 18 万年前。
502|||{SPEAKER_AKERS}{PART_000}那就希望他们还没做到吧。
503|||{SPEAKER_COLLINS}{PART_000}为什么？{PART_001}你还是不相信他们心怀善意？
504|||{SPEAKER_AKERS}{PART_000}不，{PART_001}我相信他们。{PART_002}但我更相信 {SIG_N065}。{PART_003}破坏因果律可能会招致灾难。
505|||{SPEAKER_COLLINS}{PART_000}也许你把思路限制得太死，{PART_001}太像一个生活在 1973 年的人类了。
506|||{SPEAKER_AKERS}{PART_000}很可能吧。{PART_001}但那会推翻我对宇宙的不少认知。
507|||那个词？
508|||{SPEAKER_COLLINS}{PART_000}{SIG_N242}？{PART_001}他们为什么要为这个定义一个词？
509|||他们所有人的……
510|||{SPEAKER_COLLINS}{PART_000}他们的 {SIG_N235}。
511|||{SPEAKER_AKERS}{PART_000}来自 {SIG_N045} {SIG_N199}。
512|||{SPEAKER_COLLINS}{PART_000}也就是说，它来自他们所有人。
513|||第 49 周结束（二）
514|||{SPEAKER_DOPPLER}{PART_000}翻译小组，{PART_001}$animD24这件事我考虑了很久。{PART_002}我有一个请求。{PART_003}必须坦白说，{PART_004}$animD04这是个自私的请求。{PART_005}最后几段传输到来时，{PART_006}$animD22我能和你们一起待在翻译室吗？{PART_007}我想亲临现场，{PART_008}感受一下那里的氛围，{PART_009}$animD20听听他们最后的话。{PART_010}$animD19和他们道别。{PART_011}$animD21和促成这一切的团队待在一起。{PART_012}$animD16我知道，所有辛苦的工作都是你们四个完成的，{PART_013}一周接着{PART_014}一周，{PART_015}从无比惊人的事物中解读出意义。{PART_016}$animD00那是最彻底的“外星”事物，{PART_017}无论从这个词的哪一层意义来说。{PART_018}所以，如果我的出现会打乱你们的工作流程，{PART_019}打乱你们四个过去一年里形成的体系，{PART_020}$animD07那我也乐意像其他所有人一样耐心等待。{PART_021}屏息关注你们的每一个决定，{PART_022}尽自己所能跟上每一段传输，{PART_023}尽可能理解这份来自深空的美丽礼物。{PART_024}$animD12我很乐意继续做好自己的本职工作。{PART_025}反过来，{PART_026}如果不会给各位添麻烦，{PART_027}$animD00我很想亲临现场。{PART_028}如果你们需要时间考虑，{PART_029}我们可以明早再谈。{PART_030}……
515|||{SPEAKER_COLLINS}{PART_000}多普勒博士。{PART_001}$animD21我想没必要再讨论了。{PART_002}我敢代表大家说：{PART_003}欢迎加入翻译小组！
516|||{SPEAKER_BAUTISTA}{PART_000}$animD20很高兴你能亲眼看到。
517|||{SPEAKER_AKERS}{PART_000}$animD19很高兴你能来，{PART_001}道格拉斯！
518|||{SPEAKER_DOPPLER}{PART_000}不行，{PART_001}这个绝对不行！{PART_002}叫“多普”没问题——{PART_003}$animD02而且我的朋友一般叫我“道格”。
519|||{SPEAKER_AKERS}{PART_000}那我很期待和你共事，{PART_001}道格！
520|||{SPEAKER_DOPPLER}{PART_000}$animD19我说的是朋友才叫我“道格”。{PART_001}$animD05你还是叫“多普”吧。
521|||{SPEAKER_AKERS}{PART_000}这又是为什么！{PART_001}没必要伤我的心吧！
522|||{SPEAKER_DOPPLER}{PART_000}确实没必要，{PART_001}$animD19但你这是自找的。
523|||{SPEAKER_BAUTISTA}{PART_000}你会很合群。
524|||{SPEAKER_AKERS}{PART_000}我不喜欢他融入团队的方式。{PART_001}居然就是欺负可怜的小艾伦。
525|||{SPEAKER_DOPPLER}{PART_000}艾伦。{PART_001}……
526|||{SPEAKER_AKERS}{PART_000}我知道，{PART_001}多普。{PART_002}不用担心我。
527|||{SPEAKER_DOPPLER}{PART_000}那就好。
528|||{SPEAKER_COLLINS}{PART_000}也差不多该结束了。
529|||{SPEAKER_DOPPLER}{PART_000}$animD21确实。{PART_001}$animD00你们四个，{PART_002}都好好睡一觉。{PART_003}$animD20给妻子打个电话，{PART_004}$animD21给丈夫打个电话，{PART_005}$animD19给父母打个电话，{PART_006}$animD22也给朋友们打个电话。{PART_007}告诉他们，再过不久你们就要回家了。{PART_008}祝各位{PART_009}$animD24今晚好梦。
530|||{SPEAKER_AKERS}{PART_000}各位晚安！
531|||{SPEAKER_BAUTISTA}{PART_000}晚安！
532|||{SPEAKER_COLLINS}{PART_000}愿你们做个最甜美的梦！
533|||以我们如今的了解
534|||{SPEAKER_AKERS}{PART_000}等一切尘埃落定，{PART_001}我以后该怎么办？
535|||{SPEAKER_COLLINS}{PART_000}还剩 12 段传输，{PART_001}然后就结束了。{PART_002}翻译工作完成了。
536|||{SPEAKER_DOPPLER}{PART_000}艾伦，{PART_001}凯莉，{PART_002}你们要重新站起来，{PART_003}掸掉身上的尘土，{PART_004}再找个需要你们帮助的新地方。
537|||{SPEAKER_AKERS}{PART_000}我知道，{PART_001}我知道。{PART_002}可知道了现在这一切以后呢？{PART_003}外星人真的存在。{PART_004}他们是真的。{PART_005}真的，{PART_006}真的存在。{PART_007}却又那么遥远……{PART_008}知道了这些，我该怎么若无其事地继续生活？{PART_009}知道那里存在着智慧生命……
538|||{SPEAKER_COLLINS}{PART_000}一个拥有 {SIG_N136} 和 {SIG_N137} 这般美好成员的物种。
539|||奇怪的体型
540|||{SPEAKER_AKERS}{PART_000}把他们的体型全都列出来一看，{PART_001}呼，{PART_002}这些家伙可真奇怪！
541|||{SPEAKER_COLLINS}{PART_000}艾伦！{PART_001}礼貌一点！
542|||{SPEAKER_AKERS}{PART_000}怎么了？{PART_001}他们就是很奇怪！
543|||{SPEAKER_COLLINS}{PART_000}他们与我们的确很不一样，{PART_001}尤其是有 2 个 {SIG_N131}。{PART_002}但体型存在差异并不是什么难以置信的事。
544|||{SPEAKER_AKERS}{PART_000}哦，是吗？
545|||{SPEAKER_COLLINS}{PART_000}他们往往会随着年龄增长变得更大，{PART_001}但个体差异也很大。{PART_002}而且说到 {SIG_N112} 和 {SIG_N113}，{PART_003}一定要记住：{PART_004}那是两个极端！{PART_005}就像拿最高的人类和最矮的人类相比！{PART_006}我们自己的跨度也会很大。
546|||{SPEAKER_COLLINS}{PART_000}我不会说自己懂那两种语言中的任何一种。{PART_001}语言学家的职责并不是尽可能多学几门语言。{PART_002}但了解不同的文字系统、{PART_003}句法结构、{PART_004}不同的词汇、{PART_005}不同的思维方式，很有价值。{PART_006}也许更重要的是，{PART_007}还要知道各种语言在哪些方面并无不同。
547|||{SPEAKER_AKERS}{PART_000}哦？{PART_001}比如呢？
548|||{SPEAKER_COLLINS}{PART_000}词频总会呈现相同的趋势，{PART_001}至少在语料库足够大时如此。{PART_002}如果分析一篇很长的英文文本，{PART_003}就会发现最常见的词：{PART_004}“the”“of”“to”，{PART_005}它们与低频词的出现次数会呈现可预测的比例。{PART_006}大多数人类语言都非常符合这个比例。{PART_007}至于陨石语言里的词，{PART_008}我们应该会经常看到 {SIG_N002} 和 {SIG_N030}，{PART_009}如今还有 {SIG_N086} 和 {SIG_N085}。{PART_010}不过，{PART_011}由于词语是分阶段引入的，{PART_012}而我们拥有的文本数据也相对较少，{PART_013}陨石语言与齐普夫定律的吻合程度可能没那么高。
549|||发生变化
550|||{SPEAKER_BAUTISTA}{PART_000}在 {SIG_N037} 之后，{PART_001}状态变了。
551|||{SPEAKER_AKERS}{PART_000}变了？
552|||{SPEAKER_BAUTISTA}{PART_000}不再是 {SIG_N236}，{PART_001}{SIG_N200} 0 现在是 {SIG_N236} {SIG_N029}。
553|||这有什么关系？
554|||{SPEAKER_DOPPLER}{PART_000}要是我漏掉了什么，请告诉我。{PART_001}不过，第一句话和这段传输的其余内容有什么关系？
555|||{SPEAKER_COLLINS}{PART_000}为什么这么问，{PART_001}多普勒？
556|||{SPEAKER_DOPPLER}{PART_000}我看不出 {SIG_N046} {SIG_N086} {SIG_N045} {SIG_N100} {SIG_N123}，{PART_001}和 {SIG_N133} {SIG_N145} {SIG_N085} 有什么关系。
557|||驾驶员最后的话
558|||{SPEAKER_PILOT}{PART_000}对接顺利完成。
559|||{SPEAKER_CO_PILOT}{PART_000}两艘飞船已经连接。
560|||{SPEAKER_CO_PILOT}{PART_000}那么，就这么定了。{PART_001}我们的旅程结束了。
561|||{SPEAKER_PILOT}{PART_000}不。{PART_001}我们的旅程才刚刚开始。{PART_002}指挥官。{PART_003}……{PART_004}我准备了几句话。{PART_005}会说得很简短。
562|||{SPEAKER_CO_PILOT}{PART_000}客人们还在等。
563|||{SPEAKER_PILOT}{PART_000}哦，我敢肯定，他们的驾驶员也想说几句我们不爱听的话。
564|||{SPEAKER_CO_PILOT}{PART_000}收到。
565|||{SPEAKER_PILOT}{PART_000}曾有 1700 亿人行走在地球上。{PART_001}也有数百亿个 {SIG_N129} 行走在 {SIG_N140} 上。{PART_002}我们是各自物种的使者，也是各自行星的使者。{PART_003}谨向多普勒博士、埃克斯博士、柯林斯博士、巴蒂斯塔博士、{PART_004}{PLAYER_NAME}和 {SIG_N136} 致敬。{PART_005}我知道你会热情地伸出手。{PART_006}你承载着我们这个物种感受过的全部爱意。{PART_007}承载着我们愿意奉献的全部爱意。{PART_008}请替我们传递温暖。{PART_009}翻译小组一定希望你这么做。{PART_010}{SIG_N136} 也一定如此期望。{PART_011}……
566|||{SPEAKER_CO_PILOT}{PART_000}准备好就出发吧，指挥官。''',
]


def main() -> None:
    source = json.loads(SOURCE.read_text(encoding="utf-8"))
    rows = []
    for chunk in CHUNKS:
        for line in chunk.splitlines():
            ordinal_text, translated = line.split("|||", 1)
            rows.append((int(ordinal_text), translated))
    assert len(rows) == len(source), (len(rows), len(source))
    assert [ordinal for ordinal, _ in rows] == list(range(len(source)))
    output = [
        {"text_index": item["text_index"], "translated_text": translated}
        for item, (_, translated) in zip(source, rows)
    ]
    OUTPUT.write_text(json.dumps(output, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    NEWTERMS.write_text("", encoding="utf-8")


if __name__ == "__main__":
    main()

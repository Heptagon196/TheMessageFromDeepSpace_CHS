import json
from pathlib import Path


BASE = Path(__file__).resolve().parent
SOURCE = BASE / "src_13_dialogue_chunks_1048_1136.json"
OUTPUT = BASE / "trans_13.json"
NEWTERMS = BASE / "newterms_13.txt"

TRANSLATIONS = r"""
半年
{SPEAKER_COLLINS}{PART_000}我们开始这项翻译工作，已经半年了。{PART_001}……{PART_002}$animC5原来我们已经一起度过了这么久。
{SPEAKER_BAUTISTA}{PART_000}嗯。
{SPEAKER_COLLINS}{PART_000}巴蒂斯塔，{PART_001}{PLAYER_NAME}，{PART_002}能有你们两个这样的挚友，我真的很高兴。
{SPEAKER_BAUTISTA}{PART_000}那埃克斯呢？
{SPEAKER_COLLINS}{PART_000}$animC4当然也有埃克斯。{PART_001}$animC5他现在虽然不在，{PART_002}但我很期待他回来。
{SPEAKER_BAUTISTA}{PART_000}嗯。{PART_001}……{PART_002}$animB4不过他不在，确实安静多了。
{SPEAKER_COLLINS}{PART_000}$animC3毕竟你和{PLAYER_NAME}都算不上活泼的同事。{PART_001}这话还是往好听了说。{PART_002}……
{SPEAKER_BAUTISTA}{PART_000}$animB3但我们很会开派对。
{SPEAKER_COLLINS}{PART_000}$animC2是吗？{PART_001}那你们都怎么玩？
{SPEAKER_BAUTISTA}{PART_000}$animB1撒彩纸。
{SPEAKER_COLLINS}{PART_000}啊，{PART_001}$animC5当然了。
{SPEAKER_BAUTISTA}{PART_000}$animB2还要有开心的词典。
{SPEAKER_COLLINS}{PART_000}$animC4我想，不管什么派对，都少不了一本开心的词典{PART_001}！
{SPEAKER_BAUTISTA}{PART_000}$animB4必不可少！
{SPEAKER_COLLINS}{PART_000}$animC3不开玩笑了，{PART_001}$animC5我刚才是认真的。{PART_002}这六个月，{PART_003}和你们三个在一起——{PART_004}$animC5我觉得这段时光非常……{PART_005}充实。
{SPEAKER_BAUTISTA}{PART_000}翻译一则来自数光年外的讯息。{PART_001}要说充实，{PART_002}确实。
{SPEAKER_COLLINS}{PART_000}不，{PART_001}我说的不是这个。{PART_002}$animC3刺激，{PART_003}令人振奋，{PART_004}当然了。{PART_005}$animC2但我真正想说的，{PART_006}是和你们三个相处。{PART_007}$animC4能和你们待在一起，{PART_008}你们都是那么善良、{PART_009}体贴、{PART_010}总会替别人着想。{PART_011}$animC3我这句话绝对是真心的——{PART_012}$animC5你们三个，是我这辈子交过最好的朋友。
真没找到
{SPEAKER_BAUTISTA}{PART_000}埃克斯。
{SPEAKER_AKERS}{PART_000}怎么？{PART_001}等等，{PART_002}$animA5为什么听你叫我的名字这么奇怪？
{SPEAKER_BAUTISTA}{PART_000}（耸肩）
{SPEAKER_AKERS}{PART_000}真的，{PART_001}太不习惯了……
{SPEAKER_BAUTISTA}{PART_000}$animB4你离开了两周。
{SPEAKER_COLLINS}{PART_000}不，{PART_001}不是，{PART_002}我懂艾伦的意思。
{SPEAKER_BAUTISTA}{PART_000}嗯？
{SPEAKER_COLLINS}{PART_000}$animC3你平时很少叫我们的名字！
{SPEAKER_AKERS}{PART_000}对啊！{PART_001}$animA4凯莉说得对！
{SPEAKER_COLLINS}{PART_000}你总是在接我们的话！
{SPEAKER_BAUTISTA}{PART_000}$animB3嗯。{PART_001}是。
{SPEAKER_AKERS}{PART_000}$animA1可你从来不会直接叫我们！
{SPEAKER_COLLINS}{PART_000}应该说很少叫！
{SPEAKER_BAUTISTA}{PART_000}$animB4好的，凯莉·柯林斯博士、艾伦·埃克斯博士。
{SPEAKER_AKERS}{PART_000}$animA3噫，好恶心！
{SPEAKER_COLLINS}{PART_000}$animC4听着太不对劲了！
{SPEAKER_AKERS}{PART_000}$animA4跟他说胡话似的！
{SPEAKER_COLLINS}{PART_000}你一叫我们的名字，听起来就像在逼自己，{PART_001}懂吗？{PART_002}$animC5不用勉强，{PART_003}巴蒂斯塔。{PART_004}没人逼你。
{SPEAKER_BAUTISTA}{PART_000}$animB5记下了。
{SPEAKER_AKERS}{PART_000}话说回来，{PART_001}$animA3你一开始想说什么？
{SPEAKER_BAUTISTA}{PART_000}你没找到他们的恒星。
{SPEAKER_AKERS}{PART_000}对啊？
{SPEAKER_BAUTISTA}{PART_000}$animB3真的？
{SPEAKER_AKERS}{PART_000}什么叫“真的”？？？{PART_001}$animA4对，{PART_002}我没找到！
{SPEAKER_BAUTISTA}{PART_000}$animB5哦。
{SPEAKER_AKERS}{PART_000}我把全世界的天文星表都查遍了！{PART_001}可他们的恒星，{PART_002}相对来说，{PART_003}又小又暗！{PART_004}$animA3所以地球上根本没人发现过！{PART_005}银河系里又不是每颗恒星都记在某本簿子上！
{SPEAKER_BAUTISTA}{PART_000}你真的全查过了？
{SPEAKER_AKERS}{PART_000}$animA4我是不是全查过了？？{PART_001}$animA2你问得好像我把钱包落在哪里了！{PART_002}哦，{PART_003}我再翻翻沙发缝，别是把他们的恒星漏在里面了！
{SPEAKER_BAUTISTA}{PART_000}那还真让人意外。
{SPEAKER_AKERS}{PART_000}对啊！{PART_001}当然意外！
{SPEAKER_BAUTISTA}{PART_000}但这种事发生在你身上，我也不会意外。
{SPEAKER_AKERS}{PART_000}喂！{PART_001}那里也{PART_002}没有！{PART_003}这一点我很确定！
{SPEAKER_BAUTISTA}{PART_000}我相信你。{PART_001}你是天文学家。
{SPEAKER_AKERS}{PART_000}$animA5老天……{PART_001}你这人可真是……{PART_002}你自己知道吗？{PART_003}“每个地方都查过了吗”，老天，你真是……
{SPEAKER_COLLINS}{PART_000}$animC3这下真戳到痛处了，{PART_001}巴蒂斯塔。
{SPEAKER_BAUTISTA}{PART_000}对。{PART_001}记住了。
什么形状？
{SPEAKER_COLLINS}{PART_000}$animC1{SIG_N019} 0 的内容是 4 和 3。{PART_001}所以它可能是一个{SIG_N016}。
{SPEAKER_AKERS}{PART_000}到这里我还听得懂。
{SPEAKER_COLLINS}{PART_000}$animC3而{SIG_N019} 1 的内容完全不同。{PART_001}里面有一个{SIG_N016}、一个{SIG_N017}、两个数字、一个{SIG_N011}和两个{SIG_N018}。
{SPEAKER_AKERS}{PART_000}$animA3所以他们到底在问什么？
{SPEAKER_COLLINS}{PART_000}我猜他们是在问{SIG_N019} 1 的形态。{PART_001}它是{SIG_N016}、{SIG_N017}、{SIG_N018}、{SIG_N011}，还是{SIG_N019}？{PART_002}{SIG_N019} 1 到底是什么？
一样大
{SPEAKER_AKERS}{PART_000}这三个{SIG_N026}怎么可能都成立？
{SPEAKER_BAUTISTA}{PART_000}嗯。
{SPEAKER_AKERS}{PART_000}“嗯”什么？
{SPEAKER_BAUTISTA}{PART_000}我也在想这件事。
{SPEAKER_AKERS}{PART_000}哦，{PART_001}是吗？{PART_002}看来英雄所见略同！{PART_003}或者至少能一起犯难。
{SPEAKER_COLLINS}{PART_000}你们怎么理解这些{SIG_N026}？
{SPEAKER_BAUTISTA}{PART_000}0 不等于 1。
{SPEAKER_BAUTISTA}{PART_000}0 的面积不大于 1。
{SPEAKER_BAUTISTA}{PART_000}0 的面积不小于 1。
{SPEAKER_COLLINS}{PART_000}如果我的推断没走偏，{PART_001}那{SIG_N018} 0 和{SIG_N018} 1 的大小一定相同。
大得离谱
{SPEAKER_AKERS}{PART_000}是我看错了吗？
{SPEAKER_COLLINS}{PART_000}哪里有问题？
{SPEAKER_AKERS}{PART_000}3584 个{SIG_N076}？{PART_001}这也大得太离谱了！
{SPEAKER_BAUTISTA}{PART_000}要是这就把你吓到了，{PART_001}最好别看这段传输的结尾。
{SPEAKER_AKERS}{PART_000}呃，{PART_001}我看到{SIG_N019} 0 = {SIG_N029} {SIG_N090}，{PART_002}呃，{PART_003}{SIG_N002} {SIG_N002}，后面一大串——{PART_004}我的老天！！
{SPEAKER_BAUTISTA}{PART_000}呵呵。
{SPEAKER_AKERS}{PART_000}32768 个{SIG_N076}！？{PART_001}这也太大了吧！
一个信号？
{SPEAKER_AKERS}{PART_000}喂，呃，{PART_001}巴蒂斯塔。
{SPEAKER_BAUTISTA}{PART_000}没坏。
{SPEAKER_AKERS}{PART_000}什么都没坏？{PART_001}真的？
{SPEAKER_BAUTISTA}{PART_000}对。{PART_001}这段传输只有一个信号。
{SPEAKER_AKERS}{PART_000}就一个{SIG_N012}，没别的了？？
{SPEAKER_BAUTISTA}{PART_000}嗯。
{SPEAKER_AKERS}{PART_000}那好吧……{PART_001}你确定一切都正常就行……
与什么有关？
{SPEAKER_AKERS}{PART_000}$animA1我在查之前的传输。{PART_001}上面写着{SIG_N129} {SIG_N099} {SIG_N079}，还有{SIG_N129} {SIG_N100} {SIG_N045} {SIG_N128}。{PART_002}$animA3所以陨石先生只是想要{SIG_N079}吗？
{SPEAKER_COLLINS}{PART_000}$animC4不，{PART_001}陨石小姐用{SIG_N099} {SIG_N079}描述过很多词。{PART_002}这里要的是更具体的东西，{PART_003}和{SIG_N045}有关。
因数
{SPEAKER_AKERS}{PART_000}$animA1这段传输感觉像是没说完，{PART_001}我不喜欢。
{SPEAKER_COLLINS}{PART_000}$animC5我也很难从中看出任何含义。
{SPEAKER_BAUTISTA}{PART_000}$animA0嗯。{PART_001}$animB1因数。
{SPEAKER_AKERS}{PART_000}因数？
{SPEAKER_BAUTISTA}{PART_000}把表达式补完。{PART_001}{SIG_N006}前面填哪两个因数，结果等于 13。
{SPEAKER_AKERS}{PART_000}$animA3那什么都可以啊。{PART_001}比如 2 {SIG_N002} 6.5，{PART_002}或者 4 {SIG_N002}……
{SPEAKER_AKERS}{PART_000}$animA2对啊，没错！
{SPEAKER_BAUTISTA}{PART_000}13 是质数。
{SPEAKER_COLLINS}{PART_000}所以呢？
{SPEAKER_BAUTISTA}{PART_000}$animB4能补完这个表达式的整数因数只有两个。
{SPEAKER_AKERS}{PART_000}那就看看{PLAYER_NAME}会填什么吧！
想复杂了
{SPEAKER_COLLINS}{PART_000}1 {SIG_N129} {SIG_N086} {SIG_N110} {SIG_N044} {SIG_N043} {SIG_N038} {SIG_N085}——{PART_001}$animC3这能让我们推断出什么？{PART_002}{SIG_N110} {SIG_N043}，{PART_003}这似乎是关键。{PART_004}$animC4也许他们是要我们用一个{SIG_N130}照着他们的行动来做，{PART_005}又或者我们必须意识到——
{SPEAKER_AKERS}{PART_000}凯莉，{PART_001}$animA2你是不是有点想太远了{PART_002}？
{SPEAKER_COLLINS}{PART_000}$animC5啊？
{SPEAKER_AKERS}{PART_000}如果一个{SIG_N129}要为{SIG_N110} {SIG_N043}负责，{PART_001}$animA4那{SIG_N119} {SIG_N043}，{PART_002}也该算在他们头上。
{SPEAKER_COLLINS}{PART_000}这……{PART_001}有道理。{PART_002}也许是我想复杂了。
“现在”怎么说
{SPEAKER_AKERS}{PART_000}凯莉，{PART_001}“现在”该怎么表达？{PART_002}因为陨石想要的就这个，{PART_003}对吧？
{SPEAKER_COLLINS}{PART_000}用他们的词来说，{PART_001}我会写：{PART_002}{SIG_N119} {SIG_N065}。
{SPEAKER_AKERS}{PART_000}哦，我懂了！{PART_001}好思路！
缺少单位
{SPEAKER_AKERS}{PART_000}$animA1我算出来的也一样。
{SPEAKER_COLLINS}{PART_000}$animC1那也许是少了一个{SIG_N068}。
{SPEAKER_AKERS}{PART_000}$animA3啊，{PART_001}有可能，{PART_002}真有可能。
用他们起的名字
{SPEAKER_AKERS}{PART_000}$animA3怎么回事！
{SPEAKER_COLLINS}{PART_000}$animC5嗯？
{SPEAKER_AKERS}{PART_000}$anim1他们刚说自己的{SIG_N172}是 2 {SIG_N056} 7 的一半，{PART_001}可它又不是 2 原子 7？！
{SPEAKER_BAUTISTA}{PART_000}当然。
{SPEAKER_AKERS}{PART_000}$animA5那就是你的电脑出故障了。{PART_001}{PLAYER_NAME}，{PART_002}再发一次！
{SPEAKER_BAUTISTA}{PART_000}不。{PART_001}$animB2没有错误。{PART_002}这段传输前面已经定义了 2 {SIG_N056} 7$animB1。
{SPEAKER_COLLINS}{PART_000}你是说，我们得用他们给它起的名字？
{SPEAKER_BAUTISTA}{PART_000}$animB5嗯。
面
{SPEAKER_COLLINS}{PART_000}这个问题太开放了。{PART_001}$animC5我不知道该从哪里入手。
{SPEAKER_AKERS}{PART_000}这里有一个{SIG_N048}，{PART_001}也就是一种特定的{SIG_N018}——
{SPEAKER_BAUTISTA}{PART_000}球体。
{SPEAKER_AKERS}{PART_000}$animA3我正要说呢！{PART_001}$animA2总之，{PART_002}我们要描述的是{SIG_N171} {SIG_N023}。
{SPEAKER_COLLINS}{PART_000}关键在{SIG_N171}，{PART_001}而这个词恰恰是我们最没把握的。
{SPEAKER_AKERS}{PART_000}$animA5可惜啊。
{SPEAKER_COLLINS}{PART_000}从最近的传输来看，{PART_001}我知道它描述了某种几何特征。
{SPEAKER_BAUTISTA}{PART_000}三维几何$animB1。
{SPEAKER_AKERS}{PART_000}哦，对，{PART_001}你说得没错！{PART_002}$animA4他们画了个立方体，说它有 8 个{SIG_N016}、12 个{SIG_N017}和 6 个{SIG_N171}！
{SPEAKER_BAUTISTA}{PART_000}$animB4嗯！
{SPEAKER_COLLINS}{PART_000}想到什么了？
{SPEAKER_BAUTISTA}{PART_000}$animB3面。{PART_001}$animB5立方体有 6 个面。
{SPEAKER_COLLINS}{PART_000}$animC4妙啊！
{SPEAKER_AKERS}{PART_000}$animA3不过，{PART_001}最后这部分还得解决：{PART_002}$animA1多少个{SIG_N171}才构成一个{SIG_N048}？
无限
{SPEAKER_BAUTISTA}{PART_000}要描述一个{SIG_N048}，说它有 0 个{SIG_N171}，{PART_001}或者有无限多个，才说得通。{PART_002}如果我们有表示无限的词，{PART_003}那应该就是他们在等的传输。
映照上一段
{SPEAKER_COLLINS}{PART_000}我从中唯一能看出的，{PART_001}就是它在映照上一段传输。
{SPEAKER_BAUTISTA}{PART_000}嗯？
{SPEAKER_COLLINS}{PART_000}$animC1上一段传输说{SIG_N129} {SIG_N193} {SIG_N100} {SIG_N108} {SIG_N036}，{PART_001}也就是说，他们拥有某种能力。
{SPEAKER_BAUTISTA}{PART_000}$animB3{SIG_N044} {SIG_N038} {SIG_N085}的能力。
{SPEAKER_COLLINS}{PART_000}对，{PART_001}$animC2正是。{PART_002}$animB5但现在问题转向了我们，{PART_003}转向{SIG_N130}。{PART_004}$animC3我们拥有{SIG_N044} {SIG_N163} {SIG_N085} {SIG_N036}的能力……{PART_005}$animC5这部分还没解开。
另一个有名字的外星人？
{SPEAKER_COLLINS}{PART_000}$animC3上一段传输说的是某个特定的{SIG_N130}——
{SPEAKER_COLLINS}{PART_000}而这段，{PART_001}$animC1似乎说的是某个特定的{SIG_N129}。
{SPEAKER_AKERS}{PART_000}$animA3我们有按名字定义过什么{SIG_N129}吗？
{SPEAKER_COLLINS}{PART_000}$animA3除了{SIG_N136}？{PART_001}$animC5我来翻翻{PLAYER_NAME}的词典。
太沉重了
{SPEAKER_COLLINS}{PART_000}我也想过{SIG_N154}，{PART_001}没错。{PART_002}$animC2但这个词分量很重，{PART_003}放在这里太重了。
{SPEAKER_AKERS}{PART_000}沉重？
{SPEAKER_COLLINS}{PART_000}$animC5{SIG_N154}更适合用来作道德判断。{PART_001}$animC4这里需要一个能表达个人、{PART_002}主观愉悦程度的词。
不是他们的
{SPEAKER_BAUTISTA}{PART_000}$animB1{SIG_N044} 0 是他们的。
温暖的感觉
{SPEAKER_COLLINS}{PART_000}{SIG_N087}作用于{SIG_N230}时，应该会带来一种温暖、{PART_001}让人心里发软的感觉。
{SPEAKER_AKERS}{PART_000}那要是有表示这种感觉的词，{PART_001}就，{PART_002}呃，{PART_003}$animA1发过去吧，{PLAYER_NAME}！
{SPEAKER_COLLINS}{PART_000}$animC5对。{PART_001}$animA5问题是，我们不知道哪个词该填在这里。{PART_002}$animC3不过我怀疑，这个词应该在过去 100 段传输里出现过。
{SPEAKER_AKERS}{PART_000}$animA3还能再缩小点范围吗？{PART_001}$animA5这 100 段传输里，陨石可没少定义新词……
{SPEAKER_COLLINS}{PART_000}它一定是某个被定义为{SIG_N152}的词。
不谋而合
{SPEAKER_COLLINS}{PART_000}我也是这么想的，{PART_001}{PLAYER_NAME}！{PART_002}$animC5不管他们在等什么，{PART_003}我们一定越来越接近了。
不是热？
{SPEAKER_AKERS}{PART_000}{SIG_N164}，{PART_001}{SIG_N090}$animA3确实会产生很多这种东西。
{SPEAKER_COLLINS}{PART_000}$animC2知道陨石为什么不接受吗？
{SPEAKER_AKERS}{PART_000}$animA4我猜重点在{SIG_N089}。{PART_001}$animA5{SIG_N089}真正{SIG_N038}的到底是什么？
有点眉目了
{SPEAKER_AKERS}{PART_000}$animA1你这回想到点子上了，{PART_001}{PLAYER_NAME}。{PART_002}$animA3一个{SIG_N090}会把轻的{SIG_N056}合成更重的{SIG_N056}。
{SPEAKER_COLLINS}{PART_000}$animC2{SIG_N089}，{PART_001}对吧？
{SPEAKER_AKERS}{PART_000}$animA4正中靶心，吸墨纸！
{SPEAKER_COLLINS}{PART_000}$animC5所以，{SIG_N090}在{SIG_N089} {SIG_N085}时真正产生的是什么？
{SPEAKER_AKERS}{PART_000}不清楚。{PART_001}$animA5可能有什么细微之处被我漏掉了！{PART_002}这方面你和{PLAYER_NAME}一直都很拿手！
电
{SPEAKER_AKERS}{PART_000}{SIG_N062} {SIG_N039} {SIG_N240}……{PART_001}$animA1听起来有点像在描述电！
计算机
{SPEAKER_BAUTISTA}{PART_000}$animB5嗯。
{SPEAKER_AKERS}{PART_000}又有更新了？
{SPEAKER_BAUTISTA}{PART_000}不。{PART_001}$animB1是这段传输。
{SPEAKER_AKERS}{PART_000}嘿，凯莉！{PART_001}$animA1你看，{PART_002}他居然真的感兴——
{SPEAKER_COLLINS}{PART_000}$animA0说吧，{PART_001}巴蒂斯塔。
{SPEAKER_BAUTISTA}{PART_000}{SIG_N237} {SIG_N197} = 计算机。
语言
{SPEAKER_AKERS}{PART_000}凯莉，{PART_001}你好像一直在琢磨什么。{PART_002}$animA3愿意说说吗？
{SPEAKER_COLLINS}{PART_000}愿意，{PART_001}我正想说。
{SPEAKER_AKERS}{PART_000}$animA1哦，{PART_001}她看起来很认真。{PART_002}$animA5肯定有大发现。
{SPEAKER_COLLINS}{PART_000}是{SIG_N196}——{PART_001}$animC1这个词让我很在意。{PART_002}如果{SIG_N129} 0 {SIG_N086} {SIG_N129} 1 {SIG_N196} {SIG_N085}……{PART_003}$animC3那么——{PART_004}$animC4哦！
{SPEAKER_AKERS}{PART_000}$animA0要来了。
{SPEAKER_COLLINS}{PART_000}$animC1语言。{PART_001}$animC5他们在描述语言！
讯息译完了
{SPEAKER_AKERS}{PART_000}那就全译完了。
{SPEAKER_COLLINS}{PART_000}讯息已经译完了。
接近
{SPEAKER_PILOT}{PART_000}已目视确认他们的飞船。{PART_001}副驾驶员，对方速度如何？
{SPEAKER_CO_PILOT}{PART_000}速度已经与对接要求一致；{PART_001}误差小于每秒 0.001 米。
{SPEAKER_PILOT}{PART_000}这正是我们想听的。{PART_001}继续惯性飞行。
{SPEAKER_CO_PILOT}{PART_000}收到。
接近（二）
{SPEAKER_PILOT}{PART_000}幸好 99% 的路程都是他们飞过来的。
{SPEAKER_CO_PILOT}{PART_000}他们等了不到 1 个{SIG_N070}，收到{SIG_N044} 1 后便出发了。{PART_001}而且航速接近光速。
{SPEAKER_PILOT}{PART_000}他们的减速能力同样惊人。{PART_001}看，他们正在把速度调整到与我们一致。
{SPEAKER_CO_PILOT}{PART_000}尽管我们优化了{SIG_N242}，{PART_001}我还是认为，过去这 18 万年里，他们的技术一定又进步了。
{SPEAKER_PILOT}{PART_000}毫无疑问。
收到信号
{SPEAKER_PILOT}{PART_000}收到一个信号。{PART_001}只有一个频率。
{SPEAKER_PILOT}{PART_000}氢谱线。
{SPEAKER_CO_PILOT}{PART_000}猜猜会持续多久，指挥官？{PART_001}……{PART_002}驾驶员？
{SPEAKER_PILOT}{PART_000}收到。{PART_001}指挥官，发射器已按您的要求启动，随时可以发射。{PART_002}对接前只有一次传输机会——准备好就发吧。
接近（三）
{SPEAKER_PILOT}{PART_000}指挥官，想好要说什么了吗？{PART_001}……
编译失败
{SPEAKER_PILOT}{PART_000}编译失败。{PART_001}再试一次，指挥官。{PART_002}记得参考{PLAYER_NAME}的词典。
驾驶员最后的话
{SPEAKER_PILOT}{PART_000}正在对接。{PART_001}……{PART_002}让我们为您骄傲，指挥官。
收到传输
{SPEAKER_CO_PILOT}{PART_000}{SIG_N045}的回应已接入。{PART_001}指挥官，{PART_002}通讯一关闭，{PART_003}我们就会关闭发射器并对接。{PART_004}等您下令。{PART_005}……
传输已发送
{SPEAKER_PILOT}{PART_000}传输已发送。
{SPEAKER_COLLINS}{PART_000}其中 93.75% 是{SIG_N143}$animC1？{PART_001}不管{PLAYER_NAME}最后怎么理解，{PART_002}这个比例都很高。
电脑功德
{SPEAKER_BAUTISTA}{PART_000}更优化的{SIG_N053}。{PART_001}能减少{SIG_N052}的数量。{PART_002}$animB3好耶。
{SPEAKER_COLLINS}{PART_000}$animC3“好耶”？{PART_001}我还以为{SIG_N052}越多，你越兴奋。{PART_002}可那样一来，{SIG_N053}就显得没那么厉害，{PART_003}不是吗？
{SPEAKER_BAUTISTA}{PART_000}$animB5没错。{PART_001}$animB2但数据更少，{PART_002}传输更快，{PART_003}处理量更低。{PART_004}$animB1电脑会很感激。
{SPEAKER_COLLINS}{PART_000}$animC2你还担心电脑过得好不好？
{SPEAKER_BAUTISTA}{PART_000}当然。{PART_001}$animB4这样才能积点电脑功德。
{SPEAKER_COLLINS}{PART_000}$animC5“电脑功德”？
{SPEAKER_BAUTISTA}{PART_000}$animB5尊重电脑；{PART_001}电脑也尊重你。
{SPEAKER_COLLINS}{PART_000}没想到你还这么迷信，{PART_001}巴蒂斯塔博士……
{SPEAKER_BAUTISTA}{PART_000}让计算机系统配合工作，对履行我的职责很重要。
刚才那是怎么回事？
{SPEAKER_COLLINS}{PART_000}{SIG_N140} {SIG_N131} {SIG_N143} {SIG_N023}——{PART_001}$animC4要么是 1，要么是 2。
{SPEAKER_AKERS}{PART_000}$animA3可这是什么意思？
{SPEAKER_COLLINS}{PART_000}我也不确定，{PART_001}$animC3但{SIG_N116}的{SIG_N131}位于{SIG_N140}上，它们的{SIG_N143} {SIG_N023}为 2。
{SPEAKER_AKERS}{PART_000}$animA1而{SIG_N129}就是其中之一。
数量真多！
{SPEAKER_AKERS}{PART_000}$animA1他们数量可真多！{PART_001}看来挺忙啊！
{SPEAKER_COLLINS}{PART_000}$animC4艾伦……{PART_001}$animC5算了。
{SPEAKER_BAUTISTA}{PART_000}$animA0幸好他们用了优化后的{SIG_N129} {SIG_N053}。{PART_001}$animB1电脑也很高兴。{PART_002}绘制{SIG_N052}时没有内存溢出，{PART_003}$animB5我们的堆空间安全了。
{SPEAKER_COLLINS}{PART_000}你的电脑功德一定积得很高，{PART_001}巴蒂斯塔。
{SPEAKER_BAUTISTA}{PART_000}$animB4电脑开心，{PART_001}生活舒心。
不是 8？？
{SPEAKER_COLLINS}{PART_000}9？{PART_001}{SIG_N142} {SIG_N129}的数量好像很多。
{SPEAKER_AKERS}{PART_000}为什么不是 8？{PART_001}$animA4他们那么喜欢 8！
{SPEAKER_BAUTISTA}{PART_000}不是所有东西都非得是 8。
{SPEAKER_AKERS}{PART_000}$animA3可他们喜欢 8 啊！
{SPEAKER_BAUTISTA}{PART_000}我们喜欢 10。{PART_001}$animB1难道{SIG_N046} {SIG_N114} {SIG_N142} {SIG_N023}就是 10？
{SPEAKER_AKERS}{PART_000}好吧，{PART_001}$animA5好吧，{PART_002}你说得有道理。{PART_003}但就差 1 个。{PART_004}$animA2本来可以很完美的，{PART_005}懂吧？
{SPEAKER_BAUTISTA}{PART_000}$animB3那就叫他们整个物种一起努努力。{PART_001}想办法变成那样。
{SPEAKER_AKERS}{PART_000}$animA4说不定我真会！！
1 是 2？
{SPEAKER_COLLINS}{PART_000}等等，{PART_001}$animC5什么？
{SPEAKER_AKERS}{PART_000}连你也看糊涂了，{PART_001}是吧，{PART_002}凯莉？
{SPEAKER_COLLINS}{PART_000}1 个{SIG_N131}是 2？{PART_001}这是什么意思？
{SPEAKER_AKERS}{PART_000}完全不知道。
{SPEAKER_COLLINS}{PART_000}不管怎么说，{PART_001}{PLAYER_NAME}正在把这些理清楚。{PART_002}我又得靠你了。
1 是 9？
{SPEAKER_AKERS}{PART_000}现在 1 个{SIG_N131}又是 9——{PART_001}8 加 1？{PART_002}到底怎么回事？{PART_003}{PLAYER_NAME}？{PART_004}凯莉？
{SPEAKER_COLLINS}{PART_000}我……{PART_001}不，{PART_002}我现在也理解不了。
虽不同，却共生
{SPEAKER_AKERS}{PART_000}不妙，{PART_001}她又在那边琢磨起来了。
{SPEAKER_COLLINS}{PART_000}我好像开始看清全貌了，{PART_001}希望如此。{PART_002}{SIG_N147}和{SIG_N146}——{PART_003}尽管彼此不同，{PART_004}{SIG_N131}不同，{PART_005}{SIG_N023}不同，{PART_006}{SIG_N135}也不同。{PART_007}可他们，{PART_008}生活在一起。{PART_009}他们是{SIG_N129}。
共享生命周期
{SPEAKER_COLLINS}{PART_000}{SIG_N144}同步。
{SPEAKER_AKERS}{PART_000}怎么了，{PART_001}凯莉？
{SPEAKER_COLLINS}{PART_000}他们一起{SIG_N132}、{SIG_N134}、{SIG_N133}。{PART_001}他们共同经历一个完整的生命周期。
我们的银河系
{SPEAKER_COLLINS}{PART_000}埃克斯，{PART_001}{SIG_N090}在这个{SIG_N053}中的位置和尺度，{PART_002}你能从这些数据里看出什么吗？{PART_003}它指的是某个已知的{SIG_N098}吗？
{SPEAKER_AKERS}{PART_000}单看比例，{PART_001}我一眼就能确定，{PART_002}这绝不是对真实{SIG_N098}的准确描绘，{PART_003}至少不是按比例画的。{PART_004}但我确实看到了旋臂，{PART_005}就像我们的银河系！
数量最多
{SPEAKER_AKERS}{PART_000}我一直被{SIG_N106} {SIG_N032} {SIG_N023}绊得晕头转向。{PART_001}$animA1只要能弄懂这个，{PART_002}我就能跟上了。
{SPEAKER_COLLINS}{PART_000}$animA5我认为{SIG_N106}表示最极端的情况，{PART_001}比如最大的数，或最小的数。{PART_002}$animC3而{SIG_N032}这个词，我们已经很有把握了。
{SPEAKER_AKERS}{PART_000}对，{PART_001}{SIG_N032}就是“A 大于 B”。
{SPEAKER_COLLINS}{PART_000}$animC2{SIG_N023}还是与数量有关，{PART_001}数额、{PART_002}个数、{PART_003}总数。
{SPEAKER_AKERS}{PART_000}$animA3这部分我也完全同意。{PART_001}那合在一起，{SIG_N106} {SIG_N032} {SIG_N023}究竟是……$animA2什么？
{SPEAKER_COLLINS}{PART_000}$animC4我认为，它指的是出现次数最多的东西。
分成两部分的图像
{SPEAKER_AKERS}{PART_000}$animA3又是图像传输，{PART_001}是吧。{PART_002}而且分成两部分。
{SPEAKER_COLLINS}{PART_000}$animA0我猜第一个{SIG_N011}是在问{SIG_N053}里画的是哪一类，{PART_001}$animC2是{SIG_N016}、{SIG_N017}，还是{SIG_N018}。
{SPEAKER_AKERS}{PART_000}而第二个{SIG_N011}是在问，{PART_001}$animA4{PLAYER_NAME}认为图上东西的{SIG_N023}是什么。
{SPEAKER_COLLINS}{PART_000}$animC5和我的理解一致。
{SPEAKER_AKERS}{PART_000}$animA3比如他们画了 3 个{SIG_N018}，上面都是那些小小的{SIG_N052}，{PART_001}$animA2我们只要填{SIG_N014} {SIG_N018} {SIG_N003} 3 {SIG_N015}？
{SPEAKER_COLLINS}{PART_000}按我的理解，{PART_001}没错。
电脑崩溃 =[
{SPEAKER_BAUTISTA}{PART_000}你为什么要这么做？=[
就填 0？？
{SPEAKER_BAUTISTA}{PART_000}$animB1为什么还没到下一段。
{SPEAKER_AKERS}{PART_000}因为我们还没想明白，{PART_001}巴蒂斯塔。{PART_002}$animA3他们这段传输说不通。
{SPEAKER_COLLINS}{PART_000}$animC2确实像是无解。{PART_001}$animC5我完全不知道陨石在等什么。
{SPEAKER_AKERS}{PART_000}$animA0什么？{PART_001}为什么？
{SPEAKER_BAUTISTA}{PART_000}{SIG_N012} {SIG_N004} 0。{PART_001}$animB5就是 0。
恭喜
{SPEAKER_DOPPLER}{PART_000}你成功了。{PART_001}你完成了任务，也译出了这则讯息。{PART_002}这是我们第一次接触地外智慧生命，{PART_003}而{PLAYER_NAME}，{PART_004}是你带领团队走过了这一切。{PART_005}977 段传输——{PART_006}讲述了 18 万年前一个族群的故事，{PART_007}他们和你一样，也在好奇自己身处宇宙何方。{PART_008}你的成就堪称不可思议，{PART_009}值得为自己骄傲。
{SPEAKER_AUTO_LOG}{PART_000}{PLAYER_NAME}，{PART_001}你已经抵达游戏的终点。{PART_002}不过我们还计划了最后一项内容。{PART_003}最终结局片段仍在开发中，{PART_004}等它完成后，{PART_005}请回来看看。{PART_006}我想你一定会喜欢。{PART_007}你现在会看到这段对白，{PART_008}是因为你完成翻译工作的速度实在太快了。{PART_009}感谢游玩 <3{PART_010}……
没有运算符
{SPEAKER_AKERS}{PART_000}我看不懂第一行。{PART_001}$animA1这里根本没有数学运算符！
{SPEAKER_BAUTISTA}{PART_000}没有错误。{PART_001}不要冤枉电脑。
{SPEAKER_AKERS}{PART_000}$animA3你确定不是电脑——
{SPEAKER_BAUTISTA}{PART_000}$animB4没有错误。
{SPEAKER_AKERS}{PART_000}$animA5好吧，{PART_001}好吧。
不是数字
{SPEAKER_COLLINS}{PART_000}我得承认，{PART_001}这段传输……{PART_002}{SIG_N011} 0 是做什么的？{PART_003}它会是什么数字？
{SPEAKER_AKERS}{PART_000}我一直把它改写成 1 {SIG_N002} 1 空白 {SIG_N004} 2，{PART_001}想看看能不能找到头绪。
{SPEAKER_COLLINS}{PART_000}天啊。
{SPEAKER_AKERS}{PART_000}怎么？
{SPEAKER_COLLINS}{PART_000}埃克斯，{PART_001}你可能想到点子上了。{PART_002}要是{SIG_N011} 0 不是数字呢？{PART_003}在那个空白处填什么，才能让方程成立？
不留空格
{SPEAKER_COLLINS}{PART_000}我明白你的思路了，{PART_001}{PLAYER_NAME}。{PART_002}也许不需要{SIG_N002}？
同一种
{SPEAKER_AKERS}{PART_000}2 {SIG_N143} {SIG_N129} {SIG_N086} {SIG_N142} {SIG_N144} {SIG_N085}。{PART_001}{SIG_N142}我觉得说得通。{PART_002}是不是我漏了什么？
{SPEAKER_COLLINS}{PART_000}这样可以。{PART_001}也许他们要的是更具体的答案？
{SPEAKER_AKERS}{PART_000}比如？
{SPEAKER_COLLINS}{PART_000}我们知道{SIG_N131}在{SIG_N143} {SIG_N128}中是什么，{PART_001}所以我推断，{SIG_N142} {SIG_N128}一定也要一致。
人类百分比
{SPEAKER_AKERS}{PART_000}$animA2对了，{PART_001}现在我们对{SIG_N143}和{SIG_N142}了解得更多了……{PART_002}{SIG_N046}里有百分之多少是{SIG_N143}？
{SPEAKER_COLLINS}{PART_000}$animC3问得好。{PART_001}$animC2我估计{SIG_N046}里大约有 80% 是{SIG_N143}。
{SPEAKER_AKERS}{PART_000}$animA1他们有 93%！！{PART_001}看来他们胜过我们了！
{SPEAKER_COLLINS}{PART_000}$animC5这应该不是比赛吧，{PART_001}埃克斯。
{SPEAKER_AKERS}{PART_000}$animA3你知道我就是这样。
我们是某种东西
{SPEAKER_AKERS}{PART_000}{SIG_N046} {SIG_N100} {SIG_N154}？{PART_001}嗯。
二选一
{SPEAKER_BAUTISTA}{PART_000}二选一。{PART_001}{SIG_N154}或{SIG_N155}。{PART_002}$animB1又可以穷举一次了。
{SPEAKER_COLLINS}{PART_000}应该是可以，{PART_001}$animC4可为什么要这么做？
{SPEAKER_BAUTISTA}{PART_000}$animB0为了继续往下走。
{SPEAKER_COLLINS}{PART_000}$animC5可我们追求的是理解。{PART_001}就算靠穷举解开了，{PART_002}也得弄清它的含义。{PART_003}我们是在翻译，{PART_004}巴蒂斯塔。
围绕一个概念
{SPEAKER_AKERS}{PART_000}凯莉，{PART_001}看得出你又在琢磨什么。
{SPEAKER_COLLINS}{PART_000}{SIG_N154}和{SIG_N155}。{PART_001}那些传输大多和{SIG_N132}有关，{PART_002}或者{SIG_N133}、{SIG_N046}、{SIG_N045}。
{SPEAKER_AKERS}{PART_000}除了那段关于{SIG_N044}的。
{SPEAKER_COLLINS}{PART_000}除了那段，{PART_001}{SIG_N154}和{SIG_N155}一直围绕着{SIG_N101}这个概念。
柯林斯还好吗
{SPEAKER_AKERS}{PART_000}$animA1嘿，{PART_001}那段说的是{SIG_N044}！{PART_002}$animA3每次看到都高兴。{PART_003}说起来，{PART_004}你还好吗，{PART_005}凯莉？{PART_006}$animA2上周，{PART_007}你好像……{PART_008}$animA5呃……{PART_009}脑子里有点乱。
{SPEAKER_COLLINS}{PART_000}确实，{PART_001}我当时有点招架不住。{PART_002}$animC5不过我花了些时间，重新理清了头绪。
{SPEAKER_AKERS}{PART_000}$animA0很高兴看到你恢复平时的样子！
{SPEAKER_COLLINS}{PART_000}谢谢你，艾伦！{PART_001}$animC2你真贴心。
定义放行
{SPEAKER_AKERS}{PART_000}你也知道，{PART_001}$animA4定义传输要是{SIG_N012} {SIG_N004} 0，准没好事。
{SPEAKER_COLLINS}{PART_000}$animC1尤其是下一段还引入了两个新信号……
{SPEAKER_BAUTISTA}{PART_000}$animA0{SIG_N012} {SIG_N004} 0 永远是好事。
{SPEAKER_COLLINS}{PART_000}$animC5唉，{PART_001}我就知道你会这么想。
不管怎样，二者都是
{SPEAKER_COLLINS}{PART_000}{SIG_N156}不对？{PART_001}有意思。
{SPEAKER_AKERS}{PART_000}不管{SIG_N157}是什么意思，{PART_001}两边都是{SIG_N157}。
紧随其后
{SPEAKER_AKERS}{PART_000}好像只要有{SIG_N133}，{PART_001}后面就会跟着{SIG_N157}。
紧密联系
{SPEAKER_AKERS}{PART_000}{SIG_N143}和{SIG_N142}——{PART_001}它们的关系也太紧密了。
{SPEAKER_COLLINS}{PART_000}我知道……
{SPEAKER_AKERS}{PART_000}怎么了？
{SPEAKER_COLLINS}{PART_000}让我不禁想知道{SIG_N153}是什么意思。
新朋友
{SPEAKER_AKERS}{PART_000}是我理解的那样吗？
{SPEAKER_COLLINS}{PART_000}应该是。{PART_001}这，{PART_002}如果我们的理解没错，{PART_003}意味着我们也许要认识一位新朋友了。
五个信号
{SPEAKER_AKERS}{PART_000}$animA3那段传输只有五个信号。
{SPEAKER_COLLINS}{PART_000}$animC5可这五个信号表达了不少东西……
再次确认
{SPEAKER_COLLINS}{PART_000}我不确定这是什么意思，{PART_001}$animC1但结构很像定义传输。{PART_002}$animC3也许只需要再次确认我们对{SIG_N144}的理解？
序列中的下一个
{SPEAKER_COLLINS}{PART_000}看来不用再照着他们的信号发送了。
{SPEAKER_AKERS}{PART_000}而这段是第一段包含多个信号的传输。{PART_001}你觉得我们该发多个，还是只发一个？
{SPEAKER_COLLINS}{PART_000}我猜陨石只在等一个信号，{PART_001}也就是序列里的下一个。
{SPEAKER_AKERS}{PART_000}好吧，{PART_001}看看这样行不行。
又要照着发？
{SPEAKER_COLLINS}{PART_000}{PLAYER_NAME}，{PART_001}$animC4我有个想法。{PART_002}$animC3我猜陨石是在测试我们能不能跟上。{PART_003}$animC2他们想看看，我们能否对不同的信号作出回应。
{SPEAKER_AKERS}{PART_000}$animA3那你觉得该怎么做？
{SPEAKER_COLLINS}{PART_000}$animC5我觉得还得再照着他们的传输发一次。
从 1 开始？
{SPEAKER_COLLINS}{PART_000}巴蒂斯塔，{PART_001}那里为什么会有个 1？
{SPEAKER_BAUTISTA}{PART_000}嗯？
{SPEAKER_COLLINS}{PART_000}{SIG_N045} 1？{PART_001}他们通常从 0 开始计数。
{SPEAKER_BAUTISTA}{PART_000}嗯，{PART_001}从 0 开始编号。
{SPEAKER_COLLINS}{PART_000}他们不会到现在才改。{PART_001}这个 1 一定是回应的关键。
根本算不出来
{SPEAKER_AKERS}{PART_000}{SIG_N203} {SIG_N023}，{PART_001}这个根本没法知道，{PART_002}对吧？{PART_003}$animA1这里有{SIG_N203} 0、{SIG_N203} 3、{SIG_N203} 8，后面还没完。{PART_004}要算出{SIG_N203} {SIG_N023}，也就是单个{SIG_N129}的总数，感觉根本不可能。
氦元素速成课
{SPEAKER_AKERS}{PART_000}{SIG_N056}_2，{PART_001}按{PLAYER_NAME}的定义，{PART_002}$animA3那肯定就是氦。
{SPEAKER_COLLINS}{PART_000}那这能给我们什么启发？
{SPEAKER_AKERS}{PART_000}它很小，{PART_001}丰度很高，{PART_002}质量低，只有两个质子。{PART_003}$animA1它位于元素周期表第 18 族，{PART_004}也就是说，它属于稀有气体。{PART_005}而且它和氢一样，{PART_006}几乎从宇宙诞生起就存在了。{PART_007}也许这里面有什么能派上用场，{PART_008}或者参考页签里会有线索。{PART_009}我目前就想到这些。
氦是稀有气体
{SPEAKER_AKERS}{PART_000}我终于有点看懂这里的句法结构了。{PART_001}氦是稀有气体，{PART_002}所以{SIG_N055}_2 {SIG_N058} {SIG_N029}。{PART_003}而{SIG_N056} 2 也不是。
有什么区别？
{SPEAKER_COLLINS}{PART_000}$animC3既然我们已经很熟悉{SIG_N129}了，{PART_001}我想分析一下它与{SIG_N045}相比，分别起什么作用。
{SPEAKER_AKERS}{PART_000}$animA3什么意思？
{SPEAKER_COLLINS}{PART_000}$animC5这两个词的概念非常相近；{PART_001}它们的含义究竟有何区别？{PART_002}为什么选择一个，而不用另一个？
{SPEAKER_AKERS}{PART_000}$animA5对，{PART_001}这个问题，{PART_002}我也想过。{PART_003}我能理解{PLAYER_NAME}给出的定义，{PART_004}{SIG_N045}和{SIG_N129}。{PART_005}这些我都懂，{PART_006}$animA3可还是很难判断，什么时候该用这个，什么时候会变成那个。
有什么区别？（二）
{SPEAKER_COLLINS}{PART_000}{SIG_N045}和{SIG_N129}——{PART_001}$animC3这让我想到查尔斯·菲尔莫尔对框架语义学的研究。{PART_002}在语言中，{PART_003}$animC2符号与意义相对应，{PART_004}这一点我们都知道。{PART_005}我们把“面包”这个词对应到面包的某个概念上，{PART_006}而你我脑海里的东西大概很相似。
{SPEAKER_AKERS}{PART_000}$animA2对，{PART_001}我想到了一整块面包。
{SPEAKER_COLLINS}{PART_000}$animC5嗯，{PART_001}呃，{PART_002}很好。{PART_003}$animC4我想的也是。{PART_004}$animC3可这意味着什么？{PART_005}我们说“面包”时，脑海里的概念到底是什么？
{SPEAKER_AKERS}{PART_000}是一种食物，{PART_001}有特定的味道，{PART_002}由谷物做成，{PART_003}$animA3我会想到三明治和汉堡，{PART_004}还有——
{SPEAKER_COLLINS}{PART_000}对，对，{PART_001}这些我都赞同。{PART_002}$animC2但菲尔莫尔的基本观点是，词语不仅是装载意义的容器，{PART_003}还是让我们思考或唤起不同语境的方式。{PART_004}$animC5而这些框架的核心，都与人类经验有关。{PART_005}$animC3从这个角度看，{PART_006}语言并不是由基本单位和一层层描述堆砌而成的。{PART_007}关键在于借助我们的视角来理解。
{SPEAKER_AKERS}{PART_000}嗯……{PART_001}$animA3那“面包”呢？{PART_002}这里有哪些框架？
{SPEAKER_COLLINS}{PART_000}“面包”会唤起与进食、{PART_001}烹饪、{PART_002}耕作有关的框架。{PART_003}$animA0同时也会让我们想到面包的特征。{PART_004}$animC4浅褐色的外皮、{PART_005}柔软洁白的内里、{PART_006}香气、{PART_007}气味。{PART_008}这一切都和我们的经验，以及与面包的联想有关。{PART_009}$animC5但还不止如此。{PART_010}波斯语里，{PART_011}有一个短语，意思是：{PART_012}“天上的白面包”。{PART_013}这个短语指的是月亮。{PART_014}那他们为什么会把月亮叫作面包？
{SPEAKER_AKERS}{PART_000}跟我管它叫奶酪一个道理！{PART_001}$animA4因为看着像啊。
{SPEAKER_COLLINS}{PART_000}没错！{PART_001}$animC5这就是框架语义学。
{SPEAKER_BAUTISTA}{PART_000}你应该更清楚才对，{PART_001}天文学家。
{SPEAKER_AKERS}{PART_000}什么？
{SPEAKER_BAUTISTA}{PART_000}月亮不是奶酪做的。
{SPEAKER_AKERS}{PART_000}$animA4我早就知道了！{PART_001}你知道我查过这事，{PART_002}对吧？{PART_003}不，{PART_004}那甚至都算不上研究，{PART_005}只是“查找”。{PART_006}阿波罗任务、{PART_007}尼尔·阿姆斯特朗、{PART_008}巴兹·奥尔德林，{PART_009}$animA3想起来了吗？
{SPEAKER_BAUTISTA}{PART_000}$animB4哦，对。{PART_001}你跑腿买咖啡的日子。{PART_002}$animB5……{PART_003}$animB2这个话头是你自己递过来的。
{SPEAKER_AKERS}{PART_000}$animA5对。{PART_001}对。{PART_002}是我自己。
有什么区别？（三）
{SPEAKER_AKERS}{PART_000}那么语言学家小姐，{PART_001}$animA3{SIG_N045}和{SIG_N129}会唤起什么框架？
{SPEAKER_COLLINS}{PART_000}我会先看它们各自用在哪里。{PART_001}$animC1{SIG_N045}出现得早得多，{PART_002}所以有时会用在如今陨石应该会改用{SIG_N129}的地方。{PART_003}$animC5还记得很久以前那幅自画像{SIG_N053}吗？{PART_004}画面上只有一个{SIG_N129}，{PART_005}可我们回应时用了{SIG_N045}。
{SPEAKER_AKERS}{PART_000}$animA5你是说，要是现在再看到一次，{PART_001}我们就会用{SIG_N129}。
{SPEAKER_COLLINS}{PART_000}$animC3事实上，后来真的又出现了！{PART_001}他们定义了{SIG_N129}与{SIG_N128}的关系后，{PART_002}我们再指他们的{SIG_N053}肖像时，用的就是{SIG_N129}。
{SPEAKER_AKERS}{PART_000}所以一个东西只有 1 个时，{PART_001}就用{SIG_N129}，{PART_002}有多个就用{SIG_N045}？{PART_003}这只是他们的复数形式！
{SPEAKER_COLLINS}{PART_000}这个规律似乎也不成立。{PART_001}$animC5我们见过描绘并讨论多个{SIG_N129}的传输。
{SPEAKER_AKERS}{PART_000}$animA0哼。
{SPEAKER_COLLINS}{PART_000}我还是会想到语境，{PART_001}也就是框架。{PART_002}艾伦，{PART_003}假如有一段传输说的是{SIG_N044} 0，{PART_004}$animC4他们会用哪个词指发送者？
{SPEAKER_AKERS}{PART_000}$animA1当然是{SIG_N045}。
{SPEAKER_COLLINS}{PART_000}为什么？
{SPEAKER_AKERS}{PART_000}$animA3因为我觉得那是他们全体送来的。{PART_001}重点不在某个{SIG_N128}。{PART_002}当然，{PART_003}是{SIG_N136}把它组装起来的，{PART_004}但真正的重点是他们的{SIG_N131}。
{SPEAKER_COLLINS}{PART_000}$animC2但它只在一部分意义上与他们的{SIG_N131}有关。
{SPEAKER_AKERS}{PART_000}为什么这么说？
{SPEAKER_COLLINS}{PART_000}在我看来，{SIG_N131}唤起的是科学框架，{PART_001}也就是生物分类。{PART_002}{SIG_N045}指的东西更……{PART_003}宽泛。
{SPEAKER_AKERS}{PART_000}$animA5可他们也把{SIG_N045}说成一种{SIG_N131}。
{SPEAKER_COLLINS}{PART_000}那只是在讨论{SIG_N131}时才这样说。
{SPEAKER_AKERS}{PART_000}$animA2那它还能是什么？{PART_001}是他们的政府，{PART_002}还是他们的文明，{PART_003}或者他们用来组织自己的某种形式？
{SPEAKER_COLLINS}{PART_000}$animC4既然这些我们都没讨论过，{PART_001}那它肯定不是专指其中某一个。
{SPEAKER_AKERS}{PART_000}$animA5所以它到底是什么？
{SPEAKER_COLLINS}{PART_000}$animC3我也说不太准。{PART_001}只能看看哪些语境会用{SIG_N045}，而不用{SIG_N129}。{PART_002}根据我的归纳，{PART_003}$animC5{SIG_N045}指的是他们广义上的集体。{PART_004}它会让人想到他们的文化、{PART_005}{SIG_N131}，{PART_006}以及合作关系。
图像帮上忙了
{SPEAKER_AKERS}{PART_000}嘿，{PART_001}$animA1你知道吗，{PART_002}$animA1那个{SIG_N053}真帮我弄懂了{SIG_N162}。
{SPEAKER_BAUTISTA}{PART_000}嗯。{PART_001}有意思。
{SPEAKER_AKERS}{PART_000}哦？{PART_001}$animA3有什么让你感兴趣了？{PART_002}快说！
{SPEAKER_BAUTISTA}{PART_000}$animB1他们用{SIG_N054}突出某个特征。
{SPEAKER_AKERS}{PART_000}$animA0这怎么了？
{SPEAKER_BAUTISTA}{PART_000}这种做法，{PART_001}人类也会用。{PART_002}这很合理。{PART_003}每个{SIG_N052}都携带五个信息通道。{PART_004}$animB3{SIG_N049}、{SIG_N050}、{SIG_N051}和{SIG_N022}对于描绘目标物体都很重要。
{SPEAKER_AKERS}{PART_000}也就是目标{SIG_N129}。
{SPEAKER_BAUTISTA}{PART_000}对。{PART_001}$animB5目标{SIG_N129}。{PART_002}空间结构比{SIG_N054}更值得表现，{PART_003}所以他们可以改变那个信息通道，突出{SIG_N162}。{PART_004}……{PART_005}$animB0说完了。
{SPEAKER_AKERS}{PART_000}我倒没这么想过，{PART_001}但还挺有意思的。{PART_002}我们和{SIG_N129}又多了一个共同点。
凯莉的语言学
{SPEAKER_COLLINS}{PART_000}在这里见到语义基元，感觉很奇妙。{PART_001}我们在埃克斯的天文学里泡了那么久，{PART_002}还研究了那么多化学。{PART_003}现在终于又碰上{SIG_N100}、{SIG_N106}、{SIG_N110}、{SIG_N111}、{SIG_N116}、{SIG_N115}这样的词。{PART_004}看到这些真让人开心。
{SPEAKER_AKERS}{PART_000}就像刚开始那会儿，{PART_001}对吧？{PART_002}还记得吗，{PART_003}那已经是半年前了。{PART_004}当时就我们四个，{PART_005}对着氢谱线，{PART_006}重新学习怎么做数学题。
{SPEAKER_COLLINS}{PART_000}这就是我说的语义基元：{PART_001}最基础的概念。{PART_002}只不过现在，东西会是{SIG_N104}或{SIG_N105}。
{SPEAKER_AKERS}{PART_000}是吗？{PART_001}你这语言学家的脑子真好使。{PART_002}我说话时基本不想这些。
{SPEAKER_BAUTISTA}{PART_000}看得出来。
{SPEAKER_AKERS}{PART_000}没那个必要！{PART_001}还有，你从哪里冒出来的？{PART_002}继续安静去！
{SPEAKER_BAUTISTA}{PART_000}嗯 :(
凯莉的语言学（三）
{SPEAKER_AKERS}{PART_000}凯莉，{PART_001}你知道吗，刚才那还是没回答问题。{PART_002}那一大段语言、显微镜什么的，{PART_003}是挺有意思，{PART_004}可还是没回答。{PART_005}这种答案我看大学招生手册就能看到。
{SPEAKER_COLLINS}{PART_000}可这就是我的答案。{PART_001}你不喜欢，我也没办法。
{SPEAKER_AKERS}{PART_000}答案本身没问题，{PART_001}你就是在故意绕开问题！
{SPEAKER_COLLINS}{PART_000}可你根本没把我的答案当回事！{PART_001}这就是我的答案！
{SPEAKER_AKERS}{PART_000}这是个跟人保持距离的回答。
{SPEAKER_COLLINS}{PART_000}我就是个爱跟人保持距离的人。
{SPEAKER_AKERS}{PART_000}别这么说……
凯莉的语言学（四）
{SPEAKER_COLLINS}{PART_000}对不起，艾伦，{PART_001}我刚才表现得有些……{PART_002}太疏离了。
{SPEAKER_AKERS}{PART_000}你道什么歉？
{SPEAKER_COLLINS}{PART_000}说实话，{PART_001}那确实就是我的答案。{PART_002}学习不同的语言如何构建意义，{PART_003}真的很迷人，{PART_004}甚至令人振奋！{PART_005}对我来说，{PART_006}也能借此看清自己。{PART_007}你能明白吗？
{SPEAKER_AKERS}{PART_000}其他语言就像一面镜子，让你看见自己，{PART_001}是吧？
{SPEAKER_COLLINS}{PART_000}与其说是镜子，{PART_001}不如说更像……{PART_002}一只温暖的手。
凯莉的语言学（二）
{SPEAKER_AKERS}{PART_000}那么，凯莉，{PART_001}你为什么喜欢语言学？{PART_002}是什么吸引了你？{PART_003}我已经知道巴蒂斯塔为什么是电脑天才了。{PART_004}可你为什么喜欢语言？
{SPEAKER_BAUTISTA}{PART_000}嗯？{PART_001}你知道我为什么喜欢电脑？
{SPEAKER_AKERS}{PART_000}当然！{PART_001}你对这些东西特别着迷！{PART_002}但别误会，{PART_003}我很欣赏你的热情！{PART_004}你的才华就是我们庆祝时撒彩纸的源泉！
{SPEAKER_COLLINS}{PART_000}……还有我们的词典、{PART_001}计算器、{PART_002}参考页签——
{SPEAKER_AKERS}{PART_000}所以凯莉，{PART_001}回到我的问题。{PART_002}为什么是语言学？
{SPEAKER_COLLINS}{PART_000}我在马萨诸塞大学取得了语言学理学学士学位。{PART_001}之后也在那里完成了博士学业，随后受聘为波士顿学院助理教授。{PART_002}33 岁时，我晋升教授并获得终身教职，{PART_003}是校内做到这一点最年轻的女性。
{SPEAKER_AKERS}{PART_000}这些都很了不起……{PART_001}可我问的是“为什么”？{PART_002}什么吸引你进入语言学？{PART_003}你当然很擅长！{PART_004}我想知道是什么把你带进这个领域的。{PART_005}我就是好奇。
{SPEAKER_COLLINS}{PART_000}我喜欢学习其他语言，也喜欢研究词语如何产生意义。{PART_001}把沟通本身放到显微镜下观察时，我觉得自己会更了解这个世界。{PART_002}这让我着迷，也让我重新认识自己。
有什么区别？（四）
{SPEAKER_COLLINS}{PART_000}如果我们弄清了他们如何区分{SIG_N045}和{SIG_N129}，{PART_001}也就是这两个词共有的作用，{PART_002}那我们或许也能弄懂{SIG_N046}和{SIG_N130}，{PART_003}以及它们之间的关系。{PART_004}我们可以理解{SIG_N130}在{SIG_N046}中扮演的角色。{PART_005}我觉得值得一试。
三泽空军基地
{SPEAKER_AKERS}{PART_000}$animA1真希望外星人能说得正常点。
{SPEAKER_BAUTISTA}{PART_000}这个问题已经说过了——
{SPEAKER_AKERS}{PART_000}对，{PART_001}对，{PART_002}我知道。{PART_003}只是抱怨两句。{PART_004}我一看到{SIG_N081} {SIG_N087}、{SIG_N080} {SIG_N085}这种东西，有时就转不过来。{PART_005}$animA5{SIG_N085}到底有什么用？{PART_006}感觉毫无意义。
{SPEAKER_BAUTISTA}{PART_000}我也会这样评价你。
{SPEAKER_AKERS}{PART_000}喂！
{SPEAKER_COLLINS}{PART_000}对我来说倒很好理解，{PART_001}$animC3但也许是因为我熟悉日语。{PART_002}日语句子经常以“desu”或“da”的音收尾。{PART_003}它本身没有意义，但会修饰前面的动词或形容词。
{SPEAKER_AKERS}{PART_000}你会说日语？{PART_001}$animA3我猜是为了做研究？
{SPEAKER_COLLINS}{PART_000}$animC5说来也巧，{PART_001}我会的那一点日语，是在想当语言学家之前学的。
{SPEAKER_AKERS}{PART_000}$animA5那你是怎么学会的？
{SPEAKER_COLLINS}{PART_000}朝鲜战争结束后，{PART_001}$animC2我父亲被派驻到三泽空军基地。
{SPEAKER_AKERS}{PART_000}哦，真不错！{PART_001}待了多久？
{SPEAKER_COLLINS}{PART_000}两年，{PART_001}但我不会说那段日子“不错”。{PART_002}$animC4那里很美，{PART_003}偶尔可以一路走到海边，{PART_004}$animC5可基地很小，{PART_005}并不适合家属生活。{PART_006}一开始我根本不想去。
{SPEAKER_AKERS}{PART_000}$animA3是吗？
{SPEAKER_COLLINS}{PART_000}当时我马上就要高中毕业了。{PART_001}本来说好不再搬家了，{PART_002}$animA5但这种事你也知道。{PART_003}我好不容易才交到朋友，{PART_004}认识了几个很好的人。{PART_005}我们本来要一起去马萨诸塞大学。{PART_006}可当然，{PART_007}$animC0我父亲有他的职责。
{SPEAKER_AKERS}{PART_000}可你后来还是回去了，{PART_001}不是吗？{PART_002}读本科，{PART_003}对吧？
{SPEAKER_COLLINS}{PART_000}对。
{SPEAKER_AKERS}{PART_000}再见到他们一定很开心。
{SPEAKER_COLLINS}{PART_000}是啊，要是能见到就好了。
{SPEAKER_AKERS}{PART_000}你没再见过他们？
{SPEAKER_COLLINS}{PART_000}$animC5后来一直没凑成。{PART_001}对 17 岁的孩子来说，分别两年实在太久了。{PART_002}而且他们从小学起就互相认识。
诺佐米
{SPEAKER_AKERS}{PART_000}所以凯莉，{PART_001}你当时就这么，{PART_002}怎么说，{PART_003}耳濡目染地学会了日语？{PART_004}$animA3光待在那里就吸收进去了？
{SPEAKER_COLLINS}{PART_000}哈哈，{PART_001}差不多吧！{PART_002}$animC3不过人在当地能学到的东西之少，可能会让你意外。{PART_003}$animA0待在基地里，{PART_004}就像身处一小块美国。{PART_005}但就像我说的，{PART_006}$animC5那里的海很美。{PART_007}所以放学后，我会走去港口。{PART_008}坐在那里看夕阳落入海浪。{PART_009}$animC5周末的时候，{PART_010}我会在清晨看渔民随翻涌的海雾出航。{PART_011}我听他们说话，{PART_012}元音比我习惯的美式英语尖锐、平直得多。{PART_013}那一整幅景象都很宁静。{PART_014}后来，我在那里交到了一个朋友。{PART_015}是个叫诺佐米的温柔女孩。
{SPEAKER_AKERS}{PART_000}她会英语吗？
{SPEAKER_COLLINS}{PART_000}一点也不会！
{SPEAKER_AKERS}{PART_000}$animA4而你也不会日语……
{SPEAKER_COLLINS}{PART_000}一个词都不会。
{SPEAKER_AKERS}{PART_000}那你们两个到底怎么聊……{PART_001}$animA2任何事情？
{SPEAKER_COLLINS}{PART_000}$animC4这才有意思！{PART_001}$animC5我们在码头注意到了彼此，{PART_002}两个人都闲得无聊。{PART_003}$animA5于是我们走向对方，{PART_004}对上目光，然后，{PART_005}就试着交谈！{PART_006}可当然没聊出什么结果。{PART_007}但我得告诉你，{PART_008}那是我这辈子经历过最美好的初次相识。
{SPEAKER_AKERS}{PART_000}你这话听着，怎么好像那时候不太喜欢别人？
{SPEAKER_COLLINS}{PART_000}不，{PART_001}哈哈，我不是那个意思。{PART_002}我从来都不厌恶人类。{PART_003}$animC3也许对父亲或空军有点不满，{PART_004}但从没对整个人类失望。{PART_005}我的意思是，她很友善。{PART_006}我说不出她讲了什么，{PART_007}却能看出她想表达什么。{PART_008}$animC5发现言语和意图并不总是紧紧相连，感觉很奇妙。
与诺佐米交流
{SPEAKER_AKERS}{PART_000}所以说真的，{PART_001}凯莉，{PART_002}$animA3你们俩到底聊些什么？{PART_003}你和纳佐米……？
{SPEAKER_COLLINS}{PART_000}是诺佐米，{PART_001}不过没错。{PART_002}后来我们都零零碎碎学会了几句对方的话。{PART_003}我们会指指东西、拿起实物比划，{PART_004}但有很长一阵子，做得最多的还是一起笑。{PART_005}我们基本只能交流这么多。
{SPEAKER_AKERS}{PART_000}$animA4毕竟笑声是全世界的共同语言。
{SPEAKER_BAUTISTA}{PART_000}$animB4外星人可不是。
{SPEAKER_AKERS}{PART_000}说不定啊！{PART_001}$animA5我们又不知道他们不会笑！
{SPEAKER_BAUTISTA}{PART_000}$animB0嗯。
{SPEAKER_AKERS}{PART_000}我只知道你不会笑，{PART_001}巴蒂斯塔！
{SPEAKER_BAUTISTA}{PART_000}$animB3我会笑。
{SPEAKER_AKERS}{PART_000}$animA0你最近一次笑是什么时候？
{SPEAKER_BAUTISTA}{PART_000}昨晚。
{SPEAKER_AKERS}{PART_000}我不信。{PART_001}$animA4为什么笑？
{SPEAKER_BAUTISTA}{PART_000}我给妻子打了电话，她讲了个好笑的笑话——
{SPEAKER_AKERS}{PART_000}——$animA2我总忘记你结婚了！{PART_001}我从没觉得你是那种……{PART_002}$animA5怎么说才礼貌……{PART_003}$animA3会温柔待人的人！
{SPEAKER_BAUTISTA}{PART_000}$animB5你这话说得真好听。
{SPEAKER_AKERS}{PART_000}$animA4怎么了！这是真的！
人生阶段
{SPEAKER_AKERS}{PART_000}你上次和诺佐米说话是什么时候？
{SPEAKER_COLLINS}{PART_000}就是那时候，{PART_001}25 年前。
{SPEAKER_AKERS}{PART_000}什么？！{PART_001}你是怕日语已经忘光了吗？{PART_002}可当年不会说，也没拦住你们两个……
{SPEAKER_COLLINS}{PART_000}哦，不不，{PART_001}人生就是这样，{PART_002}你知道的。
{SPEAKER_AKERS}{PART_000}不，{PART_001}我不知道。{PART_002}那你给我讲讲。
{SPEAKER_COLLINS}{PART_000}你会遇见一些人，{PART_001}享受彼此相伴的时光，{PART_002}希望能留下一些珍贵、{PART_003}无比珍贵的回忆。{PART_004}可除了自己建立的家庭，{PART_005}命运终究会把你带去别处。
{SPEAKER_AKERS}{PART_000}别这样，凯莉。
{SPEAKER_COLLINS}{PART_000}我不觉得这是件悲哀的事！{PART_001}人生有不同的阶段。{PART_002}它会把你带到该去的地方。{PART_003}如果我留在那里，{PART_004}就不会遇见约翰。{PART_005}也不会有克洛伊和卡特——
{SPEAKER_AKERS}{PART_000}原来你有孩子？
{SPEAKER_COLLINS}{PART_000}对，{PART_001}我是。
{SPEAKER_AKERS}{PART_000}你以前好像从没提过，{PART_001}凯莉。{PART_002}这不会又像我忘了巴蒂斯塔已经结婚那样，{PART_003}其实是我自己忘了吧？{PART_004}喂，书呆子，{PART_005}你记得她有孩子吗？
{SPEAKER_BAUTISTA}{PART_000}我拒绝回应这个称呼。{PART_001}不过我也不记得。
{SPEAKER_AKERS}{PART_000}看见没有？{PART_001}那个书呆子可是什么都不会忘！
{SPEAKER_COLLINS}{PART_000}那你们现在知道了。
{SPEAKER_AKERS}{PART_000}克洛伊和卡特……
长得真怪
{SPEAKER_AKERS}{PART_000}我能直说吗？{PART_001}$animA3我绝对没有冒犯的意思……
{SPEAKER_BAUTISTA}{PART_000}$animB5肯定不是什么好话。
{SPEAKER_COLLINS}{PART_000}不，{PART_001}$animC5绝对不是。
{SPEAKER_AKERS}{PART_000}$animA1他们长得真怪！{PART_001}我是说前两个{SIG_N053}。{PART_002}$animA3我知道巴蒂斯塔满脑子都是“哦，他们会用{SIG_N054}，真厉害，嗯嗯”，{PART_003}可说真的，{PART_004}$animA1他们就是很怪！
{SPEAKER_BAUTISTA}{PART_000}你这样说我们的朋友，不好。
{SPEAKER_COLLINS}{PART_000}$animC3我——{PART_001}$animC4等等，{PART_002}巴蒂斯塔……{PART_003}$animC5哎呀……
{SPEAKER_BAUTISTA}{PART_000}不。
{SPEAKER_AKERS}{PART_000}他管他们叫我们的朋友！
{SPEAKER_BAUTISTA}{PART_000}没有。{PART_001}正在删除日志。
{SPEAKER_COLLINS}{PART_000}你知道不能这么做。
{SPEAKER_BAUTISTA}{PART_000}哼。
多普勒、柯林斯和她
{SPEAKER_COLLINS}{PART_000}$animC1按{PLAYER_NAME}给{SIG_N143}定的词来看，{PART_001}多普勒、{PART_002}我，{PART_003}还有{SIG_N136}，应该都是{SIG_N143}。
{SPEAKER_AKERS}{PART_000}$animA3不过我觉得{SIG_N136}比你更胜一筹，{PART_001}凯莉……
{SPEAKER_COLLINS}{PART_000}$animC4这又不是比赛！{PART_001}而且我们和她属于不同的{SIG_N131}！{PART_002}$animC1根本不能相提并论！
最后一次道别
{SPEAKER_COLLINS}{PART_000}哦，{PLAYER_NAME}！{PART_001}真高兴又见到你！{PART_002}我正在重新整理笔记，{PART_003}最后再整理一次。{PART_004}艾伦和巴蒂斯塔两天前就已经动身回家了，{PART_005}所以这里一直很安静。{PART_006}多普勒公开了我们翻译工作的完整记录后，{PART_007}哈哈，整个世界都够他忙的！{PART_008}不过今天，我把约翰和孩子们带来了！{PART_009}终于能让他们看看，过去一年我都待在什么地方。{PART_010}我还给克洛伊和卡特看了你的词典！{PART_011}他们很喜欢你给{SIG_N136}和{SIG_N129}起的名字。{PART_012}{SIG_N097}吸引了克洛伊的注意。{PART_013}她好像正在学校里学习银河系。{PART_014}所以一下就被它迷住了。{PART_015}{SIG_N095}把她逗笑了，{PART_016}但听完我的解释，她就入迷了。{PART_017}不过要是艾伦听到我拿“碎肉”打的比方，肯定要说我几句。{PART_018}卡特觉得{SIG_N144}特别好玩。{PART_019}可能正是这个年纪吧。{PART_020}而且我们既然都来了，我实在没忍住。{PART_021}我一时心软，把巴蒂斯塔对它的假说也给他看了。{PART_022}看来巴蒂斯塔也觉得{SIG_N144}很好玩，{PART_023}是吧。{PART_024}那看来任何年纪都一样。{PART_025}他们还问起{SIG_N212}和{SIG_N211}。{PART_026}我尽力解释了，{PART_027}可你也知道有多难。{PART_028}陨石可是花了大约 100 段传输来解释！{PART_029}我怎么可能两分钟讲清外星人的{SIG_N210}！{PART_030}啊，{PART_031}能给他们看看我待过的地方，真是太好了。{PART_032}能再次见到约翰也很开心，{PART_033}真的。{PART_034}我让他们去热辣肉饼了。{PART_035}他们也就在电话里听我提过一百次而已。{PART_036}而且，{PART_037}卡特饿了。{PART_038}所以多普勒在忙，{PART_039}另外两个人又走了，{PART_040}这里一直很安静。{PART_041}……{PART_042}不过你从来都不爱说话，{PART_043}对吧，{PLAYER_NAME}？{PART_044}说来也有意思，{PART_045}有时我觉得，你表达的东西比我们任何人都多。{PART_046}你在选择词语时表达了太多。{PART_047}{SIG_N244}、{PART_048}{SIG_N243}、{PART_049}{SIG_N245}……{PART_050}我一直没来得及写下对这三个词的假说……{PART_051}但我仍然在想！{PART_052}我猜{SIG_N136}和{SIG_N045}是在表达某种感激。{PART_053}我也有同样的感受，{PART_054}虽然心里还是很想见见他们。{PART_055}可他们离我们究竟有多远？{PART_056}我记得艾伦算过。{PART_057}好像是 120 光年。{PART_058}也许有一天，我们会找到那颗潮汐锁定行星，{PART_059}那颗体积为地球八倍的……{SIG_N140}。{PART_060}总之，{PART_061}你的话表达了很多东西，{PART_062}{PLAYER_NAME}。{PART_063}而且，那正是你自己的词语。{PART_064}沟通时，倾听者付出的努力不比说话者少。{PART_065}再想想，{PART_066}我的天……{PART_067}几百年、{PART_068}几千年以后，{PART_069}人类使用的将是你的词语。{PART_070}我想那种感觉一定……{PART_071}算了，{PART_072}这一次，我想象不出来。{PART_073}……{PART_074}能和你一起工作，真的很开心。{PART_075}我本想说，希望以后还能见到你，{PART_076}但我知道我们一定会再见。{PART_077}我会来看你，{PART_078}好吗？{PART_079}我们会抽出时间——{PART_080}你、我、艾伦、巴蒂斯塔、多普勒。{PART_081}我很想让你们都见见我的家人。{PART_082}……{PART_083}说起他们，{PART_084}应该很快就会从热辣肉饼回来了。{PART_085}我该去外面等他们。{PART_086}不过天还亮着就离开这栋楼，感觉会很奇怪。{PART_087}对吧？{PART_088}能和你共事，是我的荣幸。{PART_089}我把你视作自己最好的朋友之一。{PART_090}{SIG_N243} {PLAYER_NAME}。
{SPEAKER_AUTO_LOG}{PART_000}（凯莉走开了。{PART_001}面带微笑。{PART_002}……{PART_003}现在只剩你了！{PART_004}去做些了不起的事吧。）
凯莉与诺佐米
{SPEAKER_AKERS}{PART_000}感觉刚才说跑题了，{PART_001}凯莉。{PART_002}$animA5你和诺佐米都聊些什么？
{SPEAKER_COLLINS}{PART_000}后来我们拿到了一本双语词典，{PART_001}$animC3交流就快多了。{PART_002}我们发现彼此还是有些共同话题可以聊。{PART_003}当时我们都是 17 岁，都在为父母和学校烦恼。{PART_004}我们还发现，两个人都喜欢跟着收音机一起唱歌。{PART_005}同样，{PART_006}还是听不太懂，{PART_007}但模仿那些声音很有趣，{PART_008}能体会以不同方式表达语言是什么感觉。
{SPEAKER_AKERS}{PART_000}嗯。
{SPEAKER_COLLINS}{PART_000}$animC3她开始请我去她家，教我玩纸牌。{PART_001}可她放学比我晚一个小时，{PART_002}$animC2所以我会和她母亲坐在一起等她。{PART_003}她母亲总是默默把一杯茶放到我面前。{PART_004}我会尽自己所能，礼貌地说“谢谢”，{PART_005}她也会轻声回答，{PART_006}同时带着微笑。{PART_007}后来诺佐米告诉我，她母亲觉得我努力说日语的样子很可爱。{PART_008}为了表达感激，{PART_009}我的用语正式过头了。{PART_010}$animC5不过我想，她觉得那很讨人喜欢。
梅雨季
{SPEAKER_COLLINS}{PART_000}不知不觉间，{PART_001}诺佐米和我已经形影不离。{PART_002}简直像绑在一起。{PART_003}$animC4其实有一次，我们还真绑在了一起！{PART_004}我把自己的右腿和她的左腿绑住，{PART_005}$animC2两个人三条腿，走了整个下午。
{SPEAKER_AKERS}{PART_000}整个下午？{PART_001}你们不用上学吗？
{SPEAKER_COLLINS}{PART_000}那是学校放暑假时的梅雨季。{PART_001}$animC3到那时，{PART_002}我已经会说足够多的日语，能和她聊上一整天，{PART_003}虽然还是很费劲。{PART_004}我们聊学校、{PART_005}男孩、{PART_006}父母、{PART_007}衣服、{PART_008}美国的生活、{PART_009}日本的生活，{PART_010}$animC5还有未来。
{SPEAKER_AKERS}{PART_000}你们的未来？
{SPEAKER_COLLINS}{PART_000}以前我对这件事很敏感。{PART_001}因为总在搬家，{PART_002}我很难决定自己想待在哪里。{PART_003}$animC4我从来没有机会扎根，{PART_004}也无法把一个地方称为家。{PART_005}但她从没因此疏远我。{PART_006}$animC3她还是想把剩下的每一刻都用来和我一起开心。
{SPEAKER_AKERS}{PART_000}她听起来真的很善良。
{SPEAKER_COLLINS}{PART_000}$animC5我跟你说，艾伦，{PART_001}她真的是个很温柔的人。{PART_002}那是我说过最难的一次再见。
""".strip().splitlines()


LONG_GOODBYE_REPLACEMENTS = (
    ("{PART_021}我一时心软，把巴蒂斯塔对它的假说也给他看了。", "{PART_021}最后还是破例，把巴蒂斯塔对它的假说也给他看了。"),
    ("{PART_046}你在选择词语时表达了太多。", "{PART_046}你通过选择词语，表达了许多东西。"),
    ("{PART_059}那颗体积为地球八倍的……", "{PART_059}那颗大小是地球八倍的……"),
    ("{PART_061}你的话表达了很多东西，", "{PART_061}你的话里藏着很多东西，"),
    ("{PART_063}而且，那正是你自己的词语。", "{PART_063}而且，那些是只属于你的词语。"),
    ("{PART_069}人类使用的将是你的词语。", "{PART_069}人类使用的，会是你定下的词语。"),
    ("{PART_088}能和你共事，是我的荣幸。", "{PART_088}能和你共事，既愉快又荣幸。"),
)


def main() -> None:
    source = json.loads(SOURCE.read_text(encoding="utf-8"))
    assert len(source) == 567, len(source)
    assert len(TRANSLATIONS) == len(source), (len(TRANSLATIONS), len(source))
    output = [
        {"text_index": item["text_index"], "translated_text": translated}
        for item, translated in zip(source, TRANSLATIONS, strict=True)
    ]
    long_goodbye = next(item for item in output if item["text_index"] == 1196355219)
    for old, new in LONG_GOODBYE_REPLACEMENTS:
        assert old in long_goodbye["translated_text"], old
        long_goodbye["translated_text"] = long_goodbye["translated_text"].replace(old, new)
    OUTPUT.write_text(json.dumps(output, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    NEWTERMS.write_text("", encoding="utf-8")


if __name__ == "__main__":
    main()

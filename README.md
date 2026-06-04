# 杀戮尖塔咸瑜分队

## 核心部分  
1. 小刻部分:  蜜饼体系与剥壳附魔体系
2. 武者部分:  攻击回血与低血线加攻
3. 伺夜部分:  狼咬buff,有点像毒小刀的机制
4. 杂耍部分(梦见啥设计啥):  隐隐召唤心烛,牙牙根据攻击距离加攻,桑葚对爪牙特攻,鱼生manman响仍旧单防高频低伤,还有一堆

## 卡牌之外的其他改动
1. 替换玩家(暂时为羽毛笔)与部分怪物皮肤(暂时只有豆苗,电弧,交通亭)
2. 玩家音效部分修改(只是部分,换皮肤的怪物音效还没有修改,也许以后会改吧...)  
&ensp; 
## 文件结构
```
├─scenes
│  └─screens
│      └─char_select    # 人物选择场景
├─src                   # 方舟资源,语音与spine动画
│
├─wyu                   
│  ├─images             # 图片资源
│  ├─localization       # 本地化文本
│  └─Scenes             # 场景资源
└─wyuCode               # 代码部分
    ├─Cards             # 卡牌代码
    ├─Character         # 角色代码
    ├─Enchantments      # 附魔代码
    ├─Extensions        # 处理文件路径的地方
    ├─Monsters          # 添加的怪物
    ├─Patch             # 额外脚本
    │  ├─ExFabricator   # Fabricator动画替换
    │  ├─ExGuardbot     # Guardbot动画替换
    │  ├─ExNoisebot     # Noisebot动画替换
    │  └─ExStabbot      # ExStabbot动画替换
    ├─Potions           # 药水代码
    ├─Powers            # 效果代码
    │  └─simple         # 不知道怎么直接添加注释,就直接弄了一个空药水用于注释了()
    └─Relics            # 遗物
```

## 已更换皮肤:
斧头机器人(AXEBOT) -> 交通亭量产型
直飞产卵虫(OVICOPTER) -> 豆苗
机械师(FABRICATOR) -> 电弧


## 待完成事项

卡牌设计想法：  
卡密来一个闪电类的卡牌


换皮肤：  
商人换为可露希尔，包括音效
幽灵鱼换为萌萌香，灵体时加泡泡特效
大螃蟹换成豌豆黄.

**换皮肤后音效与特效未更换**

新敌人:
蒸汽骑士:多段重击
变形者集群:多段锁血
内卫:


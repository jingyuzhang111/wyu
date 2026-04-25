# 杀戮尖塔咸瑜分队

## 核心部分
核心当然是可爱的小刻啦!  
1. 小刻部分:  蜜饼体系与剥壳附魔体系
2. 武者部分:  攻击回血与低血线加攻
3. 伺夜部分:  狼咬buff,有点像毒小刀
4. 杂耍部分(梦见啥设计啥):  隐隐召唤心烛,牙牙根据攻击距离加攻,桑葚对爪牙特攻,鱼生manman响仍旧单防高频低伤,还有一堆


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

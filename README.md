# DamageRuleLoot 伤害规则掉落

- 作者: 羽学
- 出处: Tshock官方QQ群816771079 
- 根据玩家输出百分比决定是否掉落宝藏袋，从伤害统计插件基础上进行二创。

## 更新日志

```
v1.2.2
再次重构《伤怪建表法》，使伤害更接近准确数值
加入对暴击连续统计播报与怯战人数播报

v1.2.1
加入对暴击伤害计数法来归纳玩家的真实伤害
将广告内容放到了Config方便玩家自定义

v1.2.0
重构全部代码，以枳的伤害统计插件作为基础二次开发

对各别分体化的BOSS伤害输出做了特殊处理
美化了输出榜播报内容
加入了额外伤害榜NPC扩展项
加入了惩罚榜与伤害榜的独立开关配置项

v1.1.0
移除了大部分不需要的参数
把《玩家输出表》转换成了字典键值方便参考观看
优化了多BOSS场景下也能判断宝藏袋掉落

v1.0.0
从伤害统计插件基础上进行二创的伤害规则掉落插件
新玩家进服会自动创建【玩家数据表】，如果玩家已经在配置里则会清空【伤害值】
玩家对BOSS的【伤害百分比】超过【领取条件】的百分比才能捡到【物品ID】内的物品

```

## 指令

| 语法                             | 别名  |       权限       |                   说明                   |
| -------------------------------- | :---: | :--------------: | :--------------------------------------: |
| /reload  | 无 |   tshock.cfg.reload    |    重载配置文件    |

---
配置注意事项
---
1.玩家对BOSS的`伤害百分比`超过`领取条件`的百分比才能捡到`宝藏袋`
  
2.`参与伤害榜的非BOSS怪名称`的不会参与惩罚榜播报

3.`惩罚榜`播报只关联有宝藏袋的BOSS

## 配置

```json
{
  "插件开关": true,
  "是否惩罚": true,
  "广告开关": true,
  "广告内容": "[i:3456][C/F2F2C7:插件开发] [C/BFDFEA:by]  羽学 [C/E7A5CC:|] [c/00FFFF:西江小子][i:3459]",
  "伤害榜播报": true,
  "惩罚榜播报": true,
  "领取条件/百分比": 0.15,
  "参与伤害榜的非BOSS怪名称": [
    "冰雪巨人",
    "沙尘精",
    "腐化宝箱怪",
    "猩红宝箱怪",
    "神圣宝箱怪",
    "黑暗魔法师",
    "食人魔",
    "哥布林术士",
    "荷兰飞盗船",
    "恐惧鹦鹉螺",
    "血浆哥布林鲨鱼",
    "血鳗鱼",
    "海盗船长"
  ]
}
```
## 反馈
- 优先发issued -> 共同维护的插件库：https://github.com/UnrealMultiple/TShockPlugin
- 次优先：TShock官方群：816771079
- 大概率看不到但是也可以：国内社区trhub.cn ，bbstr.net , tr.monika.love
﻿using Newtonsoft.Json;
using TShockAPI;

namespace DamageRuleLoot;

public class Configuration
{
    #region 实例变量

    [JsonProperty("插件开关", Order = 0)]
    public bool Enabled { get; set; } = true;

    [JsonProperty("是否惩罚", Order = 1)]
    public bool Enabled2 { get; set; } = true;

    [JsonProperty("广告开关", Order = 2)]
    public bool Enabled3 { get; set; } = true;

    [JsonProperty("广告内容", Order = 2)]
    public string Advertisement { get; set; } = $"[i:3456][C/F2F2C7:插件开发] [C/BFDFEA:by]  羽学 [C/E7A5CC:|] [c/00FFFF:西江小子][i:3459]";

    [JsonProperty("伤害榜播报", Order = 4)]
    public bool Broadcast { get; set; } = true;

    [JsonProperty("惩罚榜播报", Order = 5)]
    public bool Broadcast2 { get; set; } = true;

    [JsonProperty("低于多少不掉宝藏袋", Order = 6)]
    public double Damages { get; set; }

    [JsonProperty("天顶新三王统计为美杜莎伤害榜", Order = 6)]
    public bool MechQueen { get; set; } = true;

    [JsonProperty("忽略计算石巨人头部输出榜伤害", Order = 7)]
    public bool GolemHead { get; set; } = false;

    [JsonProperty("攻击机械吴克四肢造成真实伤害", Order = 8)]
    public bool Prime { get; set; } = true;

    [JsonProperty("攻击鲨鱼龙给猪鲨造成真实伤害", Order = 9)]
    public bool Sharkron { get; set; } = true;

    [JsonProperty("攻击小鬼与饿鬼给肉山造成真伤(仅FTW与天顶)", Order = 9)]
    public bool FireImp { get; set; } = true;

    [JsonProperty("参与伤害榜的非BOSS怪名称", Order = 10)]
    public string[] Expand { get; set; } = new string[] { "冰雪巨人", "沙尘精", "腐化宝箱怪", "猩红宝箱怪", "神圣宝箱怪", "黑暗魔法师", "食人魔", "哥布林术士", "荷兰飞盗船", "恐惧鹦鹉螺", "血浆哥布林鲨鱼", "血鳗鱼", "海盗船长","火星飞碟" };

    [JsonProperty("监控暴击次数", Order = 11)]
    public bool CritInfo { get; set; } = false;

    [JsonProperty("监控转移伤害", Order = 12)]
    public bool TransferInfo { get; set; } = false;

    [JsonProperty("自定义转移伤害", Order = 13)]
    public bool CustomTransfer { get; set; } = true;

    [JsonProperty("自定义转移伤害表", Order = 14)]
    public List<ItemData> TList { get; set; } = new List<ItemData>();

    public Configuration()
    {
        #if DEBUG

        CritInfo = false;
        TransferInfo = true;
        Damages = 0.5;

        #else

        CritInfo = false;
        TransferInfo = false;
        Damages = 0.15;

        #endif
    }
    #endregion

    #region 数据结构
    public class ItemData
    {
        [JsonProperty("输出怪物", Order = 0)]
        public int NPCA { get; set; }

        [JsonProperty("生命停转", Order = 1)]
        public int LifeLimit { get; set; }

        [JsonProperty("涵盖暴击", Order = 2)]
        public bool Crit { get; set; }

        [JsonProperty("输入怪物", Order = 3)]
        public int[] NPCB { get; set; }


        public ItemData(int npcA, bool crit,int life, int[] npcB)
        {
            NPCA = npcA != 0 ? npcA : 4;
            Crit = crit ? crit : false;
            LifeLimit = life != 0 ? life : 1000;
            NPCB = npcB ?? new int[] { 5 };
        }
    }

    //预设参数
    public void Ints()
    {
        TList = new List<ItemData>
        {
            new ItemData(4,false, 600, new int[]{ 5 }),
            new ItemData(50,false, 800, new int[]{ 1,535 }),
            new ItemData(262, true, 10000, new int[]{ 264 })
        };
    }
    #endregion

    #region 读取与创建配置文件方法
    public static readonly string FilePath = Path.Combine(TShock.SavePath, "伤害规则掉落.json");

    public void Write()
    {
        string json = JsonConvert.SerializeObject(this, Formatting.Indented);
        File.WriteAllText(FilePath, json);
    }

    public static Configuration Read()
    {
        if (!File.Exists(FilePath))
        {
            var NewConfig = new Configuration();
            NewConfig.Ints();
            new Configuration().Write();
            return NewConfig;
        }
        else
        {
            string jsonContent = File.ReadAllText(FilePath);
            return JsonConvert.DeserializeObject<Configuration>(jsonContent)!;
        }
    }
    #endregion
}

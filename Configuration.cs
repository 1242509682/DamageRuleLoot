using Newtonsoft.Json;
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
    
    [JsonProperty("暴击监控/会刷屏", Order = 3)]
    public bool Enabled4 { get; set; } = false; 

    [JsonProperty("伤害榜播报", Order = 4)]
    public bool Broadcast { get; set; } = true;

    [JsonProperty("惩罚榜播报", Order = 5)]
    public bool Broadcast2 { get; set; } = true;

    [JsonProperty("领取条件/百分比", Order = 6)]
    public double Damages { get; set; }

    [JsonProperty("美杜莎判定", Order = 7)]
    public bool MechQueen { get; set; } = true;

    [JsonProperty("参与伤害榜的非BOSS怪名称", Order = 8)]
    public string[] Expand { get; set; } = new string[] { "冰雪巨人", "沙尘精", "腐化宝箱怪", "猩红宝箱怪", "神圣宝箱怪", "黑暗魔法师", "食人魔", "哥布林术士", "荷兰飞盗船", "恐惧鹦鹉螺", "血浆哥布林鲨鱼", "血鳗鱼", "海盗船长","火星飞碟" };

    public Configuration()
    {
        #if DEBUG

        Enabled4 = true;
        Damages = 0.5;

        #else

        Enabled4 = false;
        Damages = 0.15;

        #endif
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

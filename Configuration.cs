using Newtonsoft.Json;
using TShockAPI;

namespace Plugin
{
    internal class Configuration
    {
        #region 实例变量
        [JsonProperty("插件开关", Order = -8)]
        public bool Enabled { get; set; } = true;

        [JsonProperty("领取条件/百分比", Order = -8)]
        public double Damages { get; set; } = 0.3;

        [JsonProperty("伤害统计播报", Order = -8)]
        public bool Broadcast { get; set; } = true;

        [JsonProperty("物品ID", Order = -2)]
        public int[] ItemID { get; set; } = new int[] { 3318,3319,3320,3321,3322,3323,3324,3325,3326,3327,3328,3329,3330,3331,3332,3860,4782,4957,5111};

        [JsonProperty("玩家数据表", Order = 4)]
        public List<ItemData> Items { get; set; } = new List<ItemData>();
        #endregion

        #region 预设参数方法
        public void Ints()
        {
            Items = new List<ItemData>
            {
                new ItemData("羽学",true,0,0)
            };
        }
        #endregion

        #region 数据结构
        public class ItemData
        {
            [JsonProperty("玩家名字", Order = 1)]
            public string Name { get; set; }
            [JsonProperty("领取条件", Order = 2)]
            public bool Enabled { get; set; }
            [JsonProperty("伤害值", Order = 3)]
            public double Damage { get; set; }
            [JsonProperty("伤害百分比", Order = 4)]
            public double Damage2 { get; set; }

            public ItemData(string name = "", bool enabled = false, double damage = 0, double damage2 = 0)
            {
                Name = name ?? "";
                Enabled = enabled;
                Damage = damage;
                Damage2 = damage2;  
            }
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
}
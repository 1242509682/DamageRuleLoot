using Newtonsoft.Json;
using TShockAPI;

namespace Plugin
{
    internal class Configuration
    {
        #region 实例变量
        [JsonProperty("插件开关", Order = 1)]
        public bool Enabled { get; set; } = true;

        [JsonProperty("伤害统计播报", Order = 2)]
        public bool Broadcast { get; set; } = true;

        [JsonProperty("领取条件/百分比", Order = 3)]
        public double Damages { get; set; } = 0.15;

        [JsonProperty("玩家输出表", Order = 4)]
        [JsonConverter(typeof(DataConverter))]
        public List<ItemData> Items { get; set; } = new List<ItemData>();
        #endregion

        #region 数据结构
        public class ItemData
        {
            public string Name { get; set; }
            public double Damage { get; set; }
            public ItemData(string name = "", double damage = 0)
            {
                Name = name ?? "";
                Damage = damage;
            }
        }
        #endregion

        #region 键值转换器
        public class DataConverter : JsonConverter<List<ItemData>>
        {
            public override void WriteJson(JsonWriter writer, List<ItemData> value, JsonSerializer serializer)
            {
                var StageDict = value.ToDictionary(item => item.Name, item => item.Damage);
                serializer.Serialize(writer, StageDict);
            }

            public override List<ItemData> ReadJson(JsonReader reader, Type objectType, List<ItemData> existingValue, bool hasExistingValue, JsonSerializer serializer)
            {
                var LevelDict = serializer.Deserialize<Dictionary<string, double>>(reader);
                return LevelDict?.Select(kv => new ItemData(kv.Key, kv.Value)).ToList() ?? new List<ItemData>();
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
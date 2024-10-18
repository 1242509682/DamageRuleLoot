using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;
using static DamageRuleLoot.StrikeNPC;
using static DamageRuleLoot.Tool;

namespace DamageRuleLoot;

[ApiVersion(2, 1)]
public class DamageRuleLoot : TerrariaPlugin
{

    #region 插件信息
    public override string Name => "伤害规则掉落";
    public override string Author => "羽学 西江小子";
    public override Version Version => new Version(1, 2, 2);
    public override string Description => "涡轮增压不蒸鸭";
    #endregion

    #region 注册与释放
    public DamageRuleLoot(Main game) : base(game) { }
    private GeneralHooks.ReloadEventD _reloadHandler;
    internal static StrikeNPC Strike = new();
    public override void Initialize()
    {
        LoadConfig();
        this._reloadHandler = (_) => LoadConfig();
        GeneralHooks.ReloadEvent += this._reloadHandler;
        ServerApi.Hooks.NpcKilled.Register(this, this.OnNpcKill);
        On.Terraria.NPC.StrikeNPC += OnStrikeNPC;
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            GeneralHooks.ReloadEvent -= this._reloadHandler;
            ServerApi.Hooks.NpcKilled.Deregister(this, this.OnNpcKill);
            On.Terraria.NPC.StrikeNPC -= OnStrikeNPC;
        }
        base.Dispose(disposing);
    }
    #endregion

    #region 配置重载读取与写入方法
    internal static Configuration Config = new();
    private static void LoadConfig()
    {
        Config = Configuration.Read();
        Config.Write();
        TShock.Log.ConsoleInfo("[伤害规则掉落]重新加载配置完毕。");
    }
    #endregion

    #region 伤怪建表法+暴击计数法
    private double OnStrikeNPC(On.Terraria.NPC.orig_StrikeNPC orig, NPC self, int Damage, float knockBack, int hitDirection, bool crit, bool noEffect, bool fromNet, Entity entity)
    {
        var NotUsed = orig(self, Damage, knockBack, hitDirection, crit, noEffect, fromNet, entity);

        StrikeNPC? strike = StrikeNPC.strikeNPC.Find(x => x.npcIndex == self.whoAmI && x.npcID == self.netID);

        if (fromNet && entity is Player plr)
        {
            if (strike != null && strike.npcName != string.Empty)
            {
                if (strike.PlayerOrDamage.ContainsKey(plr.name))
                {
                    if (crit)
                    {
                        strike.PlayerOrDamage[plr.name] += Damage;
                        strike.AllDamage += Damage;

                        #if DEBUG
                        TShock.Utils.Broadcast($"[c/FBF069:【暴击】] 玩家:[c/F06576:{plr.name}] " +
                            $"对象:[c/AEA3E4:{self.FullName}] 满血:[c/FBF069:{self.lifeMax}] " +
                            $"血量:[c/6DDA6D:{self.life}] 伤害:[c/F06576:{strike.AllDamage}] 暴击数:[c/FBF069:{CritTracker.GetCritCount(plr.name)}]", 202, 221, 222);
                        #endif

                        CritTracker.UpdateCritCount(plr.name);
                    }
                    strike.PlayerOrDamage[plr.name] += Damage;
                    strike.AllDamage += Damage;
                }
                else
                {
                    strike.PlayerOrDamage.Add(plr.name, Damage);
                    strike.AllDamage += Damage;
                }
            }
            else
            {
                StrikeNPC snpc = new StrikeNPC()
                {
                    npcID = self.netID,
                    npcIndex = self.whoAmI,
                    npcName = self.FullName,
                };
                snpc.PlayerOrDamage.Add(plr.name, Damage);
                snpc.AllDamage += Damage;
                StrikeNPC.strikeNPC.Add(snpc);
            }
        }
        return NotUsed;
    }
    #endregion

    #region 对各BOSS伤害特殊处理后播报
    public static Dictionary<string, double> Destroyer = new Dictionary<string, double>(); //毁灭者
    public static Dictionary<string, double> FleshWall = new Dictionary<string, double>(); //肉山
    public static Dictionary<string, double> Eaterworld = new Dictionary<string, double>(); //世吞
    public static Dictionary<string, double> Retinazer = new Dictionary<string, double>(); // 激光眼
    public static Dictionary<string, double> Spazmatism = new Dictionary<string, double>(); // 魔焰眼
    private void OnNpcKill(NpcKilledEventArgs args)
    {
        StrikeNPC strike = StrikeNPC.strikeNPC.Find(x => x.npcIndex == args.npc.whoAmI && x.npcID == args.npc.netID)!;
        if (!Config.Enabled || strike == null || !strike.PlayerOrDamage.Any()) return;

        //毁灭者的处理
        if (args.npc.netID == 134)
        {
            foreach (var sss in strikeNPC)
            {
                if (sss.npcID == 134 || sss.npcID == 135 || sss.npcID == 136)
                {
                    foreach (var ss in sss.PlayerOrDamage)
                        UpdateDict(Destroyer, ss.Key, ss.Value);
                }
            }
            double sum = 0;
            foreach (var des in Destroyer)
            {
                sum += des.Value;
            }
            SendKillMessage(args.npc.FullName, Destroyer, sum);
            Destroyer.Clear();
            strikeNPC.RemoveAll(x => x.npcID == 134 || x.npcID == 136 || x.npcID == 135 || x.npcID != Main.npc[x.npcIndex].netID || !Main.npc[x.npcIndex].active);
            return;
        }

        //肉山和嘴巴
        else if (args.npc.netID == 113)
        {
            foreach (var sss in strikeNPC)
            {
                if (sss.npcID == 113 || sss.npcID == 114)
                {
                    foreach (var ss in sss.PlayerOrDamage)
                        UpdateDict(FleshWall, ss.Key, ss.Value);
                }
            }
            double sum = 0;
            foreach (var fw in FleshWall)
            {
                sum += fw.Value;
            }
            SendKillMessage("血肉墙", FleshWall, sum);
            FleshWall.Clear();
            strikeNPC.RemoveAll(x => x.npcID == 113 || x.npcID == 114 || x.npcID != Main.npc[x.npcIndex].netID || !Main.npc[x.npcIndex].active);
            return;
        }

        // 双子魔眼
        else if (args.npc.netID == 125 || args.npc.netID == 126)
        {
            foreach (var sss in strikeNPC)
            {
                if ((args.npc.netID == 125 && (sss.npcID == 125 || sss.npcID == 126)) || (args.npc.netID == 126 && (sss.npcID == 125 || sss.npcID == 126)))
                {
                    foreach (var ss in sss.PlayerOrDamage)
                    {
                        if (args.npc.netID == 125)
                            UpdateDict(Retinazer, ss.Key, ss.Value);

                        else if (args.npc.netID == 126)
                            UpdateDict(Spazmatism, ss.Key, ss.Value);
                    }
                }
            }

            if (args.npc.netID == 125 && Spazmatism.Count > 0)
            {
                SendKillMessage("双子魔眼", CombineDamages(Retinazer, Spazmatism), GetCombineDamages(CombineDamages(Retinazer, Spazmatism)));
                ClearDictionaries(Retinazer, Spazmatism);
            }
            else if (args.npc.netID == 126 && Retinazer.Count > 0)
            {
                SendKillMessage("双子魔眼", CombineDamages(Retinazer, Spazmatism), GetCombineDamages(CombineDamages(Retinazer, Spazmatism)));
                ClearDictionaries(Retinazer, Spazmatism);
            }
            strikeNPC.RemoveAll(x => x.npcID == 125 || x.npcID == 126 || x.npcID != Main.npc[x.npcIndex].netID || !Main.npc[x.npcIndex].active);
            return;
        }

        //其他生物，对被击杀的生物进行计数
        for (int i = 0; i < strikeNPC.Count; i++)
        {
            if (strikeNPC[i].npcIndex == args.npc.whoAmI && strikeNPC[i].npcID == args.npc.netID)
            {
                switch (strikeNPC[i].npcID)
                {
                    //黑长直
                    case 13:
                    case 14:
                    case 15:
                        {
                            bool flag = true;
                            foreach (var n in Main.npc)
                            {
                                if (n.whoAmI != args.npc.whoAmI && (n.type == 13 || n.type == 14 || n.type == 15) && n.active)
                                {
                                    flag = false; break;
                                }
                            }
                            foreach (var ss in strikeNPC[i].PlayerOrDamage)
                            {
                                UpdateDict(Eaterworld, ss.Key, ss.Value);
                            }
                            if (flag)
                            {
                                double sum = 0;
                                foreach (var eater in Eaterworld)
                                {
                                    sum += eater.Value;
                                }
                                SendKillMessage(args.npc.FullName, Eaterworld, sum);
                                strikeNPC.RemoveAll(x => x.npcID == 13 || x.npcID == 14 || x.npcID == 15 || x.npcID != Main.npc[x.npcIndex].netID || !Main.npc[x.npcIndex].active);
                                Eaterworld.Clear();
                                return;
                            }
                        }
                        break;

                    //荷兰飞船的处理，特殊点：本体不可被击中，在其他炮塔全死亡后计入击杀
                    case 492:
                        {
                            bool flag = true;
                            int index = -1;
                            foreach (var n in Main.npc)
                            {
                                if (n.whoAmI != args.npc.whoAmI && n.type == 492 && n.active)
                                {
                                    flag = false;
                                }
                                if (n.netID == 491)
                                {
                                    index = n.whoAmI;
                                }
                            }
                            if (index >= 0)
                            {
                                StrikeNPC? st = strikeNPC.Find(x => x.npcID == 491);
                                if (st == null)
                                {
                                    strikeNPC.Add(new StrikeNPC(index, 491, Lang.GetNPCNameValue(491), strikeNPC[i].PlayerOrDamage, strikeNPC[i].AllDamage, 80000f));
                                }
                                else
                                {
                                    foreach (var y in strikeNPC[i].PlayerOrDamage)
                                    {
                                        if (st.PlayerOrDamage.ContainsKey(y.Key))
                                        {
                                            st.PlayerOrDamage[y.Key] += y.Value;
                                            st.AllDamage += y.Value;
                                        }
                                        else
                                        {
                                            st.PlayerOrDamage.Add(y.Key, y.Value);
                                            st.AllDamage += y.Value;
                                        }
                                    }
                                }
                            }
                            if (flag)
                            {
                                StrikeNPC? airship = strikeNPC.Find(x => x.npcID == 491);
                                if (airship == null)
                                {
                                    strikeNPC.RemoveAll(x => x.npcID == 491 || x.npcID == 492 || x.npcID != Main.npc[x.npcIndex].netID || !Main.npc[x.npcIndex].active);
                                    return;
                                }
                                SendKillMessage(airship.npcName, airship.PlayerOrDamage, airship.AllDamage);
                                strikeNPC.RemoveAll(x => x.npcID == 491 || x.npcID == 492 || x.npcID != Main.npc[x.npcIndex].netID || !Main.npc[x.npcIndex].active);
                                return;
                            }
                        }
                        break;

                    //月球领主的处理，特殊点，本体可被击中，但肢体会假死，击中肢体也应该算入本体中
                    case 398:
                        {
                            List<StrikeNPC> strikenpcs = strikeNPC.FindAll(x => x.npcID == 397 || x.npcID == 396);
                            if (strikenpcs.Count > 0)
                            {
                                foreach (var v in strikenpcs)
                                {
                                    foreach (var vv in v.PlayerOrDamage)
                                    {
                                        if (strikeNPC[i].PlayerOrDamage.ContainsKey(vv.Key))
                                        {
                                            strikeNPC[i].PlayerOrDamage[vv.Key] += vv.Value;
                                            strikeNPC[i].AllDamage += vv.Value;
                                        }
                                        else
                                        {
                                            strikeNPC[i].PlayerOrDamage.Add(vv.Key, vv.Value);
                                            strikeNPC[i].AllDamage += vv.Value;
                                        }
                                    }
                                }
                            }
                            SendKillMessage("月亮领主", strikeNPC[i].PlayerOrDamage, strikeNPC[i].AllDamage);
                            strikeNPC.RemoveAll(x => x.npcID == 398 || x.npcID == 397 || x.npcID == 396 || x.npcID != Main.npc[x.npcIndex].netID || !Main.npc[x.npcIndex].active);
                            return;
                        }

                    //机械骷髅王的处理，特殊点，本体可能被击中，其他肢体可能会死
                    case 127:
                    case 128:
                    case 129:
                    case 130:
                    case 131:
                        {
                            StrikeNPC? strike2 = strikeNPC.Find(x => x.npcID == 127);
                            if (strike2 == null)
                            {
                                int index = -1;
                                foreach (var n in Main.npc)
                                {
                                    if (n.netID == 127)
                                    {
                                        index = n.whoAmI;
                                    }
                                }
                                if (index == -1)
                                {
                                    strikeNPC.RemoveAll(x => x.npcID == 127 || x.npcID == 128 || x.npcID == 129 || x.npcID == 130 || x.npcID == 131 || x.npcID != Main.npc[x.npcIndex].netID || !Main.npc[x.npcIndex].active);
                                    return;
                                }
                                strike2 = new StrikeNPC(index, 127, Main.npc[index].FullName, strikeNPC[i].PlayerOrDamage, strikeNPC[i].AllDamage, 300000);
                                strikeNPC.Add(strike2);
                            }
                            //把肢体受伤计算加入到本体头部中
                            else if (strikeNPC[i].npcID != 127)
                            {
                                foreach (var v in strikeNPC[i].PlayerOrDamage)
                                {
                                    if (strike2.PlayerOrDamage.ContainsKey(v.Key))
                                    {
                                        strike2.PlayerOrDamage[v.Key] += v.Value;
                                        strike2.AllDamage += v.Value;
                                    }
                                    else
                                    {
                                        strike2.PlayerOrDamage.Add(v.Key, v.Value);
                                        strike2.AllDamage += v.Value;
                                    }
                                }
                            }
                            if (strikeNPC[i].npcID == 127)
                            {
                                SendKillMessage(args.npc.FullName, strikeNPC[i].PlayerOrDamage, strikeNPC[i].AllDamage);
                                strikeNPC.RemoveAll(x => x.npcID == 127 || x.npcID == 128 || x.npcID == 129 || x.npcID == 130 || x.npcID == 131 || x.npcID != Main.npc[x.npcIndex].netID || !Main.npc[x.npcIndex].active);
                                return;
                            }
                        }
                        break;

                    //骷髅王的处理
                    case 35:
                    case 36:
                        {
                            StrikeNPC? strike5 = strikeNPC.Find(x => x.npcID == 35);
                            if (strike5 == null)
                            {
                                int index = -1;
                                foreach (var n in Main.npc)
                                {
                                    if (n.netID == 35)
                                    {
                                        index = n.whoAmI;
                                    }
                                }
                                if (index == -1)
                                {
                                    strikeNPC.RemoveAll(x => x.npcID == 35 || x.npcID == 36 || x.npcID != Main.npc[x.npcIndex].netID || !Main.npc[x.npcIndex].active);
                                    return;
                                }
                                strike5 = new StrikeNPC(index, 35, Main.npc[index].FullName, strikeNPC[i].PlayerOrDamage, strikeNPC[i].AllDamage, 16000);
                                strikeNPC.Add(strike5);
                            }
                            //把肢体受伤计算加入到本体头部中
                            else if (strikeNPC[i].npcID != 35)
                            {
                                foreach (var v in strikeNPC[i].PlayerOrDamage)
                                {
                                    if (strike5.PlayerOrDamage.ContainsKey(v.Key))
                                    {
                                        strike5.PlayerOrDamage[v.Key] += v.Value;
                                        strike5.AllDamage += v.Value;
                                    }
                                    else
                                    {
                                        strike5.PlayerOrDamage.Add(v.Key, v.Value);
                                        strike5.AllDamage += v.Value;
                                    }
                                }
                            }
                            if (strikeNPC[i].npcID == 35)
                            {
                                SendKillMessage(args.npc.FullName, strikeNPC[i].PlayerOrDamage, strikeNPC[i].AllDamage);
                                strikeNPC.RemoveAll(x => x.npcID == 35 || x.npcID == 36 || x.npcID != Main.npc[x.npcIndex].netID || !Main.npc[x.npcIndex].active);
                                return;
                            }
                        }
                        break;

                    //石巨人的特殊处理
                    case 245:
                    case 246:
                    case 247:
                    case 248:
                        {
                            StrikeNPC? strike3 = strikeNPC.Find(x => x.npcID == 245);
                            if (strike3 == null)
                            {
                                int index = -1;
                                foreach (var n in Main.npc)
                                {
                                    if (n.netID == 245)
                                    {
                                        index = n.whoAmI;
                                    }
                                }
                                if (index == -1)
                                {
                                    strikeNPC.RemoveAll(x => x.npcID == 245 || x.npcID == 246 || x.npcID == 247 || x.npcID == 248 || x.npcID != Main.npc[x.npcIndex].netID || !Main.npc[x.npcIndex].active);
                                    return;
                                }
                                strike3 = new StrikeNPC(index, 245, Main.npc[index].FullName, strikeNPC[i].PlayerOrDamage, strikeNPC[i].AllDamage, 400000);
                                strikeNPC.Add(strike3);
                            }
                            //把除了本体以外的肢体的伤害计算加到本体上
                            else if (strikeNPC[i].npcID != 245)
                            {
                                foreach (var v in strikeNPC[i].PlayerOrDamage)
                                {
                                    if (strike3.PlayerOrDamage.ContainsKey(v.Key))
                                    {
                                        strike3.PlayerOrDamage[v.Key] += v.Value;
                                        strike3.AllDamage += v.Value;
                                    }
                                    else
                                    {
                                        strike3.PlayerOrDamage.Add(v.Key, v.Value);
                                        strike3.AllDamage += v.Value;
                                    }
                                }
                            }
                            if (strikeNPC[i].npcID == 245)
                            {
                                SendKillMessage(args.npc.FullName, strikeNPC[i].PlayerOrDamage, strikeNPC[i].AllDamage);
                                strikeNPC.RemoveAll(x => x.npcID == 245 || x.npcID == 246 || x.npcID == 247 || x.npcID == 248 || x.npcID != Main.npc[x.npcIndex].netID || !Main.npc[x.npcIndex].active);
                                return;
                            }
                        }
                        break;

                    //克苏鲁之脑的特殊处理
                    case 266:
                    case 267:
                        {
                            StrikeNPC? strike4 = strikeNPC.Find(x => x.npcID == 266);
                            if (strike4 == null)
                            {
                                int index = -1;
                                foreach (var n in Main.npc)
                                {
                                    if (n.netID == 266)
                                    {
                                        index = n.whoAmI;
                                    }
                                }
                                if (index == -1)//不可能发生这种情况
                                {
                                    strikeNPC.RemoveAll(x => x.npcID == 266 || x.npcID == 267 || x.npcID != Main.npc[x.npcIndex].netID || !Main.npc[x.npcIndex].active);
                                    return;
                                }
                                strike4 = new StrikeNPC(index, 266, Main.npc[index].FullName, strikeNPC[i].PlayerOrDamage, strikeNPC[i].AllDamage, 125000);
                                strikeNPC.Add(strike4);
                            }
                            //把除了本体以外的飞眼怪的伤害计算加到本体上
                            else if (strikeNPC[i].npcID != 266)
                            {
                                foreach (var v in strikeNPC[i].PlayerOrDamage)
                                {
                                    if (strike4.PlayerOrDamage.ContainsKey(v.Key))
                                    {
                                        strike4.PlayerOrDamage[v.Key] += v.Value;
                                        strike4.AllDamage += v.Value;
                                    }
                                    else
                                    {
                                        strike4.PlayerOrDamage.Add(v.Key, v.Value);
                                        strike4.AllDamage += v.Value;
                                    }
                                }
                            }
                            if (strikeNPC[i].npcID == 266)
                            {
                                SendKillMessage(args.npc.FullName, strikeNPC[i].PlayerOrDamage, strikeNPC[i].AllDamage);
                                strikeNPC.RemoveAll(x => x.npcID == 266 || x.npcID == 267 || x.npcID != Main.npc[x.npcIndex].netID || !Main.npc[x.npcIndex].active);
                                return;
                            }
                        }
                        break;
                    default:
                        {
                            if (args.npc.boss || args.npc.netID == 551 || args.npc.netID == 668 || Config.Expand.Contains(args.npc.FullName))
                            {
                                SendKillMessage(args.npc.FullName, strikeNPC[i].PlayerOrDamage, strikeNPC[i].AllDamage);
                            }
                            strikeNPC.RemoveAt(i);
                            strikeNPC.RemoveAll(x => x.npcID != Main.npc[x.npcIndex].netID || !Main.npc[x.npcIndex].active);
                            return;
                        }
                }
            }

            if (i >= 0 && (strikeNPC[i].npcID != Main.npc[strikeNPC[i].npcIndex].netID || !Main.npc[strikeNPC[i].npcIndex].active))
            {
                strikeNPC.RemoveAt(i);
                i--;
            }
        }
    }
    #endregion

}
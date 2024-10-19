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
    public override Version Version => new Version(1, 3, 0);
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
        ServerApi.Hooks.NpcKilled.Register(this, this.OnMechQueen);
        On.Terraria.NPC.StrikeNPC += OnStrikeNPC;
        On.Terraria.NPC.StrikeNPC += AddDamage;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            GeneralHooks.ReloadEvent -= this._reloadHandler;
            ServerApi.Hooks.NpcKilled.Deregister(this, this.OnNpcKill);
            ServerApi.Hooks.NpcKilled.Deregister(this, this.OnMechQueen);
            On.Terraria.NPC.StrikeNPC -= OnStrikeNPC;
            On.Terraria.NPC.StrikeNPC -= AddDamage;
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
        var damage = orig(self, Damage, knockBack, hitDirection, crit, noEffect, fromNet, entity);
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

                        if (Config.CritInfo)
                        {
                            TShock.Utils.Broadcast($"[c/FBF069:【暴击】] 玩家:[c/F06576:{plr.name}] " +
                                $"对象:[c/AEA3E4:{self.FullName}] 满血:[c/FBF069:{self.lifeMax}] " +
                                $"血量:[c/6DDA6D:{self.life}] 伤害:[c/F06576:{damage}] " +
                                $"暴击数:[c/FBF069:{CritTracker.GetCritCount(plr.name)}]", 202, 221, 222);
                        }

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

            //不是城镇npc 雕像怪 假人才创建数据
            else if (!self.townNPC || !self.SpawnedFromStatue || self.netID != 488)
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
        return damage;
    }

    #endregion

    #region 打怪伤BOSS法
    private double AddDamage(On.Terraria.NPC.orig_StrikeNPC orig, NPC self, int Damage, float knockBack, int hitDirection, bool crit, bool noEffect, bool fromNet, Entity entity)
    {
        var damage = orig(self, Damage, knockBack, hitDirection, crit, noEffect, fromNet, entity);
        if (fromNet && entity is Player plr)
        {
            //不是雕像怪
            if (!self.SpawnedFromStatue)
            {
                //判定为鲨鱼龙
                if (Config.Sharkron && self.netID == 372 || self.netID == 373)
                {
                    //获取猪鲨id
                    StrikeNPC? strike = StrikeNPC.strikeNPC.Find(x => x.npcID == 370);
                    if (strike != null)
                    {
                        if (Damage > 0 && Main.npc[strike.npcIndex].life > 20000)
                        {
                            //猪鲨还活着
                            if (Main.npc[strike.npcIndex].active)
                            {
                                //对鲨鱼龙造成的伤害减到猪鲨身上，然后更新
                                Main.npc[strike.npcIndex].life -= Damage;
                                Main.npc[strike.npcIndex].netUpdate = true;

                                // 更新猪鲨的伤害记录
                                if (strike.PlayerOrDamage.ContainsKey(plr.name))
                                {
                                    strike.PlayerOrDamage[plr.name] += Damage;
                                    strike.AllDamage += Damage;
                                }
                            }

                            if (Config.TransferInfo)
                            {
                                if (Main.npc[strike.npcIndex].life > 20000)
                                {
                                    TShock.Utils.Broadcast($"[c/FBF069:【转移】] 玩家:[c/F06576:{plr.name}] " +
                                        $"攻击对象:[c/AEA3E4:{self.FullName}] | " +
                                        $"转移:[c/6DDA6D:{strike.npcName}] 伤害:[c/F06576:{Damage}] " +
                                        $"生命:[c/FBF069:{Main.npc[strike.npcIndex].life}]", 202, 221, 222);
                                }

                                if (Main.npc[strike.npcIndex].life <= 20000)
                                {
                                    TShock.Utils.Broadcast($"[c/F06576:【停转】] 玩家:[c/F06576:{plr.name}] " +
                                        $"转伤对象:[c/AEA3E4:{strike.npcName}] | 生命值:[c/6DDA6D:{Main.npc[strike.npcIndex].life}] < " +
                                        $"[c/F06576:{20000}]", 202, 221, 222);
                                }
                            }
                        }
                    }
                }

                //判定为FTW和天顶世界的火焰小鬼与饿鬼
                if (Config.FireImp && (Main.getGoodWorld || Main.zenithWorld) &&
                self.netID == 24 || self.netID == 115 || self.netID == 116)
                {
                    //获取肉山id
                    StrikeNPC? strike = StrikeNPC.strikeNPC.Find(x => x.npcID == 113);
                    if (strike != null)
                    {
                        if (Damage > 0 && Main.npc[strike.npcIndex].life > 1000)
                        {
                            if (Main.npc[strike.npcIndex].active)
                            {
                                //对小鬼和饿鬼造成的伤害减到肉山身上，然后更新
                                Main.npc[strike.npcIndex].life -= Damage;
                                Main.npc[strike.npcIndex].netUpdate = true;

                                // 更新肉山的伤害记录
                                if (strike.PlayerOrDamage.ContainsKey(plr.name))
                                {
                                    strike.PlayerOrDamage[plr.name] += Damage;
                                    strike.AllDamage += Damage;
                                }
                            }

                            if (Config.TransferInfo)
                            {
                                if (Main.npc[strike.npcIndex].life > 1000)
                                {
                                    TShock.Utils.Broadcast($"[c/FBF069:【转移】] 玩家:[c/F06576:{plr.name}] " +
                                        $"攻击对象:[c/AEA3E4:{self.FullName}] | " +
                                        $"转移:[c/6DDA6D:{strike.npcName}] 伤害:[c/F06576:{Damage}] " +
                                        $"生命:[c/FBF069:{Main.npc[strike.npcIndex].life}]", 202, 221, 222);
                                }

                                if (Main.npc[strike.npcIndex].life <= 1000)
                                {
                                    TShock.Utils.Broadcast($"[c/F06576:【停转】] 玩家:[c/F06576:{plr.name}] " +
                                        $"转伤对象:[c/AEA3E4:{strike.npcName}] | 生命值:[c/6DDA6D:{Main.npc[strike.npcIndex].life}] < " +
                                        $"[c/F06576:{1000}]", 202, 221, 222);
                                }
                            }
                        }
                    }
                }

                //判定为机械骷髅王四肢
                if (Config.Prime &&
                    self.netID == 128 || self.netID == 129 ||
                    self.netID == 130 || self.netID == 131)
                {
                    //获取机械骷髅王头部id
                    StrikeNPC? strike = StrikeNPC.strikeNPC.Find(x => x.npcID == 127);
                    if (strike != null)
                    {
                        //设置一个转移伤害的生命条件上限，防止虚标伤害把NPC直接抹除，不掉任何东西
                        if (Damage > 0 && Main.npc[strike.npcIndex].life > 1000)
                        {
                            if (Main.npc[strike.npcIndex].active)
                            {
                                //对四肢造成的伤害减到头部上，然后更新
                                Main.npc[strike.npcIndex].life -= Damage;
                                Main.npc[strike.npcIndex].netUpdate = true;

                                // 更新机械骷髅王头部的伤害记录
                                if (strike.PlayerOrDamage.ContainsKey(plr.name))
                                {
                                    strike.PlayerOrDamage[plr.name] += Damage;
                                    strike.AllDamage += Damage;
                                }
                            }

                            if (Config.TransferInfo)
                            {
                                if (Main.npc[strike.npcIndex].life > 1000)
                                {
                                    TShock.Utils.Broadcast($"[c/FBF069:【转移】] 玩家:[c/F06576:{plr.name}] " +
                                        $"攻击对象:[c/AEA3E4:{self.FullName}] | " +
                                        $"转移:[c/6DDA6D:{strike.npcName}] 伤害:[c/F06576:{Damage}] " +
                                        $"生命:[c/FBF069:{Main.npc[strike.npcIndex].life}]", 202, 221, 222);
                                }

                                if (Main.npc[strike.npcIndex].life <= 1000)
                                {
                                    TShock.Utils.Broadcast($"[c/F06576:【停转】] 玩家:[c/F06576:{plr.name}] " +
                                        $"转伤对象:[c/AEA3E4:{strike.npcName}] | 生命值:[c/6DDA6D:{Main.npc[strike.npcIndex].life}] < " +
                                        $"[c/F06576:{1000}]", 202, 221, 222);
                                }
                            }
                        }
                    }
                }

                //判定自定义
                if (Config.CustomTransfer)
                {
                    foreach (var Custom in Config.TList)
                    {
                        foreach (var B in Custom.NPCB)
                        {
                            if (self.netID == B)
                            {
                                StrikeNPC? strike = StrikeNPC.strikeNPC.Find(x => x.npcID == Custom.NPCA);

                                if (strike == null)
                                {
                                    continue;
                                }

                                // 根据配置项判断是否需要暴击才能转移伤害
                                bool cr = Custom.Crit || (!Custom.Crit && !crit);

                                if (Damage > 0 && Main.npc[strike.npcIndex].life > Custom.LifeLimit && cr)
                                {
                                    if (Main.npc[strike.npcIndex].active)
                                    {
                                        Main.npc[strike.npcIndex].life -= Damage;
                                        Main.npc[strike.npcIndex].netUpdate = true;

                                        if (strike.PlayerOrDamage.ContainsKey(plr.name))
                                        {
                                            strike.PlayerOrDamage[plr.name] += Damage;
                                            strike.AllDamage += Damage;
                                        }

                                    }

                                    if (Config.TransferInfo)
                                    {
                                        if (Main.npc[strike.npcIndex].life > Custom.LifeLimit)
                                        {
                                            TShock.Utils.Broadcast($"[c/FBF069:【转移】] 玩家:[c/F06576:{plr.name}] " +
                                                $"攻击对象:[c/AEA3E4:{self.FullName}] | " +
                                                $"转移:[c/6DDA6D:{strike.npcName}] 伤害:[c/F06576:{Damage}] " +
                                                $"生命:[c/FBF069:{Main.npc[strike.npcIndex].life}]", 202, 221, 222);
                                        }

                                        if (Main.npc[strike.npcIndex].life <= Custom.LifeLimit)
                                        {
                                            TShock.Utils.Broadcast($"[c/F06576:【停转】] 玩家:[c/F06576:{plr.name}] " +
                                                $"转伤对象:[c/AEA3E4:{strike.npcName}] | 生命值:[c/6DDA6D:{Main.npc[strike.npcIndex].life}] < " +
                                                $"[c/F06576:{Custom.LifeLimit}] ", 202, 221, 222);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

            }
        }
        return damage;
    }
    #endregion

    #region 对各BOSS伤害特殊处理后播报
    public static Dictionary<string, double> Destroyer = new Dictionary<string, double>(); //毁灭者
    public static Dictionary<string, double> FleshWall = new Dictionary<string, double>(); //肉山
    public static Dictionary<string, double> Eaterworld = new Dictionary<string, double>(); //世吞
    public static Dictionary<string, double> Retinazer = new Dictionary<string, double>(); // 激光眼
    public static Dictionary<string, double> Spazmatism = new Dictionary<string, double>(); // 魔焰眼
    public static Dictionary<string, double> CustomDicts = new Dictionary<string, double>(); // 自定义
    private void OnNpcKill(NpcKilledEventArgs args)
    {
        StrikeNPC strike = StrikeNPC.strikeNPC.Find(x => x.npcIndex == args.npc.whoAmI && x.npcID == args.npc.netID)!;

        if (!Config.Enabled || strike == null || !strike.PlayerOrDamage.Any()) return;

        //自定义转移伤害统计
        if (Config.CustomTransfer)
        {
            foreach (var Custom in Config.TList)
            {
                if (args.npc.netID != Custom.NPCA)
                {
                    continue;
                }

                foreach (int B in Custom.NPCB)
                {
                    foreach (var sss in strikeNPC)
                    {
                        if (sss.npcID == Custom.NPCA || sss.npcID == B)
                        {
                            foreach (var ss in strike.PlayerOrDamage)
                            {
                                UpdateDict(CustomDicts, ss.Key, ss.Value);
                            }
                        }
                    }
                }

                double sum = 0;
                foreach (var d in CustomDicts)
                {
                    sum += d.Value;
                }

                SendKillMessage(args.npc.FullName, CustomDicts, sum);
                strikeNPC.RemoveAll(x => x.npcID == Custom.NPCA || Custom.NPCB.Contains(x.npcID) || x.npcID != Main.npc[x.npcIndex].netID || !Main.npc[x.npcIndex].active);
                CustomDicts.Clear();

                return;
            }
        }

        //毁灭者的处理
        if (args.npc.netID == 134)
        {
            if (Main.zenithWorld && Config.MechQueen) return;

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
            strikeNPC.RemoveAll(x => x.npcID == 134 || x.npcID == 136 || x.npcID == 135 ||
            x.npcID != Main.npc[x.npcIndex].netID || !Main.npc[x.npcIndex].active);
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

                //如果是For the worthy或天顶种子，把小鬼和饿鬼的伤害加算到肉山身上（并排除雕像怪）
                else if (Config.FireImp && (Main.getGoodWorld || Main.zenithWorld) && !args.npc.SpawnedFromStatue &&
                    (sss.npcID == 24 || sss.npcID == 115 || sss.npcID == 116))
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
            strikeNPC.RemoveAll(x => x.npcID == 113 || x.npcID == 114 ||
            x.npcID == 24 || x.npcID == 115 || x.npcID == 116 ||
            x.npcID != Main.npc[x.npcIndex].netID || !Main.npc[x.npcIndex].active);
            return;
        }

        // 双子魔眼
        else if (args.npc.netID == 125 || args.npc.netID == 126)
        {
            if (Main.zenithWorld && Config.MechQueen) return;

            foreach (var sss in strikeNPC)
            {
                if ((args.npc.netID == 125 && (sss.npcID == 125 || sss.npcID == 126)) ||
                    (args.npc.netID == 126 && (sss.npcID == 125 || sss.npcID == 126)))
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

            if ((args.npc.netID == 125 && Spazmatism.Count > 0) ||
                (args.npc.netID == 126 && Retinazer.Count > 0))
            {
                SendKillMessage("双子魔眼", CombineDamages(Retinazer, Spazmatism), GetCombineDamages(CombineDamages(Retinazer, Spazmatism)));


                ClearDictionaries(Retinazer, Spazmatism);
            }
            strikeNPC.RemoveAll(x => x.npcID == 125 || x.npcID == 126 ||
            x.npcID != Main.npc[x.npcIndex].netID || !Main.npc[x.npcIndex].active);
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
                                if (n.whoAmI != args.npc.whoAmI && n.active &&
                                   (n.netID == 13 || n.netID == 14 || n.netID == 15))
                                {
                                    flag = false;
                                    break;
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
                                strikeNPC.RemoveAll(x => x.npcID == 13 || x.npcID == 14 || x.npcID == 15 ||
                                x.npcID != Main.npc[x.npcIndex].netID || !Main.npc[x.npcIndex].active);
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
                                    strikeNPC.RemoveAll(x => x.npcID == 491 || x.npcID == 492 ||
                                    x.npcID != Main.npc[x.npcIndex].netID || !Main.npc[x.npcIndex].active);
                                    return;
                                }
                                SendKillMessage(airship.npcName, airship.PlayerOrDamage, airship.AllDamage);
                                strikeNPC.RemoveAll(x => x.npcID == 491 || x.npcID == 492 ||
                                x.npcID != Main.npc[x.npcIndex].netID || !Main.npc[x.npcIndex].active);
                                return;
                            }
                        }
                        break;

                    //火星飞碟的处理，特殊点：本体在炮塔死亡后计入击杀
                    case 392:
                    case 393:
                    case 394:
                    case 395:
                        {
                            StrikeNPC? strike2 = strikeNPC.Find(x => x.npcID == 395);
                            if (strike2 == null)
                            {
                                int index = -1;
                                foreach (var n in Main.npc)
                                {
                                    if (n.netID == 395)
                                    {
                                        index = n.whoAmI;
                                    }
                                }
                                if (index == -1)
                                {
                                    strikeNPC.RemoveAll(x => x.npcID == 392 || x.npcID == 393 || x.npcID == 394
                                    || x.npcID == 395 || x.npcID != Main.npc[x.npcIndex].netID || !Main.npc[x.npcIndex].active);
                                    return;
                                }
                                strike2 = new StrikeNPC(index, 395, Main.npc[index].FullName, strikeNPC[i].PlayerOrDamage, strikeNPC[i].AllDamage, 81000);
                                strikeNPC.Add(strike2);
                            }
                            //把炮塔受伤计算加入到本体核心中
                            else if (strikeNPC[i].npcID != 395)
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
                            if (strikeNPC[i].npcID == 395)
                            {
                                SendKillMessage("火星飞碟", strikeNPC[i].PlayerOrDamage, strikeNPC[i].AllDamage);
                                strikeNPC.RemoveAll(x => x.npcID == 392 || x.npcID == 393 || x.npcID == 394 ||
                                x.npcID == 395 || x.npcID != Main.npc[x.npcIndex].netID || !Main.npc[x.npcIndex].active);
                                return;
                            }
                        }
                        break;

                    //猪鲨的处理，把鲨鱼龙的伤害统计到猪鲨本体身上
                    case 370:
                    case 372:
                    case 373:
                        {
                            if (!Config.Sharkron) return;

                            StrikeNPC? strike2 = strikeNPC.Find(x => x.npcID == 370);
                            if (strike2 == null)
                            {
                                int index = -1;
                                foreach (var n in Main.npc)
                                {
                                    if (n.netID == 370)
                                    {
                                        index = n.whoAmI;
                                    }
                                }
                                if (index == -1)
                                {
                                    strikeNPC.RemoveAll(x => x.npcID == 370 || x.npcID == 372 || x.npcID == 373 ||
                                    x.npcID != Main.npc[x.npcIndex].netID || !Main.npc[x.npcIndex].active);
                                    return;
                                }
                                strike2 = new StrikeNPC(index, 370, Main.npc[index].FullName, strikeNPC[i].PlayerOrDamage, strikeNPC[i].AllDamage, 81000);
                                strikeNPC.Add(strike2);
                            }
                            //把鲨鱼龙受伤计算加入到猪鲨本体中
                            else if (strikeNPC[i].npcID != 370)
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
                            if (strikeNPC[i].npcID == 370)
                            {
                                SendKillMessage("猪龙鱼公爵", strikeNPC[i].PlayerOrDamage, strikeNPC[i].AllDamage);
                                strikeNPC.RemoveAll(x => x.npcID == 370 || x.npcID == 372 || x.npcID == 373 ||
                                x.npcID != Main.npc[x.npcIndex].netID || !Main.npc[x.npcIndex].active);
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
                            strikeNPC.RemoveAll(x => x.npcID == 398 || x.npcID == 397 || x.npcID == 396 ||
                            x.npcID != Main.npc[x.npcIndex].netID || !Main.npc[x.npcIndex].active);
                            return;
                        }

                    //机械骷髅王的处理，特殊点，本体可能被击中，其他肢体可能会死
                    case 127:
                    case 128:
                    case 129:
                    case 130:
                    case 131:
                        {
                            if (Main.zenithWorld && Config.MechQueen) return;

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
                                    strikeNPC.RemoveAll(x => x.npcID == 127 || x.npcID == 128 || x.npcID == 129 ||
                                    x.npcID == 130 || x.npcID == 131 || x.npcID != Main.npc[x.npcIndex].netID || !Main.npc[x.npcIndex].active);
                                    return;
                                }
                                strike2 = new StrikeNPC(index, 127, Main.npc[index].FullName, strikeNPC[i].PlayerOrDamage, strikeNPC[i].AllDamage, 300000);
                                strikeNPC.Add(strike2);
                            }
                            //把肢体受伤计算加入到本体头部中
                            else if (strikeNPC[i].npcID != 127)
                            {
                                if (!Config.Prime) return;
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
                                strikeNPC.RemoveAll(x => x.npcID == 127 || x.npcID == 128 || x.npcID == 129 ||
                                x.npcID == 130 || x.npcID == 131 || x.npcID != Main.npc[x.npcIndex].netID || !Main.npc[x.npcIndex].active);
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
                                    strikeNPC.RemoveAll(x => x.npcID == 35 || x.npcID == 36 ||
                                    x.npcID != Main.npc[x.npcIndex].netID || !Main.npc[x.npcIndex].active);
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
                                strikeNPC.RemoveAll(x => x.npcID == 35 || x.npcID == 36 ||
                                x.npcID != Main.npc[x.npcIndex].netID || !Main.npc[x.npcIndex].active);
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
                            bool G246 = Config.GolemHead;

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
                                    strikeNPC.RemoveAll(x => x.npcID == 245 || x.npcID == 246 || x.npcID == 247 ||
                                    x.npcID == 248 || x.npcID != Main.npc[x.npcIndex].netID || !Main.npc[x.npcIndex].active);
                                    return;
                                }
                                strike3 = new StrikeNPC(index, 245, Main.npc[index].FullName, strikeNPC[i].PlayerOrDamage, strikeNPC[i].AllDamage, 400000);
                                strikeNPC.Add(strike3);
                            }
                            //把除了本体以外的肢体的伤害计算加到本体上
                            else if (strikeNPC[i].npcID != 245)
                            {
                                //开启配置开关则忽略头部的血量计算
                                if (G246 || strikeNPC[i].npcID != 246)
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
                            }

                            if (strikeNPC[i].npcID == 245)
                            {
                                SendKillMessage(args.npc.FullName, strikeNPC[i].PlayerOrDamage, strikeNPC[i].AllDamage);
                                strikeNPC.RemoveAll(x => x.npcID == 245 || x.npcID == 246 || x.npcID == 247 ||
                                x.npcID == 248 || x.npcID != Main.npc[x.npcIndex].netID || !Main.npc[x.npcIndex].active);
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
                                    strikeNPC.RemoveAll(x => x.npcID == 266 || x.npcID == 267 ||
                                    x.npcID != Main.npc[x.npcIndex].netID || !Main.npc[x.npcIndex].active);
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
                                strikeNPC.RemoveAll(x => x.npcID == 266 || x.npcID == 267 ||
                                x.npcID != Main.npc[x.npcIndex].netID || !Main.npc[x.npcIndex].active);
                                return;
                            }
                        }
                        break;
                    default:
                        {

                            if (Main.zenithWorld && Config.MechQueen &&
                                args.npc.netID == 125 || args.npc.netID == 126 || args.npc.netID == 134 ||
                                args.npc.netID == 135 || args.npc.netID == 136 || args.npc.netID == 139) continue;

                            if (Config.CustomTransfer)
                                foreach (var Custom in Config.TList)
                                    if (args.npc.netID == Custom.NPCA) continue;

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

    #region 美杜莎
    public static Dictionary<string, double> MechQueen = new Dictionary<string, double>();
    private void OnMechQueen(NpcKilledEventArgs args)
    {
        if (!Config.Enabled || !Config.MechQueen) return;

        if (NPC.IsMechQueenUp || Main.zenithWorld)
        {
            for (int i = 0; i < strikeNPC.Count; i++)
            {
                if (strikeNPC[i].npcIndex == args.npc.whoAmI && strikeNPC[i].npcID == args.npc.netID)
                {
                    switch (strikeNPC[i].npcID)
                    {
                        case 125:
                        case 126:
                        case 127:
                        case 134:
                            {
                                //标识一直开启，
                                bool flag = true;

                                //伤害统计法
                                foreach (var sss in strikeNPC)
                                {
                                    if (sss.npcID == 125 || sss.npcID == 126 || sss.npcID == 127 || sss.npcID == 134)
                                    {
                                        foreach (var ss in strikeNPC[i].PlayerOrDamage)
                                        {
                                            UpdateDict(MechQueen, ss.Key, ss.Value);
                                        }
                                    }
                                }

                                //循环到没有活着的这些NPC则视为美杜莎死亡，标识自动通过
                                foreach (var n in Main.npc)
                                {
                                    //如果当前NPC (n) 不是被杀死的NPC (args.npc) 并且还活着,则关闭标识
                                    if (n.whoAmI != args.npc.whoAmI && n.active && IDGroup(n))
                                    {
                                        flag = false;
                                        break;
                                    }
                                }

                                //以上NPC全死 则发送伤害榜
                                if (flag)
                                {
                                    double num = 0;
                                    foreach (var Mech in MechQueen)
                                    {
                                        num += Mech.Value;
                                    }
                                    SendKillMessage("美杜莎", MechQueen, num);
                                    strikeNPC.RemoveAll(x =>
                                    x.npcID == 125 || x.npcID == 126 || x.npcID == 127 || x.npcID == 134 ||
                                    x.npcID != Main.npc[x.npcIndex].netID || !Main.npc[x.npcIndex].active);
                                    MechQueen.Clear();
                                    return;
                                }
                            }
                            break;
                    }
                }
            }
        }
    }

    #region 美杜莎的主体
    public static bool IDGroup(NPC nPC)
    {
        int[] id = { 125, 126, 127, 134 };
        return id.Contains(nPC.netID);
    }
    #endregion


    #endregion

}
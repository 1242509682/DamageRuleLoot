using System.Text;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;
using Microsoft.Xna.Framework;


namespace Plugin
{
    [ApiVersion(2, 1)]
    public class DamageRuleLoot : TerrariaPlugin
    {

        #region 插件信息
        public override string Name => "伤害规则掉落";
        public override string Author => "羽学";
        public override Version Version => new Version(1, 1, 0);
        public override string Description => "涡轮增压不蒸鸭";
        #endregion

        #region 注册与释放
        public DamageRuleLoot(Main game) : base(game) { }
        private GeneralHooks.ReloadEventD _reloadHandler;
        public override void Initialize()
        {
            LoadConfig();
            this._reloadHandler = (_) => LoadConfig();
            GeneralHooks.ReloadEvent += this._reloadHandler;
            ServerApi.Hooks.NpcSpawn.Register(this, this.OnNpcSpawn);
            ServerApi.Hooks.NpcStrike.Register(this, this.OnStrike);
            ServerApi.Hooks.NpcKilled.Register(this, this.OnNpcKill);
            ServerApi.Hooks.ServerJoin.Register(this, OnJoin);
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                GeneralHooks.ReloadEvent -= this._reloadHandler;
                ServerApi.Hooks.NpcSpawn.Deregister(this, this.OnNpcSpawn);
                ServerApi.Hooks.NpcStrike.Deregister(this, this.OnStrike);
                ServerApi.Hooks.NpcKilled.Deregister(this, this.OnNpcKill);
                ServerApi.Hooks.ServerJoin.Deregister(this, OnJoin);
                this.DamageList.Clear();
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

        #region 玩家更新配置方法（创建配置结构）
        private void OnJoin(JoinEventArgs args)
        {
            var plr = TShock.Players[args.Who];
            var list = Config.Items.FirstOrDefault(x => x.Name == plr.Name);

            if (!Config.Enabled) return;
            if (!Config.Items.Any(item => item.Name == plr.Name))
            {
                Config.Items.Add(new Configuration.ItemData()
                {
                    Name = plr.Name,
                    Damage = 0,
                });
            }
            else
            {
                list!.Damage = 0;
            }
            Config.Write();
        }
        #endregion

        #region 伤害统计方法
        private readonly Dictionary<NPC, Dictionary<string, double>> DamageList = new Dictionary<NPC, Dictionary<string, double>>();
        private void OnNpcSpawn(NpcSpawnEventArgs args)
        {
            if (!Config.Enabled) return;
            var npc = Main.npc[args.NpcId];

            if (npc.boss)
            {
                this.DamageList.Add(npc, new Dictionary<string, double>());
            }
        }
        private void OnStrike(NpcStrikeEventArgs args)
        {
            if (!Config.Enabled) return;

            if (this.DamageList.ContainsKey(args.Npc))
            {
                if (!this.DamageList[args.Npc].ContainsKey(args.Player.name))
                {
                    this.DamageList[args.Npc].Add(args.Player.name, 0);
                }
                this.DamageList[args.Npc][args.Player.name] += args.Damage;
            }
        }

        private void OnNpcKill(NpcKilledEventArgs args)
        {
            if (!Config.Enabled) return;
            if (this.DamageList.ContainsKey(args.npc) && this.DamageList[args.npc].Any())
            {
                var data = this.DamageList[args.npc];
                double npcLifeMax = 0;
                data.ForEach(p => npcLifeMax += data[p.Key]);
                var text = new StringBuilder();
                data.Keys.ForEach(p => text.AppendLine($"{p}: [c/74F3C9:{data[p]}] <{data[p] / npcLifeMax:0.00%}>, "));

                if (Config.Broadcast)
                {
                    TShock.Utils.Broadcast($"[c/74F3C9:{data.Count}] 位玩家击败了 [c/74F3C9:{args.npc.FullName}]\n{text}", new Color(247, 244, 150));
                }

                foreach (var plr in TShock.Players.Where(plr => plr != null && plr.Active && plr.IsLoggedIn))
                {
                    var Damage = data.FirstOrDefault(p => p.Key == plr.Name);
                    var item = Config.Items.FirstOrDefault(x => x.Name == plr.Name);

                    if (item != null)
                    {
                        item.Damage = Damage.Value / npcLifeMax;
                        Config.Write();

                        for (int i = 0; i < Terraria.Main.maxItems; i++)
                        {
                            var item2 = Terraria.Main.item[i];

                            if (Main.timeItemSlotCannotBeReusedFor[i] == 54000)
                            {
                                if (item.Damage < Config.Damages)
                                {
                                    Terraria.Main.item[i].active = false;
                                    plr.SendData(PacketTypes.ItemDrop, "", i);

                                }
                            }
                        }
                    }
                }
                this.DamageList.Remove(args.npc);
            }
        }
        #endregion

    }
}
using System.Text;
using TShockAPI;

namespace DamageRuleLoot;

public static class Tool
{
    #region 合并多个伤害字典
    public static Dictionary<string, double> CombineDamages(params Dictionary<string, double>[] Damages)
    {
        Dictionary<string, double> comb = new Dictionary<string, double>();
        foreach (var Dict in Damages)
        {
            foreach (var data in Dict)
            {
                if (comb.ContainsKey(data.Key))
                {
                    comb[data.Key] += data.Value;
                }
                else
                {
                    comb.Add(data.Key, data.Value);
                }
            }
        }
        return comb;
    }

    public static double GetCombineDamages(Dictionary<string, double> damage)
    {
        double Damage = 0;
        foreach (var item in damage)
        {
            Damage += item.Value;
        }
        return Damage;
    }
    #endregion

    #region 更新伤害字典
    public static void UpdateDict(Dictionary<string, double> dictionary, string key, double value)
    {
        if (dictionary.ContainsKey(key))
        {
            dictionary[key] += value;
        }
        else
        {
            dictionary.Add(key, value);
        }
    }
    #endregion

    #region  清空伤害字典
    public static void ClearDictionaries(params Dictionary<string, double>[] dictionaries)
    {
        foreach (var dict in dictionaries)
        {
            dict.Clear();
        }
    }
    #endregion

    #region 统计对BOSS的伤害消息方法
    public static void SendKillMessage(string BossName, Dictionary<string, double> PlayerOrDamage, double allDamage)
    {
        StringBuilder mess = new StringBuilder();
        Dictionary<string, double> sortpairs = new Dictionary<string, double>();
        StringBuilder LowDamager = new StringBuilder();
        int PlayerCount = TShock.Utils.GetActivePlayerCount();
        int Escape = PlayerCount - PlayerOrDamage.Count;
        mess.AppendLine($"            [i:3455][c/AD89D5:伤][c/D68ACA:害][c/DF909A:排][c/E5A894:行][c/E5BE94:榜][i:3454]");
        mess.AppendLine($" 当前服务器有 [c/74F3C9:{PlayerCount}位] 玩家 | 未参战: [c/A7DDF0:{Escape}位]");
        mess.AppendLine($" 恭喜以下 [c/74F3C9:{PlayerOrDamage.Count}位] 玩家击败了 [c/F7686D:{BossName}]");

        while (PlayerOrDamage.Count > 0)
        {
            string key = null!;
            double damage = 0;
            foreach (var v in PlayerOrDamage)
            {
                if (v.Value > damage)
                {
                    key = v.Key;
                    damage = v.Value;
                }
            }
            if (key != null)
            {
                sortpairs.Add(key, damage);
                PlayerOrDamage.Remove(key);
            }
        }

        foreach (var data in sortpairs)
        {
            mess.AppendLine($" [c/A7DDF0:{TShock.UserAccounts.GetUserAccountByName(data.Key).Name}]" +
                $"   伤害:[c/74F3C9:{data.Value}]" +
                $"   暴击:[c/74F3C9:{CritTracker.GetCritCount(TShock.UserAccounts.GetUserAccountByName(data.Key).Name)}]" +
                $"   比例:[c/74F3C9:{data.Value * 1.0f / allDamage:0.00%}]");

            CritTracker.CritCounts[data.Key] = 0;

            if (DamageRuleLoot.Config.Enabled2)
            {
                if (((data.Value / allDamage) < DamageRuleLoot.Config.Damages) && !DamageRuleLoot.Config.Expand.Contains(BossName))
                {
                    LowDamager.AppendFormat(" [c/A7DDF0:{0}]([c/74F3C9:{1:0.00%}])", TShock.UserAccounts.GetUserAccountByName(data.Key).Name, data.Value / allDamage);

                    if (LowDamager.Length > 0)
                    {
                        LowDamager.Append(", ");
                    }

                    foreach (var plr in TShock.Players.Where(plr => plr != null && plr.Active && plr.IsLoggedIn))
                    {
                        if (plr.Name == data.Key)
                        {
                            for (int i = 0; i < Terraria.Main.maxItems; i++)
                            {
                                if (Terraria.Main.timeItemSlotCannotBeReusedFor[i] == 54000)
                                {
                                    Terraria.Main.item[i].active = false;
                                    plr.SendData(PacketTypes.ItemDrop, "", i);
                                }
                            }
                        }
                    }
                }
            }
        }

        if (DamageRuleLoot.Config.Broadcast)
        {
            if (DamageRuleLoot.Config.Enabled3)
            {
                mess.AppendLine(DamageRuleLoot.Config.Advertisement);
            }

            TSPlayer.All.SendMessage(mess.ToString(), 247, 244, 150);
        }

        if (LowDamager.Length > 0 && DamageRuleLoot.Config.Broadcast2)
        {
            string[] playerNames = LowDamager.ToString().Split(new[] { "," }, StringSplitOptions.None);
            string joinedNames = string.Join(", ", playerNames);

            LowDamager.Insert(0, $"[c/F06576:【注意】]输出少于 [c/A7DDF0:{DamageRuleLoot.Config.Damages:0.00%}] 禁止掉落宝藏袋:\n");

            TSPlayer.All.SendMessage(LowDamager.ToString(), 247, 244, 150);
        }
    }
    #endregion
}

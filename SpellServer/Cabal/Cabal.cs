using Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SpellServer.Character;

namespace SpellServer
{
    internal class Cabal
    {
        public enum CabalSaveError
        {
            Success,
            Generic,
            AccountMismatch,
            NameValidity,
            SlotTaken,
            PickHack,
            ListHack,
        }

        public Int16 CabalId;
        public readonly ListCollection<Int32> MemberCharacterIds;
        public String CabalName;
        public String CabalTag;
        public String CabalMotto;
        public Cabal(Player player)
        {

        }
        public bool IsCabalNameTaken(string name)
        {
            return false;
        }

        public bool IsCabalTagTaken(string tag)
        {
            return false;
        }
        /*public static CabalSaveError Save(Player player, Cabal cabal, bool IsNew)
        {
            lock (PlayerManager.Players.SyncRoot)
            {
                Character tCharacter = player.ActiveCharacter;

                if (tCharacter == null)
                {
                    if (clientCharacter == null) return SaveError.Generic;

                    tCharacter = LoadByName(player, clientCharacter.Name);

                    if (tCharacter != null)
                    {
                        if (tCharacter.AccountId != player.AccountId) return SaveError.AccountMismatch;
                    }
                    else
                    {
                        tCharacter = clientCharacter;
                        tCharacter.AccountId = player.AccountId;

                        if (IsNameTaken(clientCharacter.Name) || !IsNameValid(clientCharacter.Name, false)) return SaveError.NameValidity;
                        if (IsInSlot(tCharacter.AccountId, tCharacter.Slot)) return SaveError.SlotTaken;

                        isNew = true;
                    }
                }

                if (isNew)
                {
                    tCharacter.Experience = player.IsAdmin ? tCharacter.Experience : 0;
                    tCharacter.Level = player.IsAdmin ? tCharacter.Level : (Byte)1;
                    tCharacter.Agility = 80;
                    tCharacter.Constitution = 80;
                    tCharacter.Memory = 80;
                    tCharacter.Reasoning = 80;
                    tCharacter.Discipline = 80;
                    tCharacter.Empathy = 80;
                    tCharacter.Intuition = 80;
                    tCharacter.Presence = 80;
                    tCharacter.Quickness = 80;
                    tCharacter.Strength = 80;
                    tCharacter.SpentStatPoints = 0;
                    tCharacter.BonusStatPoints = 0;
                    tCharacter.BonusStatPointsSpent = 0;
                    tCharacter.List1 = SpellManager.GetListId(tCharacter.Class, 0);
                    tCharacter.List2 = SpellManager.GetListId(tCharacter.Class, 1);
                    tCharacter.List3 = SpellManager.GetListId(tCharacter.Class, 2);
                    tCharacter.List4 = SpellManager.GetListId(tCharacter.Class, 3);
                    tCharacter.List5 = SpellManager.GetListId(tCharacter.Class, 4);
                    tCharacter.List6 = SpellManager.GetListId(tCharacter.Class, 5);
                    tCharacter.List7 = SpellManager.GetListId(tCharacter.Class, 6);
                    tCharacter.List8 = SpellManager.GetListId(tCharacter.Class, 7);
                    tCharacter.List9 = SpellManager.GetListId(tCharacter.Class, 8);
                    tCharacter.List10 = SpellManager.GetListId(tCharacter.Class, 9);
                }
                else
                {
                    if (player.ActiveArena != null && player.ActiveArenaPlayer != null)
                    {
                        if (player.ActiveArenaPlayer.ObjectiveExp > player.ActiveArenaPlayer.CombatExp)
                        {
                            player.ActiveArenaPlayer.ObjectiveExp = player.ActiveArenaPlayer.CombatExp;
                        }

                        tCharacter.Experience += (UInt64)player.ActiveArenaPlayer.CombatExp;
                        tCharacter.Experience += (UInt64)player.ActiveArenaPlayer.ObjectiveExp;
                        tCharacter.Experience += (UInt64)player.ActiveArenaPlayer.BonusExp;
                    }

                    if (tCharacter.PendingFlags.HasFlag(PendingFlag.AwardExp) && !player.Flags.HasFlag(PlayerFlag.ExpLocked))
                    {
                        tCharacter.Experience += (UInt32)player.ActiveCharacter.AwardExp;
                    }

                    if (tCharacter.PendingFlags.HasFlag(PendingFlag.GrantLevel))
                    {
                        ResetCharacterLists(tCharacter);
                        ResetCharacterStats(tCharacter);

                        tCharacter.Level = 1;
                        tCharacter.Experience = GetLevelExperience(tCharacter.GrantedLevel);
                    }

                    for (Int32 i = 0; i < MaxLevel; i++)
                    {
                        UInt64 nextLevel = LevelExp[tCharacter.Level] + (LevelExp[tCharacter.Level] * 4);

                        nextLevel += (nextLevel * 4);
                        nextLevel = nextLevel << 2;

                        if (tCharacter.Experience >= nextLevel && tCharacter.Level < MaxLevel)
                        {
                            tCharacter.Level++;
                        }
                        else break;
                    }

                    if (tCharacter.Experience > 2330000) tCharacter.Experience = 2330000;

                    if (clientCharacter != null)
                    {
                        tCharacter.SpellKey1 = clientCharacter.SpellKey1;
                        tCharacter.SpellKey2 = clientCharacter.SpellKey2;
                        tCharacter.SpellKey3 = clientCharacter.SpellKey3;
                        tCharacter.SpellKey4 = clientCharacter.SpellKey4;
                        tCharacter.SpellKey5 = clientCharacter.SpellKey5;
                        tCharacter.SpellKey6 = clientCharacter.SpellKey6;
                        tCharacter.SpellKey7 = clientCharacter.SpellKey7;
                        tCharacter.SpellKey8 = clientCharacter.SpellKey8;
                        tCharacter.SpellKey9 = clientCharacter.SpellKey9;
                        tCharacter.SpellKey10 = clientCharacter.SpellKey10;
                        tCharacter.SpellKey11 = clientCharacter.SpellKey11;
                        tCharacter.SpellKey12 = clientCharacter.SpellKey12;
                        tCharacter.SpellKey13 = clientCharacter.SpellKey13;
                        tCharacter.SpellKey14 = clientCharacter.SpellKey14;
                        tCharacter.SpellKey15 = clientCharacter.SpellKey15;
                        tCharacter.SpellKey16 = clientCharacter.SpellKey16;
                        tCharacter.SpellKey17 = clientCharacter.SpellKey17;
                        tCharacter.SpellKey18 = clientCharacter.SpellKey18;
                        tCharacter.SpellKey19 = clientCharacter.SpellKey19;
                        tCharacter.SpellKey20 = clientCharacter.SpellKey20;
                        tCharacter.SpellKey21 = clientCharacter.SpellKey21;
                        tCharacter.SpellKey22 = clientCharacter.SpellKey22;
                        tCharacter.SpellKey23 = clientCharacter.SpellKey23;
                        tCharacter.SpellKey24 = clientCharacter.SpellKey24;
                        tCharacter.SpellKey25 = clientCharacter.SpellKey25;
                        tCharacter.SpellKey26 = clientCharacter.SpellKey26;
                        tCharacter.SpellKey27 = clientCharacter.SpellKey27;
                        tCharacter.SpellKey28 = clientCharacter.SpellKey28;
                        tCharacter.SpellKey29 = clientCharacter.SpellKey29;
                        tCharacter.SpellKey30 = clientCharacter.SpellKey30;
                        tCharacter.SpellKey31 = clientCharacter.SpellKey31;
                        tCharacter.SpellKey32 = clientCharacter.SpellKey32;
                        tCharacter.SpellKey33 = clientCharacter.SpellKey33;
                        tCharacter.SpellKey34 = clientCharacter.SpellKey34;
                        tCharacter.SpellKey35 = clientCharacter.SpellKey35;
                        tCharacter.SpellKey36 = clientCharacter.SpellKey36;
                        tCharacter.SpellKey37 = clientCharacter.SpellKey37;
                        tCharacter.SpellKey38 = clientCharacter.SpellKey38;
                        tCharacter.SpellKey39 = clientCharacter.SpellKey39;
                        tCharacter.SpellKey40 = clientCharacter.SpellKey40;

                    }
                }

                if (clientCharacter != null)
                {
                    tCharacter.OpLevel = player.IsAdmin ? clientCharacter.OpLevel : tCharacter.OpLevel;

                    if ((clientCharacter.ListLevel1 < tCharacter.ListLevel1 || clientCharacter.ListLevel2 < tCharacter.ListLevel2 || clientCharacter.ListLevel3 < tCharacter.ListLevel3 || clientCharacter.ListLevel4 < tCharacter.ListLevel4 || clientCharacter.ListLevel5 < tCharacter.ListLevel5 || clientCharacter.ListLevel6 < tCharacter.ListLevel6 || clientCharacter.ListLevel7 < tCharacter.ListLevel7 || clientCharacter.ListLevel8 < tCharacter.ListLevel8 || clientCharacter.ListLevel9 < tCharacter.ListLevel9 || clientCharacter.ListLevel10 < tCharacter.ListLevel10) && !player.IsAdmin)
                    {
                        Program.ServerForm.CheatLog.WriteMessage(String.Format("[List Hack] AID: {0}, {1} ({2}), {3}, {4}, {5}, {6}", player.AccountId, player.Username, tCharacter.Name, clientCharacter.ListLevel1, tCharacter.ListLevel1, clientCharacter.ListLevel2, tCharacter.ListLevel2), Color.Red);
                        Program.ServerForm.CheatLog.WriteMessage("List", Color.Red);
                        player.DisconnectReason = Resources.Strings_Disconnect.ListHack;
                        player.Disconnect = true;
                        return SaveError.ListHack;
                    }

                    tCharacter.ListLevel1 = clientCharacter.ListLevel1 < tCharacter.ListLevel1 && !player.IsAdmin ? tCharacter.ListLevel1 : clientCharacter.ListLevel1;
                    tCharacter.ListLevel2 = clientCharacter.ListLevel2 < tCharacter.ListLevel2 && !player.IsAdmin ? tCharacter.ListLevel2 : clientCharacter.ListLevel2;
                    tCharacter.ListLevel3 = clientCharacter.ListLevel3 < tCharacter.ListLevel3 && !player.IsAdmin ? tCharacter.ListLevel3 : clientCharacter.ListLevel3;
                    tCharacter.ListLevel4 = clientCharacter.ListLevel4 < tCharacter.ListLevel4 && !player.IsAdmin ? tCharacter.ListLevel4 : clientCharacter.ListLevel4;
                    tCharacter.ListLevel5 = clientCharacter.ListLevel5 < tCharacter.ListLevel5 && !player.IsAdmin ? tCharacter.ListLevel5 : clientCharacter.ListLevel5;
                    tCharacter.ListLevel6 = clientCharacter.ListLevel6 < tCharacter.ListLevel6 && !player.IsAdmin ? tCharacter.ListLevel6 : clientCharacter.ListLevel6;
                    tCharacter.ListLevel7 = clientCharacter.ListLevel7 < tCharacter.ListLevel7 && !player.IsAdmin ? tCharacter.ListLevel7 : clientCharacter.ListLevel7;
                    tCharacter.ListLevel8 = clientCharacter.ListLevel8 < tCharacter.ListLevel8 && !player.IsAdmin ? tCharacter.ListLevel8 : clientCharacter.ListLevel8;
                    tCharacter.ListLevel9 = clientCharacter.ListLevel9 < tCharacter.ListLevel9 && !player.IsAdmin ? tCharacter.ListLevel9 : clientCharacter.ListLevel9;
                    tCharacter.ListLevel10 = clientCharacter.ListLevel10 < tCharacter.ListLevel10 && !player.IsAdmin ? tCharacter.ListLevel10 : clientCharacter.ListLevel10;

                    if (player.IsAdmin)
                    {
                        tCharacter.Class = clientCharacter.Class;
                        tCharacter.Model = clientCharacter.Model;

                        tCharacter.List1 = clientCharacter.List1;
                        tCharacter.List2 = clientCharacter.List2;
                        tCharacter.List3 = clientCharacter.List3;
                        tCharacter.List4 = clientCharacter.List4;
                        tCharacter.List5 = clientCharacter.List5;
                        tCharacter.List6 = clientCharacter.List6;
                        tCharacter.List7 = clientCharacter.List7;
                        tCharacter.List8 = clientCharacter.List8;
                        tCharacter.List9 = clientCharacter.List9;
                        tCharacter.List10 = 18; // Admin List

                        //tCharacter.Level = clientCharacter.Level;
                        //tCharacter.Experience = clientCharacter.Experience;
                    }
                }

                if (tCharacter.PendingFlags.HasFlag(PendingFlag.ListReset))
                {
                    ResetCharacterLists(tCharacter);
                }

                if (tCharacter.PendingFlags.HasFlag(PendingFlag.GrantLevel))
                {
                    ResetCharacterLists(tCharacter);
                    ResetCharacterStats(tCharacter);
                }

                Int32 numPicks = tCharacter.ListLevel1 + tCharacter.ListLevel2 + tCharacter.ListLevel3 + tCharacter.ListLevel4 + tCharacter.ListLevel5 + tCharacter.ListLevel6 + tCharacter.ListLevel7 + tCharacter.ListLevel8 + tCharacter.ListLevel9 + tCharacter.ListLevel10;
                numPicks = (numPicks - SpellManager.GetNumLists(tCharacter.Class));

                if ((numPicks < 0 || (tCharacter.Level * 2) < numPicks) && !(player.IsAdmin || player.Admin == AdminLevel.Tester))
                {
                    Program.ServerForm.CheatLog.WriteMessage(String.Format("[Infinite Picks Hack] AID: {0}, {1} ({2})", player.AccountId, player.Username, tCharacter.Name), Color.Red);

                    Program.ServerForm.CheatLog.WriteMessage("Pick", Color.Red);

                    player.DisconnectReason = Resources.Strings_Disconnect.PickHack;
                    player.Disconnect = true;
                    return SaveError.PickHack;
                }

                tCharacter.SpellPicks = (Byte)(((tCharacter.Level * 2) - numPicks) / 2);

                PlayerFlag tempFlag = player.Flags;
                tempFlag &= ~PlayerFlag.MagestormPlus;
                tempFlag &= ~PlayerFlag.Hidden;

                MySQL.Character.Save(tCharacter, isNew, tempFlag);

                Network.Send(player, GamePacket.Outgoing.Study.SendCharacterInSlot(player, tCharacter.Slot, MySQL.Character.FindByAccountIdAndSlot(player.AccountId, tCharacter.Slot)));

                if (!isNew)
                {
                    tCharacter.Statistics.Save();
                }

                tCharacter.PendingFlags = PendingFlag.None;

                return SaveError.Success;
            }
        }*/

    }
}

using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;
using Helper;
using SharpDX.Win32;
using SpellServer.GamePacket.Incoming;
using SpellServer.Statistics;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using Color = System.Drawing.Color;
using static Mysqlx.Notice.Warning.Types;
using static SpellServer.Character;
using static SpellServer.MySQL;

namespace SpellServer
{
    public class Cabal
    {
        public enum CabalSaveError
        {
            Success,
            Generic,
            CabalMismatch,
            NameValidity,            
        }

        public Int32 CabalId;
        public readonly ListCollection<Int32> MemberCharacterIds;
        public String CabalName;
        public String CabalTag;
        public String CabalMotto;
        public String CabalLeader;

        public static Boolean IsCabalNameTaken(String name)
        {
            if (CabalManager.Cabals.FindByCabalName(name) != null)
                return true;

            var table = MySQL.Cabals.FindByName(name.Escape());
            return table != null && table.Rows.Count > 0;
        }
        public static Boolean IsCabalTagTaken(String name)
        {
            if (CabalManager.Cabals.FindByCabalTag(name) != null)
                return true;

            var table = MySQL.Cabals.FindByTag(name.Escape());
            return table != null && table.Rows.Count > 0;
        }        
        public static Boolean IsCabalNameValid(String name, Boolean allowAllCharacters)
        {
            name = name.Escape();
            Regex reg = new Regex("^[a-zA-Z]*[_]?[a-zA-Z]*$");

            if (SpellServer.Character.FilteredNames.Any(filteredName => name.ToLower().Contains(filteredName))) return false;
            return (reg.IsMatch(name) && !allowAllCharacters) && (name.Length >= 3 && name.Length < 12);
        }
        public static Boolean IsCabalTagValid(String tag)
        {
            tag = tag.Escape();
            Regex reg = new Regex("^[a-zA-Z]{1,4}$");

            if (reg.IsMatch(tag) && (tag.Length >= 1 && tag.Length < 5))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public static Cabal LoadByNameAndCabalId(Player player, String name)
        {
            DataTable table = MySQL.Character.FindByNameAndAccountId(name.Escape(), player.AccountId);
            return table.Rows.Count <= 0 ? null : new Cabal(player, table.Rows[0]);
        }
        public Cabal(Player player, DataRow data)
        {
            CabalId = data.Field<Int16>("cabalid");
            CabalName = data.Field<string>("cabalname");
            CabalTag = data.Field<string>("cabaltag");
            CabalLeader = data.Field<string>("caballeader");
        }
        public Cabal() 
        {
            MemberCharacterIds = new ListCollection<Int32>();
            CabalId = 0;
            CabalName = "";
            CabalTag = "";
            CabalLeader = "";
        }
        public static void JoinCabal(Player player, Cabal cabal)
        {
            Program.ServerForm.MainLog.WriteMessage($"{player.ActiveCharacter.Name} is joining Cabal: {cabal.CabalName}({cabal.CabalTag})", Color.Red);
            Save(player, cabal, false);

            UpdateAllPlayers(player, cabal, true);
        }
        public static void CreateCabal(Player player, String cabalName, String cabalTag)
        {
            lock (CabalManager.Cabals.SyncRoot)
            {
                Cabal newCabal = new Cabal();

                newCabal.CabalId = CabalManager.Cabals.AvailableId;

                newCabal.CabalName = cabalName;

                newCabal.CabalTag = cabalTag;

                newCabal.CabalLeader = player.ActiveCharacter.Name;

                bool isNew = true;

                Program.ServerForm.MainLog.WriteMessage($"{player.ActiveCharacter.Name} is creating and joining Cabal: {cabalName}({cabalTag})", Color.Red);

                CabalSaveError saved = Save(player, newCabal, isNew);

                UpdateAllPlayers(player, newCabal, true);

            }
        }
        public static void LeaveCabal(Player player, Cabal cabal)
        {
            player.ActiveCharacter.CabalId = 0;

            Program.ServerForm.MainLog.WriteMessage($"{player.ActiveCharacter.Name} is leaving Cabal: {cabal.CabalName}({cabal.CabalTag})", Color.Red);

            Save(player, CabalManager.Cabals.FindById(player.ActiveCharacter.CabalId), false);

            UpdateAllPlayers(player, cabal, false);
        }

        public static CabalSaveError Save(Player player, Cabal cabal, bool isNew)
        {
            if (cabal == null) return CabalSaveError.Generic;

            lock (CabalManager.Cabals.SyncRoot)
            {                     
                if (isNew)
                {
                    if (IsCabalNameTaken(cabal.CabalName) || !IsCabalNameValid(cabal.CabalName, false) || IsCabalTagTaken(cabal.CabalTag) || !IsCabalTagValid(cabal.CabalTag)) return CabalSaveError.NameValidity;

                    CabalManager.Cabals.Add(cabal);
                }
                else
                {
                    var exisiting = CabalManager.Cabals.FindById(cabal.CabalId);
                    if (exisiting != null)
                    {
                        exisiting.CabalName = cabal.CabalName;
                        exisiting.CabalTag = cabal.CabalTag;
                        exisiting.CabalLeader = cabal.CabalLeader;
                    }                                        
                }
                                
                player.ActiveCharacter.CabalId = cabal.CabalId;

                MySQL.Cabals.Save(cabal, isNew);
                MySQL.Character.Save(player.ActiveCharacter, false, player.Flags);

                return CabalSaveError.Success;
            }
        }

        public static void UpdateAllPlayers(SpellServer.Player player, Cabal cabal, bool Join)
        {
            if (player.IsInArena) return;

            lock (PlayerManager.Players.SyncRoot)
            {
                foreach (Player targetplayer in PlayerManager.Players)
                {
                    if (targetplayer == null) continue;

                    // 1. Don't send the target player to themselves (usually handled by the client)
                    if (targetplayer == player) continue;

                    // 2. THE VISIBILITY RULE
                    // If the person in the list is HIDDEN...
                    if (player.Flags.HasFlag(PlayerFlag.Hidden))
                    {
                        // ...ONLY show them if the person RECEIVING the list is an Admin.
                        if (!targetplayer.IsAdmin) continue;
                    }

                    GamePacket.Incoming.World.RequestedAllPlayers(targetplayer);

                    /*if (Join)
                    {
                        Program.ServerForm.MainLog.WriteMessage($"Sending CabalJoin message to {targetplayer.ActiveCharacter.Name}", Color.Red);
                        Network.Send(targetplayer, GamePacket.Outgoing.Study.CabalJoin(player, targetplayer));
                    }
                    else
                    {
                        Program.ServerForm.MainLog.WriteMessage($"Sending LeaveCabal message to {targetplayer.ActiveCharacter.Name}", Color.Red);
                        Network.Send(targetplayer, GamePacket.Outgoing.Study.LeaveCabal(player, targetplayer, cabal));
                    }*/
                }
            }
        }
    }
}

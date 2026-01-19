using Helper;
using Helper.Network;
using SpellServer.Statistics;
using Org.BouncyCastle.Math.EC;
using System;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace SpellServer
{
    public class Character
    {
		public enum SaveError
		{
			Success,
			Generic,
			AccountMismatch,
			NameValidity,
			SlotTaken,
			PickHack,
			ListHack,
		}
        public enum PlayerClass
        {
            Magician,
            Runemage,
            Mystic,
            Healer,
        }

        [Flags]
        public enum Milestone
        {
            None = 0x0,
            StunningForce = 0x1,
            EnergizedBolts = 0x2,
            BleedResistance1 = 0x4,
            BleedResistance2 = 0x8,
            ExceptionalHealth = 0x10,
            MentalBarrier = 0x20,
            PenetrateMind = 0x40,
        }

        public enum LoadedType
        {
            Server,
            Client,
        }

        public enum SaveType
        {
            Default,
        }

        [Flags]
        public enum PendingFlag
        {
            None = 0x0,
            ListReset = 0x1,
            AwardExp = 0x2,
            GrantLevel = 0x4,
        }

        public UInt32 AvailableStatPoints
        {
            get
            {
                long points = (((long)Level - 1) * 5) - SpentStatPoints + (BonusStatPoints - BonusStatPointsSpent);
                return (UInt32)Math.Max(0, points);
            }
        }
        public Int16 MaxHealth
        {
            get
            {
                Int64 baseHp = 0;
                float scaler = 0.0f;

                switch (Class)
                {
                    case PlayerClass.Magician:
                    {
                        baseHp = Convert.ToInt16(((Level - 1) * 4) + 19);
                        scaler = 0.0085f;
                        break;
                    }
                    case PlayerClass.Runemage:
                    {
                        baseHp = Convert.ToInt16(((Level - 1) * 6) + 19);
                        scaler = 0.01225f;
                        break;
                    }
                    case PlayerClass.Mystic:
                    {
                        baseHp = Convert.ToInt16(((Level - 1) * 5) + 20);
                        scaler = 0.01175f;
                        break;
                    }
                    case PlayerClass.Healer:
                    {
                        baseHp = Convert.ToInt16(((Level - 1) * 5) + 19);
                        scaler = 0.0088f;
                        break;
                    }
                    default:
                    {
                        return Convert.ToInt16(baseHp);
                    }
                }

                return (baseHp == 0) ? Convert.ToInt16(20) : Convert.ToInt16(baseHp + (Single)Math.Floor((baseHp * scaler) * ((Constitution - 50f) * 0.5f)));
            }
        }

        public Int16 MaxSpellPoints
        {
            get
			{
                Single baseSp = 0;
                Single statAdd = 0;

                switch (Class)
                {
                    case PlayerClass.Magician:
                    {
                        baseSp = (Single)Math.Floor(((Level + 1) + ((Level + 1) * 9)) * 0.5f) + 17;
                        statAdd = (baseSp * 0.0115f) * ((Empathy - 50f) * 0.48f);
                        break;
                    }
                    case PlayerClass.Runemage:
                    {
                        baseSp = (Single)Math.Floor(((Level + 1) + ((Level + 1) * 9)) * 0.5f) + 15;
                        statAdd = (baseSp * 0.0135f) * ((Empathy - 50f) * 0.15f);
	                    break;
                    }
                    case PlayerClass.Mystic:
                    {
                        baseSp = (Single)Math.Floor(((Level + 1) + ((Level + 1) * 9)) * 0.5f) + 16;
                        statAdd = (baseSp * 0.01675f) * ((Presence - 50f) * 0.47f);
                        break;
                    }
                    case PlayerClass.Healer:
                    {
                        baseSp = (Single)Math.Floor(((Level + 1) + ((Level + 1) * 6)) * 0.5f) + 10;
                        statAdd = (baseSp * 0.015f) * ((Intuition - 50f) * 0.85f);
                        break;
                    }
                }

				return Convert.ToInt16((Single)Math.Floor(baseSp + statAdd));
            }
        }

        private Int32 _awardExp;
        public Int32 AwardExp
        {
            get
            {
                return _awardExp;
            }
            set
            {
                if (value < 0) return;
                if (value > 0)
                {
                    PendingFlags |= PendingFlag.AwardExp;
                    _awardExp = value;
                }

                if (value == 0)
                {
                    PendingFlags &= ~PendingFlag.AwardExp;
                    _awardExp = value;
                }
            }
        }

		private UInt64 _experience;
		public UInt64 Experience
		{
			get
			{
				return _experience;
			}
			set
			{
				if (value > 2330000) value = 2330000;
				_experience = value;
			}
		}

		private Int32 _grantedLevel;
        public Int32 GrantedLevel
        {
            get
            {
                return _grantedLevel;
            }
            set
            {
                if (value < 0) return;
                if (value > 0)
                {
                    PendingFlags |= PendingFlag.GrantLevel;
                    _grantedLevel = value;
                }
                if (value == 0)
                {
                    PendingFlags &= ~PendingFlag.GrantLevel;
                    _grantedLevel = value;
                }
            }
        }

		private static readonly ListCollection<String> FilteredNames = new ListCollection<String>();
	    private static readonly UInt64[] LevelExp =
        {
            0, 5, 50, 150, 300, 500, 800, 1200, 1700, 2300, 3000, 3800, 4700, 5700, 6800, 8000, 9300, 10700, 12200, 13800, 15500, 17300, 19200, 21200, 23300, 25500, 27800, 30200, 32700, 35300, 38000, 41000, 44000, 47000, 50000, 53000, 56000, 59000, 62000, 65000, 68000, 71000, 74000, 77000, 80000, 83000, 86000, 89000, 92000, 95000, 100000
        };

	    private const Int32 MaxLevel = 25;
        public readonly SpellTreeCollection SpellTrees;

        public LoadedType LoadType;

        public readonly Int32 CharacterId;
        public Int32 AccountId;
        public readonly Byte Slot;
        public String Name;

        public String CabalName;
        public String CabalTag;
        public Int32 CabalId;

        public Byte Agility;
        public Byte Constitution;
        public Byte Memory;
        public Byte Discipline;
        public Byte Reasoning;
        public Byte Empathy;
        public Byte Intuition;
        public Byte Presence;
        public Byte Quickness;
        public Byte Strength;

        public UInt32 SpentStatPoints;
        public UInt32 BonusStatPoints;
        public UInt32 BonusStatPointsSpent;

        public Byte List1;
        public Byte List2;
        public Byte List3;
        public Byte List4;
        public Byte List5;
        public Byte List6;
        public Byte List7;
        public Byte List8;
        public Byte List9;
        public Byte List10;

        public Byte ListLevel1;
        public Byte ListLevel2;
        public Byte ListLevel3;
        public Byte ListLevel4;
        public Byte ListLevel5;
        public Byte ListLevel6;
        public Byte ListLevel7;
        public Byte ListLevel8;
        public Byte ListLevel9;
        public Byte ListLevel10;

        public PlayerClass Class;
        public Byte Level;
        public Byte SpellPicks;
        public Byte Model;

        public UInt16 SpellKey1;
        public UInt16 SpellKey2;
        public UInt16 SpellKey3;
        public UInt16 SpellKey4;
        public UInt16 SpellKey5;
        public UInt16 SpellKey6;
        public UInt16 SpellKey7;
        public UInt16 SpellKey8;
        public UInt16 SpellKey9;
        public UInt16 SpellKey10;
        public UInt16 SpellKey11;
        public UInt16 SpellKey12;
        public UInt16 SpellKey13;
        public UInt16 SpellKey14;
        public UInt16 SpellKey15;
        public UInt16 SpellKey16;
        public UInt16 SpellKey17;
        public UInt16 SpellKey18;
        public UInt16 SpellKey19;
        public UInt16 SpellKey20;
        public UInt16 SpellKey21;
        public UInt16 SpellKey22;
        public UInt16 SpellKey23;
        public UInt16 SpellKey24;
        public UInt16 SpellKey25;
        public UInt16 SpellKey26;
        public UInt16 SpellKey27;
        public UInt16 SpellKey28;
        public UInt16 SpellKey29;
        public UInt16 SpellKey30;
        public UInt16 SpellKey31;
        public UInt16 SpellKey32;
        public UInt16 SpellKey33;
        public UInt16 SpellKey34;
        public UInt16 SpellKey35;
        public UInt16 SpellKey36;
        public UInt16 SpellKey37;
        public UInt16 SpellKey38;
        public UInt16 SpellKey39;
        public UInt16 SpellKey40;

        public Byte OpLevel;

        public PlayerFlag PlayerFlags;
        public PendingFlag PendingFlags;

        public Milestone Milestones;

        public StatisticSheet Statistics;

        public Character(Player player, DataRow data)
        {
            LoadType = LoadedType.Server;
            PendingFlags = PendingFlag.None;

            CharacterId = data.Field<Int32>("charid");
            AccountId = data.Field<Int32>("accountid");
            CabalId = data.Field<Int32>("cabalid");
            Slot = data.Field<Byte>("slot");
            Name = data.Field<String>("name");
            Agility = data.Field<Byte>("agility");
            Constitution = data.Field<Byte>("constitution");
            Memory = data.Field<Byte>("memory");
            Reasoning = data.Field<Byte>("reasoning");
            Discipline = data.Field<Byte>("discipline");
            Empathy = data.Field<Byte>("empathy");
            Intuition = data.Field<Byte>("intuition");
            Presence = data.Field<Byte>("presence");
            Quickness = data.Field<Byte>("quickness");
            Strength = data.Field<Byte>("strength");
            SpentStatPoints = data.Field<UInt32>("spent_stat");
            BonusStatPoints = data.Field<UInt32>("bonus_stat");
            BonusStatPointsSpent = data.Field<UInt32>("bonus_spent");
            List1 = data.Field<Byte>("list_1");
            List2 = data.Field<Byte>("list_2");
            List3 = data.Field<Byte>("list_3");
            List4 = data.Field<Byte>("list_4");
            List5 = data.Field<Byte>("list_5");
            List6 = data.Field<Byte>("list_6");
            List7 = data.Field<Byte>("list_7");
            List8 = data.Field<Byte>("list_8");
            List9 = data.Field<Byte>("list_9");
            List10 = data.Field<Byte>("list_10");
            ListLevel1 = data.Field<Byte>("list_level_1");
            ListLevel2 = data.Field<Byte>("list_level_2");
            ListLevel3 = data.Field<Byte>("list_level_3");
            ListLevel4 = data.Field<Byte>("list_level_4");
            ListLevel5 = data.Field<Byte>("list_level_5");
            ListLevel6 = data.Field<Byte>("list_level_6");
            ListLevel7 = data.Field<Byte>("list_level_7");
            ListLevel8 = data.Field<Byte>("list_level_8");
            ListLevel9 = data.Field<Byte>("list_level_9");
            ListLevel10 = data.Field<Byte>("list_level_10");
            Class = (PlayerClass)data.Field<Byte>("class");
            Level = data.Field<Byte>("level");
            SpellPicks = data.Field<Byte>("spell_picks");
            Model = data.Field<Byte>("model");
            Experience = data.Field<UInt64>("experience");
            SpellKey1 = data.Field<UInt16>("spell_key_1");
            SpellKey2 = data.Field<UInt16>("spell_key_2");
            SpellKey3 = data.Field<UInt16>("spell_key_3");
            SpellKey4 = data.Field<UInt16>("spell_key_4");
            SpellKey5 = data.Field<UInt16>("spell_key_5");
            SpellKey6 = data.Field<UInt16>("spell_key_6");
            SpellKey7 = data.Field<UInt16>("spell_key_7");
            SpellKey8 = data.Field<UInt16>("spell_key_8");
            SpellKey9 = data.Field<UInt16>("spell_key_9");
            SpellKey10 = data.Field<UInt16>("spell_key_10");
            SpellKey11 = data.Field<UInt16>("spell_key_11");
            SpellKey12 = data.Field<UInt16>("spell_key_12");
            SpellKey13 = data.Field<UInt16>("spell_key_13");
            SpellKey14 = data.Field<UInt16>("spell_key_14");
            SpellKey15 = data.Field<UInt16>("spell_key_15");
            SpellKey16 = data.Field<UInt16>("spell_key_16");
            SpellKey17 = data.Field<UInt16>("spell_key_17");
            SpellKey18 = data.Field<UInt16>("spell_key_18");
            SpellKey19 = data.Field<UInt16>("spell_key_19");
            SpellKey20 = data.Field<UInt16>("spell_key_20");
            SpellKey21 = data.Field<UInt16>("spell_key_21");
            SpellKey22 = data.Field<UInt16>("spell_key_22");
            SpellKey23 = data.Field<UInt16>("spell_key_23");
            SpellKey24 = data.Field<UInt16>("spell_key_24");
            SpellKey25 = data.Field<UInt16>("spell_key_25");
            SpellKey26 = data.Field<UInt16>("spell_key_26");
            SpellKey27 = data.Field<UInt16>("spell_key_27");
            SpellKey28 = data.Field<UInt16>("spell_key_28");
            SpellKey29 = data.Field<UInt16>("spell_key_29");
            SpellKey30 = data.Field<UInt16>("spell_key_30");
            SpellKey31 = data.Field<UInt16>("spell_key_31");
            SpellKey32 = data.Field<UInt16>("spell_key_32");
            SpellKey33 = data.Field<UInt16>("spell_key_33");
            SpellKey34 = data.Field<UInt16>("spell_key_34");
            SpellKey35 = data.Field<UInt16>("spell_key_35");
            SpellKey36 = data.Field<UInt16>("spell_key_36");
            SpellKey37 = data.Field<UInt16>("spell_key_37");
            SpellKey38 = data.Field<UInt16>("spell_key_38");
            SpellKey39 = data.Field<UInt16>("spell_key_39");
            SpellKey40 = data.Field<UInt16>("spell_key_40");
            CabalName = data.Field<String>("cabalname");
            CabalTag = data.Field<String>("cabaltag");
            OpLevel = data.Field<Byte>("oplevel");
            PlayerFlags = (PlayerFlag)data.Field<UInt32>("flags");

            Statistics = new StatisticSheet(player, this);

			SpellTrees = new SpellTreeCollection
                         {
                             new SpellTree(SpellManager.SpellTrees.FindById(List1), ListLevel1),
                             new SpellTree(SpellManager.SpellTrees.FindById(List2), ListLevel2),
                             new SpellTree(SpellManager.SpellTrees.FindById(List3), ListLevel3),
                             new SpellTree(SpellManager.SpellTrees.FindById(List4), ListLevel4),
                             new SpellTree(SpellManager.SpellTrees.FindById(List5), ListLevel5),
                             new SpellTree(SpellManager.SpellTrees.FindById(List6), ListLevel6),
                             new SpellTree(SpellManager.SpellTrees.FindById(List7), ListLevel7),
                             new SpellTree(SpellManager.SpellTrees.FindById(List8), ListLevel8),
                             new SpellTree(SpellManager.SpellTrees.FindById(List9), ListLevel9), 
                             new SpellTree(SpellManager.SpellTrees.FindById(List10), ListLevel10)
                         };

            SpellTrees.RemoveAll(spellTrees => spellTrees.TreeSpells == null);

            LoadMilestones();
        }

        public void LoadMilestones()
        {
            if (Strength >= 120)
            {
                Milestones |= Milestone.StunningForce;

                if (Strength >= 126)
                {
                    Milestones |= Milestone.EnergizedBolts;
                }
            }

            if (Constitution >= 105)
            {
                Milestones |= Milestone.BleedResistance1;

                if (Constitution >= 115)
                {
                    Milestones &= ~Milestone.BleedResistance1;
                    Milestones |= Milestone.BleedResistance2;

                    if (Constitution >= 120)
                    {
                        Milestones |= Milestone.ExceptionalHealth;
                    }
                }
            }

            if (Reasoning >= 115)
            {
                Milestones |= Milestone.MentalBarrier;

                if (Reasoning >= 125)
                {
                    Milestones |= Milestone.PenetrateMind;
                }
            }
        }

        public Character(MemoryStream inStream)
        {
            LoadType = LoadedType.Client;
            PendingFlags = PendingFlag.None;

            Byte[] stringBuffer = new Byte[32];
            Byte[] expBuffer = new Byte[4];

            AccountId = 0;
            inStream.Seek(22, SeekOrigin.Current);
            Slot = (Byte)inStream.ReadByte();

            inStream.Seek(3, SeekOrigin.Current);
            inStream.Read(stringBuffer, 0, 20);
            Name = Encoding.ASCII.GetString(stringBuffer).Split((Char)0)[0];
            Agility = (Byte)inStream.ReadByte();
            Constitution = (Byte)inStream.ReadByte();
            Memory = (Byte)inStream.ReadByte();
            Reasoning = (Byte)inStream.ReadByte();
            Discipline = (Byte)inStream.ReadByte();
            Empathy = (Byte)inStream.ReadByte();
            Intuition = (Byte)inStream.ReadByte();
            Presence = (Byte)inStream.ReadByte();
            Quickness = (Byte)inStream.ReadByte();
            Strength = (Byte)inStream.ReadByte();
            SpentStatPoints = 0;
            BonusStatPoints = 0;
            BonusStatPointsSpent = 0;
            inStream.Seek(14, SeekOrigin.Current);
            List1 = (Byte)inStream.ReadByte();
            List2 = (Byte)inStream.ReadByte();
            List3 = (Byte)inStream.ReadByte();
            List4 = (Byte)inStream.ReadByte();
            List5 = (Byte)inStream.ReadByte();
            List6 = (Byte)inStream.ReadByte();
            List7 = (Byte)inStream.ReadByte();
            List8 = (Byte)inStream.ReadByte();
            List9 = (Byte)inStream.ReadByte();
            List10 = (Byte)inStream.ReadByte();
            inStream.Seek(2, SeekOrigin.Current);
            ListLevel1 = (Byte)inStream.ReadByte();
            ListLevel2 = (Byte)inStream.ReadByte();
            ListLevel3 = (Byte)inStream.ReadByte();
            ListLevel4 = (Byte)inStream.ReadByte();
            ListLevel5 = (Byte)inStream.ReadByte();
            ListLevel6 = (Byte)inStream.ReadByte();
            ListLevel7 = (Byte)inStream.ReadByte();
            ListLevel8 = (Byte)inStream.ReadByte();
            ListLevel9 = (Byte)inStream.ReadByte();
            ListLevel10 = (Byte)inStream.ReadByte();
            inStream.Seek(2, SeekOrigin.Current);

            Byte pClass = (Byte)inStream.ReadByte();
            if (pClass > 3) pClass = 0;
            Class = (PlayerClass)pClass;

            Level = (Byte)inStream.ReadByte();
            SpellPicks = (Byte)inStream.ReadByte();
            inStream.Seek(1, SeekOrigin.Current);
            Model = (Byte)inStream.ReadByte();
            inStream.Seek(3, SeekOrigin.Current);


            inStream.Read(expBuffer, 0, 4);
            Experience = NetHelper.FlipBytes(BitConverter.ToUInt32(expBuffer, 0));

            Byte[] kBuffer = new Byte[2];
            inStream.Read(kBuffer, 0, 2);
            SpellKey1 = NetHelper.FlipBytes(BitConverter.ToUInt16(kBuffer, 0));
            inStream.Read(kBuffer, 0, 2);
            SpellKey2 = NetHelper.FlipBytes(BitConverter.ToUInt16(kBuffer, 0));
            inStream.Read(kBuffer, 0, 2);
            SpellKey3 = NetHelper.FlipBytes(BitConverter.ToUInt16(kBuffer, 0));
            inStream.Read(kBuffer, 0, 2);
            SpellKey4 = NetHelper.FlipBytes(BitConverter.ToUInt16(kBuffer, 0));
            inStream.Read(kBuffer, 0, 2);
            SpellKey5 = NetHelper.FlipBytes(BitConverter.ToUInt16(kBuffer, 0));
            inStream.Read(kBuffer, 0, 2);

            SpellKey6 = NetHelper.FlipBytes(BitConverter.ToUInt16(kBuffer, 0));
            inStream.Read(kBuffer, 0, 2);
            SpellKey7 = NetHelper.FlipBytes(BitConverter.ToUInt16(kBuffer, 0));
            inStream.Read(kBuffer, 0, 2);
            SpellKey8 = NetHelper.FlipBytes(BitConverter.ToUInt16(kBuffer, 0));
            inStream.Read(kBuffer, 0, 2);
            SpellKey9 = NetHelper.FlipBytes(BitConverter.ToUInt16(kBuffer, 0));
            inStream.Read(kBuffer, 0, 2);
            SpellKey10 = NetHelper.FlipBytes(BitConverter.ToUInt16(kBuffer, 0));
            inStream.Read(kBuffer, 0, 2);

            SpellKey11 = NetHelper.FlipBytes(BitConverter.ToUInt16(kBuffer, 0));
            inStream.Read(kBuffer, 0, 2);
            SpellKey12 = NetHelper.FlipBytes(BitConverter.ToUInt16(kBuffer, 0));
            inStream.Read(kBuffer, 0, 2);
            SpellKey13 = NetHelper.FlipBytes(BitConverter.ToUInt16(kBuffer, 0));
            inStream.Read(kBuffer, 0, 2);
            SpellKey14 = NetHelper.FlipBytes(BitConverter.ToUInt16(kBuffer, 0));
            inStream.Read(kBuffer, 0, 2);
            SpellKey15 = NetHelper.FlipBytes(BitConverter.ToUInt16(kBuffer, 0));
            inStream.Read(kBuffer, 0, 2);

            SpellKey16 = NetHelper.FlipBytes(BitConverter.ToUInt16(kBuffer, 0));
            inStream.Read(kBuffer, 0, 2);
            SpellKey17 = NetHelper.FlipBytes(BitConverter.ToUInt16(kBuffer, 0));
            inStream.Read(kBuffer, 0, 2);
            SpellKey18 = NetHelper.FlipBytes(BitConverter.ToUInt16(kBuffer, 0));
            inStream.Read(kBuffer, 0, 2);
            SpellKey19 = NetHelper.FlipBytes(BitConverter.ToUInt16(kBuffer, 0));
            inStream.Read(kBuffer, 0, 2);
            SpellKey20 = NetHelper.FlipBytes(BitConverter.ToUInt16(kBuffer, 0));
            inStream.Read(kBuffer, 0, 2);

            SpellKey21 = NetHelper.FlipBytes(BitConverter.ToUInt16(kBuffer, 0));
            inStream.Read(kBuffer, 0, 2);
            SpellKey22 = NetHelper.FlipBytes(BitConverter.ToUInt16(kBuffer, 0));
            inStream.Read(kBuffer, 0, 2);
            SpellKey23 = NetHelper.FlipBytes(BitConverter.ToUInt16(kBuffer, 0));
            inStream.Read(kBuffer, 0, 2);
            SpellKey24 = NetHelper.FlipBytes(BitConverter.ToUInt16(kBuffer, 0));
            inStream.Read(kBuffer, 0, 2);
            SpellKey25 = NetHelper.FlipBytes(BitConverter.ToUInt16(kBuffer, 0));
            inStream.Read(kBuffer, 0, 2);

            SpellKey26 = NetHelper.FlipBytes(BitConverter.ToUInt16(kBuffer, 0));
            inStream.Read(kBuffer, 0, 2);
            SpellKey27 = NetHelper.FlipBytes(BitConverter.ToUInt16(kBuffer, 0));
            inStream.Read(kBuffer, 0, 2);
            SpellKey28 = NetHelper.FlipBytes(BitConverter.ToUInt16(kBuffer, 0));
            inStream.Read(kBuffer, 0, 2);
            SpellKey29 = NetHelper.FlipBytes(BitConverter.ToUInt16(kBuffer, 0));
            inStream.Read(kBuffer, 0, 2);
            SpellKey30 = NetHelper.FlipBytes(BitConverter.ToUInt16(kBuffer, 0));
            inStream.Read(kBuffer, 0, 2);

            SpellKey31 = NetHelper.FlipBytes(BitConverter.ToUInt16(kBuffer, 0));
            inStream.Read(kBuffer, 0, 2);
            SpellKey32 = NetHelper.FlipBytes(BitConverter.ToUInt16(kBuffer, 0));
            inStream.Read(kBuffer, 0, 2);
            SpellKey33 = NetHelper.FlipBytes(BitConverter.ToUInt16(kBuffer, 0));
            inStream.Read(kBuffer, 0, 2);
            SpellKey34 = NetHelper.FlipBytes(BitConverter.ToUInt16(kBuffer, 0));
            inStream.Read(kBuffer, 0, 2);
            SpellKey35 = NetHelper.FlipBytes(BitConverter.ToUInt16(kBuffer, 0));
            inStream.Read(kBuffer, 0, 2);

            SpellKey36 = NetHelper.FlipBytes(BitConverter.ToUInt16(kBuffer, 0));
            inStream.Read(kBuffer, 0, 2);
            SpellKey37 = NetHelper.FlipBytes(BitConverter.ToUInt16(kBuffer, 0));
            inStream.Read(kBuffer, 0, 2);
            SpellKey38 = NetHelper.FlipBytes(BitConverter.ToUInt16(kBuffer, 0));
            inStream.Read(kBuffer, 0, 2);
            SpellKey39 = NetHelper.FlipBytes(BitConverter.ToUInt16(kBuffer, 0));
            inStream.Read(kBuffer, 0, 2);
            SpellKey40 = NetHelper.FlipBytes(BitConverter.ToUInt16(kBuffer, 0));
            inStream.Read(kBuffer, 0, 2);

            inStream.Read(stringBuffer, 0, 32);
            CabalName = Encoding.ASCII.GetString(stringBuffer).Split((Char)0)[0];

            inStream.Read(stringBuffer, 0, 8);
            CabalTag = Encoding.ASCII.GetString(stringBuffer).Split((Char)0)[0];

            inStream.Seek(7, SeekOrigin.Current);
            OpLevel = (Byte)inStream.ReadByte();

            inStream.Read(expBuffer, 0, 4);
            PlayerFlags = (PlayerFlag)NetHelper.FlipBytes(BitConverter.ToUInt32(expBuffer, 0));

			SpellTrees = new SpellTreeCollection
                         {
                             new SpellTree(SpellManager.SpellTrees.FindById(List1), ListLevel1),
                             new SpellTree(SpellManager.SpellTrees.FindById(List2), ListLevel2),
                             new SpellTree(SpellManager.SpellTrees.FindById(List3), ListLevel3),
                             new SpellTree(SpellManager.SpellTrees.FindById(List4), ListLevel4),
                             new SpellTree(SpellManager.SpellTrees.FindById(List5), ListLevel5),
                             new SpellTree(SpellManager.SpellTrees.FindById(List6), ListLevel6),
                             new SpellTree(SpellManager.SpellTrees.FindById(List7), ListLevel7),
                             new SpellTree(SpellManager.SpellTrees.FindById(List8), ListLevel8),
                             new SpellTree(SpellManager.SpellTrees.FindById(List9), ListLevel9), 
                             new SpellTree(SpellManager.SpellTrees.FindById(List10), ListLevel10)
                         };

            SpellTrees.RemoveAll(spellTrees => spellTrees.TreeSpells == null);
        }

		public static void LoadFilteredNames()
		{
			try
			{
				using (FileStream fs = new FileStream("Namefilter.txt", FileMode.Open))
				{
					using (StreamReader sr = new StreamReader(fs))
					{
						while (!sr.EndOfStream)
						{
							FilteredNames.Add(sr.ReadLine());
						}
					}
				}
			}
			catch (Exception ex)
			{
				Program.ServerForm.MainLog.WriteMessage("Error loading name filter.", Color.Red);
				Program.ServerForm.MainLog.WriteMessage(ex.Message, Color.Red);
			}
		}

        public static Boolean IsNameTaken(String name)
        {
            return MySQL.Character.FindByName(name.Escape()).Rows.Count > 0;
        }

        public static Boolean IsNameValid(String name, Boolean allowAllCharacters)
        {
            name = name.Escape();
            Regex reg = new Regex("^[a-zA-Z]*[_]?[a-zA-Z]*$");

            if (FilteredNames.Any(filteredName => name.ToLower().Contains(filteredName))) return false;
            return (reg.IsMatch(name) && !allowAllCharacters) && (name.Length >= 3 && name.Length < 12);
        }

	    private static Boolean IsInSlot(Int32 accountId, Byte slot)
	    {
            return MySQL.Character.FindByAccountIdAndSlot(accountId, slot).Rows.Count > 0;
        }

        public static Character LoadByName(Player player, String name)
        {
			DataTable table = MySQL.Character.FindByName(name.Escape());
            return table.Rows.Count <= 0 ? null : new Character(player, table.Rows[0]);
        }

        public static Character LoadByNameAndAccountId(Player player, String name)
        {
	        DataTable table = MySQL.Character.FindByNameAndAccountId(name.Escape(), player.AccountId);
            return table.Rows.Count <= 0 ? null : new Character(player, table.Rows[0]);
        }

        public static SaveError Save(Player player, Character clientCharacter)
        {
            lock (PlayerManager.Players.SyncRoot)
            {
                Boolean isNew = false;
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

                    if (player.IsAdmin)
                    {
                        if (player.Admin == AdminLevel.Staff)
                        {
                            tCharacter.OpLevel = 3;
                        }
                        else if (player.Admin == AdminLevel.Developer)
                        {
                            tCharacter.OpLevel = 5;
                        }
                        else
                        {
                            tCharacter.OpLevel = 1;
                        }
                    }

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
                    if (clientCharacter.CabalId != 0)
                    {
                        tCharacter.CabalId = clientCharacter.CabalId;
                        tCharacter.CabalName = clientCharacter.CabalName;
                        tCharacter.CabalTag = clientCharacter.CabalTag;
                    }
                }

                if (clientCharacter != null)
                {
                    if (player.IsAdmin)
                    {
                        if (player.Admin == AdminLevel.Staff)
                        {
                            tCharacter.OpLevel = 3;
                        }
                        else if (player.Admin == AdminLevel.Developer)
                        {
                            tCharacter.OpLevel = 5;
                        }
                        else
                        {
                            tCharacter.OpLevel = 1;
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

                    if ((clientCharacter.ListLevel1 < tCharacter.ListLevel1 || clientCharacter.ListLevel2 < tCharacter.ListLevel2 || clientCharacter.ListLevel3 < tCharacter.ListLevel3 || clientCharacter.ListLevel4 < tCharacter.ListLevel4 || clientCharacter.ListLevel5 < tCharacter.ListLevel5 || clientCharacter.ListLevel6 < tCharacter.ListLevel6 || clientCharacter.ListLevel7 < tCharacter.ListLevel7 || clientCharacter.ListLevel8 < tCharacter.ListLevel8 || clientCharacter.ListLevel9 < tCharacter.ListLevel9 || clientCharacter.ListLevel10 < tCharacter.ListLevel10) && !player.IsAdmin)
                    {
                        Program.ServerForm.CheatLog.WriteMessage(String.Format("[List Hack] AID: {0}, {1} ({2}), {3}, {4}, {5}, {6}", player.AccountId, player.Username, tCharacter.Name, clientCharacter.ListLevel1, tCharacter.ListLevel1, clientCharacter.ListLevel2, tCharacter.ListLevel2), Color.Red);
                        
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

                Int32 numPicks = tCharacter.ListLevel1 + tCharacter.ListLevel2 + tCharacter.ListLevel3 + tCharacter.ListLevel4 + tCharacter.ListLevel5 + tCharacter.ListLevel6 + tCharacter.ListLevel7 + tCharacter.ListLevel8 + tCharacter.ListLevel9 + tCharacter.ListLevel10;
                numPicks = (numPicks - SpellManager.GetNumLists(tCharacter.Class));
                                
                if ((numPicks < 0 || (tCharacter.Level * 2) < numPicks) && !(player.IsAdmin || player.Admin >= AdminLevel.Tester))
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
        }

	    private static UInt64 GetLevelExperience(Int32 level)
        {
            if (level <= 0 || level > 25) return 0;

            UInt64 experience = LevelExp[level - 1] + (LevelExp[level - 1] * 4);
            experience += (experience * 4);
            experience = experience << 2;

            return experience;
        }

	    private static void ResetCharacterLists(Character character)
        {
            character.SpellKey1 = 0;
            character.SpellKey2 = 0;
            character.SpellKey3 = 0;
            character.SpellKey4 = 0;
            character.SpellKey5 = 0;
            character.SpellKey6 = 0;
            character.SpellKey7 = 0;
            character.SpellKey8 = 0;
            character.SpellKey9 = 0;
            character.SpellKey10 = 0;
            character.SpellKey11 = 0;
            character.SpellKey12 = 0;
            character.SpellKey13 = 0;
            character.SpellKey14 = 0;
            character.SpellKey15 = 0;
            character.SpellKey16 = 0;
            character.SpellKey17 = 0;
            character.SpellKey18 = 0;
            character.SpellKey19 = 0;
            character.SpellKey20 = 0;
            character.SpellKey21 = 0;
            character.SpellKey22 = 0;
            character.SpellKey23 = 0;
            character.SpellKey24 = 0;
            character.SpellKey25 = 0;
            character.SpellKey26 = 0;
            character.SpellKey27 = 0;
            character.SpellKey28 = 0;
            character.SpellKey29 = 0;
            character.SpellKey30 = 0;
            character.SpellKey31 = 0;
            character.SpellKey32 = 0;
            character.SpellKey33 = 0;
            character.SpellKey34 = 0;
            character.SpellKey35 = 0;
            character.SpellKey36 = 0;
            character.SpellKey37 = 0;
            character.SpellKey38 = 0;
            character.SpellKey39 = 0;
            character.SpellKey40 = 0;

            if (character.ListLevel1 > 0) character.ListLevel1 = 1;
            if (character.ListLevel2 > 0) character.ListLevel2 = 1;
            if (character.ListLevel3 > 0) character.ListLevel3 = 1;
            if (character.ListLevel4 > 0) character.ListLevel4 = 1;
            if (character.ListLevel5 > 0) character.ListLevel5 = 1;
            if (character.ListLevel6 > 0) character.ListLevel6 = 1;
            if (character.ListLevel7 > 0) character.ListLevel7 = 1;
            if (character.ListLevel8 > 0) character.ListLevel8 = 1;
            if (character.ListLevel9 > 0) character.ListLevel9 = 1;
            if (character.ListLevel10 > 0) character.ListLevel10 = 1;

            character.SpellPicks = character.Level;
        }

	    private static void ResetCharacterStats(Character character)
        {
            character.Agility = 80;
            character.Constitution = 80;
            character.Memory = 80;
            character.Reasoning = 80;
            character.Discipline = 80;
            character.Empathy = 80;
            character.Intuition = 80;
            character.Presence = 80;
            character.Quickness = 80;
            character.Strength = 80;
            character.SpentStatPoints = 0;
            character.BonusStatPointsSpent = 0;
        }
    }
}

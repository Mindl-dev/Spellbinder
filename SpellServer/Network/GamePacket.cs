using Helper;
using Helper.Math;
using Helper.Network;
using Org.BouncyCastle.Security.Certificates;
using SharpDX;
using SpellServer.GamePacket.Incoming;
using SpellServer.GamePacket.Outgoing;
using SpellServer.Sound;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Net;
using System.Numerics;
using System.Text;
using System.Threading;
using ZstdSharp.Unsafe;
using static Google.Protobuf.Reflection.SourceCodeInfo.Types;
using static Mysqlx.Crud.Order.Types;
using static SpellServer.ArenaPlayer;
using Color = System.Drawing.Color;
using OrientedBoundingBox = Helper.Math.OrientedBoundingBox;

namespace SpellServer
{

    namespace GamePacket
    {
        namespace Incoming
        {
            public static class Arena
            {
                public static void Jump(SpellServer.Player player, MemoryStream inStream, bool UDP = false)
                {
                    if (player.ActiveArena == null || player.ActiveArenaPlayer == null || !player.IsAdmin) return;

                    Byte[] tBuffer = new Byte[2];
                    inStream.Seek(2, SeekOrigin.Begin);
                    inStream.Read(tBuffer, 0, 2);

                    Int16 targetId = NetHelper.FlipBytes(BitConverter.ToInt16(tBuffer, 0));

                    Network.Send(player, Outgoing.Arena.PlayerJump(player.ActiveArenaPlayer, targetId, UDP));
                }
                public static void God(SpellServer.Player player, MemoryStream inStream, bool UDP = false)
                {
                    if (player.ActiveArena == null || player.ActiveArenaPlayer == null || !player.IsAdmin) return;

                    inStream.Seek(3, SeekOrigin.Begin);

                    Boolean godStatus = Convert.ToBoolean(inStream.ReadByte());

                    if (godStatus)
                    {
                        player.ActiveArenaPlayer.SpecialFlags |= ArenaPlayer.SpecialFlag.God;
                    }
                    else
                    {
                        player.ActiveArenaPlayer.SpecialFlags &= ~ArenaPlayer.SpecialFlag.God;
                    }

                    Network.Send(player, Outgoing.Arena.PlayerGod(player.ActiveArenaPlayer, godStatus, UDP));
                }
                public static void Yank(SpellServer.Player player, MemoryStream inStream, bool UDP = false)
                {
                    if (player.ActiveArena == null || player.ActiveArenaPlayer == null || !player.IsAdmin) return;

                    inStream.Seek(3, SeekOrigin.Begin);
                    Byte targetId = (Byte)inStream.ReadByte();

                    ArenaPlayer targetArenaPlayer = player.ActiveArena.ArenaPlayers.FindById(targetId);

                    if (targetArenaPlayer != null)
                    {
                        player.ActiveArena.PlayerYank(player, targetArenaPlayer, player.ActiveArenaPlayer.ArenaPlayerId, player.ActiveArenaPlayer.Location);
                    }
                }
                public static void PlayerMoveState(SpellServer.Player player, MemoryStream inStream, bool UDP = false)
                {
                    if (player.ActiveArena == null || player.ActiveArenaPlayer == null || player.Flags.HasFlag(PlayerFlag.Hidden)) return;

                    inStream.Seek(2, SeekOrigin.Begin);

                    byte[] data = new byte[12];
                    inStream.Read(data, 0, 12);

                    ushort rawWord = NetHelper.FlipBytes(BitConverter.ToUInt16(data, 0)); // Note: check endianness
                    int elementId = (rawWord >> 9) & 0x03;
                    int rawAngle = rawWord & 0x0FFF;
                    
                    float direction = MathHelper.DirectionToRadians(rawAngle);

                    ushort rawZ = NetHelper.FlipBytes(BitConverter.ToUInt16(data, 2));
                    int zPos = rawZ & 0x7FF;
                    if ((rawZ & 0x800) != 0) zPos = -zPos; // Sign bit at 0x800

                    int speedScalar = (rawZ >> 12) & 0x0F;

                    byte mSpeed = (byte)((speedScalar / 15.0f) * 255);

                    int xPos = NetHelper.FlipBytes(BitConverter.ToUInt16(data, 4)) & 0x1FFF;
                    int yRaw = NetHelper.FlipBytes(BitConverter.ToUInt16(data, 6));
                    int yPos = yRaw & 0x1FFF;

                    bool isSpecialState = (yRaw & 0x8000) != 0;

                    byte originalByte7 = data[7];

                    byte byte7 = (byte)(yRaw >> 8);
                    int accel = (byte7 >> 3) & 0x03;
                    int flags = byte7 & 0x07;
                    int elementFromFlags = (byte7 >> 5) & 0x03;

                    ArenaPlayer.StatusFlag statusFlags = ((ArenaPlayer.StatusFlag)flags) & ~ArenaPlayer.StatusFlag.Hurt;

                    if (player.ActiveArenaPlayer.StatusFlags.HasFlag(ArenaPlayer.StatusFlag.Hurt))
                    {
                        statusFlags |= ArenaPlayer.StatusFlag.Hurt;
                    }

                    byte statusPart = (byte)player.ActiveArenaPlayer.StatusFlags;

                    SharpDX.Vector3 location = new SharpDX.Vector3(xPos, yPos, zPos) - ArenaPlayer.PlayerOrigin;

                    data[7] = (byte)((originalByte7 & 0xF8) | (statusPart & 0x07));

                    player.ActiveArena.PlayerMove(player.ActiveArenaPlayer, statusFlags, mSpeed, location, direction);

                    Network.SendToArena(player.ActiveArenaPlayer, Outgoing.Arena.PlayerMoveState(player.ActiveArenaPlayer, data, UDP), false);

                }
                /*public static void PlayerMoveState(SpellServer.Player player, MemoryStream inStream, bool UDP = false)
                {
                    if (player.ActiveArena == null || player.ActiveArenaPlayer == null || player.Flags.HasFlag(PlayerFlag.Hidden)) return;

                    Byte[] tBuffer = new Byte[2];
                    inStream.Seek(2, SeekOrigin.Begin);

                    ArenaPlayer.StatusFlag statusFlags = ((ArenaPlayer.StatusFlag)inStream.ReadByte()) & ~ArenaPlayer.StatusFlag.Hurt;

                    if (player.ActiveArenaPlayer.StatusFlags.HasFlag(ArenaPlayer.StatusFlag.Hurt))
                    {
                        statusFlags |= ArenaPlayer.StatusFlag.Hurt;
                    }

                    Byte mSpeed = (Byte) inStream.ReadByte();

                    inStream.Read(tBuffer, 0, 2);
                    Int16 zPos = (Int16) (NetHelper.FlipBytes(BitConverter.ToInt16(tBuffer, 0)) & 0xFFF);
                    zPos = zPos > 0x7FF ? (Int16) (-(zPos & 0x7FF)) : zPos;

                    inStream.Read(tBuffer, 0, 2);
                    Int16 xPos = NetHelper.FlipBytes(BitConverter.ToInt16(tBuffer, 0));

                    inStream.Read(tBuffer, 0, 2);
                    Int16 yPos = NetHelper.FlipBytes(BitConverter.ToInt16(tBuffer, 0));

                    inStream.Read(tBuffer, 0, 2);
                    Single direction = MathHelper.DirectionToRadians(NetHelper.FlipBytes(BitConverter.ToInt16(tBuffer, 0)));

                    SharpDX.Vector3 location = new SharpDX.Vector3(xPos, yPos, zPos) - ArenaPlayer.PlayerOrigin;

                    Byte[] relayBuffer = new Byte[12];
                    inStream.Seek(2, SeekOrigin.Begin);
                    inStream.Read(relayBuffer, 0, 12);
                    relayBuffer[0] = (Byte)player.ActiveArenaPlayer.StatusFlags;

                    player.ActiveArena.PlayerMove(player.ActiveArenaPlayer, statusFlags, mSpeed, location, direction);

                    Network.SendToArena(player.ActiveArenaPlayer, Outgoing.Arena.PlayerMoveState(player.ActiveArenaPlayer, relayBuffer, UDP), false);
                }*/
                public static void PlayerMoveStateShort(SpellServer.Player player, MemoryStream inStream, bool UDP = false)
                {
                    if (player.ActiveArena == null || player.ActiveArenaPlayer == null || player.Flags.HasFlag(PlayerFlag.Hidden)) return;

                    inStream.Seek(2, SeekOrigin.Begin);
                    byte[] data = new byte[8];
                    inStream.Read(data, 0, 8);

                    ushort rawAngleWord = NetHelper.FlipBytes(BitConverter.ToUInt16(data, 0));
                    int rawAngle = rawAngleWord & 0x0FFF;
                    player.ActiveArenaPlayer.Direction = MathHelper.DirectionToRadians(rawAngle);

                    byte stateByte = data[2];
                    ArenaPlayer.StatusFlag statusFlags = (ArenaPlayer.StatusFlag)(stateByte & 0x07);

                    if (player.ActiveArenaPlayer.StatusFlags.HasFlag(ArenaPlayer.StatusFlag.Hurt))
                    {
                        statusFlags |= ArenaPlayer.StatusFlag.Hurt;
                    }

                    byte byte5 = data[5];
                    int speedScalar = byte5 & 0x0F;
                    byte mSpeed = (byte)((speedScalar / 15.0f) * 255);

                    if ((byte5 & 0xF0) != 0)
                    {
                        // You could map these to custom server flags like IsTouchingWall or IsInTransition
                    }

                    statusFlags &= ~ArenaPlayer.StatusFlag.Hurt;
                    if (player.ActiveArenaPlayer.StatusFlags.HasFlag(ArenaPlayer.StatusFlag.Hurt))
                    {
                        statusFlags |= ArenaPlayer.StatusFlag.Hurt;
                    }

                    player.ActiveArena.PlayerMove(player.ActiveArenaPlayer, statusFlags, mSpeed, player.ActiveArenaPlayer.Location, player.ActiveArenaPlayer.Direction);

                    Network.SendToArena(player.ActiveArenaPlayer, Outgoing.Arena.PlayerMoveStateShort(player.ActiveArenaPlayer, data, UDP), false);
                }
                /*public static void PlayerMoveStateShort(SpellServer.Player player, MemoryStream inStream, bool UDP = false)
                {
                    if (player.ActiveArena == null || player.ActiveArenaPlayer == null || player.Flags.HasFlag(PlayerFlag.Hidden)) return;

                    Byte[] tBuffer = new Byte[2];
                    inStream.Seek(2, SeekOrigin.Begin);

                    player.ActiveArenaPlayer.Direction = MathHelper.DirectionToRadians(NetHelper.FlipBytes(BitConverter.ToInt16(tBuffer, 0)));

                    Byte[] relayBuffer = new Byte[8];
                    inStream.Seek(2, SeekOrigin.Begin);
                    inStream.Read(relayBuffer, 0, 8);

                    Network.SendToArena(player.ActiveArenaPlayer, Outgoing.Arena.PlayerMoveStateShort(player.ActiveArenaPlayer, relayBuffer, UDP), false);
                }*/
                public static void TappedAtShrine(SpellServer.Player player)
                {
                    if (player.ActiveArena == null || player.ActiveArenaPlayer == null) return;

                    player.ActiveArena.TappedAtShrine(player.ActiveArenaPlayer);   
                }      
                public static void CalledGhost(SpellServer.Player player, MemoryStream inStream, bool UDP = false)
                {
                    if (player.ActiveArena == null || player.ActiveArenaPlayer == null) return;

                    inStream.Seek(5, SeekOrigin.Begin);
                    Byte targetId = (Byte)inStream.ReadByte();

                    ArenaPlayer targetArenaPlayer = player.ActiveArena.ArenaPlayers.FindById(targetId);

                    Byte[] relayBuffer = new Byte[10];
                    inStream.Seek(2, SeekOrigin.Begin);
                    inStream.Read(relayBuffer, 0, 10);

                    Program.ServerForm.MainLog.WriteMessage($"targetname: {targetArenaPlayer.ActiveCharacter.Name}, targetid: {targetArenaPlayer.ArenaPlayerId.ToString()}", Color.Red);

                    if (targetArenaPlayer != null)
                    {
                        player.ActiveArena.CalledGhost(player.ActiveArenaPlayer, targetArenaPlayer, relayBuffer); 
                    }
                }
                public static void BiasedPool(SpellServer.Player player, MemoryStream inStream, bool UDP = false)
                {
                    if (player.ActiveArena == null || player.ActiveArenaPlayer == null) return;

                    inStream.Seek(2, SeekOrigin.Begin);

                    Byte poolTeam = (Byte)inStream.ReadByte();

                    player.ActiveArena.BiasedPool(player.ActiveArenaPlayer, poolTeam);
                }
                public static void BiasedShrine(SpellServer.Player player, MemoryStream inStream, bool UDP = false)
                {
                    if (player.ActiveArena == null || player.ActiveArenaPlayer == null) return;

                    inStream.Seek(2, SeekOrigin.Begin);

                    Byte shrineTeam = (Byte)inStream.ReadByte();

                    player.ActiveArena.BiasedShrine(player.ActiveArenaPlayer, shrineTeam);
                }
                public static void CastEffect(SpellServer.Player player, MemoryStream inStream, bool UDP = false)
                {
                    if (player.ActiveArena == null || player.ActiveArenaPlayer == null) return;

                    Byte[] tBuffer = new Byte[2];
                    inStream.Seek(2, SeekOrigin.Begin);

                    inStream.Read(tBuffer, 0, 2);
                    Int16 spellId = NetHelper.FlipBytes(BitConverter.ToInt16(tBuffer, 0));

                    Spell spell = SpellManager.Spells[spellId];
                    if (spell == null) return;

                    if (player.ActiveArena.CastEffect(player.ActiveArenaPlayer, spell))
                    {
                        Network.SendToArena(player.ActiveArenaPlayer, Outgoing.Arena.CastEffect(player.ActiveArenaPlayer, spellId, UDP), false);
                    }
                }
                public static void CastTargeted(SpellServer.Player player, MemoryStream inStream, bool UDP = false)
                {
                    if (player.ActiveArena == null || player.ActiveArenaPlayer == null) return;

                    Byte[] tBuffer = new Byte[2];
                    inStream.Seek(2, SeekOrigin.Begin);

                    inStream.Read(tBuffer, 0, 2);
                    Int16 spellId = NetHelper.FlipBytes(BitConverter.ToInt16(tBuffer, 0));

                    inStream.Seek(5, SeekOrigin.Current);
                    Byte targetId = (Byte)inStream.ReadByte();

                    inStream.Seek(8, SeekOrigin.Current);
                    Boolean isResisted = Convert.ToBoolean((Byte)inStream.ReadByte());

                    Byte[] relayBuffer = new Byte[28];
                    inStream.Seek(2, SeekOrigin.Begin);
                    inStream.Read(relayBuffer, 0, 28);

                    Spell spell = SpellManager.Spells[spellId];
                    if (spell == null) return;

                    ArenaPlayer targetArenaPlayer = player.ActiveArena.ArenaPlayers.FindById(targetId);
                    if (targetArenaPlayer == null) return;

                    if (isResisted)
                    {
                        relayBuffer[5] = 0;

                        targetArenaPlayer.IsInCombat = true;
                        player.ActiveArenaPlayer.IsInCombat = true;

                        Network.Send(targetArenaPlayer.WorldPlayer, Outgoing.Arena.CastTargeted(player.ActiveArenaPlayer, relayBuffer, UDP));
                    }
                    else
                    {
                        if (player.ActiveArena.CastTargeted(player.ActiveArenaPlayer, targetArenaPlayer, spell))
                        {
                            Network.SendToArena(player.ActiveArenaPlayer, Outgoing.Arena.CastTargeted(player.ActiveArenaPlayer, relayBuffer, UDP), false);
                        }
                    }
                }
                public static void CastRune(SpellServer.Player player, MemoryStream inStream, bool UDP = false)
                {
                    if (player.ActiveArena == null || player.ActiveArenaPlayer == null) return;

                    Byte[] tBuffer = new Byte[2];
                    inStream.Seek(2, SeekOrigin.Begin);

                    inStream.Read(tBuffer, 0, 2);
                    Int16 spellId = NetHelper.FlipBytes(BitConverter.ToInt16(tBuffer, 0));

                    inStream.Read(tBuffer, 0, 2);
                    Int16 objectId = NetHelper.FlipBytes(BitConverter.ToInt16(tBuffer, 0));

                    inStream.Read(tBuffer, 0, 2);
                    Int16 xPos = NetHelper.FlipBytes(BitConverter.ToInt16(tBuffer, 0));

                    inStream.Read(tBuffer, 0, 2);
                    Int16 yPos = NetHelper.FlipBytes(BitConverter.ToInt16(tBuffer, 0));

                    inStream.Read(tBuffer, 0, 2);
                    Int16 zPos = NetHelper.FlipBytes(BitConverter.ToInt16(tBuffer, 0));
                    zPos = zPos > 0x7FF ? (Int16) (-((zPos & 0x7FF) ^ 0x7FF)) : zPos;

                    inStream.Read(tBuffer, 0, 2);
                    Single fDirection = MathHelper.DirectionToRadians(NetHelper.FlipBytes(BitConverter.ToInt16(tBuffer, 0)));                 

                    Byte[] relayBuffer = new Byte[22];
                    inStream.Seek(2, SeekOrigin.Begin);
                    inStream.Read(relayBuffer, 0, 22);

                    Spell spell = SpellManager.Spells[spellId];
                    if (spell == null) return;

                    Rune rune = new Rune(objectId, player.ActiveArenaPlayer, spell, new SharpDX.Vector3(xPos, yPos, zPos), fDirection, relayBuffer);

                    Program.ServerForm.MainLog.WriteMessage($"rune id: {objectId.ToString()}", Color.Red);

                    if (player.ActiveArena.CastRune(player.ActiveArenaPlayer, rune))
                    {
                        Network.SendToArena(player.ActiveArenaPlayer, Outgoing.Arena.CastRune(player.ActiveArenaPlayer, relayBuffer, UDP), false);
                    }
                }
                public static void CastBolt(SpellServer.Player player, MemoryStream inStream, bool UDP = false)
                {
                    if (player.ActiveArena == null || player.ActiveArenaPlayer == null) return;

                    Byte[] tBuffer = new Byte[2];
                    inStream.Seek(2, SeekOrigin.Begin);

                    inStream.Read(tBuffer, 0, 2);
                    Int16 spellId = BitConverter.ToInt16(tBuffer, 0);

                    inStream.Seek(5, SeekOrigin.Current);
                    Byte targetId = (Byte)inStream.ReadByte();

                    inStream.Read(tBuffer, 0, 2);
                    Int16 distance = NetHelper.FlipBytes(BitConverter.ToInt16(tBuffer, 0));

                    Byte[] relayBuffer = new Byte[34];
                    inStream.Seek(2, SeekOrigin.Begin);
                    inStream.Read(relayBuffer, 0, 34);

                    Spell spell = SpellManager.Spells[spellId];
                    if (spell == null) return;

                    ArenaPlayer targetArenaPlayer = player.ActiveArena.ArenaPlayers.FindById(targetId);
                                    }
                public static void CastProjectile(SpellServer.Player player, MemoryStream inStream, bool UDP = false)
                {
                    if (player.ActiveArena == null || player.ActiveArenaPlayer == null) return;

                    Byte[] tBuffer = new Byte[2];
                    inStream.Seek(2, SeekOrigin.Begin);

                    inStream.Read(tBuffer, 0, 2);
                    Int16 spellId = NetHelper.FlipBytes(BitConverter.ToInt16(tBuffer, 0));

                    inStream.Read(tBuffer, 0, 2);
                    Int16 xPos = NetHelper.FlipBytes(BitConverter.ToInt16(tBuffer, 0));

                    inStream.Read(tBuffer, 0, 2);
                    Int16 yPos = NetHelper.FlipBytes(BitConverter.ToInt16(tBuffer, 0));

                    inStream.Read(tBuffer, 0, 2);
                    Int16 zPos = NetHelper.FlipBytes(BitConverter.ToInt16(tBuffer, 0));
                    zPos = zPos > 0x7FF ? (Int16) (-((zPos & 0x7FF) ^ 0x7FF)) : zPos;

                    inStream.Read(tBuffer, 0, 2);
                    Single fDirection = MathHelper.DirectionToRadians(NetHelper.FlipBytes(BitConverter.ToInt16(tBuffer, 0)));
                    ushort Direction = NetHelper.FlipBytes(BitConverter.ToUInt16(tBuffer, 0));

                    Program.ServerForm.MainLog.WriteMessage($"fDirection: {fDirection.ToString()}, Direction: {Direction.ToString()}", Color.Blue);

                    // Read angle as byte (0–255)
                    inStream.Seek(2, SeekOrigin.Current);
                    Int32 rawangle = (Byte)inStream.ReadByte();
                    Int32 angle = rawangle > 0x7F ? (rawangle & 0x7F) ^ 0x7F : -rawangle;
                    Single fAngle = angle;

                    Program.ServerForm.MainLog.WriteMessage($"angleraw: {rawangle.ToString()}, angle > 0x7F ? (angle & 0x7F) ^ 0x7F : -angle;: {angle.ToString()}", Color.Blue);

                    Byte[] relayBuffer = new Byte[16];
                    inStream.Seek(2, SeekOrigin.Begin);
                    inStream.Read(relayBuffer, 0, 16);

                    Spell spell = SpellManager.Spells[spellId];
                    if (spell == null) return;

                    ProjectileGroup projectileGroup = new ProjectileGroup(player.ActiveArenaPlayer, spell, new SharpDX.Vector3(xPos, yPos, zPos), fDirection, fAngle);

                    if (player.ActiveArena.CastProjectile(player.ActiveArenaPlayer, spell, projectileGroup))
                    {
                        Network.SendToArena(player.ActiveArenaPlayer, Outgoing.Arena.CastProjectile(player.ActiveArenaPlayer, relayBuffer, UDP), false);
                    }
                }
                public static void CastWall(SpellServer.Player player, MemoryStream inStream, bool UDP = false)
                {
                    if (player.ActiveArena == null || player.ActiveArenaPlayer == null) return;

                    Byte[] tBuffer = new Byte[2];
                    inStream.Seek(2, SeekOrigin.Begin);

                    inStream.Read(tBuffer, 0, 2);
                    Int16 spellId = NetHelper.FlipBytes(BitConverter.ToInt16(tBuffer, 0));

                    inStream.Read(tBuffer, 0, 2);
                    Int16 objectId = NetHelper.FlipBytes(BitConverter.ToInt16(tBuffer, 0));

                    inStream.Read(tBuffer, 0, 2);
                    Int16 xPos = NetHelper.FlipBytes(BitConverter.ToInt16(tBuffer, 0));

                    inStream.Read(tBuffer, 0, 2);
                    Int16 yPos = NetHelper.FlipBytes(BitConverter.ToInt16(tBuffer, 0));

                    inStream.Read(tBuffer, 0, 2);
                    Int16 zPos = NetHelper.FlipBytes(BitConverter.ToInt16(tBuffer, 0));
                    zPos = zPos > 0x7FF ? (Int16) (-((zPos & 0x7FF) ^ 0x7FF)) : zPos;

                    inStream.Read(tBuffer, 0, 2);
                    Single fDirection = MathHelper.DirectionToRadians(NetHelper.FlipBytes(BitConverter.ToInt16(tBuffer, 0)));

                    Byte[] relayBuffer = new Byte[18];
                    inStream.Seek(2, SeekOrigin.Begin);
                    inStream.Read(relayBuffer, 0, 18);

                    Spell spell = SpellManager.Spells[spellId];
                    if (spell == null) return;

                    Wall wall = new Wall(objectId, player.ActiveArenaPlayer, spell, new SharpDX.Vector3(xPos, yPos, zPos), fDirection, relayBuffer);

                    if (player.ActiveArena.CastWall(player.ActiveArenaPlayer, wall))
                    {
                        Network.SendToArena(player.ActiveArenaPlayer, Outgoing.Arena.CastWall(relayBuffer, UDP), false);
                    }
                }
                public static void CastDispell(SpellServer.Player player, MemoryStream inStream, bool UDP = false)
                {
                    if (player.ActiveArena == null || player.ActiveArenaPlayer == null) return;

                    Byte[] tBuffer = new Byte[2];
                    inStream.Seek(2, SeekOrigin.Begin);

                    inStream.Read(tBuffer, 0, 2);
                    Int16 xPos = NetHelper.FlipBytes(BitConverter.ToInt16(tBuffer, 0));

                    inStream.Read(tBuffer, 0, 2);
                    Int16 yPos = NetHelper.FlipBytes(BitConverter.ToInt16(tBuffer, 0));

                    inStream.Read(tBuffer, 0, 2);
                    Int16 zPos = NetHelper.FlipBytes(BitConverter.ToInt16(tBuffer, 0));
                    zPos = zPos > 0x7FF ? (Int16)(-((zPos & 0x7FF) ^ 0x7FF)) : zPos;
                    //zPos -= 0x44;

                    inStream.Read(tBuffer, 0, 2);
                    Single fDirection = MathHelper.DirectionToRadians(NetHelper.FlipBytes(BitConverter.ToInt16(tBuffer, 0)));

                    inStream.Seek(4, SeekOrigin.Current);

                    inStream.Read(tBuffer, 0, 2);
                    Int16 spellId = NetHelper.FlipBytes(BitConverter.ToInt16(tBuffer, 0));

                    SharpDX.Vector3 vector = new SharpDX.Vector3(xPos, yPos, zPos);

                    Spell spell = SpellManager.Spells[spellId];
                    if (spell == null) return;

                    Wall wall = player.ActiveArena.Walls.FindByVector(player, vector, fDirection, spell);

                    if (wall == null)
                    {
                        if (player.ActiveCharacter.Class == SpellServer.Character.PlayerClass.Runemage)
                        {
                            
                            Program.ServerForm.MainLog.WriteMessage($"spellid: {spell.Id.ToString()}", Color.Red);

                            if (spell.Id == 286)
                            {
                                List<Rune> playerRunes = player.ActiveArena.Runes.FindAll(rune => rune.Owner.ArenaPlayerId == player.PlayerId);

                                foreach (Rune rune in playerRunes)
                                {
                                    if (rune == null) continue;

                                    float distance = SharpDX.Vector3.Distance(rune.Location, player.ActiveArenaPlayer.BoundingBox.Location);

                                    if (distance <= spell.Range)
                                    {
                                        player.ActiveArena.CastDispell(player.ActiveArenaPlayer, rune, spell);
                                    }
                                }
                            }
                            else if (spell.Id == 290)
                            {
                                Rune rune = player.ActiveArena.Runes.FindByVector(player, vector, fDirection, spell);

                                if (rune == null) return;

                                player.ActiveArena.CastDispell(player.ActiveArenaPlayer, rune, spell);
                            }
                        }

                        return;
                    }

                    player.ActiveArena.CastDispell(player.ActiveArenaPlayer, wall, spell);
                }
                public static void ThinDamage(SpellServer.Player player, MemoryStream inStream, bool UDP = false)
                {
                    if (player.ActiveArena == null || player.ActiveArenaPlayer == null) return;

                    Byte[] tBuffer = new Byte[2];
                    inStream.Seek(4, SeekOrigin.Begin);

                    inStream.Read(tBuffer, 0, 2);
                    Int16 thinId = NetHelper.FlipBytes(BitConverter.ToInt16(tBuffer, 0));

                    inStream.Read(tBuffer, 0, 2);
                    Int16 damage = NetHelper.FlipBytes(BitConverter.ToInt16(tBuffer, 0));

                    player.ActiveArena.ThinDamage(player.ActiveArenaPlayer, thinId, damage);
                }
                public static void ActivatedTrigger(SpellServer.Player player, MemoryStream inStream, bool UDP = false)
                {
                    if (player.ActiveArena == null || player.ActiveArenaPlayer == null) return;

                    inStream.Seek(5, SeekOrigin.Begin);
                    Byte triggerId = (Byte)inStream.ReadByte();

                    Trigger trigger = player.ActiveArena.Grid.Triggers[triggerId];
                    if (trigger == null) return;

                    Program.ServerForm.MainLog.WriteMessage($"trigger id: {triggerId.ToString()}", Color.Red);

                    player.ActiveArena.ActivatedTrigger(player.ActiveArenaPlayer, trigger);
                }
            }   

            public static class Character
            {
                public static void Save(SpellServer.Player player, MemoryStream inStream, bool UDP = false)
                {
                    SpellServer.Character.Save(player, new SpellServer.Character(inStream));
                }
                public static void Delete(SpellServer.Player player, MemoryStream inStream, bool UDP = false)
                {
                    if (player.TableId != 0) return;

                    Byte[] dbuffer = new Byte[20];
                    inStream.Seek(2, SeekOrigin.Current);
                    inStream.Read(dbuffer, 0, 20);
                    String name = Encoding.ASCII.GetString(dbuffer).Split((Char) 0)[0].Escape();

                    SpellServer.Character deleteCharacter = SpellServer.Character.LoadByNameAndAccountId(player, name);

                    if (deleteCharacter != null)
                    {
                        Program.ServerForm.MiscLog.WriteMessage(String.Format("[Character Delete] {{{0}}} {1} ({2}), Level: {3}, Class: {4}, EXP: {5}, IP: {6}, Serial: {7}", player.AccountId, player.Username, deleteCharacter.Name, deleteCharacter.Level, deleteCharacter.Class, deleteCharacter.Experience, player.IpAddress, player.Serial), Color.Blue);
                        
						MySQL.CharacterStatistics.OverallDeleteByCharId((deleteCharacter.CharacterId));
	                    MySQL.CharacterStatistics.WeeklyDeleteByCharId((deleteCharacter.CharacterId));
	                    MySQL.Character.Delete(player.AccountId, name);
                    }

                    player.ActiveCharacter = null;
                }
            }

            public static class Login
            {
                public static void Authenticate(SpellServer.Player player, MemoryStream inStream, bool UDP = false)
                {
                    Byte[] version = new Byte[4], loginBuffer = new Byte[20], serialBuffer = new Byte[32];

                    inStream.Seek(3, SeekOrigin.Current);
                    inStream.Read(version, 0, 4);
                    inStream.Read(loginBuffer, 0, 20);
                    String password = Encoding.ASCII.GetString(loginBuffer).Split((Char)0)[0];
                    inStream.Read(serialBuffer, 0, 32);
                    String serial = Encoding.ASCII.GetString(serialBuffer).Split((Char)0)[0].Escape();
                    inStream.Seek(224, SeekOrigin.Current);
                    inStream.Read(loginBuffer, 0, 20);
                    String username = Encoding.ASCII.GetString(loginBuffer).Split((Char)0)[0];

                    Subscription.Authenticate(player, username, password, serial, version);
                }

                public static void Disconnect(SpellServer.Player player)
                {
                    player.DisconnectReason = Resources.Strings_Disconnect.Logoff;
                    player.Disconnect = true;
                }
            }

            public static class Player
            {
                
                public static void EstablishDatagram(SpellServer.Player player, MemoryStream inStream, bool UDP = false)
                {
                    //Byte[] buffer = new Byte[40];
                    //inStream.Read(buffer, 0, (int) inStream.Length);

                    //Program.ServerForm.MainLog.WriteMessage(String.Format("[EstablishDatagram-Inbound] {0}", BitConverter.ToString(buffer)), Color.Red);

                    Byte[] ipBuffer = new byte[20];
                    inStream.Read(ipBuffer, 0, 20);
                    String ipAddr = Encoding.ASCII.GetString(ipBuffer).Split((Char)0)[0];

                    inStream.Seek(2, SeekOrigin.Current);

                    Byte[] port = new Byte[2];
                    inStream.Read(port, 0, 2);

                    player.UdpportLE = new byte[] { port[0], port[1] };
                    player.UdpportBE = new byte[] { port[1], port[0] };
                    player.UdpIpAddress = ipAddr;

                    Program.ServerForm.MainLog.WriteMessage($"UdpportLE: {BitConverter.ToString(player.UdpportLE)}, UdpIp: {player.UdpIpAddress}", Color.Red);

                    Network.Send(player, Outgoing.Player.EstablishDatagram(player, UDP), UDP);

                }
                public static void Heartbeat(SpellServer.Player player, MemoryStream inStream, bool UDP = false)
                {
                    Byte[] heartbeatBuffer = new Byte[4];

                    inStream.Seek(2, SeekOrigin.Current);
                    inStream.Read(heartbeatBuffer, 0, 4);

                    player.Heartbeat(NetHelper.FlipBytes(BitConverter.ToUInt32(heartbeatBuffer, 0)));
                }
                public static void HasEnteredWorld(SpellServer.Player player, bool UDP = false)
                {
                    Network.Send(player, Outgoing.Player.HasEnteredWorld());
                }
                public static void EnterWorld(SpellServer.Player player, MemoryStream inStream, bool UDP = false)
                {
                    
                    inStream.Seek(4, SeekOrigin.Current);
                    Byte worldId = (Byte)inStream.ReadByte();
                    Team team = (Team) (Byte)inStream.ReadByte();
                    Byte[] nameBuffer = new Byte[12];
                    inStream.Read(nameBuffer, 0, 12);
                    String charName = Encoding.ASCII.GetString(nameBuffer).Split((Char) 0)[0];

                    switch (worldId)
                    {
                        case 0: // Enter Char Select Screen
                            {
                                SpellServer.World.PlayerEnteredWorld(player, worldId, team, charName);
                                break;
                            }
                        case 255: // Enter Main Lobby
                            {
                                SpellServer.World.PlayerEnteredWorld(player, worldId, team, charName);
                                break;
                            }

                        default:
                        {
                            if (worldId >= 0x65 && worldId <= 0x81)
                            {
                                worldId = (Byte)(worldId - 0x64);

                                //Network.Send(player, Outgoing.Player.EstablishDatagram(player));

                                SpellServer.World.PlayerEnteredWorld(player, worldId, team, charName);
                            }
                            else
                            {
                                SpellServer.World.PlayerEnteredWorld(player, worldId, team, charName);
                            }
                            break;
                        }

                    }

                    /*if ((worldId == 0x00) || (worldId == 0xFF) || (worldId >= 1 && worldId <= 16))
                    {
                        SpellServer.World.PlayerEnteredWorld(player, worldId, team, charName);
                    }
                    else
                    {
                        inStream.Seek(3, SeekOrigin.Current);
                        worldId = (Byte)inStream.ReadByte();
                        inStream.Seek(1, SeekOrigin.Current);
                        team = (Team)(Byte)inStream.ReadByte();
                        nameBuffer = new Byte[12];
                        inStream.Read(nameBuffer, 0, 12);
                        charName = Encoding.ASCII.GetString(nameBuffer).Split((Char)0)[0];

                        Program.ServerForm.MainLog.WriteMessage(String.Format("{0},{1},{2}", worldId.ToString("X2"), team.ToString(), charName), Color.Blue);

                        SpellServer.World.PlayerEnteredWorld(player, worldId, team, charName);
                    }*/
                }
                public static void ExitWorld(SpellServer.Player player, bool UDP = false)
                {
                    if (player.ActiveArena == null || player.ActiveArenaPlayer == null) return;

                    SpellServer.Arena arena = player.ActiveArena;

                    player.ActiveArena.PlayerLeft(player.ActiveArenaPlayer);

                    Thread.Sleep(2000);

                    for (Int32 i = 0; i < arena.ArenaPlayers.Count; i++)
                    {
                        Network.Send(player, Outgoing.Arena.PlayerState(arena.ArenaPlayers[i], UDP));
                    }
                }
                public static void Chat(SpellServer.Player player, MemoryStream inStream, bool UDP = false)
                {
                    Int32 tLen = Convert.ToInt32(inStream.Length) - 10;
                    Byte[] cBuffer = new Byte[tLen];
                    Byte[] tBuffer = new Byte[2];
                    inStream.Seek(4, SeekOrigin.Begin);
                    inStream.Read(tBuffer, 0, 2);
                    Int16 target = NetHelper.FlipBytes(BitConverter.ToInt16(tBuffer, 0));
                    ChatType targetType = (ChatType) (Byte)inStream.ReadByte();
                    inStream.Seek(3, SeekOrigin.Current);
                    inStream.Read(cBuffer, 0, tLen);

                    String message = Encoding.ASCII.GetString(cBuffer).Split((Char) 0)[0];

                    SpellServer.World.ProcessChatMessage(player, target, targetType, message);
                }
                public static void SwitchedToTableOrArena(SpellServer.Player player, MemoryStream inStream, bool UDP = false)
                {
                    inStream.Seek(5, SeekOrigin.Begin);
                    player.TableId = (Byte)inStream.ReadByte();
                }
                public static void InviteToTable(MemoryStream inStream, bool UDP = false)
                {
                    inStream.Seek(2, SeekOrigin.Begin);
                    Byte tableId = (Byte)inStream.ReadByte();
                    inStream.Seek(3, SeekOrigin.Current);

                    Byte[] inviteData = new Byte[64];
                    inStream.Read(inviteData, 0, 64);
                    BitArray bitArray = new BitArray(inviteData);

                    Table targetTable = TableManager.Tables.FindById(tableId);

                    for (Int16 i = 1; i < bitArray.Count; i++)
                    {
                        if (!bitArray[i]) continue;

                        SpellServer.Player targetPlayer = PlayerManager.Players.FindById(i);
                        if (targetPlayer == null || targetTable == null) return;

                        targetTable.InvitePlayerToTable(targetPlayer, inviteData);
                    }
                }
            }

            public static class Study
            {
                public static void RequestCharacterInSlot(SpellServer.Player player, MemoryStream inStream, bool UDP = false)
                {
                    inStream.Seek(22, SeekOrigin.Current);
                    Byte slot = (Byte)inStream.ReadByte();

                    Network.Send(player, Outgoing.Study.SendCharacterInSlot(player, slot, MySQL.Character.FindByAccountIdAndSlot(player.AccountId, slot), UDP));
                }
                public static void IsNameTaken(SpellServer.Player player, MemoryStream inStream, bool UDP = false)
                {
                    Byte[] tBuffer = new Byte[12];
                    inStream.Seek(2, SeekOrigin.Current);
                    inStream.Read(tBuffer, 0, 12);
                    String name = Encoding.ASCII.GetString(tBuffer).Split((Char) 0)[0].Escape();

                    Boolean isTaken = SpellServer.Character.IsNameTaken(name);
                    Network.Send(player, Outgoing.Study.IsNameTaken(player, name, isTaken, UDP));
                }
                public static void IsNameValid(SpellServer.Player player, MemoryStream inStream, bool UDP = false)
                {
                    Byte[] tBuffer = new Byte[12];
                    inStream.Seek(2, SeekOrigin.Current);
                    inStream.Read(tBuffer, 0, 12);
                    String name = Encoding.ASCII.GetString(tBuffer).Split((Char) 0)[0].Escape();

                    Boolean isValid = SpellServer.Character.IsNameValid(name, false);
                    Network.Send(player, Outgoing.Study.IsNameValid(player, name, isValid, UDP));
                }
                public static void HighScores(SpellServer.Player player, MemoryStream inStream, bool UDP = false)
                {
                    inStream.Seek(5, SeekOrigin.Begin);
                    Byte pClassId = (Byte)inStream.ReadByte();
					Network.Send(player, Outgoing.Study.HighScores(pClassId, MySQL.Character.GetHighScoreList(pClassId-1), UDP));
                }
            }

            public static class World
            {
                public static void RequestedPlayer(SpellServer.Player player, MemoryStream inStream, bool UDP = false)
                {
                    Byte[] tBuffer = new Byte[2];
                    inStream.Seek(4, SeekOrigin.Begin);

                    inStream.Read(tBuffer, 0, 2);
                    Int16 tPlayerId = NetHelper.FlipBytes(BitConverter.ToInt16(tBuffer, 0));

                    if (player.ActiveArena != null)
                    {
                        ArenaPlayer arenaPlayer = player.ActiveArena.ArenaPlayers.FindById((Byte)tPlayerId);

                        if (arenaPlayer != null)
                        {
                            Network.Send(player, Outgoing.Arena.PlayerJoin(arenaPlayer), UDP);
                        }
                    }
                    else
                    {
                        SpellServer.Player tPlayer = PlayerManager.Players.FindById(tPlayerId);

                        if (tPlayer != null)
                        {
                            Network.Send(player, Outgoing.World.PlayerJoin(tPlayer), UDP);
                        }
                    }
                }
                public static void RequestedAllPlayers(SpellServer.Player player, bool UDP = false)
                {
                    MemoryStream outStream = null;
                    Int32 j = 0;

                    if (player.IsInArena)
                    {
                        lock (player.ActiveArena.ArenaPlayers.SyncRoot)
                        {
                            for (Int32 i = 0; i < player.ActiveArena.ArenaPlayers.Count; i++)
                            {
                                ArenaPlayer arenaPlayer = player.ActiveArena.ArenaPlayers[i];
                                if (arenaPlayer == null || player.ActiveArenaPlayer == arenaPlayer || arenaPlayer.WorldPlayer.Flags.HasFlag(PlayerFlag.Hidden)) continue;

                                outStream = Outgoing.Arena.ArenaPlayerEnterLarge(arenaPlayer, outStream);

                                if (j++ < 10) continue;

                                Network.Send(player, outStream, UDP);
                                outStream = null;
                                j = 0;
                            }  
                        } 
                    }
                    else
                    {
                        lock (PlayerManager.Players.SyncRoot)
                        {
                            for (Int32 i = 0; i < PlayerManager.Players.Count; i++)
                            {
                                SpellServer.Player p = PlayerManager.Players[i];
                                if (p == null || p == player) continue;

                                if (player.ActiveArena == null)
                                {
                                    if (p.TableId == 0 && p.ActiveArena == null) continue;
                                }
                                else
                                {
                                    if (p.ActiveArena != player.ActiveArena) continue;
                                }

                                if (p.Flags.HasFlag(PlayerFlag.Hidden)) continue;

                                outStream = Outgoing.World.PlayerEnterLarge(p, outStream);

                                if (++j < 10) continue;

                                Network.Send(player, outStream, UDP);
                                outStream = null;
                                j = 0;
                            }
                        }
                    }

                    if (j <= 0 || outStream == null) return;

                    for (Int32 x = 10 - j; x > 0; x--)
                    {
                        for (Int32 r = 1; r <= 20; r++)
                        {
                            outStream.WriteByte(0x00);
                        }
                    }

                    Network.Send(player, outStream);
                }
                public static void RequestedArena(SpellServer.Player player, MemoryStream inStream, bool UDP = false)
                {
                    inStream.Seek(5, SeekOrigin.Begin);
                    Byte arenaId = (Byte)inStream.ReadByte();

                    SpellServer.Arena arena = ArenaManager.Arenas.FindById(arenaId);

                    if (arena != null)
                    {
                        Network.Send(player, Outgoing.World.ArenaCreated(arena, UDP));
                    }
                }
                public static void RequestedAllArenas(SpellServer.Player player, bool UDP = false)
                {
                    MemoryStream outStream = null;
                    Int32 j = 0;

                    lock (ArenaManager.Arenas.SyncRoot)
                    {
                        for (Int32 i = 0; i < ArenaManager.Arenas.Count; i++)
                        {
                            SpellServer.Arena a = ArenaManager.Arenas[i];
                            if (a == null) continue;

                            outStream = Outgoing.World.WorldEnterLarge(a, outStream);

                            if (++j < 4) continue;

                            Network.Send(player, outStream, UDP);
                            outStream = null;
                            j = 0;
                        }
                    }

                    if (j <= 0 || outStream == null) return;

                    for (Int32 x = 4 - j; x > 0; x--)
                    {
                        for (Int32 r = 1; r <= 52; r++)
                        {
                            outStream.WriteByte(0x00);
                        }
                    }

                    Network.Send(player, outStream, UDP);
                    //Network.Send(player, Outgoing.Player.EstablishDatagram(player));
                }
                public static void RequestedArenaStatus(SpellServer.Player player, bool UDP = false)
                {
                    for (int i = 0; i < ArenaManager.Arenas.Count; i++)
                    {
                        SpellServer.Arena arena = ArenaManager.Arenas[i];
                        if (arena == null) continue;

                        Network.Send(player, Outgoing.World.ArenaState(arena, player, UDP));
                    }

                    Network.Send(player, Outgoing.Player.EstablishDatagram(player));

                }
                public static void CreateTable(SpellServer.Player player, MemoryStream inStream, bool UDP = false)
                {
                    inStream.Seek(2, SeekOrigin.Begin);
                    TableType tableType = (TableType) (Byte)inStream.ReadByte();     

                    new Table(player, tableType);
                }
                public static void DeleteTable(MemoryStream inStream, bool UDP = false)
                {
                    inStream.Seek(2, SeekOrigin.Begin);

                    Byte tableId = (Byte)inStream.ReadByte();

                    Table t = TableManager.Tables.FindById(tableId);

                    if (t == null) return;

                    t.Delete = true;
                }
                public static void RequestedAllTables(SpellServer.Player player, bool UDP = false)
                {
                    lock (TableManager.Tables.SyncRoot)
                    {
                        for (Int32 i = 0; i < TableManager.Tables.Count; i++)
                        {
                            Table t = TableManager.Tables[i];
                            if (t == null) continue;

                            Network.Send(player, Outgoing.World.TableCreated(t, UDP));
                        }
                    }

                    TableManager.Tables.ProcessSavedInvites(player);

					SpellServer.World.SendSystemMessage(player, String.Format("Message of the Day: {0}", Properties.Settings.Default.MessageOfTheDay));

                    if (player.ActiveCharacter.AvailableStatPoints > 0)
                    {
                        SpellServer.World.SendSystemMessage(player, "You have unspent stat points.  Go to the website to spend them.");
                    }
                    
                    if (player.Flags.HasFlag(PlayerFlag.ChatDisabled))
                    {
                        SpellServer.World.SendSystemMessage(player, "Your chat is currently disabled. Type !mute to toggle.");
                    }

                    if (player.Flags.HasFlag(PlayerFlag.ExpLocked))
                    {
                        SpellServer.World.SendSystemMessage(player, "Your exp is currently locked. Type !lockexp to toggle.");
                    }

                    if (player.Flags.HasFlag(PlayerFlag.Muted))
                    {
                        SpellServer.World.SendSystemMessage(player, "You are currently muted and may not chat in public channels.");
                    }

                    if (player.Flags.HasFlag(PlayerFlag.MusicDisabled))
                    {
                        SpellServer.World.SendSystemMessage(player, "You currently have streaming music disabled. Type !togglemusic to toggle.");
                    }
                }
                public static void CreateArena(SpellServer.Player player, MemoryStream inStream, bool UDP = false)
                {
                    inStream.Seek(2, SeekOrigin.Begin);

                    UInt32 gridId = (Byte)inStream.ReadByte();
                    Byte levelRange = (Byte)inStream.ReadByte();
                    inStream.Seek(1, SeekOrigin.Current);
                    Byte TeamNumber = (Byte)inStream.ReadByte();

                    Grid grid = GridManager.Grids.FindById(gridId);

                    if (grid != null)
                    {
                        if (TeamNumber == 0x00)
                        { 
                            if (player.PreferredArenaMode == ArenaRuleset.ArenaMode.Custom)
                            {
                                new SpellServer.Arena(player, grid, levelRange, new ArenaRuleset(player.PreferredArenaRules));
                            }
                            else
                            {
                                new SpellServer.Arena(player, grid, levelRange, new ArenaRuleset(player.PreferredArenaMode));
                            }
                        }
                        else
                        {
                            new SpellServer.Arena(player, grid, levelRange, new ArenaRuleset(ArenaRuleset.ArenaMode.TwoTeams));
                        }
                    }
                }
                public static void DeleteArena(SpellServer.Player player, MemoryStream inStream, bool UDP = false)
                {
                    inStream.Seek(2, SeekOrigin.Begin);

                    Byte arenaId = (Byte)inStream.ReadByte();
                    SpellServer.Arena arena = ArenaManager.Arenas.FindById(arenaId);
                    if (arena == null) return;

                    lock (arena.SyncRoot)
                    {
                        if (arena.ArenaPlayers.Count > 0)
                        {
                            if (player.Admin == AdminLevel.Staff || player.Admin == AdminLevel.Developer)
                            {
                                arena.CurrentState = SpellServer.Arena.State.Ended;
                            }
                        }
                        else
                        {
                            arena.CurrentState = SpellServer.Arena.State.Ended;
                        }
                    }
                }
                public static void RequestEnterLarge(SpellServer.Player player)
                {
                    Network.Send(player, Outgoing.Arena.SuccessfulArenaEntry());
                }
            }

            public static class MageHook
            {
                public static void HackNotification(SpellServer.Player player, MemoryStream inStream, bool UDP = false)
                {
                    inStream.Seek(2, SeekOrigin.Begin);
                    Byte hackType = (Byte)inStream.ReadByte();

                    StringBuilder hackString = new StringBuilder();

                    switch (hackType)
                    {
                        case 0:
                        {
                            hackString.Append("[Debugger] ");
                            break;
                        }
                        case 1:
                        {
                            hackString.Append("[Memory Hack] ");
                            break;
                        }
                        default:
                        {
                            hackString.Append("[Unknown Hack] ");
                            break;
                        }
                    }

                    if (player.ActiveCharacter == null)
                    {
                        hackString.Append(String.Format("({0}) {1}", player.AccountId, player.Username));
                    }
                    else
                    {
                        hackString.Append(String.Format("({0}[{1}]) {2}({3})", player.AccountId, player.ActiveCharacter.CharacterId, player.Username, player.ActiveCharacter.Name));
                    }

                    Program.ServerForm.CheatLog.WriteMessage(hackString.ToString(), Color.Red);

					player.DisconnectReason = Resources.Strings_Disconnect.CheatProgram;
                    player.Disconnect = true;
                }
                public static void CheatProgramNotification(SpellServer.Player player, MemoryStream inStream, bool UDP = false)
                {
                    inStream.Seek(2, SeekOrigin.Begin);
                    Byte cheatProgram = (Byte)inStream.ReadByte();
                    Byte cheatType = (Byte)inStream.ReadByte();

                    StringBuilder hackString = new StringBuilder();

                    String programName, cheatTypeName;

                    switch (cheatProgram)
                    {
                        case 0:
                        {
                            programName = "Cheat Engine";
                            break;
                        }
                        case 1:
                        {
                            programName = "Gamehack";
                            break;
                        }
                        case 2:
                        {
                            programName = "GameCheater";
                            break;
                        }
                        case 3:
                        {
                            programName = "TSearch";
                            break;
                        }
                        case 4:
                        {
                            programName = "OllyDBG";
                            break;
                        }
                        case 5:
                        {
                            programName = "WPE Pro";
                            break;
                        }
                        default:
                        {
                            programName = "Unknown";
                            break;
                        }
                    }

                    switch (cheatType)
                    {
                        case 0:
                        {
                            cheatTypeName = "Executable";
                            break;
                        }
                        case 1:
                        {
                            cheatTypeName = "Window";
                            break;
                        }
                        default:
                        {
                            cheatTypeName = "Unknown";
                            break;
                        }
                    }

                    hackString.Append(String.Format("[Cheat Program] "));

                    if (player.ActiveCharacter == null)
                    {
                        hackString.Append(String.Format("({0}) {1} Program: {2}, Type: {3}", player.AccountId, player.Username, programName, cheatTypeName));
                    }
                    else
                    {
                        hackString.Append(String.Format("({0}[{1}]) {2}({3}) Program: {4}, Type: {5}", player.AccountId, player.ActiveCharacter.CharacterId, player.Username, player.ActiveCharacter.Name, programName, cheatTypeName));
                    }

                    Program.ServerForm.CheatLog.WriteMessage(hackString.ToString(), Color.Red);

	                player.DisconnectReason = Resources.Strings_Disconnect.CheatProgram;
                    player.Disconnect = true;
                }
            }
        }

        namespace Outgoing
        {
            public static class Arena
            {
                public static MemoryStream SpawnPlayer(ArenaPlayer arenaPlayer, bool UDP = false)
                {
                    MemoryStream outStream = new MemoryStream();
                    outStream.WriteByte(arenaPlayer.ArenaPlayerId);
                    return outStream;
                }
                public static MemoryStream ArenaPlayerEnterLarge(ArenaPlayer arenaPlayer, MemoryStream outStream, bool UDP = false)
                {

                    if (outStream == null)
                    {
                        outStream = new MemoryStream();
                        outStream.WriteByte(0x00);
                        outStream.WriteByte((Byte)PacketOutFunction.PlayerEnterLarge);
                    }

                    outStream.Write(Encoding.ASCII.GetBytes(arenaPlayer.ActiveCharacter.Name), 0, arenaPlayer.ActiveCharacter.Name.Length);
                    outStream.Seek((12 - arenaPlayer.ActiveCharacter.Name.Length), SeekOrigin.Current);
                    outStream.Write(BitConverter.GetBytes(NetHelper.FlipBytes(arenaPlayer.ArenaPlayerId)), 0, 2);
                    outStream.WriteByte(arenaPlayer.OwnerArena.ArenaId);
                    outStream.WriteByte((Byte) arenaPlayer.ActiveTeam);
                    outStream.WriteByte((Byte) arenaPlayer.ActiveCharacter.Class);
                    outStream.WriteByte(arenaPlayer.ActiveCharacter.Level);
                    outStream.WriteByte(arenaPlayer.ActiveCharacter.OpLevel);
                    outStream.WriteByte(0x00);
                    return outStream;
                }
                public static MemoryStream PlayerJump(ArenaPlayer arenaPlayer, Int16 targetId, bool UDP = false)
                {
                    MemoryStream outStream = new MemoryStream();
                    outStream.WriteByte(arenaPlayer.ArenaPlayerId);
                    outStream.WriteByte((Byte)PacketOutFunction.PlayerJump);
                    outStream.Write(BitConverter.GetBytes(NetHelper.FlipBytes(targetId)), 0, 2);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    return outStream;
                }
                public static MemoryStream PlayerYank(ArenaPlayer arenaPlayer, Byte playerId, SharpDX.Vector3 location, bool UDP = false)
                {
                    MemoryStream outStream = new MemoryStream();
                    outStream.WriteByte(arenaPlayer.ArenaPlayerId);
                    outStream.WriteByte((Byte)PacketOutFunction.PlayerYank);
                    outStream.Write(BitConverter.GetBytes(NetHelper.FlipBytes(playerId)), 0, 2);
                    outStream.Write(BitConverter.GetBytes(NetHelper.FlipBytes(Convert.ToInt16(location.X))), 0, 2);
                    outStream.Write(BitConverter.GetBytes(NetHelper.FlipBytes(Convert.ToInt16(location.Y))), 0, 2);
                    outStream.Write(BitConverter.GetBytes(NetHelper.FlipBytes(Convert.ToInt16(location.Z))), 0, 2);
                    return outStream;
                }
                public static MemoryStream PlayerGod(ArenaPlayer arenaPlayer, Boolean godStatus, bool UDP = false)
                {
                    MemoryStream outStream = new MemoryStream();
                    outStream.WriteByte(arenaPlayer.ArenaPlayerId);
                    outStream.WriteByte((Byte)PacketOutFunction.PlayerGod);
                    outStream.Write(BitConverter.GetBytes(NetHelper.FlipBytes(Convert.ToByte(godStatus))), 0, 2);
                    return outStream;
                }
                public static MemoryStream PlayerJoin(ArenaPlayer arenaPlayer, bool UDP = false)
                {
                    MemoryStream outStream = new MemoryStream();
                    outStream.WriteByte(arenaPlayer.ArenaPlayerId);
                    outStream.WriteByte((Byte)PacketOutFunction.PlayerJoin);
                    outStream.Write(BitConverter.GetBytes(NetHelper.FlipBytes(arenaPlayer.ArenaPlayerId)), 0, 2);
                    outStream.WriteByte(arenaPlayer.OwnerArena.ArenaId);
                    outStream.WriteByte((Byte) arenaPlayer.ActiveTeam);
                    outStream.Write(Encoding.ASCII.GetBytes(arenaPlayer.ActiveCharacter.Name), 0, arenaPlayer.ActiveCharacter.Name.Length);
                    outStream.Seek((12 - arenaPlayer.ActiveCharacter.Name.Length), SeekOrigin.Current);
                    outStream.WriteByte((Byte) arenaPlayer.ActiveCharacter.Class);
                    outStream.WriteByte(arenaPlayer.ActiveCharacter.Level);
                    outStream.WriteByte(arenaPlayer.ActiveCharacter.OpLevel);
                    outStream.WriteByte(0x00);
                    return outStream;
                }
                public static MemoryStream PlayerLeave(ArenaPlayer arenaPlayer, bool UDP = false)
                {
                    MemoryStream outStream = new MemoryStream();
                    outStream.WriteByte(arenaPlayer.ArenaPlayerId);
                    outStream.WriteByte((Byte)PacketOutFunction.PlayerLeave);
                    outStream.Write(BitConverter.GetBytes(NetHelper.FlipBytes(arenaPlayer.ArenaPlayerId)), 0, 2);
                    return outStream;
                }
                public static MemoryStream SuccessfulArenaEntry(bool UDP = false)
                {
                    MemoryStream outStream = new MemoryStream();
                    outStream.WriteByte(0x00);
                    outStream.WriteByte((Byte)PacketOutFunction.SuccessfulArenaEntry);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    return outStream;
                }
                public static MemoryStream PlayerState(ArenaPlayer arenaPlayer, bool UDP = false)
                {
                    MemoryStream outStream = new MemoryStream();

                    outStream.WriteByte(arenaPlayer.ArenaPlayerId);
                    outStream.WriteByte((Byte)PacketOutFunction.PlayerState);

                    outStream.WriteByte((Byte)arenaPlayer.DeathCount);
                    outStream.WriteByte((Byte)arenaPlayer.KillCount);
                    outStream.WriteByte(arenaPlayer.IsAlive ? (Byte)0 : (Byte)1);
                    outStream.WriteByte(arenaPlayer.ActiveCharacter.Level);
                    outStream.WriteByte((Byte)arenaPlayer.RaiseCount);
                    outStream.WriteByte((Byte)arenaPlayer.ActiveCharacter.Class);

                    /*outStream.WriteByte(0xFF);
                    outStream.WriteByte(0xFF);

                    outStream.WriteByte((Byte) arenaPlayer.DeathCount);
                    outStream.WriteByte((Byte) arenaPlayer.KillCount);
                    outStream.WriteByte(0xFF);
                    outStream.WriteByte(0xFF);
                    outStream.WriteByte(arenaPlayer.IsAlive ? (Byte) 0 : (Byte) 1);
                    outStream.WriteByte((Byte) arenaPlayer.ActiveCharacter.Class);
                    outStream.WriteByte(arenaPlayer.ActiveCharacter.Level);
                    outStream.WriteByte((Byte) arenaPlayer.RaiseCount);
                    outStream.WriteByte(0xFF);
                    outStream.WriteByte(0xFF);

                    outStream.WriteByte(0xFF);
                    outStream.WriteByte(0xFF);

                    outStream.WriteByte(0xFF);
                    outStream.WriteByte(0xFF);

                    outStream.WriteByte(0xFF);
                    outStream.WriteByte(0xFF);
                    outStream.WriteByte(0xFF);
                    outStream.WriteByte(0xFF);*/
                    return outStream;
                }
                public static MemoryStream PlayerMoveState(ArenaPlayer arenaPlayer, Byte[] relayBuffer, bool UDP = false)
                {
                    MemoryStream outStream = new MemoryStream();
                    outStream.WriteByte(arenaPlayer.ArenaPlayerId);
                    outStream.WriteByte((Byte)PacketOutFunction.PlayerMoveState);
                    outStream.Write(relayBuffer, 0, 12);
                    return outStream;
                }
                public static MemoryStream PlayerMoveStateShort(ArenaPlayer arenaPlayer, Byte[] relayBuffer, bool UDP = false)
                {
                    MemoryStream outStream = new MemoryStream();
                    outStream.WriteByte(arenaPlayer.ArenaPlayerId);
                    outStream.WriteByte((Byte)PacketOutFunction.PlayerMoveStateShort);
                    outStream.Write(relayBuffer, 0, 8);
                    return outStream;
                }
                public static MemoryStream CastEffect(ArenaPlayer arenaPlayer, Int16 spellId, bool UDP = false)
                {
                    MemoryStream outStream = new MemoryStream();
                    outStream.WriteByte(arenaPlayer.ArenaPlayerId);
                    outStream.WriteByte((Byte)PacketOutFunction.CastEffect);
                    outStream.Write(BitConverter.GetBytes(NetHelper.FlipBytes(spellId)), 0, 2);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    return outStream;
                }
                public static MemoryStream CastTargeted(ArenaPlayer arenaPlayer, Byte[] relayBuffer, bool UDP = false)
                {
                    MemoryStream outStream = new MemoryStream();
                    outStream.WriteByte(arenaPlayer.ArenaPlayerId);
                    outStream.WriteByte((Byte)PacketOutFunction.CastTargeted);
                    outStream.Write(relayBuffer, 0, 28);
                    return outStream;
                }
                public static MemoryStream CastTargetedEx(ArenaPlayer targetPlayer, ArenaPlayer sourcePlayer, Spell spell, bool UDP = false)
                {
                    MemoryStream outStream = new MemoryStream();
                    outStream.WriteByte(targetPlayer.ArenaPlayerId);
                    outStream.WriteByte((Byte)PacketOutFunction.CastTargeted);
                    outStream.Write(BitConverter.GetBytes(NetHelper.FlipBytes(spell.Id)), 0, 2);
                    outStream.Write(BitConverter.GetBytes(NetHelper.FlipBytes(spell.Range)), 0, 2);

                    if (sourcePlayer == null)
                    {
                        outStream.WriteByte(0);
                        outStream.WriteByte(0);
                    }
                    else
                    {
                        outStream.Write(BitConverter.GetBytes(NetHelper.FlipBytes(sourcePlayer.ArenaPlayerId)), 0, 2);
                    }
                   
                    outStream.Write(BitConverter.GetBytes(NetHelper.FlipBytes(targetPlayer.ArenaPlayerId)), 0, 2);
                    outStream.Write(new Byte[20], 0, 20);
                    return outStream;
                }
                public static MemoryStream CastRune(ArenaPlayer arenaPlayer, Byte[] relayBuffer, bool UDP = false)
                {
                    MemoryStream outStream = new MemoryStream();
                    outStream.WriteByte(arenaPlayer.ArenaPlayerId);
                    outStream.WriteByte((Byte)PacketOutFunction.CastRune);
                    outStream.Write(relayBuffer, 0, 20);
                    return outStream;
                }
                public static MemoryStream CastRuneEx(ArenaPlayer arenaPlayer, Rune rune, bool UDP = false)
                {
                    Int16 runeDirection = (Int16)MathHelper.RadiansToDirection(rune.Direction);

                    MemoryStream outStream = new MemoryStream();
                    outStream.WriteByte(arenaPlayer.ArenaPlayerId);
                    outStream.WriteByte((Byte)PacketOutFunction.CastRune);
                    outStream.Write(BitConverter.GetBytes(NetHelper.FlipBytes(rune.Spell.Id)), 0, 2);
                    outStream.Write(BitConverter.GetBytes(NetHelper.FlipBytes(rune.ObjectId)), 0, 2);
                    outStream.Write(BitConverter.GetBytes(NetHelper.FlipBytes((Int16)rune.BoundingBox.Origin.X)), 0, 2);
                    outStream.Write(BitConverter.GetBytes(NetHelper.FlipBytes((Int16)rune.BoundingBox.Origin.Y)), 0, 2);
                    outStream.Write(BitConverter.GetBytes(NetHelper.FlipBytes((Int16)rune.BoundingBox.Origin.Z)), 0, 2);
                    outStream.Write(BitConverter.GetBytes(NetHelper.FlipBytes(runeDirection)), 0, 2);
                    outStream.Write(new Byte[4], 0, 4);
                    outStream.WriteByte((Byte)rune.Team);
                    outStream.Write(new Byte[3], 0, 3);
                    return outStream;
                }
                public static MemoryStream CastBolt(ArenaPlayer arenaPlayer, Byte[] relayBuffer, bool UDP = false)
                {
                    MemoryStream outStream = new MemoryStream();
                    outStream.WriteByte(arenaPlayer.ArenaPlayerId);
                    outStream.WriteByte((Byte)PacketOutFunction.CastBolt);
                    outStream.Write(relayBuffer, 0, 34);
                    return outStream;
                }
                public static MemoryStream CastProjectile(ArenaPlayer arenaPlayer, Byte[] relayBuffer, bool UDP = false)
                {
                    MemoryStream outStream = new MemoryStream();
                    outStream.WriteByte(arenaPlayer.ArenaPlayerId);
                    outStream.WriteByte((Byte)PacketOutFunction.CastProjectile);
                    outStream.Write(relayBuffer, 0, 16);
                    return outStream;
                }
                public static MemoryStream CastProjectile(ArenaPlayer arenaPlayer, Projectile newProj, bool UDP = false)
                {
                    MemoryStream outStream = new MemoryStream();
                    outStream.WriteByte(arenaPlayer.ArenaPlayerId);
                    outStream.WriteByte((Byte)PacketOutFunction.CastProjectile);
                    outStream.Write(BitConverter.GetBytes(NetHelper.FlipBytes(newProj.Spell.Id)), 0, 2);
                    outStream.Write(BitConverter.GetBytes(NetHelper.FlipBytes((Int16)newProj.Location.X)), 0, 2);
                    outStream.Write(BitConverter.GetBytes(NetHelper.FlipBytes((Int16)newProj.Location.Y)), 0, 2);
                    outStream.Write(BitConverter.GetBytes(NetHelper.FlipBytes((Int16)newProj.Location.Z)), 0, 2);
                    outStream.Write(BitConverter.GetBytes(NetHelper.FlipBytes((Int16)newProj.Direction)), 0, 2);
                    outStream.Write(BitConverter.GetBytes(NetHelper.FlipBytes((Int16)newProj.Angle)), 0, 2);
                    return outStream;
                    
                }
                public static MemoryStream CastWall(Byte[] relayBuffer, bool UDP = false)
                {
                    MemoryStream outStream = new MemoryStream();
                    outStream.WriteByte(0x00);
                    outStream.WriteByte((Byte)PacketOutFunction.CastWall);
                    outStream.Write(relayBuffer, 0, 18);
                    return outStream;
                }
                public static MemoryStream PlayerDamage(ArenaPlayer victimPlayer, ArenaPlayer attackingPlayer, SpellDamage damages, bool UDP = false)
                {
                    MemoryStream outStream = new MemoryStream();
                    outStream.WriteByte(0x00);
                    outStream.WriteByte((Byte)PacketOutFunction.UpdateHealth);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(attackingPlayer == null ? (Byte) 0 : attackingPlayer.ArenaPlayerId);
                    outStream.WriteByte(Convert.ToByte(damages.Damage));
                    outStream.WriteByte(Convert.ToByte(damages.Power));
                    //victimPlayer.CurrentHp
                    outStream.Write(BitConverter.GetBytes(victimPlayer.CurrentHp), 0, 2);
                    return outStream;
                }
                public static MemoryStream PlayerHit(ArenaPlayer victimPlayer, bool UDP = false)
                {
                    MemoryStream outStream = new MemoryStream();
                    outStream.WriteByte(0x00);
                    outStream.WriteByte((Byte)PacketOutFunction.PlayerHit);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(victimPlayer.ArenaPlayerId);
                    return outStream;
                }
                public static MemoryStream PlayerDeath(ArenaPlayer victimPlayer, ArenaPlayer attackingPlayer, bool UDP = false)
                {
                    MemoryStream outStream = new MemoryStream();
                    outStream.WriteByte(0x00);
                    outStream.WriteByte((Byte)PacketOutFunction.PlayerDeath);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(victimPlayer.ArenaPlayerId);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(attackingPlayer == null ? (Byte) 0 : attackingPlayer.ArenaPlayerId);
                    return outStream;
                }
                public static MemoryStream PlayerResurrect(ArenaPlayer arenaPlayer, ArenaPlayer targetPlayer, bool UDP = false)
                {
                    MemoryStream outStream = new MemoryStream();
                    outStream.WriteByte(0x00);
                    outStream.WriteByte((Byte)PacketOutFunction.PlayerResurrect);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(targetPlayer.ArenaPlayerId);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(arenaPlayer.ArenaPlayerId);
                    return outStream;
                }
                public static MemoryStream BiasedShrine(ArenaPlayer arenaPlayer, Shrine shrine, Byte biasAmount, bool UDP = false)
                {
                    MemoryStream outStream = new MemoryStream();
                    outStream.WriteByte(0x00);
                    outStream.WriteByte((Byte)PacketOutFunction.BiasedShrine);
                    outStream.WriteByte(shrine.ShrineId);
                    outStream.WriteByte((Byte) shrine.Team);
                    outStream.WriteByte((Byte) shrine.CurrentBias);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte((Byte)shrine.Power);
                    outStream.WriteByte(biasAmount);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(arenaPlayer.ArenaPlayerId);
                    return outStream;
                }
                public static MemoryStream BiasedPool(ArenaPlayer arenaPlayer, Pool pool, Byte biasAmount, bool UDP = false)
                {
                    MemoryStream outStream = new MemoryStream();
                    outStream.WriteByte(0x00);
                    outStream.WriteByte((Byte)PacketOutFunction.BiasedPool);
                    outStream.WriteByte(pool.PoolId);
                    outStream.WriteByte((Byte) pool.Team);
                    outStream.WriteByte((Byte) pool.CurrentBias);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(biasAmount);
                    outStream.WriteByte((Byte) pool.Power);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(arenaPlayer.ArenaPlayerId);
                    return outStream;
                }
                public static MemoryStream UpdateShrinePoolState(SpellServer.Arena arena, bool UDP = false)
                {
                    MemoryStream outStream = new MemoryStream();

                    outStream.WriteByte(0x00);
                    outStream.WriteByte((Byte)PacketOutFunction.UpdateShrinePoolState);

                    for (Int32 i = 0; i < 3; i++)
                    {
                        if ((arena.ArenaTeams[i] != null) || (!arena.ArenaTeams[i].Shrine.IsDisabled))
                        {
                            outStream.WriteByte(arena.ArenaTeams[i].Shrine.ShrineId);
                            outStream.WriteByte(Convert.ToByte(arena.ArenaTeams[i].Shrine.Team));
                            outStream.WriteByte(Convert.ToByte(arena.ArenaTeams[i].Shrine.CurrentBias));
                            outStream.WriteByte(0x00);
                            outStream.WriteByte(Convert.ToByte(arena.ArenaTeams[i].Shrine.Power));
                            outStream.WriteByte(0x00); // Bias Amount //Figure out bias amount if needed
                            outStream.WriteByte(0x00); // PlayerId
                            outStream.WriteByte(0x00); // PlayerId 
                            //outStream.WriteByte(Convert.ToByte(arena.ArenaTeams[i].Shrine.Links[0])); 
                            //outStream.WriteByte(Convert.ToByte(arena.ArenaTeams[i].Shrine.Links[1])); // PlayerId if needed
                            //outStream.WriteByte(Convert.ToByte(arena.ArenaTeams[i].Shrine.Links[2])); // PlayerId if needed
                        }
                        else
                        {
                            outStream.WriteByte(0xFF);
                            outStream.WriteByte(0x00);
                            outStream.WriteByte(0x00);
                            outStream.WriteByte(0x00);
                            outStream.WriteByte(0x00);
                            outStream.WriteByte(0x00);
                            outStream.WriteByte(0x00);
                            outStream.WriteByte(0x00);
                        }
                    }
                    /* 1st Nexus
                    outStream.WriteByte(0x01); // Shrine index - Nexus disabled send 0xFF
                    outStream.WriteByte(0x01); // Align/Team (00 - Sysop / 01 - Dragon / 02 - Gryphon / 03 - Pheonix)
                    outStream.WriteByte(0x01); // Current bias
                    outStream.WriteByte(0x00); // 
                    outStream.WriteByte(0x64); // Health
                    outStream.WriteByte(0x00); // Bias Amount
                    outStream.WriteByte(0x01); // PlayerId
                    outStream.WriteByte(0x00); // PlayerId

                    // 2nd Nexus
                    outStream.WriteByte(0x02); // Shrine index - Nexus disabled send 0xFF
                    outStream.WriteByte(0x02); // Align/Team (00 - Sysop / 01 - Dragon / 02 - Gryphon / 03 - Pheonix)
                    outStream.WriteByte(0x02); // Current bias
                    outStream.WriteByte(0x00); // 
                    outStream.WriteByte(0x64); // Health
                    outStream.WriteByte(0x00); // Bias Amount
                    outStream.WriteByte(0x01); // PlayerId
                    outStream.WriteByte(0x00); // PlayerId

                    // 3rd Nexus
                    outStream.WriteByte(0x03); // Shrine index - Nexus disabled send 0xFF
                    outStream.WriteByte(0x03); // Align/Team (00 - Sysop / 01 - Dragon / 02 - Gryphon / 03 - Pheonix)
                    outStream.WriteByte(0x03); // Current bias
                    outStream.WriteByte(0x00); // 
                    outStream.WriteByte(0x64); // Health
                    outStream.WriteByte(0x00); // Bias Amount
                    outStream.WriteByte(0x01); // PlayerId
                    outStream.WriteByte(0x00); // PlayerId
                    */
                    for (Int32 i = 0; i < 20; i++)
                    {
                        if (arena.Grid.Pools[i] != null)
                        {
                            outStream.WriteByte(arena.Grid.Pools[i].PoolId);
                            outStream.WriteByte((Byte)arena.Grid.Pools[i].Team);
                            outStream.WriteByte((Byte)arena.Grid.Pools[i].CurrentBias);
                            outStream.WriteByte(0x00);
                            outStream.WriteByte(0x00);
                            outStream.WriteByte((Byte)arena.Grid.Pools[i].Power);
                            outStream.WriteByte(0x00);
                            outStream.WriteByte(0x00);
                        }
                        else
                        {
                            outStream.WriteByte(0xFF);
                            outStream.WriteByte(0x00);
                            outStream.WriteByte(0x00);
                            outStream.WriteByte(0x00);
                            outStream.WriteByte(0x00);
                            outStream.WriteByte(0x00);
                            outStream.WriteByte(0x00);
                            outStream.WriteByte(0x00);
                        }
                    }
                        /*outStream.WriteByte(0x00);
                        outStream.WriteByte((Byte)PacketOutFunction.UpdateShrinePoolState);

                        // Chaos Shrine
                        outStream.WriteByte(arena.ArenaTeams.Chaos.Shrine.ShrineId);
                        outStream.WriteByte((Byte) arena.ArenaTeams.Chaos.Shrine.Team);
                        outStream.WriteByte((Byte) arena.ArenaTeams.Chaos.Shrine.CurrentBias);
                        outStream.WriteByte(0x00);
                        outStream.WriteByte(0x00);

                        if (arena.ArenaTeams.Chaos.Shrine.IsIndestructible)
                        {
                            outStream.WriteByte(0x00);
                        }
                        else
                        {
                            outStream.WriteByte((Byte)arena.ArenaTeams.Chaos.Shrine.Power);
                        }

                        outStream.WriteByte(0x00);
                        outStream.WriteByte(0x00);

                        // Balance Shrine
                        outStream.WriteByte(arena.ArenaTeams.Balance.Shrine.ShrineId);
                        outStream.WriteByte((Byte) arena.ArenaTeams.Balance.Shrine.Team);
                        outStream.WriteByte((Byte) arena.ArenaTeams.Balance.Shrine.CurrentBias);
                        outStream.WriteByte(0x00);
                        outStream.WriteByte(0x00);

                        if (arena.ArenaTeams.Balance.Shrine.IsIndestructible)
                        {
                            outStream.WriteByte(0x00);
                        }
                        else
                        {
                            outStream.WriteByte((Byte)arena.ArenaTeams.Balance.Shrine.Power);
                        }

                        outStream.WriteByte(0x00);
                        outStream.WriteByte(0x00);

                        // Order Shrine
                        outStream.WriteByte(arena.ArenaTeams.Order.Shrine.ShrineId);
                        outStream.WriteByte((Byte) arena.ArenaTeams.Order.Shrine.Team);
                        outStream.WriteByte((Byte) arena.ArenaTeams.Order.Shrine.CurrentBias);
                        outStream.WriteByte(0x00);
                        outStream.WriteByte(0x00);

                        if (arena.ArenaTeams.Order.Shrine.IsIndestructible)
                        {
                            outStream.WriteByte(0x00);
                        }
                        else
                        {
                            outStream.WriteByte((Byte)arena.ArenaTeams.Order.Shrine.Power);
                        }

                        outStream.WriteByte(0x00);
                        outStream.WriteByte(0x00);

                        for (Int32 i = 0; i < 20; i++)
                        {
                            if (arena.Grid.Pools[i] != null)
                            {
                                outStream.WriteByte(arena.Grid.Pools[i].PoolId);
                                outStream.WriteByte((Byte) arena.Grid.Pools[i].Team);
                                outStream.WriteByte((Byte) arena.Grid.Pools[i].CurrentBias);
                                outStream.WriteByte(0x00);
                                outStream.WriteByte(0x00);
                                outStream.WriteByte((Byte) arena.Grid.Pools[i].Power);
                                outStream.WriteByte(0x00);
                                outStream.WriteByte(0x00);
                            }
                            else
                            {
                                outStream.WriteByte(0xFF);
                                outStream.WriteByte(0x00);
                                outStream.WriteByte(0x00);
                                outStream.WriteByte(0x00);
                                outStream.WriteByte(0x00);
                                outStream.WriteByte(0x00);
                                outStream.WriteByte(0x00);
                                outStream.WriteByte(0x00);
                            }
                        }*/
                        return outStream;
                }
                public static MemoryStream UpdateExperience(ArenaPlayer arenaPlayer, bool UDP = false)
                {
                    MemoryStream outStream = new MemoryStream();
                    outStream.WriteByte(0x00);
                    outStream.WriteByte((Byte)PacketOutFunction.UpdateExperience);
                    outStream.Write(BitConverter.GetBytes(NetHelper.FlipBytes((UInt32) arenaPlayer.CombatExp)), 0, 4);
                    outStream.Write(BitConverter.GetBytes(NetHelper.FlipBytes((UInt32) arenaPlayer.BonusExp)), 0, 4);
                    outStream.Write(BitConverter.GetBytes(NetHelper.FlipBytes((UInt32) arenaPlayer.ObjectiveExp)), 0, 4);
                    return outStream;
                }
                public static MemoryStream UpdateHealth(ArenaPlayer arenaPlayer, bool UDP = false)
                {
                    MemoryStream outStream = new MemoryStream();
                    outStream.WriteByte(0x00);
                    outStream.WriteByte((Byte)PacketOutFunction.UpdateHealth);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.Write(BitConverter.GetBytes(arenaPlayer.CurrentHp), 0, 2);
                    return outStream;
                }
                public static MemoryStream CalledGhost(ArenaPlayer arenaPlayer, ArenaPlayer targetArenaPlayer, byte[] relayBuffer, bool UDP = false)
                {
                    MemoryStream outStream = new MemoryStream();
                    outStream.WriteByte(0x00);
                    outStream.WriteByte((Byte)PacketOutFunction.CalledGhost);
                    outStream.Write(relayBuffer, 0, 10);
                    //outStream.WriteByte(0x00);
                    //outStream.WriteByte(arenaPlayer.ArenaPlayerId);
                    //outStream.WriteByte(0x00);
                    //outStream.WriteByte(targetArenaPlayer.ArenaPlayerId);
                    //outStream.WriteByte(0x02);
                    return outStream;
                }
                public static MemoryStream TappedAtShrine(ArenaPlayer arenaPlayer, Boolean canRes, bool UDP = false)
                {
                    MemoryStream outStream = new MemoryStream();
                    outStream.WriteByte(0x00);
                    outStream.WriteByte((Byte)PacketOutFunction.PlayerResurrect);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(arenaPlayer.ArenaPlayerId);
                    outStream.WriteByte(0xFF);
                    outStream.WriteByte(canRes ? (Byte) 0xFE : (Byte) 0xFF);
                    return outStream;
                }
                public static MemoryStream ActivatedTrigger(Trigger trigger, bool UDP = false)
                {
                    MemoryStream outStream = new MemoryStream();
                    outStream.WriteByte(0x00);
                    outStream.WriteByte((Byte)PacketOutFunction.ActivatedTrigger);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(Convert.ToByte(trigger.TriggerId));
                    outStream.WriteByte((Byte) trigger.CurrentState);
                    return outStream;
                }
                public static MemoryStream ObjectDeath(ArenaPlayer arenaPlayer, Int16 objectId, bool UDP = false)
                {
                    MemoryStream outStream = new MemoryStream();
                    outStream.WriteByte(0x00);
                    outStream.WriteByte((Byte)PacketOutFunction.ObjectDeath);
                    outStream.Write(BitConverter.GetBytes(NetHelper.FlipBytes(objectId)), 0, 2);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(arenaPlayer.ArenaPlayerId);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00); // rune or not byte 6
                    return outStream;
                }
                public static MemoryStream ObjectDeath(Int16 objectId, bool UDP = false)
                {
                    MemoryStream outStream = new MemoryStream();
                    outStream.WriteByte(0x00);
                    outStream.WriteByte((Byte)PacketOutFunction.ObjectDeath);
                    outStream.Write(BitConverter.GetBytes(NetHelper.FlipBytes(objectId)), 0, 2);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    return outStream;
                }
                public static MemoryStream ThinDamage(Int16 objectId, Int16 damage, bool UDP = false)
                {
                    MemoryStream outStream = new MemoryStream();
                    outStream.WriteByte(0x00);
                    outStream.WriteByte((Byte)PacketOutFunction.ThinDamage);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.Write(BitConverter.GetBytes(NetHelper.FlipBytes(objectId)), 0, 2);
                    outStream.Write(BitConverter.GetBytes(NetHelper.FlipBytes(damage)), 0, 2);
                    return outStream;
                }     
                public static MemoryStream PlaySound(GameSound.Sound sound, Int16 range, Int16 x, Int16 y, bool UDP = false)
                {
                    MemoryStream outStream = new MemoryStream();
                    outStream.WriteByte(0x00);
                    outStream.WriteByte((Byte)PacketOutFunction.PlaySound);
                    outStream.Write(BitConverter.GetBytes(NetHelper.FlipBytes((Int16)sound)), 0, 2);
                    outStream.Write(BitConverter.GetBytes(NetHelper.FlipBytes(range)), 0, 2);
                    outStream.Write(BitConverter.GetBytes(NetHelper.FlipBytes(x)), 0, 2);
                    outStream.Write(BitConverter.GetBytes(NetHelper.FlipBytes(y)), 0, 2);
                    return outStream;
                }
            }

            public static class Login
            {
                public static MemoryStream Connected(SpellServer.Player player, bool UDP = false)
                {
                    MemoryStream outStream = new MemoryStream();
                    outStream.WriteByte(0x00);
                    outStream.WriteByte((Byte)PacketOutFunction.LoginConnected);
                    outStream.WriteByte(0x00);
                    outStream.Write(Subscription.GameVersion, 0, 4);
                    outStream.Write(Encoding.ASCII.GetBytes(player.Username), 0, player.Username.Length);
                    outStream.Seek((12 - player.Username.Length), SeekOrigin.Current);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);

                    // Do Encryption? 0 = No, Anything Else = Yes
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);

                    // Unknown
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);

                    // Unknown
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    return outStream;
                }
                public static MemoryStream Error(Subscription.ErrorType loginError, bool UDP = false)
                {
                    MemoryStream outStream = new MemoryStream();
                    outStream.WriteByte(0x00);
                    outStream.WriteByte((Byte)PacketOutFunction.LoginError);
                    outStream.WriteByte((Byte)loginError);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    return outStream;
                }
            }

            public static class Player
            {
                public static MemoryStream SendPlayerId(SpellServer.Player player, bool UDP = false)
                {
                    MemoryStream outStream = new MemoryStream();
                    outStream.WriteByte(0x00);
                    outStream.WriteByte((Byte)PacketOutFunction.SendPlayerId);
                    outStream.Write(BitConverter.GetBytes(player.PlayerId), 0, 2);
                    return outStream;
                }
                public static MemoryStream SendPlayerId(ArenaPlayer arenaPlayer, bool UDP = false)
                {
                    MemoryStream outStream = new MemoryStream();
                    outStream.WriteByte(0x00);
                    outStream.WriteByte((Byte)PacketOutFunction.SendPlayerId);
                    outStream.WriteByte(arenaPlayer.ArenaPlayerId);
                    outStream.WriteByte(0x00);
                    return outStream;
                }
                public static MemoryStream HeartbeatReply(SpellServer.Player player, bool UDP = false)
                {
                    MemoryStream outStream = new MemoryStream();
                    outStream.WriteByte(0x00);
                    outStream.WriteByte((Byte)PacketOutFunction.HeartbeatReply);
                    outStream.Write(BitConverter.GetBytes(NetHelper.FlipBytes(player.LastHeartbeat)), 0, 4);
                    return outStream;
                }
                public static MemoryStream SaveSuccess(SpellServer.Player player, Byte slot, bool UDP = false)
                {
                    MemoryStream outStream = new MemoryStream();
                    outStream.WriteByte(0x00);
                    outStream.WriteByte((Byte)PacketOutFunction.SaveSuccess);
                    outStream.Write(Encoding.ASCII.GetBytes(player.Username), 0, player.Username.Length);
                    outStream.Seek((20 - player.Username.Length), SeekOrigin.Current);
                    outStream.WriteByte(slot);
                    return outStream;
                }
                public static MemoryStream SwitchedToTable(SpellServer.Player player, bool UDP = false)
                {
                    MemoryStream outStream = new MemoryStream();
                    outStream.WriteByte(0x00);
                    outStream.WriteByte((Byte)PacketOutFunction.SwitchedToTable);
                    outStream.Write(BitConverter.GetBytes(NetHelper.FlipBytes(player.PlayerId)), 0, 2);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(player.TableId);
                    return outStream;
                }
                public static MemoryStream HasEnteredWorld(bool UDP = false)
                {
                    MemoryStream outStream = new MemoryStream();
                    outStream.WriteByte(0x00);
                    outStream.WriteByte((Byte)PacketOutFunction.HasEnteredWorld);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    return outStream;
                }
                public static MemoryStream Chat(SpellServer.Player player, Int16 target, ChatType targetType, String message, bool UDP = false)
                {
                    MemoryStream outStream = new MemoryStream();
                    outStream.WriteByte(0x00);
                    outStream.WriteByte((Byte)PacketOutFunction.Chat);

                    if (player == null)
                    {
                        outStream.WriteByte(0x00);
                        outStream.WriteByte(0x00);
                    }
                    else
                    {
                        if (player.ActiveArena != null)
                        {
                            if (player.ActiveArenaPlayer != null)
                            {
                                outStream.Write(BitConverter.GetBytes(NetHelper.FlipBytes(player.ActiveArenaPlayer.ArenaPlayerId)), 0, 2);
                            }
                            else
                            {
                                outStream.WriteByte(0x00);
                                outStream.WriteByte(0x00);
                            }
                        }
                        else
                        {
                            outStream.Write(BitConverter.GetBytes(NetHelper.FlipBytes(player.PlayerId)), 0, 2);
                        }  
                    }      

                    outStream.Write(BitConverter.GetBytes(NetHelper.FlipBytes(target)), 0, 2);
                    outStream.WriteByte((Byte) targetType);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.Write(Encoding.ASCII.GetBytes(message), 0, message.Length);
                    return outStream;
                }
                public static MemoryStream InviteToTable(Table table, Byte[] inviteData, bool UDP = false)
                {
                    MemoryStream outStream = new MemoryStream();
                    outStream.WriteByte(0x00);
                    outStream.WriteByte((Byte)PacketOutFunction.InviteToTable);
                    outStream.WriteByte((Byte) table.TableId);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.Write(inviteData, 0, inviteData.Length);
                    return outStream;
                }
                public static MemoryStream EstablishDatagram(SpellServer.Player player, bool UDP = false)
                {
                    MemoryStream outStream = new MemoryStream();

                    //outStream.WriteByte(0x01);
                    //outStream.WriteByte(0x00);
                    //outStream.WriteByte(0x00);
                    //outStream.WriteByte(0x00);
                    //outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte((Byte)PacketOutFunction.EstablishDatagram);

                    IPAddress ipAddress = Network.LocalIPAddress();

                    byte[] ipBytes = Encoding.ASCII.GetBytes(ipAddress.ToString());

                    outStream.Write(ipBytes, 0, ipBytes.Length);
                    
                    for (int i = 0; i < (20 - ipBytes.Length); i++)
                    {
                        outStream.WriteByte(0x00);
                    }

                    byte[] portBytes = BitConverter.GetBytes(SpellServer.Properties.Settings.Default.UDPPort);

                    outStream.Write(portBytes, 0, 2);//outStream.Write(player.UdpportLE, 0, 2);

                    return outStream;

                    /*outStream.WriteByte(0x31);
                    outStream.WriteByte(0x32);
                    outStream.WriteByte(0x37);
                    outStream.WriteByte(0x2E);
                    outStream.WriteByte(0x30);
                    outStream.WriteByte(0x2E);
                    outStream.WriteByte(0x30);
                    outStream.WriteByte(0x2E);
                    outStream.WriteByte(0x31);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x29);
                    outStream.WriteByte(0x69);

                    return outStream;*/
                }

            }

            public static class Study
            {
                public static MemoryStream IsNameValid(SpellServer.Player player, String name, Boolean valid, bool UDP = false)
                {
                    MemoryStream outStream = new MemoryStream();
                    outStream.WriteByte(0x00);
                    outStream.WriteByte((Byte)PacketOutFunction.IsNameValid);
                    outStream.Write(Encoding.ASCII.GetBytes(name), 0, name.Length);
                    outStream.Seek((30 - name.Length), SeekOrigin.Current);
                    outStream.Write(Encoding.ASCII.GetBytes(player.Username), 0, player.Username.Length);
                    outStream.Seek((20 - player.Username.Length), SeekOrigin.Current);
                    outStream.WriteByte(Convert.ToByte(valid));
                    return outStream;
                }
                public static MemoryStream IsNameTaken(SpellServer.Player player, String name, Boolean taken, bool UDP = false)
                {
                    MemoryStream outStream = new MemoryStream();
                    outStream.WriteByte(0x00);
                    outStream.WriteByte((Byte)PacketOutFunction.IsNameTaken);
                    outStream.Write(Encoding.ASCII.GetBytes(name), 0, name.Length);
                    outStream.Seek((30 - name.Length), SeekOrigin.Current);
                    outStream.Write(Encoding.ASCII.GetBytes(player.Username), 0, player.Username.Length);
                    outStream.Seek((20 - player.Username.Length), SeekOrigin.Current);
                    outStream.WriteByte(Convert.ToByte(taken));
                    return outStream;
                }
                public static MemoryStream SendCharacterInSlot(SpellServer.Player player, Byte slot, DataTable data, bool UDP = false)
                {
                    MemoryStream outStream = new MemoryStream();

                    outStream.WriteByte(0);
                    outStream.WriteByte((Byte)PacketOutFunction.SendCharacterInSlot);
                    outStream.Write(Encoding.ASCII.GetBytes(player.Username), 0, player.Username.Length);
                    outStream.Seek((20 - player.Username.Length), SeekOrigin.Current);
                    outStream.WriteByte(slot);
                    outStream.WriteByte(0);
                    outStream.WriteByte(0);
                    outStream.WriteByte(0);

                    if (data.Rows.Count > 0)
                    {
                        DataRow charData = data.Rows[0];

                        String name = charData.Field<String>("name");
                        outStream.Write(Encoding.ASCII.GetBytes(name), 0, name.Length);
                        outStream.Seek((20 - name.Length), SeekOrigin.Current);
                        outStream.WriteByte(charData.Field<Byte>("agility"));
                        outStream.WriteByte(charData.Field<Byte>("constitution"));
                        outStream.WriteByte(charData.Field<Byte>("memory"));
                        outStream.WriteByte(charData.Field<Byte>("reasoning"));
                        outStream.WriteByte(charData.Field<Byte>("discipline"));
                        outStream.WriteByte(charData.Field<Byte>("empathy"));
                        outStream.WriteByte(charData.Field<Byte>("intuition"));
                        outStream.WriteByte(charData.Field<Byte>("presence"));
                        outStream.WriteByte(charData.Field<Byte>("quickness"));
                        outStream.WriteByte(charData.Field<Byte>("strength"));
                        outStream.WriteByte(0x00);
                        outStream.WriteByte(0x00);
                        outStream.WriteByte(charData.Field<Byte>("agility"));
                        outStream.WriteByte(charData.Field<Byte>("constitution"));
                        outStream.WriteByte(charData.Field<Byte>("memory"));
                        outStream.WriteByte(charData.Field<Byte>("reasoning"));
                        outStream.WriteByte(charData.Field<Byte>("discipline"));
                        outStream.WriteByte(charData.Field<Byte>("empathy"));
                        outStream.WriteByte(charData.Field<Byte>("intuition"));
                        outStream.WriteByte(charData.Field<Byte>("presence"));
                        outStream.WriteByte(charData.Field<Byte>("quickness"));
                        outStream.WriteByte(charData.Field<Byte>("strength"));
                        outStream.WriteByte(0x00);
                        outStream.WriteByte(0x00);
                        outStream.WriteByte(charData.Field<Byte>("list_1"));
                        outStream.WriteByte(charData.Field<Byte>("list_2"));
                        outStream.WriteByte(charData.Field<Byte>("list_3"));
                        outStream.WriteByte(charData.Field<Byte>("list_4"));
                        outStream.WriteByte(charData.Field<Byte>("list_5"));
                        outStream.WriteByte(charData.Field<Byte>("list_6"));
                        outStream.WriteByte(charData.Field<Byte>("list_7"));
                        outStream.WriteByte(charData.Field<Byte>("list_8"));
                        outStream.WriteByte(charData.Field<Byte>("list_9"));
                        outStream.WriteByte(charData.Field<Byte>("list_10"));
                        outStream.WriteByte(0x00);
                        outStream.WriteByte(0x00);
                        outStream.WriteByte(charData.Field<Byte>("list_level_1"));
                        outStream.WriteByte(charData.Field<Byte>("list_level_2"));
                        outStream.WriteByte(charData.Field<Byte>("list_level_3"));
                        outStream.WriteByte(charData.Field<Byte>("list_level_4"));
                        outStream.WriteByte(charData.Field<Byte>("list_level_5"));
                        outStream.WriteByte(charData.Field<Byte>("list_level_6"));
                        outStream.WriteByte(charData.Field<Byte>("list_level_7"));
                        outStream.WriteByte(charData.Field<Byte>("list_level_8"));
                        outStream.WriteByte(charData.Field<Byte>("list_level_9"));
                        outStream.WriteByte(charData.Field<Byte>("list_level_10"));
                        outStream.WriteByte(0x00);
                        outStream.WriteByte(0x00);
                        outStream.WriteByte(charData.Field<Byte>("class"));
                        outStream.WriteByte(charData.Field<Byte>("level"));
                        outStream.WriteByte(charData.Field<Byte>("spell_picks"));
                        outStream.WriteByte(0x00);
                        outStream.WriteByte(charData.Field<Byte>("model"));
                        outStream.WriteByte(0x00);
                        outStream.WriteByte(0x00);
                        outStream.WriteByte(0x00);
                        outStream.Write(BitConverter.GetBytes(NetHelper.FlipBytes((UInt32)(charData.Field<UInt64>("experience")))), 0, 4);

                        byte[] kBuffer = BitConverter.GetBytes(NetHelper.FlipBytes(charData.Field<UInt16>("spell_key_1")));
                        outStream.Write(kBuffer, 0, 2);
                        kBuffer = BitConverter.GetBytes(NetHelper.FlipBytes(charData.Field<UInt16>("spell_key_2")));
                        outStream.Write(kBuffer, 0, 2);
                        kBuffer = BitConverter.GetBytes(NetHelper.FlipBytes(charData.Field<UInt16>("spell_key_3")));
                        outStream.Write(kBuffer, 0, 2);
                        kBuffer = BitConverter.GetBytes(NetHelper.FlipBytes(charData.Field<UInt16>("spell_key_4")));
                        outStream.Write(kBuffer, 0, 2);
                        kBuffer = BitConverter.GetBytes(NetHelper.FlipBytes(charData.Field<UInt16>("spell_key_5")));
                        outStream.Write(kBuffer, 0, 2);
                        kBuffer = BitConverter.GetBytes(NetHelper.FlipBytes(charData.Field<UInt16>("spell_key_6")));
                        outStream.Write(kBuffer, 0, 2);
                        kBuffer = BitConverter.GetBytes(NetHelper.FlipBytes(charData.Field<UInt16>("spell_key_7")));
                        outStream.Write(kBuffer, 0, 2);
                        kBuffer = BitConverter.GetBytes(NetHelper.FlipBytes(charData.Field<UInt16>("spell_key_8")));
                        outStream.Write(kBuffer, 0, 2);
                        kBuffer = BitConverter.GetBytes(NetHelper.FlipBytes(charData.Field<UInt16>("spell_key_9")));
                        outStream.Write(kBuffer, 0, 2);
                        kBuffer = BitConverter.GetBytes(NetHelper.FlipBytes(charData.Field<UInt16>("spell_key_10")));
                        outStream.Write(kBuffer, 0, 2);
                        kBuffer = BitConverter.GetBytes(NetHelper.FlipBytes(charData.Field<UInt16>("spell_key_11")));
                        outStream.Write(kBuffer, 0, 2);
                        kBuffer = BitConverter.GetBytes(NetHelper.FlipBytes(charData.Field<UInt16>("spell_key_12")));
                        outStream.Write(kBuffer, 0, 2);
                        kBuffer = BitConverter.GetBytes(NetHelper.FlipBytes(charData.Field<UInt16>("spell_key_13")));
                        outStream.Write(kBuffer, 0, 2);
                        kBuffer = BitConverter.GetBytes(NetHelper.FlipBytes(charData.Field<UInt16>("spell_key_14")));
                        outStream.Write(kBuffer, 0, 2);
                        kBuffer = BitConverter.GetBytes(NetHelper.FlipBytes(charData.Field<UInt16>("spell_key_15")));
                        outStream.Write(kBuffer, 0, 2);
                        kBuffer = BitConverter.GetBytes(NetHelper.FlipBytes(charData.Field<UInt16>("spell_key_16")));
                        outStream.Write(kBuffer, 0, 2);
                        kBuffer = BitConverter.GetBytes(NetHelper.FlipBytes(charData.Field<UInt16>("spell_key_17")));
                        outStream.Write(kBuffer, 0, 2);
                        kBuffer = BitConverter.GetBytes(NetHelper.FlipBytes(charData.Field<UInt16>("spell_key_18")));
                        outStream.Write(kBuffer, 0, 2);
                        kBuffer = BitConverter.GetBytes(NetHelper.FlipBytes(charData.Field<UInt16>("spell_key_19")));
                        outStream.Write(kBuffer, 0, 2);
                        kBuffer = BitConverter.GetBytes(NetHelper.FlipBytes(charData.Field<UInt16>("spell_key_20")));
                        outStream.Write(kBuffer, 0, 2);
                        kBuffer = BitConverter.GetBytes(NetHelper.FlipBytes(charData.Field<UInt16>("spell_key_21")));
                        outStream.Write(kBuffer, 0, 2);
                        kBuffer = BitConverter.GetBytes(NetHelper.FlipBytes(charData.Field<UInt16>("spell_key_22")));
                        outStream.Write(kBuffer, 0, 2);
                        kBuffer = BitConverter.GetBytes(NetHelper.FlipBytes(charData.Field<UInt16>("spell_key_23")));
                        outStream.Write(kBuffer, 0, 2);
                        kBuffer = BitConverter.GetBytes(NetHelper.FlipBytes(charData.Field<UInt16>("spell_key_24")));
                        outStream.Write(kBuffer, 0, 2);
                        kBuffer = BitConverter.GetBytes(NetHelper.FlipBytes(charData.Field<UInt16>("spell_key_25")));
                        outStream.Write(kBuffer, 0, 2);
                        kBuffer = BitConverter.GetBytes(NetHelper.FlipBytes(charData.Field<UInt16>("spell_key_26")));
                        outStream.Write(kBuffer, 0, 2);
                        kBuffer = BitConverter.GetBytes(NetHelper.FlipBytes(charData.Field<UInt16>("spell_key_27")));
                        outStream.Write(kBuffer, 0, 2);
                        kBuffer = BitConverter.GetBytes(NetHelper.FlipBytes(charData.Field<UInt16>("spell_key_28")));
                        outStream.Write(kBuffer, 0, 2);
                        kBuffer = BitConverter.GetBytes(NetHelper.FlipBytes(charData.Field<UInt16>("spell_key_29")));
                        outStream.Write(kBuffer, 0, 2);
                        kBuffer = BitConverter.GetBytes(NetHelper.FlipBytes(charData.Field<UInt16>("spell_key_30")));
                        outStream.Write(kBuffer, 0, 2);
                        kBuffer = BitConverter.GetBytes(NetHelper.FlipBytes(charData.Field<UInt16>("spell_key_31")));
                        outStream.Write(kBuffer, 0, 2);
                        kBuffer = BitConverter.GetBytes(NetHelper.FlipBytes(charData.Field<UInt16>("spell_key_32")));
                        outStream.Write(kBuffer, 0, 2);
                        kBuffer = BitConverter.GetBytes(NetHelper.FlipBytes(charData.Field<UInt16>("spell_key_33")));
                        outStream.Write(kBuffer, 0, 2);
                        kBuffer = BitConverter.GetBytes(NetHelper.FlipBytes(charData.Field<UInt16>("spell_key_34")));
                        outStream.Write(kBuffer, 0, 2);
                        kBuffer = BitConverter.GetBytes(NetHelper.FlipBytes(charData.Field<UInt16>("spell_key_35")));
                        outStream.Write(kBuffer, 0, 2);
                        kBuffer = BitConverter.GetBytes(NetHelper.FlipBytes(charData.Field<UInt16>("spell_key_36")));
                        outStream.Write(kBuffer, 0, 2);
                        kBuffer = BitConverter.GetBytes(NetHelper.FlipBytes(charData.Field<UInt16>("spell_key_37")));
                        outStream.Write(kBuffer, 0, 2);
                        kBuffer = BitConverter.GetBytes(NetHelper.FlipBytes(charData.Field<UInt16>("spell_key_38")));
                        outStream.Write(kBuffer, 0, 2);
                        kBuffer = BitConverter.GetBytes(NetHelper.FlipBytes(charData.Field<UInt16>("spell_key_39")));
                        outStream.Write(kBuffer, 0, 2);
                        kBuffer = BitConverter.GetBytes(NetHelper.FlipBytes(charData.Field<UInt16>("spell_key_40")));
                        outStream.Write(kBuffer, 0, 2);


                        outStream.WriteByte(0x00);
                        outStream.WriteByte(0x00);
                        outStream.WriteByte(0x00);
                        outStream.WriteByte(0x00);
                        outStream.WriteByte(0x00);
                        outStream.WriteByte(0x00);
                        outStream.WriteByte(0x00);
                        outStream.WriteByte(0x00);
                        outStream.WriteByte(0x00);
                        outStream.WriteByte(0x00);
                        outStream.WriteByte(0x00);
                        outStream.WriteByte(0x00);

                        outStream.WriteByte(charData.Field<Byte>("oplevel"));
                        outStream.WriteByte(charData.Field<Byte>("oplevel"));
                        outStream.WriteByte(charData.Field<Byte>("oplevel"));
                        outStream.WriteByte(charData.Field<Byte>("oplevel"));
                    }
                    return outStream;
                }
                public static MemoryStream HighScores(Int32 classId, DataTable dataTable, bool UDP = false)
                {
                    MemoryStream outStream = new MemoryStream();
                    outStream.WriteByte(0x00);
                    outStream.WriteByte((Byte)PacketOutFunction.HighScores);

                    outStream.Write(BitConverter.GetBytes(NetHelper.FlipBytes(classId)), 0, 4); // List (Class ID + 1)
                    outStream.Write(BitConverter.GetBytes(NetHelper.FlipBytes(dataTable.Rows.Count)), 0, 4); // Player Count

                    for (Int32 i = 1; i <= dataTable.Rows.Count; i++)
                    {
                        String name = dataTable.Rows[i - 1].Field<String>("name");
                        outStream.Write(Encoding.ASCII.GetBytes(name), 0, name.Length);
                        outStream.Seek((60 - name.Length), SeekOrigin.Current);
                        outStream.Write(BitConverter.GetBytes(NetHelper.FlipBytes((Int32) dataTable.Rows[i - 1].Field<Byte>("level"))), 0, 4);
                        outStream.Write(BitConverter.GetBytes(NetHelper.FlipBytes(dataTable.Rows[i - 1].Field<UInt64>("experience"))), 0, 4);
                    }

                    return outStream;
                }
            }

            public static class System
            {
				public static void PlaySoundToArena(SpellServer.Arena arena, GameSound.Sound sound, bool UDP = false)
                {
                    if (arena == null) return;

                    lock (arena.SyncRoot)
                    {
                        for (Int32 i = 0; i < arena.ArenaPlayers.Count; i++)
                        {
                            Network.Send(arena.ArenaPlayers[i].WorldPlayer, Arena.PlaySound(sound, 16000, 0, 0));
                        }
                    }
                }
                public static MemoryStream PlayWebMusic(String songName, bool UDP = false)
                {
                    MemoryStream outStream = new MemoryStream();
                    outStream.WriteByte(0x00);
                    outStream.WriteByte((Byte)PacketOutFunction.PlayWebMusic);
                    outStream.Write(Encoding.ASCII.GetBytes(songName), 0, songName.Length);
                    outStream.WriteByte(0x00);
                    return outStream;
                }
                public static MemoryStream DirectTextMessage(SpellServer.Player player, String message, bool UDP = false)
                {
                    MemoryStream outStream = new MemoryStream();
                    outStream.WriteByte(0x00);
                    outStream.WriteByte((Byte)PacketOutFunction.Chat);

                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);

                    if (player == null)
                    {
                        outStream.WriteByte(0x00);
                        outStream.WriteByte(0x00);
                    }
                    else
                    {
                        if (player.ActiveArena != null)
                        {
                            if (player.ActiveArenaPlayer != null)
                            {
                                outStream.Write(BitConverter.GetBytes(NetHelper.FlipBytes(player.ActiveArenaPlayer.ArenaPlayerId)), 0, 2);
                            }
                            else
                            {
                                outStream.WriteByte(0x00);
                                outStream.WriteByte(0xFF);
                            }
                        }
                        else
                        {
                            outStream.Write(BitConverter.GetBytes(NetHelper.FlipBytes(player.PlayerId)), 0, 2);
                        }
                    }
                    
                    outStream.WriteByte((Byte) ChatType.Whisper);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.Write(Encoding.ASCII.GetBytes(message), 0, message.Length);
                    return outStream;
                }
                public static void DrawBoundingBox(ArenaPlayer arenaPlayer, OrientedBoundingBox boundingBox, bool UDP = false)
                {
                    const Int16 spellId = 222;

                    if (arenaPlayer == null) return;
                    MemoryStream outStream;

                    /*for (Int32 i = 0; i < boundingBox.Corners.Length; i++)
                    {
                        outStream = new MemoryStream();
                        outStream.WriteByte(arenaPlayer.ArenaPlayerId);
                        outStream.WriteByte((Byte)PacketOutFunction.CastProjectile);
                        outStream.Write(BitConverter.GetBytes(NetHelper.FlipBytes(spellId)), 0, 2);
                        outStream.Write(BitConverter.GetBytes(NetHelper.FlipBytes((Int16) boundingBox.Corners[i].X)), 0, 2);
                        outStream.Write(BitConverter.GetBytes(NetHelper.FlipBytes((Int16) boundingBox.Corners[i].Y)), 0, 2);
                        outStream.Write(BitConverter.GetBytes(NetHelper.FlipBytes((Int16) boundingBox.Corners[i].Z)), 0, 2);
                        outStream.WriteByte(0x0C);
                        outStream.WriteByte(0x23);
                        outStream.WriteByte(0x00);
                        outStream.WriteByte(0x00);
                        outStream.WriteByte(0x00);
                        outStream.WriteByte(0x00);
                        //outStream.WriteByte(0x00);
                        //outStream.WriteByte(0x00);
                        Network.Send(arenaPlayer.WorldPlayer, outStream);
                    }*/

                    outStream = new MemoryStream();
                    outStream.WriteByte(arenaPlayer.ArenaPlayerId);
                    outStream.WriteByte((Byte)PacketOutFunction.CastProjectile);
                    outStream.Write(BitConverter.GetBytes(NetHelper.FlipBytes(spellId)), 0, 2);
                    outStream.Write(BitConverter.GetBytes(NetHelper.FlipBytes((Int16)boundingBox.Origin.X)), 0, 2);
                    outStream.Write(BitConverter.GetBytes(NetHelper.FlipBytes((Int16)boundingBox.Origin.Y)), 0, 2);
                    outStream.Write(BitConverter.GetBytes(NetHelper.FlipBytes((Int16)boundingBox.Origin.Z)), 0, 2);
                    outStream.WriteByte(0x0C);
                    outStream.WriteByte(0x23);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    //outStream.WriteByte(0x00);
                    //outStream.WriteByte(0x00);
                    Network.Send(arenaPlayer.WorldPlayer, outStream);
                }
            }

            public static class World
            {
                public static MemoryStream SpawnPlayer(SpellServer.Player player, bool UDP = false)
                {
                    MemoryStream outStream = new MemoryStream();
                    outStream.WriteByte(0x00);
                    outStream.WriteByte((Byte)PacketOutFunction.SpawnPlayer);

                    outStream.WriteByte(0x00); //Object ID
                    outStream.WriteByte(0x00); //Object ID
                    outStream.WriteByte(0x00); // X Coord
                    outStream.WriteByte(0x00); // X Coord
                    outStream.WriteByte(0x00); // Y Coord
                    outStream.WriteByte(0x00); // Y Coord
                    outStream.WriteByte(0x00); // Z Coord
                    outStream.WriteByte(0x00); // Z Coord
                    outStream.WriteByte(0xC9); // Model
                    outStream.WriteByte(0x00); // Model
                    outStream.WriteByte(0x00); // Type (0=player)
                    outStream.WriteByte(0x00); // Subtype/Class/State
                    outStream.WriteByte(0x00); // Unknown
                    outStream.WriteByte(0x00); // Unknown
                    outStream.WriteByte(0x00); // Optional

                    return outStream;
                }
                public static MemoryStream PlayerJoin(SpellServer.Player player, bool UDP = false)
                {
                    MemoryStream outStream = new MemoryStream();
                    outStream.WriteByte((Byte) player.PlayerId);
                    outStream.WriteByte((Byte)PacketOutFunction.PlayerJoin);
                    outStream.Write(BitConverter.GetBytes(NetHelper.FlipBytes(player.PlayerId)), 0, 2);
                    outStream.WriteByte(player.TableId > 0 ? player.TableId : player.ActiveArena.ArenaId);
                    outStream.WriteByte((Byte) player.ActiveTeam);
                    outStream.Write(Encoding.ASCII.GetBytes(player.ActiveCharacter.Name), 0, player.ActiveCharacter.Name.Length);
                    outStream.Seek((12 - player.ActiveCharacter.Name.Length), SeekOrigin.Current);
                    outStream.WriteByte((Byte) player.ActiveCharacter.Class);
                    outStream.WriteByte(player.ActiveCharacter.Level);
                    outStream.WriteByte(player.ActiveCharacter.OpLevel);
                    outStream.WriteByte(0x00);
                    return outStream;
                }
                public static MemoryStream PlayerLeave(SpellServer.Player player, bool UDP = false)
                {
                    MemoryStream outStream = new MemoryStream();
                    outStream.WriteByte((Byte) player.PlayerId);
                    outStream.WriteByte((Byte)PacketOutFunction.PlayerLeave);
                    outStream.Write(BitConverter.GetBytes(NetHelper.FlipBytes(player.PlayerId)), 0, 2);
                    return outStream;
                }
                public static MemoryStream PlayerEnterLarge(SpellServer.Player player, MemoryStream outStream, bool UDP = false)
                {
                      
                    if (outStream == null)
                    {
                        outStream = new MemoryStream();
                        outStream.WriteByte(0x00);
                        outStream.WriteByte((Byte)PacketOutFunction.PlayerEnterLarge);                        
                    }

                    outStream.Write(Encoding.ASCII.GetBytes(player.ActiveCharacter.Name), 0, player.ActiveCharacter.Name.Length);
                    outStream.Seek((12 - player.ActiveCharacter.Name.Length), SeekOrigin.Current);
                    outStream.Write(BitConverter.GetBytes(NetHelper.FlipBytes(player.PlayerId)), 0, 2);
                    outStream.WriteByte(player.ActiveArena != null ? player.ActiveArena.ArenaId : player.TableId);
                    outStream.WriteByte((Byte) player.ActiveTeam);
                    outStream.WriteByte((Byte) player.ActiveCharacter.Class);
                    outStream.WriteByte(player.ActiveCharacter.Level);
                    outStream.WriteByte(player.ActiveCharacter.OpLevel);
                    outStream.WriteByte(0x00);
                    return outStream;
                }
                public static MemoryStream WorldEnterLarge(SpellServer.Arena arena, MemoryStream outStream, bool UDP = false)
                {

                    if (outStream == null)
                    {
                        outStream = new MemoryStream();
                        outStream.WriteByte(0x00);
                        outStream.WriteByte((Byte)PacketOutFunction.WorldEnterLarge);
                    }

                    outStream.WriteByte(arena.ArenaId);
                    outStream.WriteByte(0xFF);
                    outStream.Write(Encoding.ASCII.GetBytes(arena.GameName), 0, arena.GameName.Length);
                    outStream.Seek(20 - arena.GameName.Length, SeekOrigin.Current);
                    outStream.Write(Encoding.ASCII.GetBytes(arena.Grid.Name), 0, arena.Grid.Name.Length);
                    outStream.Seek(10 - arena.Grid.Name.Length, SeekOrigin.Current);
                    outStream.Write(Encoding.ASCII.GetBytes(arena.Founder), 0, arena.Founder.Length);
                    outStream.Seek(10 - arena.Founder.Length, SeekOrigin.Current);
                    outStream.Write(Encoding.ASCII.GetBytes(arena.ShortGameName), 0, arena.ShortGameName.Length);
                    outStream.Seek(10 - arena.ShortGameName.Length, SeekOrigin.Current);
                    return outStream;
                }
                public static MemoryStream TableCreated(Table table, bool UDP = false)
                {
                    MemoryStream outStream = new MemoryStream();
                    
                    outStream.WriteByte(0x00);
                    outStream.WriteByte((Byte)PacketOutFunction.TableCreated);
                    outStream.WriteByte((Byte) table.TableId);
                    outStream.WriteByte((Byte) table.Type);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.Write(Encoding.ASCII.GetBytes(table.Name), 0, table.Name.Length);
                    outStream.Seek((20 - table.Name.Length), SeekOrigin.Current);
                    outStream.Write(Encoding.ASCII.GetBytes(table.Founder), 0, table.Founder.Length);
                    outStream.Seek((10 - table.Founder.Length), SeekOrigin.Current);
                    outStream.WriteByte(0x01);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    return outStream;
                }
                public static MemoryStream TableDeleted(Table table, bool UDP = false)
                {
                    MemoryStream outStream = new MemoryStream();
                    outStream.WriteByte(0x00);
                    outStream.WriteByte((Byte)PacketOutFunction.TableDeleted);
                    outStream.WriteByte((Byte) table.TableId);
                    return outStream;
                }
                public static MemoryStream ArenaCreated(SpellServer.Arena arena, bool UDP = false)
                {
                    MemoryStream outStream = new MemoryStream();
                    outStream.WriteByte(0x00);
                    outStream.WriteByte((Byte)PacketOutFunction.ArenaCreated);
                    outStream.WriteByte(arena.ArenaId);
                    outStream.WriteByte(0x00);
                    outStream.Write(Encoding.ASCII.GetBytes(arena.GameName), 0, arena.GameName.Length);
                    outStream.Seek(20 - arena.GameName.Length, SeekOrigin.Current);
                    outStream.Write(Encoding.ASCII.GetBytes(arena.Grid.Name), 0, arena.Grid.Name.Length);
                    outStream.Seek(10 - arena.Grid.Name.Length, SeekOrigin.Current);
                    outStream.Write(Encoding.ASCII.GetBytes(arena.Founder), 0, arena.Founder.Length);
                    outStream.Seek(10 - arena.Founder.Length, SeekOrigin.Current);
                    outStream.Write(Encoding.ASCII.GetBytes(arena.ShortGameName), 0, arena.ShortGameName.Length);
                    return outStream;
                }
                public static MemoryStream ArenaState(SpellServer.Arena arena, SpellServer.Player player, bool UDP = false)
                {
                    MemoryStream outStream = new MemoryStream();

                    outStream.WriteByte(0x00);
                    outStream.WriteByte((Byte)PacketOutFunction.ArenaState);
                    outStream.WriteByte(arena.ArenaId); // 0x00 == 0
                    outStream.WriteByte(0x01); // Enables the game
                    outStream.WriteByte(arena.MaxPlayers);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte((Byte)arena.EndState); // Winner
                    outStream.WriteByte(arena.LevelRange);
                    outStream.WriteByte(arena.TableId);
                    outStream.WriteByte(Convert.ToByte(arena.ArenaPlayers.Count));
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(Convert.ToByte(!arena.ArenaTeams.Dragon.Shrine.IsDisabled)); // Enables Chaos Team
                    outStream.WriteByte(Convert.ToByte(!arena.ArenaTeams.Gryphon.Shrine.IsDisabled)); // Enables Order Team
                    outStream.WriteByte(Convert.ToByte(!arena.ArenaTeams.Pheonix.Shrine.IsDisabled)); // Enables Balance Team
                    outStream.WriteByte(0x00); // Enables Rogue Team
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.Write(BitConverter.GetBytes(NetHelper.FlipBytes(arena.baseTime + arena.elaspedSeconds)), 0, 4); //28
                    //outStream.WriteByte(0x00);
                    //outStream.WriteByte(0x00); //outStream.Write(BitConverter.GetBytes(NetHelper.FlipBytes(arena.TimeLimit)), 0, 2);
                    //outStream.WriteByte(0x00);
                    outStream.Write(BitConverter.GetBytes(NetHelper.FlipBytes(arena.baseTime)), 0, 4); //32
                    //outStream.WriteByte(0x00); //outStream.Write(BitConverter.GetBytes(NetHelper.FlipBytes((Int16)arena.Duration.ElapsedSeconds)), 0, 2);
                    //outStream.WriteByte(0x00);
                    //outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte((Byte)arena.CurrentState);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00); //0x28 == 40
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00); //outStream.WriteByte(arena.CountdownTick == null ? (Byte)0x00 : (Byte)(119 - (arena.CountdownTick.ElapsedSeconds * 4)));
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(Convert.ToByte(!arena.ArenaTeams.Pheonix.Shrine.IsDead)); //Team 3 nexus not destroyed
                    outStream.WriteByte(Convert.ToByte(!arena.ArenaTeams.Gryphon.Shrine.IsDead)); //Team 2 nexus not destroyed
                    outStream.WriteByte(Convert.ToByte(!arena.ArenaTeams.Dragon.Shrine.IsDead)); //Team 1 nexus not destoryed
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(player.LastArenaId);

                    /*outStream.WriteByte(0x00);
                    outStream.WriteByte((Byte)PacketOutFunction.ArenaState);
                    outStream.WriteByte(arena.ArenaId);
                    outStream.WriteByte(0x01); // Enables the game
                    outStream.WriteByte(arena.MaxPlayers);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte((Byte) arena.EndState); // Winner
                    outStream.WriteByte(arena.LevelRange);
                    outStream.WriteByte(arena.TableId);
                    outStream.WriteByte(Convert.ToByte(arena.ArenaPlayers.Count));
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(Convert.ToByte(!arena.ArenaTeams.Chaos.Shrine.IsDisabled)); // Enables Chaos Team
                    outStream.WriteByte(Convert.ToByte(!arena.ArenaTeams.Order.Shrine.IsDisabled)); // Enables Order Team
                    outStream.WriteByte(Convert.ToByte(!arena.ArenaTeams.Balance.Shrine.IsDisabled)); // Enables Balance Team
                    outStream.WriteByte(0x00); // Enables Rogue Team
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.Write(BitConverter.GetBytes(NetHelper.FlipBytes(arena.TimeLimit)), 0, 2);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.Write(BitConverter.GetBytes(NetHelper.FlipBytes((Int16)arena.Duration.ElapsedSeconds)), 0, 2);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte((Byte)arena.CurrentState);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(arena.CountdownTick == null ? (Byte) 0x00 : (Byte) (119 - (arena.CountdownTick.ElapsedSeconds*4)));
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(player.LastArenaId);*/
                    return outStream;
                }
                public static MemoryStream ArenaForceEndState(SpellServer.Arena arena, SpellServer.Player player, bool UDP = false)
                {
                    MemoryStream outStream = new MemoryStream();
                    outStream.WriteByte(0x00);
                    outStream.WriteByte((Byte)PacketOutFunction.ArenaState);
                    outStream.WriteByte(arena.ArenaId);
                    outStream.WriteByte(0x01);
                    outStream.WriteByte(arena.MaxPlayers);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte((Byte)SpellServer.Arena.State.Ended);
                    outStream.WriteByte(arena.LevelRange);
                    outStream.WriteByte(arena.TableId);
                    outStream.WriteByte(Convert.ToByte(arena.ArenaPlayers.Count));
                    outStream.WriteByte(0x00);  
                    outStream.WriteByte(Convert.ToByte(!arena.ArenaTeams.Dragon.Shrine.IsDisabled));
                    outStream.WriteByte(Convert.ToByte(!arena.ArenaTeams.Gryphon.Shrine.IsDisabled));
                    outStream.WriteByte(Convert.ToByte(!arena.ArenaTeams.Pheonix.Shrine.IsDisabled)); 
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.Write(BitConverter.GetBytes(NetHelper.FlipBytes(arena.TimeLimit)), 0, 2);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.Write(BitConverter.GetBytes(NetHelper.FlipBytes((Int16)arena.Duration.ElapsedSeconds)), 0, 2);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte((Byte)SpellServer.Arena.State.Ended);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(arena.CountdownTick == null ? (Byte)0x00 : (Byte)(119 - (arena.CountdownTick.ElapsedSeconds * 4)));
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(0x00);
                    outStream.WriteByte(player.LastArenaId);
                    return outStream;
                }
                public static MemoryStream ArenaDeleted(SpellServer.Arena arena, bool UDP = false)
                {
                    MemoryStream outStream = new MemoryStream();
                    outStream.WriteByte(0x00);
                    outStream.WriteByte((Byte)PacketOutFunction.ArenaDeleted);
                    outStream.WriteByte(arena.ArenaId);
                    return outStream;
                }
                
            }
        }
    }
}
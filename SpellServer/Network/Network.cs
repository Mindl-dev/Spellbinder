using Helper;
using Helper.Network;
using MySqlX.XDevAPI;
using Org.BouncyCastle.Asn1.X509;
using SpellServer.Properties;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Remoting.Messaging;
using System.Security.Permissions;
using System.Threading;

namespace SpellServer
{
    public static class Network
    {
        [Flags]
        public enum SendToType
        {
            None = 0x00,
            All = 0x01,
            World = 0x02,
            Tavern = 0x04,
            Table = 0x08,
            Arena = 0x10,
            Team = 0x20,
        }

		private const Int32 PacketHeaderFooterLength = 14;
	    private static TcpListener _gameListener;
        public static UdpClient _gameUDPListener;
        public static bool _udpServerRunning = true;
        private static readonly Dictionary<IPEndPoint, Player> _udpClients = new Dictionary<IPEndPoint, Player>();

        public static void Listen()
        {
            _gameListener = new TcpListener(new IPAddress(0), Settings.Default.ListenPort);
            _gameListener.Server.LingerState = new LingerOption(true, 10);
            _gameListener.Server.NoDelay = true;
            _gameListener.Server.ReceiveTimeout = 10000;
            _gameListener.Server.SendTimeout = 10000;
            
            while (!_gameListener.Server.IsBound)
            {
                try
                {
                    _gameListener.Start();
                }
                catch (SocketException)
                {
					foreach (Process p in Process.GetProcesses().Where(p => p.ProcessName == "SpellServer" && p.Handle != Process.GetCurrentProcess().Handle))
					{
						p.Kill();
						p.WaitForExit();
					}
                }   
            }

            _gameListener.BeginAcceptTcpClient(OnAcceptConnectionTCP, _gameListener);

            Program.ServerForm.MainLog.WriteMessage(String.Format("Server listening on TCP port {0}.", Settings.Default.ListenPort), Color.Blue);

            _gameUDPListener = new UdpClient(Settings.Default.UDPPort);
            _gameUDPListener.Client.ReceiveBufferSize = 65536;
            _gameUDPListener.Client.SendBufferSize = 65536;
            _gameUDPListener.DontFragment = true;
            

            BeginUdpReceive(); // start the async receive loop

            Program.ServerForm.MainLog.WriteMessage(String.Format("Server listening on UDP port {0}.", Settings.Default.UDPPort), Color.Blue);

        }

    private class UdpPacket
        {
            public Player Player { get; set; }
            public byte[] Data { get; set; }
        }
        private static void OnAcceptConnectionTCP(IAsyncResult asyn)
        {
            TcpListener listener = (TcpListener)asyn.AsyncState;
            TcpClient client = listener.EndAcceptTcpClient(asyn);

            listener.BeginAcceptTcpClient(OnAcceptConnectionTCP, listener);

            new Player(client);
        }

        private static void BeginUdpReceive()
        {
            _gameUDPListener.BeginReceive(OnUdpPacketReceive, null);
        }

        private static void OnUdpPacketReceive(IAsyncResult ar)
        {
            try
            {
                IPEndPoint remoteEP = null;
                byte[] data = _gameUDPListener.EndReceive(ar, ref remoteEP);
                int bytesReceived = data.Length;

                // Find player by IP (since UDP comes after TCP)
                var player = Player.Players.Values.FirstOrDefault(p =>
                {
                    return p.TcpClient?.Client?.RemoteEndPoint is IPEndPoint tcpEP &&
                           tcpEP.Address.Equals(remoteEP.Address);
                });

                if (player != null)
                {

                    player.LastKnownUdpEndpoint = remoteEP;

                    // Process the UDP packet exactly like TCP
                    Program.ServerForm.MainLog.WriteMessage(String.Format("UDP received: {0},{1}",bytesReceived.ToString(), remoteEP.Address.ToString()), Color.Blue);
                    //player.BindUdp(_gameUDPListener, remoteEP, player.IpAddress);
                    Network.GameRecvUdp(player, data, bytesReceived);

                }

                BeginUdpReceive(); // continue listening
            }
            catch (ObjectDisposedException) // server shutting down
            {
                Program.ServerForm.MainLog.WriteMessage($"UDP ObjectDisposedException", Color.Red);
                BeginUdpReceive();
            }
            catch (Exception ex)
            {
                Program.ServerForm.MainLog.WriteMessage($"UDP receive error: {ex.Message}", Color.Red);
                BeginUdpReceive();
            }
            finally
            {
                // THIS MUST BE IN FINALLY — ALWAYS RESTART
                if (Network._gameUDPListener?.Client != null)
                    Network._gameUDPListener.BeginReceive(OnUdpPacketReceive, null);
            }
        }
        public static void Disconnect(Player player)
        {
            player.TcpClient.Client.DisconnectAsync(new SocketAsyncEventArgs());

            Program.ServerForm.MainLog.WriteMessage(String.Format("{0} has disconnected. ({1})", player.IsLoggedIn ? player.Username : player.IpAddress, player.DisconnectReason), Color.BlueViolet);

            //player.UDPDisconnect();

            if (PlayerManager.Players.Contains(player))
            {
                if (player.IsLoggedIn)
                {
                    if (player.ActiveArena != null)
                    {
                        if (player.ActiveArenaPlayer != null)
                        {
                            player.ActiveArena.PlayerLeft(player.ActiveArenaPlayer);
                        }
                    }

                    if (player.ActiveCharacter != null)
                    {
	                    MySQL.OnlineCharacters.SetOffline(player.ActiveCharacter.CharacterId);
                    }

                    SendTo(player, GamePacket.Outgoing.World.PlayerLeave(player), SendToType.Tavern, false);
                }

				MySQL.OnlineAccounts.SetOffline(player.AccountId);
            }

            PlayerManager.Players.Remove(player);
        }

        /*private static Int32 GetChecksum(Byte[] data, Int32 position, Int32 length)
        {
            Int32 x = 0x7E, y = x;

            for (Int32 i = 2; i < (length - 2); i++)
            {
                x = (data[position + i] + x) & 0xFF;
                y = (x + y) & 0xFF;
            }

            return (y - ((x + y) << 8) & 0xFFFF);
        }*/
        private static Int32 GetChecksum(Byte[] data, Int32 position, Int32 length)
        {
            // The assembly uses bl (sumA) and dl (sumB) initialized to 126 (0x7E)
            byte sumA = 0x7E;
            byte sumB = 0x7E;

            // IMPORTANT: The assembly range 'esi' is PayloadLength + 10.
            // Total Packet is PayloadLength + 14. 
            // This means we skip the first 2 bytes (1B 1B) 
            // and stop before the last 2 bytes (Checksum).
            int start = position + 2;
            int end = position + length - 2;

            for (int i = start; i < end; i++)
            {
                // byte cast ensures 0-255 wrapping like the 8-bit registers bl/dl
                sumA = (byte)(sumA + data[i]);
                sumB = (byte)(sumB + sumA);
            }

            unchecked
            {
                // Final assembly logic: return v4 - ((v4 + a1) << 8)
                // where v4 is sumB and a1 is sumA
                int eax_reg = sumB;
                int ecx_reg = (byte)(sumA + sumB);
                int result = eax_reg - (ecx_reg << 8);

                return (UInt16)result;
            }
        }

        public static Int32 GameRecv(Player player, Byte[] data, Int32 size)
        {
            Int32 position = 0;

            while (position < size - 1)
            {
                if (data[position] != 0x1B || data[position + 1] != 0x1B)
                {
                    position++;
                    continue;
                }

                if (position + 6 > size) break;

                Int32 rawPayloadLen = NetHelper.FlipBytes(BitConverter.ToInt16(data, position + 2));
                Int32 fullPacketLength = rawPayloadLen + PacketHeaderFooterLength;
                Int32 packetEnd = position + fullPacketLength;

                if (rawPayloadLen <= 0 || packetEnd > size)
                {
                    // This might be a false positive 1B 1B or a partial packet.
                    // Move forward and keep scanning.
                    position++;
                    continue;
                }

                UInt16 receivedChecksum = NetHelper.FlipBytes(BitConverter.ToUInt16(data, packetEnd - 2));
                UInt16 calculatedChecksum = (UInt16)GetChecksum(data, position, fullPacketLength);

                if (receivedChecksum != calculatedChecksum)
                {
                    // Checksum failed. Likely a false positive 1B 1B. 
                    //position++;
                    //continue;
                    Program.ServerForm.MainLog.WriteMessage($"[CRC] Op: 0x{data[position + 11]:X2} Client:{receivedChecksum:X4} Server:{calculatedChecksum:X4}", Color.Orange);
                }

                Int32 packetNumber = NetHelper.FlipBytes(BitConverter.ToUInt16(data, position + 4));

                if (packetNumber != player.PacketCounter)
                {                    
                    player.PacketCounter = packetNumber;
                }
                                
                player.PacketCounter++;

                Byte opcode = data[position + 11];

                using (MemoryStream inStream = new MemoryStream(data, position + 10, fullPacketLength - 12, false))
                {
                    switch (data[position + 11])
                    {
                        case 0x01:
                        {
                            GamePacket.Incoming.Arena.PlayerMoveState(player, inStream);
                            break;
                        }
                        case 0xB8:
                        case 0x03:
                        {
                            GamePacket.Incoming.Player.EnterWorld(player, inStream);
                            break;
                        }
                        case 0x04:
                        {
                            GamePacket.Incoming.Player.ExitWorld(player);
                            break;
                        }
                        case 0x07:
                        {
                            GamePacket.Incoming.Player.Chat(player, inStream);
                            break;
                        }
                        case 0x0B:
                        {
                            GamePacket.Incoming.Player.Heartbeat(player, inStream);
                            break;
                        }
                        case 0x0F:
                        {
                            GamePacket.Incoming.Login.Authenticate(player, inStream);
                            break;
                        }
                        case 0x10:
                        {
                            GamePacket.Incoming.Login.Disconnect(player);
                            break;
                        }
                        case 0x11:
                        {
                            GamePacket.Incoming.World.RequestedArenaStatus(player);
                            break;
                        }
                        case 0x12:
                        {
                            GamePacket.Incoming.Arena.PlayerMoveStateShort(player, inStream);
                            break;
                        }
                        case 0x17:
                        {
                            GamePacket.Incoming.Player.HasEnteredWorld(player);
                            break;
                        }
                        case 0x22:
                        {
                            GamePacket.Incoming.World.CreateArena(player, inStream);
                            break;
                        }
                        case 0x23:
                        {
                            GamePacket.Incoming.World.DeleteArena(player, inStream);
                            break;
                        }
                        case 0x25:
                        {
                            GamePacket.Incoming.Arena.CastBolt(player, inStream);
                            break;
                        }
                        case 0x26:
                        {
                            GamePacket.Incoming.World.RequestedPlayer(player, inStream);
                            break;
                        }
                        case 0x28:
                        {
                            GamePacket.Incoming.Arena.BiasedPool(player, inStream);
                            break;
                        }
                        case 0x2A:
                        {
                            GamePacket.Incoming.Arena.BiasedShrine(player, inStream);
                            break;
                        }
                        case 0x2C:
                        {
                            GamePacket.Incoming.Arena.CastDispell(player, inStream);
                            break;
                        }
                        case 0x2D:
                        {
                            GamePacket.Incoming.Arena.CastTargeted(player, inStream);
                            break;
                        }
                        case 0x2F:
                        {
                            GamePacket.Incoming.Arena.ThinDamage(player, inStream);
                            break;
                        }
                        case 0x30:
                        {
                            GamePacket.Incoming.Arena.CalledGhost(player, inStream);
                            break;
                        }
                        case 0x31:
                        {
                            GamePacket.Incoming.Arena.ActivatedTrigger(player, inStream);
                            break;
                        }
                        case 0x32:
                        {
                            GamePacket.Incoming.World.RequestedArena(player, inStream);
                            break;
                        }
                        case 0x33:
                        {
                            GamePacket.Incoming.World.RequestedAllPlayers(player);
                            break;
                        }
                        case 0x35:
                        {
                            GamePacket.Incoming.World.RequestedAllArenas(player);
                            break;
                        }
                        case 0x40:
                        {
                            GamePacket.Incoming.World.RequestEnterLarge(player);
                            break;
                        }
                        case 0x45:
                        {
                            GamePacket.Incoming.World.CreateTable(player, inStream);
                            break;
                        }
                        case 0x46:
                        {
                            GamePacket.Incoming.World.DeleteTable(inStream);
                            break;
                        }
                        case 0x47:
                        {
                            GamePacket.Incoming.World.RequestedAllTables(player);
                            break;
                        }
                        case 0x52:
                        {
                            GamePacket.Incoming.Player.InviteToTable(inStream);
                            break;
                        }
                        case 0x53:
                        {
                            GamePacket.Incoming.Player.SwitchedToTableOrArena(player, inStream);
                            break;
                        }
                        case 0x54:
                        {
                            GamePacket.Incoming.Study.RequestCharacterInSlot(player, inStream);
                            break;
                        }
                        case 0x57:
                        {
                            GamePacket.Incoming.Character.Save(player, inStream);
                            break;
                        }
                        case 0x63:
                        {
                            GamePacket.Incoming.Study.IsNameTaken(player, inStream);
                            break;
                        }
                        case 0x68:
                        {
                            GamePacket.Incoming.Character.Delete(player, inStream);
                            break;
                        }
                        case 0x6A:
                        {
                            GamePacket.Incoming.Study.IsNameValid(player, inStream);
                            break;
                        }
                        case 0x7C:
                        {
                            GamePacket.Incoming.Arena.PlayerInit(player, inStream); 
                            break;
                        }
                        /*case 0xA0:
                        {
                            GamePacket.Incoming.Arena.ObjectDeath(player, inStream);
                            break;
                        }*/
                        case 0xA1:
                        {
                            GamePacket.Incoming.Study.HighScores(player, inStream);
                            break;
                        }
                        case 0xA4:
                        {
                            GamePacket.Incoming.Arena.Yank(player, inStream);
                            break;
                        }
                        case 0xAC:
                        {
                            GamePacket.Incoming.Arena.Jump(player, inStream);
                            break;
                        }
                        case 0xAD:
                        {
                            GamePacket.Incoming.Arena.God(player, inStream);
                            break;
                        }
                        case 0xB0:
                        {
                            GamePacket.Incoming.Arena.CastProjectile(player, inStream);
                            break;
                        }
                        case 0xB1:
                        {
                            GamePacket.Incoming.Arena.TappedAtShrine(player);
                            break;
                        }
                        case 0xB2:
                        {
                            GamePacket.Incoming.Arena.CastRune(player, inStream);
                            break;
                        }
                        case 0xB3:
                        {
                            GamePacket.Incoming.Arena.CastEffect(player, inStream);
                            break;
                        }
                        case 0xB4:
                        {
                            GamePacket.Incoming.Arena.CastWall(player, inStream);
                            break;
                        }
                        /*case 0xB7:
                        {
                            GamePacket.Incoming.World.HandleCabal(player, inStream);
                            break;
                        }*/
                        case 0xBC:
                        {
                            GamePacket.Incoming.Player.EstablishDatagram(player, inStream);
                            break;
                        }
                        case 0xE0:
                        {
                            GamePacket.Incoming.MageHook.HackNotification(player, inStream);
                            break;
                        }
                        case 0xE1:
                        {
                            GamePacket.Incoming.MageHook.CheatProgramNotification(player, inStream);
                            break;
                        }
                        default:
                        {
                            break;
                        }
                    }
                }
                
                position += fullPacketLength;
            }

            return position;
        }

        public static Int32 GameRecvUdp(Player player, Byte[] data, Int32 size)
        {
            Int32 position = 0;
            byte cmd = 0;
            Int32 length = 0;
            Int32 payloadEnd = 0;

            while (position < size)
            {
                // UDP Header (10 bytes):
                // 0-1 : packet length (network order)
                // 2-3 : sequence number (network order)
                // 4-5 : ??
                // 6-7 : ??
                // 8   : padding (0x00)
                // 9   : command byte
                // ... : payload
                // -2  : checksum (2 bytes)

                

                if (BitConverter.ToInt16(data, position) == 0x1B1B)
                {
                    cmd = data[position + 9];
                    length = NetHelper.FlipBytes(BitConverter.ToInt16(data, position + 2)) + PacketHeaderFooterLength;
                }
                else
                {
                    cmd = data[position + 9];
                    length = NetHelper.FlipBytes(BitConverter.ToInt16(data, position)) + 12;
                }

                payloadEnd = position + length;

                if (length < 12 || length > size - position)
                    break;
                if (payloadEnd > size)
                    break;

                // Verify checksum (last 2 bytes)
                //ushort receivedChecksum = BitConverter.ToUInt16(data, payloadEnd - 2);
                //ushort calculatedChecksum = CalculateUdpChecksum(data, position, packetLength - 2);

                //if (receivedChecksum != calculatedChecksum)
                //{
                //    Program.ServerForm.MainLog.WriteMessage(
                //        $"[UDP] Bad checksum from {player.IpAddress}", Color.Orange);
                //    break;
                //}

                // Extract payload
                MemoryStream inStream = new MemoryStream(data, position + 10, length - 10, false);

                // Process command
                switch (cmd)
                {
                    case 0x0B: GamePacket.Incoming.Player.Heartbeat(player, inStream, true); break;

                    case 0x57: GamePacket.Incoming.Character.Save(player, inStream, true); break;

                    case 0xBC: GamePacket.Incoming.Player.EstablishDatagram(player, inStream, true); break;
                    
                    // ... all other UDP commands ...
                    default:
                        Program.ServerForm.MainLog.WriteMessage(
                            $"[UDP] Unknown CMD 0x{cmd:X2} from {player.Username}", Color.Yellow);
                        break;
                }

                inStream.Dispose();
                position += length;
            }

            return position;
        }

        public static void Send(Player player, MemoryStream inStream, bool UDP = false)
        {
            try
            {
                Packet packet = new Packet(inStream);
                if (!UDP)
                {
                    player.TcpClient.Client.BeginSend(packet.PacketData, 0, packet.PacketData.Length, SocketFlags.None, SendCallback, new SendCallbackSyncResult(player));
                }
                else
                {
                    _gameUDPListener.BeginSend(packet.PacketData, packet.PacketData.Length, player.UdpIpAddress, BitConverter.ToInt16(player.UdpportBE, 0), SendCallbackUDP, new SendCallbackSyncResult(player));
                }
            }
            catch (Exception)
            {
                player.DisconnectReason = "Send (Stream) Error";
                player.Disconnect = true;
            }
        }

        /*public static void SendUDP(Player player, MemoryStream inStream)
        {
            try
            {
                Packet packet = new Packet(inStream);

                Program.ServerForm.MainLog.WriteMessage(String.Format("[UDP] Sent CMD 0x87: {0}, {1}", player.UdpIpAddress, BitConverter.ToString(player.UdpportBE)), Color.Blue);

                _gameUDPListener.BeginSend(packet.PacketData, packet.PacketData.Length, player.UdpIpAddress, BitConverter.ToInt16(player.UdpportBE, 0), SendCallbackUDP, new SendCallbackSyncResult(player));
                //_gameUDPListener.BeginSend(packet.PacketData, packet.PacketData.Length, player.IpAddress, player.Udpport, SendCallbackUDP, new SendCallbackSyncResult(player));

            }
            catch (Exception)
            {
                player.DisconnectReason = "Send (Stream) Error";
                player.Disconnect = true;
            }
        }*/

        public static void Send(Player player, Packet packet, bool UDP = false)
        {
            try
            {
                if (!UDP)
                {
                    player.TcpClient.Client.BeginSend(packet.PacketData, 0, packet.PacketData.Length, SocketFlags.None, SendCallback, new SendCallbackSyncResult(player));
                }
                else
                {
                    _gameUDPListener.BeginSend(packet.PacketData, packet.PacketData.Length, player.UdpIpAddress, BitConverter.ToInt16(player.UdpportBE, 0), SendCallbackUDP, new SendCallbackSyncResult(player));
                }
            }
            catch (Exception)
            {
                player.DisconnectReason = "Send (Byte) Error";
                player.Disconnect = true;
            }
        }

        /*public static void SendUDP(Player player, Packet packet)
        {
            try
            {
                Program.ServerForm.MainLog.WriteMessage(String.Format("[UDP] Sent CMD 0x87: {0}, {1}", player.IpAddress, BitConverter.ToString(player.UdpportBE)), Color.Blue);

                _gameUDPListener.BeginSend(packet.PacketData, packet.PacketData.Length, player.UdpIpAddress, BitConverter.ToInt16(player.UdpportBE,0), SendCallbackUDP, new SendCallbackSyncResult(player));
                //_gameUDPListener.BeginSend(packet.PacketData, packet.PacketData.Length, player.IpAddress, player.Udpport, SendCallbackUDP, new SendCallbackSyncResult(player));
            }
            catch (Exception)
            {
                player.DisconnectReason = "Send (Byte) Error";
                player.Disconnect = true;
            }
        }*/

        public static void SendTo(MemoryStream inStream, SendToType sendToType)
        {
            Packet packet = new Packet(inStream);

            for (Int16 i = 0; i < PlayerManager.Players.Count; i++)
            {
                Player p = PlayerManager.Players[i];

                if (p == null) continue;

                if (sendToType.HasFlag(SendToType.All))
                {
                    Send(p, packet);
                    continue;
                }

                if (sendToType.HasFlag(SendToType.World))
                {
                    if (p.TableId > 0)
                    {
                        Send(p, packet);
                        continue;
                    }
                }

                if (sendToType.HasFlag(SendToType.Tavern))
                {
                    if (p.TableId >= 50)
                    {
                        Send(p, packet);
                    }
                }
            }
        }

        public static void SendTo(Player player, MemoryStream inStream, SendToType sendToType, Boolean toSelf)
        {
            if (player == null) return;

            Packet packet = new Packet(inStream);

            for (Int16 i = 0; i < PlayerManager.Players.Count; i++)
            {
                Player p = PlayerManager.Players[i];

                if (p == null || (!toSelf && p == player)) continue;

                if (sendToType.HasFlag(SendToType.All))
                {
                    Send(p, packet);
                    continue;
                }

                if (sendToType.HasFlag(SendToType.World))
                {
                    if (p.TableId > 0)
                    {
                        Send(p, packet);
                        continue;
                    }
                }

                if (sendToType.HasFlag(SendToType.Tavern))
                {
                    if (p.TableId >= 50)
                    {
                        Send(p, packet);
                        continue;
                    }
                }

                if (sendToType.HasFlag(SendToType.Table))
                {
                    if (p.TableId == player.TableId)
                    {
                        Send(p, packet);
                        continue;
                    }
                }

                if (sendToType.HasFlag(SendToType.Arena))
                {
                    if (player.ActiveArena != null && p.ActiveArena == player.ActiveArena)
                    {
                        Send(p, packet);
                        continue;
                    }
                }

                if (sendToType.HasFlag(SendToType.Team))
                {
                    if (p.ActiveArena != null && p.ActiveArena == player.ActiveArena && p.ActiveTeam == player.ActiveTeam)
                    {
                        Send(p, packet);
                    }
                }
            }
        }

        public static void SendTo(Arena arena, MemoryStream inStream, SendToType sendToType)
        {
            if (arena == null) return;

            Packet packet = new Packet(inStream);

            for (Byte i = 0; i < arena.ArenaPlayers.Count; i++)
            {
                ArenaPlayer arenaPlayer = arena.ArenaPlayers[i];

                if (arenaPlayer == null) continue;

                if (sendToType.HasFlag(SendToType.All))
                {
                    Send(arenaPlayer.WorldPlayer, packet);
                    continue;
                }

                if (sendToType.HasFlag(SendToType.Arena))
                {
                    if (arenaPlayer.WorldPlayer.ActiveArena == arena)
                    {
                        Send(arenaPlayer.WorldPlayer, packet);
                    }
                }
            }
        }

        public static void SendToArena(ArenaPlayer arenaPlayer, MemoryStream inStream, Boolean sendToSource)
        {
            if (arenaPlayer == null) return;

            Arena arena = arenaPlayer.WorldPlayer.ActiveArena;
            if (arena == null) return;

            bool senderIsHidden = arenaPlayer.WorldPlayer.Flags.HasFlag(PlayerFlag.Hidden);

            Packet packet = new Packet(inStream);

            for (Byte i = 0; i < arena.ArenaPlayers.Count; i++)
            {
                ArenaPlayer targetArenaPlayer = arena.ArenaPlayers[i];

                if (targetArenaPlayer == null || (targetArenaPlayer == arenaPlayer && !sendToSource)) continue;

                if (senderIsHidden && !targetArenaPlayer.WorldPlayer.IsAdmin)
                {
                    continue;
                }

                Send(targetArenaPlayer.WorldPlayer, packet);
            }
        }       
        private static void SendCallback(IAsyncResult ar)
        {
            SendCallbackSyncResult result = (SendCallbackSyncResult)ar.AsyncState;

            try
            {
                result.Player.TcpClient.Client.EndSend(ar);
            }
            catch (Exception)
            {
                result.Player.DisconnectReason = "SendCallback Error";
                result.Player.Disconnect = true;
            }
        }

        private static void SendCallbackUDP(IAsyncResult ar)
        {
            SendCallbackSyncResult result = (SendCallbackSyncResult)ar.AsyncState;

            try
            {
                _gameUDPListener.EndSend(ar);
            }
            catch (Exception)
            {
                result.Player.DisconnectReason = "SendCallback Error";
                result.Player.Disconnect = true;
            }
        }

        private struct SendCallbackSyncResult
        {
            public Player Player;
            
            public SendCallbackSyncResult(Player player)
            {
                Player = player;

            }
        }
        public static IPAddress LocalIPAddress()
        {
            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
            {
                return null;
            }

            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());

            return host
                .AddressList
                .FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);
        }

    }
}
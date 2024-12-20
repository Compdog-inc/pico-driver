﻿using MessagePack;
using NetCoreServer;
using System.Buffers;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using WatchdogDotNet;
using Timer = System.Timers.Timer;

namespace DriverStation
{
    public class DSClient : IDisposable
    {
        [StructLayout(LayoutKind.Sequential)]
        [Serializable]
        public struct SharedStatus
        {
            public bool connected;
            public string address;
            public bool hasServerTime;
            public long serverTimeOffset;

            public readonly void Serialize(ref MessagePackWriter writer)
            {
                writer.WriteMapHeader(4+1);
                WebServer.WritePacketType(ref writer, ClientPacketType.ClientStatus);
                
                writer.Write(nameof(connected));
                writer.Write(connected);

                writer.Write(nameof(address));
                writer.Write(address);

                writer.Write(nameof(hasServerTime));
                writer.Write(hasServerTime);

                writer.Write(nameof(serverTimeOffset));
                writer.Write(serverTimeOffset);
            }
        }

        private static readonly TimeSpan timeout = TimeSpan.FromSeconds(2);

        private static ulong GetCurrentTimeUs()
        {
            return (ulong)((DateTimeOffset.UtcNow.UtcTicks - DateTimeOffset.UnixEpoch.UtcTicks) / (TimeSpan.TicksPerMillisecond / 1000));
        }

        public enum PacketType : byte
        {
            ClockSync,
            RobotProperties
        };

        [StructLayout(LayoutKind.Sequential)]
        [Serializable]
        public struct ClockSyncRequestPacket
        {
            public UInt64 clientTime;

            public ClockSyncRequestPacket(ref MessagePackReader reader)
            {
                clientTime = reader.ReadUInt64();
            }

            public readonly void Serialize(ref MessagePackWriter writer)
            {
                writer.WriteUInt64(clientTime);
            }

            public override readonly string ToString() => $"{{clientTime:{clientTime}}}";
        };

        [StructLayout(LayoutKind.Sequential)]
        [Serializable]
        public struct ClockSyncPacket
        {
            public UInt64 clientTime;
            public UInt64 serverTime;

            public ClockSyncPacket(ref MessagePackReader reader)
            {
                clientTime = reader.ReadUInt64();
                serverTime = reader.ReadUInt64();
            }

            public readonly void Serialize(ref MessagePackWriter writer)
            {
                writer.WriteUInt64(clientTime);
                writer.WriteUInt64(serverTime);
            }

            public override readonly string ToString() => $"{{clientTime:{clientTime}, serverTime:{serverTime}}}";
        };

        [StructLayout(LayoutKind.Sequential)]
        [Serializable]
        public struct Units
        {
            public float value;

            public Units(ref MessagePackReader reader)
            {
                var bytes = reader.ReadBytes().GetValueOrDefault();
                MessagePackReader subreader = new(bytes);
                value = (float)subreader.ReadDouble();
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        [Serializable]
        public struct Vector3
        {
            public Units x;
            public Units y;
            public Units z;

            public Vector3(ref MessagePackReader reader)
            {
                var bytes = reader.ReadBytes().GetValueOrDefault();
                MessagePackReader subreader = new(bytes);
                x = new Units(ref subreader);
                y = new Units(ref subreader);
                z = new Units(ref subreader);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        [Serializable]
        public struct Vector4
        {
            public Units x;
            public Units y;
            public Units z;
            public Units w;

            public Vector4(ref MessagePackReader reader)
            {
                var bytes = reader.ReadBytes().GetValueOrDefault();
                MessagePackReader subreader = new(bytes);
                x = new Units(ref subreader);
                y = new Units(ref subreader);
                z = new Units(ref subreader);
                w = new Units(ref subreader);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        [Serializable]
        public struct Transform
        {
            public Vector3 position;
            public Vector4 rotation;

            public Transform(ref MessagePackReader reader)
            {
                var bytes = reader.ReadBytes().GetValueOrDefault();
                MessagePackReader subreader = new(bytes);
                position = new Vector3(ref subreader);
                rotation = new Vector4(ref subreader);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        [Serializable]
        public struct RobotProperties
        {
            public Vector3 frameDimensions;
            public Units wheelDiameter;
            public Transform[] wheels;
            public Transform[] whiskers;

            public RobotProperties(ref MessagePackReader reader)
            {
                frameDimensions = new Vector3(ref reader);
                wheelDiameter = new Units(ref reader);

                wheels = new Transform[reader.ReadArrayHeader()];
                for (int i = 0; i < wheels.Length; i++)
                {
                    wheels[i] = new Transform(ref reader);
                }

                whiskers = new Transform[reader.ReadArrayHeader()];
                for (int i = 0; i < whiskers.Length; i++)
                {
                    whiskers[i] = new Transform(ref reader);
                }
            }
        }

        private void InvokeStatusChanged()
        {
            StatusChanged?.Invoke();
        }

        private class Client(string url, DSClient ds) : WsClient(ParseUrl(url).address, ParseUrl(url).port)
        {
            private Timer? watchdog;

            public Guid latestPingId;
            public bool hasServerTime = false;
            public long serverTimeOffset;

            public long GetServerTimeUs()
            {
                if (!hasServerTime) return 0;

                return (long)GetCurrentTimeUs() + serverTimeOffset;
            }

            private static (string host, string path, string address, int port) ParseUrl(string url)
            {
                int host_start = url.IndexOf('/') + 2;
                int host_end = url.IndexOf('/', host_start);
                string host = url[host_start..host_end];
                string path = url[host_end..];

                int hostSep = host.IndexOf(':');
                string serverStr;
                int port;
                if (hostSep != -1)
                {
                    serverStr = host[..hostSep];
                    string portStr = host[(hostSep + 1)..];
                    port = int.Parse(portStr);
                }
                else
                {
                    serverStr = host;
                    port = 80;
                }

                return (host, path, serverStr, port);
            }

            public void DisconnectAndStop()
            {
                _stop = true;
                CloseAsync(1000);
                while (IsConnected)
                    Thread.Yield();
            }

            public bool ConnectAndDontStop()
            {
                _stop = false;
                Console.WriteLine($"[DSClient]: Client connecting...");
                return ConnectAsync();
            }

            public override void OnWsConnecting(HttpRequest request)
            {
                var parts = ParseUrl(url);
                request.SetBegin("GET", parts.path);
                request.SetHeader("Host", parts.host);
                request.SetHeader("Upgrade", "websocket");
                request.SetHeader("Connection", "Upgrade");
                request.SetHeader("Sec-WebSocket-Key", Convert.ToBase64String(WsNonce));
                request.SetHeader("Sec-WebSocket-Protocol", "driverstation.pico.rover");
                request.SetHeader("Sec-WebSocket-Version", "13");
                request.SetBody();
            }

            public override void OnWsConnected(HttpResponse response)
            {
                Console.WriteLine($"[DSClient]: Connection established.");
                watchdog?.Dispose();
                watchdog = new(timeout)
                {
                    AutoReset = false
                };
                watchdog.Elapsed += (s, e) => { Console.WriteLine("[DSClient]: Connection timed out."); Disconnect(); };

                SendClockSync();

                ds.InvokeStatusChanged();
            }

            bool wsDisconnected = false;
            public override void OnWsDisconnected()
            {
                wsDisconnected = true;
                Console.WriteLine($"[DSClient]: Connection lost.");
                watchdog?.Dispose();

                ds.InvokeStatusChanged();
            }

            public override void OnWsReceivedBinary(byte[] buffer, long offset, long size)
            {
                try
                {
                    if (size > 0)
                    {
                        PacketType packetType = (PacketType)buffer[0];
                        switch (packetType)
                        {
                            case PacketType.ClockSync:
                                {
                                    long expected = GetServerTimeUs();
                                    var reader = new MessagePackReader(new ReadOnlyMemory<byte>(buffer, (int)offset + 1, (int)size - 1));
                                    ClockSyncPacket packet = new(ref reader);
                                    ulong currentTime = GetCurrentTimeUs();
                                    long rtt = ((long)currentTime - (long)packet.clientTime) / 2;
                                    ulong serverTime = rtt > 0 ? packet.serverTime + (ulong)rtt : packet.serverTime - (ulong)-rtt;
                                    serverTimeOffset = (long)serverTime - (long)currentTime;

                                    long error = (long)serverTime - expected;
                                    if (Math.Abs(error) > 30000 && hasServerTime)
                                    {
                                        var errorTs = TimeSpan.FromMicroseconds(error);
                                        Console.WriteLine($"[DSClient]: Abnormally large clock error ({errorTs.TotalMilliseconds}ms)! This could lead to packet loss.");
                                    }

                                    if (!hasServerTime)
                                    {
                                        Console.WriteLine($"[DSClient]: Retrieved server time: {serverTime}");
                                    }

                                    hasServerTime = true;

                                    ds.InvokeStatusChanged();
                                    break;
                                }
                            case PacketType.RobotProperties:
                                {
                                    var reader = new MessagePackReader(new ReadOnlyMemory<byte>(buffer, (int)offset + 1, (int)size - 1));
                                    RobotProperties packet = new(ref reader);
                                    break;
                                }
                            default:
                                {
                                    break;
                                }
                        }
                    }
                } catch { }
            }

            public void SendClockSync()
            {
                ClockSyncRequestPacket packet = new()
                {
                    clientTime = GetCurrentTimeUs()
                };

                PacketType packetType = PacketType.ClockSync;
                byte packetTypeByte = (byte)packetType;

                var buffer = new ArrayBufferWriter<byte>();
                buffer.Write(new ReadOnlySpan<byte>(ref packetTypeByte));
                MessagePackWriter writer = new(buffer);
                packet.Serialize(ref writer);

                writer.Flush();
                SendBinaryAsync(buffer.WrittenSpan);
                buffer.Clear();
            }

            public void RequestRobotProperties()
            {
                SendBinaryAsync(new byte[] { (byte)PacketType.RobotProperties });
            }

            public override void OnWsReceivedText(byte[] buffer, long offset, long size)
            {
                Console.WriteLine($"[DSClient]: Server responded with '{Encoding.UTF8.GetString(buffer, (int)offset, (int)size)}'");
            }

            public override void OnWsPing(byte[] buffer, long offset, long size)
            {
                base.OnWsPing(buffer, offset, size);
            }

            public override void OnWsPong(byte[] buffer, long offset, long size)
            {
                base.OnWsPong(buffer, offset, size);
                if (latestPingId.Equals(new Guid(new ReadOnlySpan<byte>(buffer, (int)offset, (int)size))))
                {
                    try
                    {
                        watchdog?.Restart();
                    }
                    catch { }
                }
            }

            protected override void OnDisconnected()
            {
                base.OnDisconnected();

                if(!wsDisconnected)
                    Console.WriteLine($"[DSClient]: Connection lost.");
                wsDisconnected = false;

                Thread.Sleep(100);

                if (!_stop)
                {
                    Console.WriteLine($"[DSClient]: Client connecting...");
                    ConnectAsync();
                }
            }

            protected override void OnError(SocketError error)
            {
                Console.WriteLine($"[DSClient]: error {error}");
            }

            private bool _stop;
        }

        private Client client;

        public bool IsConnected => client.IsConnected;

        public long ServerTimeUs => client.GetServerTimeUs();
        public TimeSpan ServerTime => TimeSpan.FromMicroseconds(ServerTimeUs);

        public SharedStatus Status => new()
        {
            address = client.Address,
            connected = IsConnected,
            hasServerTime = client.hasServerTime,
            serverTimeOffset = client.serverTimeOffset
        };

        public event Action? StatusChanged;

        public DSClient(string address)
        {
            client = new Client("ws://" + address + ":5002/", this);
        }

        public bool Connect()
        {
            bool res = client.ConnectAndDontStop();
            _ = Task.Run(() =>
            {
                while (true)
                {
                    client.latestPingId = Guid.NewGuid();
                    client.SendPingAsync(client.latestPingId.ToByteArray());
                    Task.Delay(1000).Wait();
                }
            });
            _ = Task.Run(() =>
            {
                while (true)
                {
                    Task.Delay(5000).Wait();

                    if (client.IsConnected)
                    {
                        client.SendClockSync();
                        client.RequestRobotProperties();
                    }
                }
            });
            return res;
        }

        public void Disconnect()
        {
            client.DisconnectAndStop();
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Disconnect();
            client.Dispose();
        }
    }
}

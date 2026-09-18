using System;
using System.Text;

namespace PBJ.Networking
{
    // ONE shared wire contract for PBJ LAN room discovery (LAN ONLY - no
    // relay, no UGS, no internet). Both LanRoomBroadcaster (host) and
    // LanRoomDiscovery (client) encode/decode exactly this layout:
    //
    //   [0..3]   magic   "PBJD" (ASCII)            - PBJ Discovery signature
    //   [4]      protocol version (= 1)            - future format changes bump this
    //   [5..8]   room code (4 ASCII bytes from RoomCode.Alphabet)
    //   [9..10]  NGO game port (ushort, little-endian, must be != 0)
    //   [11..]   optional host name (UTF-8, at most MaxHostNameBytes bytes)
    //
    // Deterministic byte layout (no serializer, no BitConverter endianness
    // assumptions). TryDecode rejects anything that does not match exactly:
    // wrong magic, wrong version, too short, non-Alphabet code characters,
    // port 0, or invalid UTF-8 in the name field. One datagram = one packet;
    // the UDP datagram boundary is the only framing.
    public readonly struct LanDiscoveryPacket
    {
        // Bump ONLY on a wire-format change; receivers reject other versions.
        public const int ProtocolVersion = 1;

        // Shared UDP port both sides agree on by default (matches the PBJ
        // 7777 game-port style, distinct from the NGO game port).
        public const int DefaultDiscoveryPort = 47777;

        internal const int HeaderLength = 11; // magic(4) + version(1) + code(4) + port(2)
        internal const int MaxHostNameBytes = 24;

        private static readonly byte[] Magic = { (byte)'P', (byte)'B', (byte)'J', (byte)'D' };

        // Strict UTF-8: throwsOnInvalidBytes so malformed name bytes are
        // rejected instead of silently replaced.
        private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);

        public string RoomCode { get; }
        public ushort GamePort { get; }
        public string HostName { get; } // "" when the host did not advertise one

        internal LanDiscoveryPacket(string roomCode, ushort gamePort, string hostName)
        {
            RoomCode = roomCode;
            GamePort = gamePort;
            HostName = hostName ?? string.Empty;
        }

        // Encodes the packet. Returns false (packet = null) on invalid input:
        // room code must pass RoomCode.IsValid; gamePort must be != 0.
        public static bool TryEncode(string roomCode, ushort gamePort, string hostName, out byte[] packet)
        {
            packet = null;
            if (!PBJ.Networking.RoomCode.IsValid(roomCode) || gamePort == 0)
            {
                return false;
            }

            byte[] nameBytes;
            try
            {
                nameBytes = EncodeHostName(hostName);
            }
            catch (Exception)
            {
                return false; // never expected for a valid .NET string; fail closed
            }

            packet = new byte[HeaderLength + nameBytes.Length];
            Magic.CopyTo(packet, 0);
            packet[4] = (byte)ProtocolVersion;
            for (int i = 0; i < PBJ.Networking.RoomCode.Length; i++)
            {
                packet[5 + i] = (byte)roomCode[i]; // validated ASCII by IsValid
            }
            packet[9] = (byte)(gamePort & 0xFF);
            packet[10] = (byte)((gamePort >> 8) & 0xFF);
            nameBytes.CopyTo(packet, HeaderLength);
            return true;
        }

        // Decodes + validates one datagram. Returns false for anything
        // malformed (see layout comment above); 'packet' is default then.
        public static bool TryDecode(byte[] data, int length, out LanDiscoveryPacket packet)
        {
            packet = default;
            if (data == null || length < HeaderLength || data.Length < length)
            {
                return false;
            }

            // Magic must match byte-for-byte.
            for (int i = 0; i < Magic.Length; i++)
            {
                if (data[i] != Magic[i])
                {
                    return false;
                }
            }

            // Version must match exactly (no backward compatibility yet).
            if (data[4] != ProtocolVersion)
            {
                return false;
            }

            // Room code: 4 bytes, all canonical Alphabet characters.
            var codeChars = new char[PBJ.Networking.RoomCode.Length];
            for (int i = 0; i < PBJ.Networking.RoomCode.Length; i++)
            {
                char c = (char)data[5 + i];
                if (PBJ.Networking.RoomCode.Alphabet.IndexOf(c) < 0)
                {
                    return false;
                }
                codeChars[i] = c;
            }

            // Game port: little-endian; 0 is treated as malformed.
            ushort gamePort = (ushort)(data[9] | (data[10] << 8));
            if (gamePort == 0)
            {
                return false;
            }

            // Host name: remaining bytes, optional, must be valid UTF-8.
            string hostName = string.Empty;
            int nameLength = length - HeaderLength;
            if (nameLength > 0)
            {
                try
                {
                    hostName = StrictUtf8.GetString(data, HeaderLength, nameLength);
                }
                catch (DecoderFallbackException)
                {
                    return false; // malformed UTF-8 -> reject the whole packet
                }
            }

            packet = new LanDiscoveryPacket(new string(codeChars), gamePort, hostName);
            return true;
        }

        // Convenience overload for exact-length buffers (e.g. UdpClient.Receive).
        public static bool TryDecode(byte[] data, out LanDiscoveryPacket packet)
        {
            packet = default;
            return data != null && TryDecode(data, data.Length, out packet);
        }

        // Host name is optional: trimmed, UTF-8 encoded, hard-capped at
        // MaxHostNameBytes without cutting a UTF-8 sequence in half.
        private static byte[] EncodeHostName(string hostName)
        {
            if (string.IsNullOrEmpty(hostName))
            {
                return Array.Empty<byte>();
            }

            string trimmed = hostName.Trim();
            if (trimmed.Length == 0)
            {
                return Array.Empty<byte>();
            }

            byte[] bytes = StrictUtf8.GetBytes(trimmed);
            if (bytes.Length <= MaxHostNameBytes)
            {
                return bytes;
            }

            // Truncate, then back off to a clean UTF-8 boundary so the
            // encoded bytes always decode without replacement characters.
            byte[] truncated = new byte[MaxHostNameBytes];
            Array.Copy(bytes, truncated, MaxHostNameBytes);
            int end = MaxHostNameBytes;
            while (end > 0 && (truncated[end - 1] & 0xC0) == 0x80)
            {
                end--; // drop continuation bytes of the cut sequence
            }
            if (end > 0 && truncated[end - 1] >= 0x80)
            {
                end--; // drop the lead byte of the cut multi-byte sequence
            }
            byte[] result = new byte[end];
            Array.Copy(truncated, result, end);
            return result;
        }
    }
}


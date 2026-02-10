using System;
using System.IO;

namespace GameClient.Network
{
    public class NetPacket
    {
        public const int HeaderSize = 16;

        public int ProtoId { get; set; }
        public int Version { get; set; }
        public int DataSize { get; set; }
        public int IsCompressed { get; set; }
        public byte[] Data { get; set; }

        public NetPacket()
        {
        }

        public NetPacket(int protoId, byte[] data)
        {
            ProtoId = protoId;
            Version = 1;
            Data = data;
            DataSize = data?.Length ?? 0;
            IsCompressed = 0;
        }

        public byte[] Marshal()
        {
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);

            writer.Write(ProtoId);
            writer.Write(Version);
            writer.Write(DataSize);
            writer.Write(IsCompressed);

            if (Data != null && Data.Length > 0)
            {
                writer.Write(Data);
            }

            return ms.ToArray();
        }

        public static NetPacket UnmarshalHeader(byte[] headerData)
        {
            if (headerData == null || headerData.Length < HeaderSize)
            {
                return null;
            }

            using var ms = new MemoryStream(headerData);
            using var reader = new BinaryReader(ms);

            return new NetPacket
            {
                ProtoId = reader.ReadInt32(),
                Version = reader.ReadInt32(),
                DataSize = reader.ReadInt32(),
                IsCompressed = reader.ReadInt32()
            };
        }

        public static NetPacket Unmarshal(byte[] data)
        {
            if (data == null || data.Length < HeaderSize)
            {
                return null;
            }

            using var ms = new MemoryStream(data);
            using var reader = new BinaryReader(ms);

            var packet = new NetPacket
            {
                ProtoId = reader.ReadInt32(),
                Version = reader.ReadInt32(),
                DataSize = reader.ReadInt32(),
                IsCompressed = reader.ReadInt32()
            };

            if (packet.DataSize > 0)
            {
                packet.Data = reader.ReadBytes(packet.DataSize);
            }

            return packet;
        }
    }
}

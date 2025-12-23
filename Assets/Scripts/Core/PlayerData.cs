using Unity.Netcode;
using Unity.Collections;
using System;

// Ağ üzerinden gönderilebilir oyuncu verisi
public struct PlayerData : INetworkSerializable, IEquatable<PlayerData>
{
    public ulong ClientId;
    public FixedString32Bytes PlayerName; // String yerine FixedString kullanmalıyız
    public int Score;
    public bool IsInfected; // Virüslü mü?

    // Veriyi ağda nasıl paketleyip açacağımızı söyleriz
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref ClientId);
        serializer.SerializeValue(ref PlayerName);
        serializer.SerializeValue(ref Score);
        serializer.SerializeValue(ref IsInfected);
    }

    // Değişiklik kontrolü için
    public bool Equals(PlayerData other)
    {
        return ClientId == other.ClientId &&
               PlayerName == other.PlayerName &&
               Score == other.Score &&
               IsInfected == other.IsInfected;
    }
}
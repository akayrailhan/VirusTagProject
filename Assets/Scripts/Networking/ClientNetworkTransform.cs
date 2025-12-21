using Unity.Netcode.Components;
using UnityEngine;

// Standart NetworkTransform'dan miras alıyoruz
[DisallowMultipleComponent]
public class ClientNetworkTransform : NetworkTransform
{
    // Yetki kontrolünü eziyoruz: Sunucu yetkili DEĞİL, Client yetkili olsun.
    protected override bool OnIsServerAuthoritative()
    {
        return false;
    }
}
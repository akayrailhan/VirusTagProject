using Unity.Netcode.Components;
using UnityEngine;

// Standart NetworkTransform'dan miras alıyoruz
[DisallowMultipleComponent]
public class ClientNetworkTransform : NetworkTransform
{
    // Yetki kontrolünü eziyoruz: Sunucu yetkili DEĞİL, Client yetkili olsun.
    // Yetki kontrolü: SERVER yetkili olsun (duvar çarpışması server fiziğiyle garanti)
protected override bool OnIsServerAuthoritative()
{
    return true;
}

}
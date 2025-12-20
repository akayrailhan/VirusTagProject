using Unity.Services.Core;
using Unity.Services.Authentication;
using System.Threading.Tasks;
using UnityEngine;

public static class AuthenticationWrapper
{
    public static async Task<bool> LoginAnonymously()
    {
        try
        {
            // Unity Servislerini Başlat
            if (UnityServices.State == ServicesInitializationState.Uninitialized)
            {
                await UnityServices.InitializeAsync();
            }

            // Zaten giriş yapılmışsa tekrar yapma
            if (AuthenticationService.Instance.IsSignedIn)
            {
                return true;
            }

            // Anonim Giriş Yap
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log($"[Auth] Signed in as: {AuthenticationService.Instance.PlayerId}");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Auth] Login Failed: {e.Message}");
            return false;
        }
    }
}
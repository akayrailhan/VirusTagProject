using UnityEngine;
using UnityEngine.InputSystem;
using System;

[CreateAssetMenu(fileName = "InputReader", menuName = "Game/Input Reader")]
public class InputReader : ScriptableObject, Controls.IPlayerActions
{
    public event Action<Vector2> MoveEvent;
    public event Action FireEvent;
    public Vector2 AimPosition { get; private set; }

    private Controls _controls;

    private void OnEnable()
    {
        if (_controls == null)
        {
            _controls = new Controls();
            _controls.Player.SetCallbacks(this);
        }
        _controls.Player.Enable();
    }

    // BU METOD ÇOK ÖNEMLİ! EKSİK OLURSA HATA VERİR.
    private void OnDisable()
    {
        if (_controls != null)
        {
            _controls.Player.Disable();
        }
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        MoveEvent?.Invoke(context.ReadValue<Vector2>());
    }

    public void OnAim(InputAction.CallbackContext context)
    {
        AimPosition = context.ReadValue<Vector2>();
    }

    public void OnFire(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            FireEvent?.Invoke();
        }
    }
}
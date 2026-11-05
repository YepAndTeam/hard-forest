using UnityEngine;
using UnityEngine.EventSystems;

public class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    public enum Action { Left, Right, Jump, Attack }
    public Action action;

    public void OnPointerDown(PointerEventData e)
    {
        if (action == Action.Left)  MobileInput.Horizontal = -1f;
        if (action == Action.Right) MobileInput.Horizontal = 1f;
        if (action == Action.Jump)  MobileInput.Jump = true;
        if (action == Action.Attack) MobileInput.Attack = true;
    }

    public void OnPointerUp(PointerEventData e)   { Release(); }
    public void OnPointerExit(PointerEventData e) { Release(); }

    void Release()
    {
        if (action == Action.Left || action == Action.Right)
            MobileInput.Horizontal = 0f;
    }

    void OnDisable() { MobileInput.Horizontal = 0f; }
}
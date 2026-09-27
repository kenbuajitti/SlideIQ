using System;
using UnityEngine;
using UnityEngine.EventSystems;
public sealed class MineRightClick : MonoBehaviour, IPointerClickHandler
{
    public Action action;
    public void OnPointerClick(PointerEventData e) { if(e.button==PointerEventData.InputButton.Right)action?.Invoke(); }
}

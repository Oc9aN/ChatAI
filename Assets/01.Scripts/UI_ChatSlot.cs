using TMPro;
using UnityEngine;

public class UI_ChatSlot : MonoBehaviour
{
    public TextMeshProUGUI Text;

    public void Refresh(string text)
    {
        Text.text = text;
    }
}

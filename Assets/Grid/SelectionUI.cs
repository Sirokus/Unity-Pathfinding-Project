using System.Collections;
using System.Collections.Generic;
using Mine;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SelectionUI : MonoBehaviour
{
    public Button[] buttons;

    private void Awake() {
        buttons = GetComponentsInChildren<Button>();

        for(int i = 0; i < buttons.Length; i++)
        {
            int index = i;
            buttons[i].onClick.AddListener(() => SetGridFindType(index));
            string btnName = ((FindType)index).ToString();
            if((FindType)index == FindType.AStar)
                btnName = "Astar(有权)";
            else if((FindType)index == FindType.AStar2)
                btnName = "Astar(无权)";
            buttons[i].GetComponentInChildren<TextMeshProUGUI>()?.SetText(btnName);
        }
    }

    void SetGridFindType(int index)
    {
        Mine.Grid.ins.findType = (FindType)index;
        Mine.Grid.ins.StartFindWay();
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TabNavigation : MonoBehaviour
{
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if(EventSystem.current.currentSelectedGameObject != null)
            {
                Selectable selectable = EventSystem.current.currentSelectedGameObject.GetComponent<Selectable>();
                selectable.FindSelectableOnRight()?.Select();
            }
        }
    }
}

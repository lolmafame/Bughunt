using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class AllPlayersValidation : MonoBehaviour
{
    public List<Toggle> toggles;
    public GameObject warningObject; 

    public GameObject hideObject;
    public GameObject openObject;

    public void OnProceedClicked()
    {
        if (!AllTogglesOn())
        {
            warningObject.SetActive(true);
            return;
        }

        hideObject.SetActive(false);
        openObject.SetActive(true);
    }

    bool AllTogglesOn()
    {
        foreach (Toggle t in toggles)
        {
            if (!t.isOn)
                return false;
        }
        return true;
    }
}

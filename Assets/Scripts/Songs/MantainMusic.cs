using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MantainMusic : MonoBehaviour
{
    public static MantainMusic instance;

    private void Awake()
    {
        DontDestroyOnLoad(this.gameObject);

        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(this.gameObject);
        }
    }
}

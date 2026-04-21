using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ConstantMusic : MonoBehaviour
{
    [SerializeField] private AudioSource fundoMusical;

    public void VolumeMusical(float value)
    {
        fundoMusical.volume = value;
    }
}

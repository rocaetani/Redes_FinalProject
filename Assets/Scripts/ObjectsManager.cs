using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectsManager : MonoBehaviour
{
    public static GameObject OverviewCamera = null;
    [SerializeField]
    private GameObject _overviewCamera = null;

    private void Awake()
    {
        if(OverviewCamera == null)
        {
            OverviewCamera = _overviewCamera;
        }
    }

    private void OnDestroy()
    {
        if(OverviewCamera == _overviewCamera)
        {
            OverviewCamera = null;
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using MLAPI;
using MLAPI.Messaging;
using MLAPI.NetworkVariable;
using UnityEngine;

public class PlayerColor : NetworkBehaviour
{
    private MeshRenderer _meshRenderer;
    
    public NetworkVariable<int> BombermanColor = new NetworkVariable<int>(new NetworkVariableSettings {WritePermission = NetworkVariablePermission.Everyone}, 0);

    private void Start()
    {
        
        
        _meshRenderer = GetComponent<MeshRenderer>();
        if (IsOwner)
        {
            ColorPick.Instance.SetPlayerColor(this);
        }

        BombermanColor.OnValueChanged += ApplyColor;

    }
    

    // 0 - Body
    // 1 - Head
    // 4 - Hand
    public void ApplyColor(int oldBombermanColor, int newBombermanColor)
    {

        BombermanColor bombermanColor = (BombermanColor) newBombermanColor;
        Material[] materials = _meshRenderer.materials;
        
        materials[0] = PlayerManager.PossibleBombermanColors[bombermanColor].Body;
        materials[1] = PlayerManager.PossibleBombermanColors[bombermanColor].Head;
        materials[4] = PlayerManager.PossibleBombermanColors[bombermanColor].Hand;

        _meshRenderer.materials = materials;
        
    }

    public void SetBomberColor(BombermanColor bombermanColor)
    {
        BombermanColor.Value = (int)bombermanColor;
    }



}

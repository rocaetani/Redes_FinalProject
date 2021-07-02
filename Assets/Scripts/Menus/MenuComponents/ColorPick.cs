using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;

public class ColorPick : MonoBehaviour
{
    public PlayerColor _playerColor;

    public static ColorPick Instance;

    public void Awake()
    {
        Instance = this;
    }

    public void SetPlayerColor(PlayerColor playerColor)
    {
        _playerColor = playerColor;
        print("Vinculei PlayerColor");
        
    }

    public void ApplyColor(int index)
    {
        BombermanColor bombermanColor = (BombermanColor) index;
        _playerColor.SetBomberColor(bombermanColor);
    }

    
    

}

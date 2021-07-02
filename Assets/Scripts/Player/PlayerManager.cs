using System.Collections;
using System.Collections.Generic;
using MLAPI;
using MLAPI.Messaging;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public ColorPick SettableColorPick;

    public static ColorPick ColorPick;

    public bool enablePlayerBehaviour = true;
    
    public List<BombermanMaterial> BombermanColors;

    public static Dictionary<BombermanColor, BombermanMaterial> PossibleBombermanColors;

    private void Awake()
    {
        //ColorPick = SettableColorPick;
        PossibleBombermanColors = new Dictionary<BombermanColor, BombermanMaterial>();
        foreach (var bombermanColor in BombermanColors)
        {
            PossibleBombermanColors.Add(bombermanColor.BombermanColor, bombermanColor);
        }
    }
    
    
    private void Start()
    {
        PlayerController.playerBehaviourEnabled = enablePlayerBehaviour;
    }





}

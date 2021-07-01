using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MLAPI.Messaging;

public static class Extensions
{
    public static IEnumerable<GameObject> FindChildrenWithTag(this GameObject root, string tag, bool includeInactive = true)
    {
        return root.GetComponentsInChildren<Transform>(includeInactive).Where(child => child.tag == tag).Select(child => child.gameObject);
    }

    public static void toggleMenu(this MonoBehaviour menuScript) => MenuManager.toggleMenu(menuScript.gameObject);
    public static void toggleMenuDelayed(this MonoBehaviour menuScript) => MenuManager.toggleMenuDelayed(menuScript.gameObject);

    // Prepare ClientRPC's params, so that we only
    // reply with an RPC to the requester Client
    public static ClientRpcParams returnToSender(this ServerRpcParams serverRpcSenderParams)
    {
        return new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[]{ serverRpcSenderParams.Receive.SenderClientId }
            }
        };
    }
}

using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace MMM.Pets {
    public class PlayerHand : UdonSharpBehaviour
    {
        public VRC_Pickup.PickupHand targetHand;
        public VRC_Pickup pickup;

        public void Vibrate()
        {
            Debug.Log($"Vibrate called on {name}");
            if (Networking.LocalPlayer.IsUserInVR())
            {
                pickup.GenerateHapticEvent(0.1f, 0.25f, 0.5f);
            }
        }
    }

}
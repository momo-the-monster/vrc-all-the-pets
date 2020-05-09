using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;

namespace MMM.Pets
{
    public class TheEgg : UdonSharpBehaviour
    {
        public UnityEngine.UI.Text textField;
        [SerializeField] private Renderer _renderer;
        [SerializeField] private string _allowedRigidbodyName = "PlayerHand";
        private bool _readyForTrigger = false;

        private int _health;
        [SerializeField] private int _startingHealth = 20;

        private void Start()
        {
            Reset();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other == null) return;

            if (!_readyForTrigger) return;;
            if (other.gameObject.name.CompareTo(_allowedRigidbodyName) == 0)
            {
                _readyForTrigger = false;
                Say("The Egg Takes Damage");
                TakeDamage();

                // Try to send "Vibrate" event to attached UdonBehaviour
                var ub = (UdonBehaviour)other.GetComponent(typeof(IUdonEventReceiver));
                if(ub != null)
                {
                    ub.SendCustomEvent("Vibrate");
                }
            } else
            {
                Debug.Log($"Collided with {other.gameObject.name} for no good reason");
            }
            
        }

        private void OnTriggerExit(Collider other)
        {
            if (other == null) return;

            _readyForTrigger = true;
        }

        private void TakeDamage()
        {
            _health -= 1;
            
            if(_health <= 0)
            {
                EndOfLife();
            } 
            else
            {
                string noun = _health > 1 ? "taps" : "tap";
                Say($"Only {_health} more {noun}!");
            }
        }

        private void EndOfLife()
        {
            Say("Thank you for freeing me.");
            gameObject.SetActive(false);
        }

        private void Reset()
        {
            _health = _startingHealth;
            _readyForTrigger = true;
            Say("Tap Egg");
        }

        private void Say(string message)
        {
            if(textField != null)
            {
                textField.text = message;
            }

            Debug.Log($"The Egg Says: {message} health: {_health}");
        }
    }

}
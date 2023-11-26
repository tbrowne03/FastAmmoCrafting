using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

using RDR2.Native;

namespace RDR2
{
    public class FastSplitPointCrafting : Script
    {
        // Variables
        private bool mGiveAmmo = false;
        private int mSpecifiedAmmoIncrement;
        private int mActualAmmoIncrement;
        private Ped mPlayerPed;
        private Keys mKeyBinding;

        private eAmmoType mAmmoAddType;
        private eAmmoType mAmmoRemoveType;        

        private List<eAmmoType> validAmmoTypes = new List<eAmmoType>();
        private Dictionary<eAmmoType, eAmmoType> splitAmmoLookup = new Dictionary<eAmmoType, eAmmoType>();

        // Constructor
        public FastSplitPointCrafting()
        {
            // Initialize Variables
            mPlayerPed = Game.Player.Character;

            // Initialize Listeners
            KeyDown += OnKeyDown;
            Tick += OnTick;

            // Initialize Valid Ammo Types
            validAmmoTypes.Add(eAmmoType.Revolver);
            validAmmoTypes.Add(eAmmoType.Pistol);
            validAmmoTypes.Add(eAmmoType.Repeater);
            validAmmoTypes.Add(eAmmoType.Rifle);

            // Populate a dictionary to relate ammo type/ split point ammo type
            splitAmmoLookup.Add(eAmmoType.Revolver, eAmmoType.RevolverSplitPoint);
            splitAmmoLookup.Add(eAmmoType.Pistol,   eAmmoType.PistolSplitPoint);
            splitAmmoLookup.Add(eAmmoType.Repeater, eAmmoType.RepeaterSplitPoint);
            splitAmmoLookup.Add(eAmmoType.Rifle,    eAmmoType.RifleSplitPoint);

            // Read the config file
            bool keyConfigSuccess = false;
            bool ammoIncrementSuccess = false;

            string filePath = "scripts/FastSplitPointCrafting.ini";
            string[] lines = new string[] {};
            try
            {
                lines = File.ReadAllLines(filePath);
            } catch (Exception)
            {
                // Do nothing
            }

            foreach (string line in lines)
            {
                string[] substring = line.Split('=');

                // Set the KeyBinding
                if (substring.Length == 2 &&
                    substring[0] == "fastAmmoCraftingKey")
                {
                    keyConfigSuccess = Enum.TryParse(substring[1], out mKeyBinding);
                    continue;
                }
                
                // Set the Increment Amount
                if (substring.Length == 2 &&
                    substring[0] == "fastAmmoCraftingAmount")
                {
                    ammoIncrementSuccess = int.TryParse(substring[1], out mSpecifiedAmmoIncrement);
                    continue;
                }
            }

            // Set default values if the config file could not be loaded successfully
            if (!keyConfigSuccess)
            {
                mKeyBinding = Keys.K;
            }

            if (!ammoIncrementSuccess ||
                mSpecifiedAmmoIncrement < 0)
            {
                mSpecifiedAmmoIncrement = 25;
            }
        }

        // Listener for Key Press Events
        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == mKeyBinding)
            {
                // If player is crouching
                if (PED.GET_PED_CROUCH_MOVEMENT(mPlayerPed))
                {
                    // Get the default ammo type for the currently held weapon
                    eAmmoType defaultAmmoType = mPlayerPed.Weapons.Current.DefaultAmmoType;

                    // If the weapon uses a valid ammo type
                    if (validAmmoTypes.Contains(defaultAmmoType))
                    {
                        mAmmoAddType = splitAmmoLookup[defaultAmmoType];
                        mAmmoRemoveType = defaultAmmoType;

                        // Check how much ammo should be crafted. Normally, this is set by mSpecifiedAmmoIncrement. However, sometimes this amount would craft too much ammo, and we should craft less.
                        // Note: The max split point ammo a gun can hold is the regular ammo max / 2
                        if ((GetAmmoCount(mAmmoAddType) + mSpecifiedAmmoIncrement) > (mPlayerPed.Weapons.Current.MaxAmmo / 2))
                        {
                            mActualAmmoIncrement = (mPlayerPed.Weapons.Current.MaxAmmo / 2) - GetAmmoCount(mAmmoAddType);
                        } else
                        {
                            mActualAmmoIncrement = mSpecifiedAmmoIncrement;
                        }

                        // If you have enough ammo 
                        if (GetAmmoCount(mAmmoRemoveType) >= mActualAmmoIncrement)
                        {
                            mGiveAmmo = true;
                        }
                        else
                        {
                            RDR2.UI.Screen.DisplaySubtitle($"Could not craft split point ammo. Insufficient materials.");
                        }
                    }
                }
            }
        }

        // Listener for tick events
        private void OnTick(object sender, EventArgs e)
        {
            if (mGiveAmmo)
            {
                // Add split point ammo to ped
                WEAPON._ADD_AMMO_TO_PED_BY_TYPE(mPlayerPed, (uint)mAmmoAddType, mActualAmmoIncrement, (uint)eAddItemReason.Default);
                
                // Remove regular ammo from ped
                WEAPON._REMOVE_AMMO_FROM_PED_BY_TYPE(mPlayerPed, (uint)mAmmoRemoveType, mActualAmmoIncrement, (uint)eRemoveItemReason.Used);

                RDR2.UI.Screen.DisplaySubtitle($"Added {mActualAmmoIncrement} split point ammo to inventory.");
                mGiveAmmo = false;
            }
        }

        // Gets the Players total ammo of a given type
        private int GetAmmoCount(eAmmoType pAmmoType)
        {
            int ammoCount = WEAPON.GET_PED_AMMO_BY_TYPE(mPlayerPed, (uint)pAmmoType);
            
            return ammoCount;
        }
    }
}

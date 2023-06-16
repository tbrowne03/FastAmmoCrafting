using System;
using System.Collections.Generic;
using System.Windows.Forms;
using RDR2;
using RDR2.UI;
using RDR2.Native;
using RDR2.Math;

namespace RDR2
{
    public class FastAmmoCrafting : Script
    {

        //Member Variables
        private bool mGiveAmmo = false;
        private int mAmmoIncrement;
        private Ped mPlayerPed;
        private string mAmmoAddType;
        private string mAmmoRemoveType;

        private Dictionary<string, string> weaponTypes = new Dictionary<string, string>();
        private Dictionary<string, string> ammoTypes = new Dictionary<string, string>();

        public FastAmmoCrafting()
        {
            //Initialize Variables
            mAmmoIncrement = 25;
            mPlayerPed = Game.Player.Character;

            //Initialize Listeners
            KeyDown += OnKeyDown;
            Tick += OnTick;

            //Pistol
            weaponTypes.Add("WEAPON_PISTOL_VOLCANIC", "AMMO_PISTOL");
            weaponTypes.Add("WEAPON_PISTOL_M1899", "AMMO_PISTOL");
            weaponTypes.Add("WEAPON_PISTOL_MAUSER", "AMMO_PISTOL");

            //Revolver
            weaponTypes.Add("WEAPON_REVOLVER_CATTLEMAN",   "AMMO_REVOLVER");
            weaponTypes.Add("WEAPON_REVOLVER_LEMAT",       "AMMO_REVOLVER");
            weaponTypes.Add("WEAPON_REVOLVER_SCHOFIELD",   "AMMO_REVOLVER");

            //Repeater
            weaponTypes.Add("WEAPON_REPEATER_CARBINE",     "AMMO_REPEATER");
            weaponTypes.Add("WEAPON_REPEATER_WINCHESTER",  "AMMO_REPEATER");


            //Rifle
            ammoTypes.Add("AMMO_REVOLVER", "AMMO_REVOLVER_SPLIT_POINT" );
            ammoTypes.Add("AMMO_PISTOL",   "AMMO_PISTOL_SPLIT_POINT"   );
            ammoTypes.Add("AMMO_REPEATER", "AMMO_REPEATER_SPLIT_POINT" );
        }

        /// <summary>
        /// Listener for Key Press Events
        /// </summary>
        /// <param name="sender">The event sender</param>
        /// <param name="e">The KeyEventArgs</param>
        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.P)
            {
                //If player is crouching
                //GET_PED_CROUCH_MOVEMENT: Hash._0xD5FE956C70FF370B
                if (Function.Call<bool>(Hash._0xD5FE956C70FF370B, mPlayerPed))
                {
                    //If weapon exists in the pre-defined list
                    if (GetCurrentWeapon())
                    {
                        //If you have enough ammo
                        if (GetAmmoCount(mAmmoRemoveType) >= mAmmoIncrement)
                        {
                            mGiveAmmo = true;
                        }
                        else
                        {
                            RDR2.UI.Screen.ShowSubtitle($"Could not craft split point ammo. Insufficient materials.");
                        }
                    }
                }
                else
                {
                    RDR2.UI.Screen.ShowSubtitle($"Crouch to begin crafting split point ammo.");
                }

            }
        }

        /// <summary>
        /// Listener for tick events
        /// </summary>
        /// <param name="sender">The object sender</param>
        /// <param name="e">The EventArgs</param>
        private void OnTick(object sender, EventArgs e)
        {
            if (mGiveAmmo)
            {
                // _GIVE_AMMO_TO_PED: Hash._0x106A811C6D3035F3
                Function.Call(Hash._0x106A811C6D3035F3, mPlayerPed, ConvertEnumToInt(mAmmoAddType), mAmmoIncrement, 0x2CD419DC);

                //_REMOVE_AMMO_FROM_PED_BY_TYPE: Hash._0xB6CFEC32E3742779
                Function.Call(Hash._0xB6CFEC32E3742779, mPlayerPed, ConvertEnumToInt(mAmmoRemoveType), mAmmoIncrement, 0x2188E0A3);

                RDR2.UI.Screen.ShowSubtitle($"Added {mAmmoIncrement} split point ammo to inventory.");

                mGiveAmmo = false;
            }
        }

        /// <summary>
        /// Gets the current player weapon
        /// </summary>
        /// <returns></returns>
        private bool GetCurrentWeapon()
        {
            //_GET_SELECTED_PED_WEAPON: Hash._0x8425C5F057012DAB
            int currentPlayerWeapon = (int)Function.Call<Hash>(Hash._0x8425C5F057012DAB, mPlayerPed);

            foreach (KeyValuePair<string, string> weapon in weaponTypes)
            {
                if (currentPlayerWeapon == ConvertEnumToInt(weapon.Key))
                {
                    mAmmoRemoveType = weapon.Value;
                    mAmmoAddType = ammoTypes[mAmmoRemoveType];
                    return true;
                }
            }

            return false;
        }

        private int ConvertEnumToInt(string convertEnum)
        {
            int outputInt = (int)Function.Call<Hash>(Hash.GET_HASH_KEY, convertEnum);
            return outputInt;
        }

        private int GetAmmoCount(string pAmmoType)
        {
            int ammoCount = Function.Call<int>(Hash.GET_PED_AMMO_BY_TYPE, mPlayerPed, ConvertEnumToInt(pAmmoType));
            return ammoCount;
        }

        

    }
}

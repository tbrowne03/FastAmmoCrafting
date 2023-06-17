using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

using RDR2.Native;

namespace RDR2
{
    public class FastSplitPointCrafting : Script
    {
        //Member Variables
        private bool mGiveAmmo = false;
        private int mAmmoIncrement;
        private Ped mPlayerPed;
        private Keys mKeyBinding;

        private int mAmmoAddType;
        private int mAmmoRemoveType;        

        private List<int> validAmmoTypes = new List<int>();
        private Dictionary<int, int> splitAmmoLookup = new Dictionary<int, int>();

        /// <summary>
        /// Constructor
        /// </summary>
        public FastSplitPointCrafting()
        {
            //Initialize Variables
            mPlayerPed = Game.Player.Character;

            //Initialize Listeners
            KeyDown += OnKeyDown;
            Tick += OnTick;

            //Initialize Valid Ammo Types
            validAmmoTypes.Add(ConvertEnumToInt("AMMO_REVOLVER"));
            validAmmoTypes.Add(ConvertEnumToInt("AMMO_PISTOL"));
            validAmmoTypes.Add(ConvertEnumToInt("AMMO_REPEATER"));

            //Populate a dictionary to relate ammo type/ split point ammo type
            splitAmmoLookup.Add(ConvertEnumToInt("AMMO_REVOLVER"), ConvertEnumToInt("AMMO_REVOLVER_SPLIT_POINT"));
            splitAmmoLookup.Add(ConvertEnumToInt("AMMO_PISTOL"),   ConvertEnumToInt("AMMO_PISTOL_SPLIT_POINT"));
            splitAmmoLookup.Add(ConvertEnumToInt("AMMO_REPEATER"), ConvertEnumToInt("AMMO_REPEATER_SPLIT_POINT"));

            //Read the config file
            bool keyConfigSuccess = false;
            bool ammoIncrementSuccess = false;

            string filePath = "scripts/FastSplitPointCrafting.ini";
            string[] lines = new string[] {};
            try
            {
                lines = File.ReadAllLines(filePath);
            } catch (Exception)
            {
                //Do nothing
            }

            foreach (string line in lines)
            {
                string[] substring = line.Split('=');

                //Set the KeyBinding
                if (substring.Length == 2 &&
                    substring[0] == "fastAmmoCraftingKey")
                {
                    keyConfigSuccess = Enum.TryParse(substring[1], out mKeyBinding);
                }
                
                //Set the Increment Amount
                if (substring.Length == 2 &&
                    substring[0] == "fastAmmoCraftingAmount")
                {
                    ammoIncrementSuccess = int.TryParse(substring[1], out mAmmoIncrement);
                }

                break;
            }

            //Set default values if the config. file could not be loaded successfully
            if (!keyConfigSuccess)
            {
                mKeyBinding = Keys.K;
            }

            if (!ammoIncrementSuccess ||
                mAmmoIncrement < 0)
            {
                mAmmoIncrement = 25;
            }
        }

        /// <summary>
        /// Listener for Key Press Events
        /// </summary>
        /// <param name="sender">The event sender</param>
        /// <param name="e">The KeyEventArgs</param>
        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == mKeyBinding)
            {
                //If player is crouching
                //GET_PED_CROUCH_MOVEMENT: Hash._0xD5FE956C70FF370B
                if (Function.Call<bool>(Hash._0xD5FE956C70FF370B, mPlayerPed))
                {
                    //Get the current weapon
                    //_GET_SELECTED_PED_WEAPON: Hash._0x8425C5F057012DAB
                    int currentPlayerWeapon = (int)Function.Call<Hash>(Hash._0x8425C5F057012DAB, mPlayerPed);

                    //Get the ammo type for the weapon
                    //_GET_AMMO_TYPE_FOR_WEAPON: Hash._0x5C2EA6C44F515F34
                    int defaultAmmoType = (int)Function.Call<Hash>(Hash._0x5C2EA6C44F515F34, currentPlayerWeapon);

                    //If the weapon uses a valid ammo type
                    if (validAmmoTypes.Contains(defaultAmmoType))
                    {
                        mAmmoAddType = splitAmmoLookup[defaultAmmoType];
                        mAmmoRemoveType = defaultAmmoType;  

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
                Function.Call(Hash._0x106A811C6D3035F3, mPlayerPed, mAmmoAddType, mAmmoIncrement, 0x2CD419DC);

                //_REMOVE_AMMO_FROM_PED_BY_TYPE: Hash._0xB6CFEC32E3742779
                Function.Call(Hash._0xB6CFEC32E3742779, mPlayerPed, mAmmoRemoveType, mAmmoIncrement, 0x2188E0A3);

                RDR2.UI.Screen.ShowSubtitle($"Added {mAmmoIncrement} split point ammo to inventory.");

                mGiveAmmo = false;
            }
        }

        /// <summary>
        /// A generic method to convert an enum to its corresponding int
        /// </summary>
        /// <param name="convertEnum"></param>
        /// <returns>An int representing the enum</returns>
        private int ConvertEnumToInt(string convertEnum)
        {
            int output = (int)Function.Call<Hash>(Hash.GET_HASH_KEY, convertEnum);
            return output;
        }

        /// <summary>
        /// Gets the Players total ammo of a given type
        /// </summary>
        /// <param name="pAmmoType">The type of ammo</param>
        /// <returns>The amount of the ammo type currently in the players inventory</returns>
        private int GetAmmoCount(int pAmmoType)
        {
            int ammoCount = Function.Call<int>(Hash.GET_PED_AMMO_BY_TYPE, mPlayerPed, pAmmoType);
            return ammoCount;
        }
    }
}

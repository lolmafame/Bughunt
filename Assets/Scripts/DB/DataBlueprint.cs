using System;
using System.Collections.Generic;
using UnityEngine;

namespace BugHunt.Architecture
{
    /// <summary>
    /// BLUEPRINT ONLY: This file defines the data structure for the entire game.
    /// BACKEND DEV: Use these classes to design your API responses.
    /// FRONTEND DEV: Use these classes to build your UI (e.g., "Coins: " + player.wallet.coins).
    /// </summary>
    public class DataBlueprint : MonoBehaviour
    {
        // =================================================================================
        // REGION 1: GAMEPLAY MECHANICS (Player Stats, Virus AI, Progression)
        // Primary Owner: Gameplay Programmer
        // =================================================================================
        #region Gameplay Mechanics

        [Serializable]
        public class PlayerProfile
        {
            // KEY: This ID links Unity to Firebase Auth
            public string firebaseUserId;
            public string username;

            // PROGRESSION
            // We track the highest level unlocked so the player can't skip ahead.
            public int maxLevelReached = 1;

            // SCORING
            // "Scoring System: Tracks bug fixes and completion time"
            public int totalBugsFixed;
            public float totalPlayTimeSeconds;

            // This dictionary tracks high scores per level (LevelID -> Score)
            public Dictionary<string, int> levelHighScores = new Dictionary<string, int>();
        }

        [Serializable]
        public class VirusConfig
        {
            // "Adaptive virus enemy... growing faster... longer debugging takes"
            // We store these values in the database so we can balance the game 
            // without forcing players to update the app.

            public string enemyId = "virus_standard";

            // How fast the virus moves at the start of the level
            public float baseMoveSpeed = 3.0f;

            // "Aggression Growth": How much speed is added every second the player delays
            public float speedGrowthPerSecond = 0.1f;

            // The absolute maximum speed (so the game remains fair)
            public float maxSpeedCap = 12.0f;

            // Distance at which the virus 'notices' the player
            public float detectionRadius = 15.0f;
        }

        [Serializable]
        public class LevelData
        {
            public string levelId; // e.g., "level_1_syntax_error"
            public string bugType; // "Syntax", "Logic", "Runtime"
            public int targetBugsToFix;
            public float timeLimitSeconds;
        }

        #endregion


        // =================================================================================
        // REGION 2: ECONOMY & SHOP (Skins, Purchases, Currency)
        // Primary Owner: Backend/Database Dev
        // =================================================================================
        #region Economy & Shop

        [Serializable]
        public class PlayerWallet
        {
            // "Optional in-game purchases... character cosmetics"
            // We separate "Paid" currency (Gems) from "Earned" currency (Coins)
            // if you want a dual-currency system.
            public int coins; // Earned by fixing bugs
            public int gems;  // Bought via Maya/PayPal
        }

        [Serializable]
        public class CosmeticItem
        {
            public string itemId;       // e.g., "skin_cyber_ninja"
            public string displayName;  // e.g., "Cyber Ninja Outfit"
            public string description;
            public string slotType;     // "Head", "Body", "Weapon"

            // The visual asset path in Unity Resources
            public string prefabPath;

            public bool isOwned;
            public bool isEquipped;
        }

        [Serializable]
        public class TransactionRequest
        {
            // This is the data packet we send to the Backend when clicking "Buy"
            public string userId;
            public string itemId;
            public float priceAmount;
            public string paymentProvider; // "MAYA" or "PAYPAL"
            public string timestamp;
        }

        #endregion
    }
}
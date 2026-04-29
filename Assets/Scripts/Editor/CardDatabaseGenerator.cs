using UnityEngine;
using UnityEditor;
using CallKitty.UI;
using CallKitty.Core;
using System.Collections.Generic;

namespace CallKitty.EditorTools
{
    public class CardDatabaseGenerator : Editor
    {
        [MenuItem("CallKitty/Generate Card Database")]
        public static void GenerateDatabase()
        {
            string dbPath = "Assets/Resources/CardDatabase.asset";
            
            // Ensure Resources folder exists
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }

            CardDatabase db = AssetDatabase.LoadAssetAtPath<CardDatabase>(dbPath);
            if (db == null)
            {
                db = ScriptableObject.CreateInstance<CardDatabase>();
                AssetDatabase.CreateAsset(db, dbPath);
            }

            List<CardDatabase.CardSpriteMapping> mappings = new List<CardDatabase.CardSpriteMapping>();

            string[] suits = { "Clubs", "Diamonds", "Hearts", "Spades" };
            Suit[] suitEnums = { Suit.Clubs, Suit.Diamonds, Suit.Hearts, Suit.Spades };
            
            string[] ranks = { "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K", "A" };
            Rank[] rankEnums = { Rank.Two, Rank.Three, Rank.Four, Rank.Five, Rank.Six, Rank.Seven, Rank.Eight, Rank.Nine, Rank.Ten, Rank.Jack, Rank.Queen, Rank.King, Rank.Ace };

            for (int i = 0; i < suits.Length; i++)
            {
                for (int j = 0; j < ranks.Length; j++)
                {
                    string spriteName = $"card{suits[i]}_{ranks[j]}";
                    string[] guids = AssetDatabase.FindAssets($"{spriteName} t:sprite", new[] { $"Assets/Sprites/{suits[i]}" });
                    
                    if (guids.Length > 0)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                        Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                        if (s != null)
                        {
                            mappings.Add(new CardDatabase.CardSpriteMapping
                            {
                                suit = suitEnums[i],
                                rank = rankEnums[j],
                                sprite = s
                            });
                        }
                    }
                }
            }

            // Load Card Back
            string[] backGuids = AssetDatabase.FindAssets("t:sprite", new[] { "Assets/Sprites/Card Back" });
            if (backGuids.Length > 0)
            {
                string backPath = AssetDatabase.GUIDToAssetPath(backGuids[0]);
                db.cardBack = AssetDatabase.LoadAssetAtPath<Sprite>(backPath);
            }

            db.cardSprites = mappings.ToArray();
            
            EditorUtility.SetDirty(db);
            AssetDatabase.SaveAssets();

            Debug.Log($"Card Database generated successfully with {mappings.Count} cards.");
        }
    }
}

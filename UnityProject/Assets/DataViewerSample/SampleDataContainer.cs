using System;
using System.Collections.Generic;
using UnityEngine;

namespace Obelune.DataViewer.Sample
{
    // data-viewer の検証用のデータ。利用者の DataContainer の例（docs/data-viewer/overview.md）と同じ形にしてある。
    // data-viewer のパッケージはこのアセンブリを参照しない。アセットとリフレクションだけで読む
    public class SampleDataContainer : ScriptableObject
    {
        public CharacterGroup character = new CharacterGroup();
        public AbilityGroup ability = new AbilityGroup();
    }

    // グループ: データの置き場と、ビューアでの表示の単位。参照はグループをまたいでよい
    [Serializable]
    public class CharacterGroup
    {
        public CharacterData characterData = new CharacterData();
        public CharacterAbilityCollectionData characterAbilityCollectionData = new CharacterAbilityCollectionData();
    }

    [Serializable]
    public class AbilityGroup
    {
        public AbilityData abilityData = new AbilityData();
    }

    public enum Rarity { N = 1, R = 2, SR = 3, SSR = 4 }

    [Serializable]
    public class CharacterData
    {
        [Serializable]
        public struct Data
        {
            public int character_id;
            public int attribute;
            public Rarity rarity;
            public int power;
            public int hp;
            public int speed;
        }

        public List<Data> data = new List<Data>();
    }

    [Serializable]
    public class CharacterAbilityCollectionData
    {
        [Serializable]
        public struct Data
        {
            public int character_id;
            public int ability_id;
            public int slot;
        }

        public List<Data> data = new List<Data>();
    }

    [Serializable]
    public class AbilityData
    {
        [Serializable]
        public struct Data
        {
            public int ability_id;
            public int evaluator;
            public int effect;
            public int cost;
            public float rate;
            public string script_key;
        }

        public List<Data> data = new List<Data>();
    }
}

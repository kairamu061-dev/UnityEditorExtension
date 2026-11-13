using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Obelune.DataViewer.Sample.Editor
{
    // 検証用のサンプルデータを Generated/ に作る。同じ種からは同じ中身になる
    public static class SampleGenerator
    {
        public const string Folder = "Assets/DataViewerSample/Generated";
        public const string AssetPath = Folder + "/SampleData.asset";

        public const int Characters = 3000;
        public const int Abilities = 1200;
        public const int CharacterAbilities = 20000;
        // 名前の CSV に載せない最後の何件か（名前のない値の見え方を確かめるため）
        public const int UnlabeledCharacters = 10;

        static readonly string[] Given =
        {
            "Claude", "アリア", "ベルン", "シオン", "ダリア", "エルク", "フィオ", "ガロ", "ハルト", "イリス",
            "ジーク", "カノン", "リオ", "ミナ", "ノア", "オルガ", "ピノ", "クオン", "ルカ", "セナ",
        };
        static readonly string[] Epithet =
        {
            "", "の騎士", "の魔導士", "の弓手", "の盗賊", "の僧侶", "の竜騎士", "の剣聖", "の賢者", "の守護者",
        };
        static readonly string[] AbilityBase =
        {
            "ファイアボール", "アイスランス", "ウィンドカッター", "ストーンウォール", "ホーリーライト",
            "シャドウバインド", "ヒール", "プロテクト", "ヘイスト", "スロウ", "ポイズン", "リジェネ",
        };
        static readonly string[] ScriptKeys =
        {
            "fireball", "icelance", "windcutter", "stonewall", "holylight",
            "shadowbind", "heal", "protect", "haste", "slow", "poison", "regen",
        };
        static readonly string[] Attributes = { "炎", "水", "風", "土", "光", "闇" };

        [MenuItem("Tools/Obelune/Data Viewer/Spike1/Generate Sample Data")]
        public static void Generate()
        {
            Directory.CreateDirectory(Folder);
            var rng = new System.Random(12345);
            var c = ScriptableObject.CreateInstance<SampleDataContainer>();

            for (int i = 1; i <= Characters; i++)
                c.character.characterData.data.Add(new CharacterData.Data
                {
                    character_id = i,
                    attribute = 1 + rng.Next(Attributes.Length),
                    rarity = (Rarity)(1 + rng.Next(4)),
                    power = 50 + rng.Next(950),
                    hp = 300 + rng.Next(4700),
                    speed = 1 + rng.Next(120),
                });

            for (int i = 1; i <= Abilities; i++)
                c.ability.abilityData.data.Add(new AbilityData.Data
                {
                    ability_id = 1000 + i,
                    evaluator = rng.Next(20),
                    effect = rng.Next(60),
                    cost = rng.Next(1, 10),
                    rate = (float)System.Math.Round(rng.NextDouble(), 2),
                    script_key = "abl_" + ScriptKeys[(i - 1) % ScriptKeys.Length] + "_" + i.ToString("D4"),
                });

            for (int i = 0; i < CharacterAbilities; i++)
                c.character.characterAbilityCollectionData.data.Add(new CharacterAbilityCollectionData.Data
                {
                    character_id = 1 + (i / 7) % Characters,
                    ability_id = 1001 + rng.Next(Abilities),
                    slot = i % 7 + 1,
                });
            // 参照切れを 2 つ仕込む（見え方を確かめるため）
            c.character.characterAbilityCollectionData.data[5] = new CharacterAbilityCollectionData.Data { character_id = 99999, ability_id = 1001, slot = 6 };
            c.character.characterAbilityCollectionData.data[12] = new CharacterAbilityCollectionData.Data { character_id = 2, ability_id = 88888, slot = 6 };
            // 「参照なし」の値（0）を 1 つ仕込む
            c.character.characterAbilityCollectionData.data[20] = new CharacterAbilityCollectionData.Data { character_id = 3, ability_id = 0, slot = 7 };

            AssetDatabase.DeleteAsset(AssetPath);
            AssetDatabase.CreateAsset(c, AssetPath);

            var names = new StringBuilder("value,label,note\n");
            for (int i = 1; i <= Characters - UnlabeledCharacters; i++)
            {
                var g = Given[(i - 1) % Given.Length];
                var e = Epithet[((i - 1) / Given.Length) % Epithet.Length];
                var n = (i - 1) / (Given.Length * Epithet.Length);
                names.Append(i).Append(',').Append(g).Append(e).Append(n > 0 ? " " + (n + 1) : "").Append(',')
                     .Append(i == 1 ? "サンプルの最初のキャラクター" : "").Append('\n');
            }
            File.WriteAllText(Folder + "/character_names.csv", names.ToString(), new UTF8Encoding(false));

            var abl = new StringBuilder("value,label,note\n");
            for (int i = 1; i <= Abilities; i++)
            {
                var b = AbilityBase[(i - 1) % AbilityBase.Length];
                var lv = (i - 1) / AbilityBase.Length + 1;
                abl.Append(1000 + i).Append(',').Append(b).Append(" Lv").Append(lv).Append(",\n");
            }
            File.WriteAllText(Folder + "/ability_names.csv", abl.ToString(), new UTF8Encoding(false));

            var attr = new StringBuilder("value,label,note\n");
            for (int i = 0; i < Attributes.Length; i++) attr.Append(i + 1).Append(',').Append(Attributes[i]).Append(",\n");
            File.WriteAllText(Folder + "/attribute_names.csv", attr.ToString(), new UTF8Encoding(false));

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[DataViewerSample] Generated {AssetPath}: characters={Characters}, abilities={Abilities}, characterAbilities={CharacterAbilities}");
        }
    }
}

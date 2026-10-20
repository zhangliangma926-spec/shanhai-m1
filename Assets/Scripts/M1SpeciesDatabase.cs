using System.Collections.Generic;

/// <summary>
/// Single source of truth for per-species stats and audio tone.
/// To add a new tameable beast, add ONE line to the table below — no need to touch
/// M1Actor's health logic or M1AudioDirector's tone logic anymore.
/// </summary>
public readonly struct M1SpeciesDefinition
{
    public readonly string id, displayName;
    public readonly float maxHealth, audioBaseTone;
    public M1SpeciesDefinition(string id, string displayName, float maxHealth, float audioBaseTone)
    { this.id = id; this.displayName = displayName; this.maxHealth = maxHealth; this.audioBaseTone = audioBaseTone; }
}

public static class M1SpeciesDatabase
{
    public const float DefaultMaxHealth = 105f;
    public const float DefaultAudioBaseTone = 420f;
    static readonly M1SpeciesDefinition Default = new M1SpeciesDefinition("", "灵兽", DefaultMaxHealth, DefaultAudioBaseTone);

    // id, 中文名, 生命上限, 音色基频(Hz)。
    // 音色分组参考原 M1AudioDirector 的关键词匹配：龟155 / 狐520 / 鸟1250 / 鹿760 / 猫貂920 / 其余默认420。
    // 注：azure_crane、red_xiao、glass_marten 三项是对原关键词匹配遗漏的修正（它们本该落入鸟/貂类音色，
    // 原来的英文 id 里没有 "bird"/"mink" 字样所以一直被漏判成默认音色）。其余物种与原逻辑计算结果完全一致。
    static readonly Dictionary<string, M1SpeciesDefinition> Table = new Dictionary<string, M1SpeciesDefinition>
    {
        { "stone_turtle",   new M1SpeciesDefinition("stone_turtle",   "岩甲灵龟", 150f, 155f)               },
        { "rock_boar",      new M1SpeciesDefinition("rock_boar",      "玄岩獠猪", 140f, DefaultAudioBaseTone) },
        { "blue_demon",     new M1SpeciesDefinition("blue_demon",     "岚角灵兽", 125f, DefaultAudioBaseTone) },
        { "spirit_beast",   new M1SpeciesDefinition("spirit_beast",   "幽蓝翼兽", 115f, DefaultAudioBaseTone) },
        { "wind_fox",       new M1SpeciesDefinition("wind_fox",       "赤尾灵狐", 90f,  520f)               },
        { "sky_bird",       new M1SpeciesDefinition("sky_bird",       "青羽灵雀", 75f,  1250f)              },
        { "glass_marten",   new M1SpeciesDefinition("glass_marten",   "琉璃水貂", 82f,  920f)   /*修正*/     },
        { "moon_moth",      new M1SpeciesDefinition("moon_moth",      "月羽灵蝶", 65f,  DefaultAudioBaseTone) },
        { "ember_cat",      new M1SpeciesDefinition("ember_cat",      "赤焰山猫", 95f,  920f)               },
        { "mist_deer",      new M1SpeciesDefinition("mist_deer",      "雾角灵鹿", 100f, 760f)               },
        { "six_eye_fox",    new M1SpeciesDefinition("six_eye_fox",    "六目灵狐", 88f,  520f)               },
        { "seven_face_fox", new M1SpeciesDefinition("seven_face_fox", "七面幻狐", 108f, 520f)               },
        { "red_xiao",       new M1SpeciesDefinition("red_xiao",       "赤霄朱雀", 118f, 1250f)  /*修正*/     },
        { "qing_jiao",      new M1SpeciesDefinition("qing_jiao",      "青蛟",     165f, DefaultAudioBaseTone) },
        { "azure_crane",    new M1SpeciesDefinition("azure_crane",    "苍羽仙鹤", 92f,  1250f)  /*修正*/     },
        { "amber_leopard",  new M1SpeciesDefinition("amber_leopard",  "琥珀雷豹", 132f, DefaultAudioBaseTone) },
        // v0.7.8 新增
        { "frost_wolf",     new M1SpeciesDefinition("frost_wolf",     "霜牙灵狼", 98f,  520f)               },
        { "jade_ox",        new M1SpeciesDefinition("jade_ox",        "玉角灵牛", 140f, 155f)               },
        { "flame_llama",    new M1SpeciesDefinition("flame_llama",    "焰驼",     112f, DefaultAudioBaseTone) },
        { "jade_hound",     new M1SpeciesDefinition("jade_hound",     "白玉灵犬", 84f,  920f)               },
    };

    public static M1SpeciesDefinition Get(string speciesId) =>
        !string.IsNullOrEmpty(speciesId) && Table.TryGetValue(speciesId, out var def) ? def : Default;

    public static float MaxHealth(string speciesId) => Get(speciesId).maxHealth;
    public static float AudioBaseTone(string speciesId) => Get(speciesId).audioBaseTone;
    public static string DisplayName(string speciesId) => Get(speciesId).displayName;
}

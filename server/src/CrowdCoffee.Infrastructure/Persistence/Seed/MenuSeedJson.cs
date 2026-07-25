using System.Text.Json.Serialization;

namespace CrowdCoffee.Infrastructure.Persistence.Seed;

/// <summary>Shape of the JSON extracted verbatim from the design's Menu.html (§4, §4a) — see menu-seed.json.</summary>
public class MenuSeedJson
{
    [JsonPropertyName("CATEGORIES")]
    public List<CategoryJson> Categories { get; set; } = [];

    [JsonPropertyName("D")]
    public Dictionary<string, string> Descriptions { get; set; } = [];

    [JsonPropertyName("ITEMS")]
    public List<ItemJson> Items { get; set; } = [];

    [JsonPropertyName("DRINK_CATS")]
    public List<string> DrinkCategories { get; set; } = [];

    [JsonPropertyName("MILK_OPTIONS")]
    public List<OptionJson> MilkOptions { get; set; } = [];

    [JsonPropertyName("SHOT_OPTION")]
    public OptionJson ShotOption { get; set; } = new();

    [JsonPropertyName("SYRUP_OPTIONS")]
    public List<OptionJson> SyrupOptions { get; set; } = [];
}

public class CategoryJson
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;
}

public class ItemJson
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("cat")]
    public string Cat { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("desc")]
    public string Desc { get; set; } = string.Empty;

    [JsonPropertyName("price")]
    public decimal Price { get; set; }
}

public class OptionJson
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("price")]
    public decimal Price { get; set; }
}

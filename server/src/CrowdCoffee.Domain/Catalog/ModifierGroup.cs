namespace CrowdCoffee.Domain.Catalog;

public enum ModifierSelectionType
{
    ExactlyOne,
    UpToN
}

public class ModifierGroup
{
    public Guid Id { get; private set; }
    public string Slug { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public ModifierSelectionType SelectionType { get; private set; }
    public int MaxSelections { get; private set; }
    public bool IsRequired { get; private set; }
    public int DisplayOrder { get; private set; }

    private readonly List<ModifierOption> _options = [];
    public IReadOnlyCollection<ModifierOption> Options => _options;

    private ModifierGroup()
    {
    }

    public ModifierGroup(Guid id, string slug, string name, ModifierSelectionType selectionType, int maxSelections, bool isRequired, int displayOrder)
    {
        Id = id;
        Slug = slug;
        Name = name;
        SelectionType = selectionType;
        MaxSelections = maxSelections;
        IsRequired = isRequired;
        DisplayOrder = displayOrder;
    }
}

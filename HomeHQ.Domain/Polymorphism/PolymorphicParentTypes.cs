using HomeHQ.Entities;

namespace HomeHQ.Polymorphism;

/// <summary>
/// ParentType values stored on polymorphic children. Must match the parent entity type name.
/// </summary>
public static class PolymorphicParentTypes
{
    public const string Asset = nameof(Asset);
    public const string Category = nameof(Category);
    public const string WarrantyType = nameof(WarrantyType);
    public const string AttachmentType = nameof(AttachmentType);

    private static readonly HashSet<string> ParentTypeNames = new(StringComparer.Ordinal)
    {
        Asset,
        Category,
        WarrantyType,
        AttachmentType
    };

    /// <summary>
    /// True when deleting this entity type should soft-delete its polymorphic children first.
    /// </summary>
    public static bool IsPolymorphicParent(string entityTypeName) => ParentTypeNames.Contains(entityTypeName);
}

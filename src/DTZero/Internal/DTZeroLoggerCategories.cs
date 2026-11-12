namespace DTZero.Internal;

/// <summary>
/// Logger category enumeration for DTZero.
/// Used to organize and filter log messages.
/// </summary>
public enum DTZeroLoggerCategory
{
    /// <summary>
    /// General library logs.
    /// </summary>
    General,

    /// <summary>
    /// Serialization operations (JSON, ordinal conversion).
    /// </summary>
    Serialization,

    /// <summary>
    /// Factory operations (Create, Fetch, Insert, Update, Delete).
    /// </summary>
    Factory,

    /// <summary>
    /// Remote call operations (client-side HTTP calls).
    /// </summary>
    Remote,

    /// <summary>
    /// Authorization checks.
    /// </summary>
    Authorization,

    /// <summary>
    /// Server-side request handling.
    /// </summary>
    Server
}

/// <summary>
/// Logger category name constants for DTZero.
/// Use these for filtering in appsettings.json or logging configuration.
/// </summary>
public static class DTZeroLoggerCategories
{
    /// <summary>
    /// Base category for all DTZero logs.
    /// </summary>
    public const string Base = "DTZero";

    /// <summary>
    /// Category for serialization operations.
    /// </summary>
    public const string Serialization = "DTZero.Serialization";

    /// <summary>
    /// Category for factory operations.
    /// </summary>
    public const string Factory = "DTZero.Factory";

    /// <summary>
    /// Category for remote call operations.
    /// </summary>
    public const string Remote = "DTZero.Remote";

    /// <summary>
    /// Category for authorization checks.
    /// </summary>
    public const string Authorization = "DTZero.Authorization";

    /// <summary>
    /// Category for server-side request handling.
    /// </summary>
    public const string Server = "DTZero.Server";

    /// <summary>
    /// Gets the category name for a DTZeroLoggerCategory.
    /// </summary>
    /// <param name="category">The category to get the name for.</param>
    /// <returns>The category name string.</returns>
    public static string GetCategoryName(DTZeroLoggerCategory category)
    {
        return category switch
        {
            DTZeroLoggerCategory.Serialization => Serialization,
            DTZeroLoggerCategory.Factory => Factory,
            DTZeroLoggerCategory.Remote => Remote,
            DTZeroLoggerCategory.Authorization => Authorization,
            DTZeroLoggerCategory.Server => Server,
            _ => Base
        };
    }
}

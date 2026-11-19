namespace PythonStudyCompanion.Models;

/// <summary>
/// Defines a study card loaded from the flashcard JSON file.
/// </summary>
public sealed class Flashcard
{
    /// <summary>
    /// Gets the stable identifier used to associate the card with study results.
    /// </summary>
    /// <value>A unique, nonempty identifier that should not change when the card is edited.</value>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the topic that determines the card's deck.
    /// </summary>
    /// <value>The topic name shown in the deck picker.</value>
    public required string Topic { get; init; }

    /// <summary>
    /// Gets the question shown on the front of the card.
    /// </summary>
    /// <value>Plain text with optional line breaks.</value>
    public required string Front { get; init; }

    /// <summary>
    /// Gets the answer shown on the back of the card.
    /// </summary>
    /// <value>Plain text with optional line breaks and Python examples.</value>
    public required string Back { get; init; }
}

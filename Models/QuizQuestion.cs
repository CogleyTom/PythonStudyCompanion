namespace PythonStudyCompanion.Models;

/// <summary>
/// Defines a multiple-choice question loaded from the quiz JSON file.
/// </summary>
public sealed class QuizQuestion
{
    /// <summary>
    /// Gets the stable identifier associated with the question's results.
    /// </summary>
    /// <value>A unique, nonempty identifier.</value>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the question's study topic.
    /// </summary>
    /// <value>The topic name shown in the quiz topic picker.</value>
    public required string Topic { get; init; }

    /// <summary>
    /// Gets the question presented to the learner.
    /// </summary>
    /// <value>Plain text with optional line breaks and Python examples.</value>
    public required string Question { get; init; }

    /// <summary>
    /// Gets the four possible answers in display order.
    /// </summary>
    /// <value>Four distinct, nonempty answer strings.</value>
    public required string[] Options { get; init; }

    /// <summary>
    /// Gets the position of the correct answer.
    /// </summary>
    /// <value>A zero-based index from zero through three.</value>
    public required int CorrectAnswerIndex { get; init; }

    /// <summary>
    /// Gets the explanation displayed after an answer is selected.
    /// </summary>
    /// <value>A nonempty explanation of the correct answer.</value>
    public required string Explanation { get; init; }
}

namespace PythonStudyCompanion.Models;

/// <summary>
/// Defines a read-and-reveal Python exercise loaded from the coding JSON file.
/// </summary>
public sealed class CodingExercise
{
    /// <summary>
    /// Gets the exercise's stable identifier.
    /// </summary>
    /// <value>A unique, nonempty identifier.</value>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the exercise's study topic.
    /// </summary>
    /// <value>The topic name shown in the exercise filter.</value>
    public required string Topic { get; init; }

    /// <summary>
    /// Gets the exercise title.
    /// </summary>
    /// <value>A short, descriptive title.</value>
    public required string Title { get; init; }

    /// <summary>
    /// Gets the exercise's difficulty level.
    /// </summary>
    /// <value>Easy, Medium, or Hard.</value>
    public required string Difficulty { get; init; }

    /// <summary>
    /// Gets the problem statement and constraints.
    /// </summary>
    /// <value>Plain text describing the required behavior.</value>
    public required string Description { get; init; }

    /// <summary>
    /// Gets the example used to demonstrate the problem.
    /// </summary>
    /// <value>Python statements that illustrate calling the solution.</value>
    public required string ExampleInput { get; init; }

    /// <summary>
    /// Gets the output produced by the example.
    /// </summary>
    /// <value>The expected printed output.</value>
    public required string ExpectedOutput { get; init; }

    /// <summary>
    /// Gets the hints in reveal order.
    /// </summary>
    /// <value>One or more nonempty hints.</value>
    public required string[] Hints { get; init; }

    /// <summary>
    /// Gets the reference Python solution.
    /// </summary>
    /// <value>Python source displayed as text, never executed by the application.</value>
    public required string Solution { get; init; }

    /// <summary>
    /// Gets the explanation of the solution and its trade-offs.
    /// </summary>
    /// <value>A nonempty explanation, including complexity where useful.</value>
    public required string Explanation { get; init; }
}

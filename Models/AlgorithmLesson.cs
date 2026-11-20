namespace PythonStudyCompanion.Models;

/// <summary>Defines the versioned algorithm reference catalog.</summary>
public sealed class AlgorithmLibrary
{
    /// <summary>Gets the catalog format version.</summary>
    /// <value>One for the currently supported format.</value>
    public required int SchemaVersion { get; init; }

    /// <summary>Gets the catalog's display title.</summary>
    /// <value>A nonempty library title.</value>
    public required string Title { get; init; }

    /// <summary>Gets the supplied algorithm lessons.</summary>
    /// <value>The lessons with stable IDs and unique learning-order values.</value>
    public required AlgorithmLesson[] Algorithms { get; init; }
}

/// <summary>Defines an educational Python algorithm reference lesson.</summary>
public sealed class AlgorithmLesson
{
    /// <summary>Gets the stable lesson identifier used in its URL.</summary>
    /// <value>A unique identifier containing ASCII letters, digits, hyphens, or underscores.</value>
    public required string Id { get; init; }

    /// <summary>Gets the recommended learning position.</summary>
    /// <value>A unique positive integer.</value>
    public required int Order { get; init; }

    /// <summary>Gets the category used to group and filter lessons.</summary>
    /// <value>A nonempty category name.</value>
    public required string Category { get; init; }

    /// <summary>Gets the algorithm title.</summary>
    /// <value>A nonempty lesson title.</value>
    public required string Title { get; init; }

    /// <summary>Gets the lesson difficulty.</summary>
    /// <value>Beginner, Intermediate, or Advanced.</value>
    public required string Difficulty { get; init; }

    /// <summary>Gets the example's programming language.</summary>
    /// <value>Python, represented by the lowercase language identifier.</value>
    public required string Language { get; init; }

    /// <summary>Gets the algorithm description.</summary>
    /// <value>A plain-text summary of the approach.</value>
    public required string Summary { get; init; }

    /// <summary>Gets the algorithm's intended uses.</summary>
    /// <value>A plain-text explanation of suitable problems.</value>
    public required string WhenToUse { get; init; }

    /// <summary>Gets the original Python example.</summary>
    /// <value>Source code including its original indentation and line breaks.</value>
    public required string Code { get; init; }

    /// <summary>Gets the example input guidance.</summary>
    /// <value>Plain text describing where to find the sample input.</value>
    public required string ExampleInput { get; init; }

    /// <summary>Gets the ordered walkthrough steps.</summary>
    /// <value>One or more nonempty explanatory steps.</value>
    public required string[] Walkthrough { get; init; }

    /// <summary>Gets the stated time complexity.</summary>
    /// <value>The supplied time bound and qualifications.</value>
    public required string TimeComplexity { get; init; }

    /// <summary>Gets the stated space complexity.</summary>
    /// <value>The supplied space bound and qualifications.</value>
    public required string SpaceComplexity { get; init; }

    /// <summary>Gets the reasoning behind the complexity bounds.</summary>
    /// <value>A plain-text complexity explanation.</value>
    public required string ComplexityExplanation { get; init; }

    /// <summary>Gets common implementation mistakes.</summary>
    /// <value>One or more nonempty cautions.</value>
    public required string[] CommonMistakes { get; init; }
}

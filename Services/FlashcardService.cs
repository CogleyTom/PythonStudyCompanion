using System.Net.Http.Json;
using System.Text.Json;
using PythonStudyCompanion.Models;

namespace PythonStudyCompanion.Services;

/// <summary>
/// Loads and validates the locally hosted flashcard catalog.
/// </summary>
public sealed class FlashcardService
{
    private readonly HttpClient http;

    /// <summary>
    /// Initializes a flashcard content service.
    /// </summary>
    /// <param name="http">The HTTP client configured with the application's base address.</param>
    public FlashcardService(HttpClient http)
    {
        this.http = http;
    }

    /// <summary>
    /// Loads flashcards in the order defined by the content file.
    /// </summary>
    /// <returns>The validated catalog, which may be empty.</returns>
    /// <exception cref="HttpRequestException">The content file cannot be retrieved.</exception>
    /// <exception cref="OperationCanceledException">The HTTP request times out or is canceled.</exception>
    /// <exception cref="JsonException">The file is malformed or contains missing fields or duplicate identifiers.</exception>
    public async Task<IReadOnlyList<Flashcard>> LoadAsync()
    {
        var cards = await http.GetFromJsonAsync<List<Flashcard>>("data/flashcards.json")
            ?? throw new JsonException("The flashcard file must contain a JSON array.");
        var identifiers = new HashSet<string>(StringComparer.Ordinal);

        foreach (var card in cards)
        {
            if (card is null ||
                string.IsNullOrWhiteSpace(card.Id) ||
                string.IsNullOrWhiteSpace(card.Topic) ||
                string.IsNullOrWhiteSpace(card.Front) ||
                string.IsNullOrWhiteSpace(card.Back) ||
                !identifiers.Add(card.Id))
            {
                throw new JsonException("Each flashcard needs a unique ID and a nonempty topic, front, and back.");
            }
        }

        return cards;
    }
}

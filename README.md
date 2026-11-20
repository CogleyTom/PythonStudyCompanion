# Python Study Companion

A personal Python learning and technical interview preparation app built with .NET 10, C#, and Blazor WebAssembly. Designed as a mobile-first Progressive Web App (PWA) for iPhone, with local progress storage and JSON study content.

No backend, database server, authentication, or Python execution engine is required.

## Project status

Milestones 1-4 and the app-side release preparation for Milestone 5 are implemented, including install icons, offline status, controlled updates, and static-host routing/header configuration. Cloudflare Pages deployment and physical-iPhone verification are still pending.

## Features

- **Home:** Due/new flashcards, total card reviews, quiz accuracy, daily activity, and study streak.
- **Flashcards:** Topic selection, 5/10/15-card session limits, tap-to-flip cards, navigation, shuffle within review priorities, and Again/Good/Easy ratings.
- **Quizzes:** Topic selection, 5/10/15-question rounds, immediate feedback and explanations, scores, missed-question review, and saved quiz history. Smaller topic selections use the available questions without adding duplicates.
- **Coding practice:** Topic and difficulty filters, problem statements, examples, progressive hints, reference solutions, and explanations. Code is displayed, never executed.
- **Algorithm Library:** Open **Practice > Algorithm Library** for 20 reference lessons, grouped categories, category/difficulty filters, live search, and learning-order or difficulty sorting. Each linked lesson includes the original Python example, syntax highlighting, Copy code, collapsible walkthroughs, complexity, and common mistakes.
- **Progress:** Local persistence, spaced repetition, topic proficiency, seven-day activity, coding reviews, and JSON backup export/import.
- **Interface:** Four navigation tabs, responsive layouts, system-aware light/dark mode, and reduced-motion support.
- **PWA:** Original installation icons, offline-readiness status, and a user-controlled update prompt.

Study content includes **120 flashcards, 40 quiz questions, and 8 coding exercises** across:

1. Python Fundamentals
2. Python Functions
3. Lists, Tuples, Sets, and Dictionaries
4. Object-Oriented Programming
5. Data Structures
6. Algorithms and Coding Patterns
7. Big O Complexity
8. Technical Interview Questions

## Run locally

### Visual Studio 2026

1. Install the **ASP.NET and web development** workload and **.NET 10 SDK**.
2. Open `PythonStudyCompanion.slnx`.
3. Select the **https** launch profile and press **F5**.
4. Trust the ASP.NET Core development certificate if prompted.

### Command line

Run from the project folder:

```powershell
dotnet run --project .\PythonStudyCompanion.csproj --launch-profile https
```

The configured HTTPS development address is `https://localhost:7253`.

To build or check formatting:

```powershell
dotnet build .\PythonStudyCompanion.slnx
dotnet format .\PythonStudyCompanion.slnx --verify-no-changes --no-restore
```

## Project structure

```text
Layout\                         Shared application shell and bottom navigation
Models\                         Study content and progress models
Pages\                          Dashboard, flashcards, practice, and progress screens
Services\                       JSON loading, progress storage, and scheduling
Properties\launchSettings.json   Local development launch profiles
wwwroot\
  css\app.css                   Application styling
  data\flashcards.json          Flashcard catalog
  data\quizzes.json             Multiple-choice question catalog
  data\coding-exercises.json    Coding exercise catalog
  data\python_algorithm_library.json  Algorithm reference lessons
  js\algorithms.js             Safe Python highlighting and clipboard support
  js\progress.js               Local storage and backup-download interop
  js\pwa.js                    Offline status and update coordination
  icon.svg                     Editable source for installation icons
  apple-touch-icon.png         iPhone home-screen icon
  manifest.webmanifest          PWA installation metadata
  service-worker.js            Development worker without offline caching
  service-worker.published.js  Published offline-caching worker
App.razor                       Routing
Program.cs                      Application startup and service registration
PythonStudyCompanion.csproj      .NET project
PythonStudyCompanion.slnx        Visual Studio solution
```

## Add study content

Edit the appropriate JSON array under `wwwroot\data`. Models in `Models` define the required fields.

- Keep IDs **unique within each catalog and stable over time**; saved progress uses those IDs.
- Use consistent topic names across the catalogs.
- Content is plain text, not HTML or Markdown. Use `\n` inside JSON strings for line breaks.
- Quiz questions require exactly four distinct, nonempty options, a zero-based `correctAnswerIndex` from `0` to `3`, and an explanation.
- Coding exercises require a difficulty of `Easy`, `Medium`, or `Hard`, example input/output, at least one hint, a solution, and an explanation.
- Republish after content changes so the published offline cache includes the updated files.

For example, a flashcard entry looks like:

```json
{
  "id": "fundamentals-example",
  "topic": "Python Fundamentals",
  "front": "What does len('Python') return?",
  "back": "6\n\nlen returns the number of characters in this string."
}
```

### Algorithm reference lessons

Edit `wwwroot\data\python_algorithm_library.json` to add or change library lessons. The original supplied file is preserved, including all example code and indentation. It uses a catalog object with `schemaVersion: 1`, a `title`, and an `algorithms` array.

Each lesson needs a stable URL-safe `id`, a unique positive `order`, `category`, `title`, `difficulty` (`Beginner`, `Intermediate`, or `Advanced`), `language: "python"`, `summary`, `whenToUse`, `code`, `exampleInput`, `walkthrough`, `timeComplexity`, `spaceComplexity`, `complexityExplanation`, and `commonMistakes`. The walkthrough and mistakes are nonempty string arrays. Code is reference text only and is never executed.

Lesson URLs use `/practice/algorithms/{id}`. The hosting rules include these routes; the existing published worker caches the JSON and highlighting module along with the app. Offline access still requires successful caching on a host that preserves the published files. The current Netlify branding-injection limitation also applies to this library.

Copy code requires browser clipboard permission; when blocked, the code is selected for manual copying. Library reading does not change quiz scores, flashcard scheduling, or study streaks. Optional bookmarks/read history are not implemented.

## Progress and review rules

Progress is stored in this browser on this device. There are no accounts or cloud synchronization.

### Flashcard scheduling

| Rating | Next review |
|---|---|
| Again | In 10 minutes; resets the successful interval |
| Good | Starts at 1 day, then doubles the previous successful interval |
| Easy | Starts at 3 days, then triples the previous successful interval |

Intervals are capped at 365 days. New cards count as due. Sessions prioritize due misses, other due cards, then new cards. Disable **Due and new cards only** to practice ahead.

Changing a card's rating within the same round corrects the original attempt instead of adding another. Starting a new review round counts as another attempt.

### Quizzes, activity, and proficiency

- Quiz answers save before feedback is shown, including answers in unfinished rounds. Completed rounds and missed-question retries have separate history entries.
- Quiz accuracy includes all saved quiz answers, including unfinished rounds and retries.
- The daily goal is **10 study actions**: card ratings, quiz answers, or coding exercises explicitly marked reviewed.
- Coding reviews count at most once per exercise per recorded local date. Revealing a solution alone does not count, and coding reviews are not scored as correct answers.
- Streaks use recorded local study dates and remain current through yesterday.
- Topic proficiency uses the latest 20 answers. **Confident** requires at least 5 answers across 3 distinct items with at least 80% accuracy.
- Active study screens are not resumed after navigation or reload; already saved answers remain recorded.

### Backups

Use the **Progress** tab to export or import a JSON backup.

- Export downloads the currently saved progress document.
- Import accepts backups up to 10 MiB and validates the format before showing a preview.
- **Replace local progress** replaces the current data; it does not merge records.
- Invalid backups and failed writes leave existing saved data unchanged.
- Clearing browser website data removes local progress. Export a backup before moving devices or changing the site's address.

If storage is unavailable or another tab has saved newer progress, the app shows an error rather than silently advancing. Reload to read newer data; backup controls remain available for recovery.

## Publish and offline use

```powershell
dotnet publish .\PythonStudyCompanion.csproj --configuration Release
```

The static site is generated in:

```text
bin\Release\net10.0\publish\wwwroot\
```

Host the contents of that folder over HTTPS. Configure navigation routes such as `/practice/quiz` to fall back to `index.html`, while serving existing static assets normally.

The current app assumes deployment at the site's root. Subfolder hosting requires matching base-path changes in `wwwroot\index.html` and `wwwroot\service-worker.published.js`.

**Offline caching is enabled in published builds, not development runs.** Load the published app online and allow its service worker to finish installing and caching before testing offline.

The header reports **Ready for offline study** after a published worker has installed and controls the page. A development run instead reports **Development - online only**. Offline status describes the cached application, not the durability of browser storage; keep backups of progress.

### App updates

After a new release is published, the app checks in the background when opened, brought to the foreground, or reconnected. Use **Check for updates** to check manually or reveal a dismissed update prompt.

When a complete update is downloaded, choose **Reload to update** or **Later**. The app does not force an immediate reload just because an update was detected. Once all old app windows close, the browser can also activate a waiting update normally.

Applying an update pauses interaction and waits for pending progress writes before reloading. Other open app tabs also reload when the new worker takes control. Saved records remain in local storage, but current flashcard/quiz sessions restart. A failed download leaves the existing version available. If activation cannot finish, close all open app windows and reopen.

Changing the site's address, clearing website data, or reinstalling can affect local storage. Export a backup first; ordinary updates at the same address do not reset saved progress.

### Upload to Cloudflare Pages

1. Run the publish command above.
2. Sign in to `https://dash.cloudflare.com` and open **Workers & Pages**. Select **Create application**, choose **Pages**, and use the direct-upload / **Drag and drop your files** option.
3. In Windows File Explorer, open `bin\Release\net10.0\publish`.
4. Enter a project name such as `python-study-companion`, drag its **wwwroot** folder onto the upload area, and select **Deploy site**. Do not upload the source `wwwroot` at the project root.
5. Open the project's production address ending in `.pages.dev`. Wait for **Ready for offline study**.
6. Open that same address in Safari on the iPhone and follow the checklist below.

The published folder includes `_redirects` for the current application routes and `_headers` for HTTP cache revalidation and basic response headers. Missing static assets are not rewritten to HTML. When adding a new page route, add it to `_redirects` too. Deploy at the site's root, not in a subfolder.

For future releases, publish again and choose **Create a new deployment** in the **same Pages project**, selecting the production environment. Keep the same production address so saved progress remains associated with it. Verify direct navigation to `/practice/quiz` after upload.

Netlify was initially tried, but its injected **Powered by Netlify** badge changed `index.html` and broke offline integrity checks. Cloudflare Pages is the selected alternative; do not disable integrity validation to work around HTML injection. When moving from the old Netlify address, export any existing progress and import it on the new address.

Do not upload source files, `obj`, or development configuration: publish only the generated `wwwroot` contents. No account credentials or automated deployment tools are required in this repository.

### Physical-iPhone checklist

A desktop browser check does not replace these real-device checks. After HTTPS deployment:

1. Open the site in Safari, wait for **Ready for offline study**, then use **Share > Add to Home Screen**. Confirm the icon and standalone appearance.
2. Launch from the home screen. Check all four tabs in portrait and landscape, safe-area spacing, text size, dark mode, card flips, selects, and touch targets.
3. Rate a flashcard, answer a quiz question, and mark a coding exercise reviewed. Close and reopen the app and confirm the saved statistics remain.
4. Enable Airplane Mode with Wi-Fi off, close the app, and reopen it. Visit every tab and start a flashcard, quiz, and coding session.
5. Export progress to Files while offline. Import that backup, inspect the preview, and verify cancel and confirmed replacement.
6. Reconnect and publish another release at the same address. Confirm the update prompt, **Later**, and **Reload to update**. Verify saved progress survives and the updated version reopens offline.
7. If Safari and the home-screen app show different progress, export from the instance containing your records and import into the intended one. Do not assume storage is shared between browser contexts.

Uploading to Cloudflare Pages, checking the live site's routing/headers, and completing this checklist remain the final release steps.

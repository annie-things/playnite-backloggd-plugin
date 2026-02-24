A Playnite plugin which imports Backloggd rating data.

## Supported metadata fields

- `CommunityScore`
- `Links`
- `Description`

## Rating source and mapping

- Source: Backloggd page JSON-LD `AggregateRating` (`ratingValue`, `ratingCount`).
- Fallback source: visible rating score block in page HTML when JSON-LD aggregate data is missing.
- Mapping: Backloggd `0.0 - 5.0` -> Playnite `0 - 100` via `round(score * 20)`.

## Rating count behavior

- `ratingCount` is added to Backloggd link label in `Links`, e.g. `Backloggd (97,968 ratings)`.
- Optional settings to write the rating count line (showing the number of Backloggd ratings) to:
  - Top of game description.
  - Game notes (for users using notes in Playnite / Play Notes).

## Settings UI actions

- Toggle writing rating count to description.
- Toggle writing rating count to notes.
- Re-write rating count for:
  - all games
  - selected games

## Build

- Target framework: `.NET Framework 4.6.2`.
- Required env var: `PLAYNITE_SDK_PATH` pointing to the directory containing `Playnite.SDK.dll`.

Build command:

```powershell
$env:PLAYNITE_SDK_PATH="C:\Users\User\AppData\Local\Playnite"
dotnet build BackloggdCommunityScore\BackloggdCommunityScore.csproj -c Release
```

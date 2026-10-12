# Contributing

Thanks for helping improve Feizai Tracker.

## Development

1. Use Windows with the .NET 8 SDK.
2. Create a branch from `main`.
3. Keep changes focused and add tests for behaviour changes.
4. Run:

   ```powershell
   dotnet test FeizaiTracker.sln -c Release
   ```

5. Open a pull request describing the user-visible outcome.

## Food data

- Prefer FSANZ AFCD for generic Australian foods.
- Use Open Food Facts for branded packaged products.
- Include a source and use values per 100 g or 100 mL.
- Do not copy proprietary supermarket catalogues or images into the repository.
- Keep serving sizes clearly labelled as estimates where applicable.

## Privacy

Do not add telemetry, advertising identifiers or network transmission of diary
data. Any new online feature must clearly document what it sends and why.


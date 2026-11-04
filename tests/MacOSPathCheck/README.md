# macOS bundle-path regression checks

On macOS with the .NET 10 SDK, run from the repository root:

```sh
dotnet run --project tests/MacOSPathCheck
```

The dependency-free console harness links the production `InitOSX.cs` source.
Only the Steam lookup and compatibility-layer state are stubbed. It uses a
temporary Unity macOS application bundle and removes that fixture afterwards;
it does not require or modify an installed game or create a native window.

It checks the valid bundle layout and the missing-assets, missing-game, and
missing-Steam results, as well as the resolved data path and bundle enumeration.
A failed assertion exits with status 1; running on another OS exits with status 2.

To check an older initializer without replacing the working source, set the
`InitializerSource` MSBuild property to its absolute path.

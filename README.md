# Vault Productions Tools

Portable editor utilities for Vault Productions Unity projects.

## Tools

- `Tools > Vault Productions > Build Settings (Package)`

Use this before Android builds to set:

- Version name
- Android version code / build number
- Production or development build mode
- Google Play AAB output
- Android keystore signing settings
- Runtime debugger visibility, when the Vault runtime debug framework exists in the target project

## Notes

This package can control the runtime debug framework settings asset at:

`Assets/VaultProductions/RuntimeDebugFramework/Runtime/Resources/RuntimeDebugFramework/RuntimeDebugFrameworkSettings.asset`

Keep runtime debugger disabled for public Play Store production builds.

## Install In Another Project

Use Unity Package Manager:

1. Open the target Unity project.
2. Go to `Window > Package Manager`.
3. Press `+`.
4. Choose `Add package from disk`.
5. Select `Packages/com.vaultproductions.tools/package.json` from this project, or copy this whole folder into the target project's `Packages` folder first.

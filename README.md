# ExpressionMenu Relinker

[日本語](README.ja.md)

A Unity Editor tool for batch-relinking VRChat ExpressionMenu SubMenu references between copied folders.

## Concept

ExpressionMenu Relinker was created to simplify the process of duplicating and maintaining VRChat ExpressionMenus across avatar projects.

When copying a folder containing multiple related ExpressionMenus for a new avatar, the copied menus continue to reference the original SubMenus. Manually updating each reference can be tedious, especially when dealing with deeply nested menu structures.

This tool scans the ExpressionMenus in a destination folder and automatically finds corresponding SubMenus in the copied folder. You can review the detected references before applying the changes.

## Overview

- **Unity Version:** 2022.3.22f1
- **Menu:** `nur_ieren > ExpressionMenu 参照付け替え`
- **Supported Assets:** VRChat `VRCExpressionsMenu`

## Features

- **Folder-Based Relinking** — Replaces SubMenu references in ExpressionMenus within a specified destination folder.
- **Optional Source Folder** — Specify the original folder to identify references that should be redirected to the copied folder.
- **Automatic Path Matching** — Matches menus using their relative paths between the source and destination folders.
- **Filename Fallback** — If no matching relative path exists, attempts to find a menu with the same filename, provided it is unique in the destination folder.
- **Automatic Reference Detection** — Scans SubMenu references in all ExpressionMenus found within the destination folder.
- **Source-Free Matching** — When no source folder is specified, detects references pointing outside the destination folder and attempts to match them by filename.
- **Preview and Selective Processing** — Displays detected references and allows individual items to be excluded before applying changes.
- **Unresolved Reference Listing** — When a source folder is specified, lists references for which no corresponding destination menu can be found.
- **Automatic Rescan** — Scans the destination folder again after applying changes to verify the remaining targets.
- **Undo Support** — Records changes in Unity's Undo system.

## Requirements

- Unity 2022.3.22f1
- VRChat SDK3 Avatars (`VRCExpressionsMenu`)

The tool does not directly reference VRC SDK types. It accesses ExpressionMenu data through Unity's AssetDatabase and SerializedObject APIs, avoiding a direct compile-time dependency on the SDK.

## Installation

Place the script at:

```text
Assets/nur_ieren/ExpressionMenuRelinker/ExpressionMenuRelinker.cs
```

An `Editor` folder or asmdef is not required. The entire script is wrapped in `#if UNITY_EDITOR`.

## How to use

1. Open `nur_ieren > ExpressionMenu 参照付け替え` from the top menu bar.
2. Assign the **destination folder** containing the copied ExpressionMenus.
3. Optionally assign the **source folder** containing the original ExpressionMenus.
4. Click `スキャン` to find references that can be relinked.
5. Review the detected references and their current and destination paths.
6. Uncheck any individual items you do not want to modify.
7. Click `付け替え実行` to apply the changes.

After applying, the tool automatically rescans the destination folder. When the number of remaining targets reaches 0, the operation is complete.

## How it works

The tool searches for all `VRCExpressionsMenu` assets within the destination folder and examines their SubMenu references.

References that already point inside the destination folder are left untouched.

The matching method depends on whether a source folder is specified:

| Source Folder | Target References | Matching Method |
| --- | --- | --- |
| Specified | References pointing inside the source folder | Same relative path in the destination folder. If no match exists, a unique filename match is attempted. |
| Not specified | References pointing outside the destination folder | A filename that is unique within the destination folder. |

When a source folder is specified, references that belong to neither the source nor the destination folder are ignored.

If no corresponding destination menu can be found while using a specified source folder, the reference is listed as **unresolved**.

## Important notes

- Only SubMenu references are modified. ExpressionParameters and icon textures are not affected.
- The source folder is optional, but specifying it provides more precise matching.
- When no source folder is specified, references without a unique filename match are skipped and are not listed as unresolved.
- Individual references can be excluded from processing by unchecking them in the preview list.
- Changes can be undone using Unity's Undo system (`Ctrl + Z`).
- The tool automatically rescans after applying changes, allowing you to verify the results.
- Back up the destination folder before the first use.

## Author

- nur_ieren
- vor_ges

## License

This project is licensed under the MIT License. See the `LICENSE` file for details.

[MIT License](https://opensource.org/license/mit)
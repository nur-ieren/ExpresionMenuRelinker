# ExpressionMenu Relinker

[日本語](README.ja.md)

A Unity editor tool that instantly re-points VRChat ExpressionMenu SubMenu references from a copied-from folder to the copied-to folder.

When you duplicate a folder of nearly identical ExpressionMenus for a new avatar, the copied menus still reference the **original** SubMenus. Fixing each reference by hand is tedious. This tool fixes all of them in one go by specifying folders.

## Requirements

- Unity 2022.3.22f1 (the version this tool was developed with)
- VRChat SDK3 Avatars (`VRCExpressionsMenu`)

The tool does not depend on VRC SDK types directly, so it is not affected by SDK version differences.

## Installation

Place the script at:

```
Assets/nur_ieren/ExpressionMenuRelinker/ExpressionMenuRelinker.cs
```

It does not need an `Editor` folder or an asmdef. The whole file is wrapped in `#if UNITY_EDITOR`.

## Usage

1. Open **nur_ieren → ExpressionMenu 参照付け替え** from the top menu bar.
2. Set the **destination folder** (the copied folder).
3. Optionally set the **source folder** (the original folder).
4. Click **Scan** to list the references that will be changed.
5. Review the list (you can uncheck individual items), then click **Apply**.

After applying, the tool rescans automatically. When the target count reaches 0, you are done.

## How it works

The tool checks every SubMenu reference of every ExpressionMenu in the destination folder. References that already point inside the destination folder are left untouched.

| Source folder | Targets | Matching |
| --- | --- | --- |
| Specified | References pointing into the source folder | Same relative path in the destination folder. If none, a file name that is unique in the destination folder |
| Empty | References pointing outside the destination folder | A file name that is unique in the destination folder |

When a source folder is specified, references with no matching menu in the destination are listed as **unresolved**.

## Notes

- Changes can be undone with Undo (Ctrl+Z).
- Only SubMenu references are processed. ExpressionParameters and icon textures are not touched.
- Back up the destination folder before the first use.

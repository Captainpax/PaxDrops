<!--
@file AGENTS.md
@description Repository-wide contributor guide for PaxDrops covering documentation upkeep, linking conventions, tooling, and AI metadata header rules.
@editCount 2
-->

# Repository Agent Guide

Read this before editing PaxDrops.

## Core Rules

- Keep changes scoped to the requested task.
- Preserve user changes you did not make.
- Check for nested `AGENTS.md` files before editing deeper folders if any are added later.
- Keep public-facing README content concise and browseable; move deeper implementation detail into linked docs.
- If gameplay behavior, save layout, commands, build scripts, or install steps change, update the nearest relevant docs
  in the same change.
- When adding or moving major docs, keep links in this file, [README.md](README.md), and [Docs/README.md](Docs/README.md)
  current.

## Linking Rules

- Use markdown links for repo paths, for example `[README](README.md)`.
- Prefer linking the nearest relevant doc instead of dumping raw paths into prose.
- Keep a clear start-here path from the repo root into deeper docs.

## Start Here

- [Public repo README](README.md)
- [Docs index](Docs/README.md)
- [Save persistence guide](README_SAVE_SYSTEM.md)
- [Architecture note](Docs/Doc.md)
- [Project file](PaxDrops.IL2CPP.csproj)
- [Windows build helper](build_and_copy_dll.bat)
- [macOS/Linux build helper](build_and_copy_dll.sh)

## Documentation Rules

- Root README rule: keep [README.md](README.md) player-focused and repo-browseable.
- Deep-dive rule: put implementation-heavy detail in [Docs/README.md](Docs/README.md), [Docs/Doc.md](Docs/Doc.md), or
  other linked docs instead of bloating the root README.
- Cross-link rule: when you add or rename documentation, update all affected navigation sections in the same change.
- Save-system rule: if persistence paths, schema, or save lifecycle behavior changes, update
  [README_SAVE_SYSTEM.md](README_SAVE_SYSTEM.md).

## AI Metadata Headers

- AI-authored or AI-edited tracked text/code files touched after this change must include a metadata header.
- Required fields:
  - `@file`
  - `@description`
  - `@editCount`
- Increment `@editCount` each time an AI updates that file.
- Header format defaults:
  - Markdown docs: HTML comment header
  - C# files: block comment header
  - Shell/batch files: native comment style for that file type
- Scope rule: apply headers to files you touch; do not retrofit untouched files just to satisfy the rule.
- Do not add headers to generated files, binary assets, or third-party payloads.

## Tooling And Shell Usage

- Use `rg` and `rg --files` for search when available.
- Always set `workdir` when invoking shell commands.
- Prefer the provided build helpers ([build_and_copy_dll.bat](build_and_copy_dll.bat) and
  [build_and_copy_dll.sh](build_and_copy_dll.sh)) over ad-hoc copy chains when validating deploy flows.
- Keep git usage non-destructive; never discard user changes without explicit instruction.
- Avoid destructive cleanup commands unless the user explicitly asks for them.

## Verification Expectations

- For doc-only changes, verify links and referenced file names/paths.
- For gameplay or tooling changes, run the narrowest meaningful verification you can from the current environment.
- If you cannot run a full game-side validation, document what was verified locally and what remains manual.

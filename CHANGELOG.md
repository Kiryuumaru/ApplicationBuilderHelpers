# Changelog

All notable changes to this project are documented here. This file follows Keep a Changelog headings.

## [Unreleased]

### Changed

- An explicitly supplied empty string (`""`) binds as `""`, never `null`. Migrate `== null` sentinels to `string.IsNullOrEmpty`. See `docs/commands.md` and `docs/adr/0001-empty-string-preserve.md`.

## Versions

Versions are set by the owner. No released versions are recorded here yet.

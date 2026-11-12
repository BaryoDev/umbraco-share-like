# Changelog

## Unreleased

- README says one like per browser, matching the design notes.
- Share links are not rendered for content with no URL.
- `MastodonInstance` is checked at startup and must be a bare host name.
- Reading time strips tags in one pass, so a long run of `<` no longer takes quadratic time.
- Tests for the like rate limit (429 after the limit).

## 0.1.0

First public release for Umbraco 17. Share links, rate-limited likes with the count stored on the server and a per-post cookie that remembers the visitor liked it, reading time, and a most-liked dashboard.

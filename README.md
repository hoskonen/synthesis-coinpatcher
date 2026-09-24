# Coin Patcher

A Skyrim Special Edition Synthesis patcher for configuring coin names and
weights.

Currently supports vanilla gold (`Skyrim.esm | 0000000F`, `Gold001`).

Default settings:

```yaml
Vanilla Coin:
  Name: Gold
  Weight: 0.01
```

The patcher changes only the MISC record's name and weight. It uses the current
winning override as its source, preserving value, model, icons, sounds,
keywords, and all other record data. No override is created when the configured
values already match.

Add the repository to Synthesis, adjust the generated settings, and run the
patcher after other mods that edit vanilla gold. Weight cannot be negative.

# Coin Patcher

A Skyrim Special Edition Synthesis patcher for configuring supported coin
weights and the vanilla coin name.

## Supported mods

- **Skyrim** — configurable name and weight for vanilla gold (`Gold001`).
- **C.O.I.N.** — configurable weights for Ancient Nord Drakr, Ancient Falmer
  Mallari, Ayleid Mala, Dwarven Nchuark, and Gibber.

C.O.I.N. support changes coin weight only. It does not modify coin names,
values, exchange rates, scripts, MCM settings, or conversion behavior.

Default settings:

```yaml
Vanilla Coin:
  Name: Gold
  Weight: 0.01

C.O.I.N.:
  Ancient Nord Drakr Weight: 0.03
  Ancient Falmer Mallari Weight: 0.02
  Ayleid Mala Weight: 0.02
  Dwarven Nchuark Weight: 0.03
  Gibber Weight: 0.01
```

The patcher uses each current winning MISC override as its source, preserving
all fields it does not configure. No override is created when the configured
values already match. If C.O.I.N. is not installed, its categories are skipped.

Add the repository to Synthesis, adjust the generated settings, and run the
patcher after other mods that edit supported coins. Weight cannot be negative.

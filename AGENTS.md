# Agent Coding Guidelines & Style Guide

These instructions define how to write and modify code in this project. Adhere strictly to these principles.

---

## 1. Zero Over-Engineering (KISS & YAGNI)
- **Do not over-engineer under any circumstances.**
- Write the most direct, simple, and basic solution that solves the exact task requested.
- **YAGNI**: Build only what is needed right now. Do not add speculative features, interfaces, factories, or abstraction layers for future scenarios.
- Prefer flat, linear logic over deep inheritance, complex design patterns, or nested callbacks.

---

## 2. Code Simplicity & Brevity
- Keep scripts, classes, and methods short, focused, and readable.
- Avoid unnecessary boilerplate, wrapper functions, or redundant utility helpers.
- Use built-in C# and Unity features directly rather than creating custom wrappers.

---

## 3. Comments: Small & Concise
- Write small, concise, single-line comments only when the *intent* or *reason* is non-obvious.
- **Do not state the obvious**: Avoid comments that merely translate code to English (e.g., do not write `// play sound` before `audioSource.Play()`).
- Do not clutter code with bulky XML doc comment blocks (`/// <summary>`) unless explicitly requested.

---

## 4. Minimal Null Checks
- Keep null checks to the bare minimum; check only where strictly necessary.
- **Where to check**: External inputs, public entry points, or optional component references that might genuinely be missing.
- **Where NOT to check**: Internal/private helper calls and invariant states that are already guaranteed.
- Use concise C# idioms when checking (e.g., null-conditional `event?.Invoke()` or `TryGetComponent`). Avoid defensive `if (x != null)` nesting sprees.

---

## 5. Decoupled & Independent Architecture
- **Keep scripts and logic decoupled**: Avoid direct, hard dependencies between systems. Each script should remain modular, focused, and self-contained.
- **Use Events for Communication**: Use C# events/delegates or `UnityEvent`s to communicate state changes across systems rather than tightly coupling classes through direct references.

---

## 6. Unity & C# Best Practices
- **Naming Conventions**:
  - `PascalCase` for classes, methods, public fields, and properties.
  - `camelCase` for local variables and method parameters.
  - Clear, descriptive names so code remains self-documenting without comments.
- **Performance in Unity**:
  - Cache component references (`GetComponent`, `Camera.main`) in `Awake` / `OnEnable` instead of querying in `Update`.
  - Avoid memory allocations (LINQ, `new`, string concatenation) inside `Update()` and frequent per-frame loops.
  - Remove empty Unity lifecycle methods (`Update()`, `Start()`) if they do not contain logic.
  - Use `CompareTag("...")` instead of `tag == "..."`.
- **Event Handling**:
  - If subscribing to C# events or delegates via code, always unsubscribe in `OnDisable` or `OnDestroy` to prevent memory leaks.
- **Modifications & Scope**:
  - Keep changes focused strictly on the files and behavior requested.
  - Do not unsolicitedly reformat or refactor unrelated code.

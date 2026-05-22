---
name: flutter-widget-animation
description: Use when creating or modifying any Flutter UI in true_capture_app — widgets, screens, components, lists, dialogs, common widgets. Applies restrained, smooth motion with the flutter_animate package and AppMotion tokens, and avoids animation patterns that re-trigger on rebuild or cause jank.
---

# Flutter Widget Animation

Every UI element built or edited in `true_capture_app` should feel smooth and alive —
but **subtly**. Motion guides attention; it never shows off.

## When this applies
- Building or editing any widget under `lib/presentation/` (screens, `common_widgets/`, tabs).
- Creating lists, cards, dialogs, bottom sheets, empty/error/loading states.
- NOT route transitions — those already exist in `core/router/app_router.dart`
  (`AnimationType`). Reuse them; do not add page-level animation.

## Tools
- Package: `flutter_animate` — declarative `.animate()` API. Already in `pubspec.yaml`
  (if a fresh checkout lacks it, add `flutter_animate` and run `flutter pub get`).
- Tokens: `lib/core/constants/app_motion.dart` (`AppMotion`). **Always** use these
  durations/curves — never hard-code `Duration(...)` or `Curves.*` in a widget.

## The intensity rule — decent, not over
- One effect per element by default (e.g. fade, or fade + small slide). Don't stack 3+.
- Durations: `AppMotion.fast` for taps/feedback, `AppMotion.normal` for entrances,
  `AppMotion.slow` only for large/hero elements.
- Movement is small: slide ≤ ~0.15 fractional offset; scale from 0.95–1.0, never from 0.
- No looping/infinite animations in normal UI (no perpetual shimmer/pulse) unless it is
  a genuine loading affordance.
- No bounce/elastic/rotate/3D-flip unless explicitly requested.
- Motion is cosmetic — it must never delay interactivity or gate navigation.

## Core patterns

### 1. Entrance for static content (mounts once)
Stagger children so they fade + rise in:
```dart
Column(
  children: [
    HeaderWidget(),
    BodyWidget(),
    FooterWidget(),
  ].animate(interval: AppMotion.stagger)
   .fadeIn(duration: AppMotion.normal, curve: AppMotion.enter)
   .slideY(begin: 0.12, end: 0, curve: AppMotion.enter),
);
```

### 2. List / grid items
Animate `ListView.builder` items for first appearance only, and **cap the stagger** so
far-down items aren't delayed for seconds:
```dart
itemBuilder: (context, index) => Tile(...)
    .animate()
    .fadeIn(delay: AppMotion.stagger * (index.clamp(0, 8)),
            duration: AppMotion.normal)
    .slideY(begin: 0.1, end: 0, curve: AppMotion.enter),
```
Items replay their entrance each time they scroll back into view — fine for short lists;
for long feeds prefer no per-item animation, or a once-only flag.

### 3. Transient widgets (error / empty / loading)
`AppErrorView`, `EmptyWidget`, etc. mount fresh each time they appear — a plain
`.fadeIn()` is correct and won't replay unexpectedly.

### 4. State changes (show/hide, swap, resize)
Prefer Flutter built-ins driven by `AppMotion`: `AnimatedSwitcher`, `AnimatedOpacity`,
`AnimatedSize`, `AnimatedContainer`. Use `flutter_animate` for entrances, built-ins for
state-driven changes.

## Hard rules — never break state or rendering

1. **`.animate()` replays whenever its widget's State is recreated.** Rebuilt in place
   (same position/key) → keeps State, no replay. But if an ancestor structurally swaps
   the subtree, State is recreated and the entrance replays.
   - Trap in this codebase: `ScreenStateAware` wraps content in a `Stack` for
     `ScreenState.apiProgress` and not for `content`. Any `.animate()` **inside** a
     `ScreenStateAware` builder replays every time the screen toggles loading. Put
     screen-entrance animation **above** `ScreenStateAware`, or animate only widgets
     unaffected by the swap.
2. Don't put `.animate()` in a widget that `setState`s frequently for non-entrance
   reasons, unless the replay is intended.
3. Don't animate huge subtrees — animate at card/tile/leaf level. Wrapping a whole
   scrollable in effects causes jank.
4. `.animate()` makes a widget non-`const`. Keep it at the usage site; keep child
   widgets `const` where possible.
5. After adding animation: run `flutter analyze` (must be clean) and verify scrolling,
   typing and taps stay smooth.

## Reference
`lib/presentation/screens/intro/intro_screen.dart` — `_IntroPage` content uses a
staggered fade + slide entrance via `AppMotion` tokens. That is the intended style
and intensity.

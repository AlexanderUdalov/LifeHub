# Design System

## UI inventory (baseline)

- Buttons: PrimeVue `Button` with mixed APIs (`variant/outlined/rounded` and legacy `p-button-*` classes).
- Cards: domain cards (`TaskCard`, `GoalCard`, `HabitCard`, `AddictionCard`, `JournalCard`) with different radii and spacing.
- Drawers: many edit dialogs reuse almost the same styles with local class names.
- Fields: duplicated label/input/hint patterns in edit drawers.
- Swatches/chips: color swatches and select chips implemented repeatedly in different components.

## Tokens

Core tokens are defined in `src/styles/design-tokens.css`:

- Typography scale: `--ds-font-size-*`
- Spacing scale: `--ds-space-*`
- Radius scale: `--ds-radius-*`
- Structural tokens: `--ds-accent-border-width`, `--ds-drawer-max-height`

## Base primitives

- `BaseButton`: normalized button API for variants and size.
- `BaseCard`: common card shell with optional accent border.
- `BaseDrawer`: shared bottom drawer behavior and sizing.
- `BaseField`: common field block for label/hint/error.

## Usage rules

- Use `rem` units for sizing; avoid keyword font sizes (`small`, `medium`, `large`).
- Prefer `Base*` components for new UI and during migration.
- Keep inline styles only for data-driven dynamic visuals (e.g. dynamic color).
- Avoid legacy `p-button-*` classes in new code.

## PR checklist (visual QA)

- Typography matches token scale.
- Spacing follows token rhythm (`--ds-space-*`).
- Border radius uses token set (`--ds-radius-*`).
- Buttons use `BaseButton` or PrimeVue props without legacy classes.
- Drawer shell uses `BaseDrawer` and shared classes.
- Verify light and dark theme screens after changes.

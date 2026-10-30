# 7. Tailwind CSS for styling

- **Status:** Accepted
- **Date:** 2026-10-30

## Context

The web app is about to get real pages: login, the raise form, the NCR detail page, then the list,
the dashboard and the investigator and verification screens. Each needs layout (Flexbox and Grid),
responsive behaviour from 375 px upwards, visible keyboard focus, and status and severity shown in
text as well as colour (PRD §8). The PRD fixes Tailwind CSS as the styling tool (§9) and forbids
UI kits without an approved ADR. This record writes down why that choice holds, what it costs, and
how it is set up, so it can be defended rather than just followed.

The ways to style a small React app:

| | Plain CSS / CSS Modules | Tailwind CSS (utility-first) | Component library (MUI, Chakra, ...) |
| --- | --- | --- | --- |
| **Where styles live** | Separate files with class names you invent | Small single-purpose classes in the markup | Inside the library's components, adjusted through props and themes |
| **Consistency** | Up to discipline: spacing and colours drift unless you build variables | Built-in scale for spacing, colour, type and breakpoints | The library's design system |
| **Dead CSS** | Grows over time; hard to know what is safe to delete | Only classes found in the source are generated | Ships the library's styles whether used or not |
| **Specificity fights** | Common as files grow | Rare: every utility is one class of equal weight | Overriding library styles is the usual fight |
| **Learning value** | Teaches CSS directly | Still CSS underneath: each class maps to one or two properties | Hides layout and accessibility work behind components |
| **Cost** | Naming and organising files | Long `className` strings; a vocabulary to learn | A large dependency and its look; hard to leave |

## Decision

Style the web app with **Tailwind CSS v4**, through its official Vite plugin.

- **Setup:** `tailwindcss` and `@tailwindcss/vite` are dev dependencies; `vite.config.ts` adds the
  plugin and `src/index.css` holds one line, `@import 'tailwindcss'`. v4 finds class names in the
  source itself, so there is no `tailwind.config.js` and no PostCSS config.
- **Utilities in the markup, components for reuse.** Repeated styling is shared by extracting a
  React component (as `AppLayout` does), not by writing custom CSS classes with `@apply`.
- **Theme changes go in CSS.** If the app needs brand colours or fonts, they are added as design
  tokens in an `@theme` block in `src/index.css`, so there is still one place to change them.
- **No component library.** Forms, tables and badges are built from semantic HTML, so the
  accessibility rules (labels, landmarks, keyboard use) are written and understood here, not
  inherited.
- **Mobile first.** Unprefixed utilities are the 375 px layout; `sm:`, `md:` and `lg:` add to it
  for wider screens.

## Consequences

**Good**

- The production CSS contains only the classes the app uses (about 9 kB, under 3 kB gzipped, with
  the layout in place), which helps the Lighthouse ≥ 90 target.
- Spacing, colour and breakpoints come from one scale, so pages look consistent without a design
  system of our own.
- A component's markup and styling are read and changed together; deleting a component deletes its
  styles.
- Built output is a plain `.css` file from the same origin, which the API's `style-src` (via
  `default-src 'self'`) already allows once the API serves the built app (Phase 6). No inline
  styles are needed.

**Bad**

- `className` strings get long and are harder to scan than a named class. Mitigated by extracting
  components, and could be helped by the official Prettier plugin for class ordering if the repo
  adopts Prettier.
- Utility names are a vocabulary to learn on top of CSS, and they can hide the underlying
  property from someone who knows only Tailwind. The owner should be able to say which CSS each
  utility in a page produces.
- Class names must appear whole in the source. Building them from pieces (`` `bg-${colour}-600` ``)
  silently produces no CSS; variants such as status badges must map each value to a full class
  string.
- Without a component library, accessible widgets that HTML lacks (a combobox, a dialog with focus
  trapping) must be built and tested here if a later slice needs them, or a headless library
  proposed in its own ADR.

## Alternatives considered

- **Plain CSS or CSS Modules:** no new tool and the most direct CSS practice, but consistency and
  dead-style cleanup are left to discipline, and the PRD already names Tailwind.
- **A component library (MUI, Chakra, Mantine):** fastest route to finished-looking forms, but
  excluded by the PRD's no-UI-kits rule, heavier to ship, and it would hide the HTML and
  accessibility work this project is meant to show.
- **Tailwind v3 with PostCSS and `tailwind.config.js`:** still common in tutorials, but v4 is the
  current release, needs less configuration, and integrates with Vite directly.

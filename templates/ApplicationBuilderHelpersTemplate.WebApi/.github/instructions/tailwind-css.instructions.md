---
applyTo: '**'
---
# Tailwind CSS Build Pipeline

## Overview

This project uses **Tailwind CSS v4** with a build-time compilation pipeline. CSS is generated during the .NET build process and served automatically via static web assets.

## File Locations

| File | Location | Purpose |
|------|----------|---------|
| Tailwind Source | `src/Presentation.WebApp.Client/wwwroot/css/tailwind.css` | Tailwind v4 config + custom components (NOT served directly) |
| Tailwind Generated | `src/Presentation.WebApp.Client/wwwroot/css/tailwind.generated.css` | Compiled output (minified) - served to users |
| Server CSS | `src/Presentation.WebApp/wwwroot/css/app.css` | Server-side styles (served to users) |
| Package config | `src/Presentation.WebApp.Client/package.json` | npm scripts for Tailwind CLI |

## How CSS Is Served

The .NET static web assets system automatically serves files from both projects:
- `css/app.css` -> from `Presentation.WebApp/wwwroot/css/app.css`
- `css/tailwind.generated.css` -> from `Presentation.WebApp.Client/wwwroot/css/tailwind.generated.css`

**You do NOT need to manually copy CSS files between projects.** The build system handles this automatically.

## How It Works

1. **Source file** (`tailwind.css`) is a **config file** containing:
   - `@import "tailwindcss";` directive (Tailwind v4 syntax)
   - `@theme` block with custom colors (e.g., `primary` palette) and fonts
   - `@layer components` with custom utility classes (`.btn-primary`, `.card`, etc.)
   - **This file is NOT served to users** - it's only used as input for compilation

2. **Build target** in `Presentation.WebApp.Client.csproj` runs automatically:
   ```xml
   <Target Name="BuildTailwindCss" BeforeTargets="Build">
       <Exec Command="npm install" Condition="!Exists('node_modules')" />
       <Exec Command="npm run tailwind:build" />
   </Target>
   ```

3. **npm scripts** in `package.json`:
   - `npm run tailwind:build` - One-time build (minified)
   - `npm run tailwind:watch` - Watch mode for development

4. **Content scanning**: `**/*.razor`, `**/*.cshtml`, `**/*.html` files are scanned for Tailwind classes

## Tailwind v4 Configuration

Tailwind v4 uses **CSS-first configuration** instead of `tailwind.config.js`. Configuration is done in the source CSS file:

```css
@import "tailwindcss";

@theme {
    --color-primary-50: #eff6ff;
    --color-primary-100: #dbeafe;
    /* ... more primary colors ... */
    --color-primary-900: #1e3a8a;
    
    --font-sans: 'Inter', ui-sans-serif, system-ui, sans-serif;
}
```

## Component Styling Approach

**Keep component-specific styles in the component file**, not in `tailwind.css`. This keeps styles co-located with the component for easier maintenance.

### What goes in `tailwind.css`:
- `@theme` block with custom colors, fonts, dimensions (primitives)
- Blazor-specific styles (loading spinner, error UI, validation)
- Truly global/shared utility classes (rare)

### What goes in `.razor` components:
- Component-specific Tailwind utility classes (inline in the markup or as computed properties)

**Example** - Card.razor:
```csharp
private string CardClass => "bg-white border border-gray-200 rounded-lg shadow dark:bg-gray-800 dark:border-gray-700";
```

## Adding New Tailwind Classes

When adding new Tailwind utility classes to Razor components:

1. Just use the class in your `.razor` file - Tailwind CLI scans all Razor files
2. Run `dotnet build` to regenerate CSS, or use watch mode during development
3. The build target handles everything automatically

## Development Workflow

For active CSS development with hot reload:

```powershell
cd src/Presentation.WebApp.Client
npm run tailwind:watch
```

This watches for changes and regenerates `tailwind.generated.css` automatically.

## Important Notes

- **Never edit `tailwind.generated.css`** - It's auto-generated and will be overwritten
- **Never edit `tailwind.css` expecting it to be served** - It's a config file, not output
- **Keep component styles in the component** - Only put primitives in `tailwind.css`
- CSS is served via .NET static web assets - no manual copying needed
- Tailwind v4 uses `@import "tailwindcss"` and `@theme` instead of JS config
- The `@tailwindcss/cli` package is used for compilation

## Troubleshooting

| Issue | Solution |
|-------|----------|
| Classes not applying | Run `dotnet build` to regenerate CSS |
| Missing node_modules | Run `npm install` in `Presentation.WebApp.Client` |
| CSS not updating | Check that `tailwind:build` ran (see build output) |
| New class not included | Ensure the class is used in a `.razor`/`.cshtml`/`.html` file |
| Custom colors not working | Ensure `@theme` block has the color variables defined |

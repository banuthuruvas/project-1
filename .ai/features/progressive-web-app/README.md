# Progressive Web App

## Overview

Adds web app manifest, service worker, and install-prompt handling so the Vue app can be installed as a standalone app on user devices. Always included in the scaffold (not optional).

## Key Files

| Layer | Path |
|---|---|
| Manifest | `src/frontend/main/public/manifest.json` |
| Service Worker | `src/frontend/main/src/service-worker.ts` |
| Install Prompt | `src/frontend/main/src/components/InstallPromptBanner.vue` |
| Composable | `src/frontend/main/src/composables/useServiceWorker.ts` |
| Vite Config | `src/frontend/main/vite.config.ts` |

## Features

- **Web App Manifest** — app name, icons, theme colors, display mode
- **Service Worker** — asset caching, offline fallback
- **Install Banner** — prompts users on supported browsers to add app to home screen
- **Offline Support** — cache-first strategy for navigation + static assets

## Configuration

The manifest is static JSON. To customize:

1. Edit `manifest.json` for branding (app name, icons, colors)
2. Adjust cache strategy in `service-worker.ts` if needed (cache-first, network-first, stale-while-revalidate, etc.)
3. Vite automatically injects the service worker registration in dev/build

## Testing

1. Serve the app over HTTPS (or localhost:3000 for dev)
2. Open DevTools → Applications tab
3. Check "Manifest" and "Service Workers" sections
4. On supported browsers, an "Install" button should appear in the address bar

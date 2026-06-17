// Blazor JS Initializer - WASM Cache Implementation
// 
// IMPORTANT: This file MUST be named "{AssemblyName}.lib.module.js"
// If you rename the project, rename this file to match the new assembly name.
// Example: MyApp.lib.module.js for assembly "MyApp"
//
// This workaround uses the Cache API since .NET 10's built-in HTTP cache 
// with force-cache doesn't work reliably in browsers.
// NOTE: Cache Storage API requires secure context (HTTPS or localhost).

const CACHE_NAME = 'blazor-wasm-cache-v1';

// Cache Storage API is only available in secure contexts (HTTPS or localhost)
const isCacheAvailable = typeof caches !== 'undefined';

export function beforeWebStart(options) {
    // Skip custom caching if Cache API is not available
    if (!isCacheAvailable) {
        return;
    }

    options.webAssembly = options.webAssembly || {};
    options.webAssembly.loadBootResource = function(type, name, defaultUri, integrity, behavior) {
        // Only cache assembly-related resources
        if (type === 'dotnetjs' || type === 'manifest') {
            return null;
        }
        
        // Skip appsettings - always fetch fresh
        if (name.includes('appsettings')) {
            return null;
        }
        
        // Return a promise that handles caching
        return (async () => {
            try {
                const cache = await caches.open(CACHE_NAME);
                
                // Try to get from cache first
                const cachedResponse = await cache.match(defaultUri);
                if (cachedResponse) {
                    return cachedResponse;
                }
                
                // Not in cache - fetch and store
                const response = await fetch(defaultUri, { cache: 'no-store' });
                
                if (response.ok) {
                    cache.put(defaultUri, response.clone());
                }
                
                return response;
            } catch (error) {
                console.error(`[Blazor Cache] Error loading ${name}:`, error);
                return fetch(defaultUri);
            }
        })();
    };
}

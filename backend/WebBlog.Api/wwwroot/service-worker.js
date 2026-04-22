const STATIC_CACHE = "webblog-static-v8";
const API_CACHE = "webblog-api-v8";

const STATIC_ASSETS = [
    "/",
    "/index.html",
    "/post.html",
    "/login.html",
    "/register.html",
    "/create-post.html",
    "/style.css",
    "/app.js",
    "/post.js",
    "/auth.js",
    "/login.js",
    "/register.js",
    "/create-post.js",
    "/offline.html",
    "/sw-register.js",
    "/my-posts.html",
    "/my-posts.js",
    "/edit-post.html",
    "/edit-post.js",
];

self.addEventListener("install", event => {
    event.waitUntil(installStaticAssets());
    self.skipWaiting();
});

self.addEventListener("activate", event => {
    event.waitUntil(
        caches.keys().then(keys =>
            Promise.all(
                keys
                    .filter(key => key !== STATIC_CACHE && key !== API_CACHE)
                    .map(key => caches.delete(key))
            )
        )
    );

    self.clients.claim();
});

self.addEventListener("fetch", event => {
    const request = event.request;

    if (request.method !== "GET") {
        return;
    }

    const url = new URL(request.url);

    if (url.origin !== self.location.origin) {
        return;
    }

    if (url.pathname.startsWith("/api/")) {
        event.respondWith(networkFirstApi(request));
        return;
    }

    if (
        request.mode === "navigate" ||
        url.pathname.endsWith(".html") ||
        url.pathname.endsWith(".js") ||
        url.pathname.endsWith(".css")
    ) {
        event.respondWith(networkFirstStatic(request));
        return;
    }

    event.respondWith(cacheFirstStatic(request));
});

async function installStaticAssets() {
    const cache = await caches.open(STATIC_CACHE);

    for (const url of STATIC_ASSETS) {
        const request = new Request(url, { cache: "reload" });
        const response = await fetch(request);

        if (!response.ok) {
            throw new Error(`Nem sikerült cache-elni: ${url} (${response.status})`);
        }

        await cache.put(request, response.clone());
    }
}

async function networkFirstStatic(request) {
    try {
        const networkResponse = await fetch(request);
        const cache = await caches.open(STATIC_CACHE);
        cache.put(request, networkResponse.clone());
        return networkResponse;
    } catch {
        const cachedResponse = await caches.match(request);
        if (cachedResponse) {
            return cachedResponse;
        }

        const offlineResponse = await caches.match("/offline.html");
        return offlineResponse || new Response("Offline oldal nem érhető el.", { status: 503 });
    }
}

async function cacheFirstStatic(request) {
    const cachedResponse = await caches.match(request);
    if (cachedResponse) {
        return cachedResponse;
    }

    const networkResponse = await fetch(request);
    const cache = await caches.open(STATIC_CACHE);
    cache.put(request, networkResponse.clone());
    return networkResponse;
}

async function networkFirstApi(request) {
    try {
        const networkResponse = await fetch(request);
        const cache = await caches.open(API_CACHE);
        cache.put(request, networkResponse.clone());
        return networkResponse;
    } catch {
        const cachedResponse = await caches.match(request);
        return cachedResponse || new Response(
            JSON.stringify({ message: "Nincs hálózati kapcsolat és nincs cache-elt API válasz." }),
            {
                status: 503,
                headers: { "Content-Type": "application/json" }
            }
        );
    }
}
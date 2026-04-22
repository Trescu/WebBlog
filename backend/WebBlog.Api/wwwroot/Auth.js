const TOKEN_KEY = "webblog_jwt";

function saveToken(token) {
    localStorage.setItem(TOKEN_KEY, token);
}

function getToken() {
    return localStorage.getItem(TOKEN_KEY);
}

function clearToken() {
    localStorage.removeItem(TOKEN_KEY);
}

async function apiFetch(url, options = {}) {
    const token = getToken();

    const headers = new Headers(options.headers || {});
    if (token) {
        headers.set("Authorization", `Bearer ${token}`);
    }

    if (!headers.has("Content-Type") && options.body) {
        headers.set("Content-Type", "application/json");
    }

    return fetch(url, {
        ...options,
        headers
    });
}

async function getCurrentUser() {
    const token = getToken();
    if (!token) {
        return null;
    }

    try {
        const response = await apiFetch("/api/auth/me");

        if (!response.ok) {
            clearToken();
            return null;
        }

        return await response.json();
    } catch {
        return null;
    }
}

async function renderAuthNav(containerId) {
    const container = document.getElementById(containerId);
    if (!container) {
        return;
    }

    const user = await getCurrentUser();

    if (user) {
        container.innerHTML = `
            <div class="auth-nav-inner">
                <span class="auth-user">Belépve: ${escapeHtml(user.username)}</span>
                <a class="nav-link-button" href="/create-post.html">Új bejegyzés</a>
                <a class="nav-link-button nav-link-button--secondary" href="/my-posts.html">Saját bejegyzéseim</a>
                <button id="logout-button" class="nav-link-button nav-link-button--secondary" type="button">Kilépés</button>
            </div>
        `;

        const logoutButton = document.getElementById("logout-button");
        logoutButton?.addEventListener("click", () => {
            clearToken();
            window.location.href = "/";
        });
    } else {
        container.innerHTML = `
            <div class="auth-nav-inner">
                <a class="nav-link-button" href="/login.html">Belépés</a>
                <a class="nav-link-button nav-link-button--secondary" href="/register.html">Regisztráció</a>
            </div>
        `;
    }
}

function escapeHtml(value) {
    return String(value)
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll('"', "&quot;")
        .replaceAll("'", "&#039;");
}
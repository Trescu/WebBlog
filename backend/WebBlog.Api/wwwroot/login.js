const loginForm = document.getElementById("login-form");
const loginStatus = document.getElementById("login-status");

initLoginPage();

async function initLoginPage() {
    await renderAuthNav("auth-nav");

    const user = await getCurrentUser();
    if (user) {
        window.location.href = "/";
    }
}

loginForm.addEventListener("submit", async (event) => {
    event.preventDefault();

    loginStatus.textContent = "";

    const username = document.getElementById("username").value.trim();
    const password = document.getElementById("password").value;

    try {
        const response = await fetch("/api/auth/login", {
            method: "POST",
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify({
                username,
                password
            })
        });

        if (response.status === 400) {
            loginStatus.textContent = "A felhasználónév és a jelszó kötelező.";
            return;
        }

        if (response.status === 401) {
            loginStatus.textContent = "Hibás felhasználónév vagy jelszó.";
            return;
        }

        if (!response.ok) {
            throw new Error("Nem sikerült bejelentkezni.");
        }

        const data = await response.json();
        saveToken(data.token);

        window.location.href = "/";
    } catch (error) {
        loginStatus.textContent = error.message;
    }
});
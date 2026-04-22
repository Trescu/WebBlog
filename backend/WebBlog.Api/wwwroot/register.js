const registerForm = document.getElementById("register-form");
const registerStatus = document.getElementById("register-status");

initRegisterPage();

async function initRegisterPage() {
    await renderAuthNav("auth-nav");

    const user = await getCurrentUser();
    if (user) {
        window.location.href = "/";
    }
}

registerForm.addEventListener("submit", async (event) => {
    event.preventDefault();

    registerStatus.textContent = "";

    const username = document.getElementById("username").value.trim();
    const email = document.getElementById("email").value.trim();
    const password = document.getElementById("password").value;

    try {
        const response = await fetch("/api/auth/register", {
            method: "POST",
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify({
                username,
                email,
                password
            })
        });

        if (response.status === 400) {
            const errorData = await response.json();
            registerStatus.textContent = extractValidationMessage(errorData);
            return;
        }

        if (response.status === 409) {
            const errorData = await response.json();
            registerStatus.textContent = errorData.message || "A felhasználónév vagy az e-mail már foglalt.";
            return;
        }

        if (!response.ok) {
            throw new Error("Nem sikerült regisztrálni.");
        }

        const data = await response.json();
        saveToken(data.token);

        window.location.href = "/";
    } catch (error) {
        registerStatus.textContent = error.message;
    }
});

function extractValidationMessage(errorData) {
    if (!errorData || !errorData.errors) {
        return "Érvénytelen adatokat adtál meg.";
    }

    const messages = Object.values(errorData.errors).flat();
    return messages.length ? messages[0] : "Érvénytelen adatokat adtál meg.";
}
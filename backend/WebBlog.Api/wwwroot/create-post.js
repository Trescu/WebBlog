const createPostForm = document.getElementById("create-post-form");
const createPostStatus = document.getElementById("create-post-status");

initCreatePostPage();

async function initCreatePostPage() {
    await renderAuthNav("auth-nav");

    const user = await getCurrentUser();
    if (!user) {
        window.location.href = "/login.html";
    }
}

createPostForm.addEventListener("submit", async (event) => {
    event.preventDefault();

    createPostStatus.textContent = "";

    const title = document.getElementById("title").value.trim();
    const content = document.getElementById("content").value.trim();

    try {
        const response = await apiFetch("/api/posts", {
            method: "POST",
            body: JSON.stringify({
                title,
                content
            })
        });

        if (response.status === 400) {
            const errorData = await response.json();
            createPostStatus.textContent = extractValidationMessage(errorData);
            return;
        }

        if (response.status === 401) {
            createPostStatus.textContent = "A bejegyzés létrehozásához be kell jelentkezni.";
            return;
        }

        if (!response.ok) {
            throw new Error("Nem sikerült létrehozni a bejegyzést.");
        }

        const data = await response.json();
        window.location.href = `/post.html?id=${data.id}`;
    } catch (error) {
        createPostStatus.textContent = error.message;
    }
});

function extractValidationMessage(errorData) {
    if (!errorData || !errorData.errors) {
        return "Érvénytelen adatokat adtál meg.";
    }

    const messages = Object.values(errorData.errors).flat();
    return messages.length ? messages[0] : "Érvénytelen adatokat adtál meg.";
}
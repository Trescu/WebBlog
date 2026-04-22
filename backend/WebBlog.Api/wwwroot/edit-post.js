const editParams = new URLSearchParams(window.location.search);
const editPostId = editParams.get("id");

const editPostStatus = document.getElementById("edit-post-status");
const editPostForm = document.getElementById("edit-post-form");
const editPostFormStatus = document.getElementById("edit-post-form-status");

initEditPostPage();

async function initEditPostPage() {
    await renderAuthNav("auth-nav");

    const user = await getCurrentUser();
    if (!user) {
        window.location.href = "/login.html";
        return;
    }

    if (!editPostId) {
        editPostStatus.textContent = "Hiányzik a bejegyzés azonosítója.";
        return;
    }

    await loadPostForEdit();
}

editPostForm.addEventListener("submit", async (event) => {
    event.preventDefault();

    editPostFormStatus.textContent = "";

    const title = document.getElementById("title").value.trim();
    const content = document.getElementById("content").value.trim();

    try {
        const response = await apiFetch(`/api/posts/${editPostId}`, {
            method: "PUT",
            body: JSON.stringify({
                title,
                content
            })
        });

        if (response.status === 400) {
            const errorData = await response.json();
            editPostFormStatus.textContent = extractValidationMessage(errorData);
            return;
        }

        if (response.status === 401) {
            window.location.href = "/login.html";
            return;
        }

        if (response.status === 403) {
            editPostFormStatus.textContent = "Ehhez a bejegyzéshez nincs jogosultságod.";
            return;
        }

        if (response.status === 404) {
            editPostFormStatus.textContent = "A bejegyzés nem található.";
            return;
        }

        if (!response.ok) {
            throw new Error("Nem sikerült menteni a bejegyzést.");
        }

        window.location.href = `/post.html?id=${editPostId}`;
    } catch (error) {
        editPostFormStatus.textContent = error.message;
    }
});

async function loadPostForEdit() {
    try {
        const response = await fetch(`/api/posts/${editPostId}`);

        if (response.status === 404) {
            editPostStatus.textContent = "A bejegyzés nem található.";
            return;
        }

        if (!response.ok) {
            throw new Error("Nem sikerült betölteni a bejegyzést.");
        }

        const post = await response.json();

        document.getElementById("title").value = post.title;
        document.getElementById("content").value = post.content;

        editPostStatus.style.display = "none";
        editPostForm.classList.remove("hidden");
    } catch (error) {
        editPostStatus.textContent = error.message;
    }
}

function extractValidationMessage(errorData) {
    if (!errorData || !errorData.errors) {
        return "Érvénytelen adatokat adtál meg.";
    }

    const messages = Object.values(errorData.errors).flat();
    return messages.length ? messages[0] : "Érvénytelen adatokat adtál meg.";
}
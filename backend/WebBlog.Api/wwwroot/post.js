const params = new URLSearchParams(window.location.search);
const postId = params.get("id");

const postStatus = document.getElementById("post-status");
const postContainer = document.getElementById("post-container");
const postTitle = document.getElementById("post-title");
const postDate = document.getElementById("post-date");
const postContent = document.getElementById("post-content");
const commentsList = document.getElementById("comments-list");
const commentForm = document.getElementById("comment-form");
const formStatus = document.getElementById("form-status");

if (!postId) {
    postStatus.textContent = "Hiányzik a bejegyzés azonosítója az URL-ből.";
} else {
    loadPost();
}

commentForm.addEventListener("submit", async (event) => {
    event.preventDefault();

    formStatus.textContent = "";

    const authorName = document.getElementById("authorName").value.trim();
    const text = document.getElementById("text").value.trim();

    try {
        const response = await fetch(`/api/posts/${postId}/comments`, {
            method: "POST",
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify({
                authorName,
                text
            })
        });

        if (response.status === 400) {
            const errorData = await response.json();
            formStatus.textContent = extractValidationMessage(errorData);
            return;
        }

        if (response.status === 404) {
            formStatus.textContent = "A bejegyzés nem található.";
            return;
        }

        if (!response.ok) {
            throw new Error("Nem sikerült elküldeni a kommentet.");
        }

        commentForm.reset();
        formStatus.textContent = "Komment sikeresen elküldve.";
        await loadPost();
    } catch (error) {
        formStatus.textContent = error.message;
    }
});

async function loadPost() {
    try {
        const response = await fetch(`/api/posts/${postId}`);

        if (response.status === 404) {
            postStatus.textContent = "A bejegyzés nem található.";
            return;
        }

        if (!response.ok) {
            throw new Error("Nem sikerült lekérni a bejegyzést.");
        }

        const post = await response.json();

        postTitle.textContent = post.title;
        postDate.textContent = formatDate(post.createdAt);
        postContent.textContent = post.content;

        renderComments(post.comments);

        postStatus.style.display = "none";
        postContainer.classList.remove("hidden");
    } catch (error) {
        postStatus.textContent = error.message;
    }
}

function renderComments(comments) {
    commentsList.innerHTML = "";

    if (!comments.length) {
        commentsList.innerHTML = "<div class='status-box'>Még nincs komment ehhez a bejegyzéshez.</div>";
        return;
    }

    for (const comment of comments) {
        const div = document.createElement("div");
        div.className = "comment-card";

        div.innerHTML = `
            <div class="comment-author">${escapeHtml(comment.authorName)}</div>
            <div class="comment-date">${formatDate(comment.createdAt)}</div>
            <div class="comment-text">${escapeHtml(comment.text)}</div>
        `;

        commentsList.appendChild(div);
    }
}

function extractValidationMessage(errorData) {
    if (!errorData || !errorData.errors) {
        return "Érvénytelen adatokat küldtél be.";
    }

    const messages = Object.values(errorData.errors).flat();
    return messages.length ? messages[0] : "Érvénytelen adatokat küldtél be.";
}

function formatDate(value) {
    return new Date(value).toLocaleString("hu-HU");
}

function escapeHtml(value) {
    return value
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll('"', "&quot;")
        .replaceAll("'", "&#039;");
}
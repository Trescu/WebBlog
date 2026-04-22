const adminStatus = document.getElementById("admin-status");
const adminContent = document.getElementById("admin-content");
const adminPostsList = document.getElementById("admin-posts-list");
const adminCommentsList = document.getElementById("admin-comments-list");

initAdminPage();

async function initAdminPage() {
    await renderAuthNav("auth-nav");

    const user = await getCurrentUser();
    if (!user) {
        window.location.href = "/login.html";
        return;
    }

    if (user.role !== "Admin") {
        adminStatus.textContent = "Ehhez az oldalhoz nincs jogosultságod.";
        return;
    }

    adminStatus.style.display = "none";
    adminContent.classList.remove("hidden");

    await loadAdminPosts();
    await loadAdminComments();
}

async function loadAdminPosts() {
    try {
        const response = await apiFetch("/api/admin/posts");

        if (response.status === 401) {
            window.location.href = "/login.html";
            return;
        }

        if (response.status === 403) {
            adminPostsList.innerHTML = "<div class='status-box'>Ehhez nincs jogosultságod.</div>";
            return;
        }

        if (!response.ok) {
            throw new Error("Nem sikerült lekérni az admin bejegyzéslistát.");
        }

        const posts = await response.json();
        adminPostsList.innerHTML = "";

        if (!posts.length) {
            adminPostsList.innerHTML = "<div class='status-box'>Nincs egyetlen bejegyzés sem.</div>";
            return;
        }

        for (const post of posts) {
            const article = document.createElement("article");
            article.className = "post-card";

            article.innerHTML = `
                <h3>${escapeHtml(post.title)}</h3>
                <div class="post-meta">
                    ${formatDate(post.createdAt)} · Szerző: ${escapeHtml(post.authorName)} · ${post.commentCount} komment
                </div>
                <div class="post-actions">
                    <a class="post-link" href="/post.html?id=${post.id}">Megnyitás</a>
                    <a class="nav-link-button" href="/edit-post.html?id=${post.id}">Szerkesztés</a>
                    <button class="nav-link-button nav-link-button--danger" type="button" data-admin-delete-post-id="${post.id}">
                        Törlés
                    </button>
                </div>
            `;

            adminPostsList.appendChild(article);
        }

        bindAdminPostDeleteButtons();
    } catch (error) {
        adminPostsList.innerHTML = `<div class="status-box">${escapeHtml(error.message)}</div>`;
    }
}

async function loadAdminComments() {
    try {
        const response = await apiFetch("/api/admin/comments");

        if (response.status === 401) {
            window.location.href = "/login.html";
            return;
        }

        if (response.status === 403) {
            adminCommentsList.innerHTML = "<div class='status-box'>Ehhez nincs jogosultságod.</div>";
            return;
        }

        if (!response.ok) {
            throw new Error("Nem sikerült lekérni az admin kommentlistát.");
        }

        const comments = await response.json();
        adminCommentsList.innerHTML = "";

        if (!comments.length) {
            adminCommentsList.innerHTML = "<div class='status-box'>Nincs egyetlen komment sem.</div>";
            return;
        }

        for (const comment of comments) {
            const article = document.createElement("article");
            article.className = "post-card";

            article.innerHTML = `
                <h3>${escapeHtml(comment.postTitle)}</h3>
                <div class="post-meta">
                    ${formatDate(comment.createdAt)} · Kommentelő: ${escapeHtml(comment.authorName)}
                </div>
                <p class="admin-card-text">${escapeHtml(comment.text)}</p>
                <div class="post-actions">
                    <a class="post-link" href="/post.html?id=${comment.postId}">Bejegyzés megnyitása</a>
                    <button class="nav-link-button nav-link-button--danger" type="button" data-admin-delete-comment-id="${comment.id}">
                        Komment törlése
                    </button>
                </div>
            `;

            adminCommentsList.appendChild(article);
        }

        bindAdminCommentDeleteButtons();
    } catch (error) {
        adminCommentsList.innerHTML = `<div class="status-box">${escapeHtml(error.message)}</div>`;
    }
}

function bindAdminPostDeleteButtons() {
    document.querySelectorAll("[data-admin-delete-post-id]").forEach(button => {
        button.addEventListener("click", async () => {
            const postId = button.getAttribute("data-admin-delete-post-id");
            if (!postId) {
                return;
            }

            const confirmed = window.confirm("Biztosan törölni szeretnéd ezt a bejegyzést?");
            if (!confirmed) {
                return;
            }

            try {
                const response = await apiFetch(`/api/posts/${postId}`, {
                    method: "DELETE"
                });

                if (response.status === 404) {
                    alert("A bejegyzés már nem található.");
                } else if (!response.ok) {
                    throw new Error("Nem sikerült törölni a bejegyzést.");
                }

                await loadAdminPosts();
                await loadAdminComments();
            } catch (error) {
                alert(error.message);
            }
        });
    });
}

function bindAdminCommentDeleteButtons() {
    document.querySelectorAll("[data-admin-delete-comment-id]").forEach(button => {
        button.addEventListener("click", async () => {
            const commentId = button.getAttribute("data-admin-delete-comment-id");
            if (!commentId) {
                return;
            }

            const confirmed = window.confirm("Biztosan törölni szeretnéd ezt a kommentet?");
            if (!confirmed) {
                return;
            }

            try {
                const response = await apiFetch(`/api/admin/comments/${commentId}`, {
                    method: "DELETE"
                });

                if (response.status === 404) {
                    alert("A komment már nem található.");
                } else if (!response.ok) {
                    throw new Error("Nem sikerült törölni a kommentet.");
                }

                await loadAdminComments();
                await loadAdminPosts();
            } catch (error) {
                alert(error.message);
            }
        });
    });
}

function formatDate(value) {
    return new Date(value).toLocaleString("hu-HU");
}
const myPostsStatus = document.getElementById("my-posts-status");
const myPostsList = document.getElementById("my-posts-list");

initMyPostsPage();

async function initMyPostsPage() {
    await renderAuthNav("auth-nav");

    const user = await getCurrentUser();
    if (!user) {
        window.location.href = "/login.html";
        return;
    }

    await loadMyPosts();
}

async function loadMyPosts() {
    try {
        const response = await apiFetch("/api/my/posts");

        if (response.status === 401) {
            window.location.href = "/login.html";
            return;
        }

        if (!response.ok) {
            throw new Error("Nem sikerült lekérni a saját bejegyzéseket.");
        }

        const posts = await response.json();

        myPostsStatus.style.display = "none";
        myPostsList.innerHTML = "";

        if (!posts.length) {
            myPostsList.innerHTML = "<div class='status-box'>Még nincs saját bejegyzésed.</div>";
            return;
        }

        for (const post of posts) {
            const article = document.createElement("article");
            article.className = "post-card";

            article.innerHTML = `
                <h3>${escapeHtml(post.title)}</h3>
                <div class="post-meta">
                    ${formatDate(post.createdAt)} · ${post.commentCount} komment
                </div>
                <div class="post-actions">
                    <a class="post-link" href="/post.html?id=${post.id}">Megnyitás</a>
                    <a class="nav-link-button" href="/edit-post.html?id=${post.id}">Szerkesztés</a>
                    <button class="nav-link-button nav-link-button--danger" type="button" data-delete-id="${post.id}">
                        Törlés
                    </button>
                </div>
            `;

            myPostsList.appendChild(article);
        }

        bindDeleteButtons();
    } catch (error) {
        myPostsStatus.textContent = error.message;
    }
}

function bindDeleteButtons() {
    document.querySelectorAll("[data-delete-id]").forEach(button => {
        button.addEventListener("click", async () => {
            const postId = button.getAttribute("data-delete-id");
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

                if (response.status === 401) {
                    window.location.href = "/login.html";
                    return;
                }

                if (response.status === 403) {
                    alert("Ehhez a bejegyzéshez nincs jogosultságod.");
                    return;
                }

                if (response.status === 404) {
                    alert("A bejegyzés már nem található.");
                    await loadMyPosts();
                    return;
                }

                if (!response.ok) {
                    throw new Error("Nem sikerült törölni a bejegyzést.");
                }

                await loadMyPosts();
            } catch (error) {
                alert(error.message);
            }
        });
    });
}

function formatDate(value) {
    return new Date(value).toLocaleString("hu-HU");
}
initIndexPage();

async function initIndexPage() {
    await renderAuthNav("auth-nav");
    await loadPosts();
}

async function loadPosts() {
    const statusBox = document.getElementById("posts-status");
    const postsList = document.getElementById("posts-list");

    try {
        const response = await fetch("/api/posts");

        if (!response.ok) {
            throw new Error("Nem sikerült lekérni a bejegyzéseket.");
        }

        const posts = await response.json();

        statusBox.style.display = "none";
        postsList.innerHTML = "";

        if (!posts.length) {
            postsList.innerHTML = "<div class='status-box'>Még nincs egyetlen bejegyzés sem.</div>";
            return;
        }

        for (const post of posts) {
            const article = document.createElement("article");
            article.className = "post-card";

            article.innerHTML = `
                <h3>${escapeHtml(post.title)}</h3>
                <div class="post-meta">
                    ${formatDate(post.createdAt)} · ${post.commentCount} komment · Szerző: ${escapeHtml(post.authorName)}
                </div>
                <p>${escapeHtml(post.preview)}</p>
                <a class="post-link" href="post.html?id=${post.id}">Megnyitás</a>
            `;

            postsList.appendChild(article);
        }
    } catch (error) {
        statusBox.textContent = error.message;
    }
}

function formatDate(value) {
    return new Date(value).toLocaleString("hu-HU");
}
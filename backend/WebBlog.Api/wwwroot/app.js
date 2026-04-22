const postsStatus = document.getElementById("posts-status");
const postsList = document.getElementById("posts-list");
const filterForm = document.getElementById("posts-filter-form");
const searchInput = document.getElementById("search");
const authorFilter = document.getElementById("author-filter");
const sortFilter = document.getElementById("sort-filter");
const resetFiltersButton = document.getElementById("reset-filters-button");

initIndexPage();

async function initIndexPage() {
    await renderAuthNav("auth-nav");
    await loadAuthors();
    bindFilterEvents();
    await loadPosts();
}

function bindFilterEvents() {
    filterForm.addEventListener("submit", async (event) => {
        event.preventDefault();
        await loadPosts();
    });

    authorFilter.addEventListener("change", async () => {
        await loadPosts();
    });

    sortFilter.addEventListener("change", async () => {
        await loadPosts();
    });

    resetFiltersButton.addEventListener("click", async () => {
        searchInput.value = "";
        authorFilter.value = "";
        sortFilter.value = "newest";
        await loadPosts();
    });
}

async function loadAuthors() {
    try {
        const response = await fetch("/api/posts/authors");

        if (!response.ok) {
            throw new Error("Nem sikerült lekérni a szerzőlistát.");
        }

        const authors = await response.json();

        authorFilter.innerHTML = `<option value="">Összes szerző</option>`;

        for (const author of authors) {
            const option = document.createElement("option");
            option.value = author;
            option.textContent = author;
            authorFilter.appendChild(option);
        }
    } catch (error) {
        console.error(error);
    }
}

async function loadPosts() {
    postsStatus.style.display = "block";
    postsStatus.textContent = "Betöltés...";
    postsList.innerHTML = "";

    try {
        const params = new URLSearchParams();

        const searchValue = searchInput.value.trim();
        const authorValue = authorFilter.value;
        const sortValue = sortFilter.value;

        if (searchValue) {
            params.set("search", searchValue);
        }

        if (authorValue) {
            params.set("author", authorValue);
        }

        if (sortValue) {
            params.set("sort", sortValue);
        }

        const url = params.toString()
            ? `/api/posts?${params.toString()}`
            : "/api/posts";

        const response = await fetch(url);

        if (!response.ok) {
            throw new Error("Nem sikerült lekérni a bejegyzéseket.");
        }

        const posts = await response.json();

        postsStatus.style.display = "none";
        postsList.innerHTML = "";

        if (!posts.length) {
            postsList.innerHTML = "<div class='status-box'>Nincs találat a megadott szűrőkre.</div>";
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
        postsStatus.textContent = error.message;
    }
}

function formatDate(value) {
    return new Date(value).toLocaleString("hu-HU");
}
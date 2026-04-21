if ("serviceWorker" in navigator) {
    window.addEventListener("load", async () => {
        try {
            await navigator.serviceWorker.register("/service-worker.js");
            console.log("Service worker sikeresen regisztrálva.");
        } catch (error) {
            console.error("A service worker regisztráció sikertelen:", error);
        }
    });
}
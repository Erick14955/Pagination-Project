window.thryvTheme = {
    allowedThemes: ["light", "dark", "liquid glass", "dark glass"],

    applyTheme: function (theme) {
        if (!theme || !this.allowedThemes.includes(theme)) {
            theme = localStorage.getItem("theme") || "light";
        }

        if (!this.allowedThemes.includes(theme)) {
            theme = "light";
        }

        localStorage.setItem("theme", theme);
        document.documentElement.setAttribute("data-theme", theme);

        document.body?.classList.remove(
            "thryv-light",
            "thryv-dark",
            "thryv-glass",
            "thryv-dark-glass"
        );

        if (document.body) {
            if (theme === "dark") {
                document.body.classList.add("thryv-dark");
            } else if (theme === "liquid glass") {
                document.body.classList.add("thryv-glass");
            } else if (theme === "dark glass") {
                document.body.classList.add("thryv-dark-glass");
            } else {
                document.body.classList.add("thryv-light");
            }
        }
    },

    applySavedTheme: function () {
        const theme = localStorage.getItem("theme") || "light";
        this.applyTheme(theme);
    }
};

(function () {
    if (window.thryvTheme && typeof window.thryvTheme.applySavedTheme === "function") {
        window.thryvTheme.applySavedTheme();
    }
})();
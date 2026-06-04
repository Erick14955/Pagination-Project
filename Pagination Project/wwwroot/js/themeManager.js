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
    },

    applySavedTheme: function () {
        const theme = localStorage.getItem("theme") || "light";
        this.applyTheme(theme);
    }
};
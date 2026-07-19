window.thryvTheme = {
    allowedThemes: [
        "light",
        "dark",
        "liquid glass",
        "dark glass"
    ],

    normalizeTheme: function (theme) {
        return this.allowedThemes.includes(theme)
            ? theme
            : "light";
    },

    getSavedTheme: function () {
        const savedTheme = localStorage.getItem("theme");
        return this.normalizeTheme(savedTheme);
    },

    applyTheme: function (theme) {
        const selectedTheme = this.normalizeTheme(
            theme || this.getSavedTheme()
        );

        localStorage.setItem("theme", selectedTheme);

        document.documentElement.setAttribute(
            "data-theme",
            selectedTheme
        );

        if (!document.body) {
            return selectedTheme;
        }

        document.body.classList.remove(
            "thryv-light",
            "thryv-dark",
            "thryv-glass",
            "thryv-dark-glass"
        );

        switch (selectedTheme) {
            case "dark":
                document.body.classList.add("thryv-dark");
                break;

            case "liquid glass":
                document.body.classList.add("thryv-glass");
                break;

            case "dark glass":
                document.body.classList.add("thryv-dark-glass");
                break;

            default:
                document.body.classList.add("thryv-light");
                break;
        }

        return selectedTheme;
    },

    applySavedTheme: function () {
        return this.applyTheme(this.getSavedTheme());
    }
};

window.thryvLogin = {
    startSubmitting: function (form) {
        if (!form) {
            return true;
        }

        if (!form.checkValidity()) {
            form.reportValidity();
            return false;
        }

        if (form.classList.contains("is-submitting")) {
            return false;
        }

        form.classList.add("is-submitting");
        form.setAttribute("aria-busy", "true");

        const button = form.querySelector("#thryv-login-submit");

        if (button) {
            button.classList.add("is-loading");
            button.setAttribute("aria-busy", "true");
            button.disabled = true;
        }

        return true;
    },

    resetSubmitting: function () {
        const form = document.querySelector(
            'form[action="/account/login"]'
        );

        if (!form) {
            return;
        }

        form.classList.remove("is-submitting");
        form.removeAttribute("aria-busy");

        const button = form.querySelector("#thryv-login-submit");

        if (button) {
            button.classList.remove("is-loading");
            button.removeAttribute("aria-busy");
            button.disabled = false;
        }
    }
};

(function initializeThemeManager() {
    function applyTheme() {
        try {
            window.thryvTheme.applySavedTheme();
        }
        catch (error) {
            console.error("Theme initialization failed:", error);
        }
    }

    applyTheme();

    if (document.readyState === "loading") {
        document.addEventListener(
            "DOMContentLoaded",
            applyTheme,
            { once: true }
        );
    }

    window.addEventListener("pageshow", function () {
        applyTheme();
        window.thryvLogin.resetSubmitting();
    });
})();

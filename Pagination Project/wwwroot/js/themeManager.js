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

        const savedTheme =
            localStorage.getItem("theme");

        return this.normalizeTheme(
            savedTheme
        );
    },


    applyTheme: function (theme) {

        const selectedTheme =
            this.normalizeTheme(
                theme ||
                this.getSavedTheme()
            );


        localStorage.setItem(
            "theme",
            selectedTheme
        );


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

                document.body.classList.add(
                    "thryv-dark"
                );

                break;


            case "liquid glass":

                document.body.classList.add(
                    "thryv-glass"
                );

                break;


            case "dark glass":

                document.body.classList.add(
                    "thryv-dark-glass"
                );

                break;


            default:

                document.body.classList.add(
                    "thryv-light"
                );

                break;
        }


        return selectedTheme;
    },


    applySavedTheme: function () {

        return this.applyTheme(
            this.getSavedTheme()
        );
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


        if (
            form.classList.contains(
                "is-submitting"
            )
        ) {

            return false;
        }


        form.classList.add(
            "is-submitting"
        );


        form.setAttribute(
            "aria-busy",
            "true"
        );


        const button =
            form.querySelector(
                "#thryv-login-submit"
            );


        if (button) {

            button.classList.add(
                "is-loading"
            );


            button.setAttribute(
                "aria-busy",
                "true"
            );


            button.disabled = true;
        }


        return true;
    },


    resetSubmitting: function () {

        const form =
            document.querySelector(
                'form[action="/account/login"]'
            );


        if (!form) {

            return;
        }


        form.classList.remove(
            "is-submitting"
        );


        form.removeAttribute(
            "aria-busy"
        );


        const button =
            form.querySelector(
                "#thryv-login-submit"
            );


        if (button) {

            button.classList.remove(
                "is-loading"
            );


            button.removeAttribute(
                "aria-busy"
            );


            button.disabled = false;
        }
    },


    togglePassword: function (button) {

        if (!button) {

            return;
        }


        const wrapper =
            button.closest(
                ".thryv-input-wrapper"
            );


        if (!wrapper) {

            return;
        }


        const input =
            wrapper.querySelector(
                ".thryv-password-input"
            );


        if (!input) {

            return;
        }


        const isHidden =
            input.type === "password";


        input.type =
            isHidden
                ? "text"
                : "password";


        button.classList.toggle(
            "is-visible",
            isHidden
        );


        button.setAttribute(
            "aria-label",
            isHidden
                ? "Hide password"
                : "Show password"
        );


        button.setAttribute(
            "aria-pressed",
            isHidden
                ? "true"
                : "false"
        );


        input.focus({
            preventScroll: true
        });


        try {

            const length =
                input.value.length;


            input.setSelectionRange(
                length,
                length
            );
        }
        catch {
        }
    },


    resetPasswordVisibility: function () {

        const passwordInputs =
            document.querySelectorAll(
                ".thryv-password-input"
            );


        passwordInputs.forEach(
            function (input) {

                input.type =
                    "password";
            }
        );


        const buttons =
            document.querySelectorAll(
                ".thryv-password-toggle"
            );


        buttons.forEach(
            function (button) {

                button.classList.remove(
                    "is-visible"
                );


                button.setAttribute(
                    "aria-label",
                    "Show password"
                );


                button.setAttribute(
                    "aria-pressed",
                    "false"
                );
            }
        );
    }
};



(function initializeThryv() {

    function applyTheme() {

        try {

            window.thryvTheme
                .applySavedTheme();
        }
        catch (error) {

            console.error(
                "Theme initialization failed:",
                error
            );
        }
    }


    function initializeLogin() {

        try {

            window.thryvLogin
                .resetSubmitting();


            window.thryvLogin
                .resetPasswordVisibility();
        }
        catch (error) {

            console.error(
                "Login initialization failed:",
                error
            );
        }
    }


    applyTheme();


    if (
        document.readyState ===
        "loading"
    ) {

        document.addEventListener(
            "DOMContentLoaded",
            function () {

                applyTheme();

                initializeLogin();
            },
            {
                once: true
            }
        );
    }
    else {

        initializeLogin();
    }


    window.addEventListener(
        "pageshow",
        function () {

            applyTheme();

            initializeLogin();
        }
    );

})();
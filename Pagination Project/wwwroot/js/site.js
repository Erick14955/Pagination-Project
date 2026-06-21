window.thryvLayout = {
    isCompact: function () {
        return window.matchMedia("(max-width: 992px)").matches;
    }
};

window.thryvModalScroll = {
    open: function () {
        document.documentElement.classList.add("thryv-modal-open");
        document.body.classList.add("thryv-modal-open");
    },

    close: function () {
        document.documentElement.classList.remove("thryv-modal-open");
        document.body.classList.remove("thryv-modal-open");
    }
};

window.thryvModalPortal = {
    open: function (elementId) {
        const modal = document.getElementById(elementId);

        if (!modal) {
            return;
        }

        if (!modal.__thryvPlaceholder) {
            const placeholder = document.createComment("thryv-modal-placeholder");
            modal.parentNode.insertBefore(placeholder, modal);
            modal.__thryvPlaceholder = placeholder;
        }

        document.body.appendChild(modal);

        document.documentElement.classList.add("thryv-modal-open");
        document.body.classList.add("thryv-modal-open");
    },

    close: function (elementId) {
        const modal = document.getElementById(elementId);

        if (modal && modal.__thryvPlaceholder && modal.__thryvPlaceholder.parentNode) {
            modal.__thryvPlaceholder.parentNode.insertBefore(modal, modal.__thryvPlaceholder);
            modal.__thryvPlaceholder.remove();
            modal.__thryvPlaceholder = null;
        }

        document.documentElement.classList.remove("thryv-modal-open");
        document.body.classList.remove("thryv-modal-open");
    },

    forceClose: function () {
        document.documentElement.classList.remove("thryv-modal-open");
        document.body.classList.remove("thryv-modal-open");
    }
};
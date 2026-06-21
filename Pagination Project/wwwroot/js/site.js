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
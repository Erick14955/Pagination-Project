window.assignmentImportModal = {
    show: function (data) {
        const existing = document.getElementById("assignment-import-modal-root");

        if (existing) {
            existing.remove();
        }

        const type = data.type || "success";
        const isError = type === "error";

        const errors = Array.isArray(data.errors) ? data.errors : [];
        const warnings = Array.isArray(data.warnings) ? data.warnings : [];

        const root = document.createElement("div");
        root.id = "assignment-import-modal-root";

        root.innerHTML = `
            <div class="assignment-modal-backdrop">
                <div class="assignment-modal ${isError ? "assignment-modal-error" : "assignment-modal-success"}">
                    <div class="assignment-modal-header">
                        <div>
                            <h2>${escapeHtml(data.title || "XLSX Import")}</h2>
                            <p>${escapeHtml(data.summary || "")}</p>
                        </div>

                        <button type="button" class="assignment-modal-close" onclick="window.assignmentImportModal.close()">
                            ✕
                        </button>
                    </div>

                    <div class="assignment-modal-stats">
                        <div>
                            <span>Books Created</span>
                            <strong>${data.booksCreated ?? 0}</strong>
                        </div>

                        <div>
                            <span>Assignments Created</span>
                            <strong>${data.assignmentsCreated ?? 0}</strong>
                        </div>

                        <div>
                            <span>Duplicated</span>
                            <strong>${data.duplicated ?? 0}</strong>
                        </div>

                        <div>
                            <span>Skipped Rows</span>
                            <strong>${data.skippedRows ?? 0}</strong>
                        </div>
                    </div>

                    ${errors.length > 0 ? `
                        <div class="assignment-modal-section assignment-modal-errors">
                            <h3>Errors found</h3>
                            <ul>
                                ${errors.slice(0, 150).map(x => `<li>${escapeHtml(x)}</li>`).join("")}
                            </ul>
                        </div>
                    ` : ""}

                    ${warnings.length > 0 ? `
                        <div class="assignment-modal-section assignment-modal-warnings">
                            <h3>Warnings</h3>
                            <ul>
                                ${warnings.slice(0, 150).map(x => `<li>${escapeHtml(x)}</li>`).join("")}
                            </ul>
                        </div>
                    ` : ""}

                    ${errors.length === 0 && warnings.length === 0 ? `
                        <div class="assignment-modal-section assignment-modal-ok">
                            <strong>No errors were found.</strong>
                            <span>The XLSX file was imported successfully.</span>
                        </div>
                    ` : ""}

                    <div class="assignment-modal-actions">
                        <button type="button" onclick="window.assignmentImportModal.close()">
                            OK
                        </button>
                    </div>
                </div>
            </div>
        `;

        document.body.appendChild(root);
    },

    close: function () {
        const existing = document.getElementById("assignment-import-modal-root");

        if (existing) {
            existing.remove();
        }
    }
};

function escapeHtml(value) {
    if (value === null || value === undefined) {
        return "";
    }

    return String(value)
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll('"', "&quot;")
        .replaceAll("'", "&#039;");
}
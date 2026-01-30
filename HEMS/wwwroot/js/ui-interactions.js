document.addEventListener('DOMContentLoaded', function () {
    const confirmModal = document.getElementById('reusableConfirmModal');
    if (!confirmModal) return;

    // Standard Modal Setup for Buttons
    confirmModal.addEventListener('show.bs.modal', function (event) {
        const button = event.relatedTarget;
        if (!button) return; // Ignore if triggered manually via script

        const title = button.getAttribute('data-title');
        const message = button.getAttribute('data-message');
        const target = button.getAttribute('data-target');
        const type = button.getAttribute('data-type');

        updateModalContent(title, message, target, type);
    });

    // Helper to update modal content and button behavior
    window.updateModalContent = function (title, message, target, type) {
        document.getElementById('confirmTitle').innerText = title;
        document.getElementById('confirmMessage').innerText = message;

        const confirmBtn = document.getElementById('confirmActionButton');
        const newConfirmBtn = confirmBtn.cloneNode(true);
        confirmBtn.parentNode.replaceChild(newConfirmBtn, confirmBtn);

        newConfirmBtn.addEventListener('click', function () {
            if (type === 'form') {
                const form = document.getElementById(target);
                if (form) form.submit();
            } else if (type === 'link') {
                window.location.href = target;
            } else {
                // Just close for warnings
                const modalInstance = bootstrap.Modal.getInstance(confirmModal);
                modalInstance.hide();
            }
        });
    }

    // BROWSER/TAB SWITCH DETECTION
    window.addEventListener('blur', function () {
        if (document.getElementById('timer')) {
            const modalElement = document.getElementById('reusableConfirmModal');
            const modalInstance = new bootstrap.Modal(modalElement);

            // Set Warning Content
            updateModalContent(
                "⚠️ Security Warning",
                "You have switched tabs or windows. This action has been logged. Please focus on your exam.",
                null,
                "warning"
            );

            modalInstance.show();
        }
    });
});

/**
 * User-Specific Resume Logic
 */
function handleResumeLogic(examId, currentIndex, userId) {
    if (!userId) return;
    localStorage.setItem(`resume_exam_${userId}_${examId}`, currentIndex);
}
/**
 * Coordinator Dashboard Logic
 * Handles Exam Delete Modals and UI Initializations
 */
document.addEventListener("DOMContentLoaded", function () {

    // --- Delete Exam Modal Logic ---
    const deleteModalElement = document.getElementById('deleteExamModal');

    // Only initialize if the modal exists on the current page
    if (deleteModalElement) {
        const deleteModal = new bootstrap.Modal(deleteModalElement);
        const titleText = document.getElementById('examTitleText');
        const idInput = document.getElementById('examIdInput');

        // Delegate click event to the table body (better for performance)
        document.addEventListener('click', function (event) {
            const btn = event.target.closest('.js-delete-exam');

            if (btn) {
                const examId = btn.getAttribute('data-id');
                const examTitle = btn.getAttribute('data-title');

                // Populate Modal
                if (titleText) titleText.innerText = examTitle;
                if (idInput) idInput.value = examId;

                // Show Modal
                deleteModal.show();
            }
        });
    }

    // --- Optional: Auto-hide Success Alerts ---
    // If you have standard bootstrap alerts, this fades them after 5 seconds
    const alerts = document.querySelectorAll('.alert-dismissible');
    alerts.forEach(alert => {
        setTimeout(() => {
            const bsAlert = new bootstrap.Alert(alert);
            bsAlert.close();
        }, 5000);
    });
});
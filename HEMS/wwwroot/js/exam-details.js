document.addEventListener("DOMContentLoaded", function () {
    // 1. Find all delete buttons
    const deleteButtons = document.querySelectorAll('.js-delete-question-btn');

    // 2. Setup the modal elements
    const deleteModal = new bootstrap.Modal(document.getElementById('deleteModal'));
    const modalQuestionIdInput = document.getElementById('modalQuestionId');
    const modalQuestionTextDisplay = document.getElementById('modalQuestionText');

    // 3. Attach click event to each button
    deleteButtons.forEach(button => {
        button.addEventListener('click', function () {
            // Get data from button attributes
            const questionId = this.getAttribute('data-id');
            const questionText = this.getAttribute('data-text');

            // Set values into the modal
            modalQuestionIdInput.value = questionId;
            modalQuestionTextDisplay.textContent = questionText;

            // Show the modal
            deleteModal.show();
        });
    });
});
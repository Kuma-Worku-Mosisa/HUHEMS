/**
 * Persistent Timer Logic
 */
function initPersistentTimer(examId, durationMinutes) {
    const storageKey = `exam_end_time_${examId}`;
    let endTime = localStorage.getItem(storageKey);

    if (!endTime) {
        // If no end time exists, set it from now
        endTime = new Date().getTime() + (durationMinutes * 60 * 1000);
        localStorage.setItem(storageKey, endTime);
    }

    const timerInterval = setInterval(function () {
        const now = new Date().getTime();
        const distance = endTime - now;

        if (distance < 0) {
            clearInterval(timerInterval);
            localStorage.removeItem(storageKey);
            alert("Time is up!");
            document.getElementById('examForm').submit();
            return;
        }

        const hours = Math.floor((distance % (1000 * 60 * 60 * 24)) / (1000 * 60 * 60));
        const minutes = Math.floor((distance % (1000 * 60 * 60)) / (1000 * 60));
        const seconds = Math.floor((distance % (1000 * 60)) / 1000);

        document.getElementById('timer').innerHTML =
            `${hours}:${minutes < 10 ? '0' : ''}${minutes}:${seconds < 10 ? '0' : ''}${seconds}`;
    }, 1000);
}

/**
 * Flagging and Interaction Logic
 */
function initQuestionInteractions(index, examId, qId) {
    const navBox = document.getElementById(`nav-box-${index}`);
    const flagBtn = document.getElementById('flagToggle');
    const flaggedInput = document.getElementById('flaggedInput');

    // Handle Radio Selection
    document.querySelectorAll('.option-input').forEach(opt => {
        opt.addEventListener('change', () => {
            navBox.classList.add('answered');
        });
    });

    // Handle Flag Toggle via AJAX
    if (flagBtn) {
        flagBtn.addEventListener('click', function () {
            const isFlagged = !navBox.classList.contains('flagged');

            // Toggle UI
            navBox.classList.toggle('flagged');
            flaggedInput.value = isFlagged;
            this.innerHTML = isFlagged ?
                '<i class="bi bi-flag-fill text-danger"></i> Remove flag' :
                '<i class="bi bi-flag"></i> Flag question';

            // Save to server without reload
            fetch('/Student/ToggleFlag', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ examId: examId, questionId: qId, isFlagged: isFlagged })
            });
        });
    }
}

function submitExamForm() {
    document.getElementById('examForm').submit();
}
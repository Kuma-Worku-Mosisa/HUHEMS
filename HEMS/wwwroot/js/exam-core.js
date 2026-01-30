function initPersistentTimer(examId, durationMinutes) {
    const storageKey = `exam_end_time_${examId}`;
    let endTime = localStorage.getItem(storageKey);

    if (!endTime) {
        endTime = new Date().getTime() + (durationMinutes * 60 * 1000);
        localStorage.setItem(storageKey, endTime);
    }

    const timerInterval = setInterval(function () {
        const now = new Date().getTime();
        const distance = endTime - now;

        if (distance < 0) {
            clearInterval(timerInterval);
            localStorage.removeItem(storageKey);
            alert("Time is up! Submitting your exam.");
            document.getElementById('examForm').submit();
            return;
        }

        const h = Math.floor(distance / (3600000));
        const m = Math.floor((distance % 3600000) / 60000);
        const s = Math.floor((distance % 60000) / 1000);

        document.getElementById('timer').innerHTML =
            `${h}:${m.toString().padStart(2, '0')}:${s.toString().padStart(2, '0')}`;
    }, 1000);
}

function initSecurity(examId) {
    // 1. Disable Right Click
    document.addEventListener('contextmenu', e => e.preventDefault());

    // 2. Disable Keyboard Shortcuts (Copy, Print, Save, View Source)
    document.addEventListener('keydown', e => {
        if (e.ctrlKey && ['c', 'v', 'p', 's', 'u'].includes(e.key.toLowerCase())) {
            e.preventDefault();
            alert("Security Alert: This action is disabled.");
        }
        if (e.key === "PrintScreen") {
            navigator.clipboard.writeText("");
            alert("Screenshots are prohibited.");
        }
    });

    // 3. Tab Switch Detection
    let switchCount = 0;
    document.addEventListener("visibilitychange", function () {
        if (document.hidden) {
            switchCount++;
            if (switchCount >= 3) {
                alert("Final Warning: You have switched tabs too many times. Submitting now.");
                localStorage.removeItem(`exam_end_time_${examId}`);
                document.getElementById('examForm').submit();
            } else {
                alert(`WARNING: Do not leave this page! (${switchCount}/3)`);
            }
        }
    });
}

function initQuestionInteractions(index, examId, qId) {
    const navBox = document.getElementById(`nav-box-${index}`);

    // Auto-update Nav Grid on Answer
    document.querySelectorAll('.option-input').forEach(opt => {
        opt.addEventListener('change', () => {
            if (navBox) navBox.classList.add('answered');
        });
    });

    // Flagging via AJAX
    $("#flagToggle").on("click", function () {
        const isFlagged = !$(navBox).hasClass('flagged');
        $(navBox).toggleClass('flagged');
        $("#flaggedInput").val(isFlagged);

        $(this).toggleClass('btn-danger btn-outline-secondary');
        $(this).find('i').toggleClass('bi-flag bi-flag-fill');
        $(this).find('span').text(isFlagged ? "Unflag" : "Flag");

        fetch('/Student/ToggleFlag', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ examId: examId, questionId: qId, isFlagged: isFlagged })
        });
    });
}

function submitExamForm() {
    // Before submission, if it's the final question, we clear the timer
    // (Actual reset logic happens on the server after processing)
    document.getElementById('examForm').submit();
}
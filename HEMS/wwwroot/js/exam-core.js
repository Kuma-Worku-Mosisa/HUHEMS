function initPersistentTimer(examId, durationMinutes) {
    const storageKey = `exam_end_time_${examId}`;
    let endTime = localStorage.getItem(storageKey);

    if (!endTime) {
        endTime = Date.now() + durationMinutes * 60 * 1000;
        localStorage.setItem(storageKey, endTime);
        // Clear old notification markers
        Object.keys(localStorage).forEach(k => {
            if (k.startsWith(`exam_notify_15_${examId}_`) ||
                k.startsWith(`exam_notify_5_${examId}_`)) {
                localStorage.removeItem(k);
            }
        });
    }

    endTime = parseInt(endTime, 10);

    function updateTimer() {
        const distance = endTime - Date.now();

        if (distance <= 0) {
            clearInterval(timerInterval);
            localStorage.removeItem(storageKey);
            document.getElementById("timer").innerText = "EXPIRED";
            document.getElementById("examForm").submit();
            return;
        }

        const h = Math.floor(distance / 3600000).toString().padStart(2, '0');
        const m = Math.floor((distance % 3600000) / 60000).toString().padStart(2, '0');
        const s = Math.floor((distance % 60000) / 1000).toString().padStart(2, '0');

        document.getElementById("timer").innerText = `${h}:${m}:${s}`;
        if (distance < 60000) document.getElementById("timer").classList.add("text-danger");
    }

    const timerInterval = setInterval(updateTimer, 1000);
    updateTimer();
}

/* =========================
   TIMER VISIBILITY
========================= */
function initTimerVisibility(examId) {
    const visKey = `exam_timer_visible_${examId}`;
    let visible = localStorage.getItem(visKey);
    visible = visible === null ? true : visible === '1';

    const timer = document.getElementById("timer");
    const pulse = document.querySelector(".pulse-timer");
    const eyeIcon = document.getElementById("eyeIcon");

    function apply() {
        if (timer && pulse && eyeIcon) {
            timer.style.display = visible ? "" : "none";
            pulse.style.display = visible ? "" : "none";
            eyeIcon.className = visible ? "bi bi-eye-fill" : "bi bi-eye-slash";
        }
    }

    const toggleBtn = document.getElementById("toggleTimerVisibility");
    if (toggleBtn) {
        toggleBtn.addEventListener("click", () => {
            visible = !visible;
            localStorage.setItem(visKey, visible ? '1' : '0');
            apply();
        });
    }
    apply();
}

/* =========================
   TIMER NOTIFICATIONS
========================= */
function initTimerNotifications(examId) {
    const endTime = parseInt(localStorage.getItem(`exam_end_time_${examId}`), 10);
    if (!endTime) return;

    const key15 = `exam_notify_15_${examId}_${endTime}`;
    const key5 = `exam_notify_5_${examId}_${endTime}`;

    let notified15 = localStorage.getItem(key15) === '1';
    let notified5 = localStorage.getItem(key5) === '1';

    setInterval(() => {
        const minutesLeft = Math.floor((endTime - Date.now()) / 60000);

        if (!notified15 && minutesLeft <= 15) {
            notified15 = true;
            localStorage.setItem(key15, '1');
            showExamModal("15 minutes remaining",
                "You have 15 minutes left. Manage your time carefully.",
                "warning");
        }

        if (!notified5 && minutesLeft <= 5) {
            notified5 = true;
            localStorage.setItem(key5, '1');
            showExamModal("5 minutes remaining",
                "Only 5 minutes left. Submit your answers.",
                "danger", 6000);
        }
    }, 1000);
}

function showExamModal(title, message, type, autoCloseMs) {
    if (typeof updateModalContent !== "function") return;

    updateModalContent(title, message, null, type);
    const modalElement = document.getElementById("reusableConfirmModal");
    if (modalElement) {
        const modal = new bootstrap.Modal(modalElement);
        modal.show();
        if (autoCloseMs) {
            setTimeout(() => modal.hide(), autoCloseMs);
        }
    }
}

/* =========================
   QUESTION INTERACTIONS (FIXED FLAG LOGIC)
========================= */
function initQuestionInteractions(index, examId) {
    const navBox = document.getElementById(`nav-box-${index}`);
    const flaggedInput = document.getElementById("flaggedInput");
    const flagToggleBtn = document.getElementById("flagToggle");
    const flagTextSpan = document.getElementById("flagText");

    // Answer selection UI update
    document.querySelectorAll(".option-input").forEach(opt => {
        opt.addEventListener("change", () => {
            navBox?.classList.add("answered");
        });
    });

    // Flag toggle Logic
    if (flagToggleBtn && flaggedInput) {
        // 1. Initialize button state based on Hidden Input (Server Truth)
        let isFlagged = flaggedInput.value === "true";
        updateFlagUI(isFlagged);

        // 2. Handle Click
        flagToggleBtn.addEventListener("click", function () {
            // Toggle state
            isFlagged = !isFlagged;

            // Update Source of Truth (Hidden Input)
            flaggedInput.value = isFlagged.toString().toLowerCase();

            // Update UI
            updateFlagUI(isFlagged);

            // Update Sidebar immediately for visual feedback
            if (navBox) {
                if (isFlagged) navBox.classList.add("flagged");
                else navBox.classList.remove("flagged");
            }
        });
    }

    function updateFlagUI(flagged) {
        if (flagged) {
            flagToggleBtn.classList.remove("btn-outline-secondary");
            flagToggleBtn.classList.add("btn-danger");
            flagToggleBtn.querySelector("i").classList.remove("bi-flag");
            flagToggleBtn.querySelector("i").classList.add("bi-flag-fill");
            if (flagTextSpan) flagTextSpan.textContent = "Remove Flag";
        } else {
            flagToggleBtn.classList.remove("btn-danger");
            flagToggleBtn.classList.add("btn-outline-secondary");
            flagToggleBtn.querySelector("i").classList.remove("bi-flag-fill");
            flagToggleBtn.querySelector("i").classList.add("bi-flag");
            if (flagTextSpan) flagTextSpan.textContent = "Flag Q";
        }
    }
}

/* =========================
   ANSWER STATUS
========================= */
function initAnswerStatus(index) {
    $(".option-input").on("change", function () {
        $("#answerStatus")
            .removeClass("text-muted")
            .addClass("text-success")
            .html('<i class="bi bi-check-circle-fill me-1"></i> Answered');

        $(`#nav-box-${index}`).addClass("answered");
    });
}

/* =========================
   CLEAR, NAVIGATE & SUBMIT
========================= */
function clearChoice() {
    $(".option-input").prop("checked", false);
    $("#answerStatus")
        .removeClass("text-success")
        .addClass("text-muted")
        .html('<i class="bi bi-circle me-1"></i> Not yet answered');
}

// Submits the form simply (used by Next button)
function submitExamForm() {
    document.getElementById("examForm").submit();
}

// NEW: Updates the target index and then submits. 
// This saves the CURRENT question's data (including flag) before moving.
function navigateToIndex(index) {
    const form = document.getElementById("examForm");
    const nextIdxField = document.getElementById("nextIdxField");

    if (form && nextIdxField) {
        nextIdxField.value = index;
        form.submit();
    }
}
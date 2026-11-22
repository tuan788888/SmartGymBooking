(() => {
    document.querySelectorAll('[data-confirm-delete-chat]').forEach(deleteForm => {
        deleteForm.addEventListener('submit', event => {
            if (!window.confirm('Xóa cuộc trò chuyện này và toàn bộ tin nhắn?')) event.preventDefault();
        });
    });
    const form = document.getElementById('ai-chat-form');
    if (!form) return;
    const input = document.getElementById('Message');
    const chat = document.getElementById('chatMessages');
    const button = document.getElementById('chatSend');
    const status = document.getElementById('chatStatus');
    let sending = false;
    const updateCount = () => {
        document.getElementById('chatCount').textContent = `${input.value.length} / 2000`;
        input.style.height = 'auto';
        input.style.height = Math.min(input.scrollHeight, 160) + 'px';
    };
    input.addEventListener('input', updateCount);
    updateCount();
    chat.scrollTop = chat.scrollHeight;
    document.querySelectorAll('[data-prompt]').forEach(item => item.addEventListener('click', () => {
        if (sending) return;
        input.value = item.dataset.prompt;
        updateCount();
        input.focus();
    }));
    input.addEventListener('keydown', event => {
        if (event.key === 'Enter' && !event.shiftKey && !event.isComposing) {
            event.preventDefault();
            if (!sending && input.value.trim()) form.requestSubmit();
        }
    });
    form.addEventListener('submit', event => {
        if (sending) { event.preventDefault(); return; }
        if (!input.value.trim()) { event.preventDefault(); input.value = ''; form.reportValidity(); return; }
        if (!form.checkValidity() || (window.jQuery && !window.jQuery(form).valid())) return;
        sending = true;
        button.disabled = true;
        button.textContent = 'Đang gửi…';
        form.closest('.pt-chat').setAttribute('aria-busy', 'true');
        status.textContent = 'Đang chờ phản hồi, vui lòng giữ trang mở…';
        const pending = document.createElement('div');
        pending.className = 'pt-pending';
        pending.setAttribute('role', 'status');
        pending.textContent = 'AI PT đang chuẩn bị câu trả lời…';
        chat.appendChild(pending);
        chat.scrollTop = chat.scrollHeight;
    });
    window.addEventListener('pageshow', event => { if (event.persisted) window.location.reload(); });
})();

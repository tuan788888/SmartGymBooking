
document.querySelectorAll('img[data-pt-avatar]').forEach(function (image) {
    function showInitial() {
        if (!image.isConnected) return;
        const avatar = document.createElement('span');
        avatar.className = 'avatar-circle d-inline-flex align-items-center justify-content-center';
        avatar.style.width = image.style.width || '70px';
        avatar.style.height = image.style.height || '70px';
        avatar.setAttribute('role', 'img');
        avatar.setAttribute('aria-label', image.alt);
        avatar.textContent = Array.from(image.alt.trim())[0] || 'PT';
        image.replaceWith(avatar);
    }
    image.addEventListener('error', showInitial, { once: true });
    if (image.complete && image.naturalWidth === 0) showInitial();
});

(() => {
  document.querySelectorAll('.account-page form[method="post"]:not([data-profile-form]):not([data-payment-choice-form])').forEach(form => {
    form.addEventListener('submit', () => {
      if (form.getAttribute('aria-busy') === 'true') return;
      form.setAttribute('aria-busy', 'true');
      const button = form.querySelector('button[type="submit"]');
      if (button) {
        button.disabled = true;
        button.textContent = 'Đang xử lý…';
      }
      const status = document.createElement('span');
      status.className = 'account-status';
      status.setAttribute('role', 'status');
      status.setAttribute('aria-live', 'polite');
      status.textContent = 'Đang xử lý, vui lòng chờ…';
      form.append(status);
    });
  });
})();

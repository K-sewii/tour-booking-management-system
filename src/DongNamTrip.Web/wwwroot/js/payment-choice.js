(() => {
  const dialog = document.querySelector('#payment-choice-dialog');
  const opener = document.querySelector('[data-payment-open]');
  if (!dialog || !opener) return;

  const form = dialog.querySelector('[data-payment-choice-form]');
  const select = dialog.querySelector('#methodCode');
  const error = dialog.querySelector('#payment-dialog-error');
  const save = dialog.querySelector('[data-payment-save]');
  const label = document.querySelector('[data-payment-label]');
  const status = document.querySelector('#payment-choice-status');
  let previousFocus = opener;

  function showError(message) {
    error.textContent = message;
    error.hidden = false;
    error.focus();
  }

  opener.addEventListener('click', () => {
    previousFocus = document.activeElement || opener;
    error.hidden = true;
    error.textContent = '';
    status.hidden = true;
    dialog.showModal();
    select.focus();
  });
  dialog.querySelectorAll('[data-payment-close]').forEach(button =>
    button.addEventListener('click', () => dialog.close()));
  dialog.addEventListener('close', () => {
    (previousFocus?.isConnected ? previousFocus : opener).focus();
  });

  form.addEventListener('submit', async event => {
    event.preventDefault();
    if (!select.value) {
      showError('Vui lòng chọn một phương thức ưu tiên.');
      return;
    }
    error.hidden = true;
    error.textContent = '';
    save.disabled = true;
    save.textContent = 'Đang lưu…';
    try {
      const response = await fetch(form.action, { method: 'POST', body: new FormData(form), credentials: 'same-origin' });
      const result = await response.json();
      if (response.status === 401 && result.loginUrl) {
        window.location.assign(result.loginUrl);
        return;
      }
      if (!response.ok) {
        showError(result.message || 'Chưa lưu được lựa chọn. Vui lòng thử lại.');
        return;
      }
      label.textContent = result.label;
      opener.textContent = 'Đổi';
      status.textContent = result.message;
      status.hidden = false;
      dialog.close();
    } catch {
      showError('Chưa kết nối được máy chủ. Vui lòng thử lại.');
    } finally {
      save.disabled = false;
      save.textContent = 'Lưu';
    }
  });
})();

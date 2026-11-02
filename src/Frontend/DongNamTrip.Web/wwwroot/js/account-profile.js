(() => {
  const form = document.querySelector('[data-profile-form]');
  if (!form) return;
  const fields = [...form.querySelectorAll('input:not([type=hidden])')];
  const submit = form.querySelector('[data-profile-submit]');
  const status = form.querySelector('[data-profile-status]');
  let busy = false;
  function validate(field) {
    const value = field.value.trim();
    let error = '';
    if (field.name === 'Name') {
      if (!value) error = 'Vui lòng nhập họ tên.';
      else if (value.length < 2 || value.length > 100) error = 'Họ tên cần có từ 2 đến 100 ký tự.';
    }
    if (field.name === 'Email') {
      if (!value) error = 'Vui lòng nhập email.';
      else if (value.length > 254) error = 'Email tối đa 254 ký tự.';
      else if (!/^[^\s@]+@[^\s@]+$/.test(value) || /[^\x00-\x7F]/.test(value)) error = 'Email không đúng định dạng.';
    }
    if (field.name === 'PhoneNumber' && value && value !== field.dataset.originalPhone?.trim() && !/^[0-9]{1,15}$/.test(value)) error = 'Số điện thoại chỉ gồm tối đa 15 chữ số.';
    field.setAttribute('aria-invalid', error ? 'true' : 'false');
    document.getElementById(field.name + '-error').textContent = error;
    return !error;
  }
  fields.forEach(field => {
    field.addEventListener('blur', () => validate(field));
    field.addEventListener('input', () => {
      if (field.getAttribute('aria-invalid') === 'true') validate(field);
    });
  });
  form.addEventListener('submit', event => {
    if (busy) { event.preventDefault(); return; }
    if (!fields.map(validate).every(Boolean)) {
      event.preventDefault();
      fields.find(field => field.getAttribute('aria-invalid') === 'true')?.focus();
      return;
    }
    busy = true;
    form.querySelectorAll('.validation-summary-errors, .account-retry-hint').forEach(element => { element.hidden = true; });
    form.setAttribute('aria-busy', 'true');
    submit.disabled = true;
    submit.textContent = 'Đang lưu…';
    status.textContent = 'Đang lưu thay đổi, vui lòng chờ…';
    const cancel = form.querySelector('[data-profile-cancel]');
    cancel.setAttribute('aria-disabled', 'true');
    cancel.addEventListener('click', event => event.preventDefault());
  });
  const invalid = fields.find(field => field.getAttribute('aria-invalid') === 'true');
  const result = [...document.querySelectorAll('[data-profile-result]')].find(element => element.textContent.trim());
  (invalid || result)?.focus();
})();

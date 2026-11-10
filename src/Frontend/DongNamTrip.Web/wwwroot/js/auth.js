(() => {
  const messages = {
    emailRequired: "Vui lòng nhập email",
    emailInvalid: "Email không đúng định dạng.",
    passwordRequired: "Vui lòng nhập mật khẩu",
    nameRequired: "Vui lòng nhập họ tên.",
    passwordLength: "Mật khẩu phải có ít nhất 8 ký tự.",
    passwordComposition: "Mật khẩu cần có ít nhất một chữ và một số.",
    confirmationRequired: "Vui lòng nhập lại mật khẩu.",
    confirmationMismatch: "Nhập lại mật khẩu chưa khớp.",
    phone: "Số điện thoại chỉ gồm tối đa 15 chữ số.",
    terms: "Vui lòng đồng ý với điều khoản."
  };

  document.querySelectorAll("[data-auth-form]").forEach((form) => {
    const mode = form.dataset.authMode;
    const submit = form.querySelector("[data-auth-submit]");
    const status = form.querySelector("[data-auth-status]");
    const fields = [...form.querySelectorAll("input")].filter((field) => field.type !== "hidden");
    const fieldError = (field) => document.getElementById(`${field.name}-error`);

    const setFieldError = (field, message) => {
      const error = fieldError(field);
      field.setCustomValidity(message || "");
      field.setAttribute("aria-invalid", message ? "true" : "false");
      field.classList.toggle("input-validation-error", Boolean(message));
      if (error) error.textContent = message || "";
      return !message;
    };

    const validate = (field) => {
      const value = field.value;
      let message = "";
      if (field.name === "Name" && !value.trim()) message = messages.nameRequired;
      if (field.name === "Email") {
        if (!value.trim()) message = messages.emailRequired;
        else if (value.length > 254 || !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value.trim())) message = messages.emailInvalid;
      }
      if (field.name === "PhoneNumber" && value && (!/^\d{1,15}$/.test(value))) message = messages.phone;
      if (field.name === "Password") {
        if (!value) message = messages.passwordRequired;
        else if (mode === "register" && value.length < 8) message = messages.passwordLength;
        else if (mode === "register" && (!/[A-Za-zÀ-ỹ]/.test(value) || !/\d/.test(value))) message = messages.passwordComposition;
      }
      if (field.name === "ConfirmPassword") {
        const password = form.elements.Password;
        if (!value) message = messages.confirmationRequired;
        else if (password && value !== password.value) message = messages.confirmationMismatch;
      }
      if (field.name === "TermsAccepted" && !field.checked) message = messages.terms;
      return setFieldError(field, message);
    };

    const updateTerms = () => {
      const terms = form.querySelector(
        'input[type="checkbox"][name="TermsAccepted"]'
      );
      if (terms && submit) submit.disabled = !terms.checked;
    };

    fields.forEach((field) => {
      if (field.name === "PhoneNumber") {
        field.addEventListener("input", () => {
          field.value = field.value.replace(/\D/g, "").slice(0, 15);
          validate(field);
        });
      } else {
        field.addEventListener("input", () => {
          if (field.getAttribute("aria-invalid") === "true") validate(field);
          if (field.name === "Password" && mode === "register" && form.elements.ConfirmPassword?.value) validate(form.elements.ConfirmPassword);
        });
      }
      field.addEventListener("blur", () => validate(field));
      field.addEventListener("change", () => {
        validate(field);
        updateTerms();
      });
    });

    form.addEventListener("submit", (event) => {
      const valid = fields.map(validate).every(Boolean);
      updateTerms();
      if (!valid) {
        event.preventDefault();
        const firstInvalid = fields.find((field) => field.getAttribute("aria-invalid") === "true");
        firstInvalid?.focus();
        return;
      }
      if (submit) {
        form.classList.add("is-submitting");
        form.setAttribute("aria-busy", "true");
        submit.disabled = true;
        submit.textContent = submit.dataset.busyLabel || "Đang xử lý…";
      }
      if (status) status.textContent = "Đang xử lý, vui lòng chờ…";
    });

    updateTerms();
    const invalid = fields.find((field) => field.classList.contains("input-validation-error"));
    (invalid || form.querySelector("[autofocus]"))?.focus();
  });
})();

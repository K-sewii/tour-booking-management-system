export function safeReturn(value: string | null) {
  if (!value || !value.startsWith('/') || value.startsWith('//') || value.includes('\\')) return '/';
  try { return new URL(value, window.location.origin).origin === window.location.origin ? value : '/'; }
  catch { return '/'; }
}

export function AuthArt({ mode }: { mode: 'login' | 'register' }) {
  // Rendered by AuthLayout as a sibling of the form.
  return <div className="auth-art"><img src="/images/auth-saigon-night.png" alt="Sài Gòn về đêm với tòa nhà và vòng xoay rực sáng"/><div><div className="eyebrow">{mode === 'login' ? 'MỖI CHUYẾN ĐI LÀ MỘT KHỞI ĐẦU' : 'BẮT ĐẦU HÀNH TRÌNH CỦA BẠN'}</div><h1>{mode === 'login' ? <>Hẹn bạn,<br/>ở một nơi mới.</> : <>Đi gần,<br/>khám phá nhiều hơn.</>}</h1></div></div>;
}


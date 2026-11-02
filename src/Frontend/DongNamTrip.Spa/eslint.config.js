import reactHooks from 'eslint-plugin-react-hooks';
import tseslint from 'typescript-eslint';

// Chỉ bắt lỗi thật: quy tắc hooks và các lỗi TypeScript cơ bản. Định dạng không nằm trong lint.
export default tseslint.config(
  { ignores: ['node_modules', 'dist', '../DongNamTrip.Web/wwwroot/spa'] },
  ...tseslint.configs.recommended,
  {
    files: ['src/**/*.{ts,tsx}'],
    plugins: { 'react-hooks': reactHooks },
    rules: { 'react-hooks/rules-of-hooks': 'error', 'react-hooks/exhaustive-deps': 'warn' },
  },
);

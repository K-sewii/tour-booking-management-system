import { createRoot } from 'react-dom/client';
import App from './App';
import '../../../DongNamTrip.Web/wwwroot/css/site.css';
import '../../../DongNamTrip.Web/wwwroot/css/auth.css';
import '../../../DongNamTrip.Web/wwwroot/css/account.css';
import '../styles/spa.css';

createRoot(document.getElementById('root')!).render(<App/>);

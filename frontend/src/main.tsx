import { createRoot, hydrateRoot } from 'react-dom/client';
import App from './App';
import './styles.css';

const node = document.getElementById('root')!;
const payload = document.getElementById('public-data');
const initial = payload ? JSON.parse(payload.textContent || '{}') : undefined;
if (initial) hydrateRoot(node, <App initial={initial} />);
else createRoot(node).render(<App />);

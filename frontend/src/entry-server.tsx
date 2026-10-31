import { renderToString } from 'react-dom/server';
import { StaticRouter } from 'react-router-dom';
import { AppRoutes } from './App';
import type { PublicData } from './lib/public-data';
export { headMarkup, safeJson } from './lib/seo';
export function render(url: string, initial: PublicData) {
  return renderToString(
    <StaticRouter location={url}>
      <AppRoutes initial={initial} />
    </StaticRouter>,
  );
}

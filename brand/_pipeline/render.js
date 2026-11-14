import puppeteer from 'puppeteer';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import fs from 'node:fs/promises';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const BRAND = path.resolve(__dirname, '..');
const TEMPLATES = path.join(BRAND, 'render-templates');

const log = (...args) => console.log(`[render] ${args.join(' ')}`);

// (template, outputPath, width, height, transparent?)
const JOBS = [
  // ============ APP ICON — rounded tile, dark gradient bg ============
  ['icon-tile-dark.html',   'app-icon/png/icon-16.png',   16,  16,  true],
  ['icon-tile-dark.html',   'app-icon/png/icon-24.png',   24,  24,  true],
  ['icon-tile-dark.html',   'app-icon/png/icon-32.png',   32,  32,  true],
  ['icon-tile-dark.html',   'app-icon/png/icon-48.png',   48,  48,  true],
  ['icon-tile-dark.html',   'app-icon/png/icon-64.png',   64,  64,  true],
  ['icon-tile-dark.html',   'app-icon/png/icon-128.png',  128, 128, true],
  ['icon-tile-dark.html',   'app-icon/png/icon-256.png',  256, 256, true],

  // ============ MICROSOFT STORE TILES — square corners ============
  ['icon-tile-dark-square.html', 'store-tiles/StoreLogo.png',           50,  50,  true],
  ['icon-tile-dark-square.html', 'store-tiles/Square44x44Logo.png',     44,  44,  true],
  ['icon-tile-dark-square.html', 'store-tiles/Square71x71Logo.png',     71,  71,  true],
  ['icon-tile-dark-square.html', 'store-tiles/Square150x150Logo.png',   150, 150, true],
  ['wide-tile.html',             'store-tiles/Wide310x150Logo.png',     310, 150, false],
  ['large-tile.html',            'store-tiles/Square310x310Logo.png',   310, 310, false],
  ['splash.html',                'store-tiles/SplashScreen.png',        620, 300, false],

  // Scaled MS Store tile variants (Microsoft recommends 100/125/150/200/400 scales)
  ['icon-tile-dark-square.html', 'store-tiles/Square44x44Logo.scale-200.png', 88,  88,  true],
  ['icon-tile-dark-square.html', 'store-tiles/Square44x44Logo.scale-400.png', 176, 176, true],
  ['icon-tile-dark-square.html', 'store-tiles/Square150x150Logo.scale-200.png', 300, 300, true],
  ['wide-tile.html',             'store-tiles/Wide310x150Logo.scale-200.png',   620, 300, false],

  // ============ SYSTEM TRAY — transparent, multiple themes/sizes ============
  // Dark Windows theme (white icon)
  ['tray-w-only-white.html', 'tray/transparent/tray-white-16.png', 16, 16, true],
  ['tray-w-only-white.html', 'tray/transparent/tray-white-20.png', 20, 20, true],
  ['tray-w-only-white.html', 'tray/transparent/tray-white-24.png', 24, 24, true],
  ['tray-bracket-white.html','tray/transparent/tray-white-32.png', 32, 32, true],
  ['tray-bracket-white.html','tray/transparent/tray-white-48.png', 48, 48, true],

  // Light Windows theme (dark icon)
  ['tray-w-only-dark.html',  'tray/transparent/tray-dark-16.png',  16, 16, true],
  ['tray-w-only-dark.html',  'tray/transparent/tray-dark-20.png',  20, 20, true],
  ['tray-w-only-dark.html',  'tray/transparent/tray-dark-24.png',  24, 24, true],
  ['tray-bracket-dark.html', 'tray/transparent/tray-dark-32.png',  32, 32, true],
  ['tray-bracket-dark.html', 'tray/transparent/tray-dark-48.png',  48, 48, true],

  // Active state (teal)
  ['tray-w-only-teal.html',  'tray/transparent/tray-active-16.png', 16, 16, true],
  ['tray-w-only-teal.html',  'tray/transparent/tray-active-20.png', 20, 20, true],
  ['tray-w-only-teal.html',  'tray/transparent/tray-active-24.png', 24, 24, true],
  ['tray-bracket-teal.html', 'tray/transparent/tray-active-32.png', 32, 32, true],

  // ============ INSTALLER (Inno Setup) ============
  ['wizard-banner.html', 'installer/WizardImage.png',      164, 314, false],
  ['wizard-header.html', 'installer/WizardSmallImage.png', 497, 58,  false],

  // ============ WEBSITE ============
  ['icon-tile-dark.html', 'web/favicon/favicon-16.png',  16,  16,  true],
  ['icon-tile-dark.html', 'web/favicon/favicon-32.png',  32,  32,  true],
  ['icon-tile-dark.html', 'web/favicon/favicon-48.png',  48,  48,  true],
  ['icon-tile-dark.html', 'web/favicon/favicon-180.png', 180, 180, true], // Apple touch
  ['icon-tile-dark.html', 'web/favicon/favicon-192.png', 192, 192, true], // Android chrome
  ['icon-tile-teal.html', 'web/favicon/maskable-192.png', 192, 192, true],
  ['icon-tile-teal.html', 'web/favicon/maskable-512.png', 512, 512, true],
  ['icon-tile-dark.html', 'web/favicon/favicon-512.png', 512, 512, true],
  ['og-card.html',        'web/og-image.png',            1200, 630, false],

  // ============ IN-APP ============
  ['splash.html',        'in-app/splash.png',        620, 300, false],
  ['about-header.html',  'in-app/about-header.png',  420, 110, false],
];

async function render(browser, template, outRel, width, height, transparent) {
  const tplPath = path.join(TEMPLATES, template);
  const outPath = path.join(BRAND, outRel);
  await fs.mkdir(path.dirname(outPath), { recursive: true });

  const page = await browser.newPage();
  // Use deviceScaleFactor=1 to get exact pixel output at given width/height
  await page.setViewport({ width, height, deviceScaleFactor: 1 });
  await page.goto('file:///' + tplPath.replace(/\\/g, '/'), { waitUntil: 'networkidle0' });
  // Wait for Satoshi to load
  await page.evaluate(async () => { if (document.fonts && document.fonts.ready) { await document.fonts.ready; } });

  await page.screenshot({
    path: outPath,
    type: 'png',
    omitBackground: !!transparent,
    clip: { x: 0, y: 0, width, height },
  });
  await page.close();
  log(`✓ ${outRel} (${width}×${height})`);
}

(async () => {
  log(`Brand pipeline starting · ${JOBS.length} assets`);
  const browser = await puppeteer.launch({
    headless: 'shell',
    args: ['--font-render-hinting=none', '--disable-font-subpixel-positioning'],
  });
  try {
    for (const job of JOBS) {
      await render(browser, ...job);
    }
  } finally {
    await browser.close();
  }
  log(`✓ Done — rendered ${JOBS.length} assets`);
})();

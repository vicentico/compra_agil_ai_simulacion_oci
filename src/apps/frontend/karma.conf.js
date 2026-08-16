// Complemento (no reemplazo) de la configuración Karma que genera
// internamente @angular/build:karma. Solo agrega un launcher headless
// sin sandbox, necesario para correr en contenedores/CI ejecutando como
// root (p.ej. GitHub Actions, este mismo scaffold). En desarrollo local
// `ng test` usa Chrome normalmente sin necesidad de este launcher.
module.exports = function (config) {
  config.set({
    frameworks: ['jasmine'],
    customLaunchers: {
      ChromeHeadlessCI: {
        base: 'ChromeHeadless',
        flags: ['--no-sandbox', '--disable-gpu'],
      },
    },
  });
};

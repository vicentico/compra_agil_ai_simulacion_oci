export const environment = {
  production: false,
  // En dev local vía Traefik: el navegador resuelve api.ppip.localhost por
  // RFC 6761 sin tocar /etc/hosts. Ver infrastructure/docker/README.md.
  apiBaseUrl: 'http://api.ppip.localhost',
};

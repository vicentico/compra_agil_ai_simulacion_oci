# syntax=docker/dockerfile:1
FROM node:22-alpine AS build
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci
COPY . .
RUN npm run build -- --configuration production

FROM nginx:1.27-alpine AS runtime
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist/ppip-frontend/browser /usr/share/nginx/html
# nginx:alpine ya corre como usuario "nginx" no-root en sus procesos worker.
EXPOSE 80
HEALTHCHECK --interval=10s --timeout=5s --retries=6 --start-period=15s \
  CMD wget --no-verbose --tries=1 --spider http://localhost:80/healthz || exit 1

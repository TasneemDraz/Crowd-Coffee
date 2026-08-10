# Story 17.1 (T17.1.2). Build-time API base URL: VITE_API_BASE_URL already defaults to '/api'
# in every src/api/*.ts and src/lib/adminApi.ts (baked in well before this epic) — leaving the
# build ARG unset keeps that default, which is exactly right here since nginx.conf below
# reverse-proxies /api, /hubs, and /uploads to the api service under the same origin the SPA is
# served from (mirrors Vite's dev-server proxy, and sidesteps needing CORS for this topology
# entirely — CORS in Program.cs still exists for any deployment that instead puts the API on a
# different origin).
FROM node:22-alpine AS build
WORKDIR /app

COPY package.json package-lock.json ./
RUN npm ci

COPY . .
ARG VITE_API_BASE_URL
ENV VITE_API_BASE_URL=${VITE_API_BASE_URL}
RUN npm run build

FROM nginx:1.27-alpine AS runtime
COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80

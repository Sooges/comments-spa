# Build the Angular application
FROM node:22-alpine AS build
WORKDIR /app

# the packages are installed first, so they are not installed again when only the source changes
COPY package.json package-lock.json ./
RUN npm ci

COPY . .
RUN npm run build

# Serve the files with nginx, it also passes the requests to the API
FROM nginx:alpine
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist/frontend/browser /usr/share/nginx/html
EXPOSE 80

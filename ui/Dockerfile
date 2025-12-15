FROM node:lts-slim AS build
WORKDIR /src
RUN npm install -g @angular/cli

COPY package*.json ./
RUN npm ci

COPY . ./

ARG BASE_HREF=/
RUN ng build --configuration=production --base-href $BASE_HREF

FROM nginx:stable AS final
EXPOSE 80

COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /src/dist/ui/browser /usr/share/nginx/html

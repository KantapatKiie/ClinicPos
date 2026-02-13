FROM node:20-alpine AS deps
WORKDIR /app
COPY src/frontend/clinic-pos-web/package.json src/frontend/clinic-pos-web/package-lock.json ./
RUN npm ci

FROM node:20-alpine AS builder
WORKDIR /app
COPY --from=deps /app/node_modules ./node_modules
COPY src/frontend/clinic-pos-web .
RUN npm run build

FROM node:20-alpine AS runner
WORKDIR /app
ENV NODE_ENV=production
COPY --from=builder /app .
EXPOSE 3000
CMD ["npm", "run", "start"]

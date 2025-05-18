# Stage 1: build
FROM golang:1.24-alpine AS builder
RUN apk add --no-cache git
WORKDIR /app

# Másoljuk át először a modul fájlokat
COPY go.mod go.sum .
RUN go mod tidy

# Másoljuk át a többi fájlt
COPY . .

# Ellenőrizzük a fájlokat
RUN ls -la /app

# Build
RUN CGO_ENABLED=0 GOOS=linux go build -o publisher-api main.go

# Stage 2: futtató image
FROM alpine:3.18
RUN apk add --no-cache ca-certificates
WORKDIR /root/
COPY --from=builder /app/publisher-api .

EXPOSE 8080
ENTRYPOINT ["./publisher-api"]
# Gatekeeper

**Gatekeeper** is a rule-based policy engine operating over a REST API, capable of allowing or rejecting HTTP requests. The system is designed for easy deployment in containerized environments (e.g., behind a Traefik reverse proxy).

---

## 🔧 Main Components

### 🧠 Gatekeeper.Server (Backend)

The core of the project: a policy engine implemented in F# using the Giraffe framework.

* **Purpose:** Evaluates each incoming HTTP request based on its context.
* **Decision Making:** Only requests matching an existing rule are permitted. If no rule applies, it returns **403 Forbidden**.
* **Storage:** Stores rules and user data in an SQLite database.
* **Configuration:** Customizable via environment variables (e.g., admin credentials).

### 🖥 Gatekeeper.Client (Frontend)

A simple HTML/JS/CSS-based admin dashboard for creating, editing, and deleting policy rules.

* Serves static files via Nginx.
* Available as a Docker image (see below).

### 🧪 Demo Projects

* **TodoDemo:** A simple todo-management REST API built with F# and Giraffe.
* **PublisherDemo:** Another demo service, also written in F#.

---

## 🚀 Usage

### 1. Run with Docker Compose (recommended)

```bash
docker-compose up --build
```

This command starts:

* The Gatekeeper.Server application (F# policy engine)
* The Gatekeeper.Client frontend (admin dashboard)
* The demo REST APIs
* The Traefik proxy

### 2. Environment Variables

The backend (Gatekeeper.Server) uses the following environment variables:

```env
ADMIN_EMAIL=admin@gatekeeper.org
ADMIN_PASSWORD=0000admin
```

Set these in the `docker-compose.yml` file or at runtime using `-e` flags.

### 3. Local Execution

To run the server locally without containers:

```bash
cd Gatekeeper.Server
dotnet run
```

The demo projects (`TodoDemo`, `PublisherDemo`) can be started in the same way.

---

## 🛠 Technologies

* **F#**
* **Giraffe** web framework
* **SQLite** database
* **Nginx** for serving static files
* **Docker** & **Docker Compose** for containerization
* **Traefik** as reverse proxy

---

## 📦 Docker Hub

The project images are available on Docker Hub:

* **Backend (policy engine):** `[knapkomadmin/knapkom-gatekeeper-be](https://hub.docker.com/repository/docker/knapkomadmin/knapkom-gatekeeper-be)`
* **Frontend (admin dashboard):** `[knapkomadmin/knapkom-gatekeeper-fe](https://hub.docker.com/repository/docker/knapkomadmin/knapkom-gatekeeper-fe)`


## 🔜 Further Enhancements

* 🧪 **Testing:** Unit and integration tests
* 🔐 **Security:** Authentication, password hashing, audit logging
* 🤝 **Contributing:** Guidelines for contributors


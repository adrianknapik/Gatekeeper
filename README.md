# Gatekeeper
Gatekeeper is a rule-based policy engine that operates over a REST API, designed to allow or reject HTTP requests based on their context. Built as an MVP for a university course in 10 days, it is deployable in containerized environments (e.g., behind a Traefik reverse proxy) and provides a foundation for real-world enterprise use.
## 📌 Current Version (MVP)
This is an MVP developed with a focus on simplicity and core functionality. The current version only supports JWT token validation for evaluating request contexts. Support for additional contexts (e.g., IP, country, timestamp) is planned for future iterations.

## 🔧 Main Components
### 🧠 Gatekeeper.Server (Backend)
The core policy engine, implemented in F# using the Giraffe framework.

- Purpose: Evaluates incoming HTTP requests based on predefined rules.
- Decision Logic: Requests are allowed only if they match an existing rule; otherwise, a 403 Forbidden response is returned.
- Storage: Rules are stored in an SQLite database and cached in memory for performance.
- Configuration: Configurable via environment variables (e.g., admin credentials, JWT secret).

### 🖥 Gatekeeper.Client (Frontend)
A simple admin dashboard built with HTML, JavaScript, and CSS for creating, editing, and deleting policy rules.

- Technology: Static files served via Nginx.
- Features: Rule management with a user-friendly interface.
- Deployment: Available as a Docker image.

### 🧪 Demo Projects

- TodoDemo: A minimal REST API for managing todos, built with F# and Giraffe.
- PublisherDemo: A sample service for testing policy enforcement, also in F#.


## 🚀 Getting Started
1. Run with Docker Compose (Recommended)
```docker-compose up --build```

This command starts:

- Gatekeeper.Server (policy engine)
- Gatekeeper.Client (admin dashboard)
- Demo APIs (TodoDemo, PublisherDemo)
- Traefik (reverse proxy)

2. Environment Variables
The backend (Gatekeeper.Server) uses the following environment variables:
```
ADMIN_EMAIL=admin@gatekeeper.org
ADMIN_PASSWORD=0000admin
```
Set these in the docker-compose.yml file or pass them at runtime using -e flags.

3. Local Execution
To run the server locally without Docker:
```
cd Gatekeeper.Server
dotnet run
```
Similarly, the demo projects (TodoDemo, PublisherDemo) can be started with dotnet run in their respective directories.

## 🛠 Technologies

- Backend: F# (Giraffe framework)
- Frontend: HTML, JavaScript, CSS, Nginx
- Database: SQLite
- Containerization: Docker, Docker Compose
- Reverse Proxy: Traefik


## 📦 Docker Hub
The project images are available on Docker Hub:

- Backend: [knapkomadmin/knapkom-gatekeeper-be](https://hub.docker.com/r/knapkomadmin/knapkom-gatekeeper-be)
- Frontend: [knapkomadmin/knapkom-gatekeeper-fe](https://hub.docker.com/r/knapkomadmin/knapkom-gatekeeper-fe)


## 🖼 Screenshots
- Login Screen
[The admin dashboard login page, accessible with the configured ADMIN_EMAIL and ADMIN_PASSWORD.](http://knapkom.com/due/fsharp/images/gkLogin.png)
- Rule Management
[The rule creation and editing interface, where users define field-operator-value logic for JWT-based policies.](http://knapkom.com/due/fsharp/images/gkRules.png)
- Testing Policies
[A sample request evaluation in the demo environment, showing how Gatekeeper processes JWT-based rules.](http://knapkom.com/due/fsharp/images/gkTest.png)

## 🛠 API Endpoints
The Gatekeeper REST API provides the following endpoints:
```
POST /api/rules: Create a new rule (JSON payload).{
  "field": "role",
  "operator": "==",
  "value": "admin",
  "action": "Allow"
}
```
```
GET /api/rules: List all rules.
```
```
DELETE /api/rules/:id: Delete a rule by ID.
```
```
POST /api/gk/evaluate: Evaluate a request context (requires a valid JWT token).{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```


Note: A detailed OpenAPI specification is planned for future releases.

## 📋 Rule Logic
Rules follow a field-operator-value structure and are evaluated based on JWT token claims (e.g., role, sub). Example:
```
[
  {
    "field": "role",
    "operator": "==",
    "value": "admin",
    "action": "Allow"
  },
  {
    "field": "sub",
    "operator": "!=",
    "value": "blocked_user",
    "action": "Deny"
  }
]
```

## 💻 System Requirements

- Docker and Docker Compose (v2.0 or later)
- .NET 8 SDK (for local backend development)
- Minimum 2 GB RAM for running containers


## 🔐 Security Notes

The MVP uses basic authentication for the admin dashboard (ADMIN_EMAIL/ADMIN_PASSWORD).
JWT validation requires a secret key (JWT_SECRET) for token verification.
Future enhancements include password hashing, audit logging, and OAuth support.


## 🔜 Future Enhancements

Support for additional request contexts (IP, country, timestamp, etc.)
Unit and integration tests for the policy engine and API
Enhanced security features (e.g., audit logging, OAuth)
Improved rule management with advanced logical operators
OpenAPI/Swagger documentation


## 🤝 Contributing
Contributions are welcome! Please submit issues or pull requests via the GitHub repository.
Development Setup

Install dependencies:
- .NET 8 SDK
- Docker


## 📬 Contact
For questions or bug reports, please open an issue on GitHub or contact adrian.knapik@outlook.hu.

## 🎯 About the Project
Gatekeeper was developed as an MVP for a university course, with the goal of creating a simple yet extensible policy engine for HTTP request authorization. Built with limited resources, it focuses on core functionality and serves as a foundation for future enhancements.

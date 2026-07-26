# Microclimate IoT System

A robust and scalable IoT solution for monitoring microclimate environments using ESP32 microcontrollers. 

## Tech Stack

*   **Backend:** ASP.NET Core Minimal API (.NET 8), C#, Entity Framework Core
*   **Architecture:** Clean Architecture (Domain, Application, Infrastructure, WebAPI)
*   **Frontend:** Angular 21
*   **Message Broker:** RabbitMQ
*   **Database:** MS SQL Server 2022
*   **Hardware:** ESP32 Microcontrollers

## Setup & Quick Start

Follow these steps to set up the local development environment on your machine.

### 1. Start External Dependencies (Docker)

Spin up SQL Server and RabbitMQ using Docker Compose. Ensure you have Docker and Docker Compose installed.

```bash
# Start the containers in the background
docker-compose up -d
```

*   **SQL Server:** `localhost:1433` (User: `sa`, Password: `SuperStrong!Passw0rd2024`)
*   **RabbitMQ Management UI:** <http://localhost:15672> (User: `iot_admin`, Password: `iot_secure_password`)
*   **MQTT Protocol Port:** `1883`

### 2. Backend Setup (.NET 8)

The backend is built using Native Minimal APIs following Clean Architecture principles.

#### Running the API
The application **automatically applies any pending database migrations upon startup**.

```bash
cd backend/src/MicroclimateIotSystem.WebAPI
dotnet run
```
Once running, navigate to the Swagger documentation UI at <https://localhost:7191/swagger> (the root URL redirects here automatically).

#### Managing Database Migrations
While migrations are applied on app startup, you may need to manage them manually during development. Ensure you have the EF Core CLI tools installed:

```bash
dotnet tool install --global dotnet-ef
```

To **add a new migration**, run this from the `backend/src/MicroclimateIotSystem.WebAPI` directory:
```bash
dotnet ef migrations add <MigrationName> --project ../MicroclimateIotSystem.Infrastructure/MicroclimateIotSystem.Infrastructure.csproj --startup-project MicroclimateIotSystem.WebAPI.csproj -o Migrations
```

To **update the database manually**:
```bash
dotnet ef database update --project ../MicroclimateIotSystem.Infrastructure/MicroclimateIotSystem.Infrastructure.csproj --startup-project MicroclimateIotSystem.WebAPI.csproj
```

### 3. Frontend Setup (Angular)

The frontend is a standard Angular application. Ensure you have Node.js and npm installed.

```bash
cd frontend

# Install dependencies
npm install

# Start the development server
npm start
```
The web application will be available at <http://localhost:4200/>.

### 4. Firmware Setup (ESP32)

The firmware uses the **PlatformIO** ecosystem. Ensure you have the [PlatformIO Core CLI](https://docs.platformio.org/en/latest/core/index.html) or the PlatformIO VS Code Extension installed.

```bash
cd firmware

# Build the firmware
pio run

# Upload to the connected ESP32 device
pio run --target upload

# Open the serial monitor to view logs
pio device monitor
```

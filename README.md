# Microclimate IoT System

A robust and scalable IoT solution for monitoring microclimate environments using ESP32 microcontrollers. 

## Technology Stack

*   **Backend:** ASP.NET Core Minimal API (.NET 8), C#
*   **Architecture:** Clean Architecture (Domain, Application, Infrastructure, WebAPI)
*   **Frontend:** Angular
*   **Message Broker:** RabbitMQ
*   **Database:** MS SQL Server 2022
*   **Hardware:** ESP32 Microcontrollers

## Quick Start

Follow these steps to set up the local development environment on your Linux machine:

### 1. Start External Dependencies

Spin up SQL Server and RabbitMQ using Docker Compose. Ensure you have Docker and Docker Compose installed.

```bash
# Start the containers in the background
docker-compose up -d
```

*   **SQL Server:** `localhost:1433` (User: `SA`, Password: `SuperStrong!Passw0rd2024`)
*   **RabbitMQ Management UI:** [http://localhost:15672](http://localhost:15672) (User: `iot_admin`, Password: `iot_secure_password`)

### 2. Run the API

The .NET 8 backend is organized within the `MicroclimateIotSystem` directory and utilizes the modern `.slnx` solution format. You can build and run the Web API from the root folder like this:

```bash
cd MicroclimateIotSystem/src/MicroclimateIotSystem.WebAPI
dotnet run
```

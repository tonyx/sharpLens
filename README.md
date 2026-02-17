# Antigravity Vibe-Coding Experiment: SharpLens

This project is an **antigravity based vibe-coding experiment** meant to implement a more extended and general event browser.

At the moment, it is tested against the **sharpino** template, with some changes to demonstrate the capabilities.

## Goal

The ultimate goal of this project is to create a **general event browser** capable of:
*   Handling **multiple commands** that act against **multiple streams**.
*   Adapting to **application layer services** that may expose **complex logic** related to those multiple streams.

This tool aims to provide deep insight and control over event-sourced systems, transcending simple CRUD operations to fully embrace the complexity of distributed, event-driven architectures.

## Implementation Details

This project is based on the **Bolero** template.

## Getting Started

### 1. Database Setup (Postgres)

The database configuration and docker compose file are located in `src/sharpLens.Sample`.

To start the Postgres database and populate it:

1.  Navigate to `src/sharpLens.Sample`:
    ```bash
    cd src/sharpLens.Sample
    ```
2.  Start the Docker container:
    ```bash
    docker compose up -d
    ```
```markdown
    This will start a Postgres instance on port **5434** (configurable in `.env`).

3.  Setup or reset the database:
    *   `dbmate up` to setup the database inside the docker template.
    *   `dbmate drop` to reset it.
```

### 2. Running the Application

To run the application (Server & Client):

1.  Navigate to `src/sharpLens.Server`:
    ```bash
    cd src/sharpLens.Server
    ```
2.  Run the project:
    ```bash
    dotnet run
    ```
    This will start the server, which serves the Blazor/Bolero client. The application will be accessible at the ports specified in the output (usually `https://localhost:5001` or similar).

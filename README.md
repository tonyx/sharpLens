# Antigravity Vibe-Coding Experiment: SharpLens

This project is an **antigravity based vibe-coding experiment** meant to implement a more extended and general event browser.

At the moment, it is tested against the **sharpino** template, with some changes to demonstrate the capabilities.

## Goal

The ultimate goal of this project is to create a **general event browser** capable of:
*   Handling **multiple commands** that act against **multiple streams**.
*   Adapting to **application layer services** that may expose **complex logic** related to those multiple streams.

This tool aims to provide deep insight and control over event-sourced systems, transcending simple CRUD operations to fully embrace the complexity of distributed, event-driven architectures.

## Implemented Features

### 1. Generic Object Browser
*   **Functionality**: Displays a list of entities (starting with `Todo` objects) fetched from the backend.
*   **Implementation**: Created a reusable `ObjectBrowser` component that renders objects in a table format using reflection to display properties.

### 2. Dynamic Command Executor
*   **Functionality**: Allows users to execute domain commands (e.g., `Activate`, `Complete`, `Comment`) on any selected object.
*   **Implementation**:
    *   Built a `CommandExecutor` component that inspects the command union type (via `ReflectionHelper`) to dynamically generate forms for command parameters.
    *   Implemented handling for complex inputs like `DateTime` and tuples (e.g., for `Comment`).
    *   Added error handling to display feedback if a command fails (e.g., domain validation errors).

### 3. Event History & Inspection
*   **Functionality**: View the full history of events that have occurred for a specific entity.
*   **Implementation**:
    *   Added an "Event Browser" view that fetches and displays the **Initial Snapshot** (the state at creation) followed by all subsequent events (e.g., `Added`, `Started`, `Commented`).
    *   Integrated this view directly into the main Todo page for easy access alongside the command interface.

### 4. Object Creator
*   **Functionality**: A UI to create new entities (Todos) by invoking their `New` constructor.
*   **Implementation**:
    *   Developed an `ObjectCreator` component that reflects on the `New` static member to build a creation form.
    *   Refactored this into a stateful `Bolero.Component` to ensure data binding works correctly during user interaction (fixing the empty text bug).

### 5. Infrastructure & Polish
*   **Git Configuration**: Fixed the submodule issue with `sharpLens.Sample` so it is correctly tracked in the main repository.
*   **Navigation**: Cleaned up the main menu to focus on the core functionality.
*   **Documentation**: Created a Project `README.md` with setup instructions for Docker/Postgres and running the application.

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

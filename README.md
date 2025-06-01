# Simple Web Server (C# TCP Sockets)

This project implements a basic web server using raw C# TCP sockets, designed to serve static HTML, CSS, and JavaScript files from a designated `webroot` directory. It focuses on demonstrating fundamental web server concepts, multi-threading for concurrent client handling, and robust HTTP error responses.

## Features

-   **TCP Socket Listener:** Runs on a configurable port (default: 8080), continuously listening for incoming client connections.
-   **Multi-threaded Client Handling:** Each incoming client connection is processed in a separate thread, allowing the server to handle multiple requests concurrently.
-   **Static File Serving:** Serves `.html`, `.css`, and `.js` files directly from the `webroot` directory.
-   **Robust HTTP Error Handling:**
    -   `400 Bad Request`: Returned for malformed HTTP request lines.
    -   `403 Forbidden`: Returned for unsupported file types (e.g., `.txt`, `.png`) and, crucially, for blocked directory traversal attempts (e.g., `../`, `%2E%2E%2F`).
    -   `404 Not Found`: Returned for requested files that do not exist within the `webroot` directory.
    -   `405 Method Not Allowed`: Returned for non-GET HTTP request methods (e.g., POST, PUT, DELETE).
    -   `500 Internal Server Error`: Returned for unhandled server-side exceptions, ensuring the server doesn't crash from unexpected errors.
-   **Security:** Implements critical checks to prevent directory traversal attacks. It URL-decodes incoming paths *before* performing path validation to correctly identify and block attempts to access files outside the `webroot` directory (e.g., `C:\..\..\etc\passwd`).
-   **Connection Management:** Ensures each client connection is properly closed after its request has been processed.

## Getting Started

### Prerequisites

-   [.NET SDK (v6.0 or higher recommended)](https://dotnet.microsoft.com/download)
-   [Git](https://git-scm.com/downloads)
-   (Optional, for advanced testing): `curl` (command-line tool) and `telnet` / `netcat` (for raw requests).

### Installation and Running

1.  **Clone the repository:**
    ```bash
    git clone https://github.com/YOUR_GITHUB_USERNAME/SimpleWebServerTCP.git
    cd SimpleWebServerTCP/SimpleWebServer # Navigate into the C# project folder
    ```
    *(Remember to replace `YOUR_GITHUB_USERNAME` with your actual GitHub username)*

2.  **Prepare the `webroot` directory:**
    The server automatically creates a `webroot` directory (e.g., `SimpleWebServer/bin/Debug/netX.X/webroot/`) and a default `index.html` file if they don't exist. For comprehensive testing, ensure your `webroot` directory is populated with sample static files as follows:

    ```
    /webroot
      ├── index.html   (Your main page)
      ├── about.html   (A linked page)
      ├── styles.css   (A stylesheet for HTML files)
      ├── script.js    (A JavaScript file for HTML files)
      ├── secret.txt   (A plain text file, for 403 forbidden type test)
      └── image.png    (Any image file, for 403 forbidden type test)
    ```
    *(You can use the sample content provided in previous steps for these files).*

3.  **Run the server:**
    ```bash
    dotnet run
    ```
    The server will start listening on `http://localhost:8080/`. Observe the console output for status messages, client connections, and request processing details.

## Usage

Once the server is running, you can interact with it using your web browser or command-line tools.

### Basic Navigation (Web Browser)

-   **Home Page:** `http://localhost:8080/`
-   **About Page:** `http://localhost:8080/about.html`
-   **Direct CSS Access:** `http://localhost:8080/styles.css`
-   **Direct JavaScript Access:** `http://localhost:8080/script.js`

### Testing Error Handling (Web Browser & `curl` / `telnet`)

-   **404 Not Found:** `http://localhost:8080/nonexistent.html`
-   **403 Forbidden (Unsupported File Type):**
    -   `http://localhost:8080/secret.txt`
    -   `http://localhost:8080/image.png`
-   **403 Forbidden (Directory Traversal Attempts):**
    -   `curl http://localhost:8080/../Program.cs`
    -   `curl http://localhost:8080/%2E%2E%2FProgram.cs` (URL-encoded `../`)
    -   `curl http://localhost:8080/webroot/../Program.cs`
-   **405 Method Not Allowed:** (Use `curl`)
    ```bash
    curl -X POST http://localhost:8080/index.html
    ```
-   **400 Bad Request:** (Use `telnet` or `netcat` for raw requests)
    ```bash
    telnet localhost 8080
    # Then type: INVALID_REQUEST_LINE [ENTER][ENTER]
    ```

Observe the HTML error pages displayed in the browser and the detailed logs in the server's console for each test.

## Technologies Used

-   C#
-   .NET (Console Application)
-   System.Net.Sockets (for TCP communication)
-   System.IO (for file system operations)
-   System.Net.WebUtility (for URL decoding)

## Commit History

This project's development history on GitHub reflects a step-by-step approach, with continuous commits marking progress, feature additions, and crucial bug fixes as identified during testing.

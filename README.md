# SkillSnap: Full-Stack Developer Portfolio (Capstone Project)

This repository contains the source code for SkillSnap, a full-stack web application designed to showcase a developer's skills and projects. This project serves as a capstone to demonstrate a comprehensive understanding of modern web development principles, from backend architecture to frontend user interface design.

## Project Overview

SkillSnap is a personal portfolio platform built with a .NET-centric technology stack. It features a clean, component-based frontend that consumes a robust backend API. The application is designed to be a single source of truth for a developer's professional profile, including their biography, skills, and a list of their notable projects.

## Technology Stack & Concepts Demonstrated

This project was built to showcase proficiency across the full development stack.

### 1. Backend Development (ASP.NET Core Web API)

The backend is a powerful and scalable RESTful API built with ASP.NET Core.

-   **Framework:** Utilizes the latest version of ASP.NET Core for building high-performance APIs.
-   **Language:** Written in C#, leveraging its strong typing and object-oriented capabilities.
-   **Architecture:** Follows the controller pattern, providing clear and organized endpoints for CRUD (Create, Read, Update, Delete) operations on all data models (Users, Skills, Projects).
-   **Data Access:** Uses **Entity Framework Core** as the Object-Relational Mapper (O-RM) to interact with the database, abstracting away raw SQL queries.
-   **API Design:** Adheres to RESTful principles, with clear and predictable API endpoints. Swagger/OpenAPI is integrated for automatic API documentation and testing.

### 2. Frontend Development (Blazor WebAssembly)

The frontend is a modern Single Page Application (SPA) built with Blazor WebAssembly.

-   **Framework:** Leverages Blazor to allow for C# and .NET to be used on the client-side, enabling a consistent language and framework across the entire stack.
-   **Component-Based UI:** The user interface is built using reusable Razor components (`ProfileCard`, `ProjectList`, `SkillTags`), promoting a clean and maintainable codebase.
-   **Client-Side Logic:** All UI logic is written in C#, including fetching data from the backend API using `HttpClient`.
-   **Styling:** The application is styled with standard CSS, with each component having its own isolated stylesheet for better organization.

### 3. Database Management (SQLite)

The application uses a lightweight but powerful database to store its data.

-   **Database:** **SQLite** was chosen for its simplicity and file-based nature, making it ideal for development and small-to-medium scale applications.
-   **Migrations:** **EF Core Migrations** are used to manage the database schema. This code-first approach allows the database schema to evolve with the application's data models.
-   **Data Seeding:** The database is automatically seeded with sample data on initial startup, ensuring the application is populated with content for demonstration purposes.

### 4. Security Principles

Security has been a key consideration throughout the development process.

-   **HTTPS:** The application is configured to run over HTTPS by default, ensuring that all communication between the client and server is encrypted.
-   **CORS (Cross-Origin Resource Sharing):** The backend API implements a strict CORS policy, allowing requests only from the configured frontend application's origin. This prevents other websites from making unauthorized requests to the API.
-   **Future Enhancements:** The architecture is prepared for future security enhancements, such as implementing authentication and authorization using technologies like ASP.NET Core Identity or JWT (JSON Web Tokens) to secure endpoints and manage user access.

## How to Run the Project

1.  **Prerequisites:**
    *   .NET SDK (version specified in `global.json` or the project files)
    *   A code editor like Visual Studio or VS Code.

2.  **Run the Backend:**
    *   Navigate to the `SkillSnap` directory in your terminal.
    *   Run the command: `dotnet run --project Backend`
    *   The API will start, and the database (`skillsnap.db`) will be created and seeded if it doesn't exist.

3.  **Run the Frontend:**
    *   Open a new terminal in the `SkillSnap` directory.
    *   Run the command: `dotnet run --project Frontend`
    *   The Blazor WebAssembly application will start. You can now access it in your browser at the specified URL (e.g., `https://localhost:7123`).

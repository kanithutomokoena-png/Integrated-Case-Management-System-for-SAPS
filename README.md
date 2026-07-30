# Integrated Case Management System for SAPS

## Overview

The **Integrated Case Management System (ICMS)** is a web-based application developed to improve the management of criminal case information within the South African Police Service (SAPS). The system streamlines the process of capturing, managing, and tracking cases while maintaining a secure and organized repository for case information and evidence.
---

## Features

* User authentication and role-based access control
* Secure case registration and management
* Case status tracking
* Evidence upload and management
* Officer-specific case access
* Search and filtering of cases
* Dashboard displaying case statistics
* Archive and retrieval of completed cases

---

## Technologies Used

### Backend

* ASP.NET Core MVC
* C#
* Entity Framework Core
* ASP.NET Core Identity

### Frontend

* Razor Views
* HTML5
* CSS3
* Bootstrap 5
* JavaScript

### Database

* MySQL

---

## Development Tools

* Visual Studio Code
* Visual Studio
* Git
* GitHub
* MySQL Workbench
* .NET SDK
* Python 3

---

## System Modules

### Authentication

* User login
* Secure authentication
* Role-based authorization

### Case Management

* Register new cases
* View case details
* Update case information
* Track investigation status

### Evidence Management

* Upload digital evidence
* Store supporting documents
* Associate evidence with cases

### Dashboard

* Total cases
* Open cases
* Closed cases
* Investigation statistics

---

## Project Structure

```text
Integrated-Case-Management-System-for-SAPS
│
├── Controllers/
├── Models/
├── Views/
├── Data/
├── Services/
├── wwwroot/
├── Program.cs
├── appsettings.json
└── README.md
```

---

## Getting Started

### Prerequisites

* .NET SDK
* MySQL Server
* Visual Studio Code or Visual Studio
* Git

### Installation

1. Clone the repository.

```bash
git clone https://github.com/kanithutomokoena-png/Integrated-Case-Management-System-for-SAPS.git
```

2. Navigate to the project folder.

```bash
cd Integrated-Case-Management-System-for-SAPS
```

3. Configure the MySQL connection string in `appsettings.json`.

4. Restore dependencies.

```bash
dotnet restore
```

5. Apply Entity Framework Core migrations.

```bash
dotnet ef database update
```

6. Run the application.

```bash
dotnet run
```

---

## Future Improvements

* Email notifications
* Advanced reporting and analytics
* Interactive crime heat maps
* Facial recognition integration
* Mobile application
* Cloud deployment
* Real-time crime dashboards

---

## Project Objectives

* Improve efficiency in case management.
* Reduce manual administrative work.
* Enhance case tracking and evidence management.
* Provide secure access to authorized personnel.

---

## Repository

GitHub Repository:

https://github.com/kanithutomokoena-png/Integrated-Case-Management-System-for-SAPS

---

## Author

**Kananelo Thuto Mokoena**

Software Developer

---

## License

This project was developed for educational and research purposes.


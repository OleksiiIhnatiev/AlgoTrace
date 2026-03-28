<a id="readme-top"></a>

<!-- PROJECT SHIELDS -->
[![Contributors][contributors-shield]][contributors-url]
[![Issues][issues-shield]][issues-url]
[![LinkedIn][linkedin-shield]][linkedin-url]

<!-- PROJECT LOGO -->
<br />
<div align="center">
  <h3 align="center">AlgoTrace</h3>
</div>

<!-- TABLE OF CONTENTS -->
<details>
  <summary>Table of Contents</summary>
  <ol>
    <li>
      <a href="#about-the-project">About The Project</a>
      <ul>
        <li><a href="#key-features">Key Features</a></li>
        <li><a href="#built-with">Built With</a></li>
      </ul>
    </li>
    <li>
      <a href="#getting-started">Getting Started</a>
      <ul>
        <li><a href="#prerequisites">Prerequisites</a></li>
        <li><a href="#installation-&-launch">Installation & Launch</a></li>
      </ul>
    </li>
    <li><a href="#usage">Usage</a></li>
    <li><a href="#roadmap">Roadmap</a></li>
    <li><a href="#contributing">Contributing</a></li>
    <li><a href="#contact">Contact</a></li>
  </ol>
</details>

<!-- ABOUT THE PROJECT -->
## About The Project

AlgoTrace is a comprehensive web application designed to detect source code plagiarism through sophisticated multi-level analysis. Whether you are an educator evaluating student assignments or a developer auditing a codebase, AlgoTrace provides deep insights into code similarity beyond simple text matching.

### Key Features

* **Intuitive User Interface (Vue.js):** A highly responsive UI that allows users to interact with the system seamlessly. You can paste code directly into the editor or upload source files. The results are displayed in a human-readable format, highlighting the exact matching fragments, overall similarity percentages, and providing brief, actionable explanations.
* **Robust Server Logic (C# / ASP.NET):** The backend is responsible for handling heavy computational tasks. It manages secure user authorization, saves uploaded code to the database, and executes various advanced anti-plagiarism algorithms (e.g., Levenshtein distance, AST structure comparisons).
* **Reliable Data Storage (Microsoft SQL Server):** The application utilizes MS SQL Server to securely store user accounts, scan histories, and comparison results. This allows for historical tracking and deep analysis of matches across different code submissions over time.

<p align="right">(<a href="#readme-top">back to top</a>)</p>

### Built With

* [![Vue][Vue.js]][Vue-url]
* [![Dotnet][Dotnet-shield]][Dotnet-url]
* [![MicrosoftSQLServer][SQL-shield]][SQL-url]
* [![Docker][Docker-shield]][Docker-url]

<p align="right">(<a href="#readme-top">back to top</a>)</p>

<!-- GETTING STARTED -->
## Getting Started

Follow these instructions to get a local copy of the project up and running.

### Prerequisites

You will need to have Docker installed on your machine to build and run the containers.
* [Docker Desktop](https://www.docker.com/products/docker-desktop/) (includes Docker Compose)
* Git

### Installation & Launch

1. Clone the repository
   ```sh
   git clone https://github.com/OleksiiIhnatiev/AlgoTrace.git
   ```
2. Navigate to the project directory
   ```sh
   cd AlgoTrace
   ```
3. Build and launch the application using Docker Compose
   ```sh
   docker-compose up -d --build
   ```
4. Once the containers are running, open your browser and navigate to:
   ```text
   http://localhost:8080/
   ```
### Managing the Application

* **To stop the application:**
  Run the following command to stop and remove the containers, networks, and volumes tied to the application:
  ```sh
  docker-compose down
  ```
* **To apply code changes:**
  If you modify the source code or configurations, you need to rebuild the Docker images. Run the launch command again with the `--build` flag:
  ```sh
  docker-compose up -d --build
  ```
<p align="right">(<a href="#readme-top">back to top</a>)</p>

<!-- USAGE EXAMPLES -->
## Usage

1. **Register/Login:** Create a new account or log into the application.
2. **Upload Code:** Use the dashboard to either paste code snippets directly or upload files (e.g., `.cs`, `.py`, `.js`).
3. **Select Algorithms:** Choose the depth of your analysis (Textual, Token-based, Tree/AST, or Graph).
4. **Review Results:** The app will generate a detailed report showing side-by-side comparisons, matched lines, and an overall similarity score.

<p align="right">(<a href="#readme-top">back to top</a>)</p>

<!-- ROADMAP -->
## Roadmap

- [x] Basic Vue.js UI setup
- [x] C# API structure and User Auth
- [x] Database Integration (MS SQL)
- [x] Text-level checking algorithms (e.g., Levenshtein)
- [ ] Abstract Syntax Tree (AST) comparison implementation
- [ ] Support for broader language parsing
- [ ] Export reports to PDF/CSV

See the open issues for a full list of proposed features (and known issues).

<p align="right">(<a href="#readme-top">back to top</a>)</p>

<!-- CONTRIBUTING -->
## Contributing

Contributions are what make the open source community such an amazing place to learn, inspire, and create. Any contributions you make are **greatly appreciated**.

1. Fork the Project
2. Create your Feature Branch (`git checkout -b feature/AmazingFeature`)
3. Commit your Changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the Branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

<a href="https://github.com/OleksiiIhnatiev/AlgoTrace/graphs/contributors">
  <img src="https://contrib.rocks/image?repo=OleksiiIhnatiev/AlgoTrace" />
</a>


<p align="right">(<a href="#readme-top">back to top</a>)</p>

<!-- CONTACT -->
## Contact

Your Name - @your_twitter - email@example.com

Project Link: https://github.com/OleksiiIhnatiev/AlgoTrace

<p align="right">(<a href="#readme-top">back to top</a>)</p>

<!-- MARKDOWN LINKS & IMAGES -->
[contributors-shield]: https://img.shields.io/github/contributors/OleksiiIhnatiev/AlgoTrace.svg?style=for-the-badge
[contributors-url]: https://github.com/OleksiiIhnatiev/AlgoTrace/graphs/contributors
[forks-shield]: https://img.shields.io/github/forks/OleksiiIhnatiev/AlgoTrace.svg?style=for-the-badge
[stars-shield]: https://img.shields.io/github/stars/OleksiiIhnatiev/AlgoTrace.svg?style=for-the-badge
[issues-shield]: https://img.shields.io/github/issues/OleksiiIhnatiev/AlgoTrace.svg?style=for-the-badge
[issues-url]: https://github.com/OleksiiIhnatiev/AlgoTrace/issues
[linkedin-shield]: https://img.shields.io/badge/-LinkedIn-black.svg?style=for-the-badge&logo=linkedin&colorB=555
[linkedin-url]: https://www.linkedin.com/in/oleksii-ihnatiev-75232a329/
[Vue.js]: https://img.shields.io/badge/Vue.js-35495E?style=for-the-badge&logo=vuedotjs&logoColor=4FC08D
[Vue-url]: https://vuejs.org/
[Dotnet-shield]: https://img.shields.io/badge/.NET-5C2D91?style=for-the-badge&logo=.net&logoColor=white
[Dotnet-url]: https://dotnet.microsoft.com/
[SQL-shield]: https://img.shields.io/badge/Microsoft_SQL_Server-CC2927?style=for-the-badge&logo=microsoft-sql-server&logoColor=white
[SQL-url]: https://www.microsoft.com/en-us/sql-server
[Docker-shield]: https://img.shields.io/badge/Docker-2496ED?style=for-the-badge&logo=docker&logoColor=white
[Docker-url]: https://www.docker.com/

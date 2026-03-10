# Project Abstract: Online Examination System with Anti-Cheat Proctoring Features

## 1. Project Idea

The Online Examination System is designed to provide educational institutions with a secure, robust, and scalable platform for conducting online assessments and examinations. Traditional online evaluations often suffer from academic malpractice, such as students browsing external resources during exams. To address these challenges, the primary objective of this project is to build an end-to-end framework that not only digitizes the complete examination lifecycle—from administrative planning to final grading—but also enforces strict anti-cheat and proctoring measures.  

The platform allows administrators to oversee the organizational structure mapping colleges, courses, and subjects. Teachers are empowered to author rich, multimedia questions (with optional supporting images) and configure exams with tight time constraints. For students, the platform ensures transparency by calculating results automatically post-exam, while also retaining the ability to hide results from displaying immediately during live examinations and handling network disconnects or abandoned attempts smoothly.

## 2. Implementation

The system is implemented as an MVC application leveraging modern Microsoft web technologies to ensure enterprise-level performance and security:
- **Core Technology Stack:** The application is built on **ASP.NET Core MVC** (.NET Core) serving as the robust backend, while utilizing **Entity Framework Core (EF Core)** for efficient data persistence and complex relational mapping across diverse entities (Admin, Teacher, Student, Courses, Subjects, Exams, Questions, etc.). 
- **Database Management:** **Microsoft SQL Server** serves as the primary database, enforcing foreign-key constraints and data integrity schemas mapped directly from C# models.
- **Authentication & Authorization:** Secure access and role enforcement rely entirely on the Session state instead of a standard cookie-based or claims approach. The application uses custom action filters: `SessionValidationFilter` to check if a user is logged in by validating `HttpContext.Session.GetInt32("UserId")`, and `AdminAuthorizeFilter` to enforce role-based access by checking `session.GetString("Role")`. Passwords are secured using custom hashing mechanisms (e.g., PBKDF2 bcrypt-style helpers).
- **Dynamic Exam Logic & Client-Side Proctoring:** An interactive candidate dashboard is implemented leveraging Vanilla JavaScript, HTML5, and CSS. The examination frontend utilizes dynamic countdown timers synchronized with the server's exam duration boundaries. JavaScript-driven proctoring enforces a **Fullscreen-only mode**, significantly restricting candidates from navigating away, opening new tabs, or using external tools.
- **Background Event Handling:** Advanced validation prevents teachers from setting exam windows shorter than the duration, and asynchronous tasks automatically finalize abandoned or uncompleted exam attempts upon time expiration, successfully sealing the attempt and persisting generated scores.
- **Data Imports:** An integrated `ImportService` processes excel sheets to dynamically seed the database with subjects, eliminating manual data entry overhead for large institutions. 

## 3. Results

The implementation yielded a fully functional and highly secure online examination environment. The success of the project is highlighted by the following outcomes:

- **Enhanced Academic Integrity:** The seamless integration of fullscreen requirements, dynamic timers, and behavior logs drastically reduced the opportunity for students to commit malpractice.
- **Streamlined Examination Workflows:** Educators successfully saved administrative time using bulk Excel importing for subjects, easily linking questions to exams, and relying on autonomous validation bounds for exam setups. 
- **Reliable Evaluation & Auto-Grading:** Students receive calculated, immediate feedback upon completion (unless deferred for concurrent live exams). Abandoned sessions no longer skew analytics, as the system reliably concludes dangling attempts on its own—scoring the subset of questions completed up to the abort point.
- **Responsive & Accessible Design:** All three user roles (Admin, Teacher, and Student) benefit from intuitive dashboards structured around clear, responsive views, enabling fluid transitions from exam creation to exam execution across various devices. 

In summary, the transition to this Online Examination System modernizes assessment, reinforces test integrity, and scales reliably for continuous educational usage.

using EnrollDesk.Api.Domain;
using EnrollDesk.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace EnrollDesk.Api.Data;

/// <summary>Idempotent demo-data seed: a registrar account, a student account, a catalog and one active term.</summary>
public static class DbInitializer
{
    public const string DemoAdminEmail = "admin@enrolldesk.edu";
    public const string DemoAdminPassword = "Admin#2026";
    public const string DemoStudentEmail = "student@enrolldesk.edu";
    public const string DemoStudentPassword = "Student#2026";

    public static async Task SeedAsync(EnrollDeskDbContext db)
    {
        if (await db.Users.AnyAsync())
        {
            return;
        }

        var (adminHash, adminSalt) = PasswordHasher.Hash(DemoAdminPassword);
        var (studentHash, studentSalt) = PasswordHasher.Hash(DemoStudentPassword);

        var admin = new User
        {
            FullName = "Morgan Reyes",
            Email = DemoAdminEmail,
            PasswordHash = adminHash,
            PasswordSalt = adminSalt,
            Role = UserRole.Registrar,
            CreatedAtUtc = DateTime.UtcNow
        };

        var student = new User
        {
            FullName = "Jamie Torres",
            Email = DemoStudentEmail,
            PasswordHash = studentHash,
            PasswordSalt = studentSalt,
            Role = UserRole.Student,
            CreatedAtUtc = DateTime.UtcNow
        };

        db.Users.AddRange(admin, student);

        var term = new Term
        {
            Name = "2026-2",
            StartDate = new DateOnly(2026, 8, 17),
            EndDate = new DateOnly(2026, 12, 18),
            AddDropDeadline = new DateOnly(2026, 8, 31),
            WithdrawalDeadline = new DateOnly(2026, 11, 20),
            MaxCreditsPerTerm = 18,
            IsActive = true
        };
        db.Terms.Add(term);

        var intro = new Course { Code = "CS101", Title = "Introduction to Programming", Credits = 4, Description = "Variables, control flow and functions in a modern language." };
        var dataStructures = new Course { Code = "CS201", Title = "Data Structures", Credits = 4, Description = "Lists, trees, hash tables and their complexity trade-offs." };
        var algorithms = new Course { Code = "CS301", Title = "Algorithms", Credits = 4, Description = "Sorting, graph traversal and algorithmic analysis." };
        var calcOne = new Course { Code = "MATH101", Title = "Calculus I", Credits = 4, Description = "Limits, derivatives and their applications." };
        var calcTwo = new Course { Code = "MATH201", Title = "Calculus II", Credits = 4, Description = "Integration techniques and infinite series." };
        var writing = new Course { Code = "ENG101", Title = "Academic Writing", Credits = 3, Description = "Composition and argumentation for academic work." };

        db.Courses.AddRange(intro, dataStructures, algorithms, calcOne, calcTwo, writing);

        db.CoursePrerequisites.AddRange(
            new CoursePrerequisite { Course = dataStructures, RequiredCourse = intro },
            new CoursePrerequisite { Course = algorithms, RequiredCourse = dataStructures },
            new CoursePrerequisite { Course = calcTwo, RequiredCourse = calcOne }
        );

        db.Sections.AddRange(
            new Section
            {
                Course = intro, Term = term, SectionNumber = "001", InstructorName = "Dr. Alan Kay", Location = "Hall A-101",
                Days = MeetingDays.Monday | MeetingDays.Wednesday | MeetingDays.Friday,
                StartTime = new TimeSpan(9, 0, 0), EndTime = new TimeSpan(10, 0, 0),
                Capacity = 2, SeatsTaken = 0
            },
            new Section
            {
                Course = intro, Term = term, SectionNumber = "002", InstructorName = "Dr. Alan Kay", Location = "Hall A-102",
                Days = MeetingDays.Tuesday | MeetingDays.Thursday,
                StartTime = new TimeSpan(9, 0, 0), EndTime = new TimeSpan(10, 30, 0),
                Capacity = 30, SeatsTaken = 0
            },
            new Section
            {
                Course = dataStructures, Term = term, SectionNumber = "001", InstructorName = "Dr. Grace Hopper", Location = "Hall B-201",
                Days = MeetingDays.Monday | MeetingDays.Wednesday,
                StartTime = new TimeSpan(11, 0, 0), EndTime = new TimeSpan(12, 30, 0),
                Capacity = 25, SeatsTaken = 0
            },
            new Section
            {
                Course = calcOne, Term = term, SectionNumber = "001", InstructorName = "Dr. Katherine Johnson", Location = "Hall C-010",
                Days = MeetingDays.Monday | MeetingDays.Wednesday | MeetingDays.Friday,
                StartTime = new TimeSpan(8, 0, 0), EndTime = new TimeSpan(9, 0, 0),
                Capacity = 30, SeatsTaken = 0
            },
            new Section
            {
                Course = writing, Term = term, SectionNumber = "001", InstructorName = "Prof. Toni Morrison", Location = "Hall D-005",
                Days = MeetingDays.Tuesday | MeetingDays.Thursday,
                StartTime = new TimeSpan(11, 0, 0), EndTime = new TimeSpan(12, 0, 0),
                Capacity = 20, SeatsTaken = 0
            }
        );

        await db.SaveChangesAsync();
    }
}

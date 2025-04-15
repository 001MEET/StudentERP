using StudentERP.Models;
using System;
using System.Threading.Tasks;

namespace StudentERP.Repository.IRepository
{
    public interface IStudentLoginRepository
    {
        Task<bool> RegisterStudent(StudentLogin studentLogin);
        Task<StudentLogin> GetStudentByEmail(string email);
        string HashPassword(string password);
        Task<bool> UpdateStudent(StudentLogin studentLogin);
        Task<StudentProfile?> GetStudentProfile(Guid studentId);
        Task<bool> SaveOrReplaceStudentProfile(StudentProfile profile);

        Task<PersonalDetails?> GetPersonalDetails(Guid studentId);
        Task<ContactDetail?> GetContactDetails(Guid studentId);
        Task<ParentsDetails?> GetParentsDetails(Guid studentId);
        Task SendLoginNotificationEmail(string email, bool isSuccess);

    }
}
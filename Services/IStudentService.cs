using System.Collections.Generic;
using System.Threading.Tasks;
using TmsApi.Models;

namespace TmsApi.Services;

public interface IStudentService
{
    Task<IEnumerable<Student>> GetAllAsync();
    Task<Student?> GetByIdAsync(string id);
    Task<Student> RegisterAsync(Student student);
    Task<bool> RemoveAsync(string id);
}
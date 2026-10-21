using Microsoft.Data.SqlClient;
using StudentManager.Models;

namespace StudentManager.Data;

/// <summary>
/// Repository: semua akses database (ADO.NET) ada di sini.
/// ViewModel tidak pernah menulis SQL secara langsung.
/// </summary>
public class StudentRepository
{
    private readonly string connectionString =
        @"Server=(localdb)\MSSQLLocalDB;
          Database=StudentDB;
          Trusted_Connection=True;
          TrustServerCertificate=True;";

    // ===================== READ =====================
    public List<Student> GetAll()
    {
        var students = new List<Student>();

        using SqlConnection connection = new SqlConnection(connectionString);

        string sql =
            "SELECT Id, NIM, Nama, Jurusan, Gender, Email " +
            "FROM Students ORDER BY Id DESC";

        using SqlCommand command = new SqlCommand(sql, connection);

        connection.Open();
        using SqlDataReader reader = command.ExecuteReader();

        while (reader.Read())
            students.Add(Map(reader));

        return students;
    }

    // ===================== INSERT =====================
    public void Insert(Student student)
    {
        using SqlConnection connection = new SqlConnection(connectionString);

        string sql =
            "INSERT INTO Students " +
            "(NIM, Nama, Jurusan, Gender, Email) " +
            "VALUES (@NIM,@Nama,@Jurusan,@Gender,@Email)";

        using SqlCommand command = new SqlCommand(sql, connection);
        AddParameters(command, student);

        connection.Open();
        command.ExecuteNonQuery();
    }

    // ===================== UPDATE =====================
    public void Update(Student student)
    {
        using SqlConnection connection = new SqlConnection(connectionString);

        string sql =
            "UPDATE Students SET NIM=@NIM, Nama=@Nama, " +
            "Jurusan=@Jurusan, Gender=@Gender, Email=@Email " +
            "WHERE Id=@Id";

        using SqlCommand command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", student.Id);
        AddParameters(command, student);

        connection.Open();
        command.ExecuteNonQuery();
    }

    // ===================== DELETE =====================
    public void Delete(int id)
    {
        using SqlConnection connection = new SqlConnection(connectionString);

        using SqlCommand command =
            new SqlCommand("DELETE FROM Students WHERE Id=@Id", connection);
        command.Parameters.AddWithValue("@Id", id);

        connection.Open();
        command.ExecuteNonQuery();
    }

    // ===================== SEARCH =====================
    public List<Student> Search(string keyword)
    {
        var students = new List<Student>();

        using SqlConnection connection = new SqlConnection(connectionString);

        string sql =
            "SELECT Id, NIM, Nama, Jurusan, Gender, Email " +
            "FROM Students WHERE NIM LIKE @Keyword " +
            "OR Nama LIKE @Keyword OR Jurusan LIKE @Keyword " +
            "OR Email LIKE @Keyword " +
            "ORDER BY Id DESC";

        using SqlCommand command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Keyword", "%" + keyword.Trim() + "%");

        connection.Open();
        using SqlDataReader reader = command.ExecuteReader();

        while (reader.Read())
            students.Add(Map(reader));

        return students;
    }

    // ===================== VALIDASI NIM UNIK =====================
    /// <summary>
    /// Cek apakah NIM sudah dipakai mahasiswa lain (excludeId = data yang sedang diedit).
    /// </summary>
    public bool NimExists(string nim, int excludeId)
    {
        using SqlConnection connection = new SqlConnection(connectionString);

        using SqlCommand command = new SqlCommand(
            "SELECT COUNT(1) FROM Students WHERE NIM=@NIM AND Id<>@Id", connection);
        command.Parameters.AddWithValue("@NIM", nim);
        command.Parameters.AddWithValue("@Id", excludeId);

        connection.Open();
        return Convert.ToInt32(command.ExecuteScalar()) > 0;
    }

    // ===================== HELPER =====================
    private static Student Map(SqlDataReader reader) => new()
    {
        Id = Convert.ToInt32(reader["Id"]),
        NIM = reader["NIM"].ToString()!,
        Nama = reader["Nama"].ToString()!,
        Jurusan = reader["Jurusan"].ToString()!,
        Gender = reader["Gender"].ToString()!,
        Email = reader["Email"].ToString()!   // DBNull -> ""
    };

    private static void AddParameters(SqlCommand command, Student student)
    {
        command.Parameters.AddWithValue("@NIM", student.NIM.Trim());
        command.Parameters.AddWithValue("@Nama", student.Nama.Trim());
        command.Parameters.AddWithValue("@Jurusan", student.Jurusan);
        command.Parameters.AddWithValue("@Gender", student.Gender);
        command.Parameters.AddWithValue("@Email",
            string.IsNullOrWhiteSpace(student.Email) ? DBNull.Value : student.Email.Trim());
    }
}

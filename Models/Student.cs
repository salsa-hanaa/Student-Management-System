namespace StudentManager.Models;

/// <summary>
/// Model: representasi satu baris data pada tabel Students.
/// </summary>
public class Student
{
    public int Id { get; set; }
    public string NIM { get; set; } = "";
    public string Nama { get; set; } = "";
    public string Jurusan { get; set; } = "";
    public string Gender { get; set; } = "";
    public string Email { get; set; } = "";

    /// <summary>
    /// Nomor urut untuk kolom "No" di DataGrid (tidak disimpan ke database).
    /// </summary>
    public int No { get; set; }

    /// <summary>
    /// Membuat salinan objek, dipakai agar form tidak langsung
    /// mengubah data di tabel sebelum tombol Simpan ditekan.
    /// </summary>
    public Student Clone() => new()
    {
        Id = Id,
        NIM = NIM,
        Nama = Nama,
        Jurusan = Jurusan,
        Gender = Gender,
        Email = Email
    };
}

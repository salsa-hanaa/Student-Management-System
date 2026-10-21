using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using Microsoft.Data.SqlClient;
using StudentManager.Data;
using StudentManager.Models;

namespace StudentManager.ViewModels;

/// <summary>
/// ViewModel: menyimpan state UI, command, pencarian, statistik, dan validasi.
/// View (XAML) hanya melakukan Binding ke property dan command di kelas ini.
/// </summary>
public class StudentViewModel : INotifyPropertyChanged
{
    private readonly StudentRepository _repository;

    /// <summary>Seluruh data dari database (dipakai untuk statistik).</summary>
    private List<Student> _allStudents = new();

    // ===================== DATA UNTUK BINDING =====================

    /// <summary>Data yang tampil di DataGrid (bisa hasil pencarian).</summary>
    public ObservableCollection<Student> Students { get; } = new();

    public List<string> JurusanList { get; } = new() { "Informatika", "Sistem Informasi" };
    public List<string> GenderList { get; } = new() { "Laki-laki", "Perempuan" };

    public string TodayText =>
        DateTime.Now.ToString("dddd, dd MMMM yyyy", new CultureInfo("id-ID"));

    private Student? _selectedStudent;
    /// <summary>Baris yang dipilih di DataGrid. Saat dipilih, datanya disalin ke form.</summary>
    public Student? SelectedStudent
    {
        get => _selectedStudent;
        set
        {
            _selectedStudent = value;
            OnPropertyChanged();
            if (value != null)
                EditStudent(value);
        }
    }

    private Student _formStudent = new();
    /// <summary>Data yang sedang diisi/diedit di form input.</summary>
    public Student FormStudent
    {
        get => _formStudent;
        set
        {
            _formStudent = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsEditMode));
            OnPropertyChanged(nameof(FormTitle));
            OnPropertyChanged(nameof(SaveButtonText));
        }
    }

    public bool IsEditMode => FormStudent.Id != 0;
    public string FormTitle => IsEditMode ? "EDIT MAHASISWA" : "INPUT MAHASISWA";
    public string SaveButtonText => IsEditMode ? "Update" : "Simpan";

    private string _searchText = "";
    public string SearchText
    {
        get => _searchText;
        set { _searchText = value; OnPropertyChanged(); }
    }

    private string _statusMessage = "";
    public string StatusMessage
    {
        get => _statusMessage;
        set { _statusMessage = value; OnPropertyChanged(); }
    }

    private bool _isError;
    public bool IsError
    {
        get => _isError;
        set { _isError = value; OnPropertyChanged(); }
    }

    // ===================== STATISTIK =====================
    public int TotalStudents => _allStudents.Count;
    public int TotalInformatika => _allStudents.Count(x => x.Jurusan == "Informatika");
    public int TotalSistemInformasi => _allStudents.Count(x => x.Jurusan == "Sistem Informasi");
    public int TotalLakiLaki => _allStudents.Count(x => x.Gender == "Laki-laki");
    public int TotalPerempuan => _allStudents.Count(x => x.Gender == "Perempuan");
    public string DisplayInfo => $"Menampilkan {Students.Count} dari {TotalStudents} mahasiswa";

    // ===================== COMMANDS =====================
    public ICommand SaveCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand ResetCommand { get; }
    public ICommand SearchCommand { get; }
    public ICommand ClearSearchCommand { get; }

    public StudentViewModel()
    {
        _repository = new StudentRepository();

        SaveCommand = new RelayCommand(Save);
        EditCommand = new RelayCommand(p => { if (p is Student s) EditStudent(s); });
        DeleteCommand = new RelayCommand(Delete, CanDelete);
        ResetCommand = new RelayCommand(Reset);
        SearchCommand = new RelayCommand(Search);
        ClearSearchCommand = new RelayCommand(ClearSearch);

        LoadData();
    }

    // ===================== LOGIKA =====================

    private void LoadData()
    {
        try
        {
            _allStudents = _repository.GetAll();

            // Jika sedang ada kata kunci pencarian, tampilkan hasil pencarian
            var display = string.IsNullOrWhiteSpace(SearchText)
                ? _allStudents
                : _repository.Search(SearchText);

            FillGrid(display);
        }
        catch (SqlException ex)
        {
            ShowDatabaseError(ex);
        }
    }

    private void FillGrid(IEnumerable<Student> data)
    {
        Students.Clear();
        int no = 1;
        foreach (var student in data)
        {
            student.No = no++;
            Students.Add(student);
        }
        RefreshStatistics();
    }

    private void EditStudent(Student student)
    {
        // Salin data supaya perubahan di form belum mengubah tabel
        FormStudent = student.Clone();
        SetStatus($"Mode edit: {student.Nama}. Ubah data lalu klik Update.", false);
    }

    private void Save()
    {
        string? error = Validate(FormStudent);
        if (error != null)
        {
            SetStatus(error, true);
            return;
        }

        try
        {
            if (_repository.NimExists(FormStudent.NIM.Trim(), FormStudent.Id))
            {
                SetStatus($"NIM {FormStudent.NIM} sudah terdaftar.", true);
                return;
            }

            bool isNew = FormStudent.Id == 0;
            if (isNew)
                _repository.Insert(FormStudent);
            else
                _repository.Update(FormStudent);

            string nama = FormStudent.Nama;
            LoadData();
            Reset();
            SetStatus(isNew ? $"Data {nama} berhasil disimpan." : $"Data {nama} berhasil diperbarui.", false);
        }
        catch (SqlException ex)
        {
            ShowDatabaseError(ex);
        }
    }

    private bool CanDelete(object? parameter) => parameter is Student || IsEditMode;

    private void Delete(object? parameter)
    {
        // Parameter dari tombol hapus di baris DataGrid, atau data yang sedang diedit di form
        Student? target = parameter as Student ?? (IsEditMode ? FormStudent : null);
        if (target == null) return;

        var confirm = MessageBox.Show(
            $"Hapus data mahasiswa {target.Nama} ({target.NIM})?",
            "Konfirmasi Hapus",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.Yes) return;

        try
        {
            _repository.Delete(target.Id);
            string nama = target.Nama;
            LoadData();
            Reset();
            SetStatus($"Data {nama} berhasil dihapus.", false);
        }
        catch (SqlException ex)
        {
            ShowDatabaseError(ex);
        }
    }

    private void Search()
    {
        try
        {
            var result = string.IsNullOrWhiteSpace(SearchText)
                ? _repository.GetAll()
                : _repository.Search(SearchText);

            FillGrid(result);
        }
        catch (SqlException ex)
        {
            ShowDatabaseError(ex);
        }
    }

    private void ClearSearch()
    {
        SearchText = "";
        Search();
    }

    private void Reset()
    {
        _selectedStudent = null;
        OnPropertyChanged(nameof(SelectedStudent));
        FormStudent = new Student();
        SetStatus("", false);
    }

    private static string? Validate(Student s)
    {
        if (string.IsNullOrWhiteSpace(s.NIM)) return "NIM wajib diisi.";
        if (!Regex.IsMatch(s.NIM.Trim(), @"^\d+$")) return "NIM hanya boleh berisi angka.";
        if (string.IsNullOrWhiteSpace(s.Nama)) return "Nama wajib diisi.";
        if (string.IsNullOrWhiteSpace(s.Jurusan)) return "Pilih jurusan terlebih dahulu.";
        if (string.IsNullOrWhiteSpace(s.Gender)) return "Pilih gender terlebih dahulu.";
        if (!string.IsNullOrWhiteSpace(s.Email) &&
            !Regex.IsMatch(s.Email.Trim(), @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            return "Format email tidak valid.";
        return null;
    }

    private void SetStatus(string message, bool isError)
    {
        IsError = isError;
        StatusMessage = message;
    }

    private void ShowDatabaseError(SqlException ex)
    {
        SetStatus("Gagal terhubung ke database.", true);
        MessageBox.Show(
            "Tidak dapat mengakses database StudentDB.\n\n" +
            "Pastikan SQL Server LocalDB berjalan dan script Database/StudentDB.sql sudah dijalankan.\n\n" +
            "Detail: " + ex.Message,
            "Database Error",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }

    private void RefreshStatistics()
    {
        OnPropertyChanged(nameof(TotalStudents));
        OnPropertyChanged(nameof(TotalInformatika));
        OnPropertyChanged(nameof(TotalSistemInformasi));
        OnPropertyChanged(nameof(TotalLakiLaki));
        OnPropertyChanged(nameof(TotalPerempuan));
        OnPropertyChanged(nameof(DisplayInfo));
    }

    // ===================== INotifyPropertyChanged =====================
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
